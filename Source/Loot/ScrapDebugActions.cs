using System;
using LootScavenging.Model;
using LudeonTK;
using Verse;

namespace LootScavenging.Loot;

// Dev-mode switches for checking scrap in game before the settings page exists (ticket 11). Not written to the settings file.
internal static class ScrapDebugActions
{
    private const string Category = "Loot Scavenging"; // not translated

    [DebugAction(Category, "Scrap: cycle drop mode", allowedGameStates = AllowedGameStates.Playing)] // not translated
    private static void CycleDropMode()
    {
        LootRules rules = LootScavengingMod.Settings.rules;
        var modes = (DropMode[])Enum.GetValues(typeof(DropMode));
        rules.dropMode = modes[((int)rules.dropMode + 1) % modes.Length];
        Log.Message($"[Loot Scavenging] drop mode = {rules.dropMode}"); // not translated
    }

    [DebugAction(Category, "Scrap: toggle on ground or on body", allowedGameStates = AllowedGameStates.Playing)] // not translated
    private static void ToggleScrapOnGround()
    {
        LootScavengingSettings s = LootScavengingMod.Settings;
        s.scrapOnGround = !s.scrapOnGround;
        Log.Message($"[Loot Scavenging] scrapOnGround = {s.scrapOnGround}"); // not translated
    }

    [DebugAction(Category, "Scrap: cycle scrap market value", allowedGameStates = AllowedGameStates.Playing)] // not translated
    private static void CycleScrapValue()
    {
        LootScavengingSettings s = LootScavengingMod.Settings;
        s.scrapMarketValue = s.scrapMarketValue < 0.5f ? 1f : LootScavengingSettings.DefaultScrapMarketValue;
        ScrapDefs.ApplyMarketValue();
        Log.Message($"[Loot Scavenging] scrap market value = {s.scrapMarketValue}"); // not translated
    }
}
