using System;
using System.Collections.Generic;
using HarmonyLib;
using LootScavenging.Model;
using RimWorld;
using Verse;

namespace LootScavenging.Loot;

// The gear of a pawn who joins the colony (ticket 20). Everything that makes a pawn a colonist goes through Pawn.SetFaction:
// the quests of wanderers, refugees, rescued and creepjoiners, recruiting, enslaving, a trade, a rescue. His worn apparel,
// held equipment and pack (ticket 21) are handled just before the faction changes, so he is still judged as the group he was (a prisoner keeps his
// old faction, a wanderer has none). Two Who rows choose what happens (ticket 25): Recruited prisoners, and Other joiners.
// Apply sorts his gear under the item rules, Mark marks his out-of-reach gear unfamiliar, and Exclude leaves it alone.
//
// RecruitUtility.Recruit is hooked as well. It clears the prisoner status and unlocks his apparel before it calls SetFaction,
// so the SetFaction prefix would see neither. A pawn handled by it is marked in SortedDrop until the call ends, and the
// SetFaction prefix inside skips him: nothing is judged twice, so a chance roll is not rolled again.
//
// A joiner who is not on any map has nowhere to drop his gear. A walk-in wanderer joins this way: his quest calls SetFaction
// while he is still a world pawn, then spawns him at the map edge, by drop pod or by shuttle. So do pawns who join a caravan.
// Under Apply he is noted in UnsortedJoiners with the faction he had, and his gear is sorted once, at his first spawn
// (ticket 05). The join mark needs no map and is set at once.
//
// Not covered: a pawn the game makes a colonist without SetFaction (generated straight into the player's faction, as the
// game-end wanderers and the pawns of a new game are).

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
            OwnerGroup row = pawn.IsPrisonerOfColony ? OwnerGroup.RecruitedPrisoner : OwnerGroup.OtherJoiner;
            switch (LootScavengingMod.Settings.values.rules.RowFor(row))
            {
                case WhoRule.Mark:
                    Unfamiliar.MarkJoining(pawn, pawn.Faction);
                    break;
                case WhoRule.Apply:
                    // His joiner row judges the gear; locked apparel stays, as at a strip. His pack is sorted too (ticket 21):
                    // a pawn downed with his weapon kept may carry it there.
                    if (pawn.MapHeld == null)
                        UnsortedJoiners.Add(pawn, pawn.Faction);
                    else
                        GearSorter.Sort(pawn, Moment.Join, equipment: true, apparel: true, inventory: true, ownerRow: row);
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

/// <summary>
/// Joiners whose gear waits for their first spawn (ticket 05), by thing id, with the faction each had before joining. Saved
/// with the game, so a save made while a wanderer's drop pod or shuttle is on its way keeps him on the list. A save without
/// this list (0.6.0 and earlier) loads it empty.
/// </summary>
public class UnsortedJoiners : GameComponent
{
    private static UnsortedJoiners current;

    private Dictionary<int, Faction> formerFactions = new Dictionary<int, Faction>();
    private List<int> idsWorking;
    private List<Faction> factionsWorking;

    public UnsortedJoiners(Game game)
    {
        current = this;
    }

    internal static void Add(Pawn pawn, Faction formerFaction)
    {
        if (current != null)
            current.formerFactions[pawn.thingIDNumber] = formerFaction;
    }

    /// <summary>Removes the pawn from the list. True, with the faction he had, when he was on it.</summary>
    internal static bool TryTake(Pawn pawn, out Faction formerFaction)
    {
        formerFaction = null;
        if (current == null || current.formerFactions.Count == 0 || !current.formerFactions.TryGetValue(pawn.thingIDNumber, out formerFaction))
            return false;
        current.formerFactions.Remove(pawn.thingIDNumber);
        return true;
    }

    public override void ExposeData()
    {
        // A wanderer's former faction is null; a faction reference saves and loads null as it is.
        Scribe_Collections.Look(ref formerFactions, "unsortedJoiners", LookMode.Value, LookMode.Reference, ref idsWorking, ref factionsWorking); // not translated
        if (Scribe.mode == LoadSaveMode.PostLoadInit && formerFactions == null)
            formerFactions = new Dictionary<int, Faction>();
    }
}

// A listed joiner's first spawn on a map: an edge walk-in, a drop pod or shuttle opening, a caravan entering a map.
[HarmonyPatch(typeof(Pawn), nameof(Pawn.SpawnSetup))]
internal static class JoinerSpawnHook
{
    private static void Postfix(Pawn __instance, bool respawningAfterLoad)
    {
        if (respawningAfterLoad || Current.ProgramState != ProgramState.Playing || !UnsortedJoiners.TryTake(__instance, out Faction formerFaction))
            return;
        // Taken off the list first, so a failed sort is never tried again. The row is read now, so a player who turned it from
        // Apply while he travelled is obeyed. Prisoners are recruited on a map, so an off-map joiner is always an other joiner.
        if (__instance.Dead || __instance.Faction is not { IsPlayer: true }
            || LootScavengingMod.Settings.values.rules.RowFor(OwnerGroup.OtherJoiner) != WhoRule.Apply)
            return;

        SortedDrop.Enter(__instance);
        try
        {
            GearSorter.SortArrivedJoiner(__instance, formerFaction, OwnerGroup.OtherJoiner);
        }
        catch (Exception e)
        {
            Log.Error($"[Loot Scavenging] Sorting the gear of {__instance}, arriving after joining the colony, failed: {e}"); // not translated
        }
        finally
        {
            SortedDrop.Exit(__instance);
        }
    }
}
