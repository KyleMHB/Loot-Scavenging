using System;

namespace LootScavenging.Model;

/// <summary>Why the evaluator chose its outcome. Written to the dev-mode log, one line per decision.</summary>
public enum DecisionReason
{
    ColonyPawn,
    OwnerGroupOff,
    NoMap,
    NotHomeMap,
    NoCategoryRule,
    CategoryRule,
    Protected,
    RemoveList,
    ConditionsNotMet,
    ChanceFailed,

    /// <summary>The owner's Who row is "Mark unfamiliar": kept whole, and marked when out of reach (ticket 25).</summary>
    MarkRow,
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
/// The ordered loot checks from the spec's "Loot rules" (ticket 25): owner row, place, protection, remove list, then the
/// category rule. The first check that decides ends the run. No side effects: the caller applies the outcome.
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
            ?? CheckProtection(item, rules)
            ?? CheckRemoveList(item, rules)
            ?? CheckMarkRow(item, rules)
            ?? ApplyCategory(item, rules, roll ?? fallbackRandom.NextDouble);
    }

    /// <summary>
    /// Whether the rules act on this pawn at all: steps 1 and 2, which depend only on the pawn and its map. A row set to Mark
    /// covers its group: the gear is judged, and kept whole.
    /// </summary>
    public static bool CoversPawn(OwnerGroup owner, MapKind map, LootRules rules)
    {
        var pawn = new DropCandidate { Owner = owner, Map = map };
        return CheckOwner(pawn, rules) == null && CheckPlace(pawn, rules) == null;
    }

    // Step 1.
    private static LootDecision? CheckOwner(DropCandidate item, LootRules rules)
    {
        if (item.Owner == OwnerGroup.Colony)
            return LootDecision.Leave(DecisionReason.ColonyPawn);
        if (rules.RowFor(item.Owner) == WhoRule.Exclude)
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

    // "Mark unfamiliar" replaces the category rule for the group (ticket 25). After the remove list, as the old keep-original
    // drop mode was, so an item the player always removes is still removed.
    private static LootDecision? CheckMarkRow(DropCandidate item, LootRules rules)
    {
        return rules.RowFor(item.Owner) == WhoRule.Mark ? LootDecision.Leave(DecisionReason.MarkRow) : (LootDecision?)null;
    }

    // Step 3.
    private static LootDecision? CheckProtection(DropCandidate item, LootRules rules)
    {
        return IsProtected(item, rules) ? LootDecision.Leave(DecisionReason.Protected) : (LootDecision?)null;
    }

    /// <summary>The protection step on its own: quest items, unique, persona and relic weapons (each behind its toggle) and
    /// the keep list. The unfamiliar label asks it too, so a protected item is never locked away.</summary>
    public static bool IsProtected(DropCandidate item, LootRules rules)
    {
        return (rules.protectQuestItems && item.QuestTagged)
            || (rules.protectUniqueWeapons && item.UniqueWeapon)
            || (rules.protectPersonaWeapons && item.PersonaWeapon)
            || (rules.protectRelics && item.Relic)
            || rules.IsOnKeepList(item.DefName);
    }

    // The remove list targets an item outright, before the category rule, so it applies to out-of-reach items too: conditions
    // and chance are skipped. It uses the category's outcome, or Delete when the category keeps items (or has no rule), so
    // "always remove" never does nothing.
    private static LootDecision? CheckRemoveList(DropCandidate item, LootRules rules)
    {
        if (!rules.IsOnRemoveList(item.DefName))
            return null;
        CategoryRule rule = rules.RuleFor(item.Category);
        Outcome targeted = rule != null && rule.outcome != Outcome.Keep ? rule.outcome : Outcome.Delete;
        return new LootDecision(targeted, DecisionReason.RemoveList);
    }

    // Step 6: the category rule, its conditions and its chance.
    private static LootDecision ApplyCategory(DropCandidate item, LootRules rules, Func<double> roll)
    {
        CategoryRule rule = rules.RuleFor(item.Category);
        if (rule == null)
            return LootDecision.Leave(DecisionReason.NoCategoryRule);
        if (!MeetsConditions(item, rule))
            return LootDecision.Leave(DecisionReason.ConditionsNotMet);
        if (rule.chance < 1f && !(roll() < rule.chance))
            return LootDecision.Leave(DecisionReason.ChanceFailed);
        return new LootDecision(rule.outcome, DecisionReason.CategoryRule);
    }

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
