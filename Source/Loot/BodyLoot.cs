using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using LootScavenging.Model;
using RimWorld;
using Verse;

namespace LootScavenging.Loot;

/// <summary>
/// Loot kept on a pawn's body for the "where loot lands at death" setting. Pawn.Kill drops the weapon and inventory
/// beside the corpse, so what should stay is taken off the pawn before that drop and put back into the dead pawn's
/// inventory after it, where stripping, butchering or cremating the corpse finds it. With "everything stays on the body"
/// the same happens when a pawn is downed: the weapon goes into its pack with the rest, because the game drops a weapon a
/// downed pawn holds whenever it is spawned again (after a reload or being carried), and a pawn that gets back up should
/// not be armed at once. "Keep the weapon in his pack" (ticket 21, the default) uses the same hold at a downing, with no
/// judging at all.
/// </summary>
internal static class BodyLoot
{
    // Things taken off a pawn for the length of its Kill or MakeDowned call.
    private static readonly Dictionary<Pawn, List<Thing>> held = new Dictionary<Pawn, List<Thing>>();

    // Pawns dying or being downed whose loot all stays on the body, decided before their gear is sorted so that
    // disassembly products and ejected charges are held too instead of placed on the ground.
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
    /// Called at the start of a death or a downing, before the gear is sorted: with "everything stays on the body", marks
    /// a covered pawn so its loot is held. Pawns whose gear the game destroys on drop (most mechanoids) and pawns that
    /// cannot be stripped drop as usual, so nothing is saved that would otherwise vanish or be stuck on a corpse.
    /// </summary>
    public static void BeginDrop(Pawn pawn)
    {
        if (LootScavengingMod.Settings.values.deathDrop != DeathDrop.AllOnBody || !pawn.Spawned || !CanHoldLoot(pawn))
            return;
        MapKind map = pawn.Map.IsPlayerHome ? MapKind.Home : MapKind.Away;
        if (LootEvaluator.CoversPawn(GearSorter.OwnerOf(pawn), map, LootScavengingMod.Settings.values.rules))
            keepsAll.Add(pawn);
    }

    /// <summary>Takes a marked pawn's weapon and inventory off it before the game's drop. Runs after the gear is sorted, so
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

    // Pawns downed under "keep the weapon in his pack" (ticket 21). Their things are held over the downed drop like the
    // ones above, but nothing was judged, so they go back unmarked and the later strip, arrest, join or death judges them.
    private static readonly HashSet<Pawn> stowing = new HashSet<Pawn>();

    // Held things to put back as not for sale: a trader's own pack goods keep the mark, and a weapon moved from his hands
    // never becomes trade stock.
    private static readonly HashSet<Thing> notForSale = new HashSet<Thing>();

    /// <summary>
    /// Whether a downing puts this pawn's weapon in his pack. Pawns the rules do not cover, pawns with no pack, and pawns
    /// whose gear the game destroys on drop or that cannot be stripped (most mechanoids) drop as before.
    /// </summary>
    public static bool StowsAtDowning(Pawn pawn)
    {
        Map map = pawn.MapHeld;
        if (map == null || pawn.inventory == null || !CanHoldLoot(pawn))
            return false;
        return DownedGears.KeepsInPack(LootScavengingMod.Settings.values.rules, GearSorter.OwnerOf(pawn),
            map.IsPlayerHome ? MapKind.Home : MapKind.Away);
    }

    /// <summary>Takes a downing pawn's weapon and pack off him before the game's drop, unjudged. <see cref="PlaceOnBody"/>
    /// puts them all in his pack.</summary>
    public static void Stow(Pawn pawn)
    {
        stowing.Add(pawn);
        if (pawn.equipment != null)
        {
            foreach (ThingWithComps eq in pawn.equipment.AllEquipmentListForReading.ToList())
            {
                pawn.equipment.Remove(eq);
                notForSale.Add(eq);
                Hold(pawn, eq);
            }
        }
        foreach (Thing thing in pawn.inventory.innerContainer.ToList())
        {
            if (pawn.inventory.NotForSale(thing))
                notForSale.Add(thing);
            pawn.inventory.innerContainer.Remove(thing);
            Hold(pawn, thing);
        }
    }

    /// <summary>Moves one weapon from a downed pawn's hands to his pack instead of the drop the game was about to make.</summary>
    public static void StowEquipment(Pawn pawn, ThingWithComps eq)
    {
        pawn.equipment.Remove(eq);
        notForSale.Add(eq);
        Give(pawn, eq);
    }

    // Things the rules already judged at a downing and put back in the pawn's pack: only under "drop the weapon" with
    // "everything stays on the body". A later death, strip or second downing would otherwise judge them again: chance
    // re-rolled, degrade applied twice. Weak keys, so the table empties itself; it is not saved, so after a reload such
    // things are judged once more.
    private static readonly ConditionalWeakTable<Thing, object> judgedAtDowning = new ConditionalWeakTable<Thing, object>();

    public static bool WasJudgedAtDowning(Thing thing) => judgedAtDowning.TryGetValue(thing, out _);

    // Called after Pawn.Kill or MakeDowned: the game's drop has run, so things added now stay on the pawn. The entry is
    // always used up: if the call was cancelled or threw, the pawn simply gets its things back in its inventory.
    public static void PlaceOnBody(Pawn pawn, bool downed)
    {
        keepsAll.Remove(pawn);
        bool judged = downed && !stowing.Remove(pawn);
        if (!held.TryGetValue(pawn, out List<Thing> list))
            return;
        held.Remove(pawn);
        foreach (Thing thing in list)
        {
            if (judged && !judgedAtDowning.TryGetValue(thing, out _))
                judgedAtDowning.Add(thing, null);
            Give(pawn, thing);
        }
    }

    public static void Give(Pawn pawn, Thing thing)
    {
        if (notForSale.Remove(thing) && pawn.inventory != null)
        {
            pawn.inventory.TryAddItemNotForSale(thing);
            if (pawn.inventory.Contains(thing))
                return;
        }
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
