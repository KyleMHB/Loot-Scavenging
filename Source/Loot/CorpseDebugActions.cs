using LudeonTK;
using Verse;

namespace LootScavenging.Loot;

// Dev-mode switches for the corpse rule, same lifetime as the loot ones: until the game closes.
internal static class CorpseDebugActions
{
    private const string Category = "Loot Scavenging"; // not translated

    private static LootScavenging.Model.CorpseRules Corpses => LootScavengingMod.Settings.values.corpses;

    [DebugAction(Category, "Log corpse rules", allowedGameStates = AllowedGameStates.Playing)] // not translated
    private static void LogCorpseRules()
    {
        LootScavenging.Model.CorpseRules c = Corpses;
        Log.Message($"[Loot Scavenging] corpses: humanlike={c.humanlike} animal={c.animal} mechanoid={c.mechanoid}"); // not translated
    }

    [DebugAction(Category, "Toggle corpse removal: humanlike", allowedGameStates = AllowedGameStates.Playing)] // not translated
    private static void ToggleHumanlike()
    {
        Corpses.humanlike = !Corpses.humanlike;
        LogCorpseRules();
    }

    [DebugAction(Category, "Toggle corpse removal: animal", allowedGameStates = AllowedGameStates.Playing)] // not translated
    private static void ToggleAnimal()
    {
        Corpses.animal = !Corpses.animal;
        LogCorpseRules();
    }

    [DebugAction(Category, "Toggle corpse removal: mechanoid", allowedGameStates = AllowedGameStates.Playing)] // not translated
    private static void ToggleMechanoid()
    {
        Corpses.mechanoid = !Corpses.mechanoid;
        LogCorpseRules();
    }
}
