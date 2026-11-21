using LootScavenging.Model;
using Xunit;

namespace LootScavenging.Tests;

public class DropModeTests
{
    private static DropCandidate Drop(EraResult era, ItemCategory category = ItemCategory.Weapon)
    {
        return new DropCandidate { Owner = OwnerGroup.Hostile, Moment = Moment.Death, Map = MapKind.Home, Category = category, ItemEra = era };
    }

    private static readonly EraResult OutOfReach = new EraResult(Era.Industrial, EraSource.Item, true);
    private static readonly EraResult InReach = new EraResult(Era.Neolithic, EraSource.Item, false);
    private static readonly EraResult Components = new EraResult(Era.Undefined, EraSource.Components, false);

    private static LootRules Rules(DropMode mode) => TestRules.Basic(r => { r.dropMode = mode; });

    [Fact]
    public void ScrapOutOfReachScrapsGearBeyondReach()
    {
        var d = LootEvaluator.Evaluate(Drop(OutOfReach), Rules(DropMode.ScrapOutOfReach));
        Assert.Equal(Outcome.Scrap, d.Outcome);
        Assert.Equal(DecisionReason.OutOfReachScrap, d.Reason);
    }

    [Fact]
    public void ScrapOutOfReachLeavesGearWithinReachToItsCategoryRule()
    {
        var d = LootEvaluator.Evaluate(Drop(InReach), Rules(DropMode.ScrapOutOfReach));
        Assert.Equal(Outcome.Delete, d.Outcome); // default weapon rule
        Assert.Equal(DecisionReason.CategoryRule, d.Reason);
    }

    [Fact]
    public void ScrapEverythingScrapsGearWithinReachToo()
    {
        var d = LootEvaluator.Evaluate(Drop(InReach, ItemCategory.Apparel), Rules(DropMode.ScrapEverything));
        Assert.Equal(Outcome.Scrap, d.Outcome);
        Assert.Equal(DecisionReason.ScrapEverything, d.Reason);
    }

    [Fact]
    public void ScrapEverythingScrapsGearWithNoEra()
    {
        var d = LootEvaluator.Evaluate(Drop(default(EraResult)), Rules(DropMode.ScrapEverything));
        Assert.Equal(Outcome.Scrap, d.Outcome);
    }

    [Fact]
    public void OffIgnoresEraAndUsesTheCategoryRule()
    {
        var d = LootEvaluator.Evaluate(Drop(OutOfReach, ItemCategory.Apparel), Rules(DropMode.Off));
        Assert.Equal(Outcome.Keep, d.Outcome);
        Assert.Equal(DecisionReason.CategoryRule, d.Reason);
    }

    [Fact]
    public void KeepOriginalLeavesOutOfReachGearWhole()
    {
        var d = LootEvaluator.Evaluate(Drop(OutOfReach), Rules(DropMode.KeepOriginal));
        Assert.Equal(Outcome.Keep, d.Outcome);
        Assert.Equal(DecisionReason.OutOfReachKept, d.Reason);
    }

    [Fact]
    public void KeepOriginalLeavesGearWithinReachToItsCategoryRule()
    {
        var d = LootEvaluator.Evaluate(Drop(InReach), Rules(DropMode.KeepOriginal));
        Assert.Equal(DecisionReason.CategoryRule, d.Reason);
    }

    [Theory]
    [InlineData(DropMode.ScrapOutOfReach)]
    [InlineData(DropMode.ScrapEverything)]
    public void ComponentsFallbackBreaksGearIntoComponentsInTheScrapModes(DropMode mode)
    {
        var d = LootEvaluator.Evaluate(Drop(Components), Rules(mode));
        Assert.Equal(Outcome.Disassemble, d.Outcome);
        Assert.Equal(DecisionReason.ComponentsFallback, d.Reason);
    }

    [Theory]
    [InlineData(DropMode.Off)]
    [InlineData(DropMode.KeepOriginal)]
    public void ComponentsFallbackIsIgnoredWhereScrapIsNotUsed(DropMode mode)
    {
        var d = LootEvaluator.Evaluate(Drop(Components), Rules(mode));
        Assert.Equal(DecisionReason.CategoryRule, d.Reason);
    }

    [Fact]
    public void EarlierStepsStillWinOverTheDropMode()
    {
        var d = LootEvaluator.Evaluate(
            new DropCandidate { Owner = OwnerGroup.Colony, Moment = Moment.Death, Map = MapKind.Home, ItemEra = OutOfReach },
            Rules(DropMode.ScrapEverything));
        Assert.Equal(DecisionReason.ColonyPawn, d.Reason);
    }
}
