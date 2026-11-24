namespace LootScavenging.Model;

/// <summary>What happens to the weapon and pack of a non-colony pawn as he is downed (ticket 21).</summary>
public enum DownedGear
{
    /// <summary>His weapon goes into his pack and nothing drops or is judged. It is all sorted later, when he is stripped, arrested, joins or dies.</summary>
    KeepInPack,

    /// <summary>He drops his weapon and pack as in the base game, sorted as they drop when "Also when stripped or arrested" is on.</summary>
    DropWeapon,
}

public static class DownedGears
{
    /// <summary>
    /// Whether a downing puts this pawn's weapon in his pack instead of dropping it. Only pawns the rules cover (owner group
    /// and place): anyone else drops as in the base game. It does not depend on "Also when stripped or arrested", since
    /// nothing is judged at the downing.
    /// </summary>
    public static bool KeepsInPack(LootRules rules, OwnerGroup owner, MapKind map) =>
        rules.downedGear == DownedGear.KeepInPack && LootEvaluator.CoversPawn(owner, map, rules);
}
