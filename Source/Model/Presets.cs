using System.Collections.Generic;

namespace LootScavenging.Model;

/// <summary>The presets on the settings page (spec, "Settings page", presets table). Saved by name, so keep the names.</summary>
public enum PresetId
{
    Progression,
    Cleanup,
    Vanilla,
}

/// <summary>
/// The presets. Progression is the pack's patchable defaults (<see cref="PresetConfig.Progression"/>, read from the settings
/// defaults Def at startup); Cleanup and Vanilla start from it and set the rows the spec's presets table names. Each preset
/// sets every setting; the keep and remove lists are the player's own picks, so applying a preset leaves them as they are
/// and the "changed" comparison ignores them. Locked settings always hold the Progression value, whichever preset is applied.
/// Every method also takes a <see cref="PresetConfig"/> so tests need no shared state; without one it uses <see cref="Config"/>.
/// </summary>
public static class Presets
{
    /// <summary>What the presets are built from now. The mod replaces it once the Def is loaded.</summary>
    public static PresetConfig Config { get; private set; } = PresetConfig.Code();

    public static void Configure(PresetConfig config)
    {
        Config = config ?? PresetConfig.Code();
    }

    /// <summary>The Progression preset with its default lists.</summary>
    public static SettingValues Defaults() => Defaults(Config);

    public static SettingValues Defaults(PresetConfig config) => Build(PresetId.Progression, config);

    /// <summary>A fresh copy of a preset's values.</summary>
    public static SettingValues Build(PresetId preset) => Build(preset, Config);

    public static SettingValues Build(PresetId preset, PresetConfig config)
    {
        SettingValues v = config.Progression.Clone();
        switch (preset)
        {
            case PresetId.Cleanup: Cleanup(v); break;
            case PresetId.Vanilla: Vanilla(v); break;
        }
        config.Locks.Enforce(v, config.Progression);
        return v;
    }

    /// <summary>The preset's values with the current keep and remove lists carried over.</summary>
    public static SettingValues Apply(PresetId preset, SettingValues current) => Apply(preset, current, Config);

    public static SettingValues Apply(PresetId preset, SettingValues current, PresetConfig config)
    {
        SettingValues values = Build(preset, config);
        values.rules.keepList = new List<string>(current.rules.keepList ?? new List<string>());
        values.rules.removeList = new List<string>(current.rules.removeList ?? new List<string>());
        return values;
    }

    /// <summary>Whether any setting differs from the preset. The lists do not count, since no preset sets them.</summary>
    public static bool IsChanged(PresetId preset, SettingValues current) => IsChanged(preset, current, Config);

    public static bool IsChanged(PresetId preset, SettingValues current, PresetConfig config) => !Apply(preset, current, config).SameAs(current);

    /// <summary>
    /// The values compiled into the mod: the Progression preset with empty lists. The settings defaults Def mirrors them, and
    /// is used instead once it loads.
    /// </summary>
    public static SettingValues CodeProgression()
    {
        var v = Common();
        foreach (OwnerGroup group in LootRules.RowGroups)
            v.rules.SetRow(group, WhoRule.Apply);
        v.rules.whoOtherJoiners = WhoRule.Mark;
        v.rules.weapons = CategoryRule.ScrapBeyondReach();
        v.rules.apparel = CategoryRule.ScrapBeyondReach();
        return v;
    }

    // Everything the presets table names is set here, so a pack that patches the Progression defaults cannot leak into
    // Cleanup or Vanilla through the rows they do not mention.
    private static void ResetBehaviour(SettingValues v)
    {
        LootRules r = v.rules;
        foreach (OwnerGroup group in LootRules.RowGroups)
            r.SetRow(group, WhoRule.Exclude);
        foreach (CategoryInfo category in ItemCategories.All)
            r.SetRule(category, Keep());
        v.corpses.humanlike = false;
        v.corpses.animal = false;
        v.corpses.mechanoid = false;
    }

    // Cleanup: hostile pawns only, and no era handling. Weapons below Normal, apparel below Good, and either below 50% hit
    // points are deleted, and so is all ammunition; everything else is left as in the game. Every hostile humanlike corpse is
    // removed. The "Don't drop your stuff" boxes and the cremation strip are not in the presets table, so they keep the
    // Progression value.
    private static void Cleanup(SettingValues v)
    {
        ResetBehaviour(v);
        v.rules.whoHostile = WhoRule.Apply;
        v.rules.weapons = DeleteBelow(ItemQuality.Normal, 0.5f);
        v.rules.apparel = DeleteBelow(ItemQuality.Good, 0.5f);
        v.rules.ammunition = new CategoryRule(Outcome.Delete);
        v.corpses.humanlike = true;
    }

    // Vanilla: no group is affected, so nothing changes, and everyone drops his gear as in the base game.
    private static void Vanilla(SettingValues v)
    {
        ResetBehaviour(v);
        v.colonistsKeepGear = false;
        v.hostilesKeepGear = false;
        v.nonHostilesKeepGear = false;
        v.cremationStripsCorpses = false;
    }

    // The compiled-in defaults: everything the table does not name, written out so they never depend on class initialisers.
    private static SettingValues Common()
    {
        var v = new SettingValues();
        LootRules r = v.rules;
        r.homeMapOnly = false;
        foreach (CategoryInfo category in ItemCategories.All)
            r.SetRule(category, Keep());
        r.disassemblyYield = DisassemblyYield.EraTypical;
        r.disassemblyEfficiency = Disassembly.DefaultEfficiency;
        r.protectQuestItems = true;
        r.protectUniqueWeapons = true;
        r.protectPersonaWeapons = true;
        r.protectRelics = true;
        r.keepList = new List<string>();
        r.removeList = new List<string>();

        v.corpses = new CorpseRules { humanlike = false, animal = false, mechanoid = false };

        v.colonistsKeepGear = true;
        v.hostilesKeepGear = true;
        v.nonHostilesKeepGear = true;
        v.cremationStripsCorpses = false;
        v.unfamiliarScope = UnfamiliarScope.AllOutOfReach;
        v.recycleOrStudy = RecycleOrStudy.OneOrTheOther;
        v.studyTechprintProjects = false;
        foreach (ScrapTier tier in SettingValues.Tiers)
        {
            v.SetStudyPoints(tier, SettingValues.DefaultStudyPoints(tier));
            v.SetScrapMarketValue(tier, SettingValues.DefaultScrapMarketValue);
        }
        return v;
    }

    private static CategoryRule Keep() => new CategoryRule(Outcome.Keep);

    private static CategoryRule DeleteBelow(ItemQuality quality, float hitPointFraction)
    {
        return new CategoryRule(Outcome.Delete)
        {
            limitQuality = true,
            belowQuality = quality,
            limitHitPoints = true,
            belowHitPointFraction = hitPointFraction,
        };
    }
}
