using LootScavenging.Model;
using RimWorld;
using Verse;

namespace LootScavenging;

/// <summary>
/// Added to every weapon and apparel def at startup (see <see cref="Unfamiliar"/>). It carries the unfamiliar label and
/// inspect line, and the looted marker for the looted-only scope and the join mark for gear its wearer joined in. Each is saved only when set, so the default scope
/// writes nothing to a save.
/// </summary>
public class CompProperties_Unfamiliar : CompProperties
{
    public CompProperties_Unfamiliar()
    {
        compClass = typeof(CompUnfamiliar);
    }
}

public class CompUnfamiliar : ThingComp
{
    /// <summary>The era the item had when it was looted; Undefined means the item carries no marker.</summary>
    public Era lootedEra = Era.Undefined;

    /// <summary>
    /// The era the item had when its wearer joined the colony wearing it (ticket 20); Undefined means no join mark. Unlike
    /// the looted marker it makes the item unfamiliar in every drop mode.
    /// </summary>
    public Era joinedEra = Era.Undefined;

    /// <summary>The era either marker saved, for the scrap an unfamiliar item counts as.</summary>
    public Era MarkedEra => lootedEra != Era.Undefined ? lootedEra : joinedEra;

    public override void PostExposeData()
    {
        int saved = (int)lootedEra;
        Scribe_Values.Look(ref saved, "lootScavengingLootedEra", 0);
        lootedEra = (Era)saved;

        int joined = (int)joinedEra;
        Scribe_Values.Look(ref joined, "lootScavengingJoinedEra", 0);
        joinedEra = (Era)joined;
    }

    // A split piece is a new thing, so it needs the mark copied; marked and unmarked stacks must not merge back.
    public override void PostSplitOff(Thing piece)
    {
        CompUnfamiliar other = piece.TryGetComp<CompUnfamiliar>();
        if (other == null)
            return;
        other.lootedEra = lootedEra;
        other.joinedEra = joinedEra;
    }

    public override bool AllowStackWith(Thing other)
    {
        CompUnfamiliar comp = other.TryGetComp<CompUnfamiliar>();
        return comp == null || (comp.lootedEra == lootedEra && comp.joinedEra == joinedEra);
    }

    public override string TransformLabel(string label)
    {
        return Unfamiliar.IsUnfamiliar(parent) ? "LootScavenging_UnfamiliarLabel".Translate(label).Resolve() : label;
    }

    public override string CompInspectStringExtra()
    {
        if (!Unfamiliar.IsUnfamiliar(parent))
            return null;
        // Gear with no research to finish (psychic lances) waits on the colony's tech level instead.
        string key = EraReader.HasResearchPrerequisite(parent.def) ? "LootScavenging_UnfamiliarInspect" : "LootScavenging_UnfamiliarInspectNoResearch";
        return key.Translate().Resolve();
    }
}
