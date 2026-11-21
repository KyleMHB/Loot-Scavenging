using System.Collections.Generic;
using HarmonyLib;
using LootScavenging.Model;
using RimWorld;
using Verse;

namespace LootScavenging.Loot;

/// <summary>The scrap ThingDefs, one per ScrapTier in enum order, looked up by name once the defs are loaded.</summary>
[StaticConstructorOnStartup]
internal static class ScrapDefs
{
    private static readonly ThingDef[] byTier =
    {
        DefDatabase<ThingDef>.GetNamed("LS_Scrap_Neolithic"),
        DefDatabase<ThingDef>.GetNamed("LS_Scrap_Medieval"),
        DefDatabase<ThingDef>.GetNamed("LS_Scrap_Industrial"),
        DefDatabase<ThingDef>.GetNamed("LS_Scrap_Spacer"),
        DefDatabase<ThingDef>.GetNamed("LS_Scrap_Ultra"),
        DefDatabase<ThingDef>.GetNamed("LS_Scrap_Archotech"),
    };

    static ScrapDefs()
    {
        ApplyMarketValue();
    }

    public static ThingDef For(ScrapTier tier) => byTier[(int)tier];

    public static bool IsScrap(ThingDef def)
    {
        for (int i = 0; i < byTier.Length; i++)
        {
            if (byTier[i] == def)
                return true;
        }
        return false;
    }

    /// <summary>Writes the scrap value setting onto the scrap defs. Called again by the settings page when the value changes.</summary>
    public static void ApplyMarketValue()
    {
        float value = LootScavengingMod.Settings?.scrapMarketValue ?? LootScavengingSettings.DefaultScrapMarketValue;
        for (int i = 0; i < byTier.Length; i++)
            byTier[i].SetStatBaseValue(StatDefOf.MarketValue, value);
    }
}

/// <summary>
/// Turns one item still held by its pawn into scrap of its era, before the game drops it. Scrap goes into the pawn's
/// inventory, so the game drops it with the rest and merges it into full stacks: one pile per tier near each body.
/// Nothing is saved by the mod and no data is added to items.
/// </summary>
internal static class ScrapApplier
{
    // Scrap waiting to be put back on a pawn after its death drop, for the "leave it on the body" setting.
    private static readonly Dictionary<Pawn, List<Thing>> onBody = new Dictionary<Pawn, List<Thing>>();

    public static void Apply(Pawn pawn, Thing thing, EraResult era, Moment moment)
    {
        ScrapTier tier = ScrapTiers.TierFor(era.Era, EraReader.ColonyEra());
        ThingDef scrapDef = ScrapDefs.For(tier);
        int units = ScrapAmounts.Units(thing.MarketValue, thing.stackCount);

        OutcomeApplier.Delete(pawn, thing);

        bool keepOnBody = moment == Moment.Death && !LootScavengingMod.Settings.scrapOnGround;
        foreach (int count in ScrapAmounts.SplitIntoStacks(units, scrapDef.stackLimit))
        {
            Thing scrap = ThingMaker.MakeThing(scrapDef);
            scrap.stackCount = count;
            if (keepOnBody)
                Remember(pawn, scrap);
            else
                Give(pawn, scrap);
        }

        if (Prefs.DevMode)
            Log.Message($"[Loot Scavenging] {thing} became {units} {scrapDef.defName}{(keepOnBody ? " (kept on the body)" : "")}"); // not translated
    }

    private static void Remember(Pawn pawn, Thing scrap)
    {
        if (!onBody.TryGetValue(pawn, out List<Thing> list))
            onBody[pawn] = list = new List<Thing>();
        list.Add(scrap);
    }

    // Called after Pawn.Kill: the death drop has run, so scrap added now stays in the dead pawn's inventory, where stripping,
    // butchering or cremating the corpse finds it. The entry is always used up: if the death was cancelled the pawn simply
    // keeps the scrap in its inventory, as at any other moment.
    public static void PlaceOnBody(Pawn pawn)
    {
        if (!onBody.TryGetValue(pawn, out List<Thing> list))
            return;
        onBody.Remove(pawn);
        foreach (Thing scrap in list)
            Give(pawn, scrap);
    }

    /// <summary>Drops a pawn's set-aside scrap when Kill threw, so the static list does not hold it forever.</summary>
    public static void Forget(Pawn pawn) => onBody.Remove(pawn);

    private static void Give(Pawn pawn, Thing scrap)
    {
        if (pawn.inventory != null && pawn.inventory.innerContainer.TryAdd(scrap, true))
            return;

        // No inventory (some mechanoids): put it on the ground where the pawn or its corpse lies.
        Map map = pawn.MapHeld;
        if (map != null && pawn.PositionHeld.IsValid)
            GenPlace.TryPlaceThing(scrap, pawn.PositionHeld, map, ThingPlaceMode.Near);
        else
            scrap.Destroy();
    }
}

// After the death drop, a dead pawn gets the scrap that was set aside for its body. Pawn.Kill has two postfixes from this
// mod; they share one Harmony id, so only HarmonyPriority orders them. This one runs first: when CorpseRemovalHook then
// removes the corpse, its strip drops the inventory, scrap included, to the ground instead of the scrap vanishing.
[HarmonyPatch(typeof(Pawn), nameof(Pawn.Kill))]
internal static class ScrapOnBodyHook
{
    [HarmonyPriority(Priority.High)]
    private static void Postfix(Pawn __instance)
    {
        try
        {
            ScrapApplier.PlaceOnBody(__instance);
        }
        catch (System.Exception e)
        {
            Log.Error($"[Loot Scavenging] Leaving scrap on {__instance} failed: {e}"); // not translated
        }
    }

    // Finalizers run after every postfix. When Kill (or a patch on it) threw, the postfix may not have used the entry.
    private static void Finalizer(Pawn __instance, System.Exception __exception)
    {
        if (__exception != null)
            ScrapApplier.Forget(__instance);
    }
}
