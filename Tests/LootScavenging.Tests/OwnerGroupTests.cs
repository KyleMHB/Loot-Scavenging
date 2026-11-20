using LootScavenging.Model;
using Xunit;

namespace LootScavenging.Tests;

public class OwnerGroupTests
{
    [Fact]
    public void PlayerFactionPawnsAreColony()
    {
        Assert.Equal(OwnerGroup.Colony, OwnerGroups.Classify(isPlayerFaction: true, isPrisonerOfColony: false, hasFaction: true, factionHostileToPlayer: false));
    }

    [Fact]
    public void PrisonersOfTheColonyArePrisonersEvenWithoutAFaction()
    {
        Assert.Equal(OwnerGroup.Prisoner, OwnerGroups.Classify(isPlayerFaction: false, isPrisonerOfColony: true, hasFaction: false, factionHostileToPlayer: false));
        Assert.Equal(OwnerGroup.Prisoner, OwnerGroups.Classify(isPlayerFaction: false, isPrisonerOfColony: true, hasFaction: true, factionHostileToPlayer: true));
    }

    [Fact]
    public void FactionlessPawnsAreNoFaction()
    {
        Assert.Equal(OwnerGroup.NoFaction, OwnerGroups.Classify(isPlayerFaction: false, isPrisonerOfColony: false, hasFaction: false, factionHostileToPlayer: false));
    }

    [Fact]
    public void FactionPawnsSplitByHostility()
    {
        Assert.Equal(OwnerGroup.Hostile, OwnerGroups.Classify(isPlayerFaction: false, isPrisonerOfColony: false, hasFaction: true, factionHostileToPlayer: true));
        Assert.Equal(OwnerGroup.NonHostileFaction, OwnerGroups.Classify(isPlayerFaction: false, isPrisonerOfColony: false, hasFaction: true, factionHostileToPlayer: false));
    }
}
