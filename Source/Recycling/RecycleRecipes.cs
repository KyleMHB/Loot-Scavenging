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
    // The ×10 bills (ticket 19): the ×1 name with this added before the tier, e.g. LS_Recycle_Bulk_Spacer.
    public const string BulkInfix = "Bulk_"; // not translated

    private static readonly Dictionary<RecipeDef, ScrapTierDef> tierOf = new Dictionary<RecipeDef, ScrapTierDef>();
    private static readonly HashSet<RecipeDef> bulk = new HashSet<RecipeDef>();

    /// <summary>The tier a scrap or studied-scrap recycling recipe, ×1 or ×10, is for; false for every other recipe.</summary>
    public static bool TryGetTier(RecipeDef recipe, out ScrapTierDef tier)
    {
        tier = null;
        return recipe != null && tierOf.TryGetValue(recipe, out tier);
    }

    /// <summary>How many ×1 batches one iteration of the recipe uses: 10 for a ×10 bill, otherwise 1.</summary>
    public static int Factor(RecipeDef recipe) => recipe != null && bulk.Contains(recipe) ? BulkBills.Factor : 1;

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

            yield return Build(def, def.scrap, defName, users, false);
            string bulkName = DefNamePrefix + BulkInfix + t;
            if (!(hotReload && DefDatabase<RecipeDef>.GetNamedSilentFail(bulkName) != null))
                yield return Build(def, def.scrap, bulkName, users, true);

            // Studied scrap (ticket 05) recycles at the same benches, from the same pool at the tier's studied factor.
            if (def.studiedScrap == null)
                continue;
            string studiedName = StudiedDefNamePrefix + t;
            if (!(hotReload && DefDatabase<RecipeDef>.GetNamedSilentFail(studiedName) != null))
                yield return Build(def, def.studiedScrap, studiedName, users, false);
            string studiedBulkName = StudiedDefNamePrefix + BulkInfix + t;
            if (!(hotReload && DefDatabase<RecipeDef>.GetNamedSilentFail(studiedBulkName) != null))
                yield return Build(def, def.studiedScrap, studiedBulkName, users, true);
        }
    }

    private static RecipeDef Build(ScrapTierDef def, ThingDef scrap, string defName, List<ThingDef> users, bool isBulk)
    {
        // The ×10 bill names ten units, so it waits until ten are on hand and a smaller pile is left to the ×1 bill; the cap
        // patch then lets it take up to ten ×1 batches (BillIngredientCapPatch).
        RecipeDef recipe = BillRecipes.New(defName, users, new[] { scrap }, isBulk ? BulkBills.MinUnits : 1f, allowedByDefault: true);
        recipe.label = (isBulk ? "LootScavenging_RecycleBulkLabel" : "LootScavenging_RecycleLabel").Translate(scrap.label);
        recipe.jobString = "LootScavenging_RecycleJob".Translate(scrap.label);
        recipe.description = "LootScavenging_RecycleDescription".Translate(scrap.label);
        if (isBulk)
            recipe.description += " " + "LootScavenging_BulkBillNote".Translate(BulkBills.MinUnits.ToString());
        // Nominal, for the bill list: the real work is set per iteration from the units taken (BillWorkPatch).
        recipe.workAmount = def.workPerUnit * (isBulk ? BulkBills.MinUnits : 1);
        // The ×1 bill takes a whole stack of any size, so a lone unit is recycled too (vanilla "burn drugs" does the same).
        recipe.ignoreIngredientCountTakeEntireStacks = !isBulk;
        recipe.specialProducts = new List<SpecialProductType> { SpecialProductType.Smelted };
        BillRecipes.LikeSmelting(recipe);
        tierOf[recipe] = def;
        if (isBulk)
            bulk.Add(recipe);
        return recipe;
    }
}
