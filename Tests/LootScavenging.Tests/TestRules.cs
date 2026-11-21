using System;
using LootScavenging.Model;

namespace LootScavenging.Tests;

/// <summary>
/// The rules the evaluator tests were written against, now that the code default is the Progression preset: the hostile
/// group only, death only, weapons deleted and everything else kept.
/// </summary>
internal static class TestRules
{
    public static LootRules Basic(Action<LootRules> edit = null)
    {
        var rules = new LootRules
        {
            hostile = true,
            nonHostileFaction = false,
            noFaction = false,
            prisoners = false,
            onDownedAndStrip = false,
        };
        rules.weapons.outcome = Outcome.Delete;
        edit?.Invoke(rules);
        return rules;
    }
}
