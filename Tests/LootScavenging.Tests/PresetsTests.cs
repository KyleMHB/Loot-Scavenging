using LootScavenging.Model;
using Xunit;

namespace LootScavenging.Tests;

// One test per row of the spec's presets table, judged through the evaluators rather than the field values.
public class PresetsTests
{
    private static readonly EraResult OutOfReachEra = new EraResult(Era.Spacer, EraSource.Research, true);

    private static DropCandidate Drop(ItemCategory category = ItemCategory.Weapon, OwnerGroup owner = OwnerGroup.Hostile,
        Moment moment = Moment.Death, ItemQuality? quality = ItemQuality.Normal, float? hitPoints = 1f, bool outOfReach = false)
    {
        return new DropCandidate
        {
            Owner = owner,
            Moment = moment,
            Map = MapKind.Home,
            Category = category,
            Quality = quality,
            HitPointFraction = hitPoints,
            ItemEra = outOfReach ? OutOfReachEra : default,
            OutOfReach = outOfReach,
            DefName = "Gun_Autopistol",
        };
    }

    private static Outcome Judge(PresetId preset, DropCandidate drop) => LootEvaluator.Evaluate(drop, Presets.Build(preset).rules, () => 0.0).Outcome;

    [Theory]
    [InlineData(ItemQuality.Poor, 1f, Outcome.Delete)]
    [InlineData(ItemQuality.Awful, 1f, Outcome.Delete)]
    [InlineData(ItemQuality.Normal, 0.6f, Outcome.Keep)]
    [InlineData(ItemQuality.Normal, 0.4f, Outcome.Delete)]
    [InlineData(ItemQuality.Excellent, 0.49f, Outcome.Delete)]
    [InlineData(ItemQuality.Good, 0.5f, Outcome.Keep)]
    public void CleanupDeletesWeaponsBelowNormalOrBelowHalfHitPoints(ItemQuality quality, float hitPoints, Outcome expected)
    {
        Assert.Equal(expected, Judge(PresetId.Cleanup, Drop(ItemCategory.Weapon, quality: quality, hitPoints: hitPoints)));
    }

    [Theory]
    [InlineData(ItemQuality.Normal, 1f, Outcome.Delete)]
    [InlineData(ItemQuality.Good, 1f, Outcome.Keep)]
    [InlineData(ItemQuality.Legendary, 0.3f, Outcome.Delete)]
    [InlineData(ItemQuality.Excellent, 0.5f, Outcome.Keep)]
    public void CleanupDeletesApparelBelowGoodOrBelowHalfHitPoints(ItemQuality quality, float hitPoints, Outcome expected)
    {
        Assert.Equal(expected, Judge(PresetId.Cleanup, Drop(ItemCategory.Apparel, quality: quality, hitPoints: hitPoints)));
    }

    [Fact]
    public void OnlyCleanupDeletesAmmunition()
    {
        Assert.Equal(Outcome.Delete, Judge(PresetId.Cleanup, Drop(ItemCategory.Ammunition, quality: null, hitPoints: 1f)));
        Assert.Equal(Outcome.Keep, Judge(PresetId.Progression, Drop(ItemCategory.Ammunition, quality: null, hitPoints: 0.1f, outOfReach: true)));
        Assert.Equal(Outcome.Keep, Judge(PresetId.Vanilla, Drop(ItemCategory.Ammunition, quality: null, hitPoints: 0.1f)));
    }

    [Theory]
    [InlineData(ItemCategory.Drugs)]
    [InlineData(ItemCategory.Medicine)]
    [InlineData(ItemCategory.Food)]
    [InlineData(ItemCategory.Other)]
    public void EveryPresetKeepsInventoryGroups(ItemCategory category)
    {
        foreach (PresetId preset in new[] { PresetId.Progression, PresetId.Cleanup, PresetId.Vanilla })
            Assert.Equal(Outcome.Keep, Judge(preset, Drop(category, quality: null, hitPoints: 0.1f)));
    }

    [Fact]
    public void ProgressionScrapsOutOfReachGearAndKeepsGearWithinReach()
    {
        foreach (ItemCategory category in new[] { ItemCategory.Weapon, ItemCategory.Apparel })
        {
            Assert.Equal(Outcome.Scrap, Judge(PresetId.Progression, Drop(category, outOfReach: true)));
            Assert.Equal(Outcome.Keep, Judge(PresetId.Progression, Drop(category, quality: ItemQuality.Awful, hitPoints: 0.1f)));
        }
    }

    [Fact]
    public void ProgressionKeepsMedicineBeyondReach()
    {
        Assert.Equal(Outcome.Keep, Judge(PresetId.Progression, Drop(ItemCategory.Medicine, outOfReach: true)));
    }

    [Theory]
    [InlineData(PresetId.Progression)]
    [InlineData(PresetId.Cleanup)]
    [InlineData(PresetId.Vanilla)]
    public void NoPresetIncludesMedicineInTheDropMode(PresetId preset)
    {
        Assert.False(Presets.Build(preset).rules.dropIncludesMedicine);
    }

    [Fact]
    public void CleanupAndVanillaHaveNoEraHandling()
    {
        Assert.Equal(Outcome.Keep, Judge(PresetId.Cleanup, Drop(ItemCategory.Weapon, quality: ItemQuality.Good, outOfReach: true)));
        Assert.Equal(DropMode.Off, Presets.Build(PresetId.Cleanup).rules.dropMode);
        Assert.Equal(DropMode.Off, Presets.Build(PresetId.Vanilla).rules.dropMode);
    }

    [Theory]
    [InlineData(Moment.Death)]
    [InlineData(Moment.Downed)]
    [InlineData(Moment.Strip)]
    public void ProgressionActsAtDeathDownedAndStrip(Moment moment)
    {
        Assert.Equal(Outcome.Scrap, Judge(PresetId.Progression, Drop(moment: moment, outOfReach: true)));
    }

    [Fact]
    public void CleanupActsAtDeathOnly()
    {
        var poor = Drop(quality: ItemQuality.Poor);
        Assert.Equal(Outcome.Delete, Judge(PresetId.Cleanup, poor));
        poor.Moment = Moment.Downed;
        Assert.Equal(DecisionReason.MomentOff, LootEvaluator.Evaluate(poor, Presets.Build(PresetId.Cleanup).rules).Reason);
        poor.Moment = Moment.Strip;
        Assert.Equal(DecisionReason.MomentOff, LootEvaluator.Evaluate(poor, Presets.Build(PresetId.Cleanup).rules).Reason);
    }

    [Theory]
    [InlineData(PresetId.Progression, OwnerGroup.Hostile, true)]
    [InlineData(PresetId.Progression, OwnerGroup.NonHostileFaction, true)]
    [InlineData(PresetId.Progression, OwnerGroup.NoFaction, true)]
    [InlineData(PresetId.Progression, OwnerGroup.Prisoner, true)]
    [InlineData(PresetId.Cleanup, OwnerGroup.Hostile, true)]
    [InlineData(PresetId.Cleanup, OwnerGroup.NonHostileFaction, false)]
    [InlineData(PresetId.Cleanup, OwnerGroup.NoFaction, false)]
    [InlineData(PresetId.Cleanup, OwnerGroup.Prisoner, false)]
    [InlineData(PresetId.Vanilla, OwnerGroup.Hostile, false)]
    [InlineData(PresetId.Vanilla, OwnerGroup.NonHostileFaction, false)]
    [InlineData(PresetId.Vanilla, OwnerGroup.NoFaction, false)]
    [InlineData(PresetId.Vanilla, OwnerGroup.Prisoner, false)]
    public void OwnerGroupsMatchTheTable(PresetId preset, OwnerGroup owner, bool affected)
    {
        var decision = LootEvaluator.Evaluate(Drop(owner: owner, quality: ItemQuality.Awful, outOfReach: true), Presets.Build(preset).rules);
        Assert.Equal(affected, decision.Reason != DecisionReason.OwnerGroupOff);
    }

    [Fact]
    public void VanillaKeepsEverything()
    {
        foreach (ItemCategory category in new[] { ItemCategory.Weapon, ItemCategory.Apparel, ItemCategory.Ammunition })
            Assert.Equal(Outcome.Keep, Judge(PresetId.Vanilla, Drop(category, quality: ItemQuality.Awful, hitPoints: 0.1f, outOfReach: true)));
    }

    private static CorpseDecision Corpse(PresetId preset, CorpseKind kind)
    {
        SettingValues v = Presets.Build(preset);
        var corpse = new CorpseCandidate { Owner = OwnerGroup.Hostile, Map = MapKind.Home, Kind = kind };
        return CorpseEvaluator.Evaluate(corpse, v.rules, v.corpses, 0.999);
    }

    [Fact]
    public void CleanupRemovesEveryHostileHumanlikeCorpseOnly()
    {
        Assert.Equal(CorpseDecision.Remove, Corpse(PresetId.Cleanup, CorpseKind.Humanlike));
        Assert.Equal(CorpseDecision.KindOff, Corpse(PresetId.Cleanup, CorpseKind.Animal));
        Assert.Equal(CorpseDecision.KindOff, Corpse(PresetId.Cleanup, CorpseKind.Mechanoid));
    }

    [Theory]
    [InlineData(PresetId.Progression)]
    [InlineData(PresetId.Vanilla)]
    public void ProgressionAndVanillaLeaveCorpses(PresetId preset)
    {
        Assert.NotEqual(CorpseDecision.Remove, Corpse(preset, CorpseKind.Humanlike));
        Assert.NotEqual(CorpseDecision.Remove, Corpse(preset, CorpseKind.Animal));
        Assert.NotEqual(CorpseDecision.Remove, Corpse(preset, CorpseKind.Mechanoid));
    }

    [Theory]
    [InlineData(PresetId.Progression)]
    [InlineData(PresetId.Cleanup)]
    [InlineData(PresetId.Vanilla)]
    public void NoPresetRemovesApparelWhenButchering(PresetId preset)
    {
        SettingValues v = Presets.Build(preset);
        v.rules.hostile = true;
        Assert.False(CorpseEvaluator.RemovesApparelOnButchering(OwnerGroup.Hostile, MapKind.Home, v.rules, v.corpses));
    }

    [Fact]
    public void TheDefaultIsProgression()
    {
        Assert.True(Presets.Defaults().SameAs(Presets.Build(PresetId.Progression)));
        Assert.False(Presets.Defaults().SameAs(Presets.Build(PresetId.Cleanup)));
    }

    [Fact]
    public void TheClassDefaultsMatchProgression()
    {
        Assert.True(new SettingValues().SameAs(Presets.Build(PresetId.Progression)));
    }

    [Theory]
    [InlineData(PresetId.Progression)]
    [InlineData(PresetId.Cleanup)]
    [InlineData(PresetId.Vanilla)]
    public void APresetJustAppliedIsUnchanged(PresetId preset)
    {
        Assert.False(Presets.IsChanged(preset, Presets.Apply(preset, Presets.Defaults())));
    }

    [Fact]
    public void ChangingAnySettingMarksThePresetChanged()
    {
        var edits = new System.Action<SettingValues>[]
        {
            v => v.rules.prisoners = !v.rules.prisoners,
            v => v.rules.apparel.belowQuality = ItemQuality.Excellent,
            v => v.rules.weapons.chance = 0.75f,
            v => v.rules.degradeMax = 0.6f,
            v => v.corpses.animal = true,
            v => v.scrapMarketValue = 1f,
            v => v.studyEras = StudyEraLimit.AnyEra,
            v => v.unfamiliarScope = UnfamiliarScope.LootedOnly,
        };
        foreach (var edit in edits)
        {
            SettingValues current = Presets.Apply(PresetId.Cleanup, Presets.Defaults());
            edit(current);
            Assert.True(Presets.IsChanged(PresetId.Cleanup, current));
        }
    }

    [Fact]
    public void EditingAppliedValuesLeavesThePresetItself()
    {
        SettingValues current = Presets.Build(PresetId.Cleanup);
        current.rules.weapons.outcome = Outcome.Scrap;
        Assert.Equal(Outcome.Delete, Presets.Build(PresetId.Cleanup).rules.weapons.outcome);
        Assert.True(Presets.IsChanged(PresetId.Cleanup, current));
    }

    [Fact]
    public void PresetsKeepTheListsAndTheListsDoNotCountAsAChange()
    {
        SettingValues current = Presets.Defaults();
        current.rules.keepList.Add("Gun_ChargeRifle");
        current.rules.removeList.Add("Apparel_TribalA");

        SettingValues applied = Presets.Apply(PresetId.Vanilla, current);

        Assert.Equal(new[] { "Gun_ChargeRifle" }, applied.rules.keepList);
        Assert.Equal(new[] { "Apparel_TribalA" }, applied.rules.removeList);
        Assert.False(Presets.IsChanged(PresetId.Vanilla, applied));
        Assert.False(Presets.IsChanged(PresetId.Progression, current));
    }

    [Fact]
    public void ResetClearsTheLists()
    {
        Assert.Empty(Presets.Defaults().rules.keepList);
        Assert.Empty(Presets.Defaults().rules.removeList);
    }
}
