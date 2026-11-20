using System.Collections.Generic;
using LootScavenging.Model;
using LudeonTK;
using Verse;

namespace LootScavenging.Loot;

// Dev-mode switches for checking the inventory rules in game before the settings page exists (ticket 11). Changes last
// until the game closes; they are not written to the settings file.
internal static class InventoryDebugActions
{
    private const string Category = "Loot Scavenging"; // not translated

    private static LootRules Rules => LootScavengingMod.Settings.rules;

    [DebugAction(Category, "Inventory groups: toggle delete", allowedGameStates = AllowedGameStates.Playing)] // not translated
    private static void ToggleInventoryDelete()
    {
        Outcome next = Rules.medicine.outcome == Outcome.Delete ? Outcome.Keep : Outcome.Delete;
        foreach (CategoryRule rule in new[] { Rules.ammunition, Rules.drugs, Rules.medicine, Rules.food, Rules.other })
            rule.outcome = next;
        Log.Message($"[Loot Scavenging] Inventory groups now {next}."); // not translated
    }

    [DebugAction(Category, "Keep list: toggle an item", allowedGameStates = AllowedGameStates.Playing)] // not translated
    private static void ToggleKeepList()
    {
        PickItem(Rules.keepList, "keep list"); // not translated
    }

    [DebugAction(Category, "Remove list: toggle an item", allowedGameStates = AllowedGameStates.Playing)] // not translated
    private static void ToggleRemoveList()
    {
        PickItem(Rules.removeList, "remove list"); // not translated
    }

    private static void PickItem(List<string> list, string listName)
    {
        var options = new List<DebugMenuOption>();
        foreach (ThingDef def in DefDatabase<ThingDef>.AllDefs)
        {
            if (def.category != ThingCategory.Item)
                continue;
            ThingDef picked = def;
            options.Add(new DebugMenuOption(def.defName, DebugMenuOptionMode.Action, () =>
            {
                if (!list.Remove(picked.defName))
                    list.Add(picked.defName);
                Log.Message($"[Loot Scavenging] The {listName} is now: {string.Join(", ", list)}"); // not translated
            }));
        }
        Find.WindowStack.Add(new Dialog_DebugOptionListLister(options));
    }
}
