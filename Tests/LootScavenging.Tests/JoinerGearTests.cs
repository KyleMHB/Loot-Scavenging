using LootScavenging.Model;
using Xunit;

namespace LootScavenging.Tests;

public class JoinerGearTests
{
    [Fact]
    public void DefaultsSortPrisonersAndMarkEveryoneElse()
    {
        var rules = new LootRules();
        Assert.Equal(JoinerGear.FollowDropMode, rules.prisonerJoinGear);
        Assert.Equal(JoinerGear.MarkUnfamiliar, rules.otherJoinGear);
        var progression = Presets.Build(PresetId.Progression).rules;
        Assert.Equal(JoinerGear.FollowDropMode, progression.prisonerJoinGear);
        Assert.Equal(JoinerGear.MarkUnfamiliar, progression.otherJoinGear);
    }

    [Theory]
    [InlineData(PresetId.Cleanup)]
    [InlineData(PresetId.Vanilla)]
    public void CleanupAndVanillaLeaveJoinersAlone(PresetId preset)
    {
        var rules = Presets.Build(preset).rules;
        Assert.Equal(JoinerGear.LeaveAlone, rules.prisonerJoinGear);
        Assert.Equal(JoinerGear.LeaveAlone, rules.otherJoinGear);
    }

    [Fact]
    public void EachKindOfJoinerReadsItsOwnSetting()
    {
        var rules = new LootRules { prisonerJoinGear = JoinerGear.LeaveAlone, otherJoinGear = JoinerGear.FollowDropMode };
        Assert.Equal(JoinerGear.LeaveAlone, JoinerGears.For(rules, wasPrisoner: true));
        Assert.Equal(JoinerGear.FollowDropMode, JoinerGears.For(rules, wasPrisoner: false));
    }

    [Fact]
    public void ChangingAJoinSettingMakesAPresetChanged()
    {
        SettingValues current = Presets.Build(PresetId.Progression);
        current.rules.otherJoinGear = JoinerGear.LeaveAlone;
        Assert.True(Presets.IsChanged(PresetId.Progression, current));
    }

    [Theory]
    [InlineData(Moment.Downed)]
    [InlineData(Moment.Strip)]
    public void JoiningIsNotGatedByTheDownedAndStripSetting(Moment gated)
    {
        var rules = TestRules.Basic(r => { r.onDownedAndStrip = false; });
        var drop = new DropCandidate { Owner = OwnerGroup.Hostile, Moment = Moment.Join, Map = MapKind.Home, Category = ItemCategory.Weapon };
        Assert.Equal(Outcome.Delete, LootEvaluator.Evaluate(drop, rules).Outcome);

        drop.Moment = gated;
        Assert.Equal(DecisionReason.MomentOff, LootEvaluator.Evaluate(drop, rules).Reason);
    }

    [Fact]
    public void JoiningStillNeedsTheOwnerGroupOn()
    {
        var rules = TestRules.Basic(r => { r.prisoners = false; });
        var drop = new DropCandidate { Owner = OwnerGroup.Prisoner, Moment = Moment.Join, Map = MapKind.Home, Category = ItemCategory.Weapon };
        Assert.Equal(DecisionReason.OwnerGroupOff, LootEvaluator.Evaluate(drop, rules).Reason);
    }

    [Theory]
    [InlineData(JoinerGear.MarkUnfamiliar, true, true)]
    [InlineData(JoinerGear.MarkUnfamiliar, false, false)]
    [InlineData(JoinerGear.FollowDropMode, true, false)]
    [InlineData(JoinerGear.LeaveAlone, true, false)]
    public void OnlyOutOfReachGearIsMarked(JoinerGear setting, bool outOfReach, bool marked)
    {
        Assert.Equal(marked, UnfamiliarRule.ShouldMarkJoining(setting, outOfReach));
    }

    [Fact]
    public void AJoinMarkNeedsNoDropMode()
    {
        // The mark is not behind IsUnfamiliar(mode, ...): the ordinary rule says no in every drop mode but keep-original.
        Assert.False(UnfamiliarRule.IsUnfamiliar(DropMode.ScrapOutOfReach, UnfamiliarScope.AllOutOfReach, true, Era.Spacer, Era.Neolithic));
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
