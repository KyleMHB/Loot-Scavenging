using LootScavenging.Model;
using Xunit;

namespace LootScavenging.Tests;

public class StudyTests
{
    private static ProjectFacts Project(Era era, float cost = 1000f, float progress = 0f) =>
        new ProjectFacts(exists: true, era: era, baseCost: cost, progress: progress, needsTechprints: false,
            specialSource: false, prerequisitesCompleted: true);

    private static StudyVerdict Check(ScrapTier tier, Era colony, ProjectFacts project,
        StudyEraLimit eras = StudyEraLimit.ColonyEraAndBelow, bool techprints = false, float factor = 1f) =>
        StudyRules.Check(tier, colony, project, eras, techprints, pointsPerUnit: 20f, researchFactor: factor);

    [Fact]
    public void SameEraScrapPaysIntoTheCurrentProject()
    {
        Assert.Equal(StudyVerdict.Eligible, Check(ScrapTier.Industrial, Era.Industrial, Project(Era.Industrial)));
    }

    [Fact]
    public void LowerEraProjectsAndProjectsWithNoEraAreEligible()
    {
        Assert.Equal(StudyVerdict.Eligible, Check(ScrapTier.Industrial, Era.Industrial, Project(Era.Medieval)));
        Assert.Equal(StudyVerdict.Eligible, Check(ScrapTier.Neolithic, Era.Industrial, Project(Era.Undefined)));
        Assert.Equal(StudyVerdict.Eligible, Check(ScrapTier.Neolithic, Era.Animal, Project(Era.Animal)));
    }

    [Fact]
    public void AProjectAboveTheScrapsEraReceivesNothing()
    {
        Assert.Equal(StudyVerdict.ProjectAboveScrapEra, Check(ScrapTier.Neolithic, Era.Industrial, Project(Era.Industrial)));
        // Spacer scrap compares as Spacer, so it does not pay into Ultra projects.
        Assert.Equal(StudyVerdict.ProjectAboveScrapEra, Check(ScrapTier.Spacer, Era.Ultra, Project(Era.Ultra)));
    }

    [Fact]
    public void ScrapAboveTheColonysEraWaitsByDefault()
    {
        Assert.Equal(StudyVerdict.ScrapAboveColonyEra, Check(ScrapTier.Industrial, Era.Neolithic, Project(Era.Neolithic)));
        // Neolithic scrap also covers Animal-era gear, so an Animal-era colony can study it.
        Assert.Equal(StudyVerdict.Eligible, Check(ScrapTier.Neolithic, Era.Animal, Project(Era.Neolithic)));
    }

    [Fact]
    public void AnyEraSettingLetsHigherScrapBeStudied()
    {
        Assert.Equal(StudyVerdict.Eligible,
            Check(ScrapTier.Industrial, Era.Neolithic, Project(Era.Neolithic), StudyEraLimit.AnyEra));
    }

    [Fact]
    public void StudyOffRefusesEverything()
    {
        Assert.Equal(StudyVerdict.StudyOff,
            Check(ScrapTier.Neolithic, Era.Industrial, Project(Era.Neolithic), StudyEraLimit.None));
    }

    [Fact]
    public void NoCurrentProjectIsANormalWait()
    {
        Assert.Equal(StudyVerdict.NoProject, Check(ScrapTier.Industrial, Era.Industrial, ProjectFacts.None));
    }

    [Fact]
    public void SpecialSourceProjectsNeverReceiveAnything()
    {
        var project = new ProjectFacts(true, Era.Industrial, 1000f, 0f, needsTechprints: false, specialSource: true,
            prerequisitesCompleted: true);
        Assert.Equal(StudyVerdict.SpecialSource, Check(ScrapTier.Industrial, Era.Industrial, project, techprints: true));
        // A project with no research cost (Anomaly knowledge) is a special source too.
        Assert.Equal(StudyVerdict.SpecialSource, Check(ScrapTier.Industrial, Era.Industrial, Project(Era.Industrial, cost: 0f)));
    }

    [Fact]
    public void UnfinishedPrerequisitesAndFinishedProjectsAreRefused()
    {
        var blocked = new ProjectFacts(true, Era.Industrial, 1000f, 0f, false, false, prerequisitesCompleted: false);
        Assert.Equal(StudyVerdict.PrerequisitesUnfinished, Check(ScrapTier.Industrial, Era.Industrial, blocked));
        Assert.Equal(StudyVerdict.ProjectFinished,
            Check(ScrapTier.Industrial, Era.Industrial, Project(Era.Industrial, 1000f, 1000f)));
    }

    [Fact]
    public void TechprintProjectsReceiveNothingWithTheSettingOff()
    {
        var project = new ProjectFacts(true, Era.Industrial, 1000f, 0f, needsTechprints: true, false, true);
        Assert.Equal(StudyVerdict.NeedsTechprints, Check(ScrapTier.Industrial, Era.Industrial, project));
        Assert.Equal(StudyVerdict.Eligible, Check(ScrapTier.Industrial, Era.Industrial, project, techprints: true));
    }

    [Fact]
    public void WithTechprintsMissingStudyStopsOnePointShortOfFinishing()
    {
        var project = new ProjectFacts(true, Era.Industrial, 1000f, 990f, needsTechprints: true, false, true);
        Assert.Equal(9f, StudyRules.Payable(project, techprintProjects: true));
        var full = new ProjectFacts(true, Era.Industrial, 1000f, 999f, needsTechprints: true, false, true);
        Assert.Equal(StudyVerdict.NeedsTechprints, Check(ScrapTier.Industrial, Era.Industrial, full, techprints: true));
    }

    [Fact]
    public void AZeroResearchFactorGivesNoGainSoNothingIsUsedUp()
    {
        Assert.Equal(StudyVerdict.NoGain, Check(ScrapTier.Industrial, Era.Industrial, Project(Era.Industrial), factor: 0f));
        Assert.Equal(StudyVerdict.NoGain, Check(ScrapTier.Industrial, Era.Industrial, Project(Era.Industrial), factor: float.NaN));
    }

    [Theory]
    [InlineData(1f, 20f)]
    [InlineData(2.5f, 50f)]   // a factor above 100% multiplies, never divides
    [InlineData(0f, 0f)]
    [InlineData(float.NaN, 0f)]
    [InlineData(float.PositiveInfinity, 0f)]
    [InlineData(-1f, 0f)]
    public void PointsPerUnitFollowTheStorytellerFactor(float factor, float expected)
    {
        Assert.Equal(expected, StudyRules.PaidPerUnit(20f, factor));
    }

    [Fact]
    public void PointsAreCappedAtWhatTheProjectCanStillTake()
    {
        Assert.Equal(60f, StudyRules.Points(units: 3, paidPerUnit: 20f, payable: 1000f));
        Assert.Equal(45f, StudyRules.Points(units: 3, paidPerUnit: 20f, payable: 45f));
        Assert.Equal(0f, StudyRules.Points(units: 3, paidPerUnit: 20f, payable: float.NaN));
    }

    [Fact]
    public void AJobTakesOnlyTheUnitsTheProjectNeedsAndATripCanHold()
    {
        Assert.Equal(3, StudyRules.UnitsToTake(available: 500, paidPerUnit: 20f, payable: 45f, maxPointsPerTrip: 1000f));
        Assert.Equal(4, StudyRules.UnitsToTake(available: 4, paidPerUnit: 20f, payable: 1000f, maxPointsPerTrip: 1000f));
        Assert.Equal(10, StudyRules.UnitsToTake(available: 500, paidPerUnit: 20f, payable: 1000f, maxPointsPerTrip: 200f));
        // A unit worth more than a trip still makes a one-unit job.
        Assert.Equal(1, StudyRules.UnitsToTake(available: 500, paidPerUnit: 300f, payable: 1000f, maxPointsPerTrip: 200f));
        Assert.Equal(0, StudyRules.UnitsToTake(available: 500, paidPerUnit: 0f, payable: 1000f, maxPointsPerTrip: 200f));
    }

    [Fact]
    public void StudyTimeScalesWithPointsAndResearchSpeed()
    {
        int base100 = StudyRules.StudyTicks(100f, researchSpeed: 1f);
        Assert.Equal(100 * StudyRules.TicksPerPoint, base100);
        Assert.Equal(2 * base100, StudyRules.StudyTicks(200f, 1f));
        Assert.Equal(base100 / 2, StudyRules.StudyTicks(100f, 2f));
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(-3f)]
    [InlineData(float.NaN)]
    public void StudyTimeStaysFiniteForAnInvalidSpeed(float speed)
    {
        int ticks = StudyRules.StudyTicks(100f, speed);
        Assert.InRange(ticks, 1, StudyRules.StudyTicks(100f, StudyRules.MinResearchSpeed));
    }

    [Fact]
    public void StudyIsFasterThanBenchResearch()
    {
        // Bench research gives about 0.00825 points a tick at speed 1, so about 121 ticks a point.
        Assert.True(StudyRules.TicksPerPoint < 121);
    }

    [Fact]
    public void ATripIsBoundedInTime()
    {
        float points = StudyRules.MaxPointsPerTrip(researchSpeed: 1f);
        Assert.Equal(StudyRules.MaxTicksPerJob, StudyRules.StudyTicks(points, 1f));
    }

    [Theory]
    [InlineData(RecycleOrStudy.OneOrTheOther, 10, 0)]
    [InlineData(RecycleOrStudy.StudyThenRecycle, 10, 5)]
    [InlineData(RecycleOrStudy.StudyThenRecycle, 1, 1)]
    [InlineData(RecycleOrStudy.StudyThenRecycle, 3, 2)]
    public void RemainsAfterStudyFollowTheRecycleOrStudySetting(RecycleOrStudy mode, int units, int expected)
    {
        Assert.Equal(expected, StudyRules.RemainsAfterStudy(units, mode));
    }
}
