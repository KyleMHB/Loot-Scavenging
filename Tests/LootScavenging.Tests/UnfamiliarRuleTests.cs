using LootScavenging.Model;
using Xunit;

namespace LootScavenging.Tests;

public class UnfamiliarRuleTests
{
    private const DropMode Keep = DropMode.KeepOriginal;

    [Theory]
    [InlineData(DropMode.ScrapOutOfReach)]
    [InlineData(DropMode.ScrapEverything)]
    [InlineData(DropMode.Off)]
    public void OtherDropModesNeverMakeAnythingUnfamiliar(DropMode mode)
    {
        Assert.False(UnfamiliarRule.IsUnfamiliar(mode, UnfamiliarScope.AllOutOfReach, true, Era.Undefined, Era.Neolithic));
        Assert.False(UnfamiliarRule.IsUnfamiliar(mode, UnfamiliarScope.LootedOnly, true, Era.Spacer, Era.Neolithic));
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
        Assert.True(UnfamiliarRule.ShouldMark(Keep, UnfamiliarScope.LootedOnly, OwnerGroup.Hostile, true));
        Assert.False(UnfamiliarRule.ShouldMark(Keep, UnfamiliarScope.LootedOnly, OwnerGroup.Colony, true));
        Assert.False(UnfamiliarRule.ShouldMark(Keep, UnfamiliarScope.LootedOnly, OwnerGroup.Hostile, false));
        Assert.False(UnfamiliarRule.ShouldMark(Keep, UnfamiliarScope.AllOutOfReach, OwnerGroup.Hostile, true));
        Assert.False(UnfamiliarRule.ShouldMark(DropMode.Off, UnfamiliarScope.LootedOnly, OwnerGroup.Hostile, true));
    }

    [Fact]
    public void MarkerEraIsNeverUndefined()
    {
        Assert.Equal(Era.Animal, UnfamiliarRule.MarkerEra(new EraResult(Era.Undefined, EraSource.Research, true)));
        Assert.Equal(Era.Spacer, UnfamiliarRule.MarkerEra(new EraResult(Era.Spacer, EraSource.Research, true)));
    }
}
