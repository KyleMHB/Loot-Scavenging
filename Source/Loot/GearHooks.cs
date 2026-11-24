using System;
using HarmonyLib;
using LootScavenging.Model;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace LootScavenging.Loot;

// Thin hooks before the three moments a pawn's gear changes hands (spec, "Code structure"). Each one sorts only the
// gear the game is about to drop at that moment. Call order and signatures: spec, "Unverified".
// An exception here would stop the game method itself (a pawn that cannot die), so each hook logs and carries on.

[HarmonyPatch(typeof(Pawn), nameof(Pawn.Kill))]
internal static class PawnKillHook
{
    // Last, so a mod that cancels the death runs first and this prefix sees it through __runOriginal.
    [HarmonyPriority(Priority.Last)]
    private static void Prefix(Pawn __instance, bool __runOriginal)
    {
        if (!__runOriginal || Current.ProgramState != ProgramState.Playing || __instance.Dead || __instance.Destroyed)
            return;
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
    // things would otherwise be lost.
    private static void Finalizer(Pawn __instance, Exception __exception)
    {
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
    private static void Prefix(Pawn_HealthTracker __instance, Pawn ___pawn, bool __runOriginal)
    {
        // Mirrors the game: an already downed pawn errors out, and only a spawned pawn outside a bed drops anything.
        if (!__runOriginal || Current.ProgramState != ProgramState.Playing || __instance.Downed || !___pawn.Spawned || ___pawn.InBed())
            return;
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

    private static void Finalizer(Pawn ___pawn, Exception __exception)
    {
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
    private static void Prefix(Pawn __instance, bool __runOriginal)
    {
        // A dead pawn's gear was sorted at death, so corpse strips (including butchering) are left to the game.
        // In a caravan the gear moves to other members instead of dropping.
        if (!__runOriginal || Current.ProgramState != ProgramState.Playing || __instance.Dead || __instance.GetCaravan() != null)
            return;
        try
        {
            GearSorter.Sort(__instance, Moment.Strip, equipment: true, apparel: true, inventory: true);
        }
        catch (Exception e)
        {
            Log.Error($"[Loot Scavenging] Sorting gear of stripped {__instance} failed: {e}"); // not translated
        }
    }
}
