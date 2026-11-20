using System.Collections.Generic;
using Verse;

namespace LootScavenging;

/// <summary>
/// Scrap and studied scrap. Recycling bills return everything through the game's "Smelted" special product, which asks the
/// ingredient: here that is one pick from the tier's loot pool per unit.
/// </summary>
public class ScrapThing : ThingWithComps
{
    public override IEnumerable<Thing> SmeltProducts(float efficiency)
    {
        ScrapTierDef tier = ScrapTierTable.ForScrap(def);
        return ScrapTierTable.RollProducts(tier, stackCount, ScrapTierTable.ValueFactor(tier, def));
    }
}
