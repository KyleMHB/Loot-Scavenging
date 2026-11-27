using System.Collections.Generic;
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

        ResearchProjectDef project = ScrapStudy.CurrentProject;
        string points = "LootScavenging_StudyPointsPerUnit".Translate(ScrapStudy.PaidPerUnit(worth, project).ToString("0.#")).Resolve();
        StudyVerdict verdict = ScrapStudy.Verdict(worth, project);
        if (verdict == StudyVerdict.Eligible && !HasBenchFor(project, parent.MapHeld))
            return points + "\n" + "LootScavenging_StudyWhyNoBench".Translate(project.LabelCap).Resolve();
        return points + "\n" + Reason(verdict, worth, project);
    }

    /// <summary>Whether the map has a research bench (one that takes bills) that can research the project.</summary>
    private static bool HasBenchFor(ResearchProjectDef project, Map map)
    {
        if (map == null)
            return true;
        List<Building> buildings = map.listerBuildings.allBuildingsColonist;
        for (int i = 0; i < buildings.Count; i++)
        {
            if (buildings[i] is Building_StudyBench bench && project.CanBeResearchedAt(bench, ignoreResearchBenchPowerStatus: true))
                return true;
        }
        return false;
    }

    /// <summary>The sentence saying whether scrap can be studied for a verdict, and why not.</summary>
    internal static string Reason(StudyVerdict verdict, StudyWorth worth, ResearchProjectDef project)
    {
        string projectLabel = project?.LabelCap.Resolve() ?? "";
        string scrapEra = ((TechLevel)StudyRules.EraOf(worth.Tier)).ToStringHuman();
        return verdict switch
        {
            StudyVerdict.Eligible => "LootScavenging_StudyNow".Translate(projectLabel).Resolve(),
            StudyVerdict.NoStudyPoints => "LootScavenging_StudyWhyNoPoints".Translate().Resolve(),
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
