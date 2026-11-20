using System.Collections.Generic;
using System.Linq;
using LootScavenging.Model;
using Verse;

namespace LootScavenging;

/// <summary>One line of a tier's yield, per unit of scrap: <see cref="count"/> of <see cref="thing"/> with probability <see cref="chance"/>.</summary>
public class ScrapYield
{
    public ThingDef thing;
    public int count = 1;
    public float chance = 1f;
}

/// <summary>
/// The tier table (spec, "Recycling"): one def per scrap tier naming its scrap, the benches that recycle it and the fixed
/// yield of each unit. Lives in Defs/ScrapTiers.xml, so a pack patches benches and yields without code.
/// </summary>
public class ScrapTierDef : Def
{
    public ScrapTier tier;
    public ThingDef scrap;

    /// <summary>Benches named for this tier. Each also recycles every tier below it.</summary>
    public List<ThingDef> benches = new List<ThingDef>();

    public List<ScrapYield> yields = new List<ScrapYield>();

    /// <summary>
    /// What study leaves in "study first, then recycle" mode (ticket 05). It recycles at the same benches as the scrap,
    /// at <see cref="studiedYieldFactor"/> of its yield, and is never studied again.
    /// </summary>
    public ThingDef studiedScrap;

    public float studiedYieldFactor = 0.5f;

    /// <summary>Scrap units one bill iteration uses up. Leftovers below this stay unrecycled.</summary>
    public int batchSize = 5;

    /// <summary>Work for one unit; a bill iteration takes batchSize times this.</summary>
    public float workPerUnit = 100f;

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string e in base.ConfigErrors())
            yield return e;
        if (scrap == null)
            yield return "scrap is not set"; // not translated
        if (batchSize < 1)
            yield return "batchSize must be at least 1"; // not translated
        foreach (ScrapYield y in yields)
        {
            if (y.thing == null)
                yield return "a yield names no thing"; // not translated
            else if (y.count < 1)
                yield return "yield " + y.thing.defName + " has a count below 1"; // not translated
        }
    }
}

/// <summary>Reads the tier table. Ticket 08's instant disassembly takes its era-typical yields from here.</summary>
internal static class ScrapTierTable
{
    public static ScrapTierDef For(ScrapTier tier)
    {
        ScrapTierDef found = null;
        foreach (ScrapTierDef def in DefDatabase<ScrapTierDef>.AllDefsListForReading)
        {
            if (def.tier != tier)
                continue;
            if (found == null)
                found = def;
            else
                Log.Warning($"[Loot Scavenging] {def.defName} repeats the {tier} tier already set by {found.defName}; ignoring it."); // not translated
        }
        return found;
    }

    /// <summary>The tier of a scrap or studied-scrap def.</summary>
    public static ScrapTierDef ForScrap(ThingDef scrap)
    {
        foreach (ScrapTierDef def in DefDatabase<ScrapTierDef>.AllDefsListForReading)
        {
            if (def.scrap == scrap || (def.studiedScrap != null && def.studiedScrap == scrap))
                return def;
        }
        return null;
    }

    /// <summary>A tier's yield lines for studied scrap: the scrap's yield reduced by the studied-yield factor.</summary>
    public static List<YieldEntry<ThingDef>> StudiedYields(ScrapTier tier)
    {
        ScrapTierDef def = For(tier);
        return def == null ? new List<YieldEntry<ThingDef>>() : StudiedYield.Reduce(Yields(tier), def.studiedYieldFactor);
    }

    /// <summary>The tier's yield lines as plain model entries, skipping lines with no item.</summary>
    public static List<YieldEntry<ThingDef>> Yields(ScrapTier tier)
    {
        ScrapTierDef def = For(tier);
        var list = new List<YieldEntry<ThingDef>>();
        if (def == null)
            return list;
        foreach (ScrapYield y in def.yields)
        {
            if (y.thing != null)
                list.Add(new YieldEntry<ThingDef>(y.thing, y.count, y.chance));
        }
        return list;
    }

    /// <summary>Makes stacks (split at each item's stack limit) for amounts from <see cref="YieldMath"/>.</summary>
    public static IEnumerable<Thing> MakeThings(IEnumerable<YieldAmount<ThingDef>> amounts)
    {
        foreach (YieldAmount<ThingDef> a in amounts)
        {
            foreach (int stack in ScrapAmounts.SplitIntoStacks(a.Count, a.Item.stackLimit))
            {
                Thing thing = ThingMaker.MakeThing(a.Item);
                thing.stackCount = stack;
                yield return thing;
            }
        }
    }

    public static IEnumerable<Thing> ChanceProducts(ThingDef scrap, int units)
    {
        ScrapTierDef def = ForScrap(scrap);
        if (def == null)
            return Enumerable.Empty<Thing>();
        List<YieldEntry<ThingDef>> yields = scrap == def.scrap ? Yields(def.tier) : StudiedYields(def.tier);
        return MakeThings(YieldMath.RollChance(yields, units, () => Rand.Value));
    }
}
