using System;
using LootScavenging.Model;
using LudeonTK;
using Verse;

namespace LootScavenging.Loot;

// Dev-mode switches for checking scrap in game without opening the settings page. Not written to the settings file.
internal static class ScrapDebugActions
{
    private const string Category = "Loot Scavenging"; // not translated

    [DebugAction(Category, "Scrap: cycle drop mode", allowedGameStates = AllowedGameStates.Playing)] // not translated
    private static void CycleDropMode()
    {
        LootRules rules = LootScavengingMod.Settings.values.rules;
        var modes = (DropMode[])Enum.GetValues(typeof(DropMode));
        rules.dropMode = modes[((int)rules.dropMode + 1) % modes.Length];
        Log.Message($"[Loot Scavenging] drop mode = {rules.dropMode}"); // not translated
    }

    [DebugAction(Category, "Scrap: cycle where loot lands at death", allowedGameStates = AllowedGameStates.Playing)] // not translated
    private static void CycleDeathDrop()
    {
        LootScavengingSettings s = LootScavengingMod.Settings;
        var modes = (DeathDrop[])Enum.GetValues(typeof(DeathDrop));
        s.values.deathDrop = modes[((int)s.values.deathDrop + 1) % modes.Length];
        Log.Message($"[Loot Scavenging] deathDrop = {s.values.deathDrop}"); // not translated
    }

    [DebugAction(Category, "Scrap: cycle scrap market value", allowedGameStates = AllowedGameStates.Playing)] // not translated
    private static void CycleScrapValue()
    {
        LootScavengingSettings s = LootScavengingMod.Settings;
        s.values.scrapMarketValue = s.values.scrapMarketValue < 0.5f ? 1f : SettingValues.DefaultScrapMarketValue;
        ScrapDefs.ApplyMarketValue();
        Log.Message($"[Loot Scavenging] scrap market value = {s.values.scrapMarketValue}"); // not translated
    }
}
