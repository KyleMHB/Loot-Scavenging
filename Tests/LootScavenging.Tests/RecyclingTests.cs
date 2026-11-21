using System;
using System.Collections.Generic;
using System.Linq;
using LootScavenging.Model;
using Xunit;

namespace LootScavenging.Tests;

public class RecyclingTests
{
    private static readonly (ScrapTier tier, string[] benches)[] Table =
    {
        (ScrapTier.Neolithic, new[] { "CraftingSpot" }),
        (ScrapTier.Medieval, new[] { "FueledSmithy", "ElectricSmithy" }),
        (ScrapTier.Industrial, new[] { "TableMachining" }),
        (ScrapTier.Spacer, new[] { "FabricationBench" }),
        (ScrapTier.Ultra, new[] { "FabricationBench" }),
        (ScrapTier.Archotech, new[] { "FabricationBench" }),
    };

    private static string[] UsersOf(ScrapTier scrap) => RecycleBenches.UsersFor(scrap, Table).ToArray();

    [Fact]
    public void MachiningTableRecyclesIndustrialAndMedievalScrap()
    {
        Assert.Contains("TableMachining", UsersOf(ScrapTier.Industrial));
        Assert.Contains("TableMachining", UsersOf(ScrapTier.Medieval));
        Assert.Contains("TableMachining", UsersOf(ScrapTier.Neolithic));
    }

    [Fact]
    public void SmithyRefusesIndustrialScrapButTakesMedievalAndBelow()
    {
        Assert.DoesNotContain("FueledSmithy", UsersOf(ScrapTier.Industrial));
        Assert.DoesNotContain("ElectricSmithy", UsersOf(ScrapTier.Spacer));
        Assert.Contains("FueledSmithy", UsersOf(ScrapTier.Medieval));
        Assert.Contains("ElectricSmithy", UsersOf(ScrapTier.Neolithic));
    }

    [Fact]
    public void CraftingSpotRecyclesOnlyNeolithicScrap()
    {
        Assert.Contains("CraftingSpot", UsersOf(ScrapTier.Neolithic));
        Assert.DoesNotContain("CraftingSpot", UsersOf(ScrapTier.Medieval));
    }

    [Fact]
    public void FabricationBenchRecyclesEveryTier()
    {
        foreach (ScrapTier t in (ScrapTier[])System.Enum.GetValues(typeof(ScrapTier)))
            Assert.Contains("FabricationBench", UsersOf(t));
    }

    [Fact]
    public void BenchNamedForTwoTiersTakesTheHigherOne()
    {
        var table = new[]
        {
            (ScrapTier.Neolithic, new[] { "Shared" }),
            (ScrapTier.Industrial, new[] { "Shared" }),
        };
        string[] medieval = RecycleBenches.UsersFor(ScrapTier.Medieval, table).ToArray();
        Assert.Equal(new[] { "Shared" }, medieval);
    }

    [Fact]
    public void NoBenchIsListedTwice()
    {
        string[] users = UsersOf(ScrapTier.Neolithic);
        Assert.Equal(users.Length, users.Distinct().Count());
    }

    // Market values as in the game: steel 1.9, silver 1, component 32.
    private static readonly Dictionary<string, float> Market = new Dictionary<string, float>
    {
        ["Steel"] = 1.9f, ["Silver"] = 1f, ["Component"] = 32f, ["Free"] = 0f,
    };

    private static float MarketOf(string item) => Market[item];

    private static readonly PoolEntry<string>[] Industrial =
    {
        new PoolEntry<string>("Steel", 60),
        new PoolEntry<string>("Silver", 30),
        new PoolEntry<string>("Component", 10),
    };

    [Theory]
    [InlineData(0.0, "Steel")]
    [InlineData(0.59, "Steel")]
    [InlineData(0.6, "Silver")]
    [InlineData(0.89, "Silver")]
    [InlineData(0.9, "Component")]
    [InlineData(0.999, "Component")]
    public void PickFollowsTheWeights(double roll, string expected)
    {
        Assert.True(LootPool.TryPick(Industrial, MarketOf, roll, out string item));
        Assert.Equal(expected, item);
    }

    [Fact]
    public void WorthlessAndWeightlessEntriesAreNeverPicked()
    {
        var pool = new[] { new PoolEntry<string>("Free", 50), new PoolEntry<string>("Steel", 0), new PoolEntry<string>("Silver", 1) };
        foreach (double roll in new[] { 0.0, 0.5, 0.999 })
        {
            Assert.True(LootPool.TryPick(pool, MarketOf, roll, out string item));
            Assert.Equal("Silver", item);
        }
        Assert.False(LootPool.TryPick(new[] { new PoolEntry<string>("Free", 5) }, MarketOf, 0.5, out _));
        Assert.False(LootPool.TryPick(new PoolEntry<string>[0], MarketOf, 0.5, out _));
    }

    [Fact]
    public void AmountRoundsRandomlyAroundTheExactQuantity()
    {
        // 11 silver of steel at 1.9 each is 5.79 items: 6 with probability 0.79, else 5.
        Assert.Equal(6, LootPool.Amount(11, 1.9f, 0.5));
        Assert.Equal(5, LootPool.Amount(11, 1.9f, 0.9));
        Assert.Equal(11, LootPool.Amount(11, 1f, 0.0));
    }

    [Fact]
    public void ExpensiveMaterialDropsAsOneItemWithTheMatchingChance()
    {
        // 11 silver of component (32) is 0.34 of an item: one item about a third of the time, never two.
        Assert.Equal(1, LootPool.Amount(11, 32f, 0.3));
        Assert.Equal(0, LootPool.Amount(11, 32f, 0.4));
        int hits = 0;
        var rng = new Random(7);
        for (int i = 0; i < 20000; i++)
            hits += LootPool.Amount(11, 32f, rng.NextDouble());
        Assert.InRange(hits / 20000.0, 11 / 32.0 - 0.01, 11 / 32.0 + 0.01);
    }

    [Fact]
    public void AmountIsZeroForNoValueOrNoMarketValue()
    {
        Assert.Equal(0, LootPool.Amount(0, 1.9f, 0.0));
        Assert.Equal(0, LootPool.Amount(11, 0f, 0.0));
        Assert.Equal(0, LootPool.Amount(double.NaN, 1f, 0.0));
    }

    [Fact]
    public void EachUnitPicksExactlyOneMaterial()
    {
        // One roll to pick, one to round: units 1..3 pick steel, silver, component.
        var rolls = new Queue<double>(new[] { 0.1, 0.0, 0.7, 0.0, 0.95, 0.0 });
        List<YieldAmount<string>> result = LootPool.Roll(Industrial, 11f, 1f, 3, MarketOf, () => rolls.Dequeue());
        Assert.Equal(3, result.Count);
        Assert.Equal(6, result.Single(a => a.Item == "Steel").Count);
        Assert.Equal(11, result.Single(a => a.Item == "Silver").Count);
        Assert.Equal(1, result.Single(a => a.Item == "Component").Count);
    }

    [Fact]
    public void RepeatedPicksOfOneMaterialAreAddedTogether()
    {
        List<YieldAmount<string>> result = LootPool.Roll(Industrial, 10f, 1f, 4, MarketOf, () => 0.0);
        Assert.Single(result);
        Assert.Equal("Steel", result[0].Item);
        Assert.Equal(4 * 6, result[0].Count); // 10 / 1.9 = 5.26 rounds up at roll 0
    }

    [Fact]
    public void LongRunValueMatchesValuePerUnit()
    {
        var rng = new Random(11);
        const int units = 40000;
        List<YieldAmount<string>> result = LootPool.Roll(Industrial, 11f, 1f, units, MarketOf, rng.NextDouble);
        double value = result.Sum(a => a.Count * (double)Market[a.Item]);
        Assert.InRange(value / units, 11 * 0.99, 11 * 1.01);
    }

    [Fact]
    public void LongRunShareOfValueMatchesTheWeight()
    {
        var rng = new Random(13);
        const int units = 60000;
        List<YieldAmount<string>> result = LootPool.Roll(Industrial, 11f, 1f, units, MarketOf, rng.NextDouble);
        double total = result.Sum(a => a.Count * (double)Market[a.Item]);
        foreach (PoolEntry<string> e in Industrial)
        {
            double share = result.Single(a => a.Item == e.Item).Count * (double)Market[e.Item] / total;
            Assert.InRange(share, e.Weight / 100.0 - 0.01, e.Weight / 100.0 + 0.01);
        }
    }

    [Fact]
    public void ExpectedMatchesTheWeightsAndValue()
    {
        Dictionary<string, double> e = LootPool.Expected(Industrial, 11f, 10, MarketOf).ToDictionary(x => x.Item, x => x.Amount);
        Assert.Equal(10 * 0.6 * 11 / 1.9, e["Steel"], 4);
        Assert.Equal(10 * 0.3 * 11 / 1.0, e["Silver"], 6);
        Assert.Equal(10 * 0.1 * 11 / 32.0, e["Component"], 4);
    }

    [Theory]
    [InlineData(0.5f, 0.5)]
    [InlineData(1f, 1.0)]
    [InlineData(0f, 0.0)]
    [InlineData(-1f, 0.0)]
    [InlineData(float.NaN, 0.0)]
    public void ValueFactorScalesTheAverageValue(float factor, double share)
    {
        var rng = new Random(17);
        const int units = 40000;
        List<YieldAmount<string>> result = LootPool.Roll(Industrial, 11f, factor, units, MarketOf, rng.NextDouble);
        double value = result.Sum(a => a.Count * (double)Market[a.Item]);
        Assert.InRange(value / units, 11 * share * 0.98 - 1e-9, 11 * share * 1.02 + 1e-9);
    }

    // Spec: "A stone knife becomes one unit" and "scrap is normally recycled until it is gone": a bill takes whatever lies
    // in a stack, up to its batch, and works for the units it took.
    [Theory]
    [InlineData(1, 5, 1)]
    [InlineData(3, 5, 3)]
    [InlineData(50, 5, 5)]
    [InlineData(4, 0, 1)]   // a batch below one still takes a unit
    public void AnIterationTakesUpToItsBatchAndAcceptsFewer(int available, int batch, int expected)
    {
        Assert.Equal(expected, RecycleBatch.Units(available, batch));
    }

    [Fact]
    public void RecyclingWorkIsPerUnitTaken()
    {
        Assert.Equal(80f, RecycleBatch.Work(units: 1, workPerUnit: 80f));
        Assert.Equal(400f, RecycleBatch.Work(units: 5, workPerUnit: 80f));
    }

    [Fact]
    public void NoUnitsOrNoPoolYieldNothing()
    {
        Assert.Empty(LootPool.Roll(Industrial, 11f, 1f, 0, MarketOf, () => 0.0));
        Assert.Empty(LootPool.Roll(new PoolEntry<string>[0], 11f, 1f, 5, MarketOf, () => 0.0));
        Assert.Empty(LootPool.Expected(Industrial, 11f, 0, MarketOf));
    }
}
