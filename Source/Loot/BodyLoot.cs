using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using LootScavenging.Model;
using RimWorld;
using Verse;

namespace LootScavenging.Loot;

/// <summary>
/// Loot kept on a dead pawn's body for the "where loot lands at death" setting. Pawn.Kill drops the weapon and inventory
/// beside the corpse, so what should stay is taken off the pawn before that drop and put back into the dead pawn's
/// inventory after it, where stripping, butchering or cremating the corpse finds it.
/// </summary>
internal static class BodyLoot
{
    // Things taken off a pawn for the length of its Kill call.
    private static readonly Dictionary<Pawn, List<Thing>> held = new Dictionary<Pawn, List<Thing>>();

    // Dying pawns whose loot all stays on the body, decided before their gear is sorted so that disassembly products and
    // ejected charges are held too instead of placed on the ground.
    private static readonly HashSet<Pawn> keepsAll = new HashSet<Pawn>();

    public static bool KeepsAll(Pawn pawn) => keepsAll.Contains(pawn);

    // Both on-body options skip these pawns: loot left on a body the game strips of nothing (most mechanoids) would be stuck.
    public static bool CanHoldLoot(Pawn pawn) => !pawn.kindDef.destroyGearOnDrop && pawn.kindDef.canStrip;

    public static void Hold(Pawn pawn, Thing thing)
    {
        if (!held.TryGetValue(pawn, out List<Thing> list))
            held[pawn] = list = new List<Thing>();
        list.Add(thing);
    }

    /// <summary>
    /// Called at the start of a death, before the gear is sorted: with "everything stays on the body", marks a covered
    /// pawn so its loot is held. Pawns whose gear the game destroys at death (most mechanoids) and pawns that cannot be
    /// stripped drop as usual, so nothing is saved that would otherwise vanish or be stuck on a corpse.
    /// </summary>
    public static void BeginDeath(Pawn pawn)
    {
        if (LootScavengingMod.Settings.values.deathDrop != DeathDrop.AllOnBody || !pawn.Spawned || !CanHoldLoot(pawn))
            return;
        MapKind map = pawn.Map.IsPlayerHome ? MapKind.Home : MapKind.Away;
        if (LootEvaluator.CoversPawn(GearSorter.OwnerOf(pawn), map, LootScavengingMod.Settings.values.rules))
            keepsAll.Add(pawn);
    }

    /// <summary>Takes a marked pawn's weapon and inventory off it before the death drop. Runs after the gear is sorted, so
    /// what is left is what the rules kept.</summary>
    public static void HoldGear(Pawn pawn)
    {
        if (!keepsAll.Contains(pawn))
            return;

        if (pawn.equipment != null)
        {
            foreach (ThingWithComps eq in pawn.equipment.AllEquipmentListForReading.ToList())
            {
                pawn.equipment.Remove(eq);
                Hold(pawn, eq);
            }
        }
        if (pawn.inventory != null)
        {
            foreach (Thing thing in pawn.inventory.innerContainer.ToList())
            {
                pawn.inventory.innerContainer.Remove(thing);
                Hold(pawn, thing);
            }
        }
    }

    // Called after Pawn.Kill: the death drop has run, so things added now stay in the dead pawn's inventory. The entry is
    // always used up: if the death was cancelled or threw, the pawn simply gets its things back in its inventory.
    public static void PlaceOnBody(Pawn pawn)
    {
        keepsAll.Remove(pawn);
        if (!held.TryGetValue(pawn, out List<Thing> list))
            return;
        held.Remove(pawn);
        foreach (Thing thing in list)
            Give(pawn, thing);
    }

    public static void Give(Pawn pawn, Thing thing)
    {
        if (pawn.inventory != null && pawn.inventory.innerContainer.TryAdd(thing, true))
            return;

        // No inventory (some mechanoids): put it on the ground where the pawn or its corpse lies.
        Map map = pawn.MapHeld;
        if (map != null && pawn.PositionHeld.IsValid)
            GenPlace.TryPlaceThing(thing, pawn.PositionHeld, map, ThingPlaceMode.Near);
        else
            thing.Destroy();
    }
}

// After the death drop, a dead pawn gets back what was held for its body. Pawn.Kill has two postfixes from this mod; they
// share one Harmony id, so only HarmonyPriority orders them. This one runs first: when CorpseRemovalHook then removes the
// corpse, its strip drops the inventory to the ground instead of the loot vanishing.
[HarmonyPatch(typeof(Pawn), nameof(Pawn.Kill))]
internal static class BodyLootHook
{
    [HarmonyPriority(Priority.High)]
    private static void Postfix(Pawn __instance)
    {
        try
        {
            BodyLoot.PlaceOnBody(__instance);
        }
        catch (System.Exception e)
        {
            Log.Error($"[Loot Scavenging] Leaving loot on {__instance} failed: {e}"); // not translated
        }
    }

    // Finalizers run after every postfix. When Kill (or a patch on it) threw, the postfix may not have run, and the held
    // things would otherwise be lost.
    private static void Finalizer(Pawn __instance, System.Exception __exception)
    {
        if (__exception == null)
            return;
        try
        {
            BodyLoot.PlaceOnBody(__instance);
        }
        catch (System.Exception e)
        {
            Log.Error($"[Loot Scavenging] Returning held loot to {__instance} failed: {e}"); // not translated
        }
    }
}
