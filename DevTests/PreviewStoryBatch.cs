using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using LootScavenging.Model;
using RimWorld;
using RimWorldDevTests;
using UnityEngine;
using Verse;
using static LootScavenging.DevTests.Fixtures;

namespace LootScavenging.DevTests;

// Films stills for the Workshop preview candidates with --record --frames: a story scene (raiders downed and dead, their gear
// as scrap, a colonist hauling it, a machining table with what recycling made and a research bench being studied at) and a raid
// aftermath behind sandbags, then close-ups of the story scene, the workshop, and a downed pirate shown first with his gear on the
// ground and then with scrap in its place. The game stays paused; each shot is held
// for HoldSeconds. Writes layout.json (shots and key cells) for tools/preview_story.py. Not a gameplay test: the case passes when
// every raider left scrap. Not part of run-all.
public sealed class PreviewStoryBatch : Batch
{
    public override string Id => "lootscavenging-preview-story";
    public override bool IsShowcase => true;
    public override int SegmentCount => 1;
    public override TimeSpan WallLimit => TimeSpan.FromMinutes(5);
    public override IReadOnlyList<PlannedCase> Plan { get; } = new[] { new PlannedCase("preview-story.filmed", Levels.Evidence) };

    public const double HoldSeconds = 3.0;
    private const float WideZoom = 7.5f, CloseZoom = 4f;

    private static Faction Tribe => Find.FactionManager.FirstFactionOfDef(DefDatabase<FactionDef>.GetNamedSilentFail("TribeSavage"))
        ?? Find.FactionManager.AllFactionsListForReading.FirstOrDefault(f => !f.IsPlayer && !f.Hidden && f.def.humanlikeFaction
            && f.def.techLevel == TechLevel.Neolithic && f.HostileTo(Faction.OfPlayer));

    private static string RunDir()
    {
        string pointer = Path.Combine(TestRunner.EvidenceRoot, "active.txt");
        string dir = File.Exists(pointer) ? File.ReadAllText(pointer).Trim() : null;
        return !string.IsNullOrEmpty(dir) && Directory.Exists(dir) ? dir : TestRunner.EvidenceRoot;
    }

    private static string Num(float v) => v.ToString("0.###", CultureInfo.InvariantCulture);

    public override IEnumerable<Wait> Segment(int index, BatchContext ctx)
    {
        var map = ctx.Map;
        Kit.RemoveStartingColonists(ctx);
        yield return Wait.Ticks(1);
        foreach (var w in Find.WindowStack.Windows.Where(w => w is Dialog_MessageBox || w is Dialog_NodeTree).ToList()) w.Close(false);
        Find.TickManager.CurTimeSpeed = TimeSpeed.Paused;

        map.weatherManager.curWeather = WeatherDefOf.Clear;
        map.weatherManager.lastWeather = WeatherDefOf.Clear;
        int hour = GenLocalDate.HourOfDay(map);
        Find.TickManager.DebugSetTicksGame(Find.TickManager.TicksGame + ((36 - hour) % 24) * GenDate.TicksPerHour);
        ctx.Setup("Developer set clear weather and moved the clock to midday");

        ModHarness.SaveSettings();
        ModHarness.ApplyStyle(Rules, ModHarness.LootStyle.ScrapEverything);
        // Shot before tickets 21 and 24: downed raiders drop their gear, scrapped as it drops, and the scrap of the dead lies
        // beside the bodies.
        ModHarness.GearDrops(Settings.values);
        ctx.Setup("Developer set every category but medicine and ammunition to Scrap and turned off the hostile and non-hostile keep-gear boxes, so every raider's gear becomes scrap beside him");

        var tribe = Tribe ?? throw new SetupException("no hostile neolithic faction in this world");
        var area = Kit.Area(ctx, 64, 18);
        int x0 = area.minX, z0 = area.minZ, x1 = x0 + 34;
        var points = new List<(string name, float x, float z)>();
        void Point(string name, IntVec3 c) => points.Add((name, c.x + 0.5f, c.z + 0.5f));
        int raiders = 0, raidersWithScrap = 0;
        var scrapDefs = AllScrap.ToList();

        Pawn Raider(PawnKindDef kind, Faction faction, IntVec3 cell, string weapon, string[] apparel, bool dead, Rot4 facing)
        {
            var p = Spawn(ctx, kind, faction, cell);
            foreach (var a in apparel) Dress(p, Kit.Def(a), QualityCategory.Normal);
            Arm(p, Kit.Def(weapon), QualityCategory.Normal);
            p.Rotation = facing;
            int before = scrapDefs.Sum(d => map.listerThings.ThingsOfDef(d).Where(t => t.Spawned && t.Position.DistanceTo(cell) <= 4f).Sum(t => t.stackCount));
            if (dead)
            {
                p.Kill(null);
                // Scrap everything also took the clothes; dressed again, the body reads as a raider and only the gear is gone.
                foreach (var a in apparel) Dress(p, Kit.Def(a), QualityCategory.Normal);
                p.Drawer.renderer.SetAllGraphicsDirty();
                FilthMaker.TryMakeFilth(cell, map, ThingDefOf.Filth_Blood, 3);
                FilthMaker.TryMakeFilth(cell + IntVec3.East, map, ThingDefOf.Filth_Blood, 1);
            }
            else HealthUtility.TryAnesthetize(p);
            int after = scrapDefs.Sum(d => map.listerThings.ThingsOfDef(d).Where(t => t.Spawned && t.Position.DistanceTo(cell) <= 4f).Sum(t => t.stackCount));
            raiders++;
            if (after > before) raidersWithScrap++;
            ctx.Setup($"{(dead ? "Killed" : "Downed")} {kind.defName} with {weapon} at {cell}: {after - before} scrap units nearby");
            return p;
        }

        Pawn Colonist(IntVec3 cell, Rot4 facing, string weapon = null)
        {
            var p = Kit.Worker(ctx, cell, WorkTypeDefOf.Hauling);
            p.equipment?.DestroyAllEquipment();
            if (weapon != null) Arm(p, Kit.Def(weapon), QualityCategory.Good);
            p.Rotation = facing;
            return p;
        }

        // ----- Story: raiders on the left, a hauler in the middle, the workshop on concrete to the right -----
        var concrete = DefDatabase<TerrainDef>.GetNamed("Concrete");
        foreach (var c in new CellRect(x0 + 16, z0 + 1, 12, 11).Cells) map.terrainGrid.SetTerrain(c, concrete);

        var dead = new IntVec3(x0 + 3, 0, z0 + 7);
        var downed = new IntVec3(x0 + 5, 0, z0 + 3);
        Raider(Mercenary, Pirates, dead, "Gun_AssaultRifle", new[] { "Apparel_FlakVest", "Apparel_Pants", "Apparel_CollarShirt" }, true, Rot4.East);
        Raider(Mercenary, Pirates, downed, "Gun_Autopistol", new[] { "Apparel_Pants", "Apparel_CollarShirt" }, false, Rot4.East);
        Point("bodies", new IntVec3(x0 + 4, 0, z0 + 5));

        var industrial = Scrap(ScrapTier.Industrial);
        var hauler = Colonist(new IntVec3(x0 + 11, 0, z0 + 6), Rot4.East);
        hauler.carryTracker.TryStartCarry(Stack(industrial, 6));
        Point("hauler", hauler.Position);

        var table = Kit.Place(ctx, "TableMachining", new IntVec3(x0 + 21, 0, z0 + 8), Rot4.North);
        if (table.TryGetComp<CompPowerTrader>() != null) Kit.HoldPower(ctx, table, true);
        Colonist(table.InteractionCell, Rot4.North);
        SpawnStack(map, ThingDefOf.Steel, 45, new IntVec3(x0 + 24, 0, z0 + 8));
        SpawnStack(map, ThingDefOf.ComponentIndustrial, 4, new IntVec3(x0 + 25, 0, z0 + 7));
        Point("recycle", table.Position);

        var bench = Kit.Place(ctx, "SimpleResearchBench", new IntVec3(x0 + 21, 0, z0 + 4), Rot4.North, ThingDefOf.Steel);
        Colonist(bench.InteractionCell, Rot4.North);
        SpawnStack(map, industrial, 12, new IntVec3(x0 + 24, 0, z0 + 3));
        // Scrap waiting beside the crafter too, so the workshop shot has scrap at both benches.
        SpawnStack(map, industrial, 8, new IntVec3(x0 + 19, 0, z0 + 7));
        Point("study", bench.Position);
        ctx.Setup("Story scene: a dead and a downed pirate, a colonist hauling industrial scrap, a powered machining table beside steel and components, and a research bench with a colonist at it");

        // ----- Aftermath: colonists behind sandbags, raiders of four eras down in the field -----
        for (int z = z0 + 1; z <= z0 + 11; z++) Kit.Place(ctx, Kit.Def("Sandbags"), new IntVec3(x1 + 6, 0, z), Rot4.North, null, quiet: true);
        foreach (var (z, gun) in new[] { (z0 + 3, "Gun_BoltActionRifle"), (z0 + 7, "Gun_AssaultRifle"), (z0 + 10, "Gun_BoltActionRifle") })
        {
            var p = Colonist(new IntVec3(x1 + 5, 0, z), Rot4.East, gun);
            p.drafter.Drafted = true;
            p.Rotation = Rot4.East;
        }
        var carrier = Colonist(new IntVec3(x1 + 9, 0, z0 + 5), Rot4.West);
        carrier.carryTracker.TryStartCarry(Stack(Scrap(ScrapTier.Neolithic), 5));

        var tribal = new[] { "Apparel_TribalA" };
        Raider(DefDatabase<PawnKindDef>.GetNamed("Tribal_Archer"), tribe, new IntVec3(x1 + 12, 0, z0 + 8), "Bow_Recurve", tribal, true, Rot4.West);
        Raider(Mercenary, Pirates, new IntVec3(x1 + 15, 0, z0 + 3), "Gun_AssaultRifle", new[] { "Apparel_FlakVest", "Apparel_Pants" }, true, Rot4.West);
        Raider(DefDatabase<PawnKindDef>.GetNamed("Tribal_Warrior"), tribe, new IntVec3(x1 + 18, 0, z0 + 10), "MeleeWeapon_LongSword", tribal, false, Rot4.West);
        Raider(Mercenary, Pirates, new IntVec3(x1 + 21, 0, z0 + 5), "Gun_ChargeRifle", new[] { "Apparel_Pants", "Apparel_CollarShirt" }, true, Rot4.West);
        Raider(DefDatabase<PawnKindDef>.GetNamed("Tribal_Archer"), tribe, new IntVec3(x1 + 24, 0, z0 + 8), "Bow_Short", tribal, true, Rot4.West);
        ctx.Setup("Aftermath scene: sandbags, three drafted colonists, a colonist carrying neolithic scrap, and five raiders down with recurve and short bows, an assault rifle, a longsword and a charge rifle");

        // ----- Drop: one pirate downed keeping his rifle, which is laid whole beside him; vest and helmet beside him too.
        // Between the two shots each item is replaced by industrial scrap where it lay. -----
        // Since ticket 24 a downing with the box off always sorts the weapon, so this pirate keeps it and it is laid down below.
        Settings.values.hostilesKeepGear = true;
        // Its own area, asked for once the other scenes stand, so it lands clear of their pawns and buildings.
        var dropArea = Kit.Area(ctx, 22, 14);
        var dropCell = dropArea.CenterCell;
        var victim = Spawn(ctx, Mercenary, Pirates, dropCell);
        Dress(victim, Kit.Def("Apparel_CollarShirt"), QualityCategory.Normal);
        Dress(victim, Kit.Def("Apparel_Pants"), QualityCategory.Normal);
        var rifle = Arm(victim, Kit.Def("Gun_AssaultRifle"), QualityCategory.Normal);
        victim.Rotation = Rot4.West;
        HealthUtility.TryAnesthetize(victim);
        FilthMaker.TryMakeFilth(dropCell, map, ThingDefOf.Filth_Blood, 2);
        var gear = new List<Thing>();
        if (rifle.Spawned) gear.Add(rifle);
        else
        {
            // Not dropped (the pawn kept it): lay it down by hand so the shot still shows the weapon.
            victim.equipment.Remove(rifle);
            GenPlace.TryPlaceThing(rifle, dropCell + IntVec3.East, map, ThingPlaceMode.Direct);
            gear.Add(rifle);
            ctx.Setup("The downed pirate kept his rifle; developer laid it beside him");
        }
        // In a row east of him with the rifle, so the preview's flat panels show all of it on one line.
        foreach (var (def, offset) in new[] { ("Apparel_FlakVest", new IntVec3(2, 0, 0)), ("Apparel_SimpleHelmet", new IntVec3(3, 0, 0)) })
        {
            var item = Make(Kit.Def(def), QualityCategory.Normal);
            GenPlace.TryPlaceThing(item, dropCell + offset, map, ThingPlaceMode.Direct);
            gear.Add(item);
        }
        Settings.values.hostilesKeepGear = false;
        Point("drop", dropCell);
        ctx.Setup($"Drop scene: a downed pirate at {dropCell} with {string.Join(", ", gear.Select(g => $"{g.def.defName} at {g.Position}"))}");

        // Raiders' drops and corpses come forbidden; the red crosses would cover the scrap.
        foreach (var t in map.listerThings.AllThings.Where(t => t.Spawned && (area.Contains(t.Position) || dropArea.Contains(t.Position))).ToList())
            t.SetForbidden(false, warnOnFail: false);

        // ----- Shots -----
        var shots = new List<(string name, Vector3 look, float zoom)>
        {
            ("story", new Vector3(x0 + 14f, 0f, z0 + 8f), WideZoom),
            ("aftermath", new Vector3(x1 + 14f, 0f, z0 + 8f), WideZoom),
            ("close-bodies", new Vector3(x0 + 5f, 0f, z0 + 5.5f), CloseZoom),
            ("close-recycle", new Vector3(x0 + 23f, 0f, z0 + 8f), CloseZoom),
            ("close-study", new Vector3(x0 + 23f, 0f, z0 + 3.5f), CloseZoom),
            ("workshop", new Vector3(x0 + 22f, 0f, z0 + 5.5f), 5f),
            ("drop-gear", dropCell.ToVector3Shifted(), CloseZoom),
            ("drop-scrap", dropCell.ToVector3Shifted(), CloseZoom),
            ("drop-body", dropCell.ToVector3Shifted(), CloseZoom),
        };
        int scrapOnGroundAfterDeath = -1;
        var scrapCounts = new[] { 6, 4, 2 };
        File.WriteAllText(Path.Combine(RunDir(), "layout.json"),
            "{\"hold\":" + Num((float)HoldSeconds) + ",\"shots\":[" + string.Join(",", shots.Select(s => $"{{\"name\":\"{s.name}\",\"x\":{Num(s.look.x)},\"z\":{Num(s.look.z)},\"zoom\":{Num(s.zoom)}}}"))
            + "],\"points\":[" + string.Join(",", points.Select(p => $"{{\"name\":\"{p.name}\",\"x\":{Num(p.x)},\"z\":{Num(p.z)}}}")) + "]}");

        var config = Find.CameraDriver.config;
        var range = config.sizeRange;
        config.sizeRange = new FloatRange(CloseZoom, range.max);
        Find.Selector.ClearSelection();
        try
        {
            Find.CameraDriver.SetRootPosAndSize(shots[0].look, shots[0].zoom);
            if (RunRecording.Enabled)
            {
                RunRecording.Ready();
                yield return Wait.Recorder();
            }
            foreach (var shot in shots)
            {
                if (shot.name == "drop-scrap")
                    for (int i = 0; i < gear.Count; i++)
                    {
                        var cell = gear[i].Position;
                        gear[i].Destroy();
                        GenPlace.TryPlaceThing(Stack(industrial, scrapCounts[i]), cell, map, ThingPlaceMode.Direct);
                    }
                if (shot.name == "drop-body")
                {
                    // The scrap goes back into his pack, and he dies with "everything stays on the body": the mod keeps it all
                    // in the corpse, so nothing lands on the ground.
                    foreach (var pile in map.listerThings.ThingsOfDef(industrial).Where(t => t.Spawned && t.Position.DistanceTo(dropCell) <= 3f).ToList())
                    {
                        pile.DeSpawn();
                        victim.inventory.innerContainer.TryAdd(pile);
                    }
                    Settings.values.hostilesKeepGear = true;
                    victim.Kill(null);
                    // Scrap everything also took his clothes into the corpse as scrap; dressed again so the body reads as a raider.
                    Dress(victim, Kit.Def("Apparel_CollarShirt"), QualityCategory.Normal);
                    Dress(victim, Kit.Def("Apparel_Pants"), QualityCategory.Normal);
                    victim.Drawer.renderer.SetAllGraphicsDirty();
                    scrapOnGroundAfterDeath = map.listerThings.ThingsOfDef(industrial).Where(t => t.Spawned && t.Position.DistanceTo(dropCell) <= 3f).Sum(t => t.stackCount);
                    foreach (var t in dropCell.GetThingList(map).ToList()) t.SetForbidden(false, warnOnFail: false);
                    ctx.Setup($"Developer moved the drop scrap into the pirate's inventory, set loot at death to everything on the body and killed him: {scrapOnGroundAfterDeath} scrap on the ground");
                }
                Find.CameraDriver.SetRootPosAndSize(shot.look, shot.zoom);
                yield return Wait.Seconds(HoldSeconds);
            }
            // The recorder can lose the last seconds when the game quits.
            yield return Wait.Seconds(2.0);
        }
        finally
        {
            config.sizeRange = range;
        }

        ctx.Begin("preview-story.filmed").Expect("raidersWithScrap", raiders).Observe("raidersWithScrap", raidersWithScrap)
            .Expect("scrapOnGroundAfterDeath", 0).Observe("scrapOnGroundAfterDeath", scrapOnGroundAfterDeath)
            .Check(raiders > 0 && raidersWithScrap == raiders && scrapOnGroundAfterDeath == 0,
                "a raider left no scrap, or scrap landed on the ground with everything on the body");
    }
}
