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
        Assert.Equal(StudyVerdict.Eligible, Check(ScrapTier.Ultra, Era.Ultra, Project(Era.Ultra)));
        Assert.Equal(StudyVerdict.Eligible, Check(ScrapTier.Archotech, Era.Archotech, Project(Era.Archotech)));
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
    public void AnIterationUsesOnlyTheUnitsTheProjectStillNeeds()
    {
        Assert.Equal(3, StudyRules.UnitsUsed(available: 5, paidPerUnit: 20f, payable: 45f));
        Assert.Equal(5, StudyRules.UnitsUsed(available: 5, paidPerUnit: 20f, payable: 1000f));
        Assert.Equal(0, StudyRules.UnitsUsed(available: 5, paidPerUnit: 0f, payable: 1000f));
        Assert.Equal(0, StudyRules.UnitsUsed(available: 5, paidPerUnit: 20f, payable: 0f));
        Assert.Equal(5, StudyRules.UnitsUsed(available: 5, paidPerUnit: 1e-30f, payable: float.MaxValue));
    }

    [Theory]
    [InlineData(10f, 10)]   // neolithic
    [InlineData(20f, 5)]    // medieval
    [InlineData(30f, 4)]    // industrial: rounded up
    [InlineData(50f, 2)]    // spacer
    [InlineData(500f, 1)]
    [InlineData(0f, 1)]
    [InlineData(float.NaN, 1)]
    public void ABillIterationStudiesAboutAHundredPoints(float pointsPerUnit, int expectedUnits)
    {
        Assert.Equal(expectedUnits, StudyRules.BatchUnits(pointsPerUnit));
    }

    [Fact]
    public void StudyWorkScalesWithThePointsPaid()
    {
        Assert.Equal(100f * StudyRules.TicksPerPoint, StudyRules.WorkAmount(units: 5, pointsPerUnit: 20f));
        Assert.Equal(2 * StudyRules.WorkAmount(5, 20f), StudyRules.WorkAmount(10, 20f));
        Assert.Equal(StudyRules.TicksPerPoint, StudyRules.WorkAmount(5, float.NaN));
    }

    [Fact]
    public void StudyIsFasterThanBenchResearch()
    {
        // Bench research gives about 0.00825 points a tick at speed 1, so about 121 ticks a point.
        Assert.True(StudyRules.TicksPerPoint < 121);
    }

    [Theory]
    [InlineData(20, 10, 5, true)]
    [InlineData(15, 10, 5, true)]    // studying a batch leaves exactly the kept amount
    [InlineData(14, 10, 5, false)]   // a batch would dip below the kept amount
    [InlineData(5, 0, 5, true)]
    [InlineData(4, 0, 5, false)]     // not a whole batch in stock
    public void KeepInStockLeavesAtLeastTheTargetUnstudied(int inStock, int keep, int batch, bool expected)
    {
        Assert.Equal(expected, StudyRules.KeepsStudying(inStock, keep, batch));
    }

    [Theory]
    [InlineData(RecycleOrStudy.OneOrTheOther, 10, 0)]
    [InlineData(RecycleOrStudy.StudyThenRecycle, 10, 10)]
    [InlineData(RecycleOrStudy.StudyThenRecycle, 1, 1)]
    [InlineData(RecycleOrStudy.OneOrTheOther, 0, 0)]
    public void StudiedScrapFollowsTheRecycleOrStudySetting(RecycleOrStudy mode, int units, int expected)
    {
        // Each studied unit becomes one unit of studied scrap, which recycles from the tier's pool at the studied factor.
        Assert.Equal(expected, StudyRules.StudiedScrapUnits(units, mode));
    }
}
