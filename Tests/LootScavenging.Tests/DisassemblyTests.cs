using System.Linq;
using LootScavenging.Model;
using Xunit;

namespace LootScavenging.Tests;

public class DisassemblyTests
{

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
    public void EjectedAmmoFollowsRemainingCharges()
    {
        Assert.Equal(0, Disassembly.EjectedAmmo(0, 3, 1, 0));
        Assert.Equal(6, Disassembly.EjectedAmmo(3, 5, 2, 0));
        Assert.Equal(7, Disassembly.EjectedAmmo(2, 3, 0, 10)); // ceil(10 * 2/3)
    }

    // Spec: "Loaded charges in reloadable gear follow their own setting: eject, or go with the item." Ejecting matters for
    // every outcome that destroys or replaces the item; a kept item keeps its charges loaded. Always on since 0.6.0.
    [Theory]
    [InlineData(Outcome.Scrap, true)]
    [InlineData(Outcome.Disassemble, true)]
    [InlineData(Outcome.Delete, true)]
    [InlineData(Outcome.Keep, false)]
    public void ChargesAreEjectedWhenTheItemIsDestroyedOrReplaced(Outcome outcome, bool ejects)
    {
        Assert.Equal(ejects, Disassembly.EjectsCharges(outcome));
    }
}
