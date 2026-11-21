using System.Collections.Generic;
using LootScavenging.Loot;
using LootScavenging.Model;
using Verse;

namespace LootScavenging;

// Global settings. The mod loads them once (GetSettings in the mod constructor) and keeps the instance in
// LootScavengingMod.Settings; the evaluator, hooks and benches read its fields directly, so that instance is the cache and
// there is nothing to rebuild when a value changes. The default values come from Defs/SettingsDefaults.xml (a pack can patch
// it), read in SettingsDefaultsDef.cs once the defs are loaded; Model/Presets.cs holds the compiled-in fallback.
public class LootScavengingSettings : ModSettings
{
    public LootRules rules;

    // Era fallback for gear with no research era and no tech level (spec: replaces the carrying faction's era).
    public EraFallback eraFallback;

    // Scrap (ticket 03). Market value per unit: near zero so stockpiled scrap does not raise wealth or sell for silver.
    public const float DefaultScrapMarketValue = SettingValues.DefaultScrapMarketValue;
    public float scrapMarketValue;

    // Death-time scrap lands on the ground by default; false leaves it in the corpse's inventory.
    public bool scrapOnGround;

    // Which gear is labelled unfamiliar in original-item mode (ticket 06).
    public UnfamiliarScope unfamiliarScope;

    // Corpse removal and the butchering option (ticket 10).
    public CorpseRules corpses;

    // Study (ticket 05): which scrap can be studied, recycle or study, and whether techprint projects receive points.
    public StudyEraLimit studyEras;
    public RecycleOrStudy recycleOrStudy;
    public bool studyTechprintProjects;

    // The preset last picked on the page (ticket 11). Whether it has been changed since is worked out, not saved.
    public PresetId activePreset = PresetId.Progression;

    // The one-time first-load message has been shown.
    public bool firstLoadMessageShown;

    // The settings defaults Def has been applied to this config. A config with no saved value starts false, so the Def's
    // values (which a pack may have patched) seed it once the defs are loaded.
    private bool defaultsSeeded;

    public LootScavengingSettings()
    {
        Apply(Presets.Defaults());
    }

    /// <summary>
    /// Called once the defs are loaded, since the mod constructor runs before them. A fresh config takes the Def's defaults;
    /// a saved one keeps its values except locked settings, which return to the pack's value.
    /// </summary>
    public void OnDefaultsLoaded()
    {
        if (!defaultsSeeded)
        {
            Apply(Presets.Defaults());
            activePreset = PresetId.Progression;
            defaultsSeeded = true;
        }
        else
        {
            SettingValues current = Capture();
            Presets.Config.Locks.Enforce(current, Presets.Config.Progression);
            Apply(current);
        }
        ScrapDefs.ApplyMarketValue();
    }

    /// <summary>The current values. Shares the live rule objects, so read it, do not keep it.</summary>
    public SettingValues Capture()
    {
        return new SettingValues
        {
            rules = rules,
            corpses = corpses,
            eraFallback = eraFallback,
            scrapMarketValue = scrapMarketValue,
            scrapOnGround = scrapOnGround,
            unfamiliarScope = unfamiliarScope,
            studyEras = studyEras,
            recycleOrStudy = recycleOrStudy,
            studyTechprintProjects = studyTechprintProjects,
        };
    }

    /// <summary>Sets every value from a copy of <paramref name="values"/>, so the source is never edited through the page.</summary>
    public void Apply(SettingValues values)
    {
        SettingValues v = values.Clone();
        rules = v.rules;
        corpses = v.corpses;
        eraFallback = v.eraFallback;
        scrapMarketValue = v.scrapMarketValue;
        scrapOnGround = v.scrapOnGround;
        unfamiliarScope = v.unfamiliarScope;
        studyEras = v.studyEras;
        recycleOrStudy = v.recycleOrStudy;
        studyTechprintProjects = v.studyTechprintProjects;
    }

    public bool PresetChanged => Presets.IsChanged(activePreset, Capture());

    public override void ExposeData()
    {
        base.ExposeData();

        // Missing keys load as the default preset's values.
        SettingValues d = Presets.Defaults();
        Scribe_Values.Look(ref activePreset, "activePreset", PresetId.Progression);
        Scribe_Values.Look(ref firstLoadMessageShown, "firstLoadMessageShown", false);
        Scribe_Values.Look(ref defaultsSeeded, "defaultsSeeded", false);

        CorpseRules corpseDefaults = d.corpses;
        Scribe_Values.Look(ref corpses.humanlike, "corpseHumanlike", corpseDefaults.humanlike);
        Scribe_Values.Look(ref corpses.animal, "corpseAnimal", corpseDefaults.animal);
        Scribe_Values.Look(ref corpses.mechanoid, "corpseMechanoid", corpseDefaults.mechanoid);
        Scribe_Values.Look(ref corpses.chance, "corpseChance", corpseDefaults.chance);
        Scribe_Values.Look(ref corpses.butcherRemovesApparel, "butcherRemovesApparel", corpseDefaults.butcherRemovesApparel);

        LootRules defaults = d.rules;
        Scribe_Values.Look(ref rules.hostile, "hostile", defaults.hostile);
        Scribe_Values.Look(ref rules.nonHostileFaction, "nonHostileFaction", defaults.nonHostileFaction);
        Scribe_Values.Look(ref rules.noFaction, "noFaction", defaults.noFaction);
        Scribe_Values.Look(ref rules.prisoners, "prisoners", defaults.prisoners);
        Scribe_Values.Look(ref rules.homeMapOnly, "homeMapOnly", defaults.homeMapOnly);
        Scribe_Values.Look(ref rules.onDownedAndStrip, "onDownedAndStrip", defaults.onDownedAndStrip);
        Scribe_Values.Look(ref rules.dropMode, "dropMode", defaults.dropMode);
        foreach (CategoryInfo category in ItemCategories.All)
            ScribeRule(category.Rule(rules), category.Field, category.Rule(defaults));
        Scribe_Values.Look(ref rules.degradeMin, "degradeMin", defaults.degradeMin);
        Scribe_Values.Look(ref rules.degradeMax, "degradeMax", defaults.degradeMax);
        Scribe_Values.Look(ref rules.degradeLowersQuality, "degradeLowersQuality", defaults.degradeLowersQuality);
        Scribe_Values.Look(ref rules.disassemblyYield, "disassemblyYield", defaults.disassemblyYield);
        Scribe_Values.Look(ref rules.disassemblyEfficiency, "disassemblyEfficiency", defaults.disassemblyEfficiency);
        Scribe_Values.Look(ref rules.ejectCharges, "ejectCharges", defaults.ejectCharges);
        Scribe_Values.Look(ref rules.protectQuestItems, "protectQuestItems", defaults.protectQuestItems);
        Scribe_Values.Look(ref rules.protectUniqueWeapons, "protectUniqueWeapons", defaults.protectUniqueWeapons);
        Scribe_Values.Look(ref rules.protectPersonaWeapons, "protectPersonaWeapons", defaults.protectPersonaWeapons);
        Scribe_Values.Look(ref rules.protectRelics, "protectRelics", defaults.protectRelics);
        // Stored as def names, so an entry for a removed item is just an unmatched string.
        Scribe_Collections.Look(ref rules.keepList, "keepList", LookMode.Value);
        Scribe_Collections.Look(ref rules.removeList, "removeList", LookMode.Value);
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            rules.keepList ??= new List<string>();
            rules.removeList ??= new List<string>();
        }
        Scribe_Values.Look(ref eraFallback, "eraFallback", d.eraFallback);
        Scribe_Values.Look(ref scrapMarketValue, "scrapMarketValue", d.scrapMarketValue);
        Scribe_Values.Look(ref scrapOnGround, "scrapOnGround", d.scrapOnGround);
        Scribe_Values.Look(ref unfamiliarScope, "unfamiliarScope", d.unfamiliarScope);
        Scribe_Values.Look(ref studyEras, "studyEras", d.studyEras);
        Scribe_Values.Look(ref recycleOrStudy, "recycleOrStudy", d.recycleOrStudy);
        Scribe_Values.Look(ref studyTechprintProjects, "studyTechprintProjects", d.studyTechprintProjects);
    }

    // The outcome keeps its original key ("weaponsOutcome"); the conditions and chance are new.
    private static void ScribeRule(CategoryRule rule, string name, CategoryRule defaults)
    {
        Scribe_Values.Look(ref rule.outcome, name + "Outcome", defaults.outcome);
        Scribe_Values.Look(ref rule.onlyBeyondReach, name + "OnlyBeyondReach", defaults.onlyBeyondReach);
        Scribe_Values.Look(ref rule.limitQuality, name + "LimitQuality", defaults.limitQuality);
        Scribe_Values.Look(ref rule.belowQuality, name + "BelowQuality", defaults.belowQuality);
        Scribe_Values.Look(ref rule.limitHitPoints, name + "LimitHitPoints", defaults.limitHitPoints);
        Scribe_Values.Look(ref rule.belowHitPointFraction, name + "BelowHitPointFraction", defaults.belowHitPointFraction);
        Scribe_Values.Look(ref rule.chance, name + "Chance", defaults.chance);
    }
}
