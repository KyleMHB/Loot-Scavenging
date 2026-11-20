using LootScavenging.Model;
using LudeonTK;
using Verse;

namespace LootScavenging.Loot;

// Dev-mode switches for checking the rules in game before the settings page exists (ticket 11). Changes last until
// the game closes; they are not written to the settings file.
internal static class LootDebugActions
{
    private const string Category = "Loot Scavenging"; // not translated

    private static LootRules Rules => LootScavengingMod.Settings.rules;

    [DebugAction(Category, "Log rules", allowedGameStates = AllowedGameStates.Playing)] // not translated
    private static void LogRules()
    {
        LootRules r = Rules;
        Log.Message($"[Loot Scavenging] hostile={r.hostile} nonHostileFaction={r.nonHostileFaction} noFaction={r.noFaction} prisoners={r.prisoners} homeMapOnly={r.homeMapOnly} onDownedAndStrip={r.onDownedAndStrip} weapons={r.weapons.outcome} apparel={r.apparel.outcome}"); // not translated
    }

    [DebugAction(Category, "Toggle no-faction group", allowedGameStates = AllowedGameStates.Playing)] // not translated
    private static void ToggleNoFaction()
    {
        Rules.noFaction = !Rules.noFaction;
        LogRules();
    }

    [DebugAction(Category, "Toggle prisoner group", allowedGameStates = AllowedGameStates.Playing)] // not translated
    private static void TogglePrisoners()
    {
        Rules.prisoners = !Rules.prisoners;
        LogRules();
    }

    [DebugAction(Category, "Toggle downed and strip", allowedGameStates = AllowedGameStates.Playing)] // not translated
    private static void ToggleDownedAndStrip()
    {
        Rules.onDownedAndStrip = !Rules.onDownedAndStrip;
        LogRules();
    }

    [DebugAction(Category, "Toggle home map only", allowedGameStates = AllowedGameStates.Playing)] // not translated
    private static void ToggleHomeMapOnly()
    {
        Rules.homeMapOnly = !Rules.homeMapOnly;
        LogRules();
    }
}
