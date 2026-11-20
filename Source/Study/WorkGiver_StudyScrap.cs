using LootScavenging.Model;
using RimWorld;
using Verse;
using Verse.AI;

namespace LootScavenging;

/// <summary>
/// Study scrap at a research bench (Research work type). Scans benches like vanilla research, then finds the closest
/// scrap that pays into the current project. With no project, or nothing eligible, there is no job and scrap stays put.
/// </summary>
public class WorkGiver_StudyScrap : WorkGiver_Scanner
{
    public static JobDef StudyJob => DefDatabase<JobDef>.GetNamed("LS_StudyScrap");

    public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForGroup(ThingRequestGroup.ResearchBench);

    public override bool Prioritized => true;

    public override bool ShouldSkip(Pawn pawn, bool forced = false)
    {
        return ScrapStudy.CurrentProject == null || LootScavengingMod.Settings.studyEras == StudyEraLimit.None;
    }

    public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        return TryMakeJob(pawn, t, forced) != null;
    }

    public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        return TryMakeJob(pawn, t, forced);
    }

    public override float GetPriority(Pawn pawn, TargetInfo t)
    {
        return t.Thing.GetStatValue(StatDefOf.ResearchSpeedFactor);
    }

    private static Job TryMakeJob(Pawn pawn, Thing t, bool forced)
    {
        ResearchProjectDef project = ScrapStudy.CurrentProject;
        if (project == null || !(t is Building_ResearchBench bench))
            return null;
        if (!project.CanBeResearchedAt(bench, ignoreResearchBenchPowerStatus: false) || t.IsForbidden(pawn))
            return null;
        if (!pawn.CanReserve(t, 1, -1, null, forced)
            || (t.def.hasInteractionCell && !pawn.CanReserveSittableOrSpot(t.InteractionCell, forced)))
            return null;

        // The verdict depends only on the tier (it reads every research project for the colony's era), so it is worked out
        // once per tier for this search.
        var verdicts = new StudyVerdict?[System.Enum.GetValues(typeof(ScrapTier)).Length];
        Thing scrap = GenClosest.ClosestThing_Global_Reachable(pawn.Position, pawn.Map, ScrapStudy.Candidates(pawn.Map),
            PathEndMode.ClosestTouch, TraverseParms.For(pawn), validator: c => CanStudy(pawn, c, project, forced, verdicts));
        if (scrap == null || !ScrapStudy.TryGetWorth(scrap, out StudyWorth worth))
            return null;

        int count = ScrapStudy.ThingsToTake(scrap, worth, project, ScrapStudy.ResearchSpeed(pawn, bench));
        if (count <= 0)
            return null;
        if (!new HistoryEvent(HistoryEventDefOf.Researching, pawn.Named(HistoryEventArgsNames.Doer)).Notify_PawnAboutToDo_Job())
            return null;

        Job job = JobMaker.MakeJob(StudyJob, bench, scrap);
        job.count = count;
        return job;
    }

    private static bool CanStudy(Pawn pawn, Thing thing, ResearchProjectDef project, bool forced, StudyVerdict?[] verdicts)
    {
        if (!ScrapStudy.TryGetWorth(thing, out StudyWorth worth) || thing.IsForbidden(pawn) || !pawn.CanReserve(thing, 1, -1, null, forced))
            return false;
        int i = (int)worth.Tier;
        verdicts[i] ??= ScrapStudy.Verdict(worth, project);
        return verdicts[i] == StudyVerdict.Eligible;
    }
}
