using LootScavenging.Model;
using RimWorld;
using Verse;

namespace LootScavenging;

public class CompProperties_StudyInfo : CompProperties
{
    public CompProperties_StudyInfo()
    {
        compClass = typeof(CompStudyInfo);
    }
}

/// <summary>Scrap's inspect text: the points per unit and whether it can be studied now, or why not (spec, "Study").</summary>
public class CompStudyInfo : ThingComp
{
    public override string CompInspectStringExtra()
    {
        if (Current.ProgramState != ProgramState.Playing || !ScrapStudy.TryGetWorth(parent, out StudyWorth worth))
            return null;

        string points = "LootScavenging_StudyPointsPerUnit".Translate(ScrapStudy.PaidPerUnit(worth).ToString("0.#")).Resolve();
        ResearchProjectDef project = ScrapStudy.CurrentProject;
        return points + "\n" + Reason(ScrapStudy.Verdict(worth, project), worth, project);
    }

    private static string Reason(StudyVerdict verdict, StudyWorth worth, ResearchProjectDef project)
    {
        string projectLabel = project?.LabelCap.Resolve() ?? "";
        string scrapEra = ((TechLevel)StudyRules.EraOf(worth.Tier)).ToStringHuman();
        return verdict switch
        {
            StudyVerdict.Eligible => "LootScavenging_StudyNow".Translate(projectLabel).Resolve(),
            StudyVerdict.StudyOff => "LootScavenging_StudyWhyOff".Translate().Resolve(),
            StudyVerdict.NoProject => "LootScavenging_StudyWhyNoProject".Translate().Resolve(),
            StudyVerdict.SpecialSource => "LootScavenging_StudyWhySpecialSource".Translate(projectLabel).Resolve(),
            StudyVerdict.ProjectFinished => "LootScavenging_StudyWhyFinished".Translate(projectLabel).Resolve(),
            StudyVerdict.PrerequisitesUnfinished => "LootScavenging_StudyWhyPrerequisites".Translate(projectLabel).Resolve(),
            StudyVerdict.NeedsTechprints => "LootScavenging_StudyWhyTechprints".Translate(projectLabel).Resolve(),
            StudyVerdict.ScrapAboveColonyEra => "LootScavenging_StudyWhyColonyEra".Translate(scrapEra).Resolve(),
            StudyVerdict.ProjectAboveScrapEra => "LootScavenging_StudyWhyProjectEra".Translate(projectLabel,
                project != null ? project.techLevel.ToStringHuman() : "", scrapEra).Resolve(),
            _ => "LootScavenging_StudyWhyNoGain".Translate().Resolve(),
        };
    }
}
