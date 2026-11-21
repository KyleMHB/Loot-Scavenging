using System;
using System.Collections.Generic;
using LootScavenging.Model;
using Verse;

namespace LootScavenging;

/// <summary>One material in a tier's loot pool. Its weight is its share of the tier's value, not of the item count.</summary>
public class ScrapPoolEntry
{
    public ThingDef thing;
    public float weight = 1f;
}

/// <summary>
/// The tier table (spec, "Recycling"): one def per scrap tier naming its scrap, the benches that recycle it, the silver
/// value of each unit and the loot pool each unit picks a material from. Lives in Defs/ScrapTiers.xml, so a pack patches
/// benches, value and pool without code.
/// </summary>
public class ScrapTierDef : Def
{
    public ScrapTier tier;
    public ThingDef scrap;

    /// <summary>Benches named for this tier. Each also recycles every tier below it.</summary>
    public List<ThingDef> benches = new List<ThingDef>();

    /// <summary>Silver value one unit of scrap returns, spent on the one material the unit picks.</summary>
    public float valuePerUnit = 10f;

    public List<ScrapPoolEntry> pool = new List<ScrapPoolEntry>();

    /// <summary>
    /// What study leaves in "study first, then recycle" mode (ticket 05). It recycles at the same benches as the scrap,
    /// from the same pool, at <see cref="studiedYieldFactor"/> of the value per unit, and is never studied again.
    /// </summary>
    public ThingDef studiedScrap;

    public float studiedYieldFactor = 0.5f;

    /// <summary>
    /// The most scrap units one bill iteration uses. The bill takes a whole stack of any size, so fewer is fine and a lone
    /// unit is recycled too; the rest of a larger stack stays at the bench for the next iteration.
    /// </summary>
    public int batchSize = 5;

    /// <summary>Work for one unit; an iteration takes this times the units it uses.</summary>
    public float workPerUnit = 100f;

    public override IEnumerable<string> ConfigErrors()
    {
        foreach (string e in base.ConfigErrors())
            yield return e;
        if (scrap == null)
            yield return "scrap is not set"; // not translated
        if (batchSize < 1)
            yield return "batchSize must be at least 1"; // not translated
        if (!(valuePerUnit > 0f))
            yield return "valuePerUnit must be above 0"; // not translated
        if (pool.Count == 0)
            yield return "the pool is empty"; // not translated
        foreach (ScrapPoolEntry p in pool)
        {
            if (p.thing == null)
                yield return "a pool entry names no thing"; // not translated
            else if (!(p.weight > 0f))
                yield return "pool entry " + p.thing.defName + " has a weight of 0 or less"; // not translated
        }
    }
}

/// <summary>Reads the tier table and rolls its pools. Ticket 07 calls <see cref="RollProducts"/> too.</summary>
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

    /// <summary>The share of the value per unit that <paramref name="scrap"/> pays: 1 for scrap, the studied factor (at most 1) for studied scrap.</summary>
    public static float ValueFactor(ScrapTierDef def, ThingDef scrap)
    {
        if (def == null || scrap == def.scrap)
            return 1f;
        return Math.Min(1f, def.studiedYieldFactor);
    }

    /// <summary>The tier's pool as plain model entries, skipping entries with no item.</summary>
    public static List<PoolEntry<ThingDef>> Pool(ScrapTierDef def)
    {
        var list = new List<PoolEntry<ThingDef>>();
        foreach (ScrapPoolEntry p in def.pool)
        {
            if (p.thing != null)
                list.Add(new PoolEntry<ThingDef>(p.thing, p.weight));
        }
        return list;
    }

    /// <summary>
    /// The one way to turn scrap units into materials: one pool pick per unit, worth the tier's value per unit times
    /// <paramref name="valueFactor"/> (1 for scrap, the studied factor for studied scrap, 1 for an unfamiliar original item
    /// counted as its scrap equivalent). Returns new things, split at each item's stack limit, not yet placed anywhere.
    /// </summary>
    public static IEnumerable<Thing> RollProducts(ScrapTierDef def, int units, float valueFactor = 1f)
    {
        if (def == null || units <= 0)
            return new List<Thing>();
        List<YieldAmount<ThingDef>> amounts = LootPool.Roll(Pool(def), def.valuePerUnit, valueFactor, units,
            t => t.BaseMarketValue, () => Rand.Value);
        return MakeThings(amounts);
    }

    /// <summary>Makes stacks (split at each item's stack limit) for amounts from <see cref="LootPool"/>.</summary>
    public static List<Thing> MakeThings(IEnumerable<YieldAmount<ThingDef>> amounts)
    {
        var things = new List<Thing>();
        foreach (YieldAmount<ThingDef> a in amounts)
        {
            foreach (int stack in ScrapAmounts.SplitIntoStacks(a.Count, a.Item.stackLimit))
            {
                Thing thing = ThingMaker.MakeThing(a.Item);
                thing.stackCount = stack;
                things.Add(thing);
            }
        }
        return things;
    }
}
