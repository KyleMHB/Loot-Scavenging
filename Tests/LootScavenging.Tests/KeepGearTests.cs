using LootScavenging.Model;
using Xunit;

namespace LootScavenging.Tests;

/// <summary>"Don't drop your stuff" (ticket 25, decision 2): three boxes that choose where gear lands.</summary>
public class KeepGearTests
{
    [Theory]
    [InlineData(PresetId.Progression, true)]
    [InlineData(PresetId.Cleanup, true)]
    [InlineData(PresetId.Vanilla, false)]
    public void EveryPresetButVanillaKeepsGearOnThePawn(PresetId preset, bool keeps)
    {
        var defaults = new SettingValues();
        Assert.True(defaults.colonistsKeepGear);
        Assert.True(defaults.hostilesKeepGear);
        Assert.True(defaults.nonHostilesKeepGear);
        SettingValues v = Presets.Build(preset);
        Assert.Equal(keeps, v.colonistsKeepGear);
        Assert.Equal(keeps, v.hostilesKeepGear);
        Assert.Equal(keeps, v.nonHostilesKeepGear);
    }

    [Fact]
    public void EachGroupReadsItsOwnBox()
    {
        var v = new SettingValues { colonistsKeepGear = true, hostilesKeepGear = false, nonHostilesKeepGear = true };
        Assert.True(v.KeepsGear(OwnerGroup.Colony, factionHostile: false));
        Assert.False(v.KeepsGear(OwnerGroup.Hostile, factionHostile: true));
        Assert.True(v.KeepsGear(OwnerGroup.NonHostileFaction, factionHostile: false));
        Assert.True(v.KeepsGear(OwnerGroup.NoFaction, factionHostile: false));
    }

    [Fact]
    public void UntickedTheGroupDropsItsGear()
    {
        var v = new SettingValues { hostilesKeepGear = false, nonHostilesKeepGear = false };
        Assert.False(v.KeepsGear(OwnerGroup.Hostile, factionHostile: true));
        Assert.False(v.KeepsGear(OwnerGroup.NonHostileFaction, factionHostile: false));
    }

    // A colony prisoner has no box of his own: he follows the hostility of his faction.
    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public void APrisonerFollowsHisFaction(bool factionHostile, bool keeps)
    {
        var v = new SettingValues { hostilesKeepGear = true, nonHostilesKeepGear = false };
        Assert.Equal(keeps, v.KeepsGear(OwnerGroup.Prisoner, factionHostile));
    }

    // A colony pawn uses the colonist box, never the hostile or non-hostile one.
    [Fact]
    public void AColonyPawnUsesOnlyTheColonistBox()
    {
        var v = new SettingValues { colonistsKeepGear = false, hostilesKeepGear = true, nonHostilesKeepGear = true };
        Assert.False(v.KeepsGear(OwnerGroup.Colony, factionHostile: true));
    }

    [Fact]
    public void ChangingABoxMakesAPresetChanged()
    {
        SettingValues current = Presets.Build(PresetId.Progression);
        current.hostilesKeepGear = false;
        Assert.True(Presets.IsChanged(PresetId.Progression, current));
    }
}
