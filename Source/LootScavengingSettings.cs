using LootScavenging.Model;
using Verse;

namespace LootScavenging;

// Global settings. Each ticket adds its own fields and Scribe lines here; ticket 11 adds the page and presets.
public class LootScavengingSettings : ModSettings
{
    // Era fallback for gear with no research era and no tech level (spec: replaces the carrying faction's era).
    public EraFallback eraFallback = EraFallback.CarrierFaction;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref eraFallback, "eraFallback", EraFallback.CarrierFaction);
    }
}
