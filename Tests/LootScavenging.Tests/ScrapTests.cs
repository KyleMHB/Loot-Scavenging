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

    // Recycle value per unit by tier (Defs/ScrapTiers.xml): 6, 9, 11, 33, 48, 65.
    [Theory]
    [InlineData(6f, 0.01f, 6f)]
    [InlineData(33f, 0.01f, 33f)]
    [InlineData(6f, 10f, 10f)]      // a market value setting above the recycle value is the worth
    [InlineData(6f, -3f, 6f)]
    [InlineData(float.NaN, 2f, 2f)]
    public void AUnitIsWorthTheLargerOfRecycleAndMarketValue(float recycle, float market, float expected)
    {
        Assert.Equal(expected, ScrapAmounts.WorthPerUnit(recycle, market));
    }

    [Theory]
    [InlineData(5, 100f, 11f, 5)]   // under the cap: unchanged
    [InlineData(9, 75f, 11f, 6)]    // 75 silver at an industrial colony
    [InlineData(9, 75f, 33f, 2)]    // 75 silver at a spacer colony
    [InlineData(5, 66f, 33f, 2)]    // exactly two units' worth
    [InlineData(5, 32f, 33f, 0)]    // worth less than one unit
    [InlineData(0, 100f, 11f, 0)]
    [InlineData(4, 0f, 6f, 0)]
    [InlineData(4, 12f, 0f, 4)]     // no worth, no cap
    public void UnitsNeverExceedWhatTheStackIsWorth(int units, float total, float worth, int expected)
    {
        Assert.Equal(expected, ScrapAmounts.CapUnits(units, total, worth));
    }

    [Theory]
    [InlineData(ScrapTier.Neolithic, 6f)]
    [InlineData(ScrapTier.Medieval, 9f)]
    [InlineData(ScrapTier.Industrial, 11f)]
    [InlineData(ScrapTier.Spacer, 33f)]
    [InlineData(ScrapTier.Ultra, 48f)]
    [InlineData(ScrapTier.Archotech, 65f)]
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
    [InlineData(75f, 1, 11f, 1)]    // a 75 value item is one unit at industrial
    [InlineData(75f, 1, 33f, 1)]
    [InlineData(1f, 75, 11f, 6)]    // 75 one-silver items: 75 units before the cap
    [InlineData(1f, 75, 33f, 2)]
    [InlineData(12f, 1, 6f, 1)]     // a stone knife stays at one neolithic unit
    [InlineData(12f, 1, 9f, 1)]
    [InlineData(5f, 1, 6f, 0)]      // cheaper than one unit: no scrap
    [InlineData(0f, 1, 6f, 0)]
    [InlineData(1650f, 1, 33f, 5)]  // charge rifle at spacer tier is unchanged
    [InlineData(400f, 1, 11f, 3)]
    public void CappedUnitsKeepTheCurveBelowTheCap(float unitValue, int count, float worth, int expected)
    {
        Assert.Equal(expected, ScrapAmounts.CappedUnits(unitValue, count, worth));
    }

    [Theory]
    [InlineData(1f, 75, 6f, 9)]     // 75 silver as an ammunition stack: 9 units is 54 silver, within 75
    [InlineData(1f, 75, 11f, 6)]    // 9 would be 99 silver at an industrial colony
    [InlineData(1f, 75, 33f, 2)]    // and 297 at a spacer colony
    [InlineData(0.5f, 53, 6f, 4)]   // 53 arrows: 5 units before the cap, but the stack is worth 26.5
    [InlineData(0.5f, 10, 6f, 0)]   // worth 5, less than one unit
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
