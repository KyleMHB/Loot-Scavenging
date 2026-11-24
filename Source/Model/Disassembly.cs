using System;
using System.Collections.Generic;

namespace LootScavenging.Model;

/// <summary>Where an instantly disassembled item's materials come from (spec, outcome table).</summary>
public enum DisassemblyYield
{
    /// <summary>The tier table's yield for the item's era, scaled by its scrap amount.</summary>
    EraTypical,

    /// <summary>The item's own cost list and material at the efficiency setting.</summary>
    ItemBased,
}

/// <summary>Degrade, disassembly and charge-ejection maths (ticket 08). Plain C#, so it is unit tested.</summary>
public static class Disassembly
{
    public const float DefaultDegradeMin = 0.1f;
    public const float DefaultDegradeMax = 0.4f;
    public const float DefaultEfficiency = 0.5f;

    /// <summary>
    /// Hit points a degraded item ends with: a random level inside the range, at least 1 so degrading never destroys it.
    /// Degrading never repairs: an item already at or below the picked level keeps its hit points.
    /// </summary>
    public static int DegradedHitPoints(int maxHitPoints, int currentHitPoints, float min, float max, double random01)
    {
        if (min > max)
        {
            float t = min;
            min = max;
            max = t;
        }
        min = Math.Min(1f, Math.Max(0f, min));
        max = Math.Min(1f, Math.Max(0f, max));
        double fraction = min + (max - min) * Math.Min(1.0, Math.Max(0.0, random01));
        int target = Math.Max(1, (int)Math.Round(maxHitPoints * fraction, MidpointRounding.AwayFromZero));
        target = Math.Min(target, maxHitPoints);
        return Math.Min(target, Math.Max(1, currentHitPoints));
    }

    /// <summary>One quality step down, stopping at the lowest.</summary>
    public static ItemQuality StepDown(ItemQuality quality) => quality > ItemQuality.Awful ? quality - 1 : quality;

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
    /// Components for gear with no era when the era fallback is "break it into components": a quarter of the scrap units it
    /// would have made, at least one. A half before 0.5.2, when a unit was worth twice as much (ticket 19), so an item gives
    /// the same components as before.
    /// </summary>
    public static int ComponentsFor(int scrapUnits) => Math.Max(1, scrapUnits / 4);

    /// <summary>
    /// Whether an outcome ejects the loaded charges first: with the setting on, every outcome that destroys or replaces the
    /// item. Off, the charges go with the item.
    /// </summary>
    public static bool EjectsCharges(Outcome outcome, bool ejectSetting) =>
        ejectSetting && (outcome == Outcome.Scrap || outcome == Outcome.Disassemble || outcome == Outcome.Delete);

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
