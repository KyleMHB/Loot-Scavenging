namespace LootScavenging.Model;

/// <summary>What out-of-reach gear becomes (spec, "Loot rules", drop mode table).</summary>
public enum DropMode
{
    /// <summary>Gear within reach follows its category rule; gear beyond it becomes scrap. The default.</summary>
    ScrapOutOfReach,

    /// <summary>All gear enemies drop becomes scrap.</summary>
    ScrapEverything,

    /// <summary>Out-of-reach gear drops whole, labelled unfamiliar and unequippable until researched (ticket 06).</summary>
    KeepOriginal,

    /// <summary>No era handling: out-of-reach gear follows its category rule like any other gear.</summary>
    Off,
}
