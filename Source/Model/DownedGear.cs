namespace LootScavenging.Model;

/// <summary>What happens to the weapon and pack of a non-colony pawn as he is downed (tickets 21 and 23).</summary>
public enum DownedGear
{
    /// <summary>He keeps his weapon in hand and his pack as it is; nothing drops or is judged, also when he is carried and
    /// set down, rescued into a bed or loaded. It is all sorted later, when he is stripped, arrested, joins or dies.</summary>
    KeepWeapon,

    /// <summary>He drops his weapon and pack as in the base game, sorted by the drop mode as they drop (ticket 24).</summary>
    DropWeapon,
}

public static class DownedGears
{
    /// <summary>
    /// Whether a downing leaves this pawn's weapon in his hands, unjudged, and the game's drop as he is spawned again while
    /// down is skipped. Only pawns the rules cover (owner group and place): anyone else drops as in the base game.
    /// Colonists have their own setting, "Colonists keep their gear".
    /// </summary>
    public static bool KeepsWeapon(LootRules rules, OwnerGroup owner, MapKind map) =>
        rules.downedGear == DownedGear.KeepWeapon && LootEvaluator.CoversPawn(owner, map, rules);
}
