using System.Collections.Generic;
using System.Linq;
using LootScavenging.Model;
using Xunit;

namespace LootScavenging.Tests;

// A loaded settings file, possibly partial (an Auto Mod Config preset), against the pack's defaults and locks. "loaded" is
// what the game's loader leaves before the defs are read: the file's values, and the compiled-in values for missing keys.
public class SettingsLoadTests
{
    private static PresetConfig Pack(System.Action<SettingValues> patch, params string[] locked)
    {
        SettingValues progression = Presets.CodeProgression();
        patch?.Invoke(progression);
        return new PresetConfig(progression, new SettingLocks(locked));
    }

    private static HashSet<string> Keys(params string[] keys) => new HashSet<string>(keys);

    [Fact]
    public void APartialFileKeepsItsKeysAndTakesTheDefBaselineForTheRest()
    {
        PresetConfig pack = Pack(null);
        SettingValues loaded = Presets.CodeProgression();
        loaded.corpses.chance = 0.5f;
        loaded.deathDrop = DeathDrop.AllOnBody;
        loaded.rules.apparel.outcome = Outcome.Delete; // not in the file: a stale value must be replaced

        SettingValues result = SettingsLoad.Resolve(loaded, false, Keys("activePreset", "corpseChance", "deathDrop"), pack, out bool kept);

        Assert.True(kept);
        Assert.Equal(0.5f, result.corpses.chance);
        Assert.Equal(DeathDrop.AllOnBody, result.deathDrop);
        SettingValues expected = Presets.Defaults(pack);
        expected.corpses.chance = 0.5f;
        expected.deathDrop = DeathDrop.AllOnBody;
        Assert.True(result.SameAs(expected));
    }

    [Fact]
    public void APatchedDefDefaultFillsAMissingKey()
    {
        PresetConfig pack = Pack(v =>
        {
            v.rules.dropMode = DropMode.KeepOriginal;
            v.scrapMarketValue = 2f;
            v.rules.keepList = new List<string> { "Gun_Revolver" };
        });
        SettingValues loaded = Presets.CodeProgression();
        loaded.deathDrop = DeathDrop.AllOnBody;

        SettingValues result = SettingsLoad.Resolve(loaded, false, Keys("deathDrop"), pack, out _);

        Assert.Equal(DropMode.KeepOriginal, result.rules.dropMode);
        Assert.Equal(2f, result.scrapMarketValue);
        Assert.Equal(new List<string> { "Gun_Revolver" }, result.rules.keepList);
        Assert.Equal(DeathDrop.AllOnBody, result.deathDrop);
    }

    [Fact]
    public void AnUpgradedSeededConfigAlsoTakesThePatchedDefaultForAMissingKey()
    {
        PresetConfig pack = Pack(v => v.rules.dropMode = DropMode.KeepOriginal);
        SettingValues loaded = Presets.CodeProgression();
        loaded.corpses.humanlike = true;

        SettingValues result = SettingsLoad.Resolve(loaded, true, Keys("defaultsSeeded", "corpseHumanlike"), pack, out bool kept);

        Assert.True(kept);
        Assert.Equal(DropMode.KeepOriginal, result.rules.dropMode);
        Assert.True(result.corpses.humanlike);
    }

    [Fact]
    public void ALockedKeyInTheFileEndsAtThePackValue()
    {
        PresetConfig pack = Pack(v => v.rules.dropMode = DropMode.KeepOriginal, SettingNames.DropMode, "rules.weapons");
        SettingValues loaded = Presets.CodeProgression();
        loaded.rules.dropMode = DropMode.Off;
        loaded.rules.weapons.outcome = Outcome.Delete;
        loaded.deathDrop = DeathDrop.AllOnBody;

        SettingValues result = SettingsLoad.Resolve(loaded, false, Keys("dropMode", "weaponsOutcome", "deathDrop"), pack, out _);

        Assert.Equal(DropMode.KeepOriginal, result.rules.dropMode);
        Assert.Equal(Outcome.Keep, result.rules.weapons.outcome);
        Assert.Equal(DeathDrop.AllOnBody, result.deathDrop);
    }

    [Fact]
    public void AFileWithNoSavedKeyIsUnseededAndTakesTheDefaults()
    {
        PresetConfig pack = Pack(v => v.scrapMarketValue = 2f);
        SettingValues loaded = Presets.CodeProgression();
        loaded.deathDrop = DeathDrop.AllOnBody;

        Assert.False(SettingsLoad.IsSeeded(false, null));
        Assert.False(SettingsLoad.IsSeeded(false, Keys()));
        Assert.False(SettingsLoad.IsSeeded(false, Keys("someOtherModsKey")));
        SettingValues result = SettingsLoad.Resolve(loaded, false, Keys(), pack, out bool kept);
        Assert.False(kept);
        Assert.True(result.SameAs(Presets.Defaults(pack)));
    }

    [Fact]
    public void ActivePresetAloneOrFirstLoadAloneOrAnySettingKeyCountsAsSeeded()
    {
        Assert.True(SettingsLoad.IsSeeded(false, Keys("activePreset")));
        Assert.True(SettingsLoad.IsSeeded(false, Keys("firstLoadMessageShown")));
        Assert.True(SettingsLoad.IsSeeded(false, Keys("studyTechprintProjects")));
        Assert.True(SettingsLoad.IsSeeded(true, null));
    }

    [Fact]
    public void AFileHoldingOnlyActivePresetIsKeptAndFilledFromTheBaseline()
    {
        PresetConfig pack = Pack(v => v.scrapMarketValue = 2f);
        SettingValues result = SettingsLoad.Resolve(Presets.CodeProgression(), false, Keys("activePreset"), pack, out bool kept);
        Assert.True(kept);
        Assert.True(result.SameAs(Presets.Defaults(pack)));
    }

    [Fact]
    public void WithNoFileReadSinceTheLastLoadNothingIsFilledButLocksHold()
    {
        PresetConfig pack = Pack(null, SettingNames.DropMode);
        SettingValues current = Presets.Defaults(pack);
        current.rules.dropMode = DropMode.Off;
        current.deathDrop = DeathDrop.AllOnBody;

        SettingValues result = SettingsLoad.Resolve(current, true, null, pack, out bool kept);

        Assert.True(kept);
        Assert.Equal(DropMode.ScrapOutOfReach, result.rules.dropMode);
        Assert.Equal(DeathDrop.AllOnBody, result.deathDrop);
        Assert.Equal(DropMode.Off, current.rules.dropMode); // the input is not edited
    }

    [Fact]
    public void ACleanupKeyEqualToProgressionSurvivesTheRoundTrip()
    {
        // The save leaves out a value equal to the baseline, so the fill must give back that same value.
        PresetConfig pack = Pack(null);
        SettingValues cleanup = Presets.Build(PresetId.Cleanup, pack);
        cleanup.rules.dropMode = pack.Progression.rules.dropMode;
        SettingValues baseline = SettingsLoad.Baseline(pack);
        var written = new HashSet<string>(SettingKeys.All
            .Where(k => k.Path.StartsWith("rules.keep") || k.Path.StartsWith("rules.remove") || !LeafEqual(cleanup, baseline, k.Path))
            .Select(k => k.Key)) { "activePreset" };
        Assert.DoesNotContain("dropMode", written);

        SettingValues result = SettingsLoad.Resolve(cleanup, true, written, pack, out _);
        Assert.True(result.SameAs(cleanup));
    }

    [Fact]
    public void EverySettingKeyCanBeFilled()
    {
        SettingValues baseline = Presets.CodeProgression();
        SettingValues target = Presets.Build(PresetId.Cleanup, PresetConfig.Code());
        target.rules.keepList.Add("Gun_Revolver");
        foreach (SettingKey key in SettingKeys.All)
        {
            var allBut = new HashSet<string>(SettingKeys.All.Select(k => k.Key).Where(k => k != key.Key));
            SettingValues filled = target.Clone();
            SettingsLoad.FillMissing(filled, baseline, allBut);
            Assert.True(LeafEqual(filled, baseline, key.Path), key.Key);
        }
        SettingValues all = target.Clone();
        SettingsLoad.FillMissing(all, baseline, new HashSet<string>());
        Assert.True(all.SameAs(baseline));
        Assert.NotSame(baseline.rules.keepList, all.rules.keepList);
    }

    private static bool LeafEqual(SettingValues a, SettingValues b, string path)
    {
        var fa = ValueWalk.Resolve(a, path, out object oa);
        var fb = ValueWalk.Resolve(b, path, out object ob);
        return ValueWalk.Equal(fa.GetValue(oa), fb.GetValue(ob));
    }
}
