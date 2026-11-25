using System;
using LootScavenging.Model;
using LudeonTK;
using Verse;

namespace LootScavenging.Loot;

// Dev-mode switches for checking scrap in game without opening the settings page. Not written to the settings file.
internal static class ScrapDebugActions
{
    private const string Category = "Loot Scavenging"; // not translated

    // Switches every era's market value between the default and 1 together.
    [DebugAction(Category, "Scrap: cycle scrap market value", allowedGameStates = AllowedGameStates.Playing)] // not translated
    private static void CycleScrapValue()
    {
        SettingValues values = LootScavengingMod.Settings.values;
        float next = values.ScrapMarketValue(ScrapTier.Neolithic) < 0.5f ? 1f : SettingValues.DefaultScrapMarketValue;
        foreach (ScrapTier tier in (ScrapTier[])Enum.GetValues(typeof(ScrapTier)))
            values.SetScrapMarketValue(tier, next);
        ScrapDefs.ApplyMarketValue();
        Log.Message($"[Loot Scavenging] scrap market value = {next} for every era"); // not translated
    }
}
