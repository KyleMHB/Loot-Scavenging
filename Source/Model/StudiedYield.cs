using System;
using System.Collections.Generic;

namespace LootScavenging.Model;

/// <summary>
/// Studied scrap (spec, "Recycle or study": study first, then recycle the remains at reduced value) recycles at its tier's
/// yield times the tier table's studied-yield factor.
/// </summary>
public static class StudiedYield
{
    /// <summary>
    /// Scales a tier's per-unit yield lines by <paramref name="factor"/> (capped at 1), keeping the expected amount exact:
    /// a certain line's whole part stays certain and its fraction becomes a chance of one more; a chance line's chance is
    /// scaled. A zero or invalid factor yields nothing.
    /// </summary>
    public static List<YieldEntry<T>> Reduce<T>(IEnumerable<YieldEntry<T>> entries, float factor)
    {
        var reduced = new List<YieldEntry<T>>();
        if (float.IsNaN(factor) || factor <= 0f)
            return reduced;
        float f = Math.Min(1f, factor);
        foreach (YieldEntry<T> e in entries)
        {
            if (e.Count <= 0 || !(e.Chance > 0f))
                continue;
            if (e.IsFixed)
            {
                float expected = e.Count * f;
                int whole = (int)Math.Floor(expected);
                float fraction = expected - whole;
                if (whole > 0)
                    reduced.Add(new YieldEntry<T>(e.Item, whole, 1f));
                if (fraction > 1e-4f)
                    reduced.Add(new YieldEntry<T>(e.Item, 1, fraction));
            }
            else
            {
                reduced.Add(new YieldEntry<T>(e.Item, e.Count, e.Chance * f));
            }
        }
        return reduced;
    }
}
