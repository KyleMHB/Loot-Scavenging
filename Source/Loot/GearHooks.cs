using System;
using System.Collections.Generic;
using HarmonyLib;
using LootScavenging.Model;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace LootScavenging.Loot;

// Thin hooks before the three moments a pawn's gear changes hands (spec, "Code structure"). Each one sorts only the
// gear the game is about to drop at that moment. Call order and signatures: spec, "Unverified".
// An exception here would stop the game method itself (a pawn that cannot die), so each hook logs and carries on.
//
// The game also drops a living pawn's gear outside those three (ticket 18): an arrest or a trade (DropAndForbidEverything)
// and a lost grip (TryDropEquipment). Those count as the downed-and-strip moment. A pawn joining the colony keeps his gear
// as a colony pawn; JoinerHooks.cs handles that (ticket 20). Death, downing and stripping call DropAndForbidEverything or TryDropEquipment themselves,
// so a pawn whose drop this mod is already sorting is marked for the length of that call and the inner hooks leave it
// alone: no item is judged twice.

/// <summary>Pawns whose drop the mod is sorting right now, so the game's own drop calls inside it are not sorted again.</summary>
internal static class SortedDrop
{
    private static readonly Dictionary<Pawn, int> depth = new Dictionary<Pawn, int>();

    public static bool Contains(Pawn pawn) => depth.ContainsKey(pawn);

    public static void Enter(Pawn pawn)
    {
        depth.TryGetValue(pawn, out int n);
        depth[pawn] = n + 1;
    }

    public static void Exit(Pawn pawn)
    {
        if (!depth.TryGetValue(pawn, out int n))
            return;
        if (n <= 1)
            depth.Remove(pawn);
        else
            depth[pawn] = n - 1;
    }

    /// <summary>The checks the hooks for a living pawn's drop share: a standing non-colony pawn whose drop no other hook of
    /// this mod is sorting. Dead and downed pawns are left out because Kill and MakeDowned set those states before their
    /// own drop, which the hooks above already sorted.</summary>
    public static bool ShouldSort(Pawn pawn, bool runOriginal) =>
        runOriginal && Current.ProgramState == ProgramState.Playing && pawn != null && !pawn.Dead && !pawn.Destroyed
        && !pawn.Downed && pawn.Faction is not { IsPlayer: true } && !Contains(pawn);
}

[HarmonyPatch(typeof(Pawn), nameof(Pawn.Kill))]
internal static class PawnKillHook
{
    // Last, so a mod that cancels the death runs first and this prefix sees it through __runOriginal.
    [HarmonyPriority(Priority.Last)]
    private static void Prefix(Pawn __instance, bool __runOriginal, out bool __state)
    {
        __state = false;
        if (!__runOriginal || Current.ProgramState != ProgramState.Playing || __instance.Dead || __instance.Destroyed)
            return;
        SortedDrop.Enter(__instance);
        __state = true;
        try
        {
            // Death drops equipment and inventory; apparel stays on the corpse, so this is its last sorting too.
            // Does not depend on stripping, so pawns that cannot be stripped are still sorted.
            BodyLoot.BeginDrop(__instance);
            GearSorter.Sort(__instance, Moment.Death, equipment: true, apparel: true, inventory: true);
            BodyLoot.HoldGear(__instance);
        }
        catch (Exception e)
        {
            Log.Error($"[Loot Scavenging] Sorting gear at the death of {__instance} failed: {e}"); // not translated
        }
    }

    // After the death drop, a dead pawn gets back what was held for its body. The prefix, postfix and finalizer share one
    // class so they are patched together or not at all: a prefix that holds loot without its postfix would lose it.
    // Pawn.Kill has two postfixes from this mod; they share one Harmony id, so only HarmonyPriority orders them. This one
    // runs first: when CorpseRemovalHook then removes the corpse, its strip drops the inventory to the ground instead of
    // the loot vanishing.
    [HarmonyPriority(Priority.High)]
    private static void Postfix(Pawn __instance)
    {
        try
        {
            BodyLoot.PlaceOnBody(__instance, downed: false);
        }
        catch (Exception e)
        {
            Log.Error($"[Loot Scavenging] Leaving loot on {__instance} failed: {e}"); // not translated
        }
    }

    // Finalizers run after every postfix. When Kill (or a patch on it) threw, the postfix may not have run, and the held
    // things would otherwise be lost. The pawn's mark always ends here.
    private static void Finalizer(Pawn __instance, Exception __exception, bool __state)
    {
        if (__state)
            SortedDrop.Exit(__instance);
        if (__exception == null)
            return;
        try
        {
            BodyLoot.PlaceOnBody(__instance, downed: false);
        }
        catch (Exception e)
        {
            Log.Error($"[Loot Scavenging] Returning held loot to {__instance} failed: {e}"); // not translated
        }
    }
}

[HarmonyPatch(typeof(Pawn_HealthTracker), "MakeDowned")]
internal static class PawnDownedHook
{
    [HarmonyPriority(Priority.Last)]
    private static void Prefix(Pawn_HealthTracker __instance, Pawn ___pawn, bool __runOriginal, out bool __state)
    {
        __state = false;
        // Mirrors the game: an already downed pawn errors out, and only a spawned pawn outside a bed drops anything.
        if (!__runOriginal || Current.ProgramState != ProgramState.Playing || __instance.Downed || !___pawn.Spawned || ___pawn.InBed())
            return;
        SortedDrop.Enter(___pawn);
        __state = true;
        try
        {
            // Downing drops equipment and inventory, never apparel. With "everything stays on the body" the kept gear is
            // held and handed back after the drop (the postfix below).
            BodyLoot.BeginDrop(___pawn);
            GearSorter.Sort(___pawn, Moment.Downed, equipment: true, apparel: false, inventory: true);
            BodyLoot.HoldGear(___pawn);
        }
        catch (Exception e)
        {
            Log.Error($"[Loot Scavenging] Sorting gear of downed {___pawn} failed: {e}"); // not translated
        }
    }

    // After the downed drop, a downed pawn gets back what was held, weapon included, in its inventory.
    private static void Postfix(Pawn ___pawn)
    {
        try
        {
            BodyLoot.PlaceOnBody(___pawn, downed: true);
        }
        catch (Exception e)
        {
            Log.Error($"[Loot Scavenging] Leaving loot on downed {___pawn} failed: {e}"); // not translated
        }
    }

    private static void Finalizer(Pawn ___pawn, Exception __exception, bool __state)
    {
        if (__state)
            SortedDrop.Exit(___pawn);
        if (__exception == null)
            return;
        try
        {
            BodyLoot.PlaceOnBody(___pawn, downed: true);
        }
        catch (Exception e)
        {
            Log.Error($"[Loot Scavenging] Returning held loot to downed {___pawn} failed: {e}"); // not translated
        }
    }
}

[HarmonyPatch(typeof(Pawn), nameof(Pawn.Strip))]
internal static class PawnStripHook
{
    [HarmonyPriority(Priority.Last)]
    private static void Prefix(Pawn __instance, bool __runOriginal, out bool __state)
    {
        __state = false;
        // A dead pawn's gear was sorted at death, so corpse strips (including butchering) are left to the game.
        // In a caravan the gear moves to other members instead of dropping.
        if (!__runOriginal || Current.ProgramState != ProgramState.Playing || __instance.Dead || __instance.GetCaravan() != null)
            return;
        SortedDrop.Enter(__instance);
        __state = true;
        try
        {
            GearSorter.Sort(__instance, Moment.Strip, equipment: true, apparel: true, inventory: true);
        }
        catch (Exception e)
        {
            Log.Error($"[Loot Scavenging] Sorting gear of stripped {__instance} failed: {e}"); // not translated
        }
    }

    // Strip drops the weapon through TryDropEquipment, which must not judge it again.
    private static void Finalizer(Pawn __instance, bool __state)
    {
        if (__state)
            SortedDrop.Exit(__instance);
    }
}

// Arresting a standing pawn (Pawn_GuestTracker.SetGuestStatus) and trading one (Pawn.PreTraded) drop his weapon and
// inventory through this call. Death and downing call it too, from inside the hooks above, so dead, downed and marked pawns
// are skipped. Apparel stays on, as in the game.
[HarmonyPatch(typeof(Pawn), nameof(Pawn.DropAndForbidEverything))]
internal static class DropEverythingHook
{
    [HarmonyPriority(Priority.Last)]
    private static void Prefix(Pawn __instance, bool keepInventoryAndEquipmentIfInBed, bool __runOriginal, out bool __state)
    {
        __state = false;
        // In bed with the keep flag the game drops nothing but a carried thing.
        if (!SortedDrop.ShouldSort(__instance, __runOriginal) || keepInventoryAndEquipmentIfInBed && __instance.InBed())
            return;
        SortedDrop.Enter(__instance);
        __state = true;
        try
        {
            // A carried thing is not the pawn's gear (a thief's loot, a kidnapped colonist) and drops as the game drops it.
            GearSorter.Sort(__instance, Moment.Downed, equipment: true, apparel: false, inventory: true);
        }
        catch (Exception e)
        {
            Log.Error($"[Loot Scavenging] Sorting the gear {__instance} drops failed: {e}"); // not translated
        }
    }

    private static void Finalizer(Pawn __instance, bool __state)
    {
        if (__state)
            SortedDrop.Exit(__instance);
    }
}

// A pawn that can no longer hold a weapon (Pawn_HealthTracker, lost manipulation) drops it through this call, and so do a
// few rarer paths (turning into a mutant, a gene change). The drops of death, downing, stripping and DropAndForbidEverything
// come through here too, and are skipped by the marks above.
[HarmonyPatch(typeof(Pawn_EquipmentTracker), nameof(Pawn_EquipmentTracker.TryDropEquipment))]
internal static class DropEquipmentHook
{
    [HarmonyPriority(Priority.Last)]
    private static bool Prefix(Pawn_EquipmentTracker __instance, ThingWithComps eq, ref ThingWithComps resultingEq, ref bool __result, bool __runOriginal)
    {
        Pawn pawn = __instance.pawn;
        if (eq == null || !SortedDrop.ShouldSort(pawn, __runOriginal) || !__instance.Contains(eq))
            return true;
        try
        {
            GearSorter.SortEquipment(pawn, eq, Moment.Downed);
        }
        catch (Exception e)
        {
            Log.Error($"[Loot Scavenging] Sorting the weapon {pawn} drops failed: {e}"); // not translated
        }
        if (__instance.Contains(eq))
            return true;
        // Scrapped, disassembled or deleted: nothing is left to drop, and the room a caller wanted is made.
        resultingEq = null;
        __result = true;
        return false;
    }
}
