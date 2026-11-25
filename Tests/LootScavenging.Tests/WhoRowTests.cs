using LootScavenging.Model;
using Xunit;

namespace LootScavenging.Tests;

/// <summary>
/// The Who rows and the per-category beyond-reach scrap that replaced the drop mode (ticket 25): scrap-out-of-reach is a
/// category rule with "only beyond reach", scrap-everything a plain scrap rule, and keep-original a row set to Mark.
/// </summary>
public class WhoRowTests
{
    private static DropCandidate Drop(bool outOfReach, ItemCategory category = ItemCategory.Weapon, OwnerGroup owner = OwnerGroup.Hostile)
    {
        var era = outOfReach ? new EraResult(Era.Industrial, EraSource.Item, true) : new EraResult(Era.Neolithic, EraSource.Item, false);
        return new DropCandidate { Owner = owner, Moment = Moment.Death, Map = MapKind.Home, Category = category, ItemEra = era, OutOfReach = outOfReach };
    }

    private static LootRules BeyondReach() => TestRules.Basic(r =>
    {
        r.weapons = CategoryRule.ScrapBeyondReach();
        r.apparel = CategoryRule.ScrapBeyondReach();
    });

    [Fact]
    public void MedicineKeptByItsRuleIsKeptBeyondReach()
    {
        var d = LootEvaluator.Evaluate(Drop(true, ItemCategory.Medicine), BeyondReach());
        Assert.Equal(DecisionReason.CategoryRule, d.Reason);
        Assert.Equal(Outcome.Keep, d.Outcome);
    }

    [Fact]
    public void MedicineFollowsItsOwnScrapRule()
    {
        LootRules rules = TestRules.Basic(r => { r.medicine = new CategoryRule(Outcome.Scrap); });
        var d = LootEvaluator.Evaluate(Drop(false, ItemCategory.Medicine), rules);
        Assert.Equal(Outcome.Scrap, d.Outcome);
        Assert.Equal(DecisionReason.CategoryRule, d.Reason);
    }

    [Fact]
    public void MedicineSetToScrapBeyondReachScrapsOnlyOutOfReachMedicine()
    {
        LootRules rules = TestRules.Basic(r => { r.medicine = CategoryRule.ScrapBeyondReach(); });
        Assert.Equal(Outcome.Scrap, LootEvaluator.Evaluate(Drop(true, ItemCategory.Medicine), rules).Outcome);
        Assert.Equal(DecisionReason.ConditionsNotMet, LootEvaluator.Evaluate(Drop(false, ItemCategory.Medicine), rules).Reason);
    }

    [Fact]
    public void ACategoryRuleDoesNotChangeOtherInventory()
    {
        LootRules rules = TestRules.Basic(r => { r.medicine = CategoryRule.ScrapBeyondReach(); });
        foreach (ItemCategory category in new[] { ItemCategory.Apparel, ItemCategory.Drugs, ItemCategory.Food })
            Assert.Equal(Outcome.Keep, LootEvaluator.Evaluate(Drop(true, category), rules).Outcome);
    }

    [Fact]
    public void ScrapBeyondReachScrapsGearBeyondReach()
    {
        var d = LootEvaluator.Evaluate(Drop(true), BeyondReach());
        Assert.Equal(Outcome.Scrap, d.Outcome);
        Assert.Equal(DecisionReason.CategoryRule, d.Reason);
    }

    [Fact]
    public void ScrapBeyondReachLeavesGearWithinReach()
    {
        var d = LootEvaluator.Evaluate(Drop(false), BeyondReach());
        Assert.Equal(Outcome.Keep, d.Outcome);
        Assert.Equal(DecisionReason.ConditionsNotMet, d.Reason);
    }

    [Fact]
    public void APlainScrapRuleScrapsGearWithinReachToo()
    {
        LootRules rules = TestRules.Basic(r => { r.apparel = new CategoryRule(Outcome.Scrap); });
        var d = LootEvaluator.Evaluate(Drop(false, ItemCategory.Apparel), rules);
        Assert.Equal(Outcome.Scrap, d.Outcome);
        Assert.Equal(DecisionReason.CategoryRule, d.Reason);
    }

    [Fact]
    public void APlainScrapRuleScrapsGearWithNoEra()
    {
        LootRules rules = TestRules.Basic(r => { r.weapons = new CategoryRule(Outcome.Scrap); });
        var d = LootEvaluator.Evaluate(new DropCandidate { Owner = OwnerGroup.Hostile, Map = MapKind.Home, Category = ItemCategory.Weapon }, rules);
        Assert.Equal(Outcome.Scrap, d.Outcome);
    }

    [Fact]
    public void AKeepRuleIgnoresEra()
    {
        var d = LootEvaluator.Evaluate(Drop(true, ItemCategory.Apparel), TestRules.Basic());
        Assert.Equal(Outcome.Keep, d.Outcome);
        Assert.Equal(DecisionReason.CategoryRule, d.Reason);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AMarkRowKeepsGearWholeWhateverTheCategoryRule(bool outOfReach)
    {
        LootRules rules = TestRules.Basic(r => { r.whoHostile = WhoRule.Mark; r.weapons = new CategoryRule(Outcome.Delete); });
        var d = LootEvaluator.Evaluate(Drop(outOfReach), rules);
        Assert.Equal(Outcome.Keep, d.Outcome);
        Assert.Equal(DecisionReason.MarkRow, d.Reason);
    }

    [Fact]
    public void AMarkRowCoversItsGroup()
    {
        LootRules rules = TestRules.Basic(r => { r.whoNonHostile = WhoRule.Mark; });
        Assert.True(LootEvaluator.CoversPawn(OwnerGroup.NonHostileFaction, MapKind.Home, rules));
        Assert.False(LootEvaluator.CoversPawn(OwnerGroup.NoFaction, MapKind.Home, rules));
    }

    [Fact]
    public void EachGroupReadsItsOwnRow()
    {
        var rules = new LootRules();
        foreach (OwnerGroup group in LootRules.RowGroups)
            rules.SetRow(group, WhoRule.Exclude);
        foreach (OwnerGroup group in LootRules.RowGroups)
        {
            rules.SetRow(group, WhoRule.Mark);
            Assert.Equal(WhoRule.Mark, rules.RowFor(group));
            foreach (OwnerGroup other in LootRules.RowGroups)
            {
                if (other != group)
                    Assert.Equal(WhoRule.Exclude, rules.RowFor(other));
            }
            rules.SetRow(group, WhoRule.Exclude);
        }
        Assert.Equal(WhoRule.Exclude, rules.RowFor(OwnerGroup.Colony));
    }

    [Fact]
    public void OnlyTheLootRowsCountForTheUnfamiliarLabel()
    {
        var rules = TestRules.Basic(r => { r.whoOtherJoiners = WhoRule.Mark; r.whoRecruitedPrisoners = WhoRule.Mark; });
        Assert.False(rules.AnyLootRowMarks);
        rules.whoPrisoners = WhoRule.Mark;
        Assert.True(rules.AnyLootRowMarks);
    }

    [Fact]
    public void EarlierStepsStillWinOverTheRow()
    {
        LootRules rules = TestRules.Basic(r => { r.weapons = new CategoryRule(Outcome.Scrap); });
        var d = LootEvaluator.Evaluate(Drop(true, owner: OwnerGroup.Colony), rules);
        Assert.Equal(DecisionReason.ColonyPawn, d.Reason);
    }
}
