using System;
using System.Collections.Generic;

namespace LootScavenging.Model;

/// <summary>One material in a tier's loot pool. Its <see cref="Weight"/> is its share of the tier's value.</summary>
public readonly struct PoolEntry<T>
{
    public PoolEntry(T item, float weight)
    {
        Item = item;
        Weight = weight;
    }

    public T Item { get; }
    public float Weight { get; }
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

/// <summary>
/// Loot pool maths shared by recycling, studied scrap and instant disassembly. Each unit of scrap picks one material by
/// weight and pays the tier's value per unit in that material, rounded up or down at random so the average is exact.
/// </summary>
public static class LootPool
{
    /// <summary>
    /// Weighted pick. <paramref name="roll"/> is in [0, 1). Entries with no weight or a market value of zero or less are
    /// never picked; returns false when nothing can be picked.
    /// </summary>
    public static bool TryPick<T>(IReadOnlyList<PoolEntry<T>> pool, Func<T, float> marketValue, double roll, out T item)
    {
        double total = 0.0;
        foreach (PoolEntry<T> e in pool)
        {
            if (Usable(e, marketValue))
                total += e.Weight;
        }
        item = default;
        if (total <= 0.0)
            return false;

        double target = roll * total;
        double running = 0.0;
        bool found = false;
        foreach (PoolEntry<T> e in pool)
        {
            if (!Usable(e, marketValue))
                continue;
            item = e.Item;
            found = true;
            running += e.Weight;
            if (target < running)
                break;
        }
        return found;
    }

    /// <summary>
    /// Items for <paramref name="value"/> silver of one material: value over market value, the fraction rounded up with
    /// that probability. <paramref name="roll"/> is in [0, 1).
    /// </summary>
    public static int Amount(double value, float marketValue, double roll)
    {
        if (!(value > 0.0) || !(marketValue > 0f))
            return 0;
        double exact = value / marketValue;
        int whole = (int)Math.Floor(exact);
        return roll < exact - whole ? whole + 1 : whole;
    }

    /// <summary>
    /// What <paramref name="units"/> of scrap return: one weighted pick per unit, each worth <paramref name="valuePerUnit"/>
    /// times <paramref name="valueFactor"/> (studied scrap passes its factor; NaN or below zero counts as zero). Amounts
    /// of the same material are added together. <paramref name="random"/> returns values in [0, 1).
    /// </summary>
    public static List<YieldAmount<T>> Roll<T>(IReadOnlyList<PoolEntry<T>> pool, float valuePerUnit, float valueFactor, int units,
        Func<T, float> marketValue, Func<double> random)
    {
        var counts = new Dictionary<T, int>();
        var order = new List<T>();
        double value = (double)valuePerUnit * (float.IsNaN(valueFactor) ? 0f : Math.Max(0f, valueFactor));
        for (int i = 0; i < units && value > 0.0; i++)
        {
            if (!TryPick(pool, marketValue, random(), out T item))
                break;
            int amount = Amount(value, marketValue(item), random());
            if (amount <= 0)
                continue;
            if (!counts.TryGetValue(item, out int have))
                order.Add(item);
            counts[item] = have + amount;
        }
        var result = new List<YieldAmount<T>>(order.Count);
        foreach (T item in order)
            result.Add(new YieldAmount<T>(item, counts[item]));
        return result;
    }

    /// <summary>Average items per material for <paramref name="units"/> of scrap.</summary>
    public static List<ExpectedAmount<T>> Expected<T>(IReadOnlyList<PoolEntry<T>> pool, float valuePerUnit, int units,
        Func<T, float> marketValue)
    {
        var result = new List<ExpectedAmount<T>>();
        double total = 0.0;
        foreach (PoolEntry<T> e in pool)
        {
            if (Usable(e, marketValue))
                total += e.Weight;
        }
        if (total <= 0.0 || units <= 0 || !(valuePerUnit > 0f))
            return result;
        foreach (PoolEntry<T> e in pool)
        {
            if (!Usable(e, marketValue))
                continue;
            double picks = units * (e.Weight / total);
            result.Add(new ExpectedAmount<T>(e.Item, picks * valuePerUnit / marketValue(e.Item)));
        }
        return result;
    }

    private static bool Usable<T>(PoolEntry<T> e, Func<T, float> marketValue) => e.Weight > 0f && marketValue(e.Item) > 0f;
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
