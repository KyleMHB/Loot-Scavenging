namespace LootScavenging.Model;

/// <summary>Which gear gets the unfamiliar label in original-item mode (spec, "Original-item mode").</summary>
public enum UnfamiliarScope
{
    /// <summary>All out-of-reach gear from any source. Worked out from research; nothing is saved. The default.</summary>
    AllOutOfReach,

    /// <summary>Only gear looted from pawns. A marker is saved on each looted item.</summary>
    LootedOnly,
}

/// <summary>The decisions behind the unfamiliar label and equip block, as plain facts in and out.</summary>
public static class UnfamiliarRule
{
    /// <param name="outOfReachNow">The item's own research is unfinished, or its era is above the colony's (no carrier).</param>
    /// <param name="markedEra">The era saved on the item when it was looted, or Undefined when it carries no marker.</param>
    /// <param name="colonyEra">The colony's current era.</param>
    public static bool IsUnfamiliar(DropMode mode, UnfamiliarScope scope, bool outOfReachNow, Era markedEra, Era colonyEra)
    {
        if (mode != DropMode.KeepOriginal)
            return false;
        if (scope == UnfamiliarScope.AllOutOfReach)
            return outOfReachNow;

        if (markedEra == Era.Undefined)
            return false;
        // The marker keeps the era the carrier gave the item, so gear whose era came only from its carrier still
        // clears once the colony catches up.
        return outOfReachNow || markedEra > colonyEra;
    }

    /// <summary>Whether a kept item gets the looted marker: only in looted-only scope, for gear from a pawn outside the colony.</summary>
    public static bool ShouldMark(DropMode mode, UnfamiliarScope scope, OwnerGroup owner, bool outOfReach)
    {
        return mode == DropMode.KeepOriginal && scope == UnfamiliarScope.LootedOnly
            && owner != OwnerGroup.Colony && outOfReach;
    }

    /// <summary>The era to save on the marker; never Undefined, which means "no marker".</summary>
    public static Era MarkerEra(EraResult result)
    {
        return result.Era == Era.Undefined ? Era.Animal : result.Era;
    }
}
