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

    // Corpse removal and the butchering option (ticket 10). Code defaults: everything off.
    public CorpseRules corpses = new CorpseRules();

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
        Scribe_Values.Look(ref rules.weapons.outcome, "weaponsOutcome", defaults.weapons.outcome);
        Scribe_Values.Look(ref rules.apparel.outcome, "apparelOutcome", defaults.apparel.outcome);
        Scribe_Values.Look(ref eraFallback, "eraFallback", EraFallback.CarrierFaction);
    }
}
