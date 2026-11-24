using System;
using System.Collections.Generic;

namespace LootScavenging.Model;

/// <summary>The scrap items: one per era tier (spec, "Recycling" table). Animal gear becomes neolithic scrap.
/// New tiers go at the end: <c>ScrapDefs</c> indexes its defs by this order.</summary>
public enum ScrapTier
{
    Neolithic,
    Medieval,
    Industrial,
    Spacer,
    Ultra,
    Archotech,
}

public static class ScrapTiers
{
    /// <summary>The scrap ThingDef's name for a tier (Defs/ThingDefs/Scrap.xml): the one place it is spelled.</summary>
    public static string DefName(ScrapTier tier) => "LS_Scrap_" + tier; // not translated

    /// <summary>
    /// The scrap tier for an item era. Animal rounds up to Neolithic. Gear with no era (only reachable in "scrap
    /// everything", where within-reach gear is scrapped too) becomes scrap of the colony's era.
    /// </summary>
    public static ScrapTier TierFor(Era itemEra, Era colonyEra)
    {
        Era era = itemEra == Era.Undefined ? colonyEra : itemEra;
        if (era >= Era.Archotech)
            return ScrapTier.Archotech;
        if (era == Era.Ultra)
            return ScrapTier.Ultra;
        if (era == Era.Spacer)
            return ScrapTier.Spacer;
        if (era == Era.Industrial)
            return ScrapTier.Industrial;
        if (era == Era.Medieval)
            return ScrapTier.Medieval;
        return ScrapTier.Neolithic;
    }
}

public static class ScrapAmounts
{
    /// <summary>
    /// Divides the square root of an item's market value. The root keeps a charge rifle (about 1650) to about ten units
    /// instead of hundreds while a stone knife (about 12) stays at one. Tuning knob. 8 before 0.5.2: halving it with the
    /// tiers' value per unit (ticket 19) gives about twice the units, each worth half as much.
    /// </summary>
    public const float ValueScale = 4f;

    /// <summary>
    /// Scrap units for a stack. <paramref name="unitMarketValue"/> is the game's market value of one item, which already
    /// carries its quality and hit points, so worn or poor gear yields less. At least one unit per item.
    /// </summary>
    public static int Units(float unitMarketValue, int count)
    {
        if (count < 1)
            count = 1;
        if (float.IsNaN(unitMarketValue) || unitMarketValue < 0f)
            unitMarketValue = 0f;
        int perItem = Math.Max(1, (int)Math.Round(Math.Sqrt(unitMarketValue) / ValueScale, MidpointRounding.AwayFromZero));
        return perItem * count;
    }

    /// <summary>
    /// Scrap units for a stack of stackable items (ammunition, resources, drugs): the per-item share is kept as a fraction and
    /// the stack is rounded once, with at least one unit per stack. <see cref="Units"/>'s one-per-item minimum would turn 50
    /// arrows into 50 scrap.
    /// </summary>
    public static int StackUnits(float unitMarketValue, int count)
    {
        if (count < 1)
            count = 1;
        if (float.IsNaN(unitMarketValue) || unitMarketValue < 0f)
            unitMarketValue = 0f;
        double total = Math.Sqrt(unitMarketValue) / ValueScale * count;
        return Math.Max(1, (int)Math.Round(total, MidpointRounding.AwayFromZero));
    }

    /// <summary>
    /// What one unit of a tier's scrap is worth: the larger of the silver its recycling returns (the tier's value per unit) and
    /// its market value from the settings. Either way a player can sell it for, or turn it into, this much.
    /// </summary>
    public static float WorthPerUnit(float recycleValuePerUnit, float scrapMarketValue)
    {
        float recycle = float.IsNaN(recycleValuePerUnit) || recycleValuePerUnit < 0f ? 0f : recycleValuePerUnit;
        float market = float.IsNaN(scrapMarketValue) || scrapMarketValue < 0f ? 0f : scrapMarketValue;
        return Math.Max(recycle, market);
    }

    /// <summary>
    /// Limits <paramref name="units"/> so the scrap is never worth more than the item or stack it came from: whole units of
    /// <paramref name="worthPerUnit"/> that fit in <paramref name="totalMarketValue"/>. Returns 0 when the item is worth less than
    /// one unit; the caller then makes no scrap. A worth of 0 or less sets no cap.
    /// </summary>
    public static int CapUnits(int units, float totalMarketValue, float worthPerUnit)
    {
        if (units <= 0)
            return 0;
        if (float.IsNaN(worthPerUnit) || worthPerUnit <= 0f)
            return units;
        double total = float.IsNaN(totalMarketValue) || totalMarketValue < 0f ? 0.0 : totalMarketValue;
        double fits = Math.Floor(total / worthPerUnit);
        return fits >= units ? units : (int)fits;
    }

    /// <summary><see cref="Units"/> capped to what the stack is worth. 0 means the stack makes no scrap.</summary>
    public static int CappedUnits(float unitMarketValue, int count, float worthPerUnit) =>
        CapUnits(Units(unitMarketValue, count), TotalValue(unitMarketValue, count), worthPerUnit);

    /// <summary><see cref="StackUnits"/> capped to what the stack is worth. 0 means the stack makes no scrap.</summary>
    public static int CappedStackUnits(float unitMarketValue, int count, float worthPerUnit) =>
        CapUnits(StackUnits(unitMarketValue, count), TotalValue(unitMarketValue, count), worthPerUnit);

    private static float TotalValue(float unitMarketValue, int count)
    {
        if (count < 1)
            count = 1;
        if (float.IsNaN(unitMarketValue) || unitMarketValue < 0f)
            unitMarketValue = 0f;
        return unitMarketValue * count;
    }

    /// <summary>Splits an amount into stacks no larger than the stack limit.</summary>
    public static int[] SplitIntoStacks(int units, int stackLimit)
    {
        if (units <= 0 || stackLimit < 1)
            return new int[0];
        var stacks = new List<int>();
        while (units > 0)
        {
            int n = Math.Min(units, stackLimit);
            stacks.Add(n);
            units -= n;
        }
        return stacks.ToArray();
    }
}
