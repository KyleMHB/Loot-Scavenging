using LootScavenging.Model;
using RimWorld;
using Verse;

namespace LootScavenging;

/// <summary>
/// Added to every weapon and apparel def at startup (see <see cref="Unfamiliar"/>). It carries the unfamiliar label and
/// inspect line, and the looted marker for the looted-only scope. The marker is saved only when set, so the default scope
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

    public override void PostExposeData()
    {
        int saved = (int)lootedEra;
        Scribe_Values.Look(ref saved, "lootScavengingLootedEra", 0);
        lootedEra = (Era)saved;
    }

    public override string TransformLabel(string label)
    {
        return Unfamiliar.IsUnfamiliar(parent) ? "LootScavenging_UnfamiliarLabel".Translate(label).Resolve() : label;
    }

    public override string CompInspectStringExtra()
    {
        return Unfamiliar.IsUnfamiliar(parent) ? "LootScavenging_UnfamiliarInspect".Translate().Resolve() : null;
    }
}
