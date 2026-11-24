using System;
using HarmonyLib;
using LootScavenging.Model;
using RimWorld;
using Verse;

namespace LootScavenging.Loot;

// The gear of a pawn who joins the colony (ticket 20). Everything that makes a pawn a colonist goes through Pawn.SetFaction:
// the quests of wanderers, refugees, rescued and creepjoiners, recruiting, enslaving, a trade, a rescue. His worn apparel and
// held equipment are handled just before the faction changes, so he is still judged as the group he was (a prisoner keeps his
// old faction, a wanderer has none). Two settings choose what happens: one for prisoners and slaves, one for everyone else.
//
// RecruitUtility.Recruit is hooked as well. It clears the prisoner status and unlocks his apparel before it calls SetFaction,
// so the SetFaction prefix would see neither. A pawn handled by it is marked in SortedDrop until the call ends, and the
// SetFaction prefix inside skips him: nothing is judged twice, so a chance roll is not rolled again.
//
// Not covered: a pawn the game makes a colonist without SetFaction (generated straight into the player's faction, as the
// game-end wanderers and the pawns of a new game are), and a joiner who is not on any map, whose gear the drop mode cannot
// place (the join mark still works there).

internal static class JoiningGear
{
    /// <summary>Handles a pawn about to join the player's faction. True when the caller must end the call with <see cref="End"/>.</summary>
    public static bool Begin(Pawn pawn, Faction newFaction, bool runOriginal)
    {
        if (!runOriginal || Current.ProgramState != ProgramState.Playing || newFaction == null || !newFaction.IsPlayer
            || pawn == null || pawn.Dead || pawn.Destroyed || pawn.Faction is { IsPlayer: true } || SortedDrop.Contains(pawn))
            return false;
        // Colony animals, mechanoids and quest guests who leave again are not joining. A pawn who was ever a colonist is
        // coming back, and his gear is the colony's.
        if (!pawn.RaceProps.Humanlike || pawn.records == null || PawnUtility.EverBeenColonistOrTameAnimal(pawn) || pawn.IsQuestLodger())
            return false;

        SortedDrop.Enter(pawn);
        try
        {
            JoinerGear rule = JoinerGears.For(LootScavengingMod.Settings.values.rules, pawn.IsPrisonerOfColony);
            switch (rule)
            {
                case JoinerGear.MarkUnfamiliar:
                    Unfamiliar.MarkJoining(pawn, pawn.Faction);
                    break;
                case JoinerGear.FollowDropMode:
                    // The owner group is the one he had before joining; locked apparel stays, as at a strip.
                    GearSorter.Sort(pawn, Moment.Join, equipment: true, apparel: true, inventory: false);
                    break;
            }
        }
        catch (Exception e)
        {
            Log.Error($"[Loot Scavenging] Sorting the gear of {pawn}, joining the colony, failed: {e}"); // not translated
        }
        return true;
    }

    public static void End(Pawn pawn, bool began)
    {
        if (began)
            SortedDrop.Exit(pawn);
    }
}

[HarmonyPatch(typeof(Pawn), nameof(Pawn.SetFaction))]
internal static class JoinerFactionHook
{
    [HarmonyPriority(Priority.Last)]
    private static void Prefix(Pawn __instance, Faction newFaction, bool __runOriginal, out bool __state) =>
        __state = JoiningGear.Begin(__instance, newFaction, __runOriginal);

    private static void Finalizer(Pawn __instance, bool __state) => JoiningGear.End(__instance, __state);
}

// Recruiting. Runs before the apparel is unlocked and the prisoner status cleared (see the note above).
[HarmonyPatch(typeof(RecruitUtility), nameof(RecruitUtility.Recruit))]
internal static class RecruitHook
{
    [HarmonyPriority(Priority.Last)]
    private static void Prefix(Pawn pawn, Faction faction, bool __runOriginal, out bool __state) =>
        __state = JoiningGear.Begin(pawn, faction, __runOriginal);

    private static void Finalizer(Pawn pawn, bool __state) => JoiningGear.End(pawn, __state);
}
