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

// Films the milestone devlog clip with --record --show-ui: a tribal colony behind sandbags fights off a tribal raid, and with the
// drop mode on "scrap everything" all of the raiders' gear becomes neolithic scrap. The colony studies part of it into Recurve Bow and recycles the rest at the crafting spot. Then
// a pirate with a charge rifle dies and leaves spacer scrap that no bench here can use, shown selected with its inspect line.
// Writes shots.json (marks on the game's monotonic clock, the recorder's video origin clock) for trimming. Not a gameplay test:
// the case passes when each beat happened on film. Not part of run-all.
public sealed class MilestoneBatch : Batch
{
    public override string Id => "lootscavenging-milestone";
    public override bool IsShowcase => true;
    public override int SegmentCount => 1;
    public override TimeSpan WallLimit => TimeSpan.FromMinutes(10);
    public override IReadOnlyList<PlannedCase> Plan { get; } = new[] { new PlannedCase("milestone.filmed", Levels.Evidence) };

    private const int Raiders = 6; // five archers with recurve bows and one club warrior, whose club drops as normal
    private const float WideZoom = 10.5f, CloseZoom = 6f;
    // Bill jobs play at four times game speed; the post says so.
    private const double JobSpeed = 4;

    private readonly List<(string name, double t)> marks = new();
    private static double Now() => Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency;
    private void Mark(string name) => marks.Add((name, Now())); // not translated

    private static string RunDir()
    {
        string pointer = Path.Combine(TestRunner.EvidenceRoot, "active.txt");
        string dir = File.Exists(pointer) ? File.ReadAllText(pointer).Trim() : null;
        return !string.IsNullOrEmpty(dir) && Directory.Exists(dir) ? dir : TestRunner.EvidenceRoot;
    }

    private static Wait Play(double seconds, Action everyTick, double speed = 1)
    {
        var wait = Wait.Seconds(seconds, true, speed);
        wait.EveryTick = everyTick;
        return wait;
    }

    private static Faction Tribe => Find.FactionManager.FirstFactionOfDef(DefDatabase<FactionDef>.GetNamedSilentFail("TribeSavage"))
        ?? Find.FactionManager.AllFactionsListForReading.FirstOrDefault(f => !f.IsPlayer && !f.Hidden && f.def.humanlikeFaction
            && f.def.techLevel == TechLevel.Neolithic && f.HostileTo(Faction.OfPlayer));

    private static void Outfit(Pawn pawn, string weapon)
    {
        pawn.equipment?.DestroyAllEquipment();
        pawn.apparel?.DestroyAll();
        pawn.inventory?.DestroyAll();
        Dress(pawn, Kit.Def("Apparel_TribalA"), QualityCategory.Normal);
        Arm(pawn, Kit.Def(weapon), QualityCategory.Normal);
    }

    private static void Heal(Pawn pawn)
    {
        foreach (var h in pawn.health.hediffSet.hediffs.OfType<Hediff_Injury>().ToList()) pawn.health.RemoveHediff(h);
    }

    private static bool Fleeing(Pawn p) =>
        p.GetLord()?.CurLordToil is LordToil_PanicFlee || p.CurJobDef == JobDefOf.Flee || p.CurJobDef == JobDefOf.FleeAndCower
        || p.CurJob?.exitMapOnArrival == true || p.InMentalState;

    private static void Assault(Faction faction, Map map, List<Pawn> pawns) =>
        LordMaker.MakeNewLord(faction, new LordJob_AssaultColony(faction, canKidnap: false, canTimeoutOrFlee: false, canSteal: false), map, pawns);

    public override IEnumerable<Wait> Segment(int index, BatchContext ctx)
    {
        var map = ctx.Map;
        Kit.RemoveStartingColonists(ctx);
        yield return Wait.Ticks(1);
        foreach (var w in Find.WindowStack.Windows.Where(w => w is Dialog_MessageBox || w is Dialog_NodeTree).ToList()) w.Close(false);
        MakeTribal(ctx);
        Find.Storyteller.difficulty.researchSpeedFactor = 1f;
        var recurve = Project("RecurveBow");
        Find.ResearchManager.SetCurrentProject(recurve);
        ctx.Setup("Developer set the research factor to 1 and Recurve Bow as the current project");

        map.weatherManager.curWeather = WeatherDefOf.Clear;
        map.weatherManager.lastWeather = WeatherDefOf.Clear;
        int hour = GenLocalDate.HourOfDay(map);
        Find.TickManager.DebugSetTicksGame(Find.TickManager.TicksGame + ((33 - hour) % 24) * GenDate.TicksPerHour);
        ctx.Setup("Developer set clear weather and moved the clock to 9 am");

        ModHarness.SaveSettings();
        Rules.dropMode = DropMode.ScrapEverything;
        // Filmed before ticket 21: a raider who falls drops his gear, and it is scrapped as it drops.
        Rules.downedGear = DownedGear.DropWeapon;
        ctx.Setup("Developer set the drop mode to scrap everything and downed pawns to drop the weapon, so every item the raiders drop becomes scrap");

        var tribe = Tribe ?? throw new SetupException("no hostile neolithic faction in this world");
        var area = Kit.Area(ctx, 30, 18);
        int x0 = area.minX, z0 = area.minZ;

        // A small walled camp open to the east, where a line of sandbags faces the raiders.
        var wood = ThingDefOf.WoodLog;
        for (int x = x0 + 1; x <= x0 + 15; x++)
        {
            Kit.Place(ctx, ThingDefOf.Wall, new IntVec3(x, 0, z0), Rot4.North, wood, quiet: true);
            Kit.Place(ctx, ThingDefOf.Wall, new IntVec3(x, 0, z0 + 17), Rot4.North, wood, quiet: true);
        }
        for (int z = z0 + 1; z <= z0 + 16; z++)
        {
            Kit.Place(ctx, ThingDefOf.Wall, new IntVec3(x0 + 1, 0, z), Rot4.North, wood, quiet: true);
            Kit.Place(ctx, Kit.Def("Sandbags"), new IntVec3(x0 + 15, 0, z), Rot4.North, null, quiet: true);
        }
        ctx.Setup("God-mode built a wooden-walled camp with a sandbag line along its east side");

        var spot = Kit.Place(ctx, "CraftingSpot", new IntVec3(x0 + 5, 0, z0 + 12));
        var bench = Kit.Place(ctx, "SimpleResearchBench", new IntVec3(x0 + 5, 0, z0 + 5), Rot4.North, wood);

        var crafter = Kit.Worker(ctx, new IntVec3(x0 + 14, 0, z0 + 12), WorkTypeDefOf.Crafting, SkillDefOf.Crafting);
        var researcher = Kit.Worker(ctx, new IntVec3(x0 + 14, 0, z0 + 6), WorkTypeDefOf.Research, SkillDefOf.Intellectual);
        var hunters = new[]
        {
            Kit.Worker(ctx, new IntVec3(x0 + 14, 0, z0 + 14), WorkTypeDefOf.Firefighter, SkillDefOf.Shooting),
            Kit.Worker(ctx, new IntVec3(x0 + 14, 0, z0 + 9), WorkTypeDefOf.Firefighter, SkillDefOf.Shooting),
            Kit.Worker(ctx, new IntVec3(x0 + 14, 0, z0 + 3), WorkTypeDefOf.Firefighter, SkillDefOf.Shooting),
        };
        var colonists = new List<Pawn> { crafter, researcher };
        colonists.AddRange(hunters);
        foreach (var p in colonists)
        {
            Outfit(p, "Bow_Short");
            p.skills.GetSkill(SkillDefOf.Shooting).Level = Math.Max(p.skills.GetSkill(SkillDefOf.Shooting).Level, 10);
        }
        ctx.Setup("Developer dressed the five colonists in tribalwear with short bows (both within reach) and Shooting at least 10");

        var raiders = new List<Pawn>();
        for (int i = 0; i < Raiders; i++)
        {
            var kind = DefDatabase<PawnKindDef>.GetNamed(i == Raiders - 1 ? "Tribal_Warrior" : "Tribal_Archer");
            var raider = Spawn(ctx, kind, tribe, new IntVec3(area.maxX - 2, 0, z0 + 4 + i * 2));
            Outfit(raider, i == Raiders - 1 ? "MeleeWeapon_Club" : "Bow_Recurve");
            raider.skills.GetSkill(SkillDefOf.Shooting).Level = 2;
            raiders.Add(raider);
        }
        ctx.Setup($"Developer set the raiders' Shooting to 2 and armed {Raiders - 1} {tribe.Name} archers with recurve bows (Recurve Bow unresearched) and one warrior with a club");

        var wide = area.CenterCell.ToVector3Shifted() + new Vector3(2f, 0f, 0f);
        var config = Find.CameraDriver.config;
        var range = config.sizeRange;
        config.sizeRange = new FloatRange(CloseZoom, range.max);
        bool tutor = Prefs.AdaptiveTrainingEnabled;
        Prefs.AdaptiveTrainingEnabled = false;
        ctx.Setup("Developer turned off the learning helper for the shot");
        Vector3 look = wide;
        bool holdSelection = true;
        float zoom = WideZoom;
        void Camera()
        {
            Find.CameraDriver.SetRootPosAndSize(look, zoom);
            // A click in the game window would open a pawn's tabs on the shot.
            if (holdSelection && Find.Selector.NumSelected > 0) Find.Selector.ClearSelection();
        }
        var rest = Kit.All(Camera, Kit.KeepRested(crafter), Kit.KeepRested(researcher));

        var neolithic = Scrap(ScrapTier.Neolithic);
        var spacer = Scrap(ScrapTier.Spacer);
        bool raidOver = false, studied = false, recycled = false, spacerDropped = false;
        int neolithicUnits = 0, studyUnits = 0, spacerUnits = 0;
        Thing spacerPile = null;
        try
        {
            Find.TickManager.CurTimeSpeed = TimeSpeed.Normal;
            Find.Selector.ClearSelection();
            Camera();
            if (RunRecording.Enabled)
            {
                RunRecording.Ready();
                yield return Wait.Recorder();
            }

            // ----- The raid -----
            Mark("start");
            yield return Play(1.0, rest);
            foreach (var p in colonists) p.drafter.Drafted = true;
            Assault(tribe, map, raiders);
            Mark("raid");
            bool Standing(Pawn p) => p.Spawned && !p.Dead && !p.Downed;
            // A raider who breaks and runs is shot down where he turns, so his scrap lands on camera instead of at the map edge.
            int fled = 0;
            void Routed()
            {
                foreach (var p in raiders)
                    if (Standing(p) && Fleeing(p)) { p.Kill(null); fled++; }
            }
            var fight = Kit.All(rest, Routed);
            for (int i = 0; i < 48 && raiders.Any(Standing); i++) yield return Play(0.25, fight);
            if (fled > 0) ctx.Setup($"Developer killed {fled} raider(s) as they turned to flee");
            int leftStanding = raiders.Count(Standing);
            foreach (var p in raiders.Where(Standing).ToList()) p.Kill(null);
            if (leftStanding > 0) ctx.Setup($"Developer killed {leftStanding} raider(s) still standing after 12 seconds of fighting");
            raidOver = true;
            Mark("raid-over");
            yield return Play(2.0, rest);

            foreach (var p in colonists)
            {
                Heal(p);
                p.drafter.Drafted = false;
            }
            ctx.Setup("Developer healed the colonists' injuries and undrafted them");
            neolithicUnits = Kit.CountOnMap(map, neolithic);
            ctx.Setup($"{neolithicUnits} neolithic scrap on the map after the raid; {Kit.CountOnMap(map, Kit.Def("MeleeWeapon_Club")) + Kit.CountOnMap(map, Kit.Def("Bow_Recurve"))} raider weapon(s) dropped whole");
            yield return Play(1.5, rest);

            // ----- Study, then recycle -----
            var studyBill = (Bill_Production)DefDatabase<RecipeDef>.GetNamed("LS_Study_Neolithic").MakeNewBill();
            studyBill.repeatMode = BillRepeatModeDefOf.TargetCount;
            // One study job (at most 5 units); the rest is recycled.
            studyBill.targetCount = Math.Max(1, neolithicUnits - 5);
            ((IBillGiver)bench).BillStack.AddBill(studyBill);
            var recycleBill = (Bill_Production)DefDatabase<RecipeDef>.GetNamed("LS_Recycle_Neolithic").MakeNewBill();
            recycleBill.repeatMode = BillRepeatModeDefOf.Forever;
            recycleBill.suspended = true;
            ((IBillGiver)spot).BillStack.AddBill(recycleBill);
            ctx.Setup($"Bills: study neolithic scrap keeping {studyBill.targetCount} in stock; recycle neolithic scrap, suspended until study ends");
            float progress0 = Find.ResearchManager.GetProgress(recurve);
            Mark("study");
            bool started = false;
            for (int i = 0; i < 240 && !studied; i++)
            {
                yield return Play(0.25, rest, JobSpeed);
                bool inJob = researcher.CurJob?.RecipeDef == studyBill.recipe;
                started |= inJob;
                studied = started && !inJob;
            }
            studyUnits = neolithicUnits - Kit.CountOnMap(map, neolithic);
            ctx.Setup($"Study: {studyUnits} units used, Recurve Bow {progress0:0} -> {Find.ResearchManager.GetProgress(recurve):0}");
            Mark("study-done");
            yield return Play(1.5, rest);

            recycleBill.suspended = false;
            Mark("recycle");
            for (int i = 0; i < 240 && !recycled; i++)
            {
                yield return Play(0.25, rest, JobSpeed);
                recycled = Kit.CountOnMap(map, neolithic) == 0;
            }
            Mark("recycle-done");
            yield return Play(2.0, rest);

            // ----- The spacer pirate -----
            var pirate = Spawn(ctx, Mercenary, Pirates, new IntVec3(area.maxX - 2, 0, z0 + 9));
            Outfit(pirate, "Gun_ChargeRifle");
            pirate.apparel.DestroyAll();
            Dress(pirate, Kit.Def("Apparel_CollarShirt"));
            Dress(pirate, Kit.Def("Apparel_Pants"));
            ctx.Setup("Developer armed a pirate with a charge rifle (spacer)");
            var before = Kit.StackIds(map, spacer);
            foreach (var p in colonists) p.drafter.Drafted = true;
            Assault(pirate.Faction, map, new List<Pawn> { pirate });
            Mark("pirate");
            for (int i = 0; i < 32 && Standing(pirate); i++) yield return Play(0.25, rest);
            if (Standing(pirate))
            {
                pirate.Kill(null);
                ctx.Setup("Developer killed the pirate after 8 seconds of fighting");
            }
            Mark("pirate-down");
            yield return Play(1.5, rest);
            foreach (var p in colonists) p.drafter.Drafted = false;
            spacerPile = Kit.NewStacks(map, spacer, before).FirstOrDefault();
            spacerUnits = Kit.CountOnMap(map, spacer);
            spacerDropped = spacerPile != null;

            // ----- Close on the spacer scrap -----
            if (spacerDropped)
            {
                Mark("spacer-close");
                Vector3 from = look, to = spacerPile.Position.ToVector3Shifted() + new Vector3(2.5f, 0f, 0f);
                for (int i = 1; i <= 20; i++)
                {
                    float t = i / 20f;
                    look = Vector3.Lerp(from, to, t);
                    zoom = Mathf.Lerp(WideZoom, CloseZoom, t);
                    yield return Play(0.05, rest);
                }
                holdSelection = false;
                Find.Selector.Select(spacerPile, playSound: false, forceDesignatorDeselect: false);
                Mark("spacer-selected");
                yield return Play(5.0, rest);
            }
            Mark("end");
            // The recorder can lose the last seconds when the game quits; this hold is what it loses.
            yield return Play(8.0, rest);
        }
        finally
        {
            config.sizeRange = range;
            Prefs.AdaptiveTrainingEnabled = tutor;
            Find.TickManager.CurTimeSpeed = TimeSpeed.Paused;
            File.WriteAllText(Path.Combine(RunDir(), "shots.json"),
                "{\"marks\":[" + string.Join(",", marks.Select(m => string.Format(CultureInfo.InvariantCulture, "{{\"name\":\"{0}\",\"t\":{1:0.000}}}", m.name, m.t))) + "]}");
        }

        ctx.Begin("milestone.filmed")
            .Expect("neolithicUnits", ">0").Observe("neolithicUnits", neolithicUnits).Observe("studyUnits", studyUnits)
            .Observe("studied", studied).Observe("recycled", recycled).Observe("spacerUnits", spacerUnits)
            .Observe("marks", string.Join(", ", marks.Select(m => m.name)))
            .Check(raidOver && neolithicUnits > 0 && studied && recycled && spacerDropped,
                "a beat did not happen on film: raid, neolithic scrap, study, recycle or spacer scrap");
    }
}
