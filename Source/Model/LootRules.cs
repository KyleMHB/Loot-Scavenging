namespace LootScavenging.Model;

/// <summary>One category's rule (spec, "Loot rules", steps 5 to 8). Conditions and chance arrive with later tickets.</summary>
public sealed class CategoryRule
{
    public Outcome outcome;

    public CategoryRule()
    {
    }

    public CategoryRule(Outcome outcome)
    {
        this.outcome = outcome;
    }
}

/// <summary>
/// The rule values the evaluator reads. Fields are public so the settings can Scribe them by reference.
/// The code defaults are the ticket 01 test rules; ticket 11 replaces them with the Progression preset.
/// </summary>
public sealed class LootRules
{
    // Owner groups (step 1). Colony pawns have no toggle: they are never affected.
    public bool hostile = true;
    public bool nonHostileFaction;
    public bool noFaction;
    public bool prisoners;

    // Place (step 2).
    public bool homeMapOnly;

    // Moment (step 3). Death always applies; this extends the rules to downed and strip.
    public bool onDownedAndStrip;

    // Category rules (step 5).
    public CategoryRule weapons = new CategoryRule(Outcome.Delete);
    public CategoryRule apparel = new CategoryRule(Outcome.Keep);

    public bool IsGroupOn(OwnerGroup group)
    {
        switch (group)
        {
            case OwnerGroup.Hostile: return hostile;
            case OwnerGroup.NonHostileFaction: return nonHostileFaction;
            case OwnerGroup.NoFaction: return noFaction;
            case OwnerGroup.Prisoner: return prisoners;
            default: return false;
        }
    }

    /// <summary>The rule for a category, or null when the category has none yet.</summary>
    public CategoryRule RuleFor(ItemCategory category)
    {
        switch (category)
        {
            case ItemCategory.Weapon: return weapons;
            case ItemCategory.Apparel: return apparel;
            default: return null;
        }
    }
}
