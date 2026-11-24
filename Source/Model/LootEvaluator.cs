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
    OutOfReachScrap,
    ScrapEverything,
    ComponentsFallback,
    OutOfReachKept,
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
            ?? CheckRemoveList(item, rules)
            ?? ApplyDropMode(item, rules)
            ?? ApplyCategory(item, rules, roll ?? fallbackRandom.NextDouble);
    }

    /// <summary>Whether the rules act on this pawn at all: steps 1 and 2, which depend only on the pawn and its map.</summary>
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

    // Drop mode (spec, "Drop mode"): sits after protection (step 4) and the remove list, and before the category rule (step 5).
    // Returns null to fall through to the category rule.
    private static LootDecision? ApplyDropMode(DropCandidate item, LootRules rules)
    {
        // Medicine and ammunition are not out-of-reach gear to scrap by default; their own category rules decide.
        if (item.Category == ItemCategory.Medicine && !rules.dropIncludesMedicine)
            return null;
        if (item.Category == ItemCategory.Ammunition && !rules.dropIncludesAmmunition)
            return null;
        EraResult era = item.ItemEra;
        switch (rules.dropMode)
        {
            case DropMode.ScrapOutOfReach:
                // "Break it straight into components" replaces the era for gear with none: it is not scrapped.
                if (era.BreakIntoComponents)
                    return new LootDecision(Outcome.Disassemble, DecisionReason.ComponentsFallback);
                if (era.OutOfReach)
                    return new LootDecision(Outcome.Scrap, DecisionReason.OutOfReachScrap);
                return null;
            case DropMode.ScrapEverything:
                if (era.BreakIntoComponents)
                    return new LootDecision(Outcome.Disassemble, DecisionReason.ComponentsFallback);
                return new LootDecision(Outcome.Scrap, DecisionReason.ScrapEverything);
            case DropMode.KeepOriginal:
                // Gear with no era cannot be judged unfamiliar later, so the components setting breaks it down here too
                // (spec: "break it straight into components"). Out-of-reach gear stays whole for the unfamiliar label.
                if (era.BreakIntoComponents)
                    return new LootDecision(Outcome.Disassemble, DecisionReason.ComponentsFallback);
                if (era.OutOfReach)
                    return new LootDecision(Outcome.Keep, DecisionReason.OutOfReachKept);
                return null;
            default:
                return null;
        }
    }

    // Step 4.
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

    // The remove list targets an item outright, before the drop mode, so it applies to out-of-reach items too: conditions
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

    // Steps 5 to 8.
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
