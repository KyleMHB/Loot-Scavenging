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
    public static LootDecision Evaluate(DropCandidate item, LootRules rules)
    {
        return CheckOwner(item, rules)
            ?? CheckPlace(item, rules)
            ?? CheckMoment(item, rules)
            ?? ApplyDropMode(item, rules)
            ?? ApplyCategory(item, rules);
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

    // Drop mode (spec, "Drop mode"): sits after protection (step 4) and before the category rule (step 5).
    // Returns null to fall through to the category rule.
    private static LootDecision? ApplyDropMode(DropCandidate item, LootRules rules)
    {
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
                // The unfamiliar label and equip block belong to ticket 06; here the gear just stays whole.
                if (era.OutOfReach)
                    return new LootDecision(Outcome.Keep, DecisionReason.OutOfReachKept);
                return null;
            default:
                return null;
        }
    }

    // Steps 5 and 8.
    private static LootDecision ApplyCategory(DropCandidate item, LootRules rules)
    {
        CategoryRule rule = rules.RuleFor(item.Category);
        if (rule == null)
            return LootDecision.Leave(DecisionReason.NoCategoryRule);
        return new LootDecision(rule.outcome, DecisionReason.CategoryRule);
    }
}
