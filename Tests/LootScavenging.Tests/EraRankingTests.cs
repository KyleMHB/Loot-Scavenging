using LootScavenging.Model;
using Xunit;

namespace LootScavenging.Tests;

public class EraRankingTests
{
    private static EraResult Rank(Era research = Era.Undefined, bool unfinished = false, Era item = Era.Undefined,
        Era faction = Era.Undefined, EraFallback fallback = EraFallback.CarrierFaction, Era colony = Era.Industrial)
        => EraRanking.Resolve(research, unfinished, item, faction, fallback, colony);

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
    public void ColonyFallbackReplacesFactionEra()
    {
        var r = Rank(faction: Era.Spacer, fallback: EraFallback.ColonyEra, colony: Era.Neolithic);
        Assert.Equal(Era.Neolithic, r.Era);
        Assert.Equal(EraSource.ColonyFallback, r.Source);
        Assert.False(r.OutOfReach);
    }

    [Fact]
    public void ComponentsFallbackBreaksGearDown()
    {
        var r = Rank(faction: Era.Spacer, fallback: EraFallback.Components);
        Assert.Equal(EraSource.Components, r.Source);
        Assert.True(r.BreakIntoComponents);
        Assert.False(r.OutOfReach);
    }

    [Fact]
    public void FallbacksDoNotApplyWhenResearchOrItemEraKnown()
    {
        Assert.Equal(EraSource.Item, Rank(item: Era.Industrial, fallback: EraFallback.Components).Source);
        Assert.False(Rank(item: Era.Industrial, fallback: EraFallback.Components).BreakIntoComponents);
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
    public void UnfinishedResearchIsOutOfReachEvenWithLowEra()
    {
        Assert.True(Rank(research: Era.Neolithic, unfinished: true, colony: Era.Spacer).OutOfReach);
        Assert.True(Rank(unfinished: true, item: Era.Neolithic, colony: Era.Spacer).OutOfReach);
    }
}
