using System;
using System.Collections.Generic;
using System.Linq;
using LootScavenging.Model;
using RimWorld;
using RimWorldDevTests;
using Verse;
using Verse.AI;
using static LootScavenging.DevTests.Fixtures;

namespace LootScavenging.DevTests;

// Real bill jobs (tickets 04, 05, 07, 15): a crafter at a machining table and a fabrication bench, a researcher at a research
// bench, each the only pawn with that work. The colony is medieval (Smithing done), so medieval scrap is studied for
// Complex Clothing and a charge rifle is out of reach.
public sealed class BenchBatch : Batch
{
    private const string P = "lootscavenging.bench.";
    private const string Recycle = P + "recycle-bill", RecycleStudied = P + "recycle-studied-bill", RecycleBulk = P + "recycle-bulk-bill", StudyBench = P + "study-bench-tab",
        NoProject = P + "study-no-project", StudyBill = P + "study-bill", StudyFinish = P + "study-finishes-project",
        OriginalRecycle = P + "original-recycle", OriginalStudy = P + "original-study", Techprint = P + "techprint-project", NoErrors = P + "no-mod-errors";

    public override string Id => "lootscavenging-bench";
    public override int SegmentCount => 1;
    public override int MapSize => 100;
    public override TimeSpan WallLimit => TimeSpan.FromMinutes(20);

    public override IReadOnlyList<PlannedCase> Plan => new[]
    {
        new PlannedCase(Recycle, Levels.PlayerWorkflow, "a recycling bill on 12 industrial units runs 5, 5, 2, hauls at most 5, yields pool materials, offers no keep-in-stock"),
        new PlannedCase(RecycleStudied, Levels.PlayerWorkflow, "a studied-scrap recycling bill returns about half the value"),
        new PlannedCase(RecycleBulk, Levels.PlayerWorkflow, "a x10 recycling bill never starts on 9 units; on 63 it runs 50 then 13, with ten times the work per job"),
        new PlannedCase(StudyBench, Levels.Mechanical, "the research bench is a study bench with a Bills tab"),
        new PlannedCase(NoProject, Levels.PlayerWorkflow, "with no current project the study bill never starts"),
        new PlannedCase(StudyBill, Levels.PlayerWorkflow, "keep 3 of 8: one iteration studies 5 for 37.5 points, work 1125, Intellectual XP gained, 3 left"),
        new PlannedCase(StudyFinish, Levels.PlayerWorkflow, "15 points short: the bill studies 2 units, work 450, and finishes the project"),
        new PlannedCase(OriginalRecycle, Levels.PlayerWorkflow, "an unticked unfamiliar rifle is never taken; ticked, it is recycled as its scrap; a lower bench refuses it"),
        new PlannedCase(OriginalStudy, Levels.Mechanical, "an unfamiliar rifle above the colony's era is refused; in a colony of its era it studies as its scrap equivalent"),
        new PlannedCase(Techprint, Levels.Mechanical, "a techprint project gets nothing with the setting off, and stops a point short with it on"),
        new PlannedCase(NoErrors, Levels.Mechanical, "no error naming the mod logged during the batch"),
    };

    // Follows one pawn through one bill job of a recipe: progress and XP at its start and end, the highest work left and the
    // largest stack it carried of a def.
    private sealed class JobWatch
    {
        public RecipeDef Recipe; public Pawn Pawn; public ThingDef Carried;
        public bool Started, Ended; public float P0, P1, Xp0, Xp1, MaxWork; public int MaxCarried;
        public ResearchProjectDef Project;
        public void Tick()
        {
            var carried = Pawn.carryTracker?.CarriedThing;
            if (Carried != null && carried?.def == Carried) MaxCarried = Math.Max(MaxCarried, carried.stackCount);
            bool inJob = Pawn.CurJob?.RecipeDef == Recipe;
            if (inJob && !Started)
            {
                Started = true;
                P0 = Project == null ? 0f : Find.ResearchManager.GetProgress(Project);
                Xp0 = Xp();
            }
            if (inJob && Pawn.jobs.curDriver is JobDriver_DoBill d) MaxWork = Math.Max(MaxWork, d.workLeft);
            if (Started && !inJob && !Ended)
            {
                Ended = true;
                P1 = Project == null ? 0f : Find.ResearchManager.GetProgress(Project);
                Xp1 = Xp();
            }
        }

        // XpTotalEarned sums whole levels only, so the XP towards the next level is added.
        private float Xp()
        {
            var skill = Pawn.skills.GetSkill(SkillDefOf.Intellectual);
            return skill.XpTotalEarned + skill.xpSinceLastLevel;
        }
    }

    public override IEnumerable<Wait> Segment(int index, BatchContext ctx)
    {
        long modErrorsAtStart = CaseErrors.ModMark;
        ModHarness.SaveSettings();
        var area = Kit.Area(ctx, 44, 20);
        Kit.RemoveStartingColonists(ctx);
        yield return Wait.Ticks(1);
        var map = ctx.Map;
        MakeTribal(ctx);
        Kit.Research(ctx, "Smithing");
        var difficulty = Find.Storyteller.difficulty;
        difficulty.researchSpeedFactor = 1f;
        ctx.Setup("Developer set the storyteller research factor to 1");

        int row = area.minZ + 13;
        var table = Kit.Place(ctx, "TableMachining", new IntVec3(area.minX + 5, 0, row));
        var fab = Kit.Place(ctx, "FabricationBench", new IntVec3(area.minX + 16, 0, row));
        var bench = Kit.Place(ctx, "SimpleResearchBench", new IntVec3(area.minX + 30, 0, row));
        Kit.HoldPower(ctx, table, true);
        Kit.HoldPower(ctx, fab, true);
        var crafter = Kit.Worker(ctx, new IntVec3(area.minX + 8, 0, area.minZ + 3), DefDatabase<WorkTypeDef>.GetNamed("Smithing"), SkillDefOf.Crafting);
        var researcher = Kit.Worker(ctx, new IntVec3(area.minX + 30, 0, area.minZ + 3), WorkTypeDefOf.Research, SkillDefOf.Intellectual);
        var rest = Kit.All(Kit.KeepRested(crafter), Kit.KeepRested(researcher));
        var industrial = Scrap(ScrapTier.Industrial);

        // Change in each pool material on the map, as things, over a window.
        Dictionary<ThingDef, int> Counts(IEnumerable<ThingDef> defs) => defs.Distinct().ToDictionary(d => d, d => Kit.CountOnMap(map, d));
        float GainedValue(Dictionary<ThingDef, int> before, out string desc)
        {
            var gained = before.Select(kv => (kv.Key, Kit.CountOnMap(map, kv.Key) - kv.Value)).Where(x => x.Item2 != 0).ToList();
            desc = string.Join(", ", gained.Select(x => x.Item2 + " " + x.Key.defName));
            return gained.Sum(x => x.Item2 * x.Key.BaseMarketValue);
        }

        Bill_Production AddBill(Thing giver, string recipe)
        {
            var bill = (Bill_Production)DefDatabase<RecipeDef>.GetNamed(recipe).MakeNewBill();
            ((IBillGiver)giver).BillStack.AddBill(bill);
            return bill;
        }

        // ---- 04/15: recycling bill ---------------------------------------------------------------------------------------
        {
            var c = ctx.Begin(Recycle);
            c.Subject(crafter);
            var tier = TierDef(ScrapTier.Industrial);
            var bill = AddBill(table, "LS_Recycle_Industrial");
            bill.repeatMode = BillRepeatModeDefOf.Forever;
            SpawnStack(map, industrial, 12, table.Position + new IntVec3(0, 0, -4));
            var before = Counts(tier.pool.Select(e => e.thing));
            var watch = new JobWatch { Recipe = bill.recipe, Pawn = crafter, Carried = industrial };
            var seq = new List<int> { Kit.CountOnMap(map, industrial) };
            var done = Wait.Until(() => Kit.CountOnMap(map, industrial) == 0, 20000, rest);
            done.AfterTick += () => { watch.Tick(); int now = Kit.CountOnMap(map, industrial); if (now != seq.Last()) seq.Add(now); };
            yield return done;
            float value = GainedValue(before, out string desc);
            c.Observe("scrapLeftSequence", string.Join(">", seq)).Observe("maxCarried", watch.MaxCarried).Observe("products", desc)
                .Expect("value", 12 * tier.valuePerUnit).Observe("value", value);
            if (done.TimedOut)
                c.WorkflowInconclusive("the crafter did not recycle all 12 units in 20000 ticks", (crafter.Spawned && !crafter.Dead, "the crafter is alive"),
                    (((Building_WorkTable)table).CurrentlyUsableForBills(), "the machining table is usable"));
            else
            {
                c.Step(seq.SequenceEqual(new[] { 12, 7, 2, 0 }), $"the bill uses the pile 5, 5 and 2 at a time ({string.Join(">", seq)})");
                c.Step(watch.MaxCarried > 0 && watch.MaxCarried <= tier.batchSize, $"the crafter never carries more than a batch ({watch.MaxCarried})");
                c.Step(value >= 0.4f * 12 * tier.valuePerUnit && value <= 1.6f * 12 * tier.valuePerUnit, $"12 units return about {12 * tier.valuePerUnit} silver of materials ({value:0})");
                c.Step(!bill.recipe.WorkerCounter.CanCountProducts(bill), "the bill offers no keep-in-stock count");
                c.Conclude();
            }
            bill.suspended = true;
        }
        {
            var c = ctx.Begin(RecycleStudied);
            var tier = TierDef(ScrapTier.Industrial);
            var bill = AddBill(table, "LS_RecycleStudied_Industrial");
            bill.repeatMode = BillRepeatModeDefOf.Forever;
            // 20, not 10: 0.5.2 halved a unit's value, and 10 units came to 27.5 silver, less than one component (83 drawn).
            const int Units = 20;
            SpawnStack(map, tier.studiedScrap, Units, table.Position + new IntVec3(0, 0, -4));
            var before = Counts(tier.pool.Select(e => e.thing));
            var done = Wait.Until(() => Kit.CountOnMap(map, tier.studiedScrap) == 0, 30000, rest);
            yield return done;
            float value = GainedValue(before, out string desc);
            float expected = Units * tier.valuePerUnit * tier.studiedYieldFactor;
            c.Expect("value", expected).Observe("value", value).Observe("products", desc);
            if (done.TimedOut)
                c.WorkflowInconclusive("the crafter did not recycle the studied scrap in 30000 ticks", (crafter.Spawned && !crafter.Dead, "the crafter is alive"));
            else
            {
                c.Step(value > 0f && value <= 2f * expected, $"{Units} studied units return about {expected} silver ({value:0}), half of scrap's value");
                c.Conclude();
            }
            bill.suspended = true;
        }

        // ---- 19: x10 recycling bill ----------------------------------------------------------------------------------------
        {
            var c = ctx.Begin(RecycleBulk);
            c.Subject(crafter);
            var tier = TierDef(ScrapTier.Industrial);
            var bill = AddBill(table, "LS_Recycle_Bulk_Industrial");
            bill.repeatMode = BillRepeatModeDefOf.Forever;
            var pile = SpawnStack(map, industrial, 9, table.Position + new IntVec3(0, 0, -4));
            bool sawJob = false;
            var window = Wait.Ticks(1500, rest);
            window.AfterTick += () => sawJob |= crafter.CurJob?.RecipeDef == bill.recipe;
            yield return window;
            c.Step(!sawJob && Kit.CountOnMap(map, industrial) == 9, "with 9 units on hand the x10 bill never starts");

            pile.stackCount = 63;
            ctx.Setup("Developer raised the pile to 63 industrial scrap");
            var before = Counts(tier.pool.Select(e => e.thing));
            var watch = new JobWatch { Recipe = bill.recipe, Pawn = crafter, Carried = industrial };
            var seq = new List<int> { Kit.CountOnMap(map, industrial) };
            var done = Wait.Until(() => Kit.CountOnMap(map, industrial) == 0, 30000, rest);
            done.AfterTick += () => { watch.Tick(); int now = Kit.CountOnMap(map, industrial); if (now != seq.Last()) seq.Add(now); };
            yield return done;
            float value = GainedValue(before, out string desc);
            int batch = tier.batchSize * 10;
            c.Observe("scrapLeftSequence", string.Join(">", seq)).Observe("maxCarried", watch.MaxCarried).Observe("products", desc)
                .Expect("firstJobWork", batch * tier.workPerUnit).Observe("firstJobWork", watch.MaxWork)
                .Expect("value", 63 * tier.valuePerUnit).Observe("value", value);
            if (done.TimedOut)
                c.WorkflowInconclusive("the crafter did not recycle all 63 units in 30000 ticks", (crafter.Spawned && !crafter.Dead, "the crafter is alive"),
                    (((Building_WorkTable)table).CurrentlyUsableForBills(), "the machining table is usable"));
            else
            {
                c.Step(seq.SequenceEqual(new[] { 63, 13, 0 }), $"the bill uses the pile {batch} then 13 at a time ({string.Join(">", seq)})");
                c.Step(watch.MaxCarried > 0 && watch.MaxCarried <= batch, $"the crafter never carries more than ten batches ({watch.MaxCarried})");
                c.Step(watch.MaxWork <= batch * tier.workPerUnit && watch.MaxWork >= 0.9f * batch * tier.workPerUnit,
                    $"the first job's work is {batch} units' ({watch.MaxWork:0})");
                c.Step(value >= 0.6f * 63 * tier.valuePerUnit && value <= 1.4f * 63 * tier.valuePerUnit, $"63 units return about {63 * tier.valuePerUnit} silver of materials ({value:0})");
                c.Conclude();
            }
            bill.suspended = true;
        }

        // ---- 05: study bench ---------------------------------------------------------------------------------------------
        {
            var c = ctx.Begin(StudyBench);
            c.Observe("benchClass", bench.GetType().Name);
            c.Step(bench is Building_StudyBench, "the research bench is a study bench");
            c.Step(bench.def.inspectorTabs != null && bench.def.inspectorTabs.Contains(typeof(ITab_StudyBills)), "it has the Bills tab");
            c.Conclude();
        }

        var medieval = Scrap(ScrapTier.Medieval);
        var project = Project("ComplexClothing");
        var studyBill = AddBill(bench, "LS_Study_Medieval");
        studyBill.repeatMode = BillRepeatModeDefOf.TargetCount;
        studyBill.targetCount = 3;
        SpawnStack(map, medieval, 8, bench.Position + new IntVec3(0, 0, -4));
        {
            var c = ctx.Begin(NoProject);
            c.Subject(researcher);
            if (Find.ResearchManager.GetProject() != null) Find.ResearchManager.StopProject(Find.ResearchManager.GetProject());
            bool sawJob = false;
            var window = Wait.Ticks(1500, rest);
            window.AfterTick += () => sawJob |= researcher.CurJob?.RecipeDef == studyBill.recipe;
            yield return window;
            c.Step(Find.ResearchManager.GetProject() == null, "there is no current research project");
            c.Observe("unitsLeft", Kit.CountOnMap(map, medieval)).Observe("progress", Find.ResearchManager.GetProgress(project));
            c.Step(!sawJob && Kit.CountOnMap(map, medieval) == 8, "the researcher never starts the study bill and the 8 units stay");
            c.Conclude();
            // A fresh start for the next case, whatever happened here.
            researcher.jobs.EndCurrentJob(JobCondition.InterruptForced);
            researcher.carryTracker?.DestroyCarriedThing();
            foreach (var t in map.listerThings.ThingsOfDef(medieval).ToList()) t.Destroy();
            int stray = Kit.CountOnMap(map, medieval);
            if (stray > 0) ctx.Setup($"{stray} medieval scrap still held after the reset");
            SpawnStack(map, medieval, 8, bench.Position + new IntVec3(0, 0, -4));
            float reset = Find.ResearchManager.GetProgress(project);
            ctx.Setup($"Developer interrupted the researcher and replaced the medieval scrap with a fresh 8 (Complex Clothing progress {reset:0.#})");
        }
        {
            var c = ctx.Begin(StudyBill);
            c.Subject(researcher);
            Find.ResearchManager.SetCurrentProject(project);
            c.Note("Complex Clothing becomes the current project; the bill keeps 3 medieval units in stock");
            var watch = new JobWatch { Recipe = studyBill.recipe, Pawn = researcher, Project = project };
            var done = Wait.Until(() => watch.Ended, 15000, rest);
            done.AfterTick += watch.Tick;
            yield return done;
            float points = watch.P1 - watch.P0;
            c.Expect("points", 37.5f).Observe("points", points).Expect("workLeft", 1125).Observe("workLeft", watch.MaxWork)
                .Observe("xpGained", watch.Xp1 - watch.Xp0).Observe("left", Kit.CountOnMap(map, medieval));
            if (done.TimedOut)
                c.WorkflowInconclusive("the researcher did not finish a study job in 15000 ticks", (researcher.Spawned && !researcher.Dead, "the researcher is alive"),
                    (studyBill.ShouldDoNow(), "the study bill wants doing"));
            else
            {
                c.Step(Kit.Near(points, 37.5f, 0.5f), $"one iteration adds 37.5 points ({points:0.#})");
                c.Step(watch.MaxWork <= 1125f && watch.MaxWork >= 1050f, $"its work is 1125 ({watch.MaxWork:0})");
                c.Step(watch.Xp1 > watch.Xp0, "the researcher gains Intellectual XP");
                c.Step(Kit.CountOnMap(map, medieval) == 3, "3 units are left in stock");
                c.Conclude();
            }
        }
        {
            var c = ctx.Begin(StudyFinish);
            c.Subject(researcher);
            studyBill.targetCount = 0;
            float remaining = project.Cost - Find.ResearchManager.GetProgress(project);
            Find.ResearchManager.AddProgress(project, remaining - 15f);
            researcher.jobs.EndCurrentJob(JobCondition.InterruptForced);
            ctx.Setup($"Developer added {remaining - 15f:0.#} points to Complex Clothing, leaving 15, and interrupted the researcher's current job");
            var watch = new JobWatch { Recipe = studyBill.recipe, Pawn = researcher, Project = project };
            var done = Wait.Until(() => watch.Ended, 15000, rest);
            done.AfterTick += watch.Tick;
            yield return done;
            foreach (var w in Find.WindowStack.Windows.OfType<Dialog_NodeTree>().ToList()) w.Close(false);
            float points = watch.P1 - watch.P0;
            c.Expect("points", 15).Observe("points", points).Expect("workLeft", 450).Observe("workLeft", watch.MaxWork).Observe("left", Kit.CountOnMap(map, medieval))
                .Observe("finished", project.IsFinished);
            if (done.TimedOut)
                c.WorkflowInconclusive("the researcher did not finish a study job in 15000 ticks", (researcher.Spawned && !researcher.Dead, "the researcher is alive"));
            else
            {
                c.Step(project.IsFinished, "Complex Clothing finishes through study");
                c.Step(watch.MaxWork <= 450f && watch.MaxWork >= 400f, $"the iteration's work follows its 15 points: 450 ({watch.MaxWork:0})");
                c.Step(Kit.CountOnMap(map, medieval) == 1, "only the 2 units it needed are used; 1 is left");
                c.Conclude();
            }
            studyBill.suspended = true;
        }

        // ---- 07: original items ------------------------------------------------------------------------------------------
        var rifleDef = Kit.Def("Gun_ChargeRifle");
        ModHarness.LootRows(Rules, WhoRule.Mark);
        Unfamiliar.ResetCache();
        {
            var c = ctx.Begin(OriginalRecycle);
            c.Subject(crafter);
            var spacer = TierDef(ScrapTier.Spacer);
            var bill = AddBill(fab, "LS_RecycleOriginal");
            bill.repeatMode = BillRepeatModeDefOf.Forever;
            var rifle = GenSpawn.Spawn(Make(rifleDef, QualityCategory.Normal), fab.Position + new IntVec3(0, 0, -4), map);
            int units = ExpectedScrap(rifle, null);
            c.Step(Unfamiliar.IsUnfamiliar(rifle), "the charge rifle is unfamiliar");
            c.Step(!bill.IsFixedOrAllowedIngredient(rifle), "the bill does not allow it until it is ticked");
            bool sawJob = false;
            var window = Wait.Ticks(1500, rest);
            window.AfterTick += () => sawJob |= crafter.CurJob?.RecipeDef == bill.recipe;
            yield return window;
            c.Step(!sawJob && !rifle.Destroyed, "the crafter never takes the unticked rifle");

            bill.ingredientFilter.SetAllow(rifleDef, true);
            c.Note("The player ticks the charge rifle in the bill's filter");
            c.Step(bill.IsFixedOrAllowedIngredient(rifle), "the bill now allows it");
            var lowerBill = AddBill(table, "LS_RecycleOriginal");
            lowerBill.ingredientFilter.SetAllow(rifleDef, true);
            c.Step(!lowerBill.IsFixedOrAllowedIngredient(rifle), "the machining table's bill refuses the spacer-tier rifle even ticked");
            lowerBill.suspended = true;

            var before = Counts(spacer.pool.Select(e => e.thing));
            var done = Wait.Until(() => rifle.Destroyed, 12000, rest);
            yield return done;
            float value = GainedValue(before, out string desc);
            int products = before.Count(kv => Kit.CountOnMap(map, kv.Key) != kv.Value);
            c.Expect("value", units * spacer.valuePerUnit).Observe("value", value).Observe("products", desc).Observe("units", units);
            if (done.TimedOut)
                c.WorkflowInconclusive("the crafter did not recycle the rifle in 12000 ticks", (crafter.Spawned && !crafter.Dead, "the crafter is alive"),
                    (((Building_WorkTable)fab).CurrentlyUsableForBills(), "the fabrication bench is usable"));
            else
            {
                // One pick per unit, each amount rounded up or down at random (spec: "can come out lumpy"): a pick of a
                // 200-silver advanced component at 33 a unit is one component or none, so a 4-unit sample can land far from
                // its average. Bound it by what the picks can give: each at most its value rounded up to one whole item.
                float maxPerPick = spacer.pool.Where(e => e.thing != null).Max(e => (float)Math.Ceiling(spacer.valuePerUnit / e.thing.BaseMarketValue) * e.thing.BaseMarketValue);
                c.Observe("maxValue", units * maxPerPick);
                c.Step(value > 0f && value <= units * maxPerPick && products <= units,
                    $"the rifle recycles as {units} spacer scrap units: {value:0} silver of spacer materials from {products} materials");
                c.Conclude();
            }
            bill.suspended = true;
        }
        {
            var c = ctx.Begin(OriginalStudy);
            var stone = Project("Stonecutting");
            Find.ResearchManager.SetCurrentProject(stone);
            var bill = AddBill(bench, "LS_StudyOriginal");
            bill.suspended = true;
            bill.ingredientFilter.SetAllow(rifleDef, true);
            var rifle = GenSpawn.Spawn(Make(rifleDef, QualityCategory.Normal), bench.Position + new IntVec3(2, 0, -4), map);
            c.Step(!bill.IsFixedOrAllowedIngredient(rifle), "the spacer rifle is above the colony's era and refused");
            // Only scrap of the colony's era and below is studied (ticket 25): the colony is made spacer for the rest of the case.
            var savedEra = EraReader.ColonyEraOverride;
            EraReader.ColonyEraOverride = () => Era.Spacer;
            ctx.Setup("Developer set the colony's era to Spacer");
            yield return Wait.Ticks(1); // the bill's verdict is cached for the tick
            c.Step(bill.IsFixedOrAllowedIngredient(rifle), "in a spacer colony it is accepted");
            ScrapStudy.TryGetWorth(rifle, out StudyWorth worth);
            float before = Find.ResearchManager.GetProgress(stone);
            float expected = Math.Min(worth.UnitsPerThing * worth.PointsPerUnit, stone.Cost - before);
            ((RecipeWorker_Study)bill.recipe.Worker).Notify_IterationCompleted(researcher, new List<Thing> { rifle });
            float paid = Find.ResearchManager.GetProgress(stone) - before;
            foreach (var w in Find.WindowStack.Windows.OfType<Dialog_NodeTree>().ToList()) w.Close(false);
            c.Observe("unitsPerThing", worth.UnitsPerThing).Observe("tier", worth.Tier).Expect("paid", expected).Observe("paid", paid);
            c.Step(worth.Tier == ScrapTier.Spacer && Kit.Near(paid, expected, 0.5f), $"the rifle pays as {worth.UnitsPerThing} spacer units: {paid:0.#} points");
            c.Step(rifle.Destroyed, "the rifle is used up");
            EraReader.ColonyEraOverride = savedEra;
            c.Conclude();
        }

        // ---- 05: techprint project (last: it finishes the project's prerequisites) ---------------------------------------
        {
            var tp = DefDatabase<ResearchProjectDef>.AllDefsListForReading.FirstOrDefault(p => p.TechprintCount > 0 && p.baseCost > 0f && !p.IsFinished
                && p.knowledgeCategory == null && p.RequiredAnalyzedThingCount == 0 && !p.requireGravEngineInspected);
            if (tp == null) ctx.Begin(Techprint).NotApplicable("no techprint project is loaded (techprints need Royalty)");
            else
            {
                var c = ctx.Begin(Techprint);
                if (tp.prerequisites != null) Kit.Research(ctx, tp.prerequisites.Select(p => p.defName).ToArray());
                Find.ResearchManager.SetCurrentProject(tp);
                // Only scrap of the colony's era and below is studied (ticket 25): the colony is put at the project's era.
                var savedEra = EraReader.ColonyEraOverride;
                EraReader.ColonyEraOverride = () => (Era)tp.techLevel;
                var scrapDef = Scrap(ScrapTiers.TierFor((Era)tp.techLevel, EraReader.ColonyEra()));
                ScrapStudy.TryGetWorth(scrapDef, out StudyWorth worth);
                var worker = (RecipeWorker_Study)DefDatabase<RecipeDef>.GetNamed(StudyRecipesName(scrapDef)).Worker;
                Settings.values.studyTechprintProjects = false;
                var verdictOff = ScrapStudy.Verdict(worth, tp);
                float before = Find.ResearchManager.GetProgress(tp);
                var stack = SpawnStack(map, scrapDef, 3, bench.Position + new IntVec3(-2, 0, -4));
                worker.Notify_IterationCompleted(researcher, new List<Thing> { stack });
                float off = Find.ResearchManager.GetProgress(tp) - before;
                Settings.values.studyTechprintProjects = true;
                var verdictOn = ScrapStudy.Verdict(worth, tp);
                Find.ResearchManager.AddProgress(tp, tp.Cost - Find.ResearchManager.GetProgress(tp) - 5f);
                if (stack.Destroyed) stack = SpawnStack(map, scrapDef, 3, bench.Position + new IntVec3(-2, 0, -4));
                worker.Notify_IterationCompleted(researcher, new List<Thing> { stack });
                float left = tp.Cost - Find.ResearchManager.GetProgress(tp);
                c.Observe("project", tp.defName).Observe("verdictOff", verdictOff).Observe("paidOff", off).Observe("verdictOn", verdictOn)
                    .Observe("leftAfterOn", left).Observe("finished", tp.IsFinished);
                c.Step(verdictOff == StudyVerdict.NeedsTechprints && off == 0f, $"with the setting off {tp.defName} gets nothing");
                c.Step(verdictOn == StudyVerdict.Eligible && !tp.IsFinished && Kit.Near(left, 1f, 0.01f), "with it on, study stops one point short of finishing");
                EraReader.ColonyEraOverride = savedEra;
                c.Conclude();
            }
        }

        {
            var c = ctx.Begin(NoErrors);
            c.Observe("modErrors", CaseErrors.ModMark - modErrorsAtStart);
            c.Step(CaseErrors.ModMark == modErrorsAtStart, "no error naming Loot Scavenging was logged during the batch");
            c.Conclude();
        }
    }

    private static string StudyRecipesName(ThingDef scrap) => "LS_Study_" + scrap.defName.Substring("LS_Scrap_".Length);
}
