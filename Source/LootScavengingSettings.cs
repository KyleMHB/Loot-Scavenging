using System.Collections.Generic;
using LootScavenging.Model;
using Verse;

namespace LootScavenging;

// Global settings. Each ticket adds its own fields and Scribe lines here; ticket 11 adds the page and presets.
public class LootScavengingSettings : ModSettings
{
    // Read directly by the evaluator, so there is nothing to cache or rebuild.
    public LootRules rules = new LootRules();

    // Era fallback for gear with no research era and no tech level (spec: replaces the carrying faction's era).
    public EraFallback eraFallback = EraFallback.CarrierFaction;

    // Scrap (ticket 03). Market value per unit: near zero so stockpiled scrap does not raise wealth or sell for silver.
    public const float DefaultScrapMarketValue = 0.01f;
    public float scrapMarketValue = DefaultScrapMarketValue;

    // Death-time scrap lands on the ground by default; false leaves it in the corpse's inventory.
    public bool scrapOnGround = true;

    // Which gear is labelled unfamiliar in original-item mode (ticket 06).
    public UnfamiliarScope unfamiliarScope = UnfamiliarScope.AllOutOfReach;

    // Corpse removal and the butchering option (ticket 10). Code defaults: everything off.
    public CorpseRules corpses = new CorpseRules();

    // Study (ticket 05): which scrap can be studied, recycle or study, and whether techprint projects receive points.
    public StudyEraLimit studyEras = StudyEraLimit.ColonyEraAndBelow;
    public RecycleOrStudy recycleOrStudy = RecycleOrStudy.OneOrTheOther;
    public bool studyTechprintProjects = false;

    public override void ExposeData()
    {
        base.ExposeData();

        var corpseDefaults = new CorpseRules();
        Scribe_Values.Look(ref corpses.humanlike, "corpseHumanlike", corpseDefaults.humanlike);
        Scribe_Values.Look(ref corpses.animal, "corpseAnimal", corpseDefaults.animal);
        Scribe_Values.Look(ref corpses.mechanoid, "corpseMechanoid", corpseDefaults.mechanoid);
        Scribe_Values.Look(ref corpses.chance, "corpseChance", corpseDefaults.chance);
        Scribe_Values.Look(ref corpses.butcherRemovesApparel, "butcherRemovesApparel", corpseDefaults.butcherRemovesApparel);

        var defaults = new LootRules();
        Scribe_Values.Look(ref rules.hostile, "hostile", defaults.hostile);
        Scribe_Values.Look(ref rules.nonHostileFaction, "nonHostileFaction", defaults.nonHostileFaction);
        Scribe_Values.Look(ref rules.noFaction, "noFaction", defaults.noFaction);
        Scribe_Values.Look(ref rules.prisoners, "prisoners", defaults.prisoners);
        Scribe_Values.Look(ref rules.homeMapOnly, "homeMapOnly", defaults.homeMapOnly);
        Scribe_Values.Look(ref rules.onDownedAndStrip, "onDownedAndStrip", defaults.onDownedAndStrip);
        Scribe_Values.Look(ref rules.dropMode, "dropMode", defaults.dropMode);
        ScribeRule(rules.weapons, "weapons", defaults.weapons);
        ScribeRule(rules.apparel, "apparel", defaults.apparel);
        ScribeRule(rules.ammunition, "ammunition", defaults.ammunition);
        ScribeRule(rules.drugs, "drugs", defaults.drugs);
        ScribeRule(rules.medicine, "medicine", defaults.medicine);
        ScribeRule(rules.food, "food", defaults.food);
        ScribeRule(rules.other, "other", defaults.other);
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
        Scribe_Values.Look(ref eraFallback, "eraFallback", EraFallback.CarrierFaction);
        Scribe_Values.Look(ref scrapMarketValue, "scrapMarketValue", DefaultScrapMarketValue);
        Scribe_Values.Look(ref scrapOnGround, "scrapOnGround", true);
        Scribe_Values.Look(ref unfamiliarScope, "unfamiliarScope", UnfamiliarScope.AllOutOfReach);
        Scribe_Values.Look(ref studyEras, "studyEras", StudyEraLimit.ColonyEraAndBelow);
        Scribe_Values.Look(ref recycleOrStudy, "recycleOrStudy", RecycleOrStudy.OneOrTheOther);
        Scribe_Values.Look(ref studyTechprintProjects, "studyTechprintProjects", false);
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
