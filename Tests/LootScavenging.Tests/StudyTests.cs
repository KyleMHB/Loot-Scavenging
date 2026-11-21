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
    public void ABillIterationStudiesAtMostAboutAHundredPoints(float pointsPerUnit, int expectedUnits)
    {
        Assert.Equal(expectedUnits, StudyRules.BatchUnits(pointsPerUnit));
    }

    // Spec: "scrap is normally recycled until it is gone". A stone knife makes one unit, and that unit can be studied.
    [Fact]
    public void ALoneUnitIsStudied()
    {
        StudyPlan plan = StudyRules.Plan(things: 1, unitsPerThing: 1, maxThings: 5, paidPerUnit: 20f, payable: 1000f);
        Assert.Equal(1, plan.Things);
        Assert.Equal(20f, plan.Points);
        Assert.Equal(20f * StudyRules.TicksPerPoint, plan.Work);
    }

    [Fact]
    public void AnIterationTakesNoMoreThanItsBatch()
    {
        StudyPlan plan = StudyRules.Plan(things: 40, unitsPerThing: 1, maxThings: 5, paidPerUnit: 20f, payable: 1000f);
        Assert.Equal(5, plan.Things);
        Assert.Equal(100f, plan.Points);
    }

    // Spec: "Study time scales with the points paid": after the project's remaining-cost cap and the research factor.
    [Fact]
    public void StudyTimeFollowsThePointsActuallyPaid()
    {
        StudyPlan capped = StudyRules.Plan(things: 5, unitsPerThing: 1, maxThings: 5, paidPerUnit: 20f, payable: 45f);
        Assert.Equal(3, capped.Things);
        Assert.Equal(45f, capped.Points);
        Assert.Equal(45f * StudyRules.TicksPerPoint, capped.Work);

        // Research factor 200%: each unit pays 40, so the same five units pay and take twice as long.
        StudyPlan doubled = StudyRules.Plan(things: 5, unitsPerThing: 1, maxThings: 5, paidPerUnit: 40f, payable: 1000f);
        Assert.Equal(200f, doubled.Points);
        Assert.Equal(200f * StudyRules.TicksPerPoint, doubled.Work);
    }

    [Fact]
    public void AnOriginalItemCountsAsItsScrapUnits()
    {
        StudyPlan plan = StudyRules.Plan(things: 1, unitsPerThing: 4, maxThings: 1, paidPerUnit: 20f, payable: 1000f);
        Assert.Equal(1, plan.Things);
        Assert.Equal(80f, plan.Points);
    }

    [Fact]
    public void NothingPayableUsesNothingAndTakesTheMinimumWork()
    {
        StudyPlan plan = StudyRules.Plan(things: 5, unitsPerThing: 1, maxThings: 5, paidPerUnit: 0f, payable: 1000f);
        Assert.Equal(0, plan.Things);
        Assert.Equal(0f, plan.Points);
        Assert.Equal(StudyRules.TicksPerPoint, plan.Work);
    }

    [Fact]
    public void StudyIsFasterThanBenchResearch()
    {
        // Bench research gives about 0.00825 points a tick at speed 1, so about 121 ticks a point.
        Assert.True(StudyRules.TicksPerPoint < 121);
    }

    // "Do until you have X" keeps X units in stock. The count is taken when the work starts, while the whole stack still
    // lies at the bench and counts as stock, and the iteration then uses no more than the stock above X.
    [Theory]
    [InlineData(1, 0, true, 1)]      // a lone unit with nothing kept
    [InlineData(11, 10, true, 1)]
    [InlineData(20, 10, true, 10)]
    [InlineData(10, 10, false, 0)]   // exactly the kept amount: wait
    [InlineData(4, 10, false, 0)]
    public void KeepInStockLeavesAtLeastTheTargetUnstudied(int inStock, int keep, bool studies, int allowance)
    {
        Assert.Equal(studies, StudyRules.KeepsStudying(inStock, keep));
        Assert.Equal(allowance, StudyRules.StockAllowance(inStock, keep));
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
