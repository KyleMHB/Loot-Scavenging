using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

namespace LootScavenging.Model;

/// <summary>
/// What a loaded settings file becomes once the defaults Def is read (auto-config spec, "Behaviour"). The file may be the
/// mod's own save or one another tool wrote, such as an Auto Mod Config preset, holding only some keys and no seeded flag.
/// The order is seed, fill, locks: a file holding any saved key is kept; every setting key it lacks takes the baseline;
/// locked settings then return to the pack's value.
/// </summary>
public static class SettingsLoad
{
    public const string ActivePresetKey = "activePreset"; // not translated
    // Saved by earlier versions for a first-load message since removed. Still read, so a file holding it counts as seeded.
    public const string FirstLoadKey = "firstLoadMessageShown"; // not translated
    public const string SeededKey = "defaultsSeeded"; // not translated

    // The scale of the scrap the file's research points were set for (ticket 19). 0.5.2 made scrap cheaper, two units for one,
    // so a file without the key holds points per unit worth twice what they are now.
    public const string ScrapScaleKey = "scrapScale"; // not translated
    public const string StudyPointsHalvedKey = "studyPointsHalvedNote"; // not translated
    public const int OldScrapScale = 1;
    public const int CurrentScrapScale = 2;

    /// <summary>
    /// Halves, once, each tier's research points a file from before 0.5.2 held, so a unit of the cheaper scrap pays half and the
    /// item it came from pays the same. Only keys in <paramref name="presentKeys"/> are the player's: a missing one takes the new
    /// default later, in <see cref="FillMissing"/>. A locked value is the pack's and is left alone. An old multiplier is
    /// multiplied in later, by <see cref="Resolve"/>. Returns whether any value above 0 was halved, for the page's note.
    /// </summary>
    public static bool HalveOldStudyPoints(SettingValues values, ICollection<string> presentKeys, SettingLocks locks)
    {
        if (presentKeys == null)
            return false;
        bool halved = false;
        foreach (ScrapTier tier in SettingValues.Tiers)
        {
            string name = SettingNames.StudyPoints(tier);
            if (!presentKeys.Contains(name) || locks.IsLocked(name))
                continue;
            float points = values.StudyPoints(tier);
            if (!(points > 0f))
                continue;
            values.SetStudyPoints(tier, points / 2f);
            halved = true;
        }
        return halved;
    }

    /// <summary>
    /// The value a missing key is filled with. The mod's own save writes every key, a value equal to the default too (ticket
    /// 18), so a later change to this baseline never moves a saved choice. Only a file written before 0.5.2, which left out a
    /// value equal to the baseline of its day, or one another tool wrote, lacks keys.
    /// </summary>
    public static SettingValues Baseline(PresetConfig config) => Presets.Defaults(config);

    /// <summary>
    /// Whether the config keeps its values: the flag is set, or the file held a setting key, <see cref="ActivePresetKey"/> or
    /// <see cref="FirstLoadKey"/>. <paramref name="presentKeys"/> is null when no file was read.
    /// </summary>
    public static bool IsSeeded(bool seededFlag, ICollection<string> presentKeys)
    {
        if (seededFlag)
            return true;
        if (presentKeys == null)
            return false;
        if (presentKeys.Contains(ActivePresetKey) || presentKeys.Contains(FirstLoadKey))
            return true;
        foreach (SettingKey key in SettingKeys.All)
        {
            if (presentKeys.Contains(key.Key))
                return true;
        }
        return false;
    }

    /// <summary>Sets every setting whose key is not in <paramref name="presentKeys"/> to a copy of the baseline's value.</summary>
    public static void FillMissing(SettingValues target, SettingValues baseline, ICollection<string> presentKeys)
    {
        foreach (SettingKey key in SettingKeys.All)
        {
            if (!presentKeys.Contains(key.Key))
                ValueWalk.CopyPath(target, baseline, key.Path);
        }
    }

    /// <summary>
    /// The values the loaded config ends with. <paramref name="kept"/> is false when the config was not seeded, so it takes
    /// the baseline and the caller resets the active preset to Progression. With <paramref name="presentKeys"/> null (no file
    /// read since the last call) nothing is filled. <paramref name="legacy"/> holds the settings from before 0.6.0 the file
    /// held (ticket 25); they are translated into the new settings once the missing keys are filled. <paramref name="loaded"/>
    /// is not changed.
    /// </summary>
    public static SettingValues Resolve(SettingValues loaded, bool seededFlag, ICollection<string> presentKeys, PresetConfig config,
        out bool kept, LegacySettings legacy = null)
    {
        bool hasLegacy = legacy != null && legacy.Any && presentKeys != null;
        kept = IsSeeded(seededFlag, presentKeys) || hasLegacy;
        SettingValues baseline = Baseline(config);
        if (!kept)
            return baseline;
        SettingValues result = loaded.Clone();
        if (presentKeys != null)
        {
            var present = new HashSet<string>(presentKeys);
            if (hasLegacy)
                FillOldCategoryDefaults(result, present);
            FillMissing(result, baseline, present);
            if (hasLegacy)
                Translate(result, legacy, present, baseline);
        }
        config.Locks.Enforce(result, config.Progression);
        return result;
    }

    /// <summary>
    /// Translates the settings a pack's patch set on the old nodes of the defaults Def into the new ones (ticket 25), then
    /// clears them. Only a value that differs from its 0.5.2 default is translated (decision 13), since the Def still carries
    /// every old node at that default for old patches to find.
    /// </summary>
    public static void MigrateDef(SettingValues def, IEnumerable<string> locked = null)
    {
        if (def?.rules == null || def.corpses == null)
            return;
        LegacySettings legacy = LegacySettings.TakeFromDef(def);
        if (legacy.Any)
            Translate(def, legacy, null, null);
        if (locked != null)
            LockOldDropAsSet(def, legacy, new HashSet<string>(TrimAll(locked)));
    }

    // A lock on an old drop node now locks both keep-gear boxes (SettingLocks), which take the Def's value. Unlocked, a value
    // at its 0.5.2 default becomes the 0.6.0 default, ticked (decision 13). Locked, it was the pack's choice, so the boxes
    // take what it meant: the death drop at Ground (the 0.5.2 default) dropped everything as in the base game, and the
    // unshipped downed-gear Drop the weapon dropped the weapon. Either one unticks them. A box the pack locks by its own
    // name keeps the Def's value.
    private static void LockOldDropAsSet(SettingValues def, LegacySettings legacy, HashSet<string> locked)
    {
        bool drops = locked.Contains(LegacySettings.DeathDrop)
            && (legacy.Text(LegacySettings.DeathDrop) ?? LegacySettings.Ground) == LegacySettings.Ground;
        drops |= locked.Contains(LegacySettings.DownedGearLock)
            && legacy.Text(LegacySettings.DownedGear) == LegacySettings.DropWeapon;
        if (!drops)
            return;
        if (!locked.Contains(SettingNames.HostilesKeepGear))
            def.hostilesKeepGear = false;
        if (!locked.Contains(SettingNames.NonHostilesKeepGear))
            def.nonHostilesKeepGear = false;
    }

    private static IEnumerable<string> TrimAll(IEnumerable<string> names)
    {
        foreach (string name in names)
        {
            if (name != null)
                yield return name.Trim();
        }
    }

    // Before 0.5.2 a file left out a value equal to the default of its day, and every category rule then defaulted to Keep.
    // A missing category field of an old file is that Keep, not the new default, so the drop mode step below sees it.
    private static void FillOldCategoryDefaults(SettingValues values, HashSet<string> present)
    {
        var keep = new LootRules();
        foreach (CategoryInfo category in ItemCategories.All)
            keep.SetRule(category, new CategoryRule(Outcome.Keep));
        var source = new SettingValues { rules = keep };
        foreach (SettingKey key in SettingKeys.All)
        {
            if (!key.Path.StartsWith("rules.") || key.Path.Split('.').Length != 3 || present.Contains(key.Key)) // not translated
                continue;
            ValueWalk.CopyPath(values, source, key.Path);
            present.Add(key.Key);
        }
    }

    // The decision 11 table, with decision 13: a value equal to its 0.5.2 default becomes the 0.6.0 default, which is the
    // baseline the missing keys were filled with; only a value the player changed is translated. A new key the file holds
    // itself wins over the old key. With <paramref name="baseline"/> null the values are the Def itself, which is the baseline.
    private static void Translate(SettingValues v, LegacySettings old, ICollection<string> present, SettingValues baseline)
    {
        bool fromFile = baseline != null;
        bool Has(string path) => present != null && present.Contains(SettingKeys.KeyFor(path));
        // A file holding any setting only 0.6.0 writes is in the new format: its rows and category rules are its own.
        bool newFormat = present != null && HoldsNewOnlyKey(present);

        LootRules r = v.rules;
        TranslateGroup(r, OwnerGroup.Hostile, old.Bool(LegacySettings.Hostile), Has);
        TranslateGroup(r, OwnerGroup.NonHostileFaction, old.Bool(LegacySettings.NonHostileFaction), Has);
        TranslateGroup(r, OwnerGroup.NoFaction, old.Bool(LegacySettings.NoFaction), Has);
        TranslateGroup(r, OwnerGroup.Prisoner, old.Bool(LegacySettings.Prisoners), Has);
        TranslateJoiner(r, OwnerGroup.RecruitedPrisoner, old.Text(LegacySettings.PrisonerJoinGear), LegacySettings.FollowDropMode, Has);
        TranslateJoiner(r, OwnerGroup.OtherJoiner, old.Text(LegacySettings.OtherJoinGear), LegacySettings.MarkUnfamiliar, Has);

        string mode = old.Text(LegacySettings.DropMode) ?? LegacySettings.ScrapOutOfReach;
        bool defaultMode = mode == LegacySettings.ScrapOutOfReach;
        if (!newFormat)
        {
            var codeDefaults = new LootRules();
            foreach (CategoryInfo category in ItemCategories.All)
            {
                // Degrade is cut (decision 5): Vanilla in every drop mode, never scrapped by it. The file's node was read as Keep.
                if (old.Text(LegacySettings.OutcomeKey(category)) == LegacySettings.Degrade)
                {
                    r.SetRule(category, new CategoryRule(Outcome.Keep));
                    continue;
                }
                CategoryRule rule = category.Rule(r);
                // The Def's categories hold the 0.6.0 defaults, which an old pack never set: under a drop mode it changed, a
                // category still at that default was a Keep rule of its day.
                if (!fromFile && !defaultMode && ValueWalk.Equal(rule, category.Rule(codeDefaults)))
                {
                    rule = new CategoryRule(Outcome.Keep);
                    r.SetRule(category, rule);
                }
                if (rule.outcome != Outcome.Keep)
                    continue;
                bool included = category.Category switch
                {
                    ItemCategory.Medicine => old.Bool(LegacySettings.DropIncludesMedicine) == true,
                    ItemCategory.Ammunition => old.Bool(LegacySettings.DropIncludesAmmunition) == true,
                    _ => true,
                };
                bool optional = category.Category == ItemCategory.Medicine || category.Category == ItemCategory.Ammunition;
                if (mode == LegacySettings.ScrapEverything)
                {
                    if (included)
                        r.SetRule(category, new CategoryRule(Outcome.Scrap));
                }
                else if (defaultMode)
                {
                    if (optional && included)
                        r.SetRule(category, CategoryRule.ScrapBeyondReach());
                    else if (fromFile)
                        r.SetRule(category, ValueWalk.Copy(category.Rule(baseline.rules)));
                }
            }
            // Keep original kept every covered group's gear whole and marked; the joiners that followed the drop mode did too.
            if (mode == LegacySettings.KeepOriginal)
            {
                foreach (OwnerGroup group in LootRules.RowGroups)
                {
                    if (r.RowFor(group) == WhoRule.Apply && !Has(SettingNames.Row(group)))
                        r.SetRow(group, WhoRule.Mark);
                }
            }
        }

        // Ground was the 0.5.2 default and becomes the new default (ticked); the two body drops were ticked choices.
        string deathDrop = old.Text(LegacySettings.DeathDrop);
        if (deathDrop != null && deathDrop != LegacySettings.Ground)
        {
            if (!Has(SettingNames.HostilesKeepGear))
                v.hostilesKeepGear = true;
            if (!Has(SettingNames.NonHostilesKeepGear))
                v.nonHostilesKeepGear = true;
        }

        float? multiplier = old.Float(LegacySettings.StudyPointsMultiplier);
        if (multiplier is float m && m >= 0f && !float.IsInfinity(m) && Math.Abs(m - 1f) > 0.0001f)
        {
            foreach (ScrapTier tier in SettingValues.Tiers)
                v.SetStudyPoints(tier, Math.Min(SettingValues.MaxStudyPoints, v.StudyPoints(tier) * m));
        }

        float? market = old.Float(LegacySettings.ScrapMarketValue);
        if (market is float value && !float.IsInfinity(value) && Math.Abs(value - SettingValues.DefaultScrapMarketValue) > 0.0001f)
        {
            float clamped = Math.Min(SettingValues.MaxScrapMarketValue, Math.Max(SettingValues.MinScrapMarketValue, value));
            foreach (ScrapTier tier in SettingValues.Tiers)
            {
                if (!Has(SettingNames.ScrapMarketValue(tier)))
                    v.SetScrapMarketValue(tier, clamped);
            }
        }
    }

    // Off excludes the row. On was the 0.5.2 default: the row keeps the 0.6.0 default.
    private static void TranslateGroup(LootRules r, OwnerGroup group, bool? old, Func<string, bool> has)
    {
        if (old == false && !has(SettingNames.Row(group)))
            r.SetRow(group, WhoRule.Exclude);
    }

    private static void TranslateJoiner(LootRules r, OwnerGroup group, string old, string oldDefault, Func<string, bool> has)
    {
        if (old == null || old == oldDefault || has(SettingNames.Row(group)))
            return;
        if (old == LegacySettings.FollowDropMode)
            r.SetRow(group, WhoRule.Apply);
        else if (old == LegacySettings.MarkUnfamiliar)
            r.SetRow(group, WhoRule.Mark);
        else if (old == LegacySettings.LeaveAlone)
            r.SetRow(group, WhoRule.Exclude);
    }

    private static bool HoldsNewOnlyKey(ICollection<string> present)
    {
        foreach (OwnerGroup group in LootRules.RowGroups)
        {
            if (present.Contains(SettingKeys.KeyFor(SettingNames.Row(group))))
                return true;
        }
        return present.Contains(SettingKeys.KeyFor(SettingNames.HostilesKeepGear))
            || present.Contains(SettingKeys.KeyFor(SettingNames.NonHostilesKeepGear));
    }
}

/// <summary>
/// The settings from before 0.6.0 (ticket 25) as the raw text of their saved keys, read once from a settings file or the
/// defaults Def and translated by <see cref="SettingsLoad"/>. None of them is written again.
/// </summary>
public sealed class LegacySettings
{
    public const string Hostile = "hostile"; // not translated
    public const string NonHostileFaction = "nonHostileFaction"; // not translated
    public const string NoFaction = "noFaction"; // not translated
    public const string Prisoners = "prisoners"; // not translated
    public const string PrisonerJoinGear = "prisonerJoinGear"; // not translated
    public const string OtherJoinGear = "otherJoinGear"; // not translated
    public const string DropMode = "dropMode"; // not translated
    public const string DropIncludesMedicine = "dropIncludesMedicine"; // not translated
    public const string DropIncludesAmmunition = "dropIncludesAmmunition"; // not translated
    public const string DeathDrop = "deathDrop"; // not translated
    public const string StudyPointsMultiplier = "studyPointsMultiplier"; // not translated
    public const string ScrapMarketValue = "scrapMarketValue"; // not translated
    // Never shipped and not translated, but a pack may lock it under this name (SettingLocks).
    public const string DownedGear = "downedGear"; // not translated
    public const string DownedGearLock = "rules.downedGear"; // not translated

    // Old values the translation compares against.
    public const string ScrapOutOfReach = "ScrapOutOfReach"; // not translated
    public const string ScrapEverything = "ScrapEverything"; // not translated
    public const string KeepOriginal = "KeepOriginal"; // not translated
    public const string FollowDropMode = "FollowDropMode"; // not translated
    public const string MarkUnfamiliar = "MarkUnfamiliar"; // not translated
    public const string LeaveAlone = "LeaveAlone"; // not translated
    public const string Ground = "Ground"; // not translated
    public const string DropWeapon = "DropWeapon"; // not translated
    public const string Degrade = "Degrade"; // not translated

    /// <summary>
    /// Every old saved key, including those cut without a translation: a file holding only these still counts as saved.
    /// The cut ones are read so the file keeps its values, and dropped.
    /// </summary>
    public static readonly string[] Keys =
    {
        Hostile, NonHostileFaction, NoFaction, Prisoners, PrisonerJoinGear, OtherJoinGear, DropMode, DropIncludesMedicine,
        DropIncludesAmmunition, DeathDrop, StudyPointsMultiplier, ScrapMarketValue,
        "eraFallback", "studyEras", "ejectCharges", "degradeMin", "degradeMax", "degradeLowersQuality", "corpseChance", // not translated
        "butcherRemovesApparel", DownedGear, "sortWhenArrested", "sortWhenStripped", "onDownedAndStrip", // not translated
    };

    /// <summary>The saved key of a category's outcome, which may hold the cut Degrade.</summary>
    public static string OutcomeKey(CategoryInfo category) => SettingKeys.KeyFor("rules." + category.Field + ".outcome"); // not translated

    private readonly Dictionary<string, string> texts = new Dictionary<string, string>();

    public bool Any => texts.Count > 0;

    public void Set(string key, string text)
    {
        if (key != null && text != null)
            texts[key] = text.Trim();
    }

    public string Text(string key) => texts.TryGetValue(key, out string text) ? text : null;

    public bool? Bool(string key) => bool.TryParse(Text(key), out bool value) ? value : (bool?)null;

    public float? Float(string key) =>
        float.TryParse(Text(key), NumberStyles.Float, CultureInfo.InvariantCulture, out float value) && !float.IsNaN(value)
            ? value
            : (float?)null;

    // The old nodes of the defaults Def, read into the non-public fields by the Def loader. Each is cleared once read.
    internal static LegacySettings TakeFromDef(SettingValues def)
    {
        var legacy = new LegacySettings();
        Take(def, "", legacy);
        Take(def.rules, "", legacy);
        Take(def.corpses, "corpse", legacy);
        return legacy;
    }

    private static void Take(object owner, string prefix, LegacySettings into)
    {
        foreach (FieldInfo field in owner.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
        {
            if (field.FieldType != typeof(string) || field.Name.Contains("<"))
                continue;
            string key = prefix.Length == 0 || field.Name == "butcherRemovesApparel" // not translated
                ? field.Name
                : prefix + char.ToUpperInvariant(field.Name[0]) + field.Name.Substring(1);
            into.Set(key, (string)field.GetValue(owner));
            field.SetValue(owner, null);
        }
    }
}
