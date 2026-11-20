using LootScavenging.Model;
using Xunit;

namespace LootScavenging.Tests;

public class ScrapTests
{
    [Theory]
    [InlineData(Era.Animal, Era.Industrial, ScrapTier.Neolithic)]
    [InlineData(Era.Neolithic, Era.Industrial, ScrapTier.Neolithic)]
    [InlineData(Era.Medieval, Era.Neolithic, ScrapTier.Medieval)]
    [InlineData(Era.Industrial, Era.Neolithic, ScrapTier.Industrial)]
    [InlineData(Era.Spacer, Era.Neolithic, ScrapTier.Spacer)]
    [InlineData(Era.Ultra, Era.Neolithic, ScrapTier.Spacer)]
    [InlineData(Era.Archotech, Era.Neolithic, ScrapTier.Spacer)]
    public void EraMapsToItsScrapTier(Era itemEra, Era colonyEra, ScrapTier expected)
    {
        Assert.Equal(expected, ScrapTiers.TierFor(itemEra, colonyEra));
    }

    [Fact]
    public void GearWithNoEraBecomesScrapOfTheColonysEra()
    {
        Assert.Equal(ScrapTier.Medieval, ScrapTiers.TierFor(Era.Undefined, Era.Medieval));
    }

    [Theory]
    [InlineData(12f, 1, 1)]      // stone knife: about one unit
    [InlineData(0f, 1, 1)]       // worthless gear still leaves one unit
    [InlineData(1650f, 1, 5)]    // charge rifle: several units
    [InlineData(400f, 1, 3)]
    [InlineData(12f, 40, 40)]    // a stack scales by its count
    public void AmountScalesWithWorth(float unitValue, int count, int expected)
    {
        Assert.Equal(expected, ScrapAmounts.Units(unitValue, count));
    }

    [Fact]
    public void WornGearYieldsLessThanPristineGear()
    {
        // Market value already carries quality and hit points, so lower value means fewer units.
        Assert.True(ScrapAmounts.Units(1650f, 1) > ScrapAmounts.Units(300f, 1));
    }

    [Fact]
    public void NegativeOrNanValueStillGivesOneUnit()
    {
        Assert.Equal(1, ScrapAmounts.Units(-5f, 1));
        Assert.Equal(1, ScrapAmounts.Units(float.NaN, 1));
    }

    [Theory]
    [InlineData(0, 500, new int[0])]
    [InlineData(1200, 500, new[] { 500, 500, 200 })]
    [InlineData(500, 500, new[] { 500 })]
    public void StacksAreSplitAtTheStackLimit(int units, int limit, int[] expected)
    {
        Assert.Equal(expected, ScrapAmounts.SplitIntoStacks(units, limit));
    }
}
