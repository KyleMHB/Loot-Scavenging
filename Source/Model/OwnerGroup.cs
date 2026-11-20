namespace LootScavenging.Model;

/// <summary>Who owned the gear (spec, "Loot rules", step 1). Colony pawns are never affected; the other four have toggles.</summary>
public enum OwnerGroup
{
    Colony,
    Hostile,
    NonHostileFaction,
    NoFaction,
    Prisoner,
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
