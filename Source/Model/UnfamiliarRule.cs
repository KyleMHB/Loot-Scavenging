namespace LootScavenging.Model;

/// <summary>Which gear gets the unfamiliar label while a Who row marks unfamiliar gear (spec, "Original-item mode").</summary>
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
    /// <param name="lootRowsMark">A loot Who row (hostile, non-hostile, wild or prisoners) is set to Mark.</param>
    /// <param name="outOfReachNow">The item's own research is unfinished, or its era is above the colony's (no carrier).</param>
    /// <param name="markedEra">The era saved on the item when it was looted, or Undefined when it carries no marker.</param>
    /// <param name="colonyEra">The colony's current era.</param>
    public static bool IsUnfamiliar(bool lootRowsMark, UnfamiliarScope scope, bool outOfReachNow, Era markedEra, Era colonyEra)
    {
        if (!lootRowsMark)
            return false;
        if (scope == UnfamiliarScope.AllOutOfReach)
            return outOfReachNow;

        if (markedEra == Era.Undefined)
            return false;
        // The marker keeps the era the carrier gave the item, so gear whose era came only from its carrier still
        // clears once the colony catches up.
        return outOfReachNow || markedEra > colonyEra;
    }

    /// <summary>
    /// Whether a join mark (ticket 20) still holds. It does whatever the loot rows and scope say: the mark is the player's
    /// choice for that item, not a consequence of the loot rows. Like the looted marker, it keeps the era the item had when its owner
    /// joined, so gear whose era came only from its carrier clears once the colony catches up.
    /// </summary>
    public static bool JoinMarkHolds(Era joinedEra, bool outOfReachNow, Era colonyEra)
    {
        if (joinedEra == Era.Undefined)
            return false;
        return outOfReachNow || joinedEra > colonyEra;
    }

    /// <summary>Whether a worn or held item gets the join mark: only gear that is out of reach as its owner joins, and only under a joiner row set to Mark.</summary>
    public static bool ShouldMarkJoining(WhoRule row, bool outOfReach) => row == WhoRule.Mark && outOfReach;

    /// <summary>
    /// Whether a kept item gets the looted marker: only in looted-only scope, for out-of-reach gear from a pawn outside the
    /// colony whose Who row is set to Mark.
    /// </summary>
    public static bool ShouldMark(WhoRule row, UnfamiliarScope scope, OwnerGroup owner, bool outOfReach)
    {
        return row == WhoRule.Mark && scope == UnfamiliarScope.LootedOnly
            && owner != OwnerGroup.Colony && outOfReach;
    }

    /// <summary>The era to save on the marker; never Undefined, which means "no marker".</summary>
    public static Era MarkerEra(EraResult result)
    {
        return result.Era == Era.Undefined ? Era.Animal : result.Era;
    }
}
