using System.Collections.Generic;
using LootScavenging.Model;
using Xunit;

namespace LootScavenging.Tests;

// The Def-to-settings mapping: the pack's Progression defaults and lock list drive every preset. Each test builds its own
// PresetConfig, so nothing here touches the shared Presets.Config.
public class PackDefaultsTests
{
    private static PresetConfig Pack(System.Action<SettingValues> patch, params string[] locked)
    {
        SettingValues progression = Presets.CodeProgression();
        patch?.Invoke(progression);
        return new PresetConfig(progression, new SettingLocks(locked));
    }

    [Fact]
    public void CodeConfigMatchesTheCompiledDefaults()
    {
        Assert.True(Presets.Defaults(PresetConfig.Code()).SameAs(Presets.CodeProgression()));
    }

    [Fact]
    public void APatchedDefaultIsWhatAFreshConfigStartsWith()
    {
        PresetConfig pack = Pack(v => v.rules.dropMode = DropMode.KeepOriginal);
        Assert.Equal(DropMode.KeepOriginal, Presets.Defaults(pack).rules.dropMode);
        Assert.Equal(DropMode.KeepOriginal, Presets.Build(PresetId.Progression, pack).rules.dropMode);
    }

    [Fact]
    public void AFreshConfigWithPatchedDefaultsIsNotMarkedChanged()
    {
        PresetConfig pack = Pack(v =>
        {
            v.rules.dropMode = DropMode.KeepOriginal;
            v.scrapMarketValue = 2f;
        });
        SettingValues fresh = Presets.Defaults(pack);
        Assert.False(Presets.IsChanged(PresetId.Progression, fresh, pack));
    }

    [Fact]
    public void APatchedDefaultCarriesIntoCleanupAndVanillaOnlyWhereThePresetIsSilent()
    {
        PresetConfig pack = Pack(v =>
        {
            v.scrapMarketValue = 2f;
            v.corpses.humanlike = true; // Vanilla must still leave corpses alone
            v.rules.ammunition = new CategoryRule(Outcome.Delete);
        });
        SettingValues cleanup = Presets.Build(PresetId.Cleanup, pack);
        SettingValues vanilla = Presets.Build(PresetId.Vanilla, pack);
        Assert.Equal(2f, cleanup.scrapMarketValue);
        Assert.Equal(2f, vanilla.scrapMarketValue);
        Assert.False(vanilla.corpses.humanlike);
        Assert.Equal(Outcome.Keep, vanilla.rules.ammunition.outcome);
        Assert.Equal(Outcome.Keep, cleanup.rules.ammunition.outcome);
        Assert.Equal(Outcome.Delete, cleanup.rules.weapons.outcome);
    }

    [Fact]
    public void ALockedSettingKeepsThePackValueWhicheverPresetIsApplied()
    {
        PresetConfig pack = Pack(v => v.rules.disassemblyYield = DisassemblyYield.EraTypical, "rules.disassemblyYield", "rules.weapons", "corpses.humanlike");
        SettingValues current = Presets.Defaults(pack);
        foreach (PresetId preset in new[] { PresetId.Progression, PresetId.Cleanup, PresetId.Vanilla })
        {
            SettingValues applied = Presets.Apply(preset, current, pack);
            Assert.Equal(DisassemblyYield.EraTypical, applied.rules.disassemblyYield);
            Assert.Equal(Outcome.Keep, applied.rules.weapons.outcome); // Cleanup would delete poor weapons
            Assert.False(applied.corpses.humanlike); // Cleanup would remove corpses
            Assert.False(Presets.IsChanged(preset, applied, pack));
        }
    }

    [Fact]
    public void EnforcingPutsALockedValueBackAndLeavesOthersAlone()
    {
        PresetConfig pack = Pack(null, "rules.dropMode", "scrapOnGround", "rules.weapons");
        SettingValues edited = Presets.Defaults(pack);
        edited.rules.dropMode = DropMode.Off;
        edited.scrapOnGround = false;
        edited.rules.weapons.outcome = Outcome.Delete;
        edited.rules.apparel.outcome = Outcome.Delete;
        pack.Locks.Enforce(edited, pack.Progression);
        Assert.Equal(DropMode.ScrapOutOfReach, edited.rules.dropMode);
        Assert.True(edited.scrapOnGround);
        Assert.Equal(Outcome.Keep, edited.rules.weapons.outcome);
        Assert.Equal(Outcome.Delete, edited.rules.apparel.outcome);
        Assert.NotSame(pack.Progression.rules.weapons, edited.rules.weapons);
    }

    [Fact]
    public void AnUnlockedPresetChangeStillCountsAsChanged()
    {
        PresetConfig pack = Pack(null, "rules.dropMode");
        SettingValues current = Presets.Defaults(pack);
        current.rules.apparel.outcome = Outcome.Delete;
        Assert.True(Presets.IsChanged(PresetId.Progression, current, pack));
    }

    [Fact]
    public void UnknownLockNamesAreIgnoredAndReported()
    {
        var locks = new SettingLocks(new[] { "rules.dropMode", "rules.noSuchSetting", "  ", null, "rules.keepList" });
        Assert.True(locks.IsLocked("rules.dropMode"));
        Assert.False(locks.IsLocked("rules.noSuchSetting"));
        Assert.Equal(new List<string> { "rules.noSuchSetting", "rules.keepList" }, locks.Unknown);
    }

    [Fact]
    public void EveryLockableNameIsARealSettingField()
    {
        var values = new SettingValues();
        foreach (string name in SettingLocks.Lockable)
            Assert.True(ValueWalk.PathExists(values, name), name);
    }

    [Fact]
    public void LocksAreOutsideTheSettingsSnapshot()
    {
        // SettingValues only copies and compares fields it supports; the lock set must never become one of them.
        SettingValues v = Presets.Defaults(Pack(null, "rules.dropMode"));
        Assert.True(v.Clone().SameAs(v));
    }
}
