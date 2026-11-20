using System;
using System.Collections.Generic;
using LootScavenging.Loot;
using LootScavenging.Model;
using RimWorld;
using Verse;

namespace LootScavenging;

/// <summary>Fixed research points per unit, on each scrap ThingDef (Defs/ThingDefs/Scrap.xml).</summary>
public class StudyPoints : DefModExtension
{
    public float pointsPerUnit;
}

/// <summary>What one thing is worth at the research bench.</summary>
public readonly struct StudyWorth
{
    public StudyWorth(ScrapTier tier, int unitsPerThing, float pointsPerUnit)
    {
        Tier = tier;
        UnitsPerThing = unitsPerThing;
        PointsPerUnit = pointsPerUnit;
    }

    public ScrapTier Tier { get; }

    /// <summary>Scrap units one thing of the stack counts as: 1 for scrap.</summary>
    public int UnitsPerThing { get; }

    public float PointsPerUnit { get; }
}

/// <summary>
/// Study glue (spec, "Study"): reads the game's state into the plain rules in Model/Study.cs. The two lookups ticket 07
/// extends for unfamiliar original items are <see cref="TryGetWorth"/> (what a thing is worth at the bench) and
/// <see cref="IngredientDefs"/> (what a tier's study bill may take).
/// </summary>
public static class ScrapStudy
{
    /// <summary>
    /// The single "what is this worth at the bench" lookup. Today only scrap has a worth; studied scrap has none, so it is
    /// never studied twice. Ticket 07 adds unfamiliar original items here as their scrap equivalent (tier from the item's
    /// era, units per thing from ScrapAmounts.Units).
    /// </summary>
    public static bool TryGetWorth(Thing thing, out StudyWorth worth)
    {
        worth = default;
        return thing != null && TryGetWorth(thing.def, out worth);
    }

    /// <summary>The worth of one unit of a scrap def.</summary>
    public static bool TryGetWorth(ThingDef def, out StudyWorth worth)
    {
        worth = default;
        StudyPoints points = def?.GetModExtension<StudyPoints>();
        if (points == null || !TryGetScrapTier(def, out ScrapTier tier))
            return false;
        worth = new StudyWorth(tier, 1, points.pointsPerUnit);
        return true;
    }

    /// <summary>
    /// The defs a tier's study bill allows in its ingredient filter, given that tier's scrap. Runs in the implied-def pass.
    /// Ticket 07 adds the unfamiliar original items of that tier here; the bill's filter then lets the player choose them.
    /// </summary>
    public static IEnumerable<ThingDef> IngredientDefs(ThingDef scrap)
    {
        yield return scrap;
    }

    public static bool TryGetScrapTier(ThingDef def, out ScrapTier tier)
    {
        foreach (ScrapTier t in (ScrapTier[])Enum.GetValues(typeof(ScrapTier)))
        {
            if (ScrapDefs.For(t) == def)
            {
                tier = t;
                return true;
            }
        }
        tier = default;
        return false;
    }

    public static ResearchProjectDef CurrentProject => Find.ResearchManager.GetProject();

    public static ProjectFacts FactsOf(ResearchProjectDef project)
    {
        if (project == null)
            return ProjectFacts.None;
        // Projects with a study source of their own never receive points from scrap.
        bool special = project.knowledgeCategory != null
            || (project.RequiredAnalyzedThingCount > 0 && !project.AnalyzedThingsRequirementsMet)
            || (project.requireGravEngineInspected && !project.InspectionRequirementsMet);
        float progress = project.baseCost > 0f ? Find.ResearchManager.GetProgress(project) : 0f;
        return new ProjectFacts(true, (Era)project.techLevel, project.baseCost, progress, !project.TechprintRequirementMet,
            special, project.PrerequisitesCompleted);
    }

    /// <summary>The storyteller's research factor. Only ever multiplied (Model/Study.cs), never divided by.</summary>
    public static float ResearchFactor => Find.Storyteller?.difficulty?.researchSpeedFactor ?? 1f;

    public static StudyVerdict Verdict(StudyWorth worth, ResearchProjectDef project)
    {
        LootScavengingSettings s = LootScavengingMod.Settings;
        return StudyRules.Check(worth.Tier, EraReader.ColonyEra(), FactsOf(project), s.studyEras, s.studyTechprintProjects,
            worth.PointsPerUnit, ResearchFactor);
    }

    public static float PaidPerUnit(StudyWorth worth) => StudyRules.PaidPerUnit(worth.PointsPerUnit, ResearchFactor);

    public static float Payable(ResearchProjectDef project) =>
        StudyRules.Payable(FactsOf(project), LootScavengingMod.Settings.studyTechprintProjects);

    /// <summary>Points the given number of things pays into the project now.</summary>
    public static float PointsFor(int things, StudyWorth worth, ResearchProjectDef project) =>
        StudyRules.Points(things * worth.UnitsPerThing, PaidPerUnit(worth), Payable(project));
}
