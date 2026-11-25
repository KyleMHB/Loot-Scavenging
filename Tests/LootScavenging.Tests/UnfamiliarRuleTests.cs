using LootScavenging.Model;
using Xunit;

namespace LootScavenging.Tests;

public class UnfamiliarRuleTests
{
    private const bool Keep = true;

    [Fact]
    public void NothingIsUnfamiliarWhileNoLootRowMarks()
    {
        Assert.False(UnfamiliarRule.IsUnfamiliar(false, UnfamiliarScope.AllOutOfReach, true, Era.Undefined, Era.Neolithic));
        Assert.False(UnfamiliarRule.IsUnfamiliar(false, UnfamiliarScope.LootedOnly, true, Era.Spacer, Era.Neolithic));
    }

    [Fact]
    public void AllScopeFollowsReachAndIgnoresTheMarker()
    {
        Assert.True(UnfamiliarRule.IsUnfamiliar(Keep, UnfamiliarScope.AllOutOfReach, true, Era.Undefined, Era.Neolithic));
        Assert.False(UnfamiliarRule.IsUnfamiliar(Keep, UnfamiliarScope.AllOutOfReach, false, Era.Spacer, Era.Neolithic));
    }

    [Fact]
    public void LootedOnlyNeedsTheMarker()
    {
        Assert.False(UnfamiliarRule.IsUnfamiliar(Keep, UnfamiliarScope.LootedOnly, true, Era.Undefined, Era.Neolithic));
        Assert.True(UnfamiliarRule.IsUnfamiliar(Keep, UnfamiliarScope.LootedOnly, true, Era.Spacer, Era.Neolithic));
    }

    [Fact]
    public void MarkedGearClearsWhenResearchFinishesAndColonyEraCatchesUp()
    {
        Assert.True(UnfamiliarRule.IsUnfamiliar(Keep, UnfamiliarScope.LootedOnly, false, Era.Spacer, Era.Industrial));
        Assert.False(UnfamiliarRule.IsUnfamiliar(Keep, UnfamiliarScope.LootedOnly, false, Era.Spacer, Era.Spacer));
    }

    [Fact]
    public void OnlyLootedOutOfReachGearFromOutsideTheColonyIsMarked()
    {
        Assert.True(UnfamiliarRule.ShouldMark(WhoRule.Mark, UnfamiliarScope.LootedOnly, OwnerGroup.Hostile, true));
        Assert.False(UnfamiliarRule.ShouldMark(WhoRule.Mark, UnfamiliarScope.LootedOnly, OwnerGroup.Colony, true));
        Assert.False(UnfamiliarRule.ShouldMark(WhoRule.Mark, UnfamiliarScope.LootedOnly, OwnerGroup.Hostile, false));
        Assert.False(UnfamiliarRule.ShouldMark(WhoRule.Mark, UnfamiliarScope.AllOutOfReach, OwnerGroup.Hostile, true));
        Assert.False(UnfamiliarRule.ShouldMark(WhoRule.Apply, UnfamiliarScope.LootedOnly, OwnerGroup.Hostile, true));
    }

    [Fact]
    public void MarkerEraIsNeverUndefined()
    {
        Assert.Equal(Era.Animal, UnfamiliarRule.MarkerEra(new EraResult(Era.Undefined, EraSource.Research, true)));
        Assert.Equal(Era.Spacer, UnfamiliarRule.MarkerEra(new EraResult(Era.Spacer, EraSource.Research, true)));
    }
}
