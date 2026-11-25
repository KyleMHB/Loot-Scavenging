using System;
using System.Collections.Generic;

namespace LootScavenging.Model;

/// <summary>Where an instantly disassembled item's materials come from (spec, outcome table).</summary>
public enum DisassemblyYield
{
    /// <summary>The tier table's yield for the colony's era, scaled by the item's scrap amount (ticket 25, decision 7).</summary>
    EraTypical,

    /// <summary>The item's own cost list and material at the efficiency setting.</summary>
    ItemBased,
}

/// <summary>Disassembly and charge-ejection maths (ticket 08). Plain C#, so it is unit tested.</summary>
public static class Disassembly
{
    public const float DefaultEfficiency = 0.5f;

    /// <summary>
    /// The item's ingredients at <paramref name="efficiency"/>: each line's count times the efficiency, times the number of
    /// items, with the fraction rounded up or down at random so the average is exact. The game's smelt function is not used.
    /// </summary>
    public static IEnumerable<YieldAmount<T>> FromCosts<T>(IEnumerable<YieldAmount<T>> costs, int itemCount, float efficiency, Func<double> random)
    {
        efficiency = Math.Min(1f, Math.Max(0f, efficiency));
        if (itemCount < 1)
            itemCount = 1;
        foreach (YieldAmount<T> cost in costs)
        {
            if (cost.Count <= 0)
                continue;
            // Rounded so a float such as 0.2f times 40 is 8, not 8.0000001 with a stray extra unit.
            double exact = Math.Round((double)cost.Count * itemCount * efficiency, 4);
            int whole = (int)Math.Floor(exact);
            if (random() < exact - whole)
                whole++;
            if (whole > 0)
                yield return new YieldAmount<T>(cost.Item, whole);
        }
    }

    /// <summary>
    /// Whether an outcome ejects the loaded charges first: every outcome that destroys or replaces the item. Always on since
    /// 0.6.0 (ticket 25, decision 8).
    /// </summary>
    public static bool EjectsCharges(Outcome outcome) =>
        outcome == Outcome.Scrap || outcome == Outcome.Disassemble || outcome == Outcome.Delete;

    /// <summary>Ammo to eject for the charges left: whole charges, or a proportional share of a full refill.</summary>
    public static int EjectedAmmo(int remainingCharges, int maxCharges, int ammoCountPerCharge, int ammoCountToRefill)
    {
        if (remainingCharges <= 0)
            return 0;
        if (ammoCountPerCharge > 0)
            return remainingCharges * ammoCountPerCharge;
        if (ammoCountToRefill > 0 && maxCharges > 0)
            return (int)Math.Ceiling((double)ammoCountToRefill * Math.Min(remainingCharges, maxCharges) / maxCharges);
        return 0;
    }
}
