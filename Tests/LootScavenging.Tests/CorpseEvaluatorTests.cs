using LootScavenging.Model;
using Xunit;

namespace LootScavenging.Tests;

public class CorpseEvaluatorTests
{
    private static LootRules Loot() => new LootRules { hostile = true };

    private static CorpseRules Corpses() => new CorpseRules { humanlike = true, chance = 1f };

    private static CorpseCandidate Raider() => new CorpseCandidate { Owner = OwnerGroup.Hostile, Map = MapKind.Home, Kind = CorpseKind.Humanlike };

    private static CorpseDecision Run(CorpseCandidate c, LootRules l, CorpseRules r, double roll = 0.0) => CorpseEvaluator.Evaluate(c, l, r, roll);

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
    public void WildAnimalCorpseStaysUnlessNoFactionGroupIsOn()
    {
        var c = new CorpseCandidate { Owner = OwnerGroup.NoFaction, Map = MapKind.Home, Kind = CorpseKind.Animal };
        CorpseRules rules = Corpses();
        rules.animal = true;
        Assert.Equal(CorpseDecision.OwnerGroupOff, Run(c, Loot(), rules));

        LootRules loot = Loot();
        loot.noFaction = true;
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
        CorpseRules rules = new CorpseRules { mechanoid = true, chance = 1f };
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
    public void ChanceIsRolledAfterTheChecks()
    {
        CorpseRules rules = Corpses();
        rules.chance = 0.5f;
        Assert.Equal(CorpseDecision.Remove, Run(Raider(), Loot(), rules, roll: 0.49));
        Assert.Equal(CorpseDecision.ChanceFailed, Run(Raider(), Loot(), rules, roll: 0.5));
        rules.chance = 0f;
        Assert.Equal(CorpseDecision.ChanceFailed, Run(Raider(), Loot(), rules, roll: 0.0));
    }

    [Fact]
    public void ButcheringRemovesApparelOnlyWhenOnAndOwnerAndPlaceAllow()
    {
        CorpseRules rules = new CorpseRules { butcherRemovesApparel = true };
        Assert.True(CorpseEvaluator.RemovesApparelOnButchering(OwnerGroup.Hostile, MapKind.Home, Loot(), rules));
        Assert.False(CorpseEvaluator.RemovesApparelOnButchering(OwnerGroup.Hostile, MapKind.Home, Loot(), new CorpseRules()));
        Assert.False(CorpseEvaluator.RemovesApparelOnButchering(OwnerGroup.Colony, MapKind.Home, Loot(), rules));
        Assert.False(CorpseEvaluator.RemovesApparelOnButchering(OwnerGroup.NoFaction, MapKind.Home, Loot(), rules));
        Assert.False(CorpseEvaluator.RemovesApparelOnButchering(OwnerGroup.Hostile, MapKind.None, Loot(), rules));
    }
}
