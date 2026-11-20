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
            GearSorter.Sort(__instance, Moment.Death, equipment: true, apparel: true, inventory: true);
        }
        catch (Exception e)
        {
            Log.Error($"[Loot Scavenging] Sorting gear at the death of {__instance} failed: {e}"); // not translated
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
            // Downing drops equipment and inventory, never apparel.
            GearSorter.Sort(___pawn, Moment.Downed, equipment: true, apparel: false, inventory: true);
        }
        catch (Exception e)
        {
            Log.Error($"[Loot Scavenging] Sorting gear of downed {___pawn} failed: {e}"); // not translated
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
