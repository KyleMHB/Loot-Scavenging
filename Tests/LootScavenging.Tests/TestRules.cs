using System;
using LootScavenging.Model;

namespace LootScavenging.Tests;

/// <summary>
/// The rules the evaluator tests were written against: the hostile row on and every other row excluded, weapons deleted and
/// everything else kept.
/// </summary>
internal static class TestRules
{
    public static LootRules Basic(Action<LootRules> edit = null)
    {
        var rules = new LootRules();
        foreach (OwnerGroup group in LootRules.RowGroups)
            rules.SetRow(group, WhoRule.Exclude);
        rules.whoHostile = WhoRule.Apply;
        foreach (CategoryInfo category in ItemCategories.All)
            rules.SetRule(category, new CategoryRule(Outcome.Keep));
        rules.weapons.outcome = Outcome.Delete;
        edit?.Invoke(rules);
        return rules;
    }
}
