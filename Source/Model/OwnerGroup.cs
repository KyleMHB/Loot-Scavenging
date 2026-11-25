namespace LootScavenging.Model;

/// <summary>
/// Who owned the gear (spec, "Loot rules", step 1). Colony pawns are never affected; each other group has a Who row. The two
/// joiner groups are never returned by <see cref="OwnerGroups.Classify"/>: the join hook names them, since a joiner is still
/// classified as the group he is leaving.
/// </summary>
public enum OwnerGroup
{
    Colony,
    Hostile,
    NonHostileFaction,
    NoFaction,
    Prisoner,

    /// <summary>A colony prisoner or slave being recruited or enslaved, as his gear is sorted at the join (ticket 20).</summary>
    RecruitedPrisoner,

    /// <summary>Anyone else joining the colony: wanderers, refugees, quest joiners.</summary>
    OtherJoiner,
}

/// <summary>What a Who row does with its group's gear (ticket 25, decision 3). Saved by name.</summary>
public enum WhoRule
{
    /// <summary>The item rules sort the gear.</summary>
    Apply,

    /// <summary>The gear is left alone.</summary>
    Exclude,

    /// <summary>The gear stays whole and out-of-era gear is marked unfamiliar. Replaces the item rules for the group.</summary>
    Mark,
}

public static class OwnerGroups
{
    /// <summary>
    /// Sorts a pawn into its owner group from plain facts. The order matters: a colony prisoner keeps its old faction
    /// (or none), so the prisoner test comes before the faction tests.
    /// </summary>
    public static OwnerGroup Classify(bool isPlayerFaction, bool isPrisonerOfColony, bool hasFaction, bool factionHostileToPlayer)
    {
        if (isPlayerFaction)
            return OwnerGroup.Colony;
        if (isPrisonerOfColony)
            return OwnerGroup.Prisoner;
        if (!hasFaction)
            return OwnerGroup.NoFaction;
        return factionHostileToPlayer ? OwnerGroup.Hostile : OwnerGroup.NonHostileFaction;
    }
}
