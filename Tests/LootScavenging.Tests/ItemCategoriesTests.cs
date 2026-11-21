using System;
using System.Linq;
using LootScavenging.Model;
using Xunit;

namespace LootScavenging.Tests;

public class ItemCategoriesTests
{
    [Fact]
    public void EveryCategoryHasExactlyOneEntry()
    {
        var all = (ItemCategory[])Enum.GetValues(typeof(ItemCategory));
        Assert.Equal(all.OrderBy(c => c), ItemCategories.All.Select(i => i.Category).OrderBy(c => c));
    }

    [Fact]
    public void EachEntryNamesItsRuleFieldAndReturnsThatRule()
    {
        var rules = new LootRules();
        Assert.Same(rules.weapons, rules.RuleFor(ItemCategory.Weapon));
        Assert.Same(rules.medicine, rules.RuleFor(ItemCategory.Medicine));
        Assert.Equal("weapons", ItemCategories.Of(ItemCategory.Weapon).Field);
        Assert.Equal("other", ItemCategories.Of(ItemCategory.Other).Field);
        foreach (CategoryInfo info in ItemCategories.All)
        {
            Assert.True(ValueWalk.PathExists(new SettingValues(), "rules." + info.Field), info.Field);
            Assert.StartsWith("LootScavenging_Category", info.LabelKey);
        }
    }
}
