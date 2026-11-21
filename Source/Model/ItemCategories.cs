using System;
using System.Collections.Generic;

namespace LootScavenging.Model;

/// <summary>One item category: its rule field on <see cref="LootRules"/> (also its saved-key and lock prefix) and its page label.</summary>
public sealed class CategoryInfo
{
    public CategoryInfo(ItemCategory category, string field, string labelKey, Func<LootRules, CategoryRule> rule)
    {
        Category = category;
        Field = field;
        LabelKey = labelKey;
        Rule = rule;
    }

    public ItemCategory Category { get; }
    public string Field { get; }
    public string LabelKey { get; }
    public Func<LootRules, CategoryRule> Rule { get; }
}

/// <summary>The one table of item categories, in page order. The rules, the settings page and the saved keys all read it.</summary>
public static class ItemCategories
{
    public static readonly IReadOnlyList<CategoryInfo> All = new[]
    {
        new CategoryInfo(ItemCategory.Weapon, "weapons", "LootScavenging_CategoryWeapons", r => r.weapons),
        new CategoryInfo(ItemCategory.Apparel, "apparel", "LootScavenging_CategoryApparel", r => r.apparel),
        new CategoryInfo(ItemCategory.Ammunition, "ammunition", "LootScavenging_CategoryAmmunition", r => r.ammunition),
        new CategoryInfo(ItemCategory.Drugs, "drugs", "LootScavenging_CategoryDrugs", r => r.drugs),
        new CategoryInfo(ItemCategory.Medicine, "medicine", "LootScavenging_CategoryMedicine", r => r.medicine),
        new CategoryInfo(ItemCategory.Food, "food", "LootScavenging_CategoryFood", r => r.food),
        new CategoryInfo(ItemCategory.Other, "other", "LootScavenging_CategoryOther", r => r.other),
    };

    /// <summary>The entry for a category, or null for a value outside the table.</summary>
    public static CategoryInfo Of(ItemCategory category)
    {
        foreach (CategoryInfo info in All)
        {
            if (info.Category == category)
                return info;
        }
        return null;
    }
}
