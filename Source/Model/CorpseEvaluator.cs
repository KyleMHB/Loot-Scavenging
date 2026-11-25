namespace LootScavenging.Model;

/// <summary>What kind of pawn the corpse came from. Other covers entities and anything else the rule leaves alone.</summary>
public enum CorpseKind
{
    Humanlike,
    Animal,
    Mechanoid,
    Other,
}

/// <summary>The corpse rule's values (spec, "Corpses and butchering"). Public fields so the settings can Scribe them.</summary>
public sealed class CorpseRules
{
    public bool humanlike;
    public bool animal;
    public bool mechanoid;

    // Cut in 0.6.0 (ticket 25): a ticked kind is always removed, and butchering always strips and sorts. Non-public, so they
    // are neither saved nor compared; the Def loader still fills them from an old patch, and SettingsLoad drops them.
#pragma warning disable CS0649 // filled by the game's Def loader by reflection
    internal string chance;
    internal string butcherRemovesApparel;
#pragma warning restore CS0649

    public bool IsKindOn(CorpseKind kind)
    {
        switch (kind)
        {
            case CorpseKind.Humanlike: return humanlike;
            case CorpseKind.Animal: return animal;
            case CorpseKind.Mechanoid: return mechanoid;
            default: return false;
        }
    }
}

public sealed class CorpseCandidate
{
    public OwnerGroup Owner;
    public MapKind Map;
    public CorpseKind Kind;
}

public enum CorpseDecision
{
    Remove,
    ColonyPawn,
    OwnerGroupOff,
    NoMap,
    NotHomeMap,
    KindOff,
}

/// <summary>
/// The corpse rule. Owner and place checks mirror the loot rules' steps 1 and 2 and read the same Who rows: a row set to
/// Exclude leaves its corpses alone, Apply and Mark both let them go. No side effects.
/// </summary>
public static class CorpseEvaluator
{
    public static CorpseDecision Evaluate(CorpseCandidate corpse, LootRules loot, CorpseRules rules)
    {
        CorpseDecision? blocked = CheckOwnerAndPlace(corpse.Owner, corpse.Map, loot);
        if (blocked != null)
            return blocked.Value;
        return rules.IsKindOn(corpse.Kind) ? CorpseDecision.Remove : CorpseDecision.KindOff;
    }

    /// <summary>
    /// Whether butchering this owner's corpse strips it first and sorts what comes off under the item rules (ticket 25,
    /// decision 6). A colonist, or a row set to Exclude, gets the game's own butchering.
    /// </summary>
    public static bool ButcherSorts(OwnerGroup owner, MapKind map, LootRules loot)
    {
        return CheckOwnerAndPlace(owner, map, loot) == null;
    }

    private static CorpseDecision? CheckOwnerAndPlace(OwnerGroup owner, MapKind map, LootRules loot)
    {
        if (owner == OwnerGroup.Colony)
            return CorpseDecision.ColonyPawn;
        if (loot.RowFor(owner) == WhoRule.Exclude)
            return CorpseDecision.OwnerGroupOff;
        if (map == MapKind.None)
            return CorpseDecision.NoMap;
        if (loot.homeMapOnly && map != MapKind.Home)
            return CorpseDecision.NotHomeMap;
        return null;
    }
}
