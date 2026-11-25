namespace LootScavenging.Model;

// Source/Model holds plain C# with no RimWorld references, so Tests/LootScavenging.Tests can compile it without the game.

/// <summary>What a loot rule does to one dropped item (spec, "Loot rules").</summary>
public enum Outcome
{
    Keep,
    Scrap,
    Disassemble,
    Delete,
}
