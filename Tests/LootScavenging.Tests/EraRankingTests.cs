using LootScavenging.Model;
using Xunit;

namespace LootScavenging.Tests;

public class EraRankingTests
{
    private static EraResult Rank(Era research = Era.Undefined, bool unfinished = false, Era item = Era.Undefined,
        Era faction = Era.Undefined, Era colony = Era.Industrial, bool gear = true)
        => EraRanking.Resolve(research, unfinished, item, faction, gear, colony);

    [Fact]
    public void ResearchEraWinsOverItemAndFaction()
    {
        var r = Rank(research: Era.Industrial, item: Era.Neolithic, faction: Era.Spacer);
        Assert.Equal(Era.Industrial, r.Era);
        Assert.Equal(EraSource.Research, r.Source);
    }

    [Fact]
    public void ItemEraUsedWhenNoResearchEra()
    {
        var r = Rank(item: Era.Medieval, faction: Era.Spacer);
        Assert.Equal(Era.Medieval, r.Era);
        Assert.Equal(EraSource.Item, r.Source);
    }

    [Fact]
    public void CarrierFactionEraIsTheLastFallback()
    {
        var r = Rank(faction: Era.Spacer);
        Assert.Equal(Era.Spacer, r.Era);
        Assert.Equal(EraSource.CarrierFaction, r.Source);
    }

    [Fact]
    public void TheCarrierEraDoesNotApplyWhenResearchOrItemEraKnown()
    {
        Assert.Equal(EraSource.Item, Rank(item: Era.Industrial, faction: Era.Spacer).Source);
    }

    [Fact]
    public void NoEraAnywhereIsUnknownAndNotOutOfReach()
    {
        var r = Rank();
        Assert.Equal(EraSource.Unknown, r.Source);
        Assert.False(r.OutOfReach);
    }

    [Fact]
    public void EraAboveColonyIsOutOfReach()
    {
        Assert.True(Rank(research: Era.Spacer, colony: Era.Industrial).OutOfReach);
        Assert.False(Rank(research: Era.Industrial, colony: Era.Industrial).OutOfReach);
    }

    [Fact]
    public void ColonyEraIsHighestFinishedAndFloorsAtAnimal()
    {
        Assert.Equal(Era.Animal, EraRanking.ColonyEra(new Era[0]));
        Assert.Equal(Era.Animal, EraRanking.ColonyEra(new[] { Era.Undefined }));
        Assert.Equal(Era.Medieval, EraRanking.ColonyEra(new[] { Era.Neolithic, Era.Medieval, Era.Neolithic }));
    }

    [Fact]
    public void ColonyEraTakesTheFactionEraWhenItIsHigher()
    {
        Assert.Equal(Era.Medieval, EraRanking.ColonyEra(new[] { Era.Neolithic }, Era.Medieval));
        Assert.Equal(Era.Neolithic, EraRanking.ColonyEra(new Era[0], Era.Neolithic));
        Assert.Equal(Era.Industrial, EraRanking.ColonyEra(new[] { Era.Industrial }, Era.Medieval));
        Assert.Equal(Era.Animal, EraRanking.ColonyEra(new Era[0], Era.Undefined));
    }

    [Fact]
    public void UnfinishedResearchIsOutOfReachEvenWithLowEra()
    {
        Assert.True(Rank(research: Era.Neolithic, unfinished: true, colony: Era.Spacer).OutOfReach);
        Assert.True(Rank(unfinished: true, item: Era.Neolithic, colony: Era.Spacer).OutOfReach);
    }

    // Spec: "Step 3 covers modded and uncraftable gear with no era of its own." Silver, steel or raw food carried by a
    // raider are not gear, so the carrier's era does not apply to them and they stay within reach.
    [Fact]
    public void TheCarrierEraAppliesOnlyToGear()
    {
        var r = Rank(faction: Era.Spacer, colony: Era.Neolithic, gear: false);
        Assert.Equal(EraSource.Unknown, r.Source);
        Assert.False(r.OutOfReach);
    }

    [Fact]
    public void NonGearStillUsesItsResearchAndItemEra()
    {
        Assert.True(Rank(research: Era.Industrial, unfinished: true, gear: false, colony: Era.Neolithic).OutOfReach);
        Assert.Equal(EraSource.Item, Rank(item: Era.Ultra, gear: false).Source);
    }
}
