using LootScavenging.Model;
using RimWorld;
using Verse;

namespace LootScavenging.Loot;

/// <summary>Carries out an outcome on an item still held by its pawn, before the game drops it.</summary>
internal static class OutcomeApplier
{
    public static void Apply(Pawn pawn, Thing thing, Outcome outcome)
    {
        switch (outcome)
        {
            case Outcome.Keep:
                return;
            case Outcome.Delete:
                Delete(pawn, thing);
                return;
            default:
                // Degrade, scrap and disassemble arrive with later tickets; until then the item is kept.
                Log.Warning($"[Loot Scavenging] Outcome {outcome} is not built yet; keeping {thing}."); // not translated
                return;
        }
    }

    // Each tracker's own removal keeps its bookkeeping right (primary weapon, worn graphics, inventory counts).
    internal static void Delete(Pawn pawn, Thing thing)
    {
        if (thing is ThingWithComps eq && pawn.equipment != null && pawn.equipment.Contains(eq))
        {
            pawn.equipment.DestroyEquipment(eq);
        }
        else if (thing is Apparel ap && pawn.apparel != null && pawn.apparel.Contains(ap))
        {
            pawn.apparel.Remove(ap);
            ap.Destroy();
        }
        else if (pawn.inventory != null && pawn.inventory.innerContainer.Contains(thing))
        {
            pawn.inventory.innerContainer.Remove(thing);
            thing.Destroy();
        }
    }
}
