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
/// recipes list is touched, so another mod's bills on the same bench are unaffected.
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

            yield return Build(def, def.scrap, ScrapTierTable.Yields(t), defName, users);

            // Studied scrap (ticket 05) recycles at the same benches, at the tier's reduced studied yield.
            string studiedName = StudiedDefNamePrefix + t;
            if (def.studiedScrap != null && !(hotReload && DefDatabase<RecipeDef>.GetNamedSilentFail(studiedName) != null))
                yield return Build(def, def.studiedScrap, ScrapTierTable.StudiedYields(t), studiedName, users);
        }
    }

    private static RecipeDef Build(ScrapTierDef def, ThingDef scrap, List<YieldEntry<ThingDef>> entries, string defName,
        List<ThingDef> users)
    {

        var recipe = new RecipeDef
        {
            defName = defName,
            label = "LootScavenging_RecycleLabel".Translate(scrap.label),
            jobString = "LootScavenging_RecycleJob".Translate(scrap.label),
            description = "LootScavenging_RecycleDescription".Translate(scrap.label, YieldSummary(entries)),
            workAmount = def.workPerUnit * def.batchSize,
            workSpeedStat = DefDatabase<StatDef>.GetNamedSilentFail("SmeltingSpeed"),
            effectWorking = DefDatabase<EffecterDef>.GetNamedSilentFail("Smelt"),
            soundWorking = DefDatabase<SoundDef>.GetNamedSilentFail("Recipe_Smelt"),
            specialProducts = new List<SpecialProductType> { SpecialProductType.Smelted },
            workerCounterClass = typeof(RecipeWorkerCounter_Recycle),
            recipeUsers = users,
            products = YieldMath.Fixed(entries, def.batchSize)
                .Select(y => new ThingDefCountClass(y.Item, y.Count)).ToList(),
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

    /// <summary>"4x steel, 1x component (10% chance)": what one unit returns.</summary>
    public static string YieldSummary(IEnumerable<YieldEntry<ThingDef>> entries)
    {
        var parts = new List<string>();
        foreach (YieldEntry<ThingDef> e in entries)
        {
            if (e.IsFixed)
                parts.Add("LootScavenging_YieldFixed".Translate(e.Count, e.Item.label));
            else
                parts.Add("LootScavenging_YieldChance".Translate(e.Count, e.Item.label, (e.Chance * 100f).ToString("0.#")));
        }
        return parts.ToCommaList(useAnd: true);
    }
}
