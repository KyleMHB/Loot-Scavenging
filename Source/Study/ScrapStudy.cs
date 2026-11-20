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
/// extends for unfamiliar original items are <see cref="TryGetWorth"/> and <see cref="Candidates"/>.
/// </summary>
public static class ScrapStudy
{
    /// <summary>
    /// The single "what is this worth at the bench" lookup. Today only scrap has a worth. Ticket 07 adds unfamiliar
    /// original items here as their scrap equivalent (tier from the item's era, units from ScrapAmounts.Units).
    /// </summary>
    public static bool TryGetWorth(Thing thing, out StudyWorth worth)
    {
        worth = default;
        if (thing == null)
            return false;
        StudyPoints points = thing.def.GetModExtension<StudyPoints>();
        if (points == null || !TryGetScrapTier(thing.def, out ScrapTier tier))
            return false;
        worth = new StudyWorth(tier, 1, points.pointsPerUnit);
        return true;
    }

    /// <summary>Things on the map that may be studied. Ticket 07 adds the unfamiliar items the player allows.</summary>
    public static IEnumerable<Thing> Candidates(Map map)
    {
        foreach (ScrapTier tier in (ScrapTier[])Enum.GetValues(typeof(ScrapTier)))
        {
            List<Thing> things = map.listerThings.ThingsOfDef(ScrapDefs.For(tier));
            for (int i = 0; i < things.Count; i++)
                yield return things[i];
        }
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

    /// <summary>Research speed at a bench, as bench research uses it: the pawn's speed times the bench's factor.</summary>
    public static float ResearchSpeed(Pawn pawn, Thing bench) =>
        pawn.GetStatValue(StatDefOf.ResearchSpeed) * bench.GetStatValue(StatDefOf.ResearchSpeedFactor);

    /// <summary>How many things of the stack one study trip takes; 0 when nothing is worth taking.</summary>
    public static int ThingsToTake(Thing thing, StudyWorth worth, ResearchProjectDef project, float researchSpeed)
    {
        float paid = PaidPerUnit(worth) * worth.UnitsPerThing;
        return StudyRules.UnitsToTake(thing.stackCount, paid, Payable(project), StudyRules.MaxPointsPerTrip(researchSpeed));
    }

    /// <summary>Points the given number of things pays into the project now.</summary>
    public static float PointsFor(int things, StudyWorth worth, ResearchProjectDef project) =>
        StudyRules.Points(things * worth.UnitsPerThing, PaidPerUnit(worth), Payable(project));
}

/// <summary>
/// Hand-off for "study first, then recycle the remains at reduced value". Ticket 04 owns recycling yields; until it sets
/// <see cref="Handler"/>, studied scrap leaves no remains. Arguments: the researcher, the tier, the remaining units.
/// </summary>
public static class StudyRemains
{
    public static Action<Pawn, ScrapTier, int> Handler;

    public static void Produce(Pawn pawn, ScrapTier tier, int units)
    {
        if (units <= 0)
            return;
        if (Handler != null)
            Handler(pawn, tier, units);
        else if (Prefs.DevMode)
            Log.Message($"[Loot Scavenging] {units} {tier} scrap units remain after study; recycling them needs ticket 04"); // not translated
    }
}
