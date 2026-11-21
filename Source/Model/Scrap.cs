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
    /// Divides the square root of an item's market value. The root keeps a charge rifle (about 1650) to a handful of units
    /// instead of hundreds while a stone knife (about 12) stays at one. Tuning knob.
    /// </summary>
    public const float ValueScale = 8f;

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
