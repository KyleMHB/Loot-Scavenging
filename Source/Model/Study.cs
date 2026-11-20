using System;

namespace LootScavenging.Model;

/// <summary>Which scrap can be studied (spec, "Study"). Default: the colony's current era and below.</summary>
public enum StudyEraLimit
{
    ColonyEraAndBelow,
    AnyEra,
    /// <summary>Study is switched off; scrap is only recycled.</summary>
    None,
}

/// <summary>Recycle or study (spec, "Study"). Default: each unit is used for one or the other.</summary>
public enum RecycleOrStudy
{
    OneOrTheOther,
    /// <summary>Study first, then the remains are recycled at reduced value (the recycling half is ticket 04's).</summary>
    StudyThenRecycle,
}

/// <summary>Why scrap can or cannot be studied right now. Every value except Eligible means the job does not start.</summary>
public enum StudyVerdict
{
    Eligible,
    StudyOff,
    NoProject,
    /// <summary>A project with a study source of its own (Anomaly knowledge, analysed items, grav engine) or no research cost.</summary>
    SpecialSource,
    ProjectFinished,
    PrerequisitesUnfinished,
    NeedsTechprints,
    ScrapAboveColonyEra,
    ProjectAboveScrapEra,
    /// <summary>The storyteller's research factor is zero (or invalid), so study would use scrap up for nothing.</summary>
    NoGain,
}

/// <summary>The facts about the current research project that study needs, read from the game by the glue.</summary>
public readonly struct ProjectFacts
{
    public ProjectFacts(bool exists, Era era, float baseCost, float progress, bool needsTechprints, bool specialSource,
        bool prerequisitesCompleted)
    {
        Exists = exists;
        Era = era;
        BaseCost = baseCost;
        Progress = progress;
        NeedsTechprints = needsTechprints;
        SpecialSource = specialSource;
        PrerequisitesCompleted = prerequisitesCompleted;
    }

    public static ProjectFacts None => new ProjectFacts(false, Era.Undefined, 0f, 0f, false, false, false);

    public bool Exists { get; }
    public Era Era { get; }
    public float BaseCost { get; }
    public float Progress { get; }

    /// <summary>The project needs techprints and not all of them are applied yet.</summary>
    public bool NeedsTechprints { get; }

    public bool SpecialSource { get; }
    public bool PrerequisitesCompleted { get; }

    public float Remaining => BaseCost - Progress > 0f ? BaseCost - Progress : 0f;
}

public static class StudyRules
{
    /// <summary>
    /// Study ticks per research point at research speed 1. Bench research gives about 0.00825 points a tick (about 121
    /// ticks a point), so study is about four times faster: scrap is worth carrying to the bench. Tuning knob.
    /// </summary>
    public const int TicksPerPoint = 30;

    /// <summary>One study job lasts at most this long (two in-game hours), so a 500-stack is not one day-long job.</summary>
    public const int MaxTicksPerJob = 5000;

    /// <summary>Research speed is floored here, so a pawn with no research speed still has a finite study time.</summary>
    public const float MinResearchSpeed = 0.05f;

    /// <summary>Share of the studied units that remain for recycling in <see cref="RecycleOrStudy.StudyThenRecycle"/>.</summary>
    public const float RemainsFraction = 0.5f;

    /// <summary>
    /// The era a tier is compared as: its nominal era. Spacer scrap compares as Spacer, so it does not pay into Ultra
    /// projects; neolithic scrap also covers Animal-era gear.
    /// </summary>
    public static Era EraOf(ScrapTier tier) => tier switch
    {
        ScrapTier.Neolithic => Era.Neolithic,
        ScrapTier.Medieval => Era.Medieval,
        ScrapTier.Industrial => Era.Industrial,
        _ => Era.Spacer,
    };

    private static Era LowestEraOf(ScrapTier tier) => tier == ScrapTier.Neolithic ? Era.Animal : EraOf(tier);

    public static StudyVerdict Check(ScrapTier tier, Era colonyEra, ProjectFacts project, StudyEraLimit eras,
        bool techprintProjects, float pointsPerUnit, float researchFactor)
    {
        if (eras == StudyEraLimit.None)
            return StudyVerdict.StudyOff;
        if (!project.Exists)
            return StudyVerdict.NoProject;
        if (project.SpecialSource || !(project.BaseCost > 0f))
            return StudyVerdict.SpecialSource;
        if (project.Remaining <= 0f)
            return StudyVerdict.ProjectFinished;
        if (!project.PrerequisitesCompleted)
            return StudyVerdict.PrerequisitesUnfinished;
        if (project.NeedsTechprints && !techprintProjects)
            return StudyVerdict.NeedsTechprints;
        if (eras == StudyEraLimit.ColonyEraAndBelow && LowestEraOf(tier) > colonyEra)
            return StudyVerdict.ScrapAboveColonyEra;
        if (project.Era > EraOf(tier))
            return StudyVerdict.ProjectAboveScrapEra;
        if (Payable(project, techprintProjects) <= 0f)
            return StudyVerdict.NeedsTechprints; // only a techprint project short of its last point gets here
        if (PaidPerUnit(pointsPerUnit, researchFactor) <= 0f)
            return StudyVerdict.NoGain;
        return StudyVerdict.Eligible;
    }

    /// <summary>
    /// Points the project can still take. A project whose techprints are missing stops one point short of finishing,
    /// because the game's completion would hand over the missing techprints for free.
    /// </summary>
    public static float Payable(ProjectFacts project, bool techprintProjects)
    {
        float remaining = project.Remaining;
        if (project.NeedsTechprints)
            remaining = techprintProjects && remaining > 1f ? remaining - 1f : 0f;
        return remaining;
    }

    /// <summary>
    /// Research points one unit pays: the tier's fixed points times the storyteller's research factor. The factor is only
    /// ever multiplied, so a zero factor pays zero; an invalid factor or result pays zero.
    /// </summary>
    public static float PaidPerUnit(float pointsPerUnit, float researchFactor)
    {
        float paid = pointsPerUnit * researchFactor;
        return IsUsable(paid) ? paid : 0f;
    }

    /// <summary>Points for a number of units, capped at what the project can still take. Never NaN or infinite.</summary>
    public static float Points(int units, float paidPerUnit, float payable)
    {
        if (units <= 0 || !IsUsable(paidPerUnit) || !IsUsable(payable))
            return 0f;
        return Math.Min(units * paidPerUnit, payable);
    }

    /// <summary>Units one job takes: no more than the stack, than the project needs, or than one trip can study.</summary>
    public static int UnitsToTake(int available, float paidPerUnit, float payable, float maxPointsPerTrip)
    {
        if (available <= 0 || !IsUsable(paidPerUnit) || !IsUsable(payable))
            return 0;
        // Compared as doubles before narrowing: a float-to-int cast of a huge value is not saturating on every runtime.
        double needed = Math.Ceiling((double)payable / paidPerUnit);
        double perTrip = IsUsable(maxPointsPerTrip) ? Math.Max(1d, Math.Floor((double)maxPointsPerTrip / paidPerUnit)) : 1d;
        return (int)Math.Min(available, Math.Min(needed, perTrip));
    }

    /// <summary>
    /// Study time for the points paid. <paramref name="researchSpeed"/> is the pawn's research speed times the bench's
    /// research speed factor; it is floored, never trusted as a divisor as it stands.
    /// </summary>
    public static int StudyTicks(float points, float researchSpeed)
    {
        double speed = float.IsNaN(researchSpeed) || researchSpeed < MinResearchSpeed ? MinResearchSpeed : researchSpeed;
        if (!IsUsable(points))
            return 1;
        double ticks = points * (double)TicksPerPoint / speed;
        // Allow for float rounding, so a trip sized by MaxPointsPerTrip lasts exactly MaxTicksPerJob.
        return Math.Max(1, (int)Math.Ceiling(ticks - 0.001));
    }

    /// <summary>The most points one trip can study at this speed, from <see cref="MaxTicksPerJob"/>.</summary>
    public static float MaxPointsPerTrip(float researchSpeed)
    {
        float speed = float.IsNaN(researchSpeed) || researchSpeed < MinResearchSpeed ? MinResearchSpeed : researchSpeed;
        return MaxTicksPerJob * speed / TicksPerPoint;
    }

    /// <summary>Scrap units left for recycling after study: none by default, half (rounded up) when studied first.</summary>
    public static int RemainsAfterStudy(int units, RecycleOrStudy mode)
    {
        if (mode != RecycleOrStudy.StudyThenRecycle || units <= 0)
            return 0;
        return (int)Math.Ceiling(units * RemainsFraction);
    }

    private static bool IsUsable(float value) => value > 0f && !float.IsInfinity(value) && !float.IsNaN(value);
}
