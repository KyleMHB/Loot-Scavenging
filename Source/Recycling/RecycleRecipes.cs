using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using LootScavenging.Model;
using RimWorld;
using Verse;

namespace LootScavenging;

/// <summary>
/// Generates one recycling RecipeDef per scrap tier from the tier table, in the game's own implied-def pass, so reference
/// resolution and short hashes come for free. Each recipe is attached to benches through its own recipeUsers; no bench's
/// recipes list is touched, so another mod's bills on the same bench are unaffected. A recipe has no products: everything
/// comes through the Smelted special product, which ScrapThing rolls from the tier's loot pool, so a bill has no
/// keep-in-stock count.
/// </summary>
[HarmonyPatch(typeof(DefGenerator), nameof(DefGenerator.GenerateImpliedDefs_PreResolve))]
internal static class RecycleRecipes
{
    public const string DefNamePrefix = "LS_Recycle_"; // not translated
    public const string StudiedDefNamePrefix = "LS_RecycleStudied_"; // not translated

    private static void Postfix(bool hotReload)
    {
        try
        {
            foreach (RecipeDef recipe in Generate(hotReload).ToList())
                DefGenerator.AddImpliedDef(recipe, hotReload);
        }
        catch (System.Exception e)
        {
            Log.Error($"[Loot Scavenging] Generating recycling recipes failed: {e}"); // not translated
        }
    }

    private static IEnumerable<RecipeDef> Generate(bool hotReload)
    {
        var tiers = (ScrapTier[])System.Enum.GetValues(typeof(ScrapTier));
        var table = new List<(ScrapTier tier, string[] benches)>();
        foreach (ScrapTier t in tiers)
        {
            ScrapTierDef d = ScrapTierTable.For(t);
            if (d != null)
                table.Add((t, d.benches.Where(b => b != null).Select(b => b.defName).ToArray()));
        }

        foreach (ScrapTier t in tiers)
        {
            ScrapTierDef def = ScrapTierTable.For(t);
            if (def == null || def.scrap == null)
                continue;

            string defName = DefNamePrefix + t;
            if (hotReload && DefDatabase<RecipeDef>.GetNamedSilentFail(defName) != null)
                continue;

            var users = new List<ThingDef>();
            foreach (string benchName in RecycleBenches.UsersFor(t, table))
            {
                ThingDef bench = DefDatabase<ThingDef>.GetNamedSilentFail(benchName);
                if (bench != null)
                    users.Add(bench);
            }
            if (users.Count == 0)
                continue;

            yield return Build(def, def.scrap, defName, users);

            // Studied scrap (ticket 05) recycles at the same benches, from the same pool at the tier's studied factor.
            string studiedName = StudiedDefNamePrefix + t;
            if (def.studiedScrap != null && !(hotReload && DefDatabase<RecipeDef>.GetNamedSilentFail(studiedName) != null))
                yield return Build(def, def.studiedScrap, studiedName, users);
        }
    }

    private static RecipeDef Build(ScrapTierDef def, ThingDef scrap, string defName, List<ThingDef> users)
    {
        var recipe = new RecipeDef
        {
            defName = defName,
            label = "LootScavenging_RecycleLabel".Translate(scrap.label),
            jobString = "LootScavenging_RecycleJob".Translate(scrap.label),
            description = "LootScavenging_RecycleDescription".Translate(scrap.label),
            workAmount = def.workPerUnit * def.batchSize,
            workSpeedStat = DefDatabase<StatDef>.GetNamedSilentFail("SmeltingSpeed"),
            effectWorking = DefDatabase<EffecterDef>.GetNamedSilentFail("Smelt"),
            soundWorking = DefDatabase<SoundDef>.GetNamedSilentFail("Recipe_Smelt"),
            specialProducts = new List<SpecialProductType> { SpecialProductType.Smelted },
            recipeUsers = users,
            ingredients = new List<IngredientCount>(),
            fixedIngredientFilter = new ThingFilter(),
            defaultIngredientFilter = new ThingFilter(),
        };

        var ingredient = new IngredientCount();
        ingredient.SetBaseCount(def.batchSize);
        ingredient.filter.SetAllow(scrap, true);
        recipe.ingredients.Add(ingredient);
        recipe.fixedIngredientFilter.SetAllow(scrap, true);
        recipe.defaultIngredientFilter.SetAllow(scrap, true);
        return recipe;
    }
}
