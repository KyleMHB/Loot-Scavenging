using System.Collections.Generic;
using LootScavenging.Model;
using RimWorld;
using Verse;

namespace LootScavenging;

/// <summary>
/// Original-item mode (spec, "Original-item mode"): out-of-reach gear that is kept whole is "unfamiliar" until its research
/// finishes. This class answers that question for any item (ticket 07 calls <see cref="IsUnfamiliar"/> at the bench) and
/// sets the looted marker. The decision rule itself is plain C# in Model/UnfamiliarRule.cs.
/// </summary>
[StaticConstructorOnStartup]
public static class Unfamiliar
{
    static Unfamiliar()
    {
        // Every weapon and apparel def gets the comp so the label works without patching label code. A comp with no marker
        // saves nothing, so the default scope leaves saves untouched.
        List<ThingDef> defs = DefDatabase<ThingDef>.AllDefsListForReading;
        for (int i = 0; i < defs.Count; i++)
        {
            ThingDef def = defs[i];
            if (IsGear(def) && typeof(ThingWithComps).IsAssignableFrom(def.thingClass) && def.GetCompProperties<CompProperties_Unfamiliar>() == null)
            {
                def.comps.Add(new CompProperties_Unfamiliar());
            }
        }
    }

    private static bool IsGear(ThingDef def) => EraReader.IsGear(def);

    /// <summary>
    /// True when a loot Who row is set to Mark and this item is out of reach by the active scope, or the item carries a join
    /// mark that still holds. False outside a running game.
    /// A weapon or apparel item only; anything else is never unfamiliar. Protected items (quest items, unique, persona and
    /// relic weapons, the keep list) never are: the drop keeps them whole so the colony can use them.
    /// </summary>
    public static bool IsUnfamiliar(Thing thing)
    {
        LootScavengingSettings settings = LootScavengingMod.Settings;
        if (settings == null || thing == null || Current.ProgramState != ProgramState.Playing)
            return false;

        // Labels ask for every weapon and apparel item each frame, so the common case (no join mark, no loot row set to
        // Mark) answers before any research or protection lookup.
        var comp = (thing as ThingWithComps)?.GetComp<CompUnfamiliar>();
        bool joinMarked = comp != null && comp.joinedEra != Era.Undefined;
        bool keepsOriginals = settings.values.rules.AnyLootRowMarks;
        if (!joinMarked && !keepsOriginals || !IsGear(thing.def) || IsProtected(thing, settings))
            return false;

        Refresh(settings);
        EraResult reach = ReachOf(thing.def);
        // A join mark (ticket 20) holds whatever the loot rows say: the player chose it for gear its wearer joined in.
        if (joinMarked && UnfamiliarRule.JoinMarkHolds(comp.joinedEra, reach.OutOfReach, colonyEra))
            return true;
        if (!keepsOriginals)
            return false;

        Era marked = settings.values.unfamiliarScope == UnfamiliarScope.LootedOnly ? comp?.lootedEra ?? Era.Undefined : Era.Undefined;
        return UnfamiliarRule.IsUnfamiliar(keepsOriginals, settings.values.unfamiliarScope, reach.OutOfReach, marked, colonyEra);
    }

    /// <summary>Whether the pawn is barred from equipping or wearing the item: a colony pawn and unfamiliar gear.</summary>
    public static bool Blocks(Thing thing, Pawn pawn)
    {
        // Raiders and other factions' pawns pick up whatever they like; only the colony is held back.
        return pawn != null && pawn.Faction != null && pawn.Faction.IsPlayer && IsUnfamiliar(thing);
    }

    /// <summary>
    /// Marks the out-of-reach apparel a pawn wears, the equipment he holds and the gear in his pack as he joins the colony ("mark
    /// unfamiliar", tickets 20 and 21; the joiner row set to Mark, ticket 25). Protected items are never marked. <paramref name="carrier"/> is the faction he came from, whose era reads gear with
    /// no research of its own. Locked apparel stays as it is, as at a strip.
    /// </summary>
    public static void MarkJoining(Pawn pawn, Faction carrier)
    {
        LootScavengingSettings settings = LootScavengingMod.Settings;
        if (settings == null)
            return;
        if (pawn.equipment != null)
        {
            foreach (ThingWithComps eq in pawn.equipment.AllEquipmentListForReading)
                MarkJoiningItem(eq, carrier, settings);
        }
        if (pawn.apparel != null)
        {
            foreach (Apparel ap in pawn.apparel.WornApparel)
            {
                if (!pawn.apparel.IsLocked(ap))
                    MarkJoiningItem(ap, carrier, settings);
            }
        }
        // Weapons and apparel in his pack too (ticket 21): a pawn downed under "keep the weapon in his pack" carries his
        // weapon there. Medicine, drugs and the rest are not gear and are left as they are.
        if (pawn.inventory != null)
        {
            foreach (Thing thing in pawn.inventory.innerContainer)
            {
                if (thing is ThingWithComps withComps)
                    MarkJoiningItem(withComps, carrier, settings);
            }
        }
    }

    private static void MarkJoiningItem(ThingWithComps thing, Faction carrier, LootScavengingSettings settings)
    {
        CompUnfamiliar comp = thing.GetComp<CompUnfamiliar>();
        if (comp == null || !IsGear(thing.def) || IsProtected(thing, settings))
            return;
        EraResult reach = EraReader.ItemEra(thing.def, carrier);
        if (UnfamiliarRule.ShouldMarkJoining(WhoRule.Mark, reach.OutOfReach))
            comp.joinedEra = UnfamiliarRule.MarkerEra(reach);
    }

    /// <summary>Called for each item the sorter keeps. Sets the looted marker when the scope calls for it.</summary>
    /// <param name="owner">The group the sorter judged the item under. At arrest or join this differs from the pawn's
    /// current group, so it is passed in rather than looked up again.</param>
    public static void NotifyKept(Pawn pawn, Thing thing, OwnerGroup owner)
    {
        LootScavengingSettings settings = LootScavengingMod.Settings;
        if (settings == null || !settings.values.rules.AnyLootRowMarks || settings.values.unfamiliarScope != UnfamiliarScope.LootedOnly
            || !IsGear(thing.def) || !(thing is ThingWithComps withComps) || IsProtected(thing, settings))
        {
            return;
        }

        CompUnfamiliar comp = withComps.GetComp<CompUnfamiliar>();
        if (comp == null)
            return;

        EraResult reach = EraReader.ItemEra(thing.def, pawn.Faction);
        if (UnfamiliarRule.ShouldMark(settings.values.rules.RowFor(owner), settings.values.unfamiliarScope, owner, reach.OutOfReach))
        {
            comp.lootedEra = UnfamiliarRule.MarkerEra(reach);
        }
    }

    private static bool IsProtected(Thing thing, LootScavengingSettings settings)
    {
        var facts = new DropCandidate();
        Loot.ItemClassifier.DescribeProtection(thing, facts);
        return LootEvaluator.IsProtected(facts, settings.values.rules);
    }

    // Labels ask once per item per frame, so the per-def era result and the colony era are cached for a few seconds and
    // dropped when research finishes or the game changes.
    private static readonly Dictionary<ThingDef, EraResult> reachCache = new Dictionary<ThingDef, EraResult>();
    private static Era colonyEra;
    private static int cacheStamp = -1;
    private static Game cacheGame;

    private static void Refresh(LootScavengingSettings settings)
    {
        int stamp = Find.TickManager.TicksGame / 250;
        if (stamp != cacheStamp || cacheGame != Current.Game)
        {
            reachCache.Clear();
            colonyEra = EraReader.ColonyEra();
            cacheStamp = stamp;
            cacheGame = Current.Game;
        }
    }

    private static EraResult ReachOf(ThingDef def)
    {
        if (!reachCache.TryGetValue(def, out EraResult result))
        {
            // No carrier: unfamiliarity is worked out from research and the item's own tech level, then the era of the
            // factions that typically carry the item (EraReader).

            result = EraReader.ItemEra(def, null);
            reachCache[def] = result;
        }
        return result;
    }

    public static void ResetCache()
    {
        reachCache.Clear();
        cacheStamp = -1;
    }
}
