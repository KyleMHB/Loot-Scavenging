namespace LootScavenging.Model;

/// <summary>When the gear leaves the pawn (spec, "Loot rules", step 3).</summary>
public enum Moment
{
    Death,
    Downed,
    Strip,
}

/// <summary>Where the pawn is (spec, "Loot rules", step 2).</summary>
public enum MapKind
{
    /// <summary>A player home map.</summary>
    Home,

    /// <summary>Any other map: caravan encounter, quest site, outpost, pocket map.</summary>
    Away,

    /// <summary>No map at all (world pawn, caravan on the world map). Nothing is dropped there, so nothing is sorted.</summary>
    None,
}

/// <summary>The category rule an item is matched to (spec, "Loot rules", step 5).</summary>
public enum ItemCategory
{
    Weapon,
    Apparel,

    /// <summary>Inventory with no category rule yet. Ticket 09 splits it into the inventory groups.</summary>
    Other,
}

/// <summary>Mirrors the game's QualityCategory byte values, so the glue can cast between them.</summary>
public enum ItemQuality : byte
{
    Awful,
    Poor,
    Normal,
    Good,
    Excellent,
    Masterwork,
    Legendary,
}

/// <summary>
/// A plain description of one item leaving one pawn, built by the game glue and judged by <see cref="LootEvaluator"/>.
/// Later rules add their facts here as new fields, so existing callers keep compiling.
/// </summary>
public sealed class DropCandidate
{
    public OwnerGroup Owner;
    public Moment Moment;
    public MapKind Map;
    public ItemCategory Category;

    /// <summary>Null when the item has no quality.</summary>
    public ItemQuality? Quality;

    /// <summary>Hit points over maximum, 0 to 1. Null when the item does not use hit points.</summary>
    public float? HitPointFraction;
}
