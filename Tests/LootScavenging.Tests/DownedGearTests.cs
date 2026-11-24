using LootScavenging.Model;
using Xunit;

namespace LootScavenging.Tests;

public class DownedGearTests
{
    [Theory]
    [InlineData(PresetId.Progression)]
    [InlineData(PresetId.Cleanup)]
    [InlineData(PresetId.Vanilla)]
    public void EveryPresetKeepsTheWeaponInThePack(PresetId preset)
    {
        Assert.Equal(DownedGear.KeepInPack, new LootRules().downedGear);
        Assert.Equal(DownedGear.KeepInPack, Presets.Build(preset).rules.downedGear);
    }

    [Fact]
    public void ACoveredPawnKeepsItWhateverTheStripSetting()
    {
        var rules = new LootRules { onDownedAndStrip = false };
        Assert.True(DownedGears.KeepsInPack(rules, OwnerGroup.Hostile, MapKind.Home));
    }

    [Fact]
    public void DropTheWeaponKeepsNothing()
    {
        var rules = new LootRules { downedGear = DownedGear.DropWeapon };
        Assert.False(DownedGears.KeepsInPack(rules, OwnerGroup.Hostile, MapKind.Home));
    }

    [Fact]
    public void PawnsTheRulesDoNotCoverDropAsInTheBaseGame()
    {
        var rules = new LootRules { hostile = false, homeMapOnly = true };
        Assert.False(DownedGears.KeepsInPack(rules, OwnerGroup.Colony, MapKind.Home));
        Assert.False(DownedGears.KeepsInPack(rules, OwnerGroup.Hostile, MapKind.Home));
        Assert.False(DownedGears.KeepsInPack(rules, OwnerGroup.NoFaction, MapKind.Away));
        Assert.True(DownedGears.KeepsInPack(rules, OwnerGroup.NoFaction, MapKind.Home));
    }

    [Fact]
    public void ChangingItMakesAPresetChanged()
    {
        SettingValues current = Presets.Build(PresetId.Progression);
        current.rules.downedGear = DownedGear.DropWeapon;
        Assert.True(Presets.IsChanged(PresetId.Progression, current));
    }
}
