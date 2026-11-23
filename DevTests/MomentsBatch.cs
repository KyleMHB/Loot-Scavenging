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
        AllOnBody = P + "all-on-body", AllOnBodyDisassembled = P + "all-on-body-disassembled", AllOnBodyRemoved = P + "all-on-body-corpse-removed",
        AllOnBodySkips = P + "all-on-body-skips", AllOnBodyDowned = P + "all-on-body-downed",
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
        new PlannedCase(AllOnBody, Levels.Mechanical, "everything on the body: scrap, ejected charges and inventory stay in the corpse; stripping drops them"),
        new PlannedCase(AllOnBodyDisassembled, Levels.Mechanical, "everything on the body: disassembly products stay in the corpse"),
        new PlannedCase(AllOnBodyRemoved, Levels.Mechanical, "everything on the body with corpse removal on: the loot lands on the ground"),
        new PlannedCase(AllOnBodySkips, Levels.Mechanical, "everything on the body: a colonist's gear and a mechanoid's scrap drop as usual"),
        new PlannedCase(AllOnBodyDowned, Levels.Mechanical, "everything on the body: a downed pirate keeps his in-reach club in hand and his pack, with downed handling on and off"),
        new PlannedCase(OffMap, Levels.Mechanical, "a world pawn's death (no map, no corpse) logs no error"),
        new PlannedCase(Butcher, Levels.PlayerWorkflow, "a butcher bill destroys worn apparel with the option on and drops it with the option off"),
        new PlannedCase(HomeOnly, Levels.Mechanical, "home-map-only leaves gear whole on an away map; off, it is scrapped there"),
        new PlannedCase(NoErrors, Levels.Mechanical, "no error naming the mod logged during the batch"),
    };

    public override IEnumerable<Wait> Segment(int index, BatchContext ctx)
    {
        long modErrorsAtStart = CaseErrors.ModMark;
        ModHarness.SaveSettings();
        var area = Kit.Area(ctx, 80, 14);
        Kit.RemoveStartingColonists(ctx);
        yield return Wait.Ticks(1);
        var map = ctx.Map;
        MakeTribal(ctx);
        // Two rows 6 cells apart: the first row's sites ran out.
        var sites = new Queue<IntVec3>(Sites(area, 11, 7).Concat(Sites(area, 11, 7).Select(c => c + new IntVec3(0, 0, -6))));
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

        // ---- 03/08/10: everything stays on the body ----------------------------------------------------------------------
        var silver = ThingDefOf.Silver;
        var chemfuel = ThingDefOf.Chemfuel;
        int InCorpse(Pawn dead, ThingDef def) =>
            dead.Corpse?.InnerPawn?.inventory?.innerContainer.Where(t => t.def == def).Sum(t => t.stackCount) ?? 0;
        {
            var c = ctx.Begin(AllOnBody);
            ModHarness.RestoreSettings();
            Settings.values.deathDrop = DeathDrop.AllOnBody;
            Rules.ejectCharges = true;
            var cell = sites.Dequeue();
            var p = Spawn(ctx, Mercenary, Pirates, cell);
            var gun = Arm(p, revolver, QualityCategory.Normal);
            var belt = Dress(p, Kit.Def("Apparel_SmokepopBelt"));
            p.inventory.innerContainer.TryAdd(Stack(silver, 50));
            c.Subject(p);
            int expectedScrap = ScrapAmounts.Units(gun.MarketValue, 1) + ScrapAmounts.Units(belt.MarketValue, 1);
            var watched = AllScrap.Concat(new[] { silver, chemfuel }).ToList();
            var before = watched.ToDictionary(d => d, d => Kit.StackIds(map, d));
            int Ground() => watched.Sum(d => Units(NewNear(map, d, before[d], cell)));
            p.Kill(null);
            int scrapIn = AllScrap.Sum(d => InCorpse(p, d)), silverIn = InCorpse(p, silver), fuelIn = InCorpse(p, chemfuel);
            int ground = Ground();
            c.Expect("scrapInCorpse", expectedScrap).Observe("scrapInCorpse", scrapIn).Observe("silverInCorpse", silverIn)
                .Observe("chemfuelInCorpse", fuelIn).Observe("onGroundAtDeath", ground);
            c.Step(gun.Destroyed && belt.Destroyed, "the revolver and smokepop belt are scrapped");
            c.Step(scrapIn == expectedScrap, $"their scrap ({scrapIn}, expected {expectedScrap}) is in the corpse's inventory");
            c.Step(fuelIn == 10, $"the belt's ejected chemfuel ({fuelIn}) is in the corpse's inventory");
            c.Step(silverIn == 50, $"the carried silver ({silverIn}) is in the corpse's inventory");
            c.Step(ground == 0, "nothing lands on the ground at death");
            before = watched.ToDictionary(d => d, d => Kit.StackIds(map, d));
            p.Corpse?.Strip();
            int dropped = Ground();
            c.Observe("droppedByStrip", dropped);
            c.Step(dropped == scrapIn + fuelIn + silverIn, $"stripping the corpse drops all of it ({dropped})");
            c.Conclude();
        }
        {
            var c = ctx.Begin(AllOnBodyDisassembled);
            ModHarness.RestoreSettings();
            Settings.values.deathDrop = DeathDrop.AllOnBody;
            Rules.dropMode = DropMode.Off;
            Rules.weapons = new CategoryRule(Outcome.Disassemble);
            Rules.disassemblyYield = DisassemblyYield.ItemBased;
            Rules.disassemblyEfficiency = 1f;
            var costs = revolver.CostList.Where(x => x.count > 0).ToList();
            var cell = sites.Dequeue();
            var p = Spawn(ctx, Mercenary, Pirates, cell);
            var gun = Arm(p, revolver, QualityCategory.Normal);
            c.Subject(p);
            var before = costs.ToDictionary(x => x.thingDef, x => Kit.StackIds(map, x.thingDef));
            p.Kill(null);
            c.Step(gun.Destroyed, "the revolver is disassembled");
            foreach (var cost in costs)
            {
                int inside = InCorpse(p, cost.thingDef), ground = Units(NewNear(map, cost.thingDef, before[cost.thingDef], cell));
                c.Expect(cost.thingDef.defName, cost.count).Observe(cost.thingDef.defName, inside).Observe(cost.thingDef.defName + "OnGround", ground);
                c.Step(inside == cost.count && ground == 0, $"its {cost.count} {cost.thingDef.label} are in the corpse's inventory ({inside}), none on the ground");
            }
            c.Conclude();
        }
        {
            var c = ctx.Begin(AllOnBodyRemoved);
            ModHarness.RestoreSettings();
            Settings.values.deathDrop = DeathDrop.AllOnBody;
            Settings.values.corpses.humanlike = true;
            var cell = sites.Dequeue();
            var p = Spawn(ctx, Mercenary, Pirates, cell);
            var gun = Arm(p, revolver, QualityCategory.Normal);
            p.inventory.innerContainer.TryAdd(Stack(silver, 50));
            c.Subject(p);
            int expected = ScrapAmounts.Units(gun.MarketValue, 1);
            var beforeScrap = Kit.StackIds(map, industrial);
            var beforeSilver = Kit.StackIds(map, silver);
            p.Kill(null);
            int scrap = Units(NewNear(map, industrial, beforeScrap, cell)), silverOut = Units(NewNear(map, silver, beforeSilver, cell));
            c.Expect("scrapOnGround", expected).Observe("scrapOnGround", scrap).Observe("silverOnGround", silverOut);
            c.Step(p.Corpse == null || p.Corpse.Destroyed, "the corpse is removed");
            c.Step(scrap == expected && silverOut == 50, $"its scrap ({scrap}) and silver ({silverOut}) lie on the ground");
            c.Conclude();
        }
        {
            var c = ctx.Begin(AllOnBodySkips);
            ModHarness.RestoreSettings();
            Settings.values.deathDrop = DeathDrop.AllOnBody;
            var cell = sites.Dequeue();
            var colonist = Spawn(ctx, PawnKindDefOf.Colonist, Faction.OfPlayer, cell);
            var ownGun = Arm(colonist, revolver, QualityCategory.Normal);
            colonist.Kill(null);
            c.Step(!ownGun.Destroyed && ownGun.Spawned && InCorpse(colonist, revolver) == 0, "a colonist's revolver drops whole on the ground, not into the corpse");

            // The wreck cannot be stripped, so scrap left in it would be stuck: both on-body options drop it as usual.
            cell = sites.Dequeue();
            bool ready = true;
            foreach (var mode in new[] { DeathDrop.AllOnBody, DeathDrop.ScrapOnBody })
            {
                Settings.values.deathDrop = mode;
                var mech = PawnGenerator.GeneratePawn(new PawnGenerationRequest(DefDatabase<PawnKindDef>.GetNamed("Mech_Lancer"), Faction.OfMechanoids, forceGenerateNewPawn: true));
                GenSpawn.Spawn(mech, cell, map);
                var weapon = mech.equipment?.Primary;
                if (!Kit.Require(ctx, AllOnBodySkips, (weapon != null, "the lancer spawned with a weapon"),
                    (mech.kindDef.destroyGearOnDrop && !mech.kindDef.canStrip, "the lancer's kind destroys its gear and cannot be stripped")))
                {
                    ready = false;
                    break;
                }
                var tierDef = Scrap(ScrapTiers.TierFor(EraReader.ItemEra(weapon.def, mech.Faction).Era, EraReader.ColonyEra()));
                int expected = ScrapAmounts.Units(weapon.MarketValue, 1);
                // Totals near the cell, not new stacks: the second lancer's scrap merges into the first one's pile.
                int Near() => map.listerThings.ThingsOfDef(tierDef).Where(t => t.Spawned && t.Position.DistanceTo(cell) <= 5f).Sum(t => t.stackCount);
                int start = Near();
                mech.Kill(null);
                int made = Near() - start;
                int inWreck = InCorpse(mech, tierDef);
                c.Expect("mechScrap" + mode, expected).Observe("mechScrap" + mode, made).Observe("inWreck" + mode, inWreck);
                c.Step(weapon.Destroyed && made == expected && inWreck == 0,
                    $"{mode}: the lancer's weapon becomes {made} {tierDef.defName} beside the wreck (expected {expected}), {inWreck} inside it");
                mech.Corpse?.Destroy();
            }
            if (ready) c.Conclude();
        }

        // ---- everything stays on the body when downed ---------------------------------------------------------------------
        {
            var c = ctx.Begin(AllOnBodyDowned);
            var club = Kit.Def("MeleeWeapon_Club");
            bool ready = true;
            foreach (bool downedHandling in new[] { true, false })
            {
                ModHarness.RestoreSettings();
                Settings.values.deathDrop = DeathDrop.AllOnBody;
                Rules.onDownedAndStrip = downedHandling;
                string tag = downedHandling ? "On" : "Off";
                var cell = sites.Dequeue();
                var p = Spawn(ctx, Mercenary, Pirates, cell);
                var held = Arm(p, club, QualityCategory.Normal);
                var carried = Stack(revolver, 1);
                p.inventory.innerContainer.TryAdd(carried);
                p.inventory.innerContainer.TryAdd(Stack(silver, 50));
                c.Subject(p);
                c.Note($"Everything stays on the body; downed and strip {(downedHandling ? "on" : "off")}. A pirate with a club in hand and a revolver and 50 silver in his pack is downed");
                c.Step(!EraReader.ItemEra(club, Pirates).OutOfReach && EraReader.ItemEra(revolver, Pirates).OutOfReach,
                    "the club is in reach of the tribal colony and the revolver is not");
                int expectedScrap = ScrapAmounts.Units(carried.MarketValue, 1);
                var watched = AllScrap.Concat(new[] { silver, club, revolver }).ToList();
                var before = watched.ToDictionary(d => d, d => Kit.StackIds(map, d));
                int Ground() => watched.Sum(d => Units(NewNear(map, d, before[d], cell)));
                int Pack(ThingDef def) => p.inventory.innerContainer.Where(t => t.def == def).Sum(t => t.stackCount);
                HealthUtility.TryAnesthetize(p);
                if (!Kit.Require(ctx, AllOnBodyDowned, (p.Downed && !p.Dead && p.Spawned, "the pirate is downed and alive")))
                {
                    ready = false;
                    break;
                }
                // A few ticks so anything the game drops later than MakeDowned shows too.
                yield return Wait.Ticks(60);
                int ground = Ground(), scrapIn = AllScrap.Sum(Pack), silverIn = Pack(silver), revolverIn = Pack(revolver);
                c.Observe("primary" + tag, p.equipment?.Primary?.def.defName ?? "none").Observe("scrapInPack" + tag, scrapIn)
                    .Observe("revolverInPack" + tag, revolverIn).Observe("silverInPack" + tag, silverIn).Observe("onGround" + tag, ground);
                c.Step(p.Downed && p.equipment?.Primary == held, $"{tag}: the club is still in his hands");
                if (downedHandling)
                {
                    c.Expect("scrapInPack" + tag, expectedScrap);
                    c.Step(carried.Destroyed && scrapIn == expectedScrap && revolverIn == 0,
                        $"{tag}: the carried revolver is scrapped into his pack ({scrapIn} scrap, expected {expectedScrap})");
                }
                else
                    c.Step(!carried.Destroyed && revolverIn == 1 && scrapIn == 0, $"{tag}: the carried revolver stays whole in his pack");
                c.Step(silverIn == 50, $"{tag}: the 50 silver stay in his pack ({silverIn})");
                c.Step(ground == 0, $"{tag}: nothing lands on the ground ({ground})");
                p.Destroy();
            }
            if (ready) c.Conclude();
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
