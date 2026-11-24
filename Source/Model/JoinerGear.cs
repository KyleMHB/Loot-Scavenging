namespace LootScavenging.Model;

/// <summary>What happens to the worn apparel and held equipment of a pawn as he joins the colony (ticket 20).</summary>
public enum JoinerGear
{
    /// <summary>He keeps wearing and holding out-of-reach gear. It is marked, so once it is off no colonist can use it until its research is done. Works in every drop mode.</summary>
    MarkUnfamiliar,

    /// <summary>His gear is sorted like gear taken from a stripped pawn, by the drop mode and the item rules.</summary>
    FollowDropMode,

    /// <summary>His gear is left alone.</summary>
    LeaveAlone,
}

public static class JoinerGears
{
    /// <summary>The setting that applies to a joiner: one for recruited or enslaved prisoners, one for everyone else.</summary>
    public static JoinerGear For(LootRules rules, bool wasPrisoner) => wasPrisoner ? rules.prisonerJoinGear : rules.otherJoinGear;
}
