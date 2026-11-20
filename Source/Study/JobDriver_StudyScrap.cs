using System.Collections.Generic;
using LootScavenging.Model;
using RimWorld;
using Verse;
using Verse.AI;

namespace LootScavenging;

/// <summary>
/// Carry scrap (B) to a research bench (A), study it, use it up and pay the points into the current project through
/// <see cref="ResearchManager.AddProgress"/>. Modelled on vanilla research and techprint application. Study time comes
/// from the points paid and the research speed; the researcher learns Intellectual like bench research.
/// </summary>
public class JobDriver_StudyScrap : JobDriver
{
    private const TargetIndex BenchInd = TargetIndex.A;
    private const TargetIndex ScrapInd = TargetIndex.B;

    private int studyTicks = 1;
    private ResearchProjectDef project;

    private Thing Bench => job.GetTarget(BenchInd).Thing;

    private Thing Carried => pawn.carryTracker?.CarriedThing;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref studyTicks, "studyTicks", 1);
        Scribe_Defs.Look(ref project, "project");
    }

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        if (!pawn.Reserve(Bench, job, 1, -1, null, errorOnFailed))
            return false;
        if (Bench.def.hasInteractionCell && !pawn.ReserveSittableOrSpot(Bench.InteractionCell, job, errorOnFailed))
            return false;
        return pawn.Reserve(job.GetTarget(ScrapInd), job, 1, job.count, null, errorOnFailed);
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedNullOrForbidden(BenchInd);
        this.FailOnBurningImmobile(BenchInd);
        AddFailCondition(() => ScrapStudy.CurrentProject == null);

        yield return Toils_Goto.GotoThing(ScrapInd, PathEndMode.ClosestTouch)
            .FailOnDespawnedNullOrForbidden(ScrapInd)
            .FailOnSomeonePhysicallyInteracting(ScrapInd);
        yield return Toils_Haul.StartCarryThing(ScrapInd).FailOnDestroyedNullOrForbidden(ScrapInd);
        yield return Toils_Goto.GotoThing(BenchInd, PathEndMode.InteractionCell);

        // The scrap stays in the researcher's hands while studying, so nobody hauls it away mid-job.
        Toil study = ToilMaker.MakeToil("LS_Study"); // not translated
        study.initAction = () =>
        {
            project = ScrapStudy.CurrentProject;
            Thing carried = Carried;
            float points = 0f;
            if (project != null && carried != null && ScrapStudy.TryGetWorth(carried, out StudyWorth worth))
                points = ScrapStudy.PointsFor(carried.stackCount, worth, project);
            if (points <= 0f)
            {
                EndJobWith(JobCondition.Incompletable);
                return;
            }
            studyTicks = StudyRules.StudyTicks(points, ScrapStudy.ResearchSpeed(pawn, Bench));
            ticksLeftThisToil = studyTicks;
        };
        study.tickIntervalAction = delta =>
        {
            pawn.skills?.Learn(SkillDefOf.Intellectual, 0.1f * delta);
            pawn.GainComfortFromCellIfPossible(delta, chairsOnly: true);
        };
        // The game checks fail conditions once before initAction runs, when the project is not yet recorded.
        study.FailOn(() => Carried == null || (project != null && ScrapStudy.CurrentProject != project));
        study.FailOn(() => project != null && Bench is Building_ResearchBench bench
            && !project.CanBeResearchedAt(bench, ignoreResearchBenchPowerStatus: false));
        study.FailOnCannotTouch(BenchInd, PathEndMode.InteractionCell);
        study.WithEffect(EffecterDefOf.Research, BenchInd);
        study.WithProgressBar(BenchInd, () => 1f - (float)ticksLeftThisToil / studyTicks);
        study.defaultCompleteMode = ToilCompleteMode.Delay;
        study.defaultDuration = 1;
        study.activeSkill = () => SkillDefOf.Intellectual;
        yield return study;

        Toil finish = ToilMaker.MakeToil("LS_StudyFinish"); // not translated
        finish.initAction = Finish;
        finish.defaultCompleteMode = ToilCompleteMode.Instant;
        yield return finish;
    }

    // Pays the points and uses up the studied scrap. Rechecks eligibility, since the colony or project may have changed.
    private void Finish()
    {
        Thing carried = Carried;
        if (carried == null || project == null || !ScrapStudy.TryGetWorth(carried, out StudyWorth worth)
            || ScrapStudy.Verdict(worth, project) != StudyVerdict.Eligible)
            return;

        float paidPerThing = ScrapStudy.PaidPerUnit(worth) * worth.UnitsPerThing;
        float payable = ScrapStudy.Payable(project);
        int used = StudyRules.UnitsToTake(carried.stackCount, paidPerThing, payable, float.MaxValue);
        float points = ScrapStudy.PointsFor(used, worth, project);
        if (used <= 0 || points <= 0f)
            return;

        string label = carried.LabelNoCount;
        if (used >= carried.stackCount)
            carried.Destroy();
        else
            carried.SplitOff(used).Destroy();

        string projectLabel = project.LabelCap;
        Find.ResearchManager.AddProgress(project, points, pawn);
        Messages.Message("LootScavenging_StudyDone".Translate(pawn.LabelShort, used.ToString(), label, points.ToString("0"),
            projectLabel), new LookTargets(Bench), MessageTypeDefOf.PositiveEvent, historical: false);
        if (Prefs.DevMode)
            Log.Message($"[Loot Scavenging] {pawn} studied {used} {label}: +{points} to {project.defName}"); // not translated

        StudyRemains.Produce(pawn, worth.Tier, StudyRules.RemainsAfterStudy(used * worth.UnitsPerThing,
            LootScavengingMod.Settings.recycleOrStudy));
    }
}
