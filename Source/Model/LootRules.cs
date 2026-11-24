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

    // Moment (step 3, ticket 24). Death, downing and joining are always sorted; these two extend the rules to an arrest (or a
    // trade), where the weapon and pack drop, and to a strip.
    public bool sortWhenArrested = true;
    public bool sortWhenStripped = true;

    // The single toggle both replaced, as a pack's patch of the settings defaults Def written before 0.5.2 may still set it.
    // Not public, so it is neither saved, copied nor compared; ApplyOldDownedAndStrip applies it once the Def is read.
    // Settings files and locks naming it are converted in SettingsLoad and SettingLocks.
    internal string onDownedAndStrip;

    // Downing (tickets 21 and 23): by default the pawn keeps his weapon in hand and nothing is judged until a later moment.
    public DownedGear downedGear = DownedGear.KeepWeapon;

    // The gear of pawns who join the colony (ticket 20): recruited or enslaved prisoners, and everyone else who joins.
    public JoinerGear prisonerJoinGear = JoinerGear.FollowDropMode;
    public JoinerGear otherJoinGear = JoinerGear.MarkUnfamiliar;

    // What out-of-reach gear becomes. Ticket 03 adds the evaluator step that reads it.
    public DropMode dropMode = DropMode.ScrapOutOfReach;

    // Medicine is exempt from the drop mode and follows its own category rule unless this is on.
    public bool dropIncludesMedicine;

    // Ammunition is exempt the same way: rounds mostly fit out-of-reach weapons, which are already dealt with, and scrapping
    // a big stack of cheap rounds gave far more scrap than the ammo was worth.
    public bool dropIncludesAmmunition;

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

    /// <summary>An old "Also when stripped or arrested" set to false in the Def turns both of its replacements off.</summary>
    internal void ApplyOldDownedAndStrip()
    {
        if (bool.TryParse(onDownedAndStrip?.Trim(), out bool old) && !old)
        {
            sortWhenArrested = false;
            sortWhenStripped = false;
        }
        onDownedAndStrip = null;
    }

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
    public CategoryRule RuleFor(ItemCategory category) => ItemCategories.Of(category)?.Rule(this);
}
