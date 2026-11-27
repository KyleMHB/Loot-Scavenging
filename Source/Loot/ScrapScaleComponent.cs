using System.Collections.Generic;
using LootScavenging.Model;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace LootScavenging.Loot;

/// <summary>
/// Converts a save's scrap once to the cheaper units of 0.5.2 (ticket 19), when each unit became worth half as much. A game
/// without this component's data, from before 0.5.2 or started without the mod, loads at the old scale: every stack of raw
/// and studied scrap it holds is doubled, so the scrap keeps its value, and the scale is saved. A new game starts at the
/// current scale. The mod's other saved game data is the list of joiners waiting to be sorted (UnsortedJoiners).
/// </summary>
public class ScrapScaleComponent : GameComponent
{
    private const string Key = "scrapScale"; // not translated

    // The constructor runs for a loaded game that has no saved value too (Game.FillComponents), so the field starts at the old
    // scale and only a new game moves it on without converting.
    private int scrapScale = SettingsLoad.OldScrapScale;

    public ScrapScaleComponent(Game game)
    {
    }

    public override void StartedNewGame()
    {
        scrapScale = SettingsLoad.CurrentScrapScale;
    }

    public override void LoadedGame()
    {
        if (scrapScale >= SettingsLoad.CurrentScrapScale)
            return;
        try
        {
            DoubleAll();
        }
        catch (System.Exception e)
        {
            Log.Error($"[Loot Scavenging] Converting this save's scrap to the 0.5.2 units failed: {e}"); // not translated
        }
        // Set even after a failure, so a partly converted save is never doubled a second time.
        scrapScale = SettingsLoad.CurrentScrapScale;
    }

    public override void ExposeData()
    {
        Scribe_Values.Look(ref scrapScale, Key, SettingsLoad.OldScrapScale, forceSave: true);
    }

    private static void DoubleAll()
    {
        var scrapDefs = new HashSet<ThingDef>();
        foreach (ScrapTierDef def in DefDatabase<ScrapTierDef>.AllDefsListForReading)
        {
            if (def.scrap != null)
                scrapDefs.Add(def.scrap);
            if (def.studiedScrap != null)
                scrapDefs.Add(def.studiedScrap);
        }
        if (scrapDefs.Count == 0)
            return;

        // Collected first, then changed, so a stack split off below is never doubled too. A set, since a thing can be reached
        // from more than one holder.
        var found = new HashSet<Thing>();
        var things = new List<Thing>();
        foreach (Map map in Find.Maps)
        {
            ThingOwnerUtility.GetAllThingsRecursively(map, ThingRequest.ForGroup(ThingRequestGroup.Everything), things);
            AddScrap(things, scrapDefs, found);
        }
        // Caravans, travelling transport pods and other world objects that hold things.
        ThingOwnerUtility.GetAllThingsRecursively(Find.World, things);
        AddScrap(things, scrapDefs, found);
        // World pawns' inventories: pawns away on a quest, kidnapped colonists, and the like.
        foreach (Pawn pawn in Find.WorldPawns.AllPawnsAliveOrDead)
        {
            ThingOwnerUtility.GetAllThingsRecursively(pawn, things);
            AddScrap(things, scrapDefs, found);
        }

        // Every stack is doubled before any overflow is placed: a new stack placed on the ground can merge into a neighbour,
        // which must already be doubled or it would double the merged units too.
        int before = 0, stacks = 0;
        var overflow = new List<(Thing thing, int units)>();
        foreach (Thing thing in found)
        {
            if (thing.Destroyed)
                continue;
            stacks++;
            before += thing.stackCount;
            int doubled = thing.stackCount * 2;
            int kept = System.Math.Max(thing.stackCount, System.Math.Min(doubled, thing.def.stackLimit));
            thing.stackCount = kept;
            if (doubled > kept)
                overflow.Add((thing, doubled - kept));
        }
        foreach ((Thing thing, int units) in overflow)
            PlaceOverflow(thing, units);
        if (Prefs.DevMode)
            Log.Message($"[Loot Scavenging] Scrap made cheaper in 0.5.2: doubled {stacks} stacks of scrap, {before} units to {before * 2}"); // not translated
    }

    private static void AddScrap(List<Thing> things, HashSet<ThingDef> scrapDefs, HashSet<Thing> found)
    {
        foreach (Thing thing in things)
        {
            if (thing != null && scrapDefs.Contains(thing.def))
                found.Add(thing);
        }
        things.Clear();
    }

    // The units a doubled stack could not hold go into new stacks beside it: on the ground next to it, or in the same container.
    // Where neither takes them, they go back onto the stack, over the limit, rather than be lost.
    private static void PlaceOverflow(Thing thing, int units)
    {
        int left = units;
        foreach (int count in ScrapAmounts.SplitIntoStacks(units, System.Math.Max(1, thing.def.stackLimit)))
        {
            Thing extra = ThingMaker.MakeThing(thing.def);
            extra.stackCount = count;
            if (TryPlaceBeside(thing, extra))
                left -= count;
            else
                extra.Destroy();
        }
        if (left > 0)
            thing.stackCount += left;
    }

    private static bool TryPlaceBeside(Thing thing, Thing extra)
    {
        if (thing.Spawned && GenPlace.TryPlaceThing(extra, thing.Position, thing.Map, ThingPlaceMode.Near))
            return true;
        if (thing.holdingOwner != null && thing.holdingOwner.TryAdd(extra, canMergeWithExistingStacks: false))
            return true;
        Map map = thing.MapHeld;
        return map != null && GenPlace.TryPlaceThing(extra, thing.PositionHeld, map, ThingPlaceMode.Near);
    }
}
