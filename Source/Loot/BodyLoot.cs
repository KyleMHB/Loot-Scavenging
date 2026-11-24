using System.Collections.Generic;
using System.Linq;
using LootScavenging.Model;
using RimWorld;
using Verse;

namespace LootScavenging.Loot;

/// <summary>
/// Loot kept on a pawn's body for the "where loot lands at death" setting. Pawn.Kill drops the weapon and inventory
/// beside the corpse, so what should stay is taken off the pawn before that drop and put back into the dead pawn's
/// inventory after it, where stripping, butchering or cremating the corpse finds it. The setting applies at death only
/// (ticket 24). A colonist under "Colonists keep their gear" keeps his weapon and pack in his corpse, unsorted. A covered
/// pawn's downing drops nothing under "Keep his weapon" (ticket 23, the default), and nor does a colonist's under "Colonists
/// keep their gear": he keeps his weapon in hand and his pack, unjudged, as the game does for a pawn downed in a bed.
/// </summary>
internal static class BodyLoot
{
    // Things taken off a pawn for the length of its Kill call: a dying pawn's weapon and pack, and the scrap and
    // disassembly products made for a pawn whose loot all stays on the body.
    private static readonly Dictionary<Pawn, List<Thing>> held = new Dictionary<Pawn, List<Thing>>();

    // Dying pawns whose loot all stays on the body, decided before their gear is sorted so that disassembly products and
    // ejected charges are held too instead of placed on the ground; and dying colonists who keep their gear.
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
    /// Called at the start of a death, before the gear is sorted: with "everything stays on the body", marks
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

    /// <summary>Takes a marked dying pawn's weapon and inventory off it before the game's death drop. Runs after the gear is
    /// sorted, so what is left is what the rules kept.</summary>
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

    // Pawns being downed who keep what the game would drop at the downing, for the length of their MakeDowned call: under
    // "Keep his weapon", or a colonist under "Colonists keep their gear". The skip hooks in GearHooks.cs
    // stop the game's drop of their weapon and pack, as the game itself does for a pawn downed in a bed. Nothing moves, so
    // a trader's not-for-sale marks stay as they are.
    private static readonly HashSet<Pawn> keepingGear = new HashSet<Pawn>();

    public static bool IsKeepingGear(Pawn pawn) => pawn != null && keepingGear.Contains(pawn);

    public static void BeginKeepingGear(Pawn pawn) => keepingGear.Add(pawn);

    public static void EndKeepingGear(Pawn pawn) => keepingGear.Remove(pawn);

    /// <summary>
    /// Whether a downing leaves this pawn's weapon in his hands and his pack as it is, unjudged ("Keep his weapon"), and
    /// whether he keeps holding the weapon when he is spawned again while down. Pawns the rules do not cover, and pawns whose
    /// gear the game destroys on drop or that cannot be stripped (most mechanoids), drop as before.
    /// </summary>
    public static bool KeepsWeaponAtDowning(Pawn pawn)
    {
        Map map = pawn.MapHeld;
        if (map == null || !CanHoldLoot(pawn))
            return false;
        return DownedGears.KeepsWeapon(LootScavengingMod.Settings.values.rules, GearSorter.OwnerOf(pawn),
            map.IsPlayerHome ? MapKind.Home : MapKind.Away);
    }

    /// <summary>
    /// Whether "Colonists keep their gear" (ticket 24) covers this pawn: a colonist or colony slave, whose weapon and pack
    /// then stay with him, unsorted, when he is downed, set down again or killed. Animals and mechanoids are not covered,
    /// and nor are pawns whose gear the game destroys on drop or that cannot be stripped.
    /// </summary>
    public static bool ColonistKeepsGear(Pawn pawn) =>
        LootScavengingMod.Settings.values.colonistsKeepGear && (pawn.IsColonist || pawn.IsSlaveOfColony) && CanHoldLoot(pawn);

    /// <summary>At a death: marks a colonist who keeps his gear, so HoldGear takes his weapon and pack off him unsorted and
    /// PlaceOnBody puts them in his corpse's inventory. Returns whether he was marked.</summary>
    public static bool BeginKeepingColonistGear(Pawn pawn)
    {
        if (!pawn.Spawned || !ColonistKeepsGear(pawn))
            return false;
        keepsAll.Add(pawn);
        return true;
    }

    /// <summary>Whether the rules cover this pawn where he is (owner group and map).</summary>
    public static bool Covers(Pawn pawn)
    {
        Map map = pawn.MapHeld;
        return map != null && LootEvaluator.CoversPawn(GearSorter.OwnerOf(pawn), map.IsPlayerHome ? MapKind.Home : MapKind.Away,
            LootScavengingMod.Settings.values.rules);
    }

    // Called after Pawn.Kill: the game's drop has run, so things added now stay on the corpse. The entry is always used
    // up: if the call was cancelled or threw, the pawn simply gets its things back in its inventory.
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
