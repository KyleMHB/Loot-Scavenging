using System;
using System.Collections.Generic;
using System.Linq;
using LootScavenging.Model;
using RimWorld;
using RimWorldDevTests;
using Verse;

namespace LootScavenging.DevTests;

// Setup every batch shares. Each batch is a fresh Crashlanded colony, so a batch that needs a tribal colony calls MakeTribal itself.
public static class Fixtures
{
    public static ThingDef Scrap(ScrapTier tier) => DefDatabase<ThingDef>.GetNamed(ScrapTiers.DefName(tier));
    public static ThingDef StudiedScrap(ScrapTier tier) => TierDef(tier).studiedScrap;
    public static ScrapTierDef TierDef(ScrapTier tier) => DefDatabase<ScrapTierDef>.AllDefsListForReading.First(d => d.tier == tier);
    public static IEnumerable<ThingDef> AllScrap => ((ScrapTier[])Enum.GetValues(typeof(ScrapTier))).Select(Scrap);
    public static int ScrapOnMap(Map map) => AllScrap.Sum(d => Kit.CountOnMap(map, d));

    // Non uno Pinata leaves equipment and packs on the body at death and downing (TESTING.md, 2026-11-25), so under it
    // the Held counts below also take in what pawns and corpses hold. Without Pinata they are Kit's, unchanged. Only cases
    // that check gear is kept whole or scrapped use them; cases that tell the ground from the corpse keep Kit's counts.
    public static bool PinataLoaded => ModsConfig.IsActive("avilmask.nonunopinata");

    // Equipment, pack and carried things of spawned pawns and of corpses on the map. Worn apparel is left out.
    static IEnumerable<Thing> HeldOnMap(Map map)
    {
        var pawns = map.mapPawns.AllPawnsSpawned.Concat(map.listerThings.ThingsInGroup(ThingRequestGroup.Corpse).OfType<Corpse>().Select(c => c.InnerPawn));
        foreach (var p in pawns.Where(p => p != null))
        {
            if (p.equipment != null) foreach (var t in p.equipment.AllEquipmentListForReading) yield return t;
            if (p.inventory != null) foreach (var t in p.inventory.innerContainer) yield return t;
            if (p.carryTracker?.CarriedThing != null) yield return p.carryTracker.CarriedThing;
        }
    }

    public static int HeldCount(Map map, ThingDef def)
    {
        int count = Kit.CountOnMap(map, def);
        if (!PinataLoaded) return count;
        // Kit already counts spawned pawns' packs and carried things.
        count += map.mapPawns.AllPawnsSpawned.Sum(p => p.equipment?.AllEquipmentListForReading.Where(t => t.def == def).Sum(t => t.stackCount) ?? 0);
        count += map.listerThings.ThingsInGroup(ThingRequestGroup.Corpse).OfType<Corpse>().Select(c => c.InnerPawn).Where(p => p != null)
            .Sum(p => (p.equipment?.AllEquipmentListForReading.Where(t => t.def == def).Sum(t => t.stackCount) ?? 0)
                + (p.inventory?.innerContainer.Where(t => t.def == def).Sum(t => t.stackCount) ?? 0));
        return count;
    }

    public static HashSet<int> HeldStackIds(Map map, ThingDef def)
    {
        var ids = Kit.StackIds(map, def);
        if (PinataLoaded) ids.UnionWith(HeldOnMap(map).Where(t => t.def == def).Select(t => t.thingIDNumber));
        return ids;
    }

    public static IEnumerable<Thing> HeldNewStacks(Map map, ThingDef def, HashSet<int> before)
    {
        var found = Kit.NewStacks(map, def, before);
        return PinataLoaded ? found.Concat(HeldOnMap(map).Where(t => t.def == def && !before.Contains(t.thingIDNumber))).ToList() : found;
    }

    // Kept whole: on the ground, or under Pinata still on the body or in the corpse.
    public static bool Dropped(Thing t) => t.Spawned || PinataLoaded && !t.Destroyed && t.MapHeld != null;
    public static ResearchProjectDef Project(string name) => DefDatabase<ResearchProjectDef>.GetNamed(name);
    public static LootScavengingSettings Settings => LootScavengingMod.Settings;
    public static LootRules Rules => Settings.values.rules;
    // The pirates, or another permanently hostile humanlike faction when the world has none (DLC worlds vary).
    public static Faction Pirates => Find.FactionManager.FirstFactionOfDef(FactionDefOf.Pirate)
        ?? Find.FactionManager.AllFactionsListForReading.FirstOrDefault(f => !f.IsPlayer && !f.Hidden && f.def.humanlikeFaction && f.def.permanentEnemy);
    public static PawnKindDef Mercenary => DefDatabase<PawnKindDef>.GetNamed("Mercenary_Gunner");

    // Crashlanded starts with industrial research; the tribal colony has one neolithic project and nothing else.
    public static void MakeTribal(BatchContext ctx)
    {
        Find.ResearchManager.ResetAllProgress();
        Unfamiliar.ResetCache();
        ctx.Setup("Developer reset all research progress (Crashlanded starts with industrial research)");
        Kit.Research(ctx, "PsychoidBrewing");
    }

    public static List<IntVec3> Sites(CellRect area, int count, int spacing = 6) =>
        Enumerable.Range(0, count).Select(i => new IntVec3(area.minX + 3 + i * spacing, 0, area.CenterCell.z)).ToList();

    public static Pawn Spawn(BatchContext ctx, PawnKindDef kind, Faction faction, IntVec3 cell, Map map = null)
    {
        var pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(kind, faction, forceGenerateNewPawn: true,
            canGeneratePawnRelations: false, mustBeCapableOfViolence: false, allowAddictions: false));
        // Bare, so only the gear a case gives is sorted.
        pawn.equipment?.DestroyAllEquipment();
        pawn.apparel?.DestroyAll();
        pawn.inventory?.DestroyAll();
        GenSpawn.Spawn(pawn, cell, map ?? ctx.Map);
        ctx.Setup($"Developer spawned {kind.defName} {pawn.LabelShort} ({faction?.Name ?? "no faction"}) at {cell} with all gear removed");
        return pawn;
    }

    public static Thing Make(ThingDef def, QualityCategory? quality = null)
    {
        var thing = ThingMaker.MakeThing(def, def.MadeFromStuff ? GenStuff.DefaultStuffFor(def) : null);
        if (quality.HasValue) thing.TryGetComp<CompQuality>()?.SetQuality(quality.Value, ArtGenerationContext.Outsider);
        if (thing.def.useHitPoints) thing.HitPoints = thing.MaxHitPoints;
        return thing;
    }

    public static ThingWithComps Arm(Pawn pawn, ThingDef def, QualityCategory? quality)
    {
        var gun = (ThingWithComps)Make(def, quality);
        pawn.equipment.AddEquipment(gun);
        return gun;
    }

    public static Apparel Dress(Pawn pawn, ThingDef def, QualityCategory? quality = null)
    {
        var ap = (Apparel)Make(def, quality);
        pawn.apparel.Wear(ap);
        return ap;
    }

    public static Thing Stack(ThingDef def, int count)
    {
        var thing = ThingMaker.MakeThing(def);
        thing.stackCount = count;
        return thing;
    }

    public static Thing SpawnStack(Map map, ThingDef def, int count, IntVec3 near)
    {
        var thing = Stack(def, count);
        GenPlace.TryPlaceThing(thing, near, map, ThingPlaceMode.Near, out Thing placed);
        return placed ?? thing;
    }

    // New stacks of a def since a StackIds snapshot, within a radius of a cell.
    public static List<Thing> NewNear(Map map, ThingDef def, HashSet<int> before, IntVec3 near, float radius = 5f) =>
        Kit.NewStacks(map, def, before).Where(t => t.Position.DistanceTo(near) <= radius).ToList();

    public static List<Thing> HeldNewNear(Map map, ThingDef def, HashSet<int> before, IntVec3 near, float radius = 5f) =>
        HeldNewStacks(map, def, before).Where(t => t.PositionHeld.DistanceTo(near) <= radius).ToList();

    public static int HeldScrap(Map map) => AllScrap.Sum(d => HeldCount(map, d));

    public static int Units(IEnumerable<Thing> things) => things.Sum(t => t.stackCount);

    public static string Describe(IEnumerable<Thing> things) =>
        string.Join(", ", things.GroupBy(t => t.def.defName).Select(g => g.Sum(t => t.stackCount) + " " + g.Key));

    // What one unit of a tier's scrap is worth: the larger of its recycle value and its market value setting (the same rule the mod uses).
    public static float ScrapWorth(ScrapTier tier) => ScrapAmounts.WorthPerUnit(TierDef(tier).valuePerUnit, Settings.values.ScrapMarketValue(tier));

    // The tier a thing's scrap is in, for the colony as it is now.
    public static ScrapTier TierOf(ThingDef def, Faction owner) => ScrapTiers.TierFor(EraReader.ItemEra(def, owner).Era, EraReader.ColonyEra());

    // The scrap units a thing (its whole stack) makes: the model's curve, capped so the scrap is never worth more than the thing.
    // Anything that stacks is rounded once per stack, gear per item, as ItemClassifier.ScrapUnits does.
    public static int ExpectedScrapIn(Thing thing, ScrapTier tier) => thing.def.stackLimit > 1
        ? ScrapAmounts.CappedStackUnits(thing.MarketValue, thing.stackCount, ScrapWorth(tier))
        : ScrapAmounts.CappedUnits(thing.MarketValue, thing.stackCount, ScrapWorth(tier));
    public static int ExpectedScrap(Thing thing, Faction owner) => ExpectedScrapIn(thing, TierOf(thing.def, owner));

    // Total market value of things, at base value, for comparing recycling yields with the tier's value per unit.
    public static float BaseValue(IEnumerable<Thing> things) => things.Sum(t => t.def.BaseMarketValue * t.stackCount);
}
