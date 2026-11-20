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

    /// <summary>Chance a corpse of an enabled kind is removed, 0 to 1.</summary>
    public float chance = 1f;

    /// <summary>Butchering a corpse destroys its worn apparel instead of dropping it.</summary>
    public bool butcherRemovesApparel;

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
    ChanceFailed,
}

/// <summary>
/// The corpse rule. Owner and place checks mirror the loot rules' steps 1 and 2 and read the same settings; the
/// moment check does not apply because a corpse only exists after death. No side effects.
/// </summary>
public static class CorpseEvaluator
{
    /// <param name="roll">A uniform value in [0, 1), supplied by the caller so tests can fix it.</param>
    public static CorpseDecision Evaluate(CorpseCandidate corpse, LootRules loot, CorpseRules rules, double roll)
    {
        CorpseDecision? blocked = CheckOwnerAndPlace(corpse.Owner, corpse.Map, loot);
        if (blocked != null)
            return blocked.Value;
        if (!rules.IsKindOn(corpse.Kind))
            return CorpseDecision.KindOff;
        return roll < rules.chance ? CorpseDecision.Remove : CorpseDecision.ChanceFailed;
    }

    /// <summary>Whether butchering this owner's corpse destroys its worn apparel.</summary>
    public static bool RemovesApparelOnButchering(OwnerGroup owner, MapKind map, LootRules loot, CorpseRules rules)
    {
        return rules.butcherRemovesApparel && CheckOwnerAndPlace(owner, map, loot) == null;
    }

    private static CorpseDecision? CheckOwnerAndPlace(OwnerGroup owner, MapKind map, LootRules loot)
    {
        if (owner == OwnerGroup.Colony)
            return CorpseDecision.ColonyPawn;
        if (!loot.IsGroupOn(owner))
            return CorpseDecision.OwnerGroupOff;
        if (map == MapKind.None)
            return CorpseDecision.NoMap;
        if (loot.homeMapOnly && map != MapKind.Home)
            return CorpseDecision.NotHomeMap;
        return null;
    }
}
