using LootScavenging.Model;
using Xunit;

namespace LootScavenging.Tests;

public class DownedGearTests
{
    [Theory]
    [InlineData(PresetId.Progression)]
    [InlineData(PresetId.Cleanup)]
    [InlineData(PresetId.Vanilla)]
    public void EveryPresetKeepsTheWeapon(PresetId preset)
    {
        Assert.Equal(DownedGear.KeepWeapon, new LootRules().downedGear);
        Assert.Equal(DownedGear.KeepWeapon, Presets.Build(preset).rules.downedGear);
    }

    [Fact]
    public void ACoveredPawnKeepsItWhateverTheStripSetting()
    {
        var rules = new LootRules { onDownedAndStrip = false };
        Assert.True(DownedGears.KeepsWeapon(rules, OwnerGroup.Hostile, MapKind.Home));
    }

    [Fact]
    public void DropTheWeaponKeepsNothing()
    {
        var rules = new LootRules { downedGear = DownedGear.DropWeapon };
        Assert.False(DownedGears.KeepsWeapon(rules, OwnerGroup.Hostile, MapKind.Home));
        Assert.False(DownedGears.KeepsGear(rules, DeathDrop.Ground, OwnerGroup.Hostile, MapKind.Home));
    }

    [Fact]
    public void EverythingOnTheBodyKeepsGearUnderDropTheWeapon()
    {
        var rules = new LootRules { downedGear = DownedGear.DropWeapon };
        Assert.True(DownedGears.KeepsGear(rules, DeathDrop.AllOnBody, OwnerGroup.Hostile, MapKind.Home));
        Assert.False(DownedGears.KeepsWeapon(rules, OwnerGroup.Hostile, MapKind.Home));
    }

    [Fact]
    public void PawnsTheRulesDoNotCoverDropAsInTheBaseGame()
    {
        var rules = new LootRules { hostile = false, homeMapOnly = true };
        Assert.False(DownedGears.KeepsWeapon(rules, OwnerGroup.Colony, MapKind.Home));
        Assert.False(DownedGears.KeepsWeapon(rules, OwnerGroup.Hostile, MapKind.Home));
        Assert.False(DownedGears.KeepsWeapon(rules, OwnerGroup.NoFaction, MapKind.Away));
        Assert.True(DownedGears.KeepsWeapon(rules, OwnerGroup.NoFaction, MapKind.Home));
        Assert.False(DownedGears.KeepsGear(rules, DeathDrop.AllOnBody, OwnerGroup.Colony, MapKind.Home));
    }

    [Fact]
    public void ChangingItMakesAPresetChanged()
    {
        SettingValues current = Presets.Build(PresetId.Progression);
        current.rules.downedGear = DownedGear.DropWeapon;
        Assert.True(Presets.IsChanged(PresetId.Progression, current));
    }
}
