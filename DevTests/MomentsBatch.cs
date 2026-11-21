using System;
using System.Collections.Generic;
using System.Linq;
using LootScavenging.Model;
using RimWorld;
using RimWorld.Planet;
using RimWorldDevTests;
using Verse;
using Verse.AI;
using static LootScavenging.DevTests.Fixtures;

namespace LootScavenging.DevTests;

// The moments gear changes hands (tickets 01, 03, 10): downed and stripped pawns, a colony prisoner, scrap left on the body,
// butchering, a death off the map and a death on a map that is not home. Butchering runs a real bill job; the rest are
// direct calls between ticks. The away map is made last, and removed again.
public sealed class MomentsBatch : Batch
{
    private const string P = "lootscavenging.moments.";
    private const string DownStripOn = P + "downed-and-strip-on", DownStripOff = P + "downed-and-strip-off", Prisoner = P + "prisoner-strip",
        OnBody = P + "scrap-on-body", OnBodyRemoved = P + "scrap-on-body-corpse-removed", OffMap = P + "off-map-death",
        Butcher = P + "butchering-removes-apparel", HomeOnly = P + "home-map-only", NoErrors = P + "no-mod-errors";

    public override string Id => "lootscavenging-moments";
    public override int SegmentCount => 1;
    public override int MapSize => 100;

    public override IReadOnlyList<PlannedCase> Plan => new[]
    {
        new PlannedCase(DownStripOn, Levels.Mechanical, "downing scraps an out-of-reach weapon and stripping scraps out-of-reach apparel"),
        new PlannedCase(DownStripOff, Levels.Mechanical, "with downed and strip off, both drop whole"),
        new PlannedCase(Prisoner, Levels.Mechanical, "stripping a living colony prisoner scraps out-of-reach apparel"),
        new PlannedCase(OnBody, Levels.Mechanical, "scrap left on the body is in the corpse; stripping it drops the scrap"),
        new PlannedCase(OnBodyRemoved, Levels.Mechanical, "with corpse removal on, scrap left on the body lands on the ground"),
        new PlannedCase(OffMap, Levels.Mechanical, "a world pawn's death (no map, no corpse) logs no error"),
        new PlannedCase(Butcher, Levels.PlayerWorkflow, "a butcher bill destroys worn apparel with the option on and drops it with the option off"),
        new PlannedCase(HomeOnly, Levels.Mechanical, "home-map-only leaves gear whole on an away map; off, it is scrapped there"),
        new PlannedCase(NoErrors, Levels.Mechanical, "no error naming the mod logged during the batch"),
    };

    public override IEnumerable<Wait> Segment(int index, BatchContext ctx)
    {
        long modErrorsAtStart = CaseErrors.ModMark;
        ModHarness.SaveSettings();
        var area = Kit.Area(ctx, 60, 14);
        Kit.RemoveStartingColonists(ctx);
        yield return Wait.Ticks(1);
        var map = ctx.Map;
        MakeTribal(ctx);
        var sites = new Queue<IntVec3>(Sites(area, 8, 7));
        var revolver = Kit.Def("Gun_Revolver");
        var vest = Kit.Def("Apparel_FlakVest");
        var industrial = Scrap(ScrapTier.Industrial);

        // ---- 01: downed and strip on, then off ---------------------------------------------------------------------------
        foreach (bool on in new[] { true, false })
        {
            var c = ctx.Begin(on ? DownStripOn : DownStripOff);
            ModHarness.RestoreSettings();
            Rules.onDownedAndStrip = on;
            var cell = sites.Dequeue();
            var p = Spawn(ctx, Mercenary, Pirates, cell);
            var gun = Arm(p, revolver, QualityCategory.Normal);
            var worn = Dress(p, vest, QualityCategory.Normal);
            c.Subject(p);
            c.Step(EraReader.ItemEra(vest, Pirates).OutOfReach && EraReader.ItemEra(revolver, Pirates).OutOfReach, "the revolver and flak vest are out of reach");
            // Totals near the pawn, not new stacks: scrap made later can merge into the pile made at the downing.
            int Near() => map.listerThings.ThingsOfDef(industrial).Where(t => t.Spawned && t.Position.DistanceTo(cell) <= 5f).Sum(t => t.stackCount);
            int start = Near();
            HealthUtility.TryAnesthetize(p);
            if (!Kit.Require(ctx, on ? DownStripOn : DownStripOff, (p.Downed && !p.Dead, "the pirate is downed and alive"))) continue;
            int afterDown = Near() - start;
            if (on) c.Step(gun.Destroyed && afterDown > 0, $"downing turns the revolver into scrap ({afterDown} units)");
            else c.Step(!gun.Destroyed && gun.Spawned && afterDown == 0, "downing drops the revolver whole");
            c.Step(p.apparel.Contains(worn), "the vest stays on while downed");
            start = Near();
            p.Strip();
            int afterStrip = Near() - start;
            if (on) c.Step(worn.Destroyed && afterStrip > 0, $"stripping turns the vest into scrap ({afterStrip} units)");
            else c.Step(!worn.Destroyed && worn.Spawned && afterStrip == 0, "stripping drops the vest whole");
            c.Conclude();
            p.Destroy();
        }

        // ---- 01: living prisoner strip -----------------------------------------------------------------------------------
        {
            var c = ctx.Begin(Prisoner);
            ModHarness.RestoreSettings();
            var cell = sites.Dequeue();
            var p = Spawn(ctx, Mercenary, Pirates, cell);
            var worn = Dress(p, vest, QualityCategory.Normal);
            p.guest.SetGuestStatus(Faction.OfPlayer, GuestStatus.Prisoner);
            c.Subject(p);
            if (Kit.Require(ctx, Prisoner, (p.IsPrisonerOfColony && !p.Dead, "the pirate is a living colony prisoner")))
            {
                var before = Kit.StackIds(map, industrial);
                p.Strip();
                int made = Units(NewNear(map, industrial, before, cell));
                c.Step(worn.Destroyed && made > 0, $"stripping the prisoner turns the vest into {made} scrap");
                c.Conclude();
            }
            p.Destroy();
        }

        // ---- 03/10: scrap on the body ------------------------------------------------------------------------------------
        {
            var c = ctx.Begin(OnBody);
            ModHarness.RestoreSettings();
            Settings.values.deathDrop = DeathDrop.ScrapOnBody;
            var cell = sites.Dequeue();
            var p = Spawn(ctx, Mercenary, Pirates, cell);
            var gun = Arm(p, revolver, QualityCategory.Normal);
            int expected = ScrapAmounts.Units(gun.MarketValue, 1);
            var before = Kit.StackIds(map, industrial);
            p.Kill(null);
            var corpse = p.Corpse;
            int inCorpse = corpse?.InnerPawn?.inventory?.innerContainer.Where(t => t.def == industrial).Sum(t => t.stackCount) ?? 0;
            int onGround = Units(NewNear(map, industrial, before, cell));
            c.Expect("inCorpse", expected).Observe("inCorpse", inCorpse).Observe("onGroundAtDeath", onGround);
            c.Step(gun.Destroyed && inCorpse == expected && onGround == 0, $"the scrap ({inCorpse}) stays in the corpse's inventory");
            before = Kit.StackIds(map, industrial);
            corpse?.Strip();
            int dropped = Units(NewNear(map, industrial, before, cell));
            c.Observe("droppedByStrip", dropped);
            c.Step(dropped == expected, "stripping the corpse drops it");
            c.Conclude();
        }
        {
            var c = ctx.Begin(OnBodyRemoved);
            ModHarness.RestoreSettings();
            Settings.values.deathDrop = DeathDrop.ScrapOnBody;
            Settings.values.corpses.humanlike = true;
            var cell = sites.Dequeue();
            var p = Spawn(ctx, Mercenary, Pirates, cell);
            var gun = Arm(p, revolver, QualityCategory.Normal);
            int expected = ScrapAmounts.Units(gun.MarketValue, 1);
            var before = Kit.StackIds(map, industrial);
            p.Kill(null);
            int onGround = Units(NewNear(map, industrial, before, cell));
            c.Expect("onGround", expected).Observe("onGround", onGround);
            c.Step(p.Corpse == null || p.Corpse.Destroyed, "the corpse is removed");
            c.Step(onGround == expected, "the scrap it carried lies on the ground");
            c.Conclude();
        }

        // ---- 10: death off the map ---------------------------------------------------------------------------------------
        {
            var c = ctx.Begin(OffMap);
            ModHarness.RestoreSettings();
            long mark = CaseErrors.ModMark;
            var p = PawnGenerator.GeneratePawn(new PawnGenerationRequest(Mercenary, Pirates, forceGenerateNewPawn: true));
            Find.WorldPawns.PassToWorld(p);
            p.Kill(null);
            c.Observe("modErrors", CaseErrors.ModMark - mark);
            c.Step(p.Dead && p.Corpse == null, "a world pawn dies with no corpse on any map");
            c.Step(CaseErrors.ModMark == mark, "no mod error is logged");
            c.Conclude();
        }

        // ---- 10: butchering ------------------------------------------------------------------------------------------------
        {
            var c = ctx.Begin(Butcher);
            ModHarness.RestoreSettings();
            Rules.dropMode = DropMode.Off;
            var spotCell = sites.Dequeue();
            var spot = Kit.Place(ctx, "ButcherSpot", spotCell);
            var cook = Kit.Worker(ctx, spotCell + new IntVec3(0, 0, -3), DefDatabase<WorkTypeDef>.GetNamed("Cooking"), SkillDefOf.Cooking);
            c.Subject(cook);
            var recipe = DefDatabase<RecipeDef>.GetNamed("ButcherCorpseFlesh");
            var bill = (Bill_Production)recipe.MakeNewBill();
            bill.repeatMode = BillRepeatModeDefOf.Forever;
            // Everything the recipe can take, special filters (strangers' corpses) included.
            bill.ingredientFilter.SetAllowAll(recipe.fixedIngredientFilter);
            ((IBillGiver)spot).BillStack.AddBill(bill);
            var results = new List<(bool option, bool butchered, bool destroyed, bool spawned)>();
            foreach (bool option in new[] { true, false })
            {
                Settings.values.corpses.butcherRemovesApparel = option;
                var p = Spawn(ctx, Mercenary, Pirates, spotCell + new IntVec3(3, 0, 0));
                var shirt = Dress(p, Kit.Def("Apparel_CollarShirt"), QualityCategory.Normal);
                p.Kill(null);
                var corpse = p.Corpse;
                corpse?.SetForbidden(false, false);
                c.Note($"Butchering removes worn apparel: {(option ? "on" : "off")}; a pirate's corpse in a collared shirt lies by the butcher spot");
                var done = Wait.Until(() => corpse == null || corpse.Destroyed, 6000, Kit.KeepRested(cook));
                yield return done;
                results.Add((option, !done.TimedOut, shirt.Destroyed, shirt.Spawned));
                c.Observe(option ? "optionOn" : "optionOff", $"butchered {!done.TimedOut}, shirt destroyed {shirt.Destroyed}, on ground {shirt.Spawned}");
            }
            if (results.Any(r => !r.butchered))
                c.WorkflowInconclusive("the cook did not butcher a corpse within 6000 ticks",
                    (cook.Spawned && !cook.Dead, "the cook is alive"), (bill.ShouldDoNow(), "the bill wants doing"));
            else
            {
                c.Step(results[0].destroyed && !results[0].spawned, "with the option on the shirt is destroyed with the corpse");
                c.Step(!results[1].destroyed && results[1].spawned, "with the option off the shirt is left on the ground");
                c.Conclude();
            }
            bill.suspended = true;
        }

        // ---- 01/10: home-map-only on an away map -------------------------------------------------------------------------
        {
            Map away = null;
            MapParent parent = null;
            string problem = null;
            try
            {
                if (!TileFinder.TryFindPassableTileWithTraversalDistance(map.Tile, 2, 8, out PlanetTile tile, t => !Find.WorldObjects.AnyWorldObjectAt(t)))
                    problem = "no free tile near home";
                else
                {
                    away = GetOrGenerateMapUtility.GetOrGenerateMap(tile, new IntVec3(50, 1, 50), WorldObjectDefOf.Camp);
                    parent = away.Parent;
                }
            }
            catch (Exception e) { problem = "generating an away map failed: " + e.Message; }
            if (problem != null || away == null) ctx.Begin(HomeOnly).SetupFailed(problem ?? "no away map");
            else
            {
                var c = ctx.Begin(HomeOnly);
                ctx.Setup($"Developer generated a {away.Size.x}x{away.Size.z} camp map at tile {away.Tile}; IsPlayerHome {away.IsPlayerHome}");
                c.Step(!away.IsPlayerHome, "the camp map is not a home map");
                var cell = CellFinder.RandomClosewalkCellNear(away.Center, away, 5);
                foreach (bool homeOnly in new[] { true, false })
                {
                    ModHarness.RestoreSettings();
                    Rules.homeMapOnly = homeOnly;
                    var p = Spawn(ctx, Mercenary, Pirates, cell, away);
                    var gun = Arm(p, revolver, QualityCategory.Normal);
                    int scrapBefore = AllScrap.Sum(d => Kit.CountOnMap(away, d));
                    p.Kill(null);
                    int made = AllScrap.Sum(d => Kit.CountOnMap(away, d)) - scrapBefore;
                    if (homeOnly) c.Step(!gun.Destroyed && gun.Spawned && made == 0, "with home-map-only on, a pirate's revolver drops whole on the camp map");
                    else c.Step(gun.Destroyed && made > 0, $"with it off, the revolver becomes {made} scrap there");
                    cell = CellFinder.RandomClosewalkCellNear(away.Center, away, 8);
                }
                c.Conclude();
            }
            if (away != null)
            {
                Current.Game.DeinitAndRemoveMap(away, false);
                parent?.Destroy();
                if (Find.CurrentMap != map) Current.Game.CurrentMap = map;
            }
        }

        {
            var c = ctx.Begin(NoErrors);
            c.Observe("modErrors", CaseErrors.ModMark - modErrorsAtStart);
            c.Step(CaseErrors.ModMark == modErrorsAtStart, "no error naming Loot Scavenging was logged during the batch");
            c.Conclude();
        }
    }
}
