using LootScavenging.Model;
using Xunit;

namespace LootScavenging.Tests;

public class CorpseEvaluatorTests
{
    private static LootRules Loot() => TestRules.Basic(r => { r.whoHostile = WhoRule.Apply; });

    private static CorpseRules Corpses() => new CorpseRules { humanlike = true };

    private static CorpseCandidate Raider() => new CorpseCandidate { Owner = OwnerGroup.Hostile, Map = MapKind.Home, Kind = CorpseKind.Humanlike };

    private static CorpseDecision Run(CorpseCandidate c, LootRules l, CorpseRules r) => CorpseEvaluator.Evaluate(c, l, r);

    [Fact]
    public void RemovesAHostileHumanlikeCorpseWhenOn()
    {
        Assert.Equal(CorpseDecision.Remove, Run(Raider(), Loot(), Corpses()));
    }

    [Fact]
    public void CodeDefaultsRemoveNothing()
    {
        Assert.Equal(CorpseDecision.KindOff, Run(Raider(), Loot(), new CorpseRules()));
    }

    [Fact]
    public void ColonyCorpsesAreNeverRemoved()
    {
        CorpseCandidate c = Raider();
        c.Owner = OwnerGroup.Colony;
        Assert.Equal(CorpseDecision.ColonyPawn, Run(c, Loot(), Corpses()));
    }

    [Fact]
    public void WildAnimalCorpseStaysWhileTheWildRowIsExcluded()
    {
        var c = new CorpseCandidate { Owner = OwnerGroup.NoFaction, Map = MapKind.Home, Kind = CorpseKind.Animal };
        CorpseRules rules = Corpses();
        rules.animal = true;
        Assert.Equal(CorpseDecision.OwnerGroupOff, Run(c, Loot(), rules));

        LootRules loot = Loot();
        loot.whoWild = WhoRule.Apply;
        Assert.Equal(CorpseDecision.Remove, Run(c, loot, rules));
    }

    [Fact]
    public void AwayMapsAreSkippedWhenHomeMapOnly()
    {
        LootRules loot = Loot();
        loot.homeMapOnly = true;
        CorpseCandidate c = Raider();
        c.Map = MapKind.Away;
        Assert.Equal(CorpseDecision.NotHomeMap, Run(c, loot, Corpses()));
        c.Map = MapKind.Home;
        Assert.Equal(CorpseDecision.Remove, Run(c, loot, Corpses()));
    }

    [Fact]
    public void NoMapIsSkipped()
    {
        CorpseCandidate c = Raider();
        c.Map = MapKind.None;
        Assert.Equal(CorpseDecision.NoMap, Run(c, Loot(), Corpses()));
    }

    [Fact]
    public void EachKindHasItsOwnToggle()
    {
        LootRules loot = Loot();
        CorpseRules rules = new CorpseRules { mechanoid = true };
        CorpseCandidate c = Raider();
        c.Kind = CorpseKind.Mechanoid;
        Assert.Equal(CorpseDecision.Remove, Run(c, loot, rules));
        c.Kind = CorpseKind.Humanlike;
        Assert.Equal(CorpseDecision.KindOff, Run(c, loot, rules));
        c.Kind = CorpseKind.Animal;
        Assert.Equal(CorpseDecision.KindOff, Run(c, loot, rules));
        c.Kind = CorpseKind.Other;
        Assert.Equal(CorpseDecision.KindOff, Run(c, loot, rules));
    }

    [Fact]
    public void ButcheringSortsOnlyWhenOwnerAndPlaceAllow()
    {
        Assert.True(CorpseEvaluator.ButcherSorts(OwnerGroup.Hostile, MapKind.Home, Loot()));
        Assert.False(CorpseEvaluator.ButcherSorts(OwnerGroup.Colony, MapKind.Home, Loot()));
        Assert.False(CorpseEvaluator.ButcherSorts(OwnerGroup.NoFaction, MapKind.Home, Loot()));
        Assert.False(CorpseEvaluator.ButcherSorts(OwnerGroup.Hostile, MapKind.None, Loot()));
    }

    // A row set to Mark lets corpses go and butchering sort, as Apply does; only Exclude leaves them alone.
    [Fact]
    public void AMarkRowCountsAsCovered()
    {
        LootRules loot = TestRules.Basic(r => { r.whoHostile = WhoRule.Mark; });
        Assert.Equal(CorpseDecision.Remove, Run(Raider(), loot, Corpses()));
        Assert.True(CorpseEvaluator.ButcherSorts(OwnerGroup.Hostile, MapKind.Home, loot));
    }
}
