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
    [InlineData(Era.Ultra, Era.Neolithic, ScrapTier.Ultra)]
    [InlineData(Era.Archotech, Era.Neolithic, ScrapTier.Archotech)]
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
    [InlineData(1650f, 1, 10)]   // charge rifle: several units
    [InlineData(400f, 1, 5)]
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

    // Recycle value per unit by tier (Defs/ScrapTiers.xml): 3, 4.5, 5.5, 16.5, 24, 32.5.
    [Theory]
    [InlineData(3f, 0.01f, 3f)]
    [InlineData(16.5f, 0.01f, 16.5f)]
    [InlineData(3f, 10f, 10f)]      // a market value setting above the recycle value is the worth
    [InlineData(3f, -3f, 3f)]
    [InlineData(float.NaN, 2f, 2f)]
    public void AUnitIsWorthTheLargerOfRecycleAndMarketValue(float recycle, float market, float expected)
    {
        Assert.Equal(expected, ScrapAmounts.WorthPerUnit(recycle, market));
    }

    [Theory]
    [InlineData(5, 100f, 5.5f, 5)]  // under the cap: unchanged
    [InlineData(20, 75f, 5.5f, 13)] // 75 silver at an industrial colony
    [InlineData(20, 75f, 16.5f, 4)] // 75 silver at a spacer colony
    [InlineData(5, 33f, 16.5f, 2)]  // exactly two units' worth
    [InlineData(5, 16f, 16.5f, 0)]  // worth less than one unit
    [InlineData(0, 100f, 5.5f, 0)]
    [InlineData(4, 0f, 3f, 0)]
    [InlineData(4, 12f, 0f, 4)]     // no worth, no cap
    public void UnitsNeverExceedWhatTheStackIsWorth(int units, float total, float worth, int expected)
    {
        Assert.Equal(expected, ScrapAmounts.CapUnits(units, total, worth));
    }

    [Theory]
    [InlineData(ScrapTier.Neolithic, 3f)]
    [InlineData(ScrapTier.Medieval, 4.5f)]
    [InlineData(ScrapTier.Industrial, 5.5f)]
    [InlineData(ScrapTier.Spacer, 16.5f)]
    [InlineData(ScrapTier.Ultra, 24f)]
    [InlineData(ScrapTier.Archotech, 32.5f)]
    public void ScrapIsNeverWorthMoreThanTheItemOrStack(ScrapTier tier, float worth)
    {
        foreach (float unitValue in new[] { 0f, 1f, 5f, 12f, 40f, 75f, 150f, 400f, 1650f, 6000f })
        {
            foreach (int count in new[] { 1, 2, 10, 75, 200 })
            {
                int items = ScrapAmounts.CappedUnits(unitValue, count, worth);
                int ammo = ScrapAmounts.CappedStackUnits(unitValue, count, worth);
                Assert.True(items * worth <= unitValue * count, $"{count} x {unitValue} at {tier}: {items} units");
                Assert.True(ammo * worth <= unitValue * count, $"{count} x {unitValue} ammo at {tier}: {ammo} units");
            }
        }
    }

    [Theory]
    [InlineData(75f, 1, 5.5f, 2)]   // a 75 value item is two units at industrial
    [InlineData(75f, 1, 16.5f, 2)]
    [InlineData(1f, 75, 5.5f, 13)]  // 75 one-silver items: 75 units before the cap
    [InlineData(1f, 75, 16.5f, 4)]
    [InlineData(12f, 1, 3f, 1)]     // a stone knife stays at one neolithic unit
    [InlineData(12f, 1, 4.5f, 1)]
    [InlineData(2f, 1, 3f, 0)]      // cheaper than one unit: no scrap
    [InlineData(0f, 1, 3f, 0)]
    [InlineData(1650f, 1, 16.5f, 10)] // charge rifle at spacer tier is unchanged
    [InlineData(400f, 1, 5.5f, 5)]
    public void CappedUnitsKeepTheCurveBelowTheCap(float unitValue, int count, float worth, int expected)
    {
        Assert.Equal(expected, ScrapAmounts.CappedUnits(unitValue, count, worth));
    }

    [Theory]
    [InlineData(1f, 75, 3f, 19)]    // 75 silver as an ammunition stack: 19 units is 57 silver, within 75
    [InlineData(1f, 75, 5.5f, 13)]  // 19 would be 104.5 silver at an industrial colony
    [InlineData(1f, 75, 16.5f, 4)]  // and 313.5 at a spacer colony
    [InlineData(0.5f, 53, 3f, 8)]   // 53 arrows: 9 units before the cap, but the stack is worth 26.5
    [InlineData(0.5f, 5, 3f, 0)]    // worth 2.5, less than one unit
    public void CappedAmmoStacksFollowTheSameCap(float unitValue, int count, float worth, int expected)
    {
        Assert.Equal(expected, ScrapAmounts.CappedStackUnits(unitValue, count, worth));
    }

    [Theory]
    [InlineData(0, 500, new int[0])]
    [InlineData(1200, 500, new[] { 500, 500, 200 })]
    [InlineData(500, 500, new[] { 500 })]
    public void StacksAreSplitAtTheStackLimit(int units, int limit, int[] expected)
    {
        Assert.Equal(expected, ScrapAmounts.SplitIntoStacks(units, limit));
    }

    // The names in Defs/ThingDefs/Scrap.xml.
    [Theory]
    [InlineData(ScrapTier.Neolithic, "LS_Scrap_Neolithic")]
    [InlineData(ScrapTier.Ultra, "LS_Scrap_Ultra")]
    [InlineData(ScrapTier.Archotech, "LS_Scrap_Archotech")]
    public void EachTierNamesItsScrapDef(ScrapTier tier, string defName)
    {
        Assert.Equal(defName, ScrapTiers.DefName(tier));
    }
}
