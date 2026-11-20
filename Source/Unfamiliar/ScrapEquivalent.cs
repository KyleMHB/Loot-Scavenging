using System.Collections.Generic;
using LootScavenging.Loot;
using LootScavenging.Model;
using RimWorld;
using Verse;

namespace LootScavenging;

/// <summary>
/// Original-item mode at the bench (spec, "Original-item mode"): an unfamiliar item counts as the scrap it would have become,
/// with the same tier, units, yields and study points. This is the one place that mapping lives; recycling and study both ask
/// it, so changing how scrap is worked out changes the original item with it.
/// </summary>
internal static class ScrapEquivalent
{
    /// <summary>
    /// The scrap tier and units of an unfamiliar item. False for anything else, so a familiar item is never taken: the item
    /// is used only when it is unfamiliar and the player's bill filter allows it.
    /// </summary>
    public static bool TryGet(Thing thing, out ScrapTier tier, out int units)
    {
        tier = default;
        units = 0;
        if (thing == null || !Unfamiliar.IsUnfamiliar(thing))
            return false;

        // The era the scrap would have had: the looted marker when the item carries one, else the item's own era.
        Era era = Era.Undefined;
        if (thing is ThingWithComps withComps)
            era = withComps.GetComp<CompUnfamiliar>()?.lootedEra ?? Era.Undefined;
        if (era == Era.Undefined)
            era = EraReader.ItemEra(thing.def, null).Era;

        tier = ScrapTiers.TierFor(era, EraReader.ColonyEra());
        units = ScrapAmounts.Units(thing.MarketValue, thing.stackCount);
        return true;
    }

    /// <summary>
    /// What recycling this item returns: what the same units of its tier's scrap would return. Ticket 15 swaps the body for
    /// its weighted-pool roll (<c>ScrapTierTable.RollProducts(tier, units, valueFactor)</c>); callers do not change.
    /// </summary>
    public static IEnumerable<Thing> RecycleProducts(Thing thing)
    {
        if (!TryGet(thing, out ScrapTier tier, out int units))
            return new List<Thing>();

        var products = new List<Thing>();
        products.AddRange(ScrapTierTable.MakeThings(YieldMath.Fixed(ScrapTierTable.Yields(tier), units)));
        products.AddRange(ScrapTierTable.ChanceProducts(ScrapDefs.For(tier), units));
        return products;
    }

    /// <summary>Recycling work for one item: its units at the tier's work per unit, as the scrap would take.</summary>
    public static float RecycleWork(ScrapTier tier, int units)
    {
        ScrapTierDef def = ScrapTierTable.For(tier);
        return units * (def?.workPerUnit ?? 100f);
    }

    /// <summary>Study work for one item: its units at the tier's points per unit, as the scrap would take.</summary>
    public static float StudyWork(ScrapTier tier, int units)
    {
        float points = ScrapDefs.For(tier).GetModExtension<StudyPoints>()?.pointsPerUnit ?? 0f;
        return StudyRules.WorkAmount(units, points);
    }

    // Tier recycled at a bench: the same bench rule as scrap (a bench named for a tier recycles it and every tier below).
    private static Dictionary<ScrapTier, HashSet<string>> benchesByTier;

    private static Dictionary<ScrapTier, HashSet<string>> BenchesByTier()
    {
        if (benchesByTier != null)
            return benchesByTier;
        var result = new Dictionary<ScrapTier, HashSet<string>>();
        var table = new List<(ScrapTier tier, string[] benches)>();
        foreach (ScrapTier t in (ScrapTier[])System.Enum.GetValues(typeof(ScrapTier)))
        {
            ScrapTierDef d = ScrapTierTable.For(t);
            if (d != null)
                table.Add((t, d.benches.FindAll(b => b != null).ConvertAll(b => b.defName).ToArray()));
        }
        foreach (ScrapTier t in (ScrapTier[])System.Enum.GetValues(typeof(ScrapTier)))
            result[t] = new HashSet<string>(RecycleBenches.UsersFor(t, table));
        benchesByTier = result;
        return result;
    }

    public static bool BenchRecycles(ThingDef bench, ScrapTier tier)
    {
        return bench != null && BenchesByTier().TryGetValue(tier, out HashSet<string> set) && set.Contains(bench.defName);
    }

    /// <summary>Every bench that recycles at least the lowest tier, as defs, for the original-item recycling recipe.</summary>
    public static List<ThingDef> AllRecycleBenches()
    {
        var users = new List<ThingDef>();
        foreach (string name in BenchesByTier()[ScrapTier.Neolithic])
        {
            ThingDef bench = DefDatabase<ThingDef>.GetNamedSilentFail(name);
            if (bench != null)
                users.Add(bench);
        }
        return users;
    }
}
