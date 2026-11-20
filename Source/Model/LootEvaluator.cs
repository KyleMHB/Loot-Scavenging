using System;

namespace LootScavenging.Model;

/// <summary>Why the evaluator chose its outcome. Written to the dev-mode log, one line per decision.</summary>
public enum DecisionReason
{
    ColonyPawn,
    OwnerGroupOff,
    NoMap,
    NotHomeMap,
    MomentOff,
    NoCategoryRule,
    CategoryRule,
    Protected,
    RemoveList,
    ConditionsNotMet,
    ChanceFailed,
}

public readonly struct LootDecision
{
    public readonly Outcome Outcome;
    public readonly DecisionReason Reason;

    public LootDecision(Outcome outcome, DecisionReason reason)
    {
        Outcome = outcome;
        Reason = reason;
    }

    public static LootDecision Leave(DecisionReason reason) => new LootDecision(Outcome.Keep, reason);
}

/// <summary>
/// The ordered loot checks from the spec's "Loot rules". The first check that says "leave it" ends the run.
/// No side effects: the caller applies the outcome. Later tickets insert their steps (protection, era, conditions,
/// chance) in spec order.
/// </summary>
public static class LootEvaluator
{
    private static readonly Random fallbackRandom = new Random();

    /// <summary>
    /// <paramref name="roll"/> returns a number in [0, 1) for the chance step; tests inject it. It is only called when a
    /// category's chance is below 100%.
    /// </summary>
    public static LootDecision Evaluate(DropCandidate item, LootRules rules, Func<double> roll = null)
    {
        return CheckOwner(item, rules)
            ?? CheckPlace(item, rules)
            ?? CheckMoment(item, rules)
            ?? CheckProtection(item, rules)
            ?? ApplyCategory(item, rules, roll ?? fallbackRandom.NextDouble);
    }

    // Step 1.
    private static LootDecision? CheckOwner(DropCandidate item, LootRules rules)
    {
        if (item.Owner == OwnerGroup.Colony)
            return LootDecision.Leave(DecisionReason.ColonyPawn);
        if (!rules.IsGroupOn(item.Owner))
            return LootDecision.Leave(DecisionReason.OwnerGroupOff);
        return null;
    }

    // Step 2.
    private static LootDecision? CheckPlace(DropCandidate item, LootRules rules)
    {
        if (item.Map == MapKind.None)
            return LootDecision.Leave(DecisionReason.NoMap);
        if (rules.homeMapOnly && item.Map != MapKind.Home)
            return LootDecision.Leave(DecisionReason.NotHomeMap);
        return null;
    }

    // Step 3.
    private static LootDecision? CheckMoment(DropCandidate item, LootRules rules)
    {
        if (item.Moment != Moment.Death && !rules.onDownedAndStrip)
            return LootDecision.Leave(DecisionReason.MomentOff);
        return null;
    }

    // Step 4.
    private static LootDecision? CheckProtection(DropCandidate item, LootRules rules)
    {
        bool protectedItem = (rules.protectQuestItems && item.QuestTagged)
            || (rules.protectUniqueWeapons && item.UniqueWeapon)
            || (rules.protectPersonaWeapons && item.PersonaWeapon)
            || (rules.protectRelics && item.Relic)
            || rules.IsOnKeepList(item.DefName);
        return protectedItem ? LootDecision.Leave(DecisionReason.Protected) : (LootDecision?)null;
    }

    // Steps 5 to 8.
    private static LootDecision ApplyCategory(DropCandidate item, LootRules rules, Func<double> roll)
    {
        CategoryRule rule = rules.RuleFor(item.Category);

        // The remove list targets an item outright: conditions and chance are skipped. It uses the category's outcome,
        // or Delete when the category keeps items, so "always remove" never does nothing.
        if (rules.IsOnRemoveList(item.DefName))
        {
            Outcome targeted = rule != null && rule.outcome != Outcome.Keep ? rule.outcome : Outcome.Delete;
            return new LootDecision(targeted, DecisionReason.RemoveList);
        }

        if (rule == null)
            return LootDecision.Leave(DecisionReason.NoCategoryRule);
        if (!MeetsConditions(item, rule))
            return LootDecision.Leave(DecisionReason.ConditionsNotMet);
        if (rule.chance < 1f && !(roll() < rule.chance))
            return LootDecision.Leave(DecisionReason.ChanceFailed);
        return new LootDecision(rule.outcome, DecisionReason.CategoryRule);
    }

    // Step 6.
    private static bool MeetsConditions(DropCandidate item, CategoryRule rule)
    {
        if (rule.onlyBeyondReach && !item.OutOfReach)
            return false;
        if (!rule.limitQuality && !rule.limitHitPoints)
            return true;
        bool belowQuality = rule.limitQuality && item.Quality.HasValue && item.Quality.Value < rule.belowQuality;
        bool belowHitPoints = rule.limitHitPoints && item.HitPointFraction.HasValue && item.HitPointFraction.Value < rule.belowHitPointFraction;
        return belowQuality || belowHitPoints;
    }
}
