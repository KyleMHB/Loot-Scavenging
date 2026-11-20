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
        foreach (ScrapTier t in new[] { ScrapTier.Neolithic, ScrapTier.Medieval, ScrapTier.Industrial, ScrapTier.Spacer })
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

    private static readonly YieldEntry<string>[] Industrial =
    {
        new YieldEntry<string>("Steel", 4, 1f),
        new YieldEntry<string>("Component", 1, 0.1f),
    };

    [Fact]
    public void FixedYieldScalesWithUnits()
    {
        Dictionary<string, int> fixedPart = YieldMath.Fixed(Industrial, 5).ToDictionary(y => y.Item, y => y.Count);
        Assert.Equal(20, fixedPart["Steel"]);
        Assert.False(fixedPart.ContainsKey("Component"));
    }

    [Fact]
    public void ChanceYieldRollsOncePerUnit()
    {
        // rolls 0.05, 0.5, 0.09, 0.99 against a 10% chance: units 1 and 3 hit.
        var rolls = new Queue<double>(new[] { 0.05, 0.5, 0.09, 0.99 });
        var result = YieldMath.RollChance(Industrial, 4, () => rolls.Dequeue()).ToList();
        Assert.Single(result);
        Assert.Equal("Component", result[0].Item);
        Assert.Equal(2, result[0].Count);
    }

    [Fact]
    public void ChanceYieldWithNoLuckGivesNothing()
    {
        Assert.Empty(YieldMath.RollChance(Industrial, 10, () => 0.9));
    }

    [Fact]
    public void ExpectedYieldIsCountTimesChanceTimesUnits()
    {
        Dictionary<string, double> e = YieldMath.Expected(Industrial, 10).ToDictionary(y => y.Item, y => y.Amount);
        Assert.Equal(40.0, e["Steel"], 6);
        Assert.Equal(1.0, e["Component"], 6);
    }

    [Fact]
    public void ZeroUnitsYieldNothing()
    {
        Assert.Empty(YieldMath.Fixed(Industrial, 0));
        Assert.Empty(YieldMath.RollChance(Industrial, 0, () => 0.0));
    }
}
