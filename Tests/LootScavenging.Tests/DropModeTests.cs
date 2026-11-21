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

    [Theory]
    [InlineData(DropMode.ScrapOutOfReach)]
    [InlineData(DropMode.ScrapEverything)]
    public void MedicineFallsToItsCategoryRuleWhileTheSettingIsOff(DropMode mode)
    {
        var d = LootEvaluator.Evaluate(Drop(OutOfReach, ItemCategory.Medicine), Rules(mode));
        Assert.Equal(DecisionReason.CategoryRule, d.Reason);
        Assert.Equal(Outcome.Keep, d.Outcome);
    }

    [Fact]
    public void MedicineStillFollowsItsOwnScrapRuleWhileTheSettingIsOff()
    {
        LootRules rules = TestRules.Basic(r => { r.dropMode = DropMode.ScrapOutOfReach; r.medicine = new CategoryRule(Outcome.Scrap); });
        var d = LootEvaluator.Evaluate(Drop(OutOfReach, ItemCategory.Medicine), rules);
        Assert.Equal(Outcome.Scrap, d.Outcome);
        Assert.Equal(DecisionReason.CategoryRule, d.Reason);
    }

    [Fact]
    public void MedicineBeyondReachIsScrappedWhenTheSettingIsOn()
    {
        LootRules rules = TestRules.Basic(r => { r.dropMode = DropMode.ScrapOutOfReach; r.dropIncludesMedicine = true; });
        var d = LootEvaluator.Evaluate(Drop(OutOfReach, ItemCategory.Medicine), rules);
        Assert.Equal(Outcome.Scrap, d.Outcome);
        Assert.Equal(DecisionReason.OutOfReachScrap, d.Reason);
    }

    [Fact]
    public void ScrapEverythingScrapsMedicineWhenTheSettingIsOn()
    {
        LootRules rules = TestRules.Basic(r => { r.dropMode = DropMode.ScrapEverything; r.dropIncludesMedicine = true; });
        var d = LootEvaluator.Evaluate(Drop(InReach, ItemCategory.Medicine), rules);
        Assert.Equal(Outcome.Scrap, d.Outcome);
        Assert.Equal(DecisionReason.ScrapEverything, d.Reason);
    }

    [Fact]
    public void TheMedicineSettingDoesNotChangeOtherInventory()
    {
        foreach (ItemCategory category in new[] { ItemCategory.Weapon, ItemCategory.Apparel, ItemCategory.Drugs, ItemCategory.Food })
        {
            var d = LootEvaluator.Evaluate(Drop(OutOfReach, category), Rules(DropMode.ScrapOutOfReach));
            Assert.Equal(Outcome.Scrap, d.Outcome);
        }
    }

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
    [InlineData(DropMode.KeepOriginal)]
    public void ComponentsFallbackBreaksGearIntoComponentsWheneverEraHandlingIsOn(DropMode mode)
    {
        var d = LootEvaluator.Evaluate(Drop(Components), Rules(mode));
        Assert.Equal(Outcome.Disassemble, d.Outcome);
        Assert.Equal(DecisionReason.ComponentsFallback, d.Reason);
    }

    [Fact]
    public void ComponentsFallbackIsIgnoredWhenTheDropModeIsOff()
    {
        var d = LootEvaluator.Evaluate(Drop(Components), Rules(DropMode.Off));
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
