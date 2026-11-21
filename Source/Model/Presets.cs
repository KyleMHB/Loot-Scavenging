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
/// The preset values, all in this one file (ticket 13 moves them to a patchable Def). Each preset sets every setting; the
/// keep and remove lists are the player's own picks, so applying a preset leaves them as they are and the "changed"
/// comparison ignores them. Reset to defaults clears them too.
/// </summary>
public static class Presets
{
    /// <summary>The code default: the Progression preset with empty lists.</summary>
    public static SettingValues Defaults() => Build(PresetId.Progression);

    /// <summary>A fresh copy of a preset's values, with empty lists.</summary>
    public static SettingValues Build(PresetId preset)
    {
        switch (preset)
        {
            case PresetId.Cleanup: return Cleanup();
            case PresetId.Vanilla: return Vanilla();
            default: return Progression();
        }
    }

    /// <summary>The preset's values with the current keep and remove lists carried over.</summary>
    public static SettingValues Apply(PresetId preset, SettingValues current)
    {
        SettingValues values = Build(preset);
        values.rules.keepList = new List<string>(current.rules.keepList ?? new List<string>());
        values.rules.removeList = new List<string>(current.rules.removeList ?? new List<string>());
        return values;
    }

    /// <summary>Whether any setting differs from the preset. The lists do not count, since no preset sets them.</summary>
    public static bool IsChanged(PresetId preset, SettingValues current) => !Apply(preset, current).SameAs(current);

    // Progression: out-of-reach gear becomes scrap at death, downed and strip, from every non-colony group including
    // prisoners. Gear within reach is kept. Corpses are left alone.
    private static SettingValues Progression()
    {
        var v = Common();
        v.rules.dropMode = DropMode.ScrapOutOfReach;
        v.rules.onDownedAndStrip = true;
        SetGroups(v.rules, hostile: true, nonHostileFaction: true, noFaction: true, prisoners: true);
        return v;
    }

    // Cleanup: no era handling, hostile dead only. Weapons below Normal, apparel below Good, and either below 50% hit
    // points are deleted; everything else is kept. Every hostile humanlike corpse is removed.
    private static SettingValues Cleanup()
    {
        var v = Common();
        v.rules.dropMode = DropMode.Off;
        v.rules.onDownedAndStrip = false;
        SetGroups(v.rules, hostile: true, nonHostileFaction: false, noFaction: false, prisoners: false);
        v.rules.weapons = DeleteBelow(ItemQuality.Normal, 0.5f);
        v.rules.apparel = DeleteBelow(ItemQuality.Good, 0.5f);
        v.corpses.humanlike = true;
        v.corpses.chance = 1f;
        return v;
    }

    // Vanilla: no group is affected, so nothing changes. The moment is unused; it is left at death only.
    private static SettingValues Vanilla()
    {
        var v = Common();
        v.rules.dropMode = DropMode.Off;
        v.rules.onDownedAndStrip = false;
        SetGroups(v.rules, hostile: false, nonHostileFaction: false, noFaction: false, prisoners: false);
        return v;
    }

    // Values every preset shares: everything the table does not name, written out so a preset never depends on class
    // initialisers.
    private static SettingValues Common()
    {
        var v = new SettingValues();
        LootRules r = v.rules;
        r.homeMapOnly = false;
        r.weapons = Keep();
        r.apparel = Keep();
        r.ammunition = Keep();
        r.drugs = Keep();
        r.medicine = Keep();
        r.food = Keep();
        r.other = Keep();
        r.degradeMin = Disassembly.DefaultDegradeMin;
        r.degradeMax = Disassembly.DefaultDegradeMax;
        r.degradeLowersQuality = false;
        r.disassemblyYield = DisassemblyYield.EraTypical;
        r.disassemblyEfficiency = Disassembly.DefaultEfficiency;
        r.ejectCharges = true;
        r.protectQuestItems = true;
        r.protectUniqueWeapons = true;
        r.protectPersonaWeapons = true;
        r.protectRelics = true;
        r.keepList = new List<string>();
        r.removeList = new List<string>();

        v.corpses = new CorpseRules { humanlike = false, animal = false, mechanoid = false, chance = 1f, butcherRemovesApparel = false };

        v.eraFallback = EraFallback.CarrierFaction;
        v.scrapMarketValue = SettingValues.DefaultScrapMarketValue;
        v.scrapOnGround = true;
        v.unfamiliarScope = UnfamiliarScope.AllOutOfReach;
        v.studyEras = StudyEraLimit.ColonyEraAndBelow;
        v.recycleOrStudy = RecycleOrStudy.OneOrTheOther;
        v.studyTechprintProjects = false;
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

    private static void SetGroups(LootRules r, bool hostile, bool nonHostileFaction, bool noFaction, bool prisoners)
    {
        r.hostile = hostile;
        r.nonHostileFaction = nonHostileFaction;
        r.noFaction = noFaction;
        r.prisoners = prisoners;
    }
}
