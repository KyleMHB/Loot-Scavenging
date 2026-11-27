using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using LootScavenging.Model;
using RimWorld;
using RimWorldDevTests;
using Verse;
using static LootScavenging.DevTests.Fixtures;

namespace LootScavenging.DevTests;

// The Progression pack's research mods (spec, "The Progression pack" and "Unverified"), with the `progression` mod set:
// VFE Tribals, Progression: Core, Node Research, Progression: Production, Research Papers and Semi Random Research. Run it
// with `--mods progression`; on any other set every case is reported as environment unavailable.
//
// Facts these cases rest on, read from the installed mods' code on 2026-11-21:
// - The pack keeps the colony's era in Faction.OfPlayer.def.techLevel. Node Research's emergence projects (BRM_Emergence_<era>,
//   tech level one below their target) write it when finished, VFE Tribals' advancement writes it and saves it, and Visual
//   Progression reads it. Node Research blocks starting a project above it.
// - Research Papers makes a paper in a FinishProject prefix, at the researcher when one is passed. Destroying a paper sets the
//   project's progress back to lostResearchRetainPercent of its cost.
// - Semi Random Research rejects ResearchManager.SetCurrentProject unless its tracker applies the pick, and adds a project to
//   the offers from an AddProgress prefix.
// The pack mods are reached by reflection, so this assembly loads without them.
public sealed class ProgressionBatch : Batch
{
    private const string P = "lootscavenging.progression.";
    private const string Era_ = P + "colony-era-follows-pack", Papers = P + "study-makes-paper", PaperLost = P + "lost-paper-partial-progress",
        Offer = P + "study-pays-picked-offer", Benches = P + "recycling-benches", NoErrors = P + "no-mod-errors";

    public override string Id => "lootscavenging-progression";
    public override int SegmentCount => 1;
    public override TimeSpan WallLimit => TimeSpan.FromMinutes(10);

    public override IReadOnlyList<PlannedCase> Plan => new[]
    {
        new PlannedCase(Era_, Levels.Mechanical, "study can pay for the medieval emergence once its foundations are done; after it the colony era is medieval and medieval scrap can be studied for a medieval project"),
        new PlannedCase(Papers, Levels.Mechanical, "a project finished by study leaves a research paper at the researcher, like bench research"),
        new PlannedCase(PaperLost, Levels.Mechanical, "losing that paper keeps the retained share of progress, and study can finish the project again with a new paper"),
        new PlannedCase(Offer, Levels.Mechanical, "with Semi Random Research, study pays only the picked offer, a direct pick is rejected and study leaves the offers alone"),
        new PlannedCase(Benches, Levels.Mechanical, "every tier's recycling bench exists in the pack set, can be built and carries its recycle bills, and every pool material is loaded"),
        new PlannedCase(NoErrors, Levels.Mechanical, "no error naming the mod logged during the batch"),
    };

    private static readonly string[] Needed = { "oskarpotocki.vfe.tribals", "ferny.noderesearch", "chaoticenrico.researchpapers" };

    public override IEnumerable<Wait> Segment(int index, BatchContext ctx)
    {
        var missing = Needed.Where(id => !ModsConfig.IsActive(id)).ToList();
        if (missing.Count > 0)
        {
            foreach (var planned in Plan)
                ctx.Begin(planned.Id).EnvironmentUnavailable("needs the progression mod set (--mods progression); missing " + string.Join(", ", missing));
            yield break;
        }

        long modErrorsAtStart = CaseErrors.ModMark;
        ModHarness.SaveSettings();
        ModHarness.SavePlayerTechLevel();
        Kit.Area(ctx, 30, 12);
        yield return Wait.Ticks(1);
        var map = ctx.Map;
        var researcher = map.mapPawns.FreeColonistsSpawned.First();
        Find.Storyteller.difficulty.researchSpeedFactor = 1f;
        ctx.Setup("Developer set the storyteller research factor to 1");

        // A tribal colony in the pack's terms: no research, and the player faction's tech level (with VFE Tribals' saved copy)
        // at Neolithic. Research Papers' papers for Crashlanded's research are cleared first, so losing one later cannot
        // hand that research back.
        Find.ResearchManager.ResetAllProgress();
        var papers = PapersComponent();
        AccessTools.Method(papers.GetType(), "ResetPapers").Invoke(papers, null);
        SetPlayerTechLevel(TechLevel.Neolithic);
        Unfamiliar.ResetCache();
        ctx.Setup("Developer reset all research, cleared Research Papers' papers and set the player faction's tech level to Neolithic");
        yield return Wait.Ticks(2);
        CloseInterruptions();

        // A medieval project whose prerequisites are all below medieval, so finishing them leaves research-based era Neolithic.
        var medieval = DefDatabase<ResearchProjectDef>.AllDefsListForReading.FirstOrDefault(p => p.techLevel == TechLevel.Medieval && Payable(p)
            && Ancestors(p).All(a => a.techLevel < TechLevel.Medieval));

        // ---- the colony era follows the pack's era -------------------------------------------------------------------------
        {
            var c = ctx.Begin(Era_);
            var emergence = DefDatabase<ResearchProjectDef>.GetNamedSilentFail("BRM_Emergence_Medieval");
            if (emergence == null || medieval == null)
            {
                c.SetupFailed($"emergence project {(emergence == null ? "missing" : "found")}, medieval project {(medieval == null ? "missing" : "found")}");
            }
            else
            {
                Kit.Research(ctx, Ancestors(medieval).Select(a => a.defName).ToArray());
                Unfamiliar.ResetCache();
                c.Observe("medievalProject", medieval.defName).Observe("eraBefore", EraReader.ColonyEra());
                c.Step(EraReader.ColonyEra() == Era.Neolithic, "before the advance the colony era is neolithic");
                // Study may pay for an advance (Kyle, 2026-11-21): the emergence project is an ordinary project to the study
                // gate, refused only until its own prerequisites (the era's foundations) are finished.
                ScrapStudy.TryGetWorth(Scrap(ScrapTier.Neolithic), out StudyWorth neolithicWorth);
                var unfinishedVerdict = ScrapStudy.Verdict(neolithicWorth, emergence);
                Kit.Research(ctx, Ancestors(emergence).Select(a => a.defName).ToArray());
                var readyVerdict = ScrapStudy.Verdict(neolithicWorth, emergence);
                c.Observe("emergenceTechLevel", emergence.techLevel).Observe("emergencePrerequisites", string.Join(", ", Ancestors(emergence).Select(a => a.defName)))
                    .Observe("neolithicScrapBeforeFoundations", unfinishedVerdict).Observe("neolithicScrapAfterFoundations", readyVerdict);
                c.Step(readyVerdict == StudyVerdict.Eligible, "with its foundations finished, neolithic scrap can be studied for the medieval advance");
                Unfamiliar.ResetCache();
                c.Step(EraReader.ColonyEra() == Era.Neolithic, "finishing the neolithic foundations leaves the colony neolithic");

                Find.ResearchManager.FinishProject(emergence, doCompletionDialog: false, doCompletionLetter: false);
                ctx.Setup("Developer finished BRM_Emergence_Medieval");
                yield return Wait.Ticks(2);
                CloseInterruptions();
                Unfamiliar.ResetCache();
                var faction = Faction.OfPlayer.def.techLevel;
                c.Observe("factionTechLevel", faction).Observe("colonyEra", EraReader.ColonyEra());
                if (faction != TechLevel.Medieval)
                    c.SetupFailed("Node Research did not raise the player faction to Medieval (" + faction + ")");
                else
                {
                    c.Step(EraReader.ColonyEra() == Era.Medieval, "the colony era is medieval, matching the pack's era");
                    ScrapStudy.TryGetWorth(Scrap(ScrapTier.Medieval), out StudyWorth worth);
                    var verdict = ScrapStudy.Verdict(worth, medieval);
                    c.Observe("medievalScrapVerdict", verdict);
                    c.Step(verdict == StudyVerdict.Eligible, $"medieval scrap can be studied for {medieval.defName} with \"colony's era and below\"");
                    c.Conclude();
                }
            }
        }

        // ---- Research Papers: a project finished by study leaves a paper --------------------------------------------------
        Thing paper = null;
        if (medieval == null || EraReader.ColonyEra() < Era.Medieval)
        {
            ctx.Begin(Papers).SetupFailed("the colony did not reach medieval in the era case");
            ctx.Begin(PaperLost).SetupFailed("the colony did not reach medieval in the era case");
        }
        else
        {
            {
                var c = ctx.Begin(Papers);
                c.Subject(researcher);
                PickProject(medieval);
                StudyToFinish(medieval, researcher, out int used);
                yield return Wait.Ticks(2);
                CloseInterruptions();
                paper = PaperFor(map, medieval);
                c.Observe("project", medieval.defName).Observe("unitsUsed", used).Observe("finished", medieval.IsFinished)
                    .Observe("paper", paper == null ? "none" : $"{paper.def.defName} at {paper.Position}, {paper.Position.DistanceTo(researcher.Position):0.0} cells from the researcher");
                c.Step(medieval.IsFinished, "two units of medieval scrap finished the project");
                c.Step(paper != null && paper.Spawned && paper.Position.DistanceTo(researcher.Position) <= 6f, "a research paper for it lies at the researcher");
                c.Step(TrackedProjects().Contains(medieval), "Research Papers tracks the project as held by a paper");
                c.Conclude();
            }
            {
                var c = ctx.Begin(PaperLost);
                if (paper == null) { c.SetupFailed("no paper was made in the previous case"); }
                else
                {
                    int retain = (int)Traverse.Create(AccessTools.TypeByName("ResearchPapers.ResearchPapersMod")).Field("settings").Field("lostResearchRetainPercent").GetValue();
                    float expected = Math.Min(medieval.baseCost * retain / 100f, medieval.baseCost - 0.01f);
                    paper.Destroy();
                    yield return Wait.Ticks(2);
                    CloseInterruptions();
                    float left = Find.ResearchManager.GetProgress(medieval);
                    c.Expect("progress", expected).Observe("progress", left).Observe("retainPercent", retain).Observe("finished", medieval.IsFinished);
                    c.Step(!medieval.IsFinished && Math.Abs(left - expected) < 0.5f, $"the project is unfinished with {retain}% of its progress");
                    ScrapStudy.TryGetWorth(Scrap(ScrapTier.Medieval), out StudyWorth worth);
                    c.Step(ScrapStudy.Verdict(worth, medieval) == StudyVerdict.Eligible || !IsCurrent(medieval), "the partly lost project can be studied again");
                    PickProject(medieval);
                    StudyToFinish(medieval, researcher, out int used);
                    yield return Wait.Ticks(2);
                    CloseInterruptions();
                    var again = PaperFor(map, medieval);
                    c.Observe("unitsUsedAgain", used).Observe("newPaper", again == null ? "none" : again.Position.ToString());
                    c.Step(medieval.IsFinished && again != null && again != paper, "study finished it again and a new paper was made");
                    c.Conclude();
                }
            }
        }

        // ---- Semi Random Research: study pays only the picked offer -------------------------------------------------------
        {
            var c = ctx.Begin(Offer);
            var tracker = SrrTracker();
            if (tracker == null) { c.EnvironmentUnavailable("Semi Random Research is not active"); }
            else
            {
                AccessTools.Method(tracker.GetType(), "RefreshOffers").Invoke(tracker, null);
                var offers = Offers(tracker);
                c.Observe("offers", string.Join(", ", offers.Select(o => $"{o.defName} ({o.techLevel})")));
                var picked = offers.FirstOrDefault(o => Payable(o) && o.techLevel <= TechLevel.Medieval && o.PrerequisitesCompleted);
                var other = offers.FirstOrDefault(o => o != picked && Payable(o) && o.PrerequisitesCompleted)
                    ?? DefDatabase<ResearchProjectDef>.AllDefsListForReading.FirstOrDefault(o => o != picked && Payable(o) && o.CanStartNow);
                if (picked == null || other == null)
                    c.SetupFailed("no studyable offer and second project (picked " + (picked?.defName ?? "none") + ", other " + (other?.defName ?? "none") + ")");
                else
                {
                    PickProject(picked);
                    c.Observe("picked", picked.defName).Observe("other", other.defName);
                    c.Step(IsCurrent(picked), "picking an offer through Semi Random Research makes it the current project");
                    Find.ResearchManager.SetCurrentProject(other);
                    c.Step(IsCurrent(picked), "a direct SetCurrentProject to another project is rejected");

                    var offersBefore = Offers(tracker).Select(o => o.defName).OrderBy(n => n).ToList();
                    float pickedBefore = Find.ResearchManager.GetProgress(picked), otherBefore = Find.ResearchManager.GetProgress(other);
                    var tier = ScrapTiers.TierFor((Era)picked.techLevel, EraReader.ColonyEra());
                    var stack = SpawnStack(map, Scrap(tier), 1, researcher.Position);
                    Worker(Scrap(tier)).Notify_IterationCompleted(researcher, new List<Thing> { stack });
                    yield return Wait.Ticks(2);
                    float pickedAfter = Find.ResearchManager.GetProgress(picked), otherAfter = Find.ResearchManager.GetProgress(other);
                    var offersAfter = Offers(tracker).Select(o => o.defName).OrderBy(n => n).ToList();
                    c.Observe("pickedProgress", $"{pickedBefore:0.#} > {pickedAfter:0.#}").Observe("otherProgress", $"{otherBefore:0.#} > {otherAfter:0.#}")
                        .Observe("offersAfter", string.Join(", ", offersAfter));
                    c.Step(pickedAfter > pickedBefore && otherAfter == otherBefore, "one unit of scrap paid the picked project and nothing else");
                    c.Step(offersBefore.SequenceEqual(offersAfter), "study's progress left the offers unchanged");

                    StudyToFinish(picked, researcher, out _);
                    yield return Wait.Ticks(2);
                    CloseInterruptions();
                    c.Step(picked.IsFinished && !IsCurrent(picked), "study finished the picked project and it is no longer current");
                    c.Conclude();
                }
            }
        }

        // ---- benches per era -----------------------------------------------------------------------------------------------
        {
            var c = ctx.Begin(Benches);
            bool all = true;
            foreach (var tier in DefDatabase<ScrapTierDef>.AllDefsListForReading.OrderBy(d => d.tier))
            {
                string recipe = "LS_Recycle_" + tier.tier;
                foreach (var bench in tier.benches)
                {
                    bool buildable = bench.BuildableByPlayer;
                    bool hasBill = bench.AllRecipes.Any(r => r.defName == recipe);
                    var research = (bench.researchPrerequisites ?? new List<ResearchProjectDef>()).Select(r => $"{r.defName} ({r.techLevel})");
                    c.Observe($"{tier.tier}.{bench.defName}", $"buildable {buildable}, bill {hasBill}, research [{string.Join(", ", research)}]");
                    all &= buildable && hasBill;
                }
                all &= tier.benches.Count > 0;
                var pool = tier.pool.Select(e => e.thing?.defName ?? "missing").ToList();
                c.Observe($"{tier.tier}.pool", string.Join(", ", pool));
                all &= tier.pool.Count > 0 && tier.pool.All(e => e.thing != null);
            }
            c.Step(all, "each tier lists at least one bench, each bench can be built and has its tier's recycle bill, and every pool material is loaded");
            c.Conclude();
        }

        {
            var c = ctx.Begin(NoErrors);
            c.Observe("modErrors", CaseErrors.ModMark - modErrorsAtStart);
            c.Step(CaseErrors.ModMark == modErrorsAtStart, "no error naming Loot Scavenging was logged during the batch");
            c.Conclude();
        }
    }

    // A project study can pay into: research points, nothing special, no techprints.
    private static bool Payable(ResearchProjectDef p) => p.baseCost > 0f && !p.IsFinished && p.knowledgeCategory == null && p.TechprintCount == 0
        && p.RequiredAnalyzedThingCount == 0 && !p.requireGravEngineInspected && !p.defName.StartsWith("BRM_Emergence_");

    private static List<ResearchProjectDef> Ancestors(ResearchProjectDef p)
    {
        var found = new List<ResearchProjectDef>();
        void Walk(ResearchProjectDef d)
        {
            foreach (var pre in (d.prerequisites ?? new List<ResearchProjectDef>()).Concat(d.hiddenPrerequisites ?? new List<ResearchProjectDef>()))
                if (!found.Contains(pre)) { Walk(pre); found.Add(pre); }
        }
        Walk(p);
        return found;
    }

    private static RecipeWorker_Study Worker(ThingDef scrap) =>
        (RecipeWorker_Study)DefDatabase<RecipeDef>.GetNamed("LS_Study_" + scrap.defName.Substring("LS_Scrap_".Length)).Worker;

    // Brings a project within two units of medieval-or-lower scrap of finishing and studies them, through the study bill's worker.
    private static void StudyToFinish(ResearchProjectDef project, Pawn researcher, out int used)
    {
        var tier = ScrapTiers.TierFor((Era)project.techLevel, EraReader.ColonyEra());
        var scrap = Scrap(tier);
        ScrapStudy.TryGetWorth(scrap, out StudyWorth worth);
        float perUnit = ScrapStudy.PaidPerUnit(worth, project);
        float target = project.Cost - perUnit * 1.5f;
        float now = Find.ResearchManager.GetProgress(project);
        if (target > now) Find.ResearchManager.AddProgress(project, target - now, null);
        var stack = SpawnStack(researcher.Map, scrap, 5, researcher.Position);
        int before = stack.stackCount;
        Worker(scrap).Notify_IterationCompleted(researcher, new List<Thing> { stack });
        used = stack.Destroyed ? before : before - stack.stackCount;
    }

    private static bool IsCurrent(ResearchProjectDef p) => Find.ResearchManager.GetProject() == p;

    private static List<ResearchProjectDef> Offers(object tracker) =>
        ((IEnumerable)AccessTools.Method(tracker.GetType(), "PeekAvailableProjects").Invoke(tracker, null) ?? new List<ResearchProjectDef>()).Cast<ResearchProjectDef>().ToList();

    private static GameComponent PapersComponent() => Current.Game.components.First(c => c.GetType().FullName == "ResearchPapers.ResearchPapersGameComponent");

    private static HashSet<ResearchProjectDef> TrackedProjects() =>
        (HashSet<ResearchProjectDef>)AccessTools.Field(PapersComponent().GetType(), "projects").GetValue(PapersComponent());

    private static Thing PaperFor(Map map, ResearchProjectDef project) =>
        map.listerThings.AllThings.OfType<ThingWithComps>().FirstOrDefault(t => !t.Destroyed && t.AllComps.Any(comp => comp.GetType().Name == "CompResearchPaper"
            && ((IEnumerable)AccessTools.Field(comp.GetType(), "projects").GetValue(comp)).Cast<ResearchProjectDef>().Contains(project)));

    public static void SetPlayerTechLevel(TechLevel level)
    {
        Faction.OfPlayer.def.techLevel = level;
        var tribals = Current.Game.components.FirstOrDefault(c => c.GetType().FullName == "VFETribals.GameComponent_Tribals");
        if (tribals != null) AccessTools.Field(tribals.GetType(), "playerTechLevel").SetValue(tribals, level);
    }

    // Node Research's advance window, a completion tab or a pause from Semi Random Research would hold the run.
    private static void CloseInterruptions()
    {
        foreach (var w in Find.WindowStack.Windows.Where(w => w.GetType().Name == "Window_TechAdvance").ToList())
            Find.WindowStack.TryRemove(w, doCloseSound: false);
        Find.MainTabsRoot?.EscapeCurrentTab(playSound: false);
        if (Find.TickManager.Paused) Find.TickManager.TogglePaused();
    }
}
