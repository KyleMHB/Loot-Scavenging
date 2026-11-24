using System.Collections.Generic;
using LootScavenging.Model;
using Xunit;

namespace LootScavenging.Tests;

public class InventoryRulesTests
{
    private static DropCandidate Item(ItemCategory category = ItemCategory.Medicine, string defName = "MedicineIndustrial")
    {
        return new DropCandidate
        {
            Owner = OwnerGroup.Hostile,
            Moment = Moment.Death,
            Map = MapKind.Home,
            Category = category,
            DefName = defName,
        };
    }

    private static LootRules DeleteEverything()
    {
        var rules = TestRules.Basic();
        rules.weapons.outcome = Outcome.Delete;
        rules.medicine.outcome = Outcome.Delete;
        rules.food.outcome = Outcome.Delete;
        return rules;
    }

    [Theory]
    [InlineData(ItemCategory.Ammunition)]
    [InlineData(ItemCategory.Drugs)]
    [InlineData(ItemCategory.Medicine)]
    [InlineData(ItemCategory.Food)]
    [InlineData(ItemCategory.Other)]
    public void EachInventoryGroupFollowsItsOwnRule(ItemCategory category)
    {
        var rules = TestRules.Basic();
        rules.RuleFor(category).outcome = Outcome.Delete;

        Assert.Equal(Outcome.Delete, LootEvaluator.Evaluate(Item(category), rules).Outcome);
        foreach (ItemCategory other in new[] { ItemCategory.Ammunition, ItemCategory.Drugs, ItemCategory.Medicine, ItemCategory.Food, ItemCategory.Other })
        {
            if (other != category)
                Assert.Equal(Outcome.Keep, LootEvaluator.Evaluate(Item(other), rules).Outcome);
        }
    }

    [Fact]
    public void QuestWeaponAndMedicineDropUntouchedWithDefaultRules()
    {
        var rules = TestRules.Basic();
        rules.medicine.outcome = Outcome.Delete;

        var questWeapon = Item(ItemCategory.Weapon, "Gun_BoltActionRifle");
        questWeapon.QuestTagged = true;
        var questMedicine = Item();
        questMedicine.QuestTagged = true;

        Assert.Equal(DecisionReason.Protected, LootEvaluator.Evaluate(questWeapon, rules).Reason);
        Assert.Equal(Outcome.Keep, LootEvaluator.Evaluate(questWeapon, rules).Outcome);
        Assert.Equal(Outcome.Keep, LootEvaluator.Evaluate(questMedicine, rules).Outcome);
        Assert.Equal(Outcome.Delete, LootEvaluator.Evaluate(Item(), rules).Outcome);
    }

    [Fact]
    public void UniquePersonaAndRelicWeaponsAreNeverTouched()
    {
        var rules = DeleteEverything();

        foreach (var flag in new[] { "unique", "persona", "relic" })
        {
            var weapon = Item(ItemCategory.Weapon, "MeleeWeapon_LongSword");
            if (flag == "unique") weapon.UniqueWeapon = true;
            if (flag == "persona") weapon.PersonaWeapon = true;
            if (flag == "relic") weapon.Relic = true;

            var decision = LootEvaluator.Evaluate(weapon, rules);

            Assert.Equal(Outcome.Keep, decision.Outcome);
            Assert.Equal(DecisionReason.Protected, decision.Reason);
        }
    }

    [Fact]
    public void ATurnedOffProtectionToggleLetsThatKindThrough()
    {
        var rules = DeleteEverything();
        rules.protectQuestItems = false;
        var quest = Item(ItemCategory.Weapon, "Gun_BoltActionRifle");
        quest.QuestTagged = true;

        Assert.Equal(Outcome.Delete, LootEvaluator.Evaluate(quest, rules).Outcome);
    }

    [Fact]
    public void KeepListItemsAreNeverTouchedAndBeatTheRemoveList()
    {
        var rules = DeleteEverything();
        rules.keepList.Add("MedicineIndustrial");
        rules.removeList.Add("MedicineIndustrial");

        var decision = LootEvaluator.Evaluate(Item(), rules);

        Assert.Equal(Outcome.Keep, decision.Outcome);
        Assert.Equal(DecisionReason.Protected, decision.Reason);
    }

    [Fact]
    public void RemoveListItemsAreActedOnEvenWhenTheirCategoryKeeps()
    {
        var rules = TestRules.Basic();
        rules.removeList.Add("Silver");

        var decision = LootEvaluator.Evaluate(Item(ItemCategory.Other, "Silver"), rules);

        Assert.Equal(Outcome.Delete, decision.Outcome);
        Assert.Equal(DecisionReason.RemoveList, decision.Reason);
    }

    [Fact]
    public void RemoveListUsesTheCategoryOutcomeWhenItActs()
    {
        var rules = TestRules.Basic();
        rules.food.outcome = Outcome.Scrap;
        rules.removeList.Add("MealSimple");

        Assert.Equal(Outcome.Scrap, LootEvaluator.Evaluate(Item(ItemCategory.Food, "MealSimple"), rules).Outcome);
    }

    [Fact]
    public void RemoveListSkipsConditionsAndChance()
    {
        var rules = TestRules.Basic();
        rules.other.chance = 0f;
        rules.other.onlyBeyondReach = true;
        rules.removeList.Add("Silver");

        Assert.Equal(Outcome.Delete, LootEvaluator.Evaluate(Item(ItemCategory.Other, "Silver"), rules, () => 0.99).Outcome);
    }

    // The drop mode used to return first, so an out-of-reach item on the remove list was kept whole in keep-original
    // mode and scrapped in the scrap modes.
    [Theory]
    [InlineData(DropMode.ScrapOutOfReach)]
    [InlineData(DropMode.ScrapEverything)]
    [InlineData(DropMode.KeepOriginal)]
    [InlineData(DropMode.Off)]
    public void RemoveListBeatsEveryDropModeForOutOfReachItems(DropMode mode)
    {
        var rules = TestRules.Basic();
        rules.dropMode = mode;
        rules.weapons.outcome = Outcome.Keep;
        rules.removeList.Add("Gun_ChargeRifle");
        var item = Item(ItemCategory.Weapon, "Gun_ChargeRifle");
        item.ItemEra = new EraResult(Era.Spacer, EraSource.Research, true);
        item.OutOfReach = true;

        var decision = LootEvaluator.Evaluate(item, rules);

        Assert.Equal(Outcome.Delete, decision.Outcome);
        Assert.Equal(DecisionReason.RemoveList, decision.Reason);
    }

    [Fact]
    public void RemoveListDoesNotOverrideTheOwnerCheck()
    {
        var rules = TestRules.Basic();
        rules.removeList.Add("Silver");
        var colony = Item(ItemCategory.Other, "Silver");
        colony.Owner = OwnerGroup.Colony;

        Assert.Equal(Outcome.Keep, LootEvaluator.Evaluate(colony, rules).Outcome);
    }

    [Fact]
    public void ListEntriesForItemsThatNoLongerExistAreIgnored()
    {
        var rules = TestRules.Basic();
        rules.keepList.AddRange(new[] { "RemovedMod_Gizmo", "", "silver" });
        rules.removeList.Add("RemovedMod_Widget");

        var decision = LootEvaluator.Evaluate(Item(ItemCategory.Other, "Silver"), rules);

        Assert.Equal(Outcome.Keep, decision.Outcome);
        Assert.Equal(DecisionReason.CategoryRule, decision.Reason);
        Assert.Equal(DecisionReason.CategoryRule, LootEvaluator.Evaluate(Item(ItemCategory.Other, null), rules).Reason);
    }

    [Fact]
    public void BeyondReachLimitOnlyActsOnOutOfReachItems()
    {
        var rules = TestRules.Basic();
        rules.medicine.outcome = Outcome.Delete;
        rules.medicine.onlyBeyondReach = true;
        var within = Item();
        var beyond = Item();
        beyond.OutOfReach = true;

        var kept = LootEvaluator.Evaluate(within, rules);

        Assert.Equal(Outcome.Keep, kept.Outcome);
        Assert.Equal(DecisionReason.ConditionsNotMet, kept.Reason);
        Assert.Equal(Outcome.Delete, LootEvaluator.Evaluate(beyond, rules).Outcome);
    }

    [Fact]
    public void QualityLimitActsOnlyBelowTheThreshold()
    {
        var rules = TestRules.Basic();
        rules.weapons.limitQuality = true;
        rules.weapons.belowQuality = ItemQuality.Normal;
        var poor = Item(ItemCategory.Weapon, "Gun_X");
        poor.Quality = ItemQuality.Poor;
        var normal = Item(ItemCategory.Weapon, "Gun_X");
        normal.Quality = ItemQuality.Normal;
        var noQuality = Item(ItemCategory.Weapon, "Gun_X");

        Assert.Equal(Outcome.Delete, LootEvaluator.Evaluate(poor, rules).Outcome);
        Assert.Equal(Outcome.Keep, LootEvaluator.Evaluate(normal, rules).Outcome);
        Assert.Equal(Outcome.Keep, LootEvaluator.Evaluate(noQuality, rules).Outcome);
    }

    [Fact]
    public void HitPointLimitActsOnlyBelowThePercentage()
    {
        var rules = TestRules.Basic();
        rules.apparel.outcome = Outcome.Delete;
        rules.apparel.limitHitPoints = true;
        rules.apparel.belowHitPointFraction = 0.5f;
        var worn = Item(ItemCategory.Apparel, "Apparel_X");
        worn.HitPointFraction = 0.3f;
        var sound = Item(ItemCategory.Apparel, "Apparel_X");
        sound.HitPointFraction = 0.5f;

        Assert.Equal(Outcome.Delete, LootEvaluator.Evaluate(worn, rules).Outcome);
        Assert.Equal(Outcome.Keep, LootEvaluator.Evaluate(sound, rules).Outcome);
    }

    [Fact]
    public void QualityAndHitPointLimitsAreAlternativesAndReachIsAGate()
    {
        var rules = TestRules.Basic();
        rules.weapons.limitQuality = true;
        rules.weapons.limitHitPoints = true;
        rules.weapons.onlyBeyondReach = true;
        var item = Item(ItemCategory.Weapon, "Gun_X");
        item.Quality = ItemQuality.Excellent;
        item.HitPointFraction = 0.2f;

        Assert.Equal(DecisionReason.ConditionsNotMet, LootEvaluator.Evaluate(item, rules).Reason);

        item.OutOfReach = true;
        Assert.Equal(Outcome.Delete, LootEvaluator.Evaluate(item, rules).Outcome);
    }

    [Fact]
    public void EachCategoryRollsItsOwnChance()
    {
        var rules = TestRules.Basic();
        rules.medicine.outcome = Outcome.Delete;
        rules.medicine.chance = 0.25f;
        rules.food.outcome = Outcome.Delete;
        rules.food.chance = 0.75f;
        var rolls = new Queue<double>(new[] { 0.5, 0.5, 0.1, 0.9 });

        Assert.Equal(DecisionReason.ChanceFailed, LootEvaluator.Evaluate(Item(ItemCategory.Medicine), rules, rolls.Dequeue).Reason);
        Assert.Equal(Outcome.Delete, LootEvaluator.Evaluate(Item(ItemCategory.Food), rules, rolls.Dequeue).Outcome);
        Assert.Equal(Outcome.Delete, LootEvaluator.Evaluate(Item(ItemCategory.Medicine), rules, rolls.Dequeue).Outcome);
        Assert.Equal(DecisionReason.ChanceFailed, LootEvaluator.Evaluate(Item(ItemCategory.Food), rules, rolls.Dequeue).Reason);
    }

    [Fact]
    public void ChanceIsOnlyRolledWhenTheItemQualifiesAndTheChanceIsBelowAlways()
    {
        var rules = TestRules.Basic();
        rules.medicine.outcome = Outcome.Delete;
        rules.medicine.chance = 0.5f;
        rules.medicine.onlyBeyondReach = true;
        rules.food.outcome = Outcome.Delete;
        int rolled = 0;

        LootEvaluator.Evaluate(Item(ItemCategory.Medicine), rules, () => { rolled++; return 0; });
        LootEvaluator.Evaluate(Item(ItemCategory.Food), rules, () => { rolled++; return 0; });

        Assert.Equal(0, rolled);
    }

    [Fact]
    public void ZeroChanceNeverActs()
    {
        var rules = TestRules.Basic();
        rules.medicine.outcome = Outcome.Delete;
        rules.medicine.chance = 0f;

        Assert.Equal(Outcome.Keep, LootEvaluator.Evaluate(Item(), rules, () => 0.0).Outcome);
    }
}
