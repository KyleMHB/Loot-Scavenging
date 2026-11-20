using System.Collections.Generic;
using System.Linq;
using LootScavenging.Model;
using RimWorld;
using Verse;

namespace LootScavenging.Loot;

/// <summary>
/// Walks a pawn's gear at one moment, describes each item for the evaluator, applies the outcome and logs the decision.
/// Callers pass only the gear that moment actually drops, so no item is judged twice.
/// </summary>
internal static class GearSorter
{
    public static void Sort(Pawn pawn, Moment moment, bool equipment, bool apparel, bool inventory)
    {
        // World pawns die off-map in numbers and drop nothing, so they are skipped before any logging.
        Map map = pawn.MapHeld;
        if (map == null)
            return;

        LootRules rules = LootScavengingMod.Settings.rules;
        OwnerGroup owner = OwnerOf(pawn);
        MapKind mapKind = map.IsPlayerHome ? MapKind.Home : MapKind.Away;

        if (equipment && pawn.equipment != null)
        {
            foreach (ThingWithComps eq in pawn.equipment.AllEquipmentListForReading.ToList())
                Judge(pawn, eq, owner, moment, mapKind, ItemCategory.Weapon, rules);
        }

        if (apparel && pawn.apparel != null)
        {
            // A living pawn keeps locked apparel when stripped; only a dead pawn's locked apparel leaves it.
            IEnumerable<Apparel> worn = pawn.apparel.WornApparel.ToList();
            if (!pawn.Dead)
                worn = worn.Where(ap => !pawn.apparel.IsLocked(ap));
            foreach (Apparel ap in worn)
                Judge(pawn, ap, owner, moment, mapKind, ItemCategory.Apparel, rules);
        }

        if (inventory && pawn.inventory != null)
        {
            foreach (Thing thing in pawn.inventory.innerContainer.ToList())
                Judge(pawn, thing, owner, moment, mapKind, ItemCategory.Other, rules);
        }
    }

    public static OwnerGroup OwnerOf(Pawn pawn)
    {
        // Faction-level hostility, because Thing.HostileTo is always false for a destroyed (dead) pawn.
        Faction faction = pawn.Faction;
        return OwnerGroups.Classify(
            isPlayerFaction: faction != null && faction.IsPlayer,
            isPrisonerOfColony: pawn.IsPrisonerOfColony,
            hasFaction: faction != null,
            factionHostileToPlayer: faction != null && faction.HostileTo(Faction.OfPlayer));
    }

    private static void Judge(Pawn pawn, Thing thing, OwnerGroup owner, Moment moment, MapKind mapKind, ItemCategory category, LootRules rules)
    {
        var candidate = new DropCandidate
        {
            Owner = owner,
            Moment = moment,
            Map = mapKind,
            Category = category,
            Quality = thing.TryGetQuality(out QualityCategory quality) ? (ItemQuality)quality : null,
            HitPointFraction = thing.def.useHitPoints && thing.MaxHitPoints > 0 ? (float)thing.HitPoints / thing.MaxHitPoints : null,
        };

        LootDecision decision = LootEvaluator.Evaluate(candidate, rules);
        if (Prefs.DevMode)
            Log.Message($"[Loot Scavenging] {moment} of {pawn} ({owner}, {mapKind} map): {thing} -> {decision.Outcome} ({decision.Reason})"); // not translated

        OutcomeApplier.Apply(pawn, thing, decision.Outcome);
    }
}
