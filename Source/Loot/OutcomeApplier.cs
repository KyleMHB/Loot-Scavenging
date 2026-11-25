using System.Collections.Generic;
using System.Linq;
using LootScavenging.Model;
using RimWorld;
using Verse;

namespace LootScavenging.Loot;

/// <summary>Carries out an outcome on an item still held by its pawn, before the game drops it. The one dispatch point.</summary>
internal static class OutcomeApplier
{
    public static void Apply(Pawn pawn, Thing thing, LootDecision decision, EraResult era, Moment moment)
    {
        // Charges are read and the place noted before the item is destroyed, and the ammo is put on the ground rather than
        // in the inventory, where the inventory pass would judge it again as ammunition. Always on (ticket 25, decision 8).
        if (Disassembly.EjectsCharges(decision.Outcome))
        {
            List<Thing> charges = DisassembleApplier.EjectedCharges(thing).ToList();
            Map map = pawn.MapHeld;
            IntVec3 position = pawn.PositionHeld;
            ApplyOutcome(pawn, thing, decision, era, moment);
            foreach (Thing ammo in charges)
                DisassembleApplier.Place(pawn, ammo, map, position);
            if (Prefs.DevMode && charges.Count > 0)
                Log.Message($"[Loot Scavenging] Ejected from {thing}: {string.Join(", ", charges.Select(t => t.LabelCap))}"); // not translated
            return;
        }
        ApplyOutcome(pawn, thing, decision, era, moment);
    }

    private static void ApplyOutcome(Pawn pawn, Thing thing, LootDecision decision, EraResult era, Moment moment)
    {
        switch (decision.Outcome)
        {
            case Outcome.Keep:
                // Only a Who row set to Mark marks what it keeps (ticket 25, decision 3); other kept gear is left as it is.
                if (decision.Reason == DecisionReason.MarkRow)
                    Unfamiliar.NotifyKept(pawn, thing);
                return;
            case Outcome.Delete:
                Delete(pawn, thing);
                return;
            case Outcome.Scrap:
                ScrapApplier.Apply(pawn, thing, era, moment);
                return;
            case Outcome.Disassemble:
                DisassembleApplier.Disassemble(pawn, thing);

                return;
            default:
                Log.Warning($"[Loot Scavenging] Outcome {decision.Outcome} is not handled here; keeping {thing}."); // not translated
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
