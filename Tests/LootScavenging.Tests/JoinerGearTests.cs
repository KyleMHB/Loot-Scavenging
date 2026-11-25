using LootScavenging.Model;
using Xunit;

namespace LootScavenging.Tests;

/// <summary>The two joiner Who rows (ticket 20, ticket 25): Recruited prisoners and Other joiners.</summary>
public class JoinerGearTests
{
    [Fact]
    public void DefaultsSortPrisonersAndMarkEveryoneElse()
    {
        var rules = new LootRules();
        Assert.Equal(WhoRule.Apply, rules.whoRecruitedPrisoners);
        Assert.Equal(WhoRule.Mark, rules.whoOtherJoiners);
        var progression = Presets.Build(PresetId.Progression).rules;
        Assert.Equal(WhoRule.Apply, progression.whoRecruitedPrisoners);
        Assert.Equal(WhoRule.Mark, progression.whoOtherJoiners);
    }

    [Theory]
    [InlineData(PresetId.Cleanup)]
    [InlineData(PresetId.Vanilla)]
    public void CleanupAndVanillaLeaveJoinersAlone(PresetId preset)
    {
        var rules = Presets.Build(preset).rules;
        Assert.Equal(WhoRule.Exclude, rules.whoRecruitedPrisoners);
        Assert.Equal(WhoRule.Exclude, rules.whoOtherJoiners);
    }

    [Fact]
    public void EachKindOfJoinerReadsItsOwnRow()
    {
        var rules = new LootRules { whoRecruitedPrisoners = WhoRule.Exclude, whoOtherJoiners = WhoRule.Apply };
        Assert.Equal(WhoRule.Exclude, rules.RowFor(OwnerGroup.RecruitedPrisoner));
        Assert.Equal(WhoRule.Apply, rules.RowFor(OwnerGroup.OtherJoiner));
    }

    [Fact]
    public void ChangingAJoinerRowMakesAPresetChanged()
    {
        SettingValues current = Presets.Build(PresetId.Progression);
        current.rules.whoOtherJoiners = WhoRule.Exclude;
        Assert.True(Presets.IsChanged(PresetId.Progression, current));
    }

    // A joiner's gear is judged under his joiner row, not the row of the group he came from.
    [Fact]
    public void AJoinerSetToApplyHasHisGearSortedByTheItemRules()
    {
        var rules = TestRules.Basic(r => { r.whoHostile = WhoRule.Exclude; r.whoRecruitedPrisoners = WhoRule.Apply; });
        var drop = new DropCandidate { Owner = OwnerGroup.RecruitedPrisoner, Moment = Moment.Join, Map = MapKind.Home, Category = ItemCategory.Weapon };
        var d = LootEvaluator.Evaluate(drop, rules);
        Assert.Equal(Outcome.Delete, d.Outcome);
        Assert.Equal(DecisionReason.CategoryRule, d.Reason);
    }

    [Fact]
    public void AnExcludedJoinerRowLeavesTheGearAlone()
    {
        var rules = TestRules.Basic(r => { r.whoOtherJoiners = WhoRule.Exclude; });
        var drop = new DropCandidate { Owner = OwnerGroup.OtherJoiner, Moment = Moment.Join, Map = MapKind.Home, Category = ItemCategory.Weapon };
        Assert.Equal(DecisionReason.OwnerGroupOff, LootEvaluator.Evaluate(drop, rules).Reason);
    }

    [Theory]
    [InlineData(WhoRule.Mark, true, true)]
    [InlineData(WhoRule.Mark, false, false)]
    [InlineData(WhoRule.Apply, true, false)]
    [InlineData(WhoRule.Exclude, true, false)]
    public void OnlyOutOfReachGearIsMarked(WhoRule row, bool outOfReach, bool marked)
    {
        Assert.Equal(marked, UnfamiliarRule.ShouldMarkJoining(row, outOfReach));
    }

    [Fact]
    public void AJoinMarkNeedsNoLootRowSetToMark()
    {
        // The mark is not behind IsUnfamiliar: with no loot row set to Mark the ordinary rule says no.
        Assert.False(UnfamiliarRule.IsUnfamiliar(false, UnfamiliarScope.AllOutOfReach, true, Era.Spacer, Era.Neolithic));
        Assert.True(UnfamiliarRule.JoinMarkHolds(Era.Spacer, true, Era.Neolithic));
    }

    [Fact]
    public void AnItemWithNoMarkIsNotUnfamiliarThroughIt()
    {
        Assert.False(UnfamiliarRule.JoinMarkHolds(Era.Undefined, true, Era.Neolithic));
    }

    [Fact]
    public void AJoinMarkClearsOnceResearchIsDoneAndTheColonyCatchesUp()
    {
        Assert.True(UnfamiliarRule.JoinMarkHolds(Era.Spacer, false, Era.Industrial));
        Assert.False(UnfamiliarRule.JoinMarkHolds(Era.Spacer, false, Era.Spacer));
        Assert.False(UnfamiliarRule.JoinMarkHolds(Era.Medieval, false, Era.Industrial));
    }
}
