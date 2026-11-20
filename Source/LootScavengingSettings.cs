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
        Scribe_Values.Look(ref rules.dropMode, "dropMode", defaults.dropMode);
        Scribe_Values.Look(ref rules.weapons.outcome, "weaponsOutcome", defaults.weapons.outcome);
        Scribe_Values.Look(ref rules.apparel.outcome, "apparelOutcome", defaults.apparel.outcome);
        Scribe_Values.Look(ref eraFallback, "eraFallback", EraFallback.CarrierFaction);
        Scribe_Values.Look(ref scrapMarketValue, "scrapMarketValue", DefaultScrapMarketValue);
        Scribe_Values.Look(ref scrapOnGround, "scrapOnGround", true);
    }
}
