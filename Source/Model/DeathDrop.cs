namespace LootScavenging.Model;

/// <summary>Where a covered pawn's loot lands when it dies (spec, "Scrap": "Where it appears").</summary>
public enum DeathDrop
{
    // The game's own drop: the weapon, inventory and any scrap land beside the body.
    Ground,

    // Scrap stays in the corpse's inventory; the weapon and the rest of the inventory drop as usual.
    ScrapOnBody,

    // Nothing drops: the weapon, inventory and scrap stay in the corpse's inventory until it is stripped or butchered.
    AllOnBody,
}
