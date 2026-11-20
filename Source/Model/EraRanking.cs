namespace LootScavenging.Model;

/// <summary>Mirrors RimWorld's TechLevel values so the glue can cast between them.</summary>
public enum Era : byte
{
    Undefined = 0,
    Animal = 1,
    Neolithic = 2,
    Medieval = 3,
    Industrial = 4,
    Spacer = 5,
    Ultra = 6,
    Archotech = 7,
}

/// <summary>Where an item's era came from (spec, "Loot rules").</summary>
public enum EraSource
{
    /// <summary>No research, item or carrier era was available. Treated as within reach.</summary>
    Unknown,
    Research,
    Item,
    CarrierFaction,
    /// <summary>The "treat as the colony's era" fallback setting replaced the carrier step.</summary>
    ColonyFallback,
    /// <summary>The "break into components" fallback setting replaced the carrier step.</summary>
    Components,
}

/// <summary>What replaces step 3 (the carrying faction's era) for gear with no era of its own.</summary>
public enum EraFallback
{
    CarrierFaction,
    ColonyEra,
    Components,
}

public readonly struct EraResult
{
    public EraResult(Era era, EraSource source, bool outOfReach)
    {
        Era = era;
        Source = source;
        OutOfReach = outOfReach;
    }

    public Era Era { get; }
    public EraSource Source { get; }

    /// <summary>The item's research is unfinished, or its era is above the colony's.</summary>
    public bool OutOfReach { get; }

    /// <summary>The Components fallback applied: the caller breaks the gear straight into components.</summary>
    public bool BreakIntoComponents => Source == EraSource.Components;
}

public static class EraRanking
{
    /// <summary>The colony's era is the highest era among finished research, never below Animal.</summary>
    public static Era ColonyEra(System.Collections.Generic.IEnumerable<Era> finishedProjectEras)
    {
        Era best = Era.Animal;
        foreach (Era e in finishedProjectEras)
        {
            if (e > best) best = e;
        }
        return best;
    }

    /// <param name="researchEra">Tech level of the project that unlocks the item (Undefined if none).</param>
    /// <param name="researchUnfinished">The unlocking research exists and is not finished.</param>
    /// <param name="itemEra">The item's own tech level.</param>
    /// <param name="factionEra">Tech level of the faction that carried it (Undefined if none).</param>
    public static EraResult Resolve(Era researchEra, bool researchUnfinished, Era itemEra, Era factionEra,
        EraFallback fallback, Era colonyEra)
    {
        Era era;
        EraSource source;
        if (researchEra != Era.Undefined)
        {
            era = researchEra;
            source = EraSource.Research;
        }
        else if (itemEra != Era.Undefined)
        {
            era = itemEra;
            source = EraSource.Item;
        }
        else if (fallback == EraFallback.ColonyEra)
        {
            era = colonyEra;
            source = EraSource.ColonyFallback;
        }
        else if (fallback == EraFallback.Components)
        {
            era = Era.Undefined;
            source = EraSource.Components;
        }
        else if (factionEra != Era.Undefined)
        {
            era = factionEra;
            source = EraSource.CarrierFaction;
        }
        else
        {
            era = Era.Undefined;
            source = EraSource.Unknown;
        }

        bool outOfReach = researchUnfinished || (era != Era.Undefined && era > colonyEra);
        return new EraResult(era, source, outOfReach);
    }
}
