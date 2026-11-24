namespace LootScavenging.Model;

/// <summary>What happens to the weapon and pack of a non-colony pawn as he is downed (tickets 21 and 23).</summary>
public enum DownedGear
{
    /// <summary>He keeps his weapon in hand and his pack as it is; nothing drops or is judged. It is all sorted later, when
    /// he is stripped, arrested, joins or dies, or when the game drops the weapon as he is put down outside a bed.</summary>
    KeepWeapon,

    /// <summary>He drops his weapon and pack as in the base game, sorted as they drop when "Also when stripped or arrested" is on.</summary>
    DropWeapon,
}

public static class DownedGears
{
    /// <summary>
    /// Whether a downing leaves this pawn's weapon in his hands, unjudged. Only pawns the rules cover (owner group and place):
    /// anyone else drops as in the base game. It does not depend on "Also when stripped or arrested", since nothing is judged
    /// at the downing.
    /// </summary>
    public static bool KeepsWeapon(LootRules rules, OwnerGroup owner, MapKind map) =>
        rules.downedGear == DownedGear.KeepWeapon && LootEvaluator.CoversPawn(owner, map, rules);

    /// <summary>
    /// Whether a downed pawn still holds what the game would have dropped at his downing: under "Keep his weapon", or under
    /// "everything stays on the body", where the gear the rules kept stays on him. The game drops a downed pawn's weapon when
    /// he is spawned again outside a bed; this decides whether that drop is this mod's to handle.
    /// </summary>
    public static bool KeepsGear(LootRules rules, DeathDrop deathDrop, OwnerGroup owner, MapKind map) =>
        (rules.downedGear == DownedGear.KeepWeapon || deathDrop == DeathDrop.AllOnBody) && LootEvaluator.CoversPawn(owner, map, rules);
}
