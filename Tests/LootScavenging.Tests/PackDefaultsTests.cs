using System.Collections.Generic;
using System.Linq;
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
        PresetConfig pack = Pack(v => v.rules.whoHostile = WhoRule.Mark);
        Assert.Equal(WhoRule.Mark, Presets.Defaults(pack).rules.whoHostile);
        Assert.Equal(WhoRule.Mark, Presets.Build(PresetId.Progression, pack).rules.whoHostile);
    }

    [Fact]
    public void AFreshConfigWithPatchedDefaultsIsNotMarkedChanged()
    {
        PresetConfig pack = Pack(v =>
        {
            v.rules.whoHostile = WhoRule.Mark;
            v.SetScrapMarketValue(ScrapTier.Industrial, 2f);
        });
        SettingValues fresh = Presets.Defaults(pack);
        Assert.False(Presets.IsChanged(PresetId.Progression, fresh, pack));
    }

    [Fact]
    public void APatchedDefaultCarriesIntoCleanupAndVanillaOnlyWhereThePresetIsSilent()
    {
        PresetConfig pack = Pack(v =>
        {
            v.SetScrapMarketValue(ScrapTier.Spacer, 2f);
            v.corpses.humanlike = true; // Vanilla must still leave corpses alone
            v.rules.drugs = new CategoryRule(Outcome.Delete);
        });
        SettingValues cleanup = Presets.Build(PresetId.Cleanup, pack);
        SettingValues vanilla = Presets.Build(PresetId.Vanilla, pack);
        Assert.Equal(2f, cleanup.ScrapMarketValue(ScrapTier.Spacer));
        Assert.Equal(2f, vanilla.ScrapMarketValue(ScrapTier.Spacer));
        Assert.False(vanilla.corpses.humanlike);
        Assert.Equal(Outcome.Keep, vanilla.rules.drugs.outcome);
        Assert.Equal(Outcome.Keep, cleanup.rules.drugs.outcome);
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
            Assert.Equal(Outcome.Scrap, applied.rules.weapons.outcome); // Cleanup would delete poor weapons
            Assert.False(applied.corpses.humanlike); // Cleanup would remove corpses
            Assert.False(Presets.IsChanged(preset, applied, pack));
        }
    }

    [Fact]
    public void EnforcingPutsALockedValueBackAndLeavesOthersAlone()
    {
        PresetConfig pack = Pack(null, "rules.whoHostile", "hostilesKeepGear", "rules.weapons");
        SettingValues edited = Presets.Defaults(pack);
        edited.rules.whoHostile = WhoRule.Exclude;
        edited.hostilesKeepGear = false;
        edited.rules.weapons.outcome = Outcome.Delete;
        edited.rules.apparel.outcome = Outcome.Delete;
        pack.Locks.Enforce(edited, pack.Progression);
        Assert.Equal(WhoRule.Apply, edited.rules.whoHostile);
        Assert.True(edited.hostilesKeepGear);
        Assert.Equal(Outcome.Scrap, edited.rules.weapons.outcome);
        Assert.Equal(Outcome.Delete, edited.rules.apparel.outcome);
        Assert.NotSame(pack.Progression.rules.weapons, edited.rules.weapons);
    }

    [Fact]
    public void AnUnlockedPresetChangeStillCountsAsChanged()
    {
        PresetConfig pack = Pack(null, "rules.whoHostile");
        SettingValues current = Presets.Defaults(pack);
        current.rules.apparel.outcome = Outcome.Delete;
        Assert.True(Presets.IsChanged(PresetId.Progression, current, pack));
    }

    [Fact]
    public void UnknownLockNamesAreIgnoredAndReported()
    {
        var locks = new SettingLocks(new[] { "rules.whoHostile", "rules.noSuchSetting", "  ", null, "rules.keepList" });
        Assert.True(locks.IsLocked("rules.whoHostile"));
        Assert.False(locks.IsLocked("rules.noSuchSetting"));
        Assert.Equal(new List<string> { "rules.noSuchSetting", "rules.keepList" }, locks.Unknown);
    }

    [Fact]
    public void AnOldLockNameLocksTheSettingsThatReplacedIt()
    {
        var locks = new SettingLocks(new[] { "rules.hostile", "rules.otherJoinGear", "deathDrop", "scrapMarketValue", "studyPointsMultiplier", "rules.dropIncludesMedicine" });
        Assert.True(locks.IsLocked(SettingNames.WhoHostile));
        Assert.False(locks.IsLocked(SettingNames.WhoNonHostile));
        Assert.True(locks.IsLocked(SettingNames.WhoOtherJoiners));
        Assert.True(locks.IsLocked(SettingNames.HostilesKeepGear));
        Assert.True(locks.IsLocked(SettingNames.NonHostilesKeepGear));
        Assert.False(locks.IsLocked(SettingNames.ColonistsKeepGear));
        Assert.True(locks.IsLocked("rules.medicine"));
        foreach (ScrapTier tier in SettingValues.Tiers)
        {
            Assert.True(locks.IsLocked(SettingNames.ScrapMarketValue(tier)));
            Assert.True(locks.IsLocked(SettingNames.StudyPoints(tier)));
        }
        Assert.Empty(locks.Unknown);
    }

    [Fact]
    public void TheOldDropModeLockLocksEveryRowAndCategory()
    {
        var locks = new SettingLocks(new[] { "rules.dropMode" });
        foreach (OwnerGroup group in LootRules.RowGroups)
            Assert.True(locks.IsLocked(SettingNames.Row(group)));
        foreach (CategoryInfo category in ItemCategories.All)
            Assert.True(locks.IsLocked(SettingNames.Category(category)));
    }

    [Theory]
    [InlineData("rules.sortWhenArrested")]
    [InlineData("rules.sortWhenStripped")]
    [InlineData("rules.onDownedAndStrip")]
    [InlineData("rules.degradeMin")]
    [InlineData("rules.degradeMax")]
    [InlineData("rules.degradeLowersQuality")]
    [InlineData("rules.ejectCharges")]
    [InlineData("corpses.chance")]
    [InlineData("corpses.butcherRemovesApparel")]
    [InlineData("eraFallback")]
    [InlineData("studyEras")]
    public void ACutLockNameIsAcceptedAndLocksNothing(string name)
    {
        var locks = new SettingLocks(new[] { name });
        Assert.False(locks.Any);
        Assert.Empty(locks.Unknown);
    }

    [Fact]
    public void EveryLockableNameIsARealSettingField()
    {
        var values = new SettingValues();
        foreach (string name in SettingLocks.Lockable)
            Assert.True(ValueWalk.PathExists(values, name), name);
    }

    // A pack's <locked> list names these, so the set must not change when the table is rebuilt.
    [Fact]
    public void TheLockableNamesAreTheOnesPacksUse()
    {
        string[] expected =
        {
            "rules.whoHostile", "rules.whoNonHostile", "rules.whoWild", "rules.whoPrisoners", "rules.whoRecruitedPrisoners",
            "rules.whoOtherJoiners", "rules.homeMapOnly", "colonistsKeepGear", "hostilesKeepGear", "nonHostilesKeepGear",
            "cremationStripsCorpses",
            "rules.weapons", "rules.apparel", "rules.ammunition", "rules.drugs", "rules.medicine", "rules.food", "rules.other",
            "rules.disassemblyYield", "rules.disassemblyEfficiency",
            "corpses.humanlike", "corpses.animal", "corpses.mechanoid",
            "unfamiliarScope", "recycleOrStudy", "studyTechprintProjects",
            "studyPointsNeolithic", "studyPointsMedieval", "studyPointsIndustrial", "studyPointsSpacer", "studyPointsUltra",
            "studyPointsArchotech",
            "scrapMarketValueNeolithic", "scrapMarketValueMedieval", "scrapMarketValueIndustrial", "scrapMarketValueSpacer",
            "scrapMarketValueUltra", "scrapMarketValueArchotech",
        };
        Assert.Equal(expected.OrderBy(n => n), SettingLocks.Lockable.OrderBy(n => n));
    }

    [Fact]
    public void LocksAreOutsideTheSettingsSnapshot()
    {
        // SettingValues only copies and compares fields it supports; the lock set must never become one of them.
        SettingValues v = Presets.Defaults(Pack(null, "rules.whoHostile"));
        Assert.True(v.Clone().SameAs(v));
    }
}
