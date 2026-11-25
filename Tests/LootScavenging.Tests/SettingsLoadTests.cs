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
        loaded.corpses.humanlike = true;
        loaded.hostilesKeepGear = false;
        loaded.rules.apparel.outcome = Outcome.Delete; // not in the file: a stale value must be replaced

        SettingValues result = SettingsLoad.Resolve(loaded, false, Keys("activePreset", "corpseHumanlike", "hostilesKeepGear"), pack, out bool kept);

        Assert.True(kept);
        Assert.True(result.corpses.humanlike);
        Assert.False(result.hostilesKeepGear);
        SettingValues expected = Presets.Defaults(pack);
        expected.corpses.humanlike = true;
        expected.hostilesKeepGear = false;
        Assert.True(result.SameAs(expected));
    }

    [Fact]
    public void APatchedDefDefaultFillsAMissingKey()
    {
        PresetConfig pack = Pack(v =>
        {
            v.rules.whoHostile = WhoRule.Mark;
            v.SetScrapMarketValue(ScrapTier.Spacer, 2f);
            v.rules.keepList = new List<string> { "Gun_Revolver" };
        });
        SettingValues loaded = Presets.CodeProgression();
        loaded.hostilesKeepGear = false;

        SettingValues result = SettingsLoad.Resolve(loaded, false, Keys("hostilesKeepGear"), pack, out _);

        Assert.Equal(WhoRule.Mark, result.rules.whoHostile);
        Assert.Equal(2f, result.ScrapMarketValue(ScrapTier.Spacer));
        Assert.Equal(new List<string> { "Gun_Revolver" }, result.rules.keepList);
        Assert.False(result.hostilesKeepGear);
    }

    [Fact]
    public void AnUpgradedSeededConfigAlsoTakesThePatchedDefaultForAMissingKey()
    {
        PresetConfig pack = Pack(v => v.rules.whoHostile = WhoRule.Mark);
        SettingValues loaded = Presets.CodeProgression();
        loaded.corpses.humanlike = true;

        SettingValues result = SettingsLoad.Resolve(loaded, true, Keys("defaultsSeeded", "corpseHumanlike"), pack, out bool kept);

        Assert.True(kept);
        Assert.Equal(WhoRule.Mark, result.rules.whoHostile);
        Assert.True(result.corpses.humanlike);
    }

    [Fact]
    public void ALockedKeyInTheFileEndsAtThePackValue()
    {
        PresetConfig pack = Pack(v => v.rules.whoHostile = WhoRule.Mark, SettingNames.WhoHostile, "rules.weapons");
        SettingValues loaded = Presets.CodeProgression();
        loaded.rules.whoHostile = WhoRule.Exclude;
        loaded.rules.weapons.outcome = Outcome.Delete;
        loaded.hostilesKeepGear = false;

        SettingValues result = SettingsLoad.Resolve(loaded, false, Keys("whoHostile", "weaponsOutcome", "hostilesKeepGear"), pack, out _);

        Assert.Equal(WhoRule.Mark, result.rules.whoHostile);
        Assert.Equal(Outcome.Scrap, result.rules.weapons.outcome);
        Assert.False(result.hostilesKeepGear);
    }

    [Fact]
    public void AFileWithNoSavedKeyIsUnseededAndTakesTheDefaults()
    {
        PresetConfig pack = Pack(v => v.SetScrapMarketValue(ScrapTier.Industrial, 2f));
        SettingValues loaded = Presets.CodeProgression();
        loaded.hostilesKeepGear = false;

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
        PresetConfig pack = Pack(v => v.SetScrapMarketValue(ScrapTier.Industrial, 2f));
        SettingValues result = SettingsLoad.Resolve(Presets.CodeProgression(), false, Keys("activePreset"), pack, out bool kept);
        Assert.True(kept);
        Assert.True(result.SameAs(Presets.Defaults(pack)));
    }

    [Fact]
    public void WithNoFileReadSinceTheLastLoadNothingIsFilledButLocksHold()
    {
        PresetConfig pack = Pack(null, SettingNames.WhoHostile);
        SettingValues current = Presets.Defaults(pack);
        current.rules.whoHostile = WhoRule.Exclude;
        current.hostilesKeepGear = false;

        SettingValues result = SettingsLoad.Resolve(current, true, null, pack, out bool kept);

        Assert.True(kept);
        Assert.Equal(WhoRule.Apply, result.rules.whoHostile);
        Assert.False(result.hostilesKeepGear);
        Assert.Equal(WhoRule.Exclude, current.rules.whoHostile); // the input is not edited
    }

    [Fact]
    public void ACleanupKeyEqualToProgressionSurvivesTheRoundTrip()
    {
        // A file saved before 0.5.2 left out a value equal to the baseline, so the fill must give back that same value.
        PresetConfig pack = Pack(null);
        SettingValues cleanup = Presets.Build(PresetId.Cleanup, pack);
        cleanup.rules.whoNonHostile = pack.Progression.rules.whoNonHostile;
        SettingValues baseline = SettingsLoad.Baseline(pack);
        var written = new HashSet<string>(SettingKeys.All
            .Where(k => k.Path.StartsWith("rules.keep") || k.Path.StartsWith("rules.remove") || !LeafEqual(cleanup, baseline, k.Path))
            .Select(k => k.Key)) { "activePreset" };
        Assert.DoesNotContain("whoNonHostile", written);

        SettingValues result = SettingsLoad.Resolve(cleanup, true, written, pack, out _);
        Assert.True(result.SameAs(cleanup));
    }

    // Since ticket 18 the mod's own save writes every key, a value equal to the default too.
    private static HashSet<string> EveryKey() =>
        new HashSet<string>(SettingKeys.All.Select(k => k.Key)) { "activePreset", "defaultsSeeded" };

    [Fact]
    public void AChoiceEqualToTheOldDefaultStaysWhenThePackChangesTheDefault()
    {
        // A Cleanup player's animal corpse removal is off, as the pack's default was when he saved.
        PresetConfig before = Pack(v => v.corpses.animal = false);
        SettingValues saved = Presets.Build(PresetId.Cleanup, before);
        saved.corpses.animal = false;
        saved.rules.whoNonHostile = before.Progression.rules.whoNonHostile;

        PresetConfig after = Pack(v =>
        {
            v.corpses.animal = true;
            v.rules.whoNonHostile = WhoRule.Mark;
        });
        SettingValues result = SettingsLoad.Resolve(saved, true, EveryKey(), after, out bool kept);

        Assert.True(kept);
        Assert.False(result.corpses.animal);
        Assert.Equal(before.Progression.rules.whoNonHostile, result.rules.whoNonHostile);
        Assert.True(result.SameAs(saved));
    }

    [Fact]
    public void AnOlderFileThatLeftOutADefaultStillFollowsTheNewDefault()
    {
        // Files written before ticket 18, and partial files from other tools, keep filling a missing key from the baseline.
        PresetConfig after = Pack(v => v.corpses.animal = true);
        SettingValues loaded = Presets.CodeProgression();
        loaded.corpses.animal = false;
        var written = EveryKey();
        written.Remove("corpseAnimal");

        SettingValues result = SettingsLoad.Resolve(loaded, true, written, after, out _);

        Assert.True(result.corpses.animal);
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

    // Ticket 19: a file from before 0.5.2 (no scrapScale key) holds research points for units worth twice today's.
    [Fact]
    public void AnOldFilesCustomStudyPointsAreHalvedOnce()
    {
        SettingValues values = Presets.CodeProgression();
        values.studyPointsSpacer = 40f;
        values.studyPointsUltra = 0f;
        var present = Keys(SettingNames.StudyPoints(ScrapTier.Spacer), SettingNames.StudyPoints(ScrapTier.Ultra));

        Assert.True(SettingsLoad.HalveOldStudyPoints(values, present, new SettingLocks(new string[0])));

        Assert.Equal(20f, values.studyPointsSpacer);
        Assert.Equal(0f, values.studyPointsUltra);
    }

    [Fact]
    public void AFileWithNoStudyPointsAboveZeroReportsNothingHalved()
    {
        SettingValues values = Presets.CodeProgression();
        values.studyPointsSpacer = 0f;

        Assert.False(SettingsLoad.HalveOldStudyPoints(values, Keys(SettingNames.StudyPoints(ScrapTier.Spacer)), new SettingLocks(new string[0])));
        Assert.False(SettingsLoad.HalveOldStudyPoints(values, null, new SettingLocks(new string[0])));
        Assert.Equal(0f, values.studyPointsSpacer);
    }

    [Fact]
    public void ALockedStudyPointsValueIsNotHalved()
    {
        string spacer = SettingNames.StudyPoints(ScrapTier.Spacer);
        SettingValues values = Presets.CodeProgression();
        values.studyPointsSpacer = 40f;
        values.studyPointsIndustrial = 30f;

        SettingsLoad.HalveOldStudyPoints(values, Keys(spacer, SettingNames.StudyPoints(ScrapTier.Industrial)), new SettingLocks(new[] { spacer }));

        Assert.Equal(40f, values.studyPointsSpacer);
        Assert.Equal(15f, values.studyPointsIndustrial);
    }

    [Fact]
    public void AMissingStudyPointsKeyIsNotHalvedAndTakesTheNewDefault()
    {
        PresetConfig pack = Pack(null);
        SettingValues loaded = Presets.CodeProgression();
        loaded.studyPointsIndustrial = 30f;
        var present = Keys("activePreset", SettingNames.StudyPoints(ScrapTier.Industrial));

        SettingsLoad.HalveOldStudyPoints(loaded, present, pack.Locks);
        SettingValues result = SettingsLoad.Resolve(loaded, false, present, pack, out _);

        Assert.Equal(15f, result.studyPointsIndustrial);
        Assert.Equal(Presets.Defaults(pack).studyPointsSpacer, result.studyPointsSpacer);
        Assert.Equal(12.5f, result.studyPointsSpacer);
    }

    // ---- Ticket 25, decision 11: a settings file from 0.5.2 translated into the new settings. ----

    // Every old setting at its 0.5.2 default, as the mod's own 0.5.2 save wrote it.
    private static LegacySettings OldDefaults()
    {
        var legacy = new LegacySettings();
        legacy.Set(LegacySettings.Hostile, "True");
        legacy.Set(LegacySettings.NonHostileFaction, "True");
        legacy.Set(LegacySettings.NoFaction, "True");
        legacy.Set(LegacySettings.Prisoners, "True");
        legacy.Set(LegacySettings.PrisonerJoinGear, LegacySettings.FollowDropMode);
        legacy.Set(LegacySettings.OtherJoinGear, LegacySettings.MarkUnfamiliar);
        legacy.Set(LegacySettings.DropMode, LegacySettings.ScrapOutOfReach);
        legacy.Set(LegacySettings.DropIncludesMedicine, "False");
        legacy.Set(LegacySettings.DropIncludesAmmunition, "False");
        legacy.Set(LegacySettings.DeathDrop, LegacySettings.Ground);
        legacy.Set(LegacySettings.StudyPointsMultiplier, "1");
        legacy.Set(LegacySettings.ScrapMarketValue, "0.01");
        legacy.Set("eraFallback", "CarrierFaction");
        legacy.Set("corpseChance", "1");
        legacy.Set("sortWhenArrested", "True");
        return legacy;
    }

    // The keys a 0.5.2 save holds that 0.6.0 still reads: every category rule, the corpse toggles, the study settings and
    // the colonist box. The Who rows, the two new boxes and the per-tier market values are new, so it never holds them.
    private static HashSet<string> OldFileKeys()
    {
        var keys = new HashSet<string> { "activePreset", "defaultsSeeded" };
        foreach (SettingKey key in SettingKeys.All)
        {
            bool isNew = key.Path.StartsWith("rules.who") || key.Path == SettingNames.HostilesKeepGear
                || key.Path == SettingNames.NonHostilesKeepGear || key.Path.StartsWith("scrapMarketValue");
            if (!isNew)
                keys.Add(key.Key);
        }
        return keys;
    }

    // The values a 0.5.2 save at its defaults leaves after loading: every category rule was Keep in 0.5.2.
    private static SettingValues OldLoaded()
    {
        SettingValues loaded = Presets.CodeProgression();
        foreach (CategoryInfo category in ItemCategories.All)
            loaded.rules.SetRule(category, new CategoryRule(Outcome.Keep));
        return loaded;
    }

    private static SettingValues FromOldFile(System.Action<LegacySettings> change, PresetConfig pack = null,
        System.Action<SettingValues> loadedChange = null, HashSet<string> present = null)
    {
        LegacySettings legacy = OldDefaults();
        change?.Invoke(legacy);
        SettingValues loaded = OldLoaded();
        loadedChange?.Invoke(loaded);
        return SettingsLoad.Resolve(loaded, false, present ?? OldFileKeys(), pack ?? Pack(null), out _, legacy);
    }

    // Decision 13: an old value equal to its 0.5.2 default becomes the 0.6.0 default.
    [Fact]
    public void AnOldFileAtEveryDefaultLoadsAsTheNewDefaults()
    {
        PresetConfig pack = Pack(null);
        Assert.True(FromOldFile(null, pack).SameAs(Presets.Defaults(pack)));
    }

    [Fact]
    public void AnOldFileAtEveryDefaultTakesThePacksNewDefaults()
    {
        PresetConfig pack = Pack(v =>
        {
            v.rules.whoOtherJoiners = WhoRule.Exclude;
            v.rules.apparel = new CategoryRule(Outcome.Keep);
            v.SetScrapMarketValue(ScrapTier.Spacer, 3f);
            v.nonHostilesKeepGear = false;
        });
        Assert.True(FromOldFile(null, pack).SameAs(Presets.Defaults(pack)));
    }

    [Fact]
    public void AFileHoldingOnlyOldKeysCountsAsSaved()
    {
        var legacy = new LegacySettings();
        legacy.Set(LegacySettings.Hostile, "False");
        SettingValues result = SettingsLoad.Resolve(Presets.CodeProgression(), false, Keys(), Pack(null), out bool kept, legacy);
        Assert.True(kept);
        Assert.Equal(WhoRule.Exclude, result.rules.whoHostile);
    }

    [Theory]
    [InlineData(LegacySettings.Hostile, OwnerGroup.Hostile)]
    [InlineData(LegacySettings.NonHostileFaction, OwnerGroup.NonHostileFaction)]
    [InlineData(LegacySettings.NoFaction, OwnerGroup.NoFaction)]
    [InlineData(LegacySettings.Prisoners, OwnerGroup.Prisoner)]
    public void AGroupSwitchedOffBecomesAnExcludedRow(string key, OwnerGroup group)
    {
        SettingValues result = FromOldFile(l => l.Set(key, "False"));
        foreach (OwnerGroup other in LootRules.RowGroups)
            Assert.Equal(other == group ? WhoRule.Exclude : Presets.CodeProgression().rules.RowFor(other), result.rules.RowFor(other));
    }

    [Theory]
    [InlineData(LegacySettings.PrisonerJoinGear, LegacySettings.MarkUnfamiliar, OwnerGroup.RecruitedPrisoner, WhoRule.Mark)]
    [InlineData(LegacySettings.PrisonerJoinGear, LegacySettings.LeaveAlone, OwnerGroup.RecruitedPrisoner, WhoRule.Exclude)]
    [InlineData(LegacySettings.OtherJoinGear, LegacySettings.FollowDropMode, OwnerGroup.OtherJoiner, WhoRule.Apply)]
    [InlineData(LegacySettings.OtherJoinGear, LegacySettings.LeaveAlone, OwnerGroup.OtherJoiner, WhoRule.Exclude)]
    public void AChangedJoinerChoiceBecomesItsRow(string key, string old, OwnerGroup group, WhoRule expected)
    {
        Assert.Equal(expected, FromOldFile(l => l.Set(key, old)).rules.RowFor(group));
    }

    [Fact]
    public void ScrapEverythingScrapsEveryKeptCategoryButMedicineAndAmmunition()
    {
        SettingValues result = FromOldFile(l => l.Set(LegacySettings.DropMode, LegacySettings.ScrapEverything),
            loadedChange: v => v.rules.food = new CategoryRule(Outcome.Delete));
        foreach (ItemCategory category in new[] { ItemCategory.Weapon, ItemCategory.Apparel, ItemCategory.Drugs, ItemCategory.Other })
        {
            Assert.Equal(Outcome.Scrap, result.rules.RuleFor(category).outcome);
            Assert.False(result.rules.RuleFor(category).onlyBeyondReach);
        }
        Assert.Equal(Outcome.Delete, result.rules.food.outcome); // a rule of the player's own is not touched
        Assert.Equal(Outcome.Keep, result.rules.medicine.outcome);
        Assert.Equal(Outcome.Keep, result.rules.ammunition.outcome);
    }

    [Fact]
    public void ScrapEverythingScrapsMedicineAndAmmunitionWhenTheyWereIncluded()
    {
        SettingValues result = FromOldFile(l =>
        {
            l.Set(LegacySettings.DropMode, LegacySettings.ScrapEverything);
            l.Set(LegacySettings.DropIncludesMedicine, "True");
            l.Set(LegacySettings.DropIncludesAmmunition, "True");
        });
        Assert.Equal(Outcome.Scrap, result.rules.medicine.outcome);
        Assert.Equal(Outcome.Scrap, result.rules.ammunition.outcome);
    }

    [Fact]
    public void ScrapOutOfReachWithAmmunitionIncludedScrapsAmmunitionBeyondReach()
    {
        SettingValues result = FromOldFile(l => l.Set(LegacySettings.DropIncludesAmmunition, "True"));
        Assert.Equal(Outcome.Scrap, result.rules.ammunition.outcome);
        Assert.True(result.rules.ammunition.onlyBeyondReach);
        Assert.Equal(Outcome.Keep, result.rules.medicine.outcome);
        Assert.True(result.rules.weapons.onlyBeyondReach);
    }

    [Fact]
    public void KeepOriginalMarksEveryAppliedRowAndKeepsTheCategories()
    {
        SettingValues result = FromOldFile(l =>
        {
            l.Set(LegacySettings.DropMode, LegacySettings.KeepOriginal);
            l.Set(LegacySettings.Prisoners, "False");
        });
        Assert.Equal(WhoRule.Mark, result.rules.whoHostile);
        Assert.Equal(WhoRule.Mark, result.rules.whoNonHostile);
        Assert.Equal(WhoRule.Mark, result.rules.whoWild);
        Assert.Equal(WhoRule.Mark, result.rules.whoRecruitedPrisoners);
        Assert.Equal(WhoRule.Mark, result.rules.whoOtherJoiners);
        Assert.Equal(WhoRule.Exclude, result.rules.whoPrisoners);
        foreach (CategoryInfo category in ItemCategories.All)
            Assert.Equal(Outcome.Keep, category.Rule(result.rules).outcome);
    }

    [Fact]
    public void OffKeepsTheCategoriesAndTheRows()
    {
        SettingValues result = FromOldFile(l => l.Set(LegacySettings.DropMode, "Off"));
        foreach (CategoryInfo category in ItemCategories.All)
            Assert.Equal(Outcome.Keep, category.Rule(result.rules).outcome);
        Assert.Equal(WhoRule.Apply, result.rules.whoHostile);
    }

    // Decision 5: Degrade is cut, and the category it was set on loads as Keep in every drop mode.
    [Theory]
    [InlineData(LegacySettings.ScrapOutOfReach)]
    [InlineData(LegacySettings.ScrapEverything)]
    public void ADegradeOutcomeLoadsAsKeep(string mode)
    {
        CategoryInfo weapons = ItemCategories.Of(ItemCategory.Weapon);
        SettingValues result = FromOldFile(l =>
        {
            l.Set(LegacySettings.DropMode, mode);
            l.Set(LegacySettings.OutcomeKey(weapons), LegacySettings.Degrade);
        });
        Assert.Equal(Outcome.Keep, result.rules.weapons.outcome);
        Assert.False(result.rules.weapons.onlyBeyondReach);
        Assert.Equal(Outcome.Scrap, result.rules.apparel.outcome);
    }

    [Fact]
    public void ABodyDeathDropTicksBothKeepGearBoxes()
    {
        PresetConfig pack = Pack(v => { v.hostilesKeepGear = false; v.nonHostilesKeepGear = false; });
        SettingValues ground = FromOldFile(null, pack);
        Assert.False(ground.hostilesKeepGear);
        Assert.False(ground.nonHostilesKeepGear);
        SettingValues body = FromOldFile(l => l.Set(LegacySettings.DeathDrop, "AllOnBody"), pack);
        Assert.True(body.hostilesKeepGear);
        Assert.True(body.nonHostilesKeepGear);
    }

    [Fact]
    public void AnOldMultiplierIsMultipliedIntoEveryTierAndCapped()
    {
        SettingValues result = FromOldFile(l => l.Set(LegacySettings.StudyPointsMultiplier, "2"),
            loadedChange: v => v.studyPointsSpacer = 600f);
        SettingValues defaults = Presets.CodeProgression();
        Assert.Equal(SettingValues.MaxStudyPoints, result.studyPointsSpacer);
        Assert.Equal(defaults.studyPointsIndustrial * 2f, result.studyPointsIndustrial);
        Assert.Equal(defaults.studyPointsNeolithic * 2f, result.studyPointsNeolithic);
    }

    [Theory]
    [InlineData("0.5", 0.5f)]
    [InlineData("50", 10f)]
    [InlineData("0.001", 0.01f)]
    public void AnOldMarketValueGoesToEveryTierClamped(string old, float expected)
    {
        SettingValues result = FromOldFile(l => l.Set(LegacySettings.ScrapMarketValue, old));
        foreach (ScrapTier tier in SettingValues.Tiers)
            Assert.Equal(expected, result.ScrapMarketValue(tier));
    }

    [Fact]
    public void ATierMarketValueTheFileHoldsWinsOverTheOldValue()
    {
        var present = OldFileKeys();
        present.Add(SettingNames.ScrapMarketValue(ScrapTier.Spacer));
        SettingValues result = FromOldFile(l => l.Set(LegacySettings.ScrapMarketValue, "0.5"),
            loadedChange: v => v.SetScrapMarketValue(ScrapTier.Spacer, 3f), present: present);
        Assert.Equal(3f, result.ScrapMarketValue(ScrapTier.Spacer));
        Assert.Equal(0.5f, result.ScrapMarketValue(ScrapTier.Medieval));
    }

    [Fact]
    public void ARowTheFileHoldsWinsOverTheOldGroupSwitch()
    {
        var present = OldFileKeys();
        present.Add("whoHostile");
        SettingValues result = FromOldFile(l => l.Set(LegacySettings.Hostile, "False"),
            loadedChange: v => v.rules.whoHostile = WhoRule.Mark, present: present);
        Assert.Equal(WhoRule.Mark, result.rules.whoHostile);
    }

    // A file in the 0.6.0 format holds its own rows and category rules, so a stray old drop mode changes neither.
    [Fact]
    public void AFileInTheNewFormatIgnoresTheOldDropMode()
    {
        var legacy = new LegacySettings();
        legacy.Set(LegacySettings.DropMode, LegacySettings.KeepOriginal);
        SettingValues loaded = Presets.CodeProgression();
        SettingValues result = SettingsLoad.Resolve(loaded, true, EveryKey(), Pack(null), out _, legacy);
        Assert.True(result.SameAs(loaded));
    }

    [Fact]
    public void AnOldLockHoldsTheTranslatedSetting()
    {
        PresetConfig pack = Pack(null, "rules.hostile");
        SettingValues result = FromOldFile(l => l.Set(LegacySettings.Hostile, "False"), pack);
        Assert.Equal(WhoRule.Apply, result.rules.whoHostile);
    }

    // ---- Ticket 25, decisions 11 and 13: a pack's patch on the old nodes of the defaults Def. ----

    // The Def as the loader leaves it: the 0.6.0 defaults, and every old node at its 0.5.2 default.
    private static SettingValues OldDef()
    {
        SettingValues def = Presets.CodeProgression();
        def.rules.hostile = "true";
        def.rules.nonHostileFaction = "true";
        def.rules.noFaction = "true";
        def.rules.prisoners = "true";
        def.rules.prisonerJoinGear = LegacySettings.FollowDropMode;
        def.rules.otherJoinGear = LegacySettings.MarkUnfamiliar;
        def.rules.dropMode = LegacySettings.ScrapOutOfReach;
        def.rules.dropIncludesMedicine = "false";
        def.rules.dropIncludesAmmunition = "false";
        def.rules.sortWhenArrested = "true";
        def.rules.degradeMin = "0.2";
        def.corpses.chance = "1";
        def.deathDrop = LegacySettings.Ground;
        def.studyPointsMultiplier = "1";
        def.scrapMarketValue = "0.01";
        def.eraFallback = "CarrierFaction";
        return def;
    }

    [Fact]
    public void ADefWithEveryOldNodeAtItsDefaultIsUnchanged()
    {
        SettingValues def = OldDef();
        SettingsLoad.MigrateDef(def);
        Assert.True(def.SameAs(Presets.CodeProgression()));
        Assert.Null(def.rules.dropMode);
        Assert.Null(def.corpses.chance);
        Assert.Null(def.scrapMarketValue);
    }

    [Fact]
    public void APatchedOldDefNodeIsTranslated()
    {
        SettingValues def = OldDef();
        def.rules.hostile = "false";
        def.rules.otherJoinGear = LegacySettings.LeaveAlone;
        def.scrapMarketValue = "0.5";
        def.studyPointsMultiplier = "2";

        SettingsLoad.MigrateDef(def);

        SettingValues code = Presets.CodeProgression();
        Assert.Equal(WhoRule.Exclude, def.rules.whoHostile);
        Assert.Equal(WhoRule.Exclude, def.rules.whoOtherJoiners);
        Assert.Equal(WhoRule.Apply, def.rules.whoNonHostile);
        Assert.Equal(0.5f, def.ScrapMarketValue(ScrapTier.Industrial));
        Assert.Equal(code.studyPointsSpacer * 2f, def.studyPointsSpacer);
        Assert.True(def.rules.weapons.onlyBeyondReach); // the default drop mode keeps the new category defaults
    }

    // An old pack never set the new category rules: under a drop mode it changed, they were the Keep rules of its day.
    [Fact]
    public void APatchedOldDefDropModeTranslatesTheCategories()
    {
        SettingValues def = OldDef();
        def.rules.dropMode = LegacySettings.ScrapEverything;
        def.rules.drugs = new CategoryRule(Outcome.Delete); // a category the pack patched itself

        SettingsLoad.MigrateDef(def);

        Assert.Equal(Outcome.Scrap, def.rules.weapons.outcome);
        Assert.False(def.rules.weapons.onlyBeyondReach);
        Assert.Equal(Outcome.Scrap, def.rules.food.outcome);
        Assert.Equal(Outcome.Delete, def.rules.drugs.outcome);
        Assert.Equal(Outcome.Keep, def.rules.medicine.outcome);
    }

    [Fact]
    public void AnOldDefKeepOriginalMarksTheRows()
    {
        SettingValues def = OldDef();
        def.rules.dropMode = LegacySettings.KeepOriginal;

        SettingsLoad.MigrateDef(def);

        Assert.Equal(WhoRule.Mark, def.rules.whoHostile);
        Assert.Equal(WhoRule.Mark, def.rules.whoOtherJoiners);
        Assert.Equal(Outcome.Keep, def.rules.weapons.outcome);
    }

    private static bool LeafEqual(SettingValues a, SettingValues b, string path)
    {
        var fa = ValueWalk.Resolve(a, path, out object oa);
        var fb = ValueWalk.Resolve(b, path, out object ob);
        return ValueWalk.Equal(fa.GetValue(oa), fb.GetValue(ob));
    }
}
