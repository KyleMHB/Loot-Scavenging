using System;
using LootScavenging.Loot;
using LootScavenging.Model;
using LudeonTK;
using RimWorld;
using Verse;

namespace LootScavenging;

// Dev-mode helpers for the study checks without opening the settings page. Not written to the settings file,
// except the storyteller factor, which is the game's own difficulty value (cycle it back to 1 after testing).
internal static class StudyDebugActions
{
    private const string Category = "Loot Scavenging"; // not translated

    [DebugAction(Category, "Study: spawn 20 scrap of each tier", actionType = DebugActionType.ToolMap, allowedGameStates = AllowedGameStates.PlayingOnMap)] // not translated
    private static void SpawnScrap()
    {
        Map map = Find.CurrentMap;
        IntVec3 cell = UI.MouseCell();
        if (map == null || !cell.InBounds(map))
            return;
        foreach (ScrapTier tier in (ScrapTier[])Enum.GetValues(typeof(ScrapTier)))
        {
            Thing scrap = ThingMaker.MakeThing(ScrapDefs.For(tier));
            scrap.stackCount = 20;
            GenPlace.TryPlaceThing(scrap, cell, map, ThingPlaceMode.Near);
        }
    }

    [DebugAction(Category, "Study: log eligibility per tier", allowedGameStates = AllowedGameStates.Playing)] // not translated
    private static void LogEligibility()
    {
        ResearchProjectDef project = ScrapStudy.CurrentProject;
        float progress = project != null ? Find.ResearchManager.GetProgress(project) : 0f;
        Log.Message($"[Loot Scavenging] colony era {EraReader.ColonyEra()}, project {project?.defName ?? "none"} ({project?.techLevel}), progress {progress}/{project?.Cost}, factor {ScrapStudy.ResearchFactor}"); // not translated
        foreach (ScrapTier tier in (ScrapTier[])Enum.GetValues(typeof(ScrapTier)))
        {
            if (ScrapStudy.TryGetWorth(ScrapDefs.For(tier), out StudyWorth worth))
                Log.Message($"[Loot Scavenging]   {tier}: {ScrapStudy.Verdict(worth, project)}, {ScrapStudy.PaidPerUnit(worth)} points per unit"); // not translated
        }
    }

    [DebugAction(Category, "Study: cycle study eras", allowedGameStates = AllowedGameStates.Playing)] // not translated
    private static void CycleStudyEras()
    {
        LootScavengingSettings s = LootScavengingMod.Settings;
        var values = (StudyEraLimit[])Enum.GetValues(typeof(StudyEraLimit));
        s.studyEras = values[((int)s.studyEras + 1) % values.Length];
        Log.Message($"[Loot Scavenging] studyEras = {s.studyEras}"); // not translated
    }

    [DebugAction(Category, "Study: toggle recycle or study", allowedGameStates = AllowedGameStates.Playing)] // not translated
    private static void ToggleRecycleOrStudy()
    {
        LootScavengingSettings s = LootScavengingMod.Settings;
        s.recycleOrStudy = s.recycleOrStudy == RecycleOrStudy.OneOrTheOther ? RecycleOrStudy.StudyThenRecycle : RecycleOrStudy.OneOrTheOther;
        Log.Message($"[Loot Scavenging] recycleOrStudy = {s.recycleOrStudy}"); // not translated
    }

    [DebugAction(Category, "Study: toggle techprint projects", allowedGameStates = AllowedGameStates.Playing)] // not translated
    private static void ToggleTechprints()
    {
        LootScavengingSettings s = LootScavengingMod.Settings;
        s.studyTechprintProjects = !s.studyTechprintProjects;
        Log.Message($"[Loot Scavenging] studyTechprintProjects = {s.studyTechprintProjects}"); // not translated
    }

    [DebugAction(Category, "Study: cycle storyteller research factor", allowedGameStates = AllowedGameStates.Playing)] // not translated
    private static void CycleResearchFactor()
    {
        Difficulty difficulty = Find.Storyteller.difficulty;
        difficulty.researchSpeedFactor = difficulty.researchSpeedFactor == 1f ? 0f : difficulty.researchSpeedFactor == 0f ? 2.5f : 1f;
        Log.Message($"[Loot Scavenging] researchSpeedFactor = {difficulty.researchSpeedFactor}"); // not translated
    }
}
