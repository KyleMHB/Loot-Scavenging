using System.Collections.Generic;

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
    /// default later, in <see cref="FillMissing"/>. A locked value is the pack's and is left alone. The multiplier is not
    /// touched, or the points would be halved twice. Returns whether any value above 0 was halved, for the page's note.
    /// </summary>
    public static bool HalveOldStudyPoints(SettingValues values, ICollection<string> presentKeys, SettingLocks locks)
    {
        if (presentKeys == null)
            return false;
        bool halved = false;
        foreach (ScrapTier tier in (ScrapTier[])System.Enum.GetValues(typeof(ScrapTier)))
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
    /// read since the last call) nothing is filled. <paramref name="loaded"/> is not changed.
    /// </summary>
    public static SettingValues Resolve(SettingValues loaded, bool seededFlag, ICollection<string> presentKeys, PresetConfig config, out bool kept)
    {
        kept = IsSeeded(seededFlag, presentKeys);
        SettingValues baseline = Baseline(config);
        if (!kept)
            return baseline;
        SettingValues result = loaded.Clone();
        if (presentKeys != null)
            FillMissing(result, baseline, presentKeys);
        config.Locks.Enforce(result, config.Progression);
        return result;
    }
}
