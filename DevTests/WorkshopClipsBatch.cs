using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using LootScavenging.Model;
using RimWorld;
using RimWorldDevTests;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using static LootScavenging.DevTests.Fixtures;

namespace LootScavenging.DevTests;

// Films the Workshop description GIFs and the gallery stills (.scratch/workshop-media, ticket 01) with --record --show-ui, all on
// the Progression preset at a tribal colony, at normal game speed:
//   raid      a pirate with an autopistol and a tribal warrior with a club attack a sandbag line; under the defaults the downed
//             pirate keeps his gun and the dead warrior his club; an archer strips the pirate and the gun becomes industrial scrap
//   recycle   a crafter works one recycle bill on neolithic scrap at the crafting spot
//   study     a researcher works one study bill on neolithic scrap at a research bench (Recurve Bow current)
//   on-body   "everything stays on the body": a pirate dies and nothing lands; his corpse's Gear tab shows the loot still on him;
//             a colonist strips him, steps away, and the loot lies on the ground
//   downed    default "Keep his weapon": a pirate is downed, nothing falls, and his Gear tab shows the autopistol under Equipment
//   drop-*    paused stills: the same tribal warrior (club, an autopistol in his pack) killed under each drop mode
//   inspect-* paused stills: neolithic scrap that can be studied now and spacer scrap that cannot, each selected
// Writes shots.json (marks on the game's monotonic clock, the recorder's video origin clock) and layout.json (each shot's camera)
// for tools/render_workshop_gifs.py and tools/workshop_gallery.py. Not a gameplay test: the case passes when each beat happened
// on film. Not part of run-all.
public sealed class WorkshopClipsBatch : Batch
{
    public override string Id => "lootscavenging-workshop-clips";
    public override bool IsShowcase => true;
    public override int SegmentCount => 1;
    public override TimeSpan WallLimit => TimeSpan.FromMinutes(12);
    public override IReadOnlyList<PlannedCase> Plan { get; } = new[] { new PlannedCase("workshop-clips.filmed", Levels.Evidence) };

    private const float ClipZoom = 6f, OnBodyZoom = 5f, StillZoom = 4f;
    private const double StillSeconds = 3.0;

    private readonly List<(string name, double t)> marks = new();
    private readonly List<(string name, Vector3 look, float zoom)> shots = new();
    private static double Now() => Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency;
    private void Mark(string name) => marks.Add((name, Now())); // not translated
    private static string Num(double v) => v.ToString("0.###", CultureInfo.InvariantCulture);

    private static string RunDir()
    {
        string pointer = Path.Combine(TestRunner.EvidenceRoot, "active.txt");
        string dir = File.Exists(pointer) ? File.ReadAllText(pointer).Trim() : null;
        return !string.IsNullOrEmpty(dir) && Directory.Exists(dir) ? dir : TestRunner.EvidenceRoot;
    }

    private static Faction Tribe => Find.FactionManager.FirstFactionOfDef(DefDatabase<FactionDef>.GetNamedSilentFail("TribeSavage"))
        ?? Find.FactionManager.AllFactionsListForReading.FirstOrDefault(f => !f.IsPlayer && !f.Hidden && f.def.humanlikeFaction
            && f.def.techLevel == TechLevel.Neolithic && f.HostileTo(Faction.OfPlayer));

    private static Wait Play(double seconds, Action everyTick)
    {
        var wait = Wait.Seconds(seconds, true);
        wait.EveryTick = everyTick;
        return wait;
    }

    private static void Strip(Pawn pawn)
    {
        pawn.equipment?.DestroyAllEquipment();
        pawn.apparel?.DestroyAll();
        pawn.inventory?.DestroyAll();
    }

    private static void Unforbid(Map map, CellRect rect)
    {
        foreach (var t in map.listerThings.AllThings.Where(t => t.Spawned && rect.Contains(t.Position)).ToList())
            t.SetForbidden(false, warnOnFail: false);
    }

    private static bool Standing(Pawn p) => p.Spawned && !p.Dead && !p.Downed;

    public override IEnumerable<Wait> Segment(int index, BatchContext ctx)
    {
        var map = ctx.Map;
        Kit.RemoveStartingColonists(ctx);
        yield return Wait.Ticks(1);
        foreach (var w in Find.WindowStack.Windows.Where(w => w is Dialog_MessageBox || w is Dialog_NodeTree).ToList()) w.Close(false);
        Find.TickManager.CurTimeSpeed = TimeSpeed.Paused;
        MakeTribal(ctx);
        Find.Storyteller.difficulty.researchSpeedFactor = 1f;
        var recurve = Project("RecurveBow");
        Find.ResearchManager.SetCurrentProject(recurve);
        ctx.Setup("Developer set the research factor to 1 and Recurve Bow as the current project");

        map.weatherManager.curWeather = WeatherDefOf.Clear;
        map.weatherManager.lastWeather = WeatherDefOf.Clear;
        int hour = GenLocalDate.HourOfDay(map);
        Find.TickManager.DebugSetTicksGame(Find.TickManager.TicksGame + ((34 - hour) % 24) * GenDate.TicksPerHour);
        ctx.Setup("Developer set clear weather and moved the clock to 10 am");

        ModHarness.SaveSettings();
        Settings.Apply(Presets.Apply(PresetId.Progression, Settings.Capture()));
        Settings.activePreset = PresetId.Progression;
        ctx.Setup($"Developer applied the Progression preset: hostiles keep their gear {Settings.values.hostilesKeepGear}, weapons {Rules.weapons.outcome} (beyond reach only {Rules.weapons.onlyBeyondReach}), hostile row {Rules.whoHostile}");

        var tribe = Tribe ?? throw new SetupException("no hostile neolithic faction in this world");
        var neolithic = Scrap(ScrapTier.Neolithic);
        var industrial = Scrap(ScrapTier.Industrial);
        var spacer = Scrap(ScrapTier.Spacer);
        var tribal = Kit.Def("Apparel_TribalA");

        Pawn Colonist(IntVec3 cell, WorkTypeDef work, SkillDef skill, string weapon)
        {
            var p = Kit.Worker(ctx, cell, work, skill);
            Strip(p);
            Dress(p, tribal, QualityCategory.Normal);
            if (weapon != null) Arm(p, Kit.Def(weapon), QualityCategory.Normal);
            return p;
        }

        // ----- Raid: three archers behind sandbags on the west; a pirate and a tribal warrior come from the east -----
        var raidArea = Kit.Area(ctx, 26, 12);
        int rx = raidArea.minX, rz = raidArea.minZ;
        for (int z = rz + 2; z <= rz + 9; z++) Kit.Place(ctx, Kit.Def("Sandbags"), new IntVec3(rx + 4, 0, z), Rot4.North, null, quiet: true);
        var archers = new List<Pawn>();
        foreach (int z in new[] { rz + 3, rz + 6, rz + 8 })
        {
            var p = Colonist(new IntVec3(rx + 3, 0, z), WorkTypeDefOf.Firefighter, SkillDefOf.Shooting, "Bow_Short");
            p.skills.GetSkill(SkillDefOf.Shooting).Level = 12;
            archers.Add(p);
        }
        var pirate = Spawn(ctx, Mercenary, Pirates, new IntVec3(rx + 18, 0, rz + 4));
        Dress(pirate, Kit.Def("Apparel_CollarShirt"), QualityCategory.Normal);
        Dress(pirate, Kit.Def("Apparel_Pants"), QualityCategory.Normal);
        Arm(pirate, Kit.Def("Gun_Autopistol"), QualityCategory.Normal);
        pirate.skills.GetSkill(SkillDefOf.Shooting).Level = 1;
        var warrior = Spawn(ctx, DefDatabase<PawnKindDef>.GetNamed("Tribal_Warrior"), tribe, new IntVec3(rx + 17, 0, rz + 8));
        Dress(warrior, tribal, QualityCategory.Normal);
        Arm(warrior, Kit.Def("MeleeWeapon_Club"), QualityCategory.Normal);
        ctx.Setup("Raid scene: sandbags, three tribal archers (short bows, Shooting 12); a pirate with an autopistol (Shooting 1) and a tribal warrior with a club");

        // ----- Workshop: a crafting spot and a research bench, each with neolithic scrap beside it -----
        var shopArea = Kit.Area(ctx, 20, 10);
        int sx = shopArea.minX, sz = shopArea.minZ;
        var spot = Kit.Place(ctx, "CraftingSpot", new IntVec3(sx + 4, 0, sz + 5));
        SpawnStack(map, neolithic, 6, new IntVec3(sx + 6, 0, sz + 5));
        var bench = Kit.Place(ctx, "SimpleResearchBench", new IntVec3(sx + 14, 0, sz + 5), Rot4.North, ThingDefOf.WoodLog);
        SpawnStack(map, neolithic, 6, new IntVec3(sx + 17, 0, sz + 5));
        ctx.Setup("Workshop scene: a crafting spot and a simple research bench, each with 6 neolithic scrap beside it");

        // ----- On the body: a pirate who will die with everything on the body, and a colonist to strip him -----
        var bodyArea = Kit.Area(ctx, 14, 8);
        var bodyCell = new IntVec3(bodyArea.minX + 8, 0, bodyArea.minZ + 4);

        // ----- Drop modes: four tribal warriors in a row, each with a club and an autopistol in his pack -----
        var dropArea = Kit.Area(ctx, 34, 7);
        // The four drop modes of 0.5.2, as rows and category rules.
        var dropModes = new[] { ModHarness.LootStyle.ScrapOutOfReach, ModHarness.LootStyle.ScrapEverything, ModHarness.LootStyle.KeepOriginal, ModHarness.LootStyle.Off };
        var dropCells = dropModes.Select((m, i) => new IntVec3(dropArea.minX + 4 + i * 8, 0, dropArea.minZ + 3)).ToList();

        // ----- Inspect: neolithic and spacer scrap side by side -----
        var inspectArea = Kit.Area(ctx, 12, 6);
        var neolithicPile = SpawnStack(map, neolithic, 8, new IntVec3(inspectArea.minX + 3, 0, inspectArea.minZ + 3));
        var spacerPile = SpawnStack(map, spacer, 3, new IntVec3(inspectArea.minX + 8, 0, inspectArea.minZ + 3));
        ctx.Setup("Inspect scene: 8 neolithic scrap and 3 spacer scrap");

        // ----- Downed: one raider, downed under the default "Keep his weapon" -----
        var downedArea = Kit.Area(ctx, 14, 8);
        var downedCell = new IntVec3(downedArea.minX + 8, 0, downedArea.minZ + 4);

        var allAreas = new[] { raidArea, shopArea, bodyArea, dropArea, inspectArea, downedArea };
        void UnforbidAll() { foreach (var r in allAreas) Unforbid(map, r); }
        UnforbidAll();

        var config = Find.CameraDriver.config;
        var range = config.sizeRange;
        config.sizeRange = new FloatRange(StillZoom, range.max);
        bool tutor = Prefs.AdaptiveTrainingEnabled;
        Prefs.AdaptiveTrainingEnabled = false;
        ctx.Setup("Developer turned off the learning helper for the shots");
        Vector3 look = raidArea.CenterCell.ToVector3Shifted();
        float zoom = ClipZoom;
        bool holdSelection = true;
        void Camera()
        {
            Find.CameraDriver.SetRootPosAndSize(look, zoom);
            if (holdSelection && Find.Selector.NumSelected > 0) Find.Selector.ClearSelection();
        }
        void Shot(string name, Vector3 at, float size)
        {
            look = at;
            zoom = size;
            Camera();
            shots.Add((name, at, size));
            Mark(name);
        }
        int tick = 0;
        void Tidy() { if (++tick % 15 == 0) UnforbidAll(); }
        // Each scene's colonists exist only during its shot: idle colonists wander into other shots, and raiders go for them.
        void Gone(Pawn p)
        {
            if (p == null || !p.Spawned) return;
            p.DeSpawn();
            Find.WorldPawns.PassToWorld(p);
        }
        var keep = Kit.All(Camera, Tidy);

        bool raidWeaponInHand = false, raidStripped = false;
        bool weaponInHand = false, nothingFell = false, stayedDown = false, downedTabOpen = false;
        bool pirateScrapped = false, clubWhole = false, recycled = false, studied = false, nothingOnGround = false, gearTabOpen = false, stripped = false;
        int dropShots = 0, inspectShots = 0;
        float progress0 = 0, progress1 = 0;
        try
        {
            Find.Selector.ClearSelection();
            Camera();
            if (RunRecording.Enabled)
            {
                RunRecording.Ready();
                yield return Wait.Recorder();
            }
            Find.TickManager.CurTimeSpeed = TimeSpeed.Normal;

            // ----- Raid -----
            Shot("raid", new Vector3(rx + 12f, 0f, rz + 6f), ClipZoom);
            yield return Play(1.0, keep);
            foreach (var p in archers) p.drafter.Drafted = true;
            // Direct orders, not a raid lord: with nothing else of the colony's near, a lone assault lord wandered off the map.
            // The archers shoot, the pirate shoots back from where he stands, and the warrior charges the sandbags.
            ctx.Setup("Developer ordered the archers to shoot the raiders, the pirate to shoot back and the warrior to charge the sandbags");
            int orderTick = 0;
            void Orders()
            {
                if (orderTick++ % 30 != 0) return;
                for (int i = 0; i < archers.Count; i++)
                {
                    var a = archers[i];
                    var target = i == 1 && Standing(warrior) ? warrior : Standing(pirate) ? pirate : warrior;
                    if (Standing(target) && a.CurJobDef != JobDefOf.AttackStatic)
                        a.jobs.TryTakeOrderedJob(JobMaker.MakeJob(JobDefOf.AttackStatic, target));
                }
                if (Standing(pirate) && pirate.CurJobDef != JobDefOf.AttackStatic)
                    pirate.jobs.StartJob(JobMaker.MakeJob(JobDefOf.AttackStatic, archers[0]), JobCondition.InterruptForced);
                if (Standing(warrior) && warrior.CurJobDef != JobDefOf.AttackMelee)
                    warrior.jobs.StartJob(JobMaker.MakeJob(JobDefOf.AttackMelee, archers[1]), JobCondition.InterruptForced);
            }
            Mark("raid-attack");
            var industrialBefore = Kit.StackIds(map, industrial);
            bool pirateMarked = false, warriorMarked = false;
            void Falls()
            {
                if (!pirateMarked && !Standing(pirate)) { pirateMarked = true; Mark("raid-pirate-down"); }
                if (!warriorMarked && !Standing(warrior)) { warriorMarked = true; Mark("raid-warrior-down"); }
            }
            var fight = Kit.All(keep, Orders, Falls);
            // Bow fire rarely finishes anyone inside a GIF's length, so each raider has a deadline: the pirate falls by 5 seconds and
            // the warrior by 7, one beat apart.
            for (int i = 1; i <= 28 && (Standing(pirate) || Standing(warrior)); i++)
            {
                yield return Play(0.25, fight);
                if (i == 20 && Standing(pirate)) { HealthUtility.TryAnesthetize(pirate); ctx.Setup("Developer downed the pirate after 5 seconds of fighting"); }
                if (i == 28 && Standing(warrior)) { warrior.Kill(null); ctx.Setup("Developer killed the warrior after 7 seconds of fighting"); }
            }
            Falls();
            yield return Play(1.5, keep);
            // Under the defaults the downed pirate still holds his autopistol and the dead warrior keeps his club on the body.
            raidWeaponInHand = pirate.Downed && !pirate.Dead && pirate.equipment?.Primary?.def.defName == "Gun_Autopistol";
            var club = Kit.Def("MeleeWeapon_Club");
            clubWhole = map.listerThings.ThingsOfDef(club).Any(t => t.Spawned)
                || warrior.inventory.innerContainer.Any(t => t.def == club) || warrior.equipment?.Primary?.def == club;
            ctx.Setup($"After the fight: pirate {(pirate.Dead ? "dead" : "downed")} holding {pirate.equipment?.Primary?.LabelCap ?? "nothing"}, warrior {(warrior.Dead ? "dead" : "downed")}; club whole {clubWhole}");
            // The archer nearest the pirate strips him: his out-of-reach autopistol and clothes become industrial scrap.
            var raidStripper = archers.Where(Standing).OrderBy(p => p.Position.DistanceToSquared(pirate.Position)).FirstOrDefault();
            foreach (var p in archers) if (p != raidStripper) Gone(p);
            if (raidStripper != null && pirate.Spawned && !pirate.Dead)
            {
                map.designationManager.AddDesignation(new Designation(pirate, DesignationDefOf.Strip));
                raidStripper.jobs.TryTakeOrderedJob(JobMaker.MakeJob(JobDefOf.Strip, pirate));
                Mark("raid-strip-ordered");
                ctx.Setup("Developer ordered an archer to strip the downed pirate");
                bool marked = false;
                for (int i = 0; i < 80 && !raidStripped; i++)
                {
                    yield return Play(0.25, keep);
                    raidStripped = !pirate.Spawned || !pirate.AnythingToStrip();
                    if (raidStripped && !marked) { marked = true; Mark("raid-stripped"); }
                }
                // He steps back west off the pile so the scrap shows.
                raidStripper.jobs.TryTakeOrderedJob(JobMaker.MakeJob(JobDefOf.Goto, raidStripper.Position + new IntVec3(-2, 0, 1)));
            }
            yield return Play(3.0, keep);
            Gone(raidStripper);
            pirateScrapped = raidStripped && Kit.NewStacks(map, industrial, industrialBefore).Any()
                && !map.listerThings.ThingsOfDef(Kit.Def("Gun_Autopistol")).Any(t => t.Spawned && raidArea.Contains(t.Position));
            ctx.Setup($"After the strip: stripped {raidStripped}; new industrial scrap {Kit.NewStacks(map, industrial, industrialBefore).Sum(t => t.stackCount)}");
            Mark("raid-over");
            yield return Play(1.0, keep);

            // ----- Recycle -----
            var crafter = Colonist(new IntVec3(sx + 1, 0, sz + 3), WorkTypeDefOf.Crafting, SkillDefOf.Crafting, null);
            Shot("recycle", new Vector3(sx + 4.5f, 0f, sz + 5f), ClipZoom);
            var recycleBill = (Bill_Production)DefDatabase<RecipeDef>.GetNamed("LS_Recycle_Neolithic").MakeNewBill();
            recycleBill.repeatMode = BillRepeatModeDefOf.RepeatCount;
            recycleBill.repeatCount = 1;
            recycleBill.ingredientSearchRadius = 8f;
            ((IBillGiver)spot).BillStack.AddBill(recycleBill);
            bool started = false;
            for (int i = 0; i < 360 && !recycled; i++)
            {
                yield return Play(0.25, keep);
                bool working = crafter.CurJob?.RecipeDef == recycleBill.recipe && crafter.Position == spot.InteractionCell;
                if (working && !started) { started = true; Mark("recycle-work"); }
                recycled = started && crafter.CurJob?.RecipeDef != recycleBill.recipe;
            }
            Mark("recycle-done");
            ctx.Setup($"Recycle: {(recycled ? "finished" : "not finished in 90 seconds")}");
            yield return Play(2.5, keep);
            Gone(crafter);

            // ----- Study -----
            var researcher = Colonist(new IntVec3(sx + 11, 0, sz + 2), WorkTypeDefOf.Research, SkillDefOf.Intellectual, null);
            Shot("study", new Vector3(sx + 15f, 0f, sz + 5f), ClipZoom);
            var studyBill = (Bill_Production)DefDatabase<RecipeDef>.GetNamed("LS_Study_Neolithic").MakeNewBill();
            studyBill.repeatMode = BillRepeatModeDefOf.RepeatCount;
            studyBill.repeatCount = 1;
            studyBill.ingredientSearchRadius = 8f;
            ((IBillGiver)bench).BillStack.AddBill(studyBill);
            progress0 = Find.ResearchManager.GetProgress(recurve);
            started = false;
            for (int i = 0; i < 360 && !studied; i++)
            {
                yield return Play(0.25, keep);
                bool working = researcher.CurJob?.RecipeDef == studyBill.recipe && researcher.Position == bench.InteractionCell;
                if (working && !started) { started = true; Mark("study-work"); }
                studied = started && researcher.CurJob?.RecipeDef != studyBill.recipe;
            }
            progress1 = Find.ResearchManager.GetProgress(recurve);
            Mark("study-done");
            ctx.Setup($"Study: {(studied ? "finished" : "not finished in 90 seconds")}; Recurve Bow {progress0:0} -> {progress1:0}");
            yield return Play(2.5, keep);
            Gone(researcher);

            // ----- On the body -----
            Settings.values.hostilesKeepGear = true;
            ctx.Setup("Developer ticked \"Hostiles keep their gear\", so everything stays on the body");
            // Spawned now: a hostile pawn standing there through the earlier shots would go for the colonist.
            var victim = Spawn(ctx, Mercenary, Pirates, bodyCell);
            Dress(victim, tribal, QualityCategory.Normal);
            Dress(victim, Kit.Def("Apparel_FlakVest"), QualityCategory.Normal);
            Arm(victim, Kit.Def("Gun_Autopistol"), QualityCategory.Normal);
            victim.inventory.innerContainer.TryAdd(Stack(ThingDefOf.MedicineIndustrial, 3));
            victim.inventory.innerContainer.TryAdd(Stack(ThingDefOf.Silver, 40));
            victim.Rotation = Rot4.West;
            ctx.Setup("Developer spawned a pirate in tribalwear and a flak vest with an autopistol, 3 industrial medicine and 40 silver");
            // Closer than the other clips, with the corpse left of centre: the gear tab opens over the bottom-left of the screen and
            // the GIF crops the tab and the corpse together, so the tab's text stays readable at 560 px.
            Shot("on-body", new Vector3(bodyCell.x + 3.5f, 0f, bodyCell.z + 0.9f), OnBodyZoom);
            yield return Play(1.0, keep);
            var itemsBefore = map.listerThings.AllThings.Where(t => t.def.category == ThingCategory.Item).Select(t => t.thingIDNumber).ToHashSet();
            victim.Kill(null);
            FilthMaker.TryMakeFilth(bodyCell, map, ThingDefOf.Filth_Blood, 3);
            Mark("on-body-death");
            var corpse = victim.Corpse;
            yield return Play(1.5, keep);
            nothingOnGround = !map.listerThings.AllThings.Any(t => t.Spawned && t.def.category == ThingCategory.Item && t != corpse
                && !itemsBefore.Contains(t.thingIDNumber) && t.Position.DistanceTo(bodyCell) <= 3f);
            ctx.Setup($"Pirate killed with everything on the body: nothing on the ground {nothingOnGround}");
            // Spawned after the death, two cells east (away from the tab), so the walk to the corpse is short and no live raider is
            // near him. Drafted, so he neither wanders during the gear shot nor hauls the loot away after the strip.
            var stripper = Colonist(new IntVec3(bodyCell.x + 2, 0, bodyCell.z), WorkTypeDefOf.Hauling, null, null);
            stripper.drafter.Drafted = true;
            if (corpse != null)
            {
                // The corpse's Gear tab shows what is still on the body before the strip.
                holdSelection = false;
                Find.Selector.ClearSelection();
                Find.Selector.Select(corpse, playSound: false, forceDesignatorDeselect: false);
                yield return Wait.Frames(2);
                gearTabOpen = InspectPaneUtility.OpenTab(typeof(ITab_Pawn_Gear)) != null;
                Mark("on-body-gear");
                ctx.Setup($"Gear tab open {gearTabOpen}; on the body: {Describe(victim.apparel.WornApparel.Cast<Thing>().Concat(victim.equipment.AllEquipmentListForReading).Concat(victim.inventory.innerContainer))}");
                yield return Play(2.5, keep);
                Find.Selector.ClearSelection();
                holdSelection = true;

                corpse.SetForbidden(false, warnOnFail: false);
                map.designationManager.AddDesignation(new Designation(corpse, DesignationDefOf.Strip));
                stripper.jobs.TryTakeOrderedJob(JobMaker.MakeJob(JobDefOf.Strip, corpse));
                Mark("on-body-strip-ordered");
                bool marked = false;
                for (int i = 0; i < 80 && !stripped; i++)
                {
                    yield return Play(0.25, keep);
                    stripped = corpse.Destroyed || !corpse.AnythingToStrip();
                    if (stripped && !marked) { marked = true; Mark("on-body-stripped"); }
                }
            }
            ctx.Setup($"Strip: {(stripped ? "done" : "not done in 20 seconds")}; on the ground now: {Describe(map.listerThings.AllThings.Where(t => t.Spawned && t.def.category == ThingCategory.Item && t.Position.DistanceTo(bodyCell) <= 3f))}");
            // He steps off the pile so the spilled loot shows.
            stripper.jobs.TryTakeOrderedJob(JobMaker.MakeJob(JobDefOf.Goto, new IntVec3(bodyCell.x + 4, 0, bodyCell.z + 1)));
            yield return Play(3.5, keep);
            Settings.values.hostilesKeepGear = false;
            Gone(stripper);

            // ----- Downed, under the default: he keeps his weapon in hand and nothing falls -----
            Settings.values.hostilesKeepGear = true;
            ctx.Setup("Developer ticked \"Hostiles keep their gear\" again, the default, so a downed pawn keeps his weapon");
            var raider = Spawn(ctx, Mercenary, Pirates, downedCell);
            Dress(raider, Kit.Def("Apparel_CollarShirt"), QualityCategory.Normal);
            Dress(raider, Kit.Def("Apparel_Pants"), QualityCategory.Normal);
            Dress(raider, Kit.Def("Apparel_FlakVest"), QualityCategory.Normal);
            Arm(raider, Kit.Def("Gun_Autopistol"), QualityCategory.Normal);
            raider.Rotation = Rot4.West;
            ctx.Setup("Developer spawned a pirate in a collar shirt, pants and a flak vest with an autopistol");
            // Closer to centre than the on-body shot: the Gear tab opens over the left edge and must not cover the raider.
            Shot("downed", new Vector3(downedCell.x + 1.0f, 0f, downedCell.z + 0.9f), OnBodyZoom);
            yield return Play(1.5, keep);
            var downedItemsBefore = map.listerThings.AllThings.Where(t => t.def.category == ThingCategory.Item).Select(t => t.thingIDNumber).ToHashSet();
            // Anesthetic, as in the raid beat: he stays down for hours, so he cannot get up during the shot.
            HealthUtility.TryAnesthetize(raider);
            Mark("downed-fall");
            yield return Play(1.5, keep);
            stayedDown = raider.Downed && !raider.Dead;
            weaponInHand = raider.equipment.Primary?.def.defName == "Gun_Autopistol";
            nothingFell = !map.listerThings.AllThings.Any(t => t.Spawned && t.def.category == ThingCategory.Item
                && !downedItemsBefore.Contains(t.thingIDNumber) && t.Position.DistanceTo(downedCell) <= 3f);
            ctx.Setup($"Pirate downed keeping his weapon: downed {stayedDown}, autopistol in hand {weaponInHand}, nothing on the ground {nothingFell}");
            holdSelection = false;
            Find.Selector.ClearSelection();
            Find.Selector.Select(raider, playSound: false, forceDesignatorDeselect: false);
            yield return Wait.Frames(2);
            downedTabOpen = InspectPaneUtility.OpenTab(typeof(ITab_Pawn_Gear)) != null;
            Mark("downed-gear");
            ctx.Setup($"Gear tab open {downedTabOpen}; equipment: {Describe(raider.equipment.AllEquipmentListForReading.Cast<Thing>())}; apparel: {Describe(raider.apparel.WornApparel.Cast<Thing>())}; pack: {Describe(raider.inventory.innerContainer)}");
            yield return Play(3.0, keep);
            stayedDown = stayedDown && raider.Downed && !raider.Dead;
            Find.Selector.ClearSelection();
            holdSelection = true;
            Gone(raider);
            // The drop-mode stills show what lands beside the body.
            Settings.values.hostilesKeepGear = false;

            // ----- Drop-mode stills (paused) -----
            Find.TickManager.CurTimeSpeed = TimeSpeed.Paused;
            for (int i = 0; i < dropModes.Length; i++)
            {
                ModHarness.ApplyStyle(Rules, dropModes[i]);
                Unfamiliar.ResetCache();
                var cell = dropCells[i];
                var w = Spawn(ctx, DefDatabase<PawnKindDef>.GetNamed("Tribal_Warrior"), tribe, cell);
                Dress(w, tribal, QualityCategory.Normal);
                Arm(w, Kit.Def("MeleeWeapon_Club"), QualityCategory.Normal);
                w.inventory.innerContainer.TryAdd(Make(Kit.Def("Gun_Autopistol"), QualityCategory.Normal));
                w.Rotation = Rot4.East;
                w.Kill(null);
                bool naked = w.Corpse != null && !w.apparel.WornApparel.Any();
                if (naked)
                {
                    // Scrap everything took the tribalwear too; dressed again so the body reads as a tribal raider.
                    Dress(w, tribal, QualityCategory.Normal);
                    w.Drawer.renderer.SetAllGraphicsDirty();
                }
                FilthMaker.TryMakeFilth(cell, map, ThingDefOf.Filth_Blood, 2);
                Unforbid(map, dropArea);
                var near = map.listerThings.AllThings.Where(t => t.Spawned && t.def.category == ThingCategory.Item && t.Position.DistanceTo(cell) <= 3f).ToList();
                ctx.Setup($"Drop mode {dropModes[i]}: warrior killed at {cell}{(naked ? " (redressed)" : "")}; beside him: {Describe(near)}");
                Shot("drop-" + dropModes[i].ToString().ToLowerInvariant(), cell.ToVector3Shifted() + new Vector3(0.5f, 0f, 0f), StillZoom);
                var gun = near.FirstOrDefault(t => t.def.defName == "Gun_Autopistol");
                if (dropModes[i] == ModHarness.LootStyle.KeepOriginal && gun != null)
                {
                    holdSelection = false;
                    Find.Selector.Select(gun, playSound: false, forceDesignatorDeselect: false);
                }
                yield return Wait.Seconds(StillSeconds);
                holdSelection = true;
                Find.Selector.ClearSelection();
                dropShots++;
            }
            ModHarness.ApplyStyle(Rules, ModHarness.LootStyle.ScrapOutOfReach);

            // ----- Inspect stills (paused) -----
            holdSelection = false;
            foreach (var (name, pile) in new[] { ("inspect-neolithic", neolithicPile), ("inspect-spacer", spacerPile) })
            {
                Find.Selector.ClearSelection();
                Find.Selector.Select(pile, playSound: false, forceDesignatorDeselect: false);
                Shot(name, pile.Position.ToVector3Shifted(), StillZoom);
                ctx.Setup($"{name}: selected {pile.LabelCap}; inspect: {pile.GetInspectString().Replace("\n", " | ")}");
                yield return Wait.Seconds(StillSeconds);
                inspectShots++;
            }
            Find.Selector.ClearSelection();
            Mark("end");
            // The recorder can lose the last seconds when the game quits.
            yield return Wait.Seconds(3.0);
        }
        finally
        {
            config.sizeRange = range;
            Prefs.AdaptiveTrainingEnabled = tutor;
            Find.TickManager.CurTimeSpeed = TimeSpeed.Paused;
            string dir = RunDir();
            File.WriteAllText(Path.Combine(dir, "shots.json"),
                "{\"marks\":[" + string.Join(",", marks.Select(m => $"{{\"name\":\"{m.name}\",\"t\":{Num(m.t)}}}")) + "]}");
            File.WriteAllText(Path.Combine(dir, "layout.json"),
                "{\"shots\":[" + string.Join(",", shots.Select(s => $"{{\"name\":\"{s.name}\",\"x\":{Num(s.look.x)},\"z\":{Num(s.look.z)},\"zoom\":{Num(s.zoom)}}}")) + "]}");
        }

        ctx.Begin("workshop-clips.filmed")
            .Observe("raidWeaponInHand", raidWeaponInHand).Observe("raidStripped", raidStripped).Observe("pirateScrapped", pirateScrapped).Observe("clubWhole", clubWhole).Observe("recycled", recycled)
            .Observe("studied", studied).Observe("researchGain", progress1 - progress0).Observe("nothingOnGround", nothingOnGround)
            .Observe("downedWeaponInHand", weaponInHand).Observe("downedNothingFell", nothingFell).Observe("downedStayedDown", stayedDown).Observe("downedTabOpen", downedTabOpen)
            .Observe("gearTabOpen", gearTabOpen).Observe("stripped", stripped).Observe("dropShots", dropShots).Observe("inspectShots", inspectShots)
            .Observe("marks", string.Join(", ", marks.Select(m => m.name)))
            .Check(raidWeaponInHand && raidStripped && pirateScrapped && clubWhole && recycled && studied && progress1 > progress0 && nothingOnGround && gearTabOpen && weaponInHand && nothingFell && stayedDown && downedTabOpen && stripped && dropShots == 4 && inspectShots == 2,
                "a beat did not happen on film: the downed raider's pack, raid scrap, whole club, recycle, study, nothing on the ground, the gear tab, strip, or a still");
    }
}
