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

    /// <summary>
    /// Writes the scrap value setting onto the scrap defs. Called again whenever the value may have changed: once the settings
    /// defaults load, when the page's slider, a preset or Reset changes it, and when the settings window closes.
    /// </summary>
    public static void ApplyMarketValue()
    {
        float value = LootScavengingMod.Settings?.values.scrapMarketValue ?? SettingValues.DefaultScrapMarketValue;
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
    public static void Apply(Pawn pawn, Thing thing, EraResult era, Moment moment)
    {
        ScrapTier tier = ScrapTiers.TierFor(era.Era, EraReader.ColonyEra());
        ThingDef scrapDef = ScrapDefs.For(tier);
        int units = ItemClassifier.ScrapUnits(thing);

        OutcomeApplier.Delete(pawn, thing);

        bool keepOnBody = BodyLoot.KeepsAll(pawn)
            || moment == Moment.Death && LootScavengingMod.Settings.values.deathDrop != DeathDrop.Ground && BodyLoot.CanHoldLoot(pawn);
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
