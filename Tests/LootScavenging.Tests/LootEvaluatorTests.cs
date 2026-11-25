using LootScavenging.Model;
using Xunit;

namespace LootScavenging.Tests;

public class LootEvaluatorTests
{
    private static DropCandidate Drop(OwnerGroup owner = OwnerGroup.Hostile, Moment moment = Moment.Death,
        MapKind map = MapKind.Home, ItemCategory category = ItemCategory.Weapon)
    {
        return new DropCandidate { Owner = owner, Moment = moment, Map = map, Category = category };
    }

    [Fact]
    public void WeaponDeleteRuleDeletesAHostileWeaponAndKeepsItsApparelAtDeath()
    {
        var rules = TestRules.Basic();

        var weapon = LootEvaluator.Evaluate(Drop(category: ItemCategory.Weapon), rules);
        var apparel = LootEvaluator.Evaluate(Drop(category: ItemCategory.Apparel), rules);

        Assert.Equal(Outcome.Delete, weapon.Outcome);
        Assert.Equal(DecisionReason.CategoryRule, weapon.Reason);
        Assert.Equal(Outcome.Keep, apparel.Outcome);
        Assert.Equal(DecisionReason.CategoryRule, apparel.Reason);
    }

    [Fact]
    public void ColonyGearIsNeverTouchedEvenWithEveryGroupOn()
    {
        var rules = TestRules.Basic(r => { foreach (OwnerGroup g in LootRules.RowGroups) r.SetRow(g, WhoRule.Apply); });

        var decision = LootEvaluator.Evaluate(Drop(owner: OwnerGroup.Colony), rules);

        Assert.Equal(Outcome.Keep, decision.Outcome);
        Assert.Equal(DecisionReason.ColonyPawn, decision.Reason);
    }

    [Fact]
    public void WildGearIsLeftAloneWhileTheWildRowIsExcluded()
    {
        var off = LootEvaluator.Evaluate(Drop(owner: OwnerGroup.NoFaction), TestRules.Basic(r => { r.whoWild = WhoRule.Exclude; }));
        var on = LootEvaluator.Evaluate(Drop(owner: OwnerGroup.NoFaction), TestRules.Basic(r => { r.whoWild = WhoRule.Apply; }));

        Assert.Equal(Outcome.Keep, off.Outcome);
        Assert.Equal(DecisionReason.OwnerGroupOff, off.Reason);
        Assert.Equal(Outcome.Delete, on.Outcome);
    }

    [Theory]
    [InlineData(OwnerGroup.Hostile)]
    [InlineData(OwnerGroup.NonHostileFaction)]
    [InlineData(OwnerGroup.NoFaction)]
    [InlineData(OwnerGroup.Prisoner)]
    [InlineData(OwnerGroup.RecruitedPrisoner)]
    [InlineData(OwnerGroup.OtherJoiner)]
    public void EachOwnerGroupHasItsOwnRow(OwnerGroup group)
    {
        var allOff = TestRules.Basic(r => { foreach (OwnerGroup g in LootRules.RowGroups) r.SetRow(g, WhoRule.Exclude); });
        var onlyThis = TestRules.Basic(r =>
        {
            foreach (OwnerGroup g in LootRules.RowGroups)
                r.SetRow(g, g == group ? WhoRule.Apply : WhoRule.Exclude);
        });

        Assert.Equal(DecisionReason.OwnerGroupOff, LootEvaluator.Evaluate(Drop(owner: group), allOff).Reason);
        Assert.Equal(Outcome.Delete, LootEvaluator.Evaluate(Drop(owner: group), onlyThis).Outcome);
    }

    [Fact]
    public void HomeMapOnlyLeavesGearAloneAwayFromHome()
    {
        var rules = TestRules.Basic(r => { r.homeMapOnly = true; });

        var away = LootEvaluator.Evaluate(Drop(map: MapKind.Away), rules);
        var home = LootEvaluator.Evaluate(Drop(map: MapKind.Home), rules);

        Assert.Equal(Outcome.Keep, away.Outcome);
        Assert.Equal(DecisionReason.NotHomeMap, away.Reason);
        Assert.Equal(Outcome.Delete, home.Outcome);
    }

    [Fact]
    public void AwayMapsFollowTheRulesWhenHomeMapOnlyIsOff()
    {
        var decision = LootEvaluator.Evaluate(Drop(map: MapKind.Away), TestRules.Basic(r => { r.homeMapOnly = false; }));

        Assert.Equal(Outcome.Delete, decision.Outcome);
    }

    [Fact]
    public void GearWithNoMapIsAlwaysLeftAlone()
    {
        var decision = LootEvaluator.Evaluate(Drop(map: MapKind.None), TestRules.Basic(r => { r.homeMapOnly = false; }));

        Assert.Equal(Outcome.Keep, decision.Outcome);
        Assert.Equal(DecisionReason.NoMap, decision.Reason);
    }

    // The arrest and strip toggles are cut (ticket 25): gear is sorted at every moment it comes off.
    [Theory]
    [InlineData(Moment.Death)]
    [InlineData(Moment.Downed)]
    [InlineData(Moment.Strip)]
    [InlineData(Moment.Arrest)]
    [InlineData(Moment.Join)]
    public void EveryMomentIsSorted(Moment moment)
    {
        var decision = LootEvaluator.Evaluate(Drop(moment: moment), TestRules.Basic());

        Assert.Equal(Outcome.Delete, decision.Outcome);
    }

    [Fact]
    public void EachCategoryFollowsItsOwnRule()
    {
        var rules = TestRules.Basic();
        rules.weapons.outcome = Outcome.Keep;
        rules.apparel.outcome = Outcome.Delete;

        Assert.Equal(Outcome.Keep, LootEvaluator.Evaluate(Drop(category: ItemCategory.Weapon), rules).Outcome);
        Assert.Equal(Outcome.Delete, LootEvaluator.Evaluate(Drop(category: ItemCategory.Apparel), rules).Outcome);
    }

    [Fact]
    public void InventoryGroupsAreKeptByDefault()
    {
        var decision = LootEvaluator.Evaluate(Drop(category: ItemCategory.Other), TestRules.Basic());

        Assert.Equal(Outcome.Keep, decision.Outcome);
        Assert.Equal(DecisionReason.CategoryRule, decision.Reason);
    }

    [Fact]
    public void TheOwnerCheckComesBeforeThePlaceCheck()
    {
        var rules = TestRules.Basic(r => { r.whoWild = WhoRule.Exclude; r.homeMapOnly = true; });

        var decision = LootEvaluator.Evaluate(Drop(owner: OwnerGroup.NoFaction, map: MapKind.Away, moment: Moment.Strip), rules);

        Assert.Equal(DecisionReason.OwnerGroupOff, decision.Reason);
    }

    // Ticket 25: a row set to Mark is checked after the owner, place, protection and remove list, before the category rule.
    [Fact]
    public void ThePlaceCheckComesBeforeAMarkRow()
    {
        var rules = TestRules.Basic(r => { r.whoHostile = WhoRule.Mark; r.homeMapOnly = true; });
        Assert.Equal(DecisionReason.NotHomeMap, LootEvaluator.Evaluate(Drop(map: MapKind.Away), rules).Reason);
        Assert.Equal(DecisionReason.NoMap, LootEvaluator.Evaluate(Drop(map: MapKind.None), rules).Reason);
    }

    [Fact]
    public void ProtectionComesBeforeAMarkRow()
    {
        var rules = TestRules.Basic(r => r.whoHostile = WhoRule.Mark);
        var drop = Drop();
        drop.QuestTagged = true;
        Assert.Equal(DecisionReason.Protected, LootEvaluator.Evaluate(drop, rules).Reason);
    }

    [Fact]
    public void TheRemoveListComesBeforeAMarkRow()
    {
        var rules = TestRules.Basic(r => r.whoHostile = WhoRule.Mark);
        rules.removeList.Add("Gun_Autopistol");
        var drop = Drop();
        drop.DefName = "Gun_Autopistol";
        var decision = LootEvaluator.Evaluate(drop, rules);
        Assert.Equal(DecisionReason.RemoveList, decision.Reason);
        Assert.Equal(Outcome.Delete, decision.Outcome);
    }

    [Fact]
    public void AMarkRowComesBeforeTheCategoryRule()
    {
        var rules = TestRules.Basic(r => r.whoHostile = WhoRule.Mark);
        var decision = LootEvaluator.Evaluate(Drop(), rules);
        Assert.Equal(DecisionReason.MarkRow, decision.Reason);
        Assert.Equal(Outcome.Keep, decision.Outcome);
        Assert.True(LootEvaluator.CoversPawn(OwnerGroup.Hostile, MapKind.Home, rules));
    }

    [Fact]
    public void AnExcludedRowDoesNotCoverThePawn()
    {
        var rules = TestRules.Basic(r => r.whoHostile = WhoRule.Exclude);
        Assert.False(LootEvaluator.CoversPawn(OwnerGroup.Hostile, MapKind.Home, rules));
        Assert.False(LootEvaluator.CoversPawn(OwnerGroup.Colony, MapKind.Home, TestRules.Basic()));
    }
}
