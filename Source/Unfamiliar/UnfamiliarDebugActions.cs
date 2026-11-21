using LootScavenging.Model;
using LudeonTK;
using Verse;

namespace LootScavenging;

// Dev-mode actions kept beside the settings page as quick switches, so original-item mode can be checked in game.
// Developer-only text, so literals are not translated.
public static class UnfamiliarDebugActions
{
    [DebugAction("Loot Scavenging", "Unfamiliar: toggle keep-original mode", allowedGameStates = AllowedGameStates.Playing)]
    private static void ToggleMode()
    {
        LootScavengingSettings settings = LootScavengingMod.Settings;
        settings.values.rules.dropMode = settings.values.rules.dropMode == DropMode.KeepOriginal ? DropMode.ScrapOutOfReach : DropMode.KeepOriginal;
        settings.Write();
        Unfamiliar.ResetCache();
        Log.Message("[Loot Scavenging] Drop mode is now " + settings.values.rules.dropMode); // not translated
    }

    [DebugAction("Loot Scavenging", "Unfamiliar: cycle scope", allowedGameStates = AllowedGameStates.Playing)]
    private static void CycleScope()
    {
        LootScavengingSettings settings = LootScavengingMod.Settings;
        settings.values.unfamiliarScope = settings.values.unfamiliarScope == UnfamiliarScope.AllOutOfReach ? UnfamiliarScope.LootedOnly : UnfamiliarScope.AllOutOfReach;
        settings.Write();
        Log.Message("[Loot Scavenging] Unfamiliar scope is now " + settings.values.unfamiliarScope); // not translated
    }

    [DebugAction("Loot Scavenging", "Unfamiliar: log selected item", allowedGameStates = AllowedGameStates.Playing)]
    private static void LogSelected()
    {
        Thing thing = Find.Selector.SingleSelectedThing;
        if (thing == null)
        {
            Log.Message("[Loot Scavenging] Select a weapon or apparel item first."); // not translated
            return;
        }

        CompUnfamiliar comp = (thing as ThingWithComps)?.GetComp<CompUnfamiliar>();
        Log.Message("[Loot Scavenging] " + thing + ": unfamiliar " + Unfamiliar.IsUnfamiliar(thing) // not translated
            + ", looted marker " + (comp?.lootedEra ?? Era.Undefined)
            + ", mode " + LootScavengingMod.Settings.values.rules.dropMode + ", scope " + LootScavengingMod.Settings.values.unfamiliarScope);
    }

    [DebugAction("Loot Scavenging", "Unfamiliar: mark selected item as looted", allowedGameStates = AllowedGameStates.Playing)]
    private static void MarkSelected()
    {
        CompUnfamiliar comp = (Find.Selector.SingleSelectedThing as ThingWithComps)?.GetComp<CompUnfamiliar>();
        if (comp == null)
        {
            Log.Message("[Loot Scavenging] Select a weapon or apparel item first."); // not translated
            return;
        }
        comp.lootedEra = UnfamiliarRule.MarkerEra(EraReader.ItemEra(comp.parent.def, null));
        Log.Message("[Loot Scavenging] Marked " + comp.parent + " as looted (era " + comp.lootedEra + ")."); // not translated
    }
}
