using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using LootScavenging.Model;
using RimWorld;
using Verse;

namespace LootScavenging.Loot;

/// <summary>The scrap ThingDefs, one per ScrapTier in enum order, looked up by name once the defs are loaded.</summary>
[StaticConstructorOnStartup]
internal static class ScrapDefs
{
    private static readonly ThingDef[] byTier = ((ScrapTier[])System.Enum.GetValues(typeof(ScrapTier)))
        .Select(t => DefDatabase<ThingDef>.GetNamed(ScrapTiers.DefName(t))).ToArray();

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

    /// <summary>What one unit of a tier's scrap is worth: the larger of its recycle value and its market value setting.</summary>
    public static float WorthPerUnit(ScrapTier tier)
    {
        return ScrapAmounts.WorthPerUnit(ScrapTierTable.For(tier)?.valuePerUnit ?? 0f, MarketValue(tier));
    }

    private static float MarketValue(ScrapTier tier) =>
        LootScavengingMod.Settings?.values.ScrapMarketValue(tier) ?? SettingValues.DefaultScrapMarketValue;

    /// <summary>
    /// Writes each tier's market value setting onto its scrap def (ticket 25, decision 9). Called again whenever a value may
    /// have changed: once the settings defaults load, when the page, a preset or Reset changes it, and when the settings
    /// window closes.
    /// </summary>
    public static void ApplyMarketValue()
    {
        for (int i = 0; i < byTier.Length; i++)
            byTier[i].SetStatBaseValue(StatDefOf.MarketValue, MarketValue((ScrapTier)i));
    }
}

/// <summary>
/// Turns one item still held by its pawn into scrap of its era, before the game drops it. Scrap goes into the pawn's
/// inventory, so the game drops it with the rest and merges it into full stacks: one pile per tier near each body.
/// Nothing is saved by the mod and no data is added to items.
/// </summary>
internal static class ScrapApplier
{
    public static void Apply(Pawn pawn, Thing thing, EraResult era, Moment moment)
    {
        ScrapTier tier = ScrapTiers.TierFor(era.Era, EraReader.ColonyEra());
        ThingDef scrapDef = ScrapDefs.For(tier);
        int units = ItemClassifier.ScrapUnits(thing, tier);

        OutcomeApplier.Delete(pawn, thing);

        if (units < 1)
        {
            // Worth less than one unit of its scrap: nothing in its place, as with Delete.
            if (Prefs.DevMode)
                Log.Message($"[Loot Scavenging] {thing} is worth less than one {scrapDef.defName}, so it was removed and left no scrap"); // not translated
            return;
        }

        // Under "Don't drop your stuff" a dying pawn's scrap stays on his body with the rest of his gear.
        bool keepOnBody = BodyLoot.KeepsAll(pawn);

        foreach (int count in ScrapAmounts.SplitIntoStacks(units, scrapDef.stackLimit))
        {
            Thing scrap = ThingMaker.MakeThing(scrapDef);
            scrap.stackCount = count;
            if (keepOnBody)
                BodyLoot.Hold(pawn, scrap);
            else
                BodyLoot.Give(pawn, scrap);
        }

        if (Prefs.DevMode)
            Log.Message($"[Loot Scavenging] {thing} became {units} {scrapDef.defName}{(keepOnBody ? " (kept on the body)" : "")}"); // not translated
    }
}
