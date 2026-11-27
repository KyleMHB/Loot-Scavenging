using System.Collections.Generic;
using System.Linq;
using LootScavenging.Model;
using RimWorld;
using Verse;

namespace LootScavenging.Loot;

/// <summary>
/// Walks a pawn's gear at one moment, describes each item for the evaluator, applies the outcome and logs the decision.
/// Callers pass only the gear that moment actually drops, so gear the rules kept drops whole and is not judged again by the
/// same pawn's later drop.
/// </summary>
internal static class GearSorter
{
    /// <param name="ownerRow">The group whose Who row judges the gear, for a joiner (ticket 25); by default the pawn's own.</param>
    public static void Sort(Pawn pawn, Moment moment, bool equipment, bool apparel, bool inventory, OwnerGroup? ownerRow = null)
    {
        // World pawns die off-map in numbers and drop nothing, so they are skipped before any logging.
        Map map = pawn.MapHeld;
        if (map == null)
            return;

        LootRules rules = LootScavengingMod.Settings.values.rules;
        OwnerGroup owner = ownerRow ?? OwnerOf(pawn);
        // A pawn being arrested is about to be the colony's prisoner, so the Prisoners row judges what he drops (ticket 25).
        if (moment == Moment.Arrest && owner != OwnerGroup.Colony)
            owner = OwnerGroup.Prisoner;

        MapKind mapKind = map.IsPlayerHome ? MapKind.Home : MapKind.Away;

        if (equipment && pawn.equipment != null)
        {
            foreach (ThingWithComps eq in pawn.equipment.AllEquipmentListForReading.ToList())
                Judge(pawn, eq, owner, moment, mapKind, ItemCategory.Weapon, rules);
        }

        if (apparel && pawn.apparel != null)
        {
            // A living pawn keeps locked apparel when stripped or joining. At death it is sorted like the rest: the Kill
            // prefix runs before the pawn is dead, so this tests the moment, not pawn.Dead.
            IEnumerable<Apparel> worn = pawn.apparel.WornApparel.ToList();
            if (moment == Moment.Strip || moment == Moment.Join)
                worn = worn.Where(ap => !pawn.apparel.IsLocked(ap));
            foreach (Apparel ap in worn)
                Judge(pawn, ap, owner, moment, mapKind, ItemCategory.Apparel, rules);
        }

        // A visiting caravan's pack animal carries trade goods, which are stock and not gear, so they drop whole. The game
        // marks such an animal as a Carrier; the colony's own pack animals are still sorted.
        bool caravanCargo = pawn.RaceProps.packAnimal && pawn.GetTraderCaravanRole() == TraderCaravanRole.Carrier
            && pawn.Faction != Faction.OfPlayer;
        if (inventory && pawn.inventory != null && !caravanCargo)
        {
            foreach (Thing thing in pawn.inventory.innerContainer.ToList())
                Judge(pawn, thing, owner, moment, mapKind, ItemClassifier.InventoryCategoryOf(thing), rules);
        }
    }

    /// <summary>Sorts one weapon a living pawn is about to drop on its own, outside the moments that drop everything.</summary>
    public static void SortEquipment(Pawn pawn, ThingWithComps eq, Moment moment)
    {
        Map map = pawn.MapHeld;
        if (map == null)
            return;
        Judge(pawn, eq, OwnerOf(pawn), moment, map.IsPlayerHome ? MapKind.Home : MapKind.Away, ItemCategory.Weapon,
            LootScavengingMod.Settings.values.rules);
    }

    public static OwnerGroup OwnerOf(Pawn pawn)
    {
        // Faction-level hostility, because Thing.HostileTo is always false for a destroyed (dead) pawn.
        // A pawn that was ever a colonist counts as the colony's, so a colonist who ran wild, or a dead colonist raised as a
        // shambler, keeps the colony's gear. A recruit who later turned hostile is left alone too.
        Faction faction = pawn.Faction;
        return OwnerGroups.Classify(
            isPlayerFaction: faction != null && faction.IsPlayer || pawn.records != null && PawnUtility.EverBeenColonistOrTameAnimal(pawn),
            isPrisonerOfColony: pawn.IsPrisonerOfColony,
            hasFaction: faction != null,
            factionHostileToPlayer: faction != null && faction.HostileTo(Faction.OfPlayer));
    }

    private static void Judge(Pawn pawn, Thing thing, OwnerGroup owner, Moment moment, MapKind mapKind, ItemCategory category, LootRules rules)
    {
        // Scrap already on the pawn (including scrap made earlier in this pass) is never judged again.
        if (ScrapDefs.IsScrap(thing.def))
            return;

        EraResult era = EraReader.ItemEra(thing.def, pawn.Faction);
        var candidate = new DropCandidate
        {
            ItemEra = era,
            Owner = owner,
            Moment = moment,
            Map = mapKind,
            Category = category,
            Quality = thing.TryGetQuality(out QualityCategory quality) ? (ItemQuality)quality : null,
            HitPointFraction = thing.def.useHitPoints && thing.MaxHitPoints > 0 ? (float)thing.HitPoints / thing.MaxHitPoints : null,
            OutOfReach = era.OutOfReach,
        };
        ItemClassifier.DescribeProtection(thing, candidate);

        // Every item is a whole thing here: the hooks run before the game drops anything, so a stack is judged and
        // removed as one and a stack is never split by this mod.
        LootDecision decision = LootEvaluator.Evaluate(candidate, rules, () => Rand.Value);
        if (Prefs.DevMode)
            Log.Message($"[Loot Scavenging] {moment} of {pawn} ({owner}, {mapKind} map): {thing} -> {decision.Outcome} ({decision.Reason})"); // not translated

        OutcomeApplier.Apply(pawn, thing, decision, candidate.ItemEra, moment, owner);
    }
}
