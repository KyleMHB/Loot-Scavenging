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
        var rules = TestRules.Basic(r => { r.hostile = true; r.nonHostileFaction = true; r.noFaction = true; r.prisoners = true; });

        var decision = LootEvaluator.Evaluate(Drop(owner: OwnerGroup.Colony), rules);

        Assert.Equal(Outcome.Keep, decision.Outcome);
        Assert.Equal(DecisionReason.ColonyPawn, decision.Reason);
    }

    [Fact]
    public void WildAnimalGearIsLeftAloneWhileTheNoFactionGroupIsOff()
    {
        var off = LootEvaluator.Evaluate(Drop(owner: OwnerGroup.NoFaction), TestRules.Basic(r => { r.noFaction = false; }));
        var on = LootEvaluator.Evaluate(Drop(owner: OwnerGroup.NoFaction), TestRules.Basic(r => { r.noFaction = true; }));

        Assert.Equal(Outcome.Keep, off.Outcome);
        Assert.Equal(DecisionReason.OwnerGroupOff, off.Reason);
        Assert.Equal(Outcome.Delete, on.Outcome);
    }

    [Theory]
    [InlineData(OwnerGroup.Hostile)]
    [InlineData(OwnerGroup.NonHostileFaction)]
    [InlineData(OwnerGroup.NoFaction)]
    [InlineData(OwnerGroup.Prisoner)]
    public void EachOwnerGroupHasItsOwnToggle(OwnerGroup group)
    {
        var allOff = TestRules.Basic(r => { r.hostile = false; r.nonHostileFaction = false; r.noFaction = false; r.prisoners = false; });
        var onlyThis = TestRules.Basic(r =>
        {
            r.hostile = group == OwnerGroup.Hostile;
            r.nonHostileFaction = group == OwnerGroup.NonHostileFaction;
            r.noFaction = group == OwnerGroup.NoFaction;
            r.prisoners = group == OwnerGroup.Prisoner;
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

    [Theory]
    [InlineData(Moment.Downed)]
    [InlineData(Moment.Strip)]
    public void DownedAndStripFollowTheMomentSetting(Moment moment)
    {
        var deathOnly = LootEvaluator.Evaluate(Drop(moment: moment), TestRules.Basic(r => { r.onDownedAndStrip = false; }));
        var anyDrop = LootEvaluator.Evaluate(Drop(moment: moment), TestRules.Basic(r => { r.onDownedAndStrip = true; }));

        Assert.Equal(Outcome.Keep, deathOnly.Outcome);
        Assert.Equal(DecisionReason.MomentOff, deathOnly.Reason);
        Assert.Equal(Outcome.Delete, anyDrop.Outcome);
    }

    [Fact]
    public void DeathAlwaysAppliesWhateverTheMomentSetting()
    {
        var decision = LootEvaluator.Evaluate(Drop(moment: Moment.Death), TestRules.Basic(r => { r.onDownedAndStrip = false; }));

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
    public void TheOwnerCheckComesBeforeThePlaceAndMomentChecks()
    {
        var rules = TestRules.Basic(r => { r.noFaction = false; r.homeMapOnly = true; r.onDownedAndStrip = false; });

        var decision = LootEvaluator.Evaluate(Drop(owner: OwnerGroup.NoFaction, map: MapKind.Away, moment: Moment.Strip), rules);

        Assert.Equal(DecisionReason.OwnerGroupOff, decision.Reason);
    }
}
