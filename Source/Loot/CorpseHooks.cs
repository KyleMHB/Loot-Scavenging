using System;
using HarmonyLib;
using LootScavenging.Model;
using RimWorld;
using Verse;
using Verse.AI;

namespace LootScavenging.Loot;

// Corpse removal and the butchering option (spec, "Corpses and butchering"). The decisions live in CorpseEvaluator.

[HarmonyPatch(typeof(Pawn), nameof(Pawn.Kill))]
internal static class CorpseRemovalHook
{
    // The owner is read before the death: a colony prisoner's status may not survive Kill. Last, like the gear sort, so a
    // mod that cancels the death runs first and this sees it.
    [HarmonyPriority(Priority.Last)]
    private static void Prefix(Pawn __instance, bool __runOriginal, out OwnerGroup? __state)
    {
        __state = null;
        try
        {
            if (__runOriginal && Current.ProgramState == ProgramState.Playing && !__instance.Dead && !__instance.Destroyed)
                __state = GearSorter.OwnerOf(__instance);
        }
        catch (Exception e)
        {
            Log.Error($"[Loot Scavenging] Reading the owner of {__instance} before death failed: {e}"); // not translated
        }
    }

    // By now the kill prefix has sorted the gear and the game has dropped equipment and inventory, so the corpse holds
    // only worn apparel. A pawn that left no corpse, or died off-map or in a caravan, has nothing spawned and is skipped.
    // Runs after PawnKillHook's postfix, so loot left on the body is in the inventory and the strip below drops it to the ground.
    [HarmonyPriority(Priority.Low)]
    private static void Postfix(Pawn __instance, OwnerGroup? __state)
    {
        if (__state == null || !__instance.Dead)
            return;
        try
        {
            Corpse corpse = __instance.Corpse;
            if (corpse == null || corpse.Destroyed || !corpse.Spawned)
                return;
            if (HasQuestTag(corpse) || HasQuestTag(__instance))
                return;

            var candidate = new CorpseCandidate
            {
                Owner = __state.Value,
                Map = corpse.Map.IsPlayerHome ? MapKind.Home : MapKind.Away,
                Kind = KindOf(__instance),
            };
            CorpseDecision decision = CorpseEvaluator.Evaluate(candidate, LootScavengingMod.Settings.values.rules, LootScavengingMod.Settings.values.corpses);
            if (Prefs.DevMode)
                Log.Message($"[Loot Scavenging] Corpse of {__instance} ({candidate.Owner}, {candidate.Map} map, {candidate.Kind}) -> {decision}"); // not translated
            if (decision != CorpseDecision.Remove)
                return;

            // Worn apparel the loot rules kept goes to the ground rather than vanishing with the body. Dead pawns are
            // not sorted again on strip. No faction notification: this is cleanup, not the colony stripping a body.
            __instance.Strip(notifyFaction: false);
            corpse.Destroy();
        }
        catch (Exception e)
        {
            Log.Error($"[Loot Scavenging] Removing the corpse of {__instance} failed: {e}"); // not translated
        }
    }

    private static bool HasQuestTag(Thing thing) => thing.questTags != null && thing.questTags.Count > 0;

    private static CorpseKind KindOf(Pawn pawn)
    {
        RaceProperties race = pawn.RaceProps;
        if (race.Humanlike)
            return CorpseKind.Humanlike;
        if (race.IsMechanoid)
            return CorpseKind.Mechanoid;
        if (race.Animal)
            return CorpseKind.Animal;
        return CorpseKind.Other;
    }
}

// A butcher bill strips the corpse of its apparel while it gathers its ingredients (autoStripCorpses). Marking that
// window tells the Strip patch below apart from a player's strip job and from any other strip. Butchering a humanlike
// corpse strips it and sorts what comes off under the owner's Who row (ticket 25, decision 6).
[HarmonyPatch(typeof(Toils_Recipe), "CalculateIngredients")]
internal static class ButcherIngredientsHook
{
    internal static int Depth;

    private static void Prefix(Job job, out bool __state)
    {
        __state = job?.RecipeDef?.specialProducts != null && job.RecipeDef.specialProducts.Contains(SpecialProductType.Butchery);
        if (__state)
            Depth++;
    }

    private static void Finalizer(bool __state)
    {
        if (__state)
            Depth--;
    }
}

[HarmonyPatch(typeof(Pawn), nameof(Pawn.Strip))]
internal static class ButcherStripHook
{
    [HarmonyPriority(Priority.Last)]
    private static void Prefix(Pawn __instance, bool __runOriginal, out bool __state)
    {
        __state = false;
        if (!__runOriginal || ButcherIngredientsHook.Depth <= 0 || !__instance.Dead || !__instance.RaceProps.Humanlike)
            return;
        try
        {
            Map map = __instance.MapHeld;
            MapKind mapKind = map == null ? MapKind.None : map.IsPlayerHome ? MapKind.Home : MapKind.Away;
            OwnerGroup owner = GearSorter.OwnerOf(__instance);
            if (!CorpseEvaluator.ButcherSorts(owner, mapKind, LootScavengingMod.Settings.values.rules))
                return;
            if (Prefs.DevMode)
                Log.Message($"[Loot Scavenging] Butchering the corpse of {__instance}: sorting its apparel and inventory"); // not translated
            // The weapon was dropped at death. Inventory left on the body at death is judged here, under the same rules.
            SortedDrop.Enter(__instance);
            __state = true;
            GearSorter.Sort(__instance, Moment.Strip, equipment: false, apparel: true, inventory: true);
        }
        catch (Exception e)
        {
            Log.Error($"[Loot Scavenging] Sorting the gear of butchered {__instance} failed: {e}"); // not translated
        }
    }

    private static void Finalizer(Pawn __instance, bool __state)
    {
        if (__state)
            SortedDrop.Exit(__instance);
    }

}

// A dead colonist's Gear tab shows a drop button on each inventory row, and the game drops at the pawn's own map, which
// is null inside a corpse: "Dropped X in a null map" and nothing moves. A colonist's weapon and pack stay on the body by
// default, so the button is common. It drops beside the corpse instead, unforbidden like the game's own Gear tab drop.
[HarmonyPatch(typeof(ITab_Pawn_Gear), "InterfaceDrop")]
internal static class GearTabCorpseDropHook
{
    private static bool Prefix(Thing t)
    {
        try
        {
            if (t == null || t.def.destroyOnDrop || t.ParentHolder is not Pawn_InventoryTracker inventory || !inventory.pawn.Dead)
                return true;
            Corpse corpse = inventory.pawn.Corpse;
            if (corpse == null || !corpse.Spawned)
                return true;
            inventory.innerContainer.TryDrop(t, corpse.Position, corpse.Map, ThingPlaceMode.Near, out Thing _);
            return false;
        }
        catch (Exception e)
        {
            Log.Error($"[Loot Scavenging] Dropping {t} from a dead pawn's Gear tab failed: {e}"); // not translated
            return true;
        }
    }
}
