using System.Collections.Generic;
using Verse;

namespace LootScavenging;

/// <summary>
/// Scrap. Recycling bills list the certain yield as recipe products so bills can keep a count in stock; the game asks the
/// ingredient for the rest (special product "Smelted"), which here is the chance lines, rolled once per unit.
/// </summary>
public class ScrapThing : ThingWithComps
{
    public override IEnumerable<Thing> SmeltProducts(float efficiency)
    {
        return ScrapTierTable.ChanceProducts(def, stackCount);
    }
}
