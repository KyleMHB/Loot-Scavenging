using System.Linq;
using LootScavenging.Model;
using Xunit;

namespace LootScavenging.Tests;

public class DisassemblyTests
{
    [Theory]
    [InlineData(0.0, 10)]
    [InlineData(0.5, 25)]
    [InlineData(0.999, 40)]
    public void DegradedHitPointsLandInsideTheRange(double roll, int expected)
    {
        Assert.Equal(expected, Disassembly.DegradedHitPoints(100, 100, 0.1f, 0.4f, roll));
    }

    [Fact]
    public void DegradingNeverRepairsOrDestroys()
    {
        Assert.Equal(5, Disassembly.DegradedHitPoints(100, 5, 0.1f, 0.4f, 0.5));
        Assert.Equal(1, Disassembly.DegradedHitPoints(3, 3, 0f, 0f, 0.5));
    }

    [Fact]
    public void QualityStepsDownOnceAndStopsAtAwful()
    {
        Assert.Equal(ItemQuality.Poor, Disassembly.StepDown(ItemQuality.Normal));
        Assert.Equal(ItemQuality.Awful, Disassembly.StepDown(ItemQuality.Awful));
    }

    [Fact]
    public void EfficiencyScalesTheItemBasedYield()
    {
        var costs = new[] { new YieldAmount<string>("Steel", 40), new YieldAmount<string>("Component", 2) };
        int At(float eff) => Disassembly.FromCosts(costs, 1, eff, () => 0.0).Where(a => a.Item == "Steel").Sum(a => a.Count);
        Assert.Equal(40, At(1f));
        Assert.Equal(20, At(0.5f));
        Assert.Equal(8, At(0.2f));
        Assert.Equal(0, At(0f));
    }

    [Fact]
    public void FractionsRoundBothWaysAndStacksMultiply()
    {
        var costs = new[] { new YieldAmount<string>("Component", 1) };
        Assert.Equal(0, Disassembly.FromCosts(costs, 1, 0.5f, () => 0.9).Sum(a => a.Count));
        Assert.Equal(1, Disassembly.FromCosts(costs, 1, 0.5f, () => 0.1).Sum(a => a.Count));
        Assert.Equal(10, Disassembly.FromCosts(costs, 10, 1f, () => 0.0).Sum(a => a.Count));
    }

    [Fact]
    public void ComponentsFallbackScalesWithScrapUnits()
    {
        Assert.Equal(1, Disassembly.ComponentsFor(1));
        Assert.Equal(3, Disassembly.ComponentsFor(6));
    }

    [Fact]
    public void EjectedAmmoFollowsRemainingCharges()
    {
        Assert.Equal(0, Disassembly.EjectedAmmo(0, 3, 1, 0));
        Assert.Equal(6, Disassembly.EjectedAmmo(3, 5, 2, 0));
        Assert.Equal(7, Disassembly.EjectedAmmo(2, 3, 0, 10)); // ceil(10 * 2/3)
    }

    // Spec: "Loaded charges in reloadable gear follow their own setting: eject, or go with the item." Ejecting matters for
    // every outcome that destroys or replaces the item; a kept or degraded item keeps its charges loaded.
    [Theory]
    [InlineData(Outcome.Scrap, true)]
    [InlineData(Outcome.Disassemble, true)]
    [InlineData(Outcome.Delete, true)]
    [InlineData(Outcome.Keep, false)]
    [InlineData(Outcome.Degrade, false)]
    public void ChargesAreEjectedWhenTheItemIsDestroyedOrReplaced(Outcome outcome, bool ejects)
    {
        Assert.Equal(ejects, Disassembly.EjectsCharges(outcome, ejectSetting: true));
        Assert.False(Disassembly.EjectsCharges(outcome, ejectSetting: false));
    }
}
