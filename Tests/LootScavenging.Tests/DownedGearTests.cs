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
        var rules = new LootRules { sortWhenArrested = false, sortWhenStripped = false };
        Assert.True(DownedGears.KeepsWeapon(rules, OwnerGroup.Hostile, MapKind.Home));
    }

    [Fact]
    public void DropTheWeaponKeepsNothing()
    {
        var rules = new LootRules { downedGear = DownedGear.DropWeapon };
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
    }

    // Ticket 24: loot stays on the body by default, and colonists keep their gear except in Vanilla.
    [Theory]
    [InlineData(PresetId.Progression, true)]
    [InlineData(PresetId.Cleanup, true)]
    [InlineData(PresetId.Vanilla, false)]
    public void LootStaysOnTheBodyAndColonistsKeepTheirGear(PresetId preset, bool colonistsKeep)
    {
        Assert.Equal(DeathDrop.AllOnBody, new SettingValues().deathDrop);
        Assert.True(new SettingValues().colonistsKeepGear);
        Assert.Equal(DeathDrop.AllOnBody, Presets.Build(preset).deathDrop);
        Assert.Equal(colonistsKeep, Presets.Build(preset).colonistsKeepGear);
    }

    [Fact]
    public void ChangingItMakesAPresetChanged()
    {
        SettingValues current = Presets.Build(PresetId.Progression);
        current.rules.downedGear = DownedGear.DropWeapon;
        Assert.True(Presets.IsChanged(PresetId.Progression, current));
    }
}
