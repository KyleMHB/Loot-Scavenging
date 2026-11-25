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

    /// <summary>Scrap, only for items beyond the colony's reach: the default for weapons and apparel (ticket 25).</summary>
    public static CategoryRule ScrapBeyondReach() => new CategoryRule(Outcome.Scrap) { onlyBeyondReach = true };
}

/// <summary>
/// The rule values the evaluator reads. Fields are public so the settings can Scribe them by reference.
/// The initialisers match the Progression preset (a test checks it); the settings themselves start from Presets.Defaults().
/// </summary>
public sealed class LootRules
{
    // Who (step 1, ticket 25). Colony pawns have no row: they are never affected. The two joiner rows decide a pawn's gear as
    // he joins the colony (ticket 20); the prisoner row covers what comes off a prisoner, and an arrest.
    public WhoRule whoHostile = WhoRule.Apply;
    public WhoRule whoNonHostile = WhoRule.Apply;
    public WhoRule whoWild = WhoRule.Apply;
    public WhoRule whoPrisoners = WhoRule.Apply;
    public WhoRule whoRecruitedPrisoners = WhoRule.Apply;
    public WhoRule whoOtherJoiners = WhoRule.Mark;

    // Place (step 2).
    public bool homeMapOnly;

    // Category rules (step 5). Weapons and apparel beyond the colony's reach become scrap; the rest is left as in the game.
    public CategoryRule weapons = CategoryRule.ScrapBeyondReach();
    public CategoryRule apparel = CategoryRule.ScrapBeyondReach();
    public CategoryRule ammunition = new CategoryRule(Outcome.Keep);
    public CategoryRule drugs = new CategoryRule(Outcome.Keep);
    public CategoryRule medicine = new CategoryRule(Outcome.Keep);
    public CategoryRule food = new CategoryRule(Outcome.Keep);
    public CategoryRule other = new CategoryRule(Outcome.Keep);

    // Instant disassembly (ticket 08).
    public DisassemblyYield disassemblyYield = DisassemblyYield.EraTypical;
    public float disassemblyEfficiency = Disassembly.DefaultEfficiency;

    // Protection (step 4). The four toggles default on; the keep list always wins over the remove list.
    public bool protectQuestItems = true;
    public bool protectUniqueWeapons = true;
    public bool protectPersonaWeapons = true;
    public bool protectRelics = true;

    // Lists hold item def names, so a removed mod's entries do nothing and break nothing.
    public List<string> keepList = new List<string>();
    public List<string> removeList = new List<string>();

    // Settings removed in 0.6.0 (ticket 25), as a pack's patch of the settings defaults Def may still set them. Not public, so
    // they are neither saved, copied nor compared; SettingsLoad.MigrateDef translates them once the Def is read. The names are
    // the old nodes', so an old patch still finds its node.
#pragma warning disable CS0649 // filled by the game's Def loader by reflection
    internal string hostile;
    internal string nonHostileFaction;
    internal string noFaction;
    internal string prisoners;
    internal string prisonerJoinGear;
    internal string otherJoinGear;
    internal string dropMode;
    internal string dropIncludesMedicine;
    internal string dropIncludesAmmunition;
    internal string sortWhenArrested;
    internal string sortWhenStripped;
    internal string onDownedAndStrip;
    internal string downedGear;
    internal string degradeMin;
    internal string degradeMax;
    internal string degradeLowersQuality;
    internal string ejectCharges;
#pragma warning restore CS0649

    /// <summary>The Who row for an owner group. Colony pawns have none and are always excluded.</summary>
    public WhoRule RowFor(OwnerGroup group)
    {
        switch (group)
        {
            case OwnerGroup.Hostile: return whoHostile;
            case OwnerGroup.NonHostileFaction: return whoNonHostile;
            case OwnerGroup.NoFaction: return whoWild;
            case OwnerGroup.Prisoner: return whoPrisoners;
            case OwnerGroup.RecruitedPrisoner: return whoRecruitedPrisoners;
            case OwnerGroup.OtherJoiner: return whoOtherJoiners;
            default: return WhoRule.Exclude;
        }
    }

    public void SetRow(OwnerGroup group, WhoRule rule)
    {
        switch (group)
        {
            case OwnerGroup.Hostile: whoHostile = rule; break;
            case OwnerGroup.NonHostileFaction: whoNonHostile = rule; break;
            case OwnerGroup.NoFaction: whoWild = rule; break;
            case OwnerGroup.Prisoner: whoPrisoners = rule; break;
            case OwnerGroup.RecruitedPrisoner: whoRecruitedPrisoners = rule; break;
            case OwnerGroup.OtherJoiner: whoOtherJoiners = rule; break;
        }
    }

    /// <summary>The groups with a Who row, in page order.</summary>
    public static readonly OwnerGroup[] RowGroups =
    {
        OwnerGroup.Hostile, OwnerGroup.NonHostileFaction, OwnerGroup.NoFaction, OwnerGroup.Prisoner,
        OwnerGroup.RecruitedPrisoner, OwnerGroup.OtherJoiner,
    };

    /// <summary>
    /// Whether any row for gear that drops (not the two joiner rows) is set to Mark unfamiliar: then gear kept whole from those
    /// groups can be unfamiliar. Joiner marks are saved on the item and hold either way.
    /// </summary>
    public bool AnyLootRowMarks =>
        whoHostile == WhoRule.Mark || whoNonHostile == WhoRule.Mark || whoWild == WhoRule.Mark || whoPrisoners == WhoRule.Mark;

    public bool IsOnKeepList(string defName) => defName != null && keepList != null && keepList.Contains(defName);

    public bool IsOnRemoveList(string defName) => defName != null && removeList != null && removeList.Contains(defName);

    /// <summary>The rule for a category, or null when the category has none yet.</summary>
    public CategoryRule RuleFor(ItemCategory category) => ItemCategories.Of(category)?.Rule(this);

    /// <summary>Replaces a category's rule.</summary>
    public void SetRule(CategoryInfo category, CategoryRule rule) =>
        typeof(LootRules).GetField(category.Field).SetValue(this, rule);
}
