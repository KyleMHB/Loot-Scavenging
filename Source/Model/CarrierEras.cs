using System.Collections.Generic;

namespace LootScavenging.Model;

/// <summary>
/// The era of the factions that typically carry an item, for gear met with no carrier (spec, "Out of reach", step 3). The
/// default unfamiliar scope saves nothing, so a looted uncraftable rifle no longer knows who carried it; this stands in for that
/// carrier. Built from each faction's pawn kinds and the weapon and apparel tags they generate gear from. Where several
/// factions carry an item the highest era wins, so gear kept whole because its real carrier was above the colony does not
/// then read as familiar.
/// </summary>
public sealed class CarrierEras
{
    private readonly Dictionary<string, Era> weaponTags = new Dictionary<string, Era>();
    private readonly Dictionary<string, Era> apparelTags = new Dictionary<string, Era>();
    private readonly Dictionary<string, Era> requiredApparel = new Dictionary<string, Era>();

    /// <summary>Records one pawn kind of a faction of <paramref name="era"/>. Kinds of a faction with no era are ignored.</summary>
    public void AddKind(Era era, IEnumerable<string> kindWeaponTags, IEnumerable<string> kindApparelTags,
        IEnumerable<string> requiredApparelDefNames = null)
    {
        if (era == Era.Undefined)
            return;
        Raise(weaponTags, kindWeaponTags, era);
        Raise(apparelTags, kindApparelTags, era);
        Raise(requiredApparel, requiredApparelDefNames, era);
    }

    /// <summary>The highest era among factions that can carry an item with these tags, or Undefined when none can.</summary>
    public Era For(IEnumerable<string> itemWeaponTags, IEnumerable<string> itemApparelTags, string defName = null)
    {
        Era best = Era.Undefined;
        best = Highest(weaponTags, itemWeaponTags, best);
        best = Highest(apparelTags, itemApparelTags, best);
        if (defName != null && requiredApparel.TryGetValue(defName, out Era required) && required > best)
            best = required;
        return best;
    }

    private static void Raise(Dictionary<string, Era> map, IEnumerable<string> keys, Era era)
    {
        if (keys == null)
            return;
        foreach (string key in keys)
        {
            if (key != null && (!map.TryGetValue(key, out Era seen) || era > seen))
                map[key] = era;
        }
    }

    private static Era Highest(Dictionary<string, Era> map, IEnumerable<string> keys, Era best)
    {
        if (keys == null)
            return best;
        foreach (string key in keys)
        {
            if (key != null && map.TryGetValue(key, out Era era) && era > best)
                best = era;
        }
        return best;
    }
}
