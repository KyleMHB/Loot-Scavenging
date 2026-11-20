using System;
using System.Collections.Generic;

namespace LootScavenging.Model;

/// <summary>One line of a tier's yield: per unit of scrap, <see cref="Count"/> of <see cref="Item"/> with probability <see cref="Chance"/>.</summary>
public readonly struct YieldEntry<T>
{
    public YieldEntry(T item, int count, float chance)
    {
        Item = item;
        Count = count;
        Chance = chance;
    }

    public T Item { get; }
    public int Count { get; }
    public float Chance { get; }

    /// <summary>A line that always pays out (a chance of 1 or more).</summary>
    public bool IsFixed => Chance >= 1f;
}

public readonly struct YieldAmount<T>
{
    public YieldAmount(T item, int count)
    {
        Item = item;
        Count = count;
    }

    public T Item { get; }
    public int Count { get; }
}

public readonly struct ExpectedAmount<T>
{
    public ExpectedAmount(T item, double amount)
    {
        Item = item;
        Amount = amount;
    }

    public T Item { get; }
    public double Amount { get; }
}

/// <summary>Yield maths shared by recycling and (ticket 08) instant disassembly.</summary>
public static class YieldMath
{
    /// <summary>The guaranteed part of the yield for <paramref name="units"/> of scrap.</summary>
    public static IEnumerable<YieldAmount<T>> Fixed<T>(IEnumerable<YieldEntry<T>> entries, int units)
    {
        if (units <= 0)
            yield break;
        foreach (YieldEntry<T> e in entries)
        {
            if (e.IsFixed && e.Count > 0)
                yield return new YieldAmount<T>(e.Item, e.Count * units);
        }
    }

    /// <summary>Rolls every chance line once per unit. <paramref name="random"/> returns values in [0, 1).</summary>
    public static IEnumerable<YieldAmount<T>> RollChance<T>(IEnumerable<YieldEntry<T>> entries, int units, Func<double> random)
    {
        if (units <= 0)
            yield break;
        foreach (YieldEntry<T> e in entries)
        {
            if (e.IsFixed || e.Count <= 0 || e.Chance <= 0f)
                continue;
            int hits = 0;
            for (int i = 0; i < units; i++)
            {
                if (random() < e.Chance)
                    hits++;
            }
            if (hits > 0)
                yield return new YieldAmount<T>(e.Item, hits * e.Count);
        }
    }

    /// <summary>Average yield, fixed and chance lines together.</summary>
    public static IEnumerable<ExpectedAmount<T>> Expected<T>(IEnumerable<YieldEntry<T>> entries, int units)
    {
        if (units <= 0)
            yield break;
        foreach (YieldEntry<T> e in entries)
        {
            double chance = Math.Min(1.0, Math.Max(0.0, e.Chance));
            if (e.Count > 0 && chance > 0.0)
                yield return new ExpectedAmount<T>(e.Item, e.Count * chance * units);
        }
    }
}

public static class RecycleBenches
{
    /// <summary>
    /// The benches that recycle <paramref name="scrap"/>: every bench named for that tier or a higher one. A bench named for
    /// several tiers counts at its highest. There is no check on the colony's era.
    /// </summary>
    public static IEnumerable<string> UsersFor(ScrapTier scrap, IEnumerable<(ScrapTier tier, string[] benches)> table)
    {
        var highest = new Dictionary<string, ScrapTier>();
        var order = new List<string>();
        foreach ((ScrapTier tier, string[] benches) in table)
        {
            foreach (string bench in benches)
            {
                if (!highest.TryGetValue(bench, out ScrapTier seen))
                    order.Add(bench);
                if (!highest.ContainsKey(bench) || tier > seen)
                    highest[bench] = tier;
            }
        }
        foreach (string bench in order)
        {
            if (highest[bench] >= scrap)
                yield return bench;
        }
    }
}
