using System;
using System.Collections.Generic;
using LootScavenging.Loot;
using LootScavenging.Model;
using RimWorld;
using Verse;

namespace LootScavenging;

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
    /// The single "what is this worth at the bench" lookup: scrap, or an unfamiliar original item as its scrap equivalent
    /// (its tier's points, its units per thing). Studied scrap has no worth, so it is never studied twice.
    /// </summary>
    public static bool TryGetWorth(Thing thing, out StudyWorth worth)
    {
        worth = default;
        if (thing == null)
            return false;
        if (TryGetWorth(thing.def, out worth))
            return true;
        if (!ScrapEquivalent.TryGet(thing, out ScrapTier tier, out int units)
            || !TryGetWorth(ScrapDefs.For(tier), out StudyWorth perUnit))
            return false;
        worth = new StudyWorth(tier, units, perUnit.PointsPerUnit);
        return true;
    }

    /// <summary>The worth of one unit of a scrap def: its tier's points from the settings, times the multiplier.</summary>
    public static bool TryGetWorth(ThingDef def, out StudyWorth worth)
    {
        worth = default;
        if (def == null || !TryGetScrapTier(def, out ScrapTier tier))
            return false;
        worth = new StudyWorth(tier, 1, PointsPerUnit(tier));
        return true;
    }

    /// <summary>A tier's points per unit from the settings, before the storyteller's research factor.</summary>
    public static float PointsPerUnit(ScrapTier tier) => LootScavengingMod.Settings?.values.StudyPointsPerUnit(tier) ?? 0f;

    /// <summary>
    /// The defs a tier's study bill allows in its ingredient filter, given that tier's scrap. Runs in the implied-def pass.
    /// Original items are not listed here: their own recipe (OriginalItemRecipes) takes them one at a time.
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
        // Spec: "Projects with a special study source never receive anything", whether or not that source's requirement is
        // met yet: Anomaly knowledge, analysed items and the grav engine inspection.
        bool special = project.knowledgeCategory != null
            || project.RequiredAnalyzedThingCount > 0
            || project.requireGravEngineInspected;
        float progress = project.baseCost > 0f ? Find.ResearchManager.GetProgress(project) : 0f;
        return new ProjectFacts(true, (Era)project.techLevel, project.baseCost, progress, !project.TechprintRequirementMet,
            special, project.PrerequisitesCompleted);
    }

    /// <summary>The storyteller's research factor. Only ever multiplied (Model/Study.cs), never divided by.</summary>
    public static float ResearchFactor => Find.Storyteller?.difficulty?.researchSpeedFactor ?? 1f;

    public static StudyVerdict Verdict(StudyWorth worth, ResearchProjectDef project)
    {
        LootScavengingSettings s = LootScavengingMod.Settings;
        return StudyRules.Check(worth.Tier, EraReader.ColonyEra(), FactsOf(project), s.values.studyEras, s.values.studyTechprintProjects,
            worth.PointsPerUnit, ResearchFactor);
    }

    public static float PaidPerUnit(StudyWorth worth) => StudyRules.PaidPerUnit(worth.PointsPerUnit, ResearchFactor);

    public static float Payable(ResearchProjectDef project) =>
        StudyRules.Payable(FactsOf(project), LootScavengingMod.Settings.values.studyTechprintProjects);

    /// <summary>What an iteration with these things studies now: things used, points and work (see StudyRules.Plan).</summary>
    public static StudyPlan Plan(StudyWorth worth, int things, int maxThings, ResearchProjectDef project) =>
        StudyRules.Plan(things, worth.UnitsPerThing, maxThings, PaidPerUnit(worth), project == null ? 0f : Payable(project));
}
