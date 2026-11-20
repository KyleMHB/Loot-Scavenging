using LootScavenging.Model;
using Verse;

namespace LootScavenging;

// Global settings. Each ticket adds its own fields and Scribe lines here; ticket 11 adds the page and presets.
public class LootScavengingSettings : ModSettings
{
    // Read directly by the evaluator, so there is nothing to cache or rebuild.
    public LootRules rules = new LootRules();

    public override void ExposeData()
    {
        base.ExposeData();

        var defaults = new LootRules();
        Scribe_Values.Look(ref rules.hostile, "hostile", defaults.hostile);
        Scribe_Values.Look(ref rules.nonHostileFaction, "nonHostileFaction", defaults.nonHostileFaction);
        Scribe_Values.Look(ref rules.noFaction, "noFaction", defaults.noFaction);
        Scribe_Values.Look(ref rules.prisoners, "prisoners", defaults.prisoners);
        Scribe_Values.Look(ref rules.homeMapOnly, "homeMapOnly", defaults.homeMapOnly);
        Scribe_Values.Look(ref rules.onDownedAndStrip, "onDownedAndStrip", defaults.onDownedAndStrip);
        Scribe_Values.Look(ref rules.weapons.outcome, "weaponsOutcome", defaults.weapons.outcome);
        Scribe_Values.Look(ref rules.apparel.outcome, "apparelOutcome", defaults.apparel.outcome);
    }
}
