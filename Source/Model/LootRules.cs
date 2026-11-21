using System.Collections.Generic;

namespace LootScavenging.Model;

/// <summary>One category's rule (spec, "Loot rules", steps 5 to 8).</summary>
public sealed class CategoryRule
{
    public Outcome outcome;

    // Conditions (step 6). The reach limit is a gate: when on, only items beyond the colony's reach qualify. The quality
    // and hit-point limits are alternatives: when either is on, an item qualifies if it is below any limit that is on.
    // An item without quality or hit points never meets that limit.
    public bool onlyBeyondReach;
    public bool limitQuality;
    public ItemQuality belowQuality = ItemQuality.Normal;
    public bool limitHitPoints;
    public float belowHitPointFraction = 0.5f;

    /// <summary>Step 7: chance, 0 to 1, that a qualifying item gets the outcome.</summary>
    public float chance = 1f;

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
/// The initialisers match the Progression preset (a test checks it); the settings themselves start from Presets.Defaults().
/// </summary>
public sealed class LootRules
{
    // Owner groups (step 1). Colony pawns have no toggle: they are never affected.
    public bool hostile = true;
    public bool nonHostileFaction = true;
    public bool noFaction = true;
    public bool prisoners = true;

    // Place (step 2).
    public bool homeMapOnly;

    // Moment (step 3). Death always applies; this extends the rules to downed and strip.
    public bool onDownedAndStrip = true;

    // What out-of-reach gear becomes. Ticket 03 adds the evaluator step that reads it.
    public DropMode dropMode = DropMode.ScrapOutOfReach;

    // Category rules (step 5).
    public CategoryRule weapons = new CategoryRule(Outcome.Keep);
    public CategoryRule apparel = new CategoryRule(Outcome.Keep);
    public CategoryRule ammunition = new CategoryRule(Outcome.Keep);
    public CategoryRule drugs = new CategoryRule(Outcome.Keep);
    public CategoryRule medicine = new CategoryRule(Outcome.Keep);
    public CategoryRule food = new CategoryRule(Outcome.Keep);
    public CategoryRule other = new CategoryRule(Outcome.Keep);

    // Degrade and disassemble (ticket 08).
    public float degradeMin = Disassembly.DefaultDegradeMin;
    public float degradeMax = Disassembly.DefaultDegradeMax;
    public bool degradeLowersQuality;
    public DisassemblyYield disassemblyYield = DisassemblyYield.EraTypical;
    public float disassemblyEfficiency = Disassembly.DefaultEfficiency;

    /// <summary>Loaded charges in reloadable gear: true ejects the ammo near the item, false lets it go with the item.</summary>
    public bool ejectCharges = true;

    // Protection (step 4). The four toggles default on; the keep list always wins over the remove list.
    public bool protectQuestItems = true;
    public bool protectUniqueWeapons = true;
    public bool protectPersonaWeapons = true;
    public bool protectRelics = true;

    // Lists hold item def names, so a removed mod's entries do nothing and break nothing.
    public List<string> keepList = new List<string>();
    public List<string> removeList = new List<string>();

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

    public bool IsOnKeepList(string defName) => defName != null && keepList != null && keepList.Contains(defName);

    public bool IsOnRemoveList(string defName) => defName != null && removeList != null && removeList.Contains(defName);

    /// <summary>The rule for a category, or null when the category has none yet.</summary>
    public CategoryRule RuleFor(ItemCategory category)
    {
        switch (category)
        {
            case ItemCategory.Weapon: return weapons;
            case ItemCategory.Apparel: return apparel;
            case ItemCategory.Ammunition: return ammunition;
            case ItemCategory.Drugs: return drugs;
            case ItemCategory.Medicine: return medicine;
            case ItemCategory.Food: return food;
            case ItemCategory.Other: return other;
            default: return null;
        }
    }
}
