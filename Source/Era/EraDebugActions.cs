using System.Collections.Generic;
using LootScavenging.Model;
using LudeonTK;
using RimWorld;
using Verse;

namespace LootScavenging;

// Dev-mode readout for the era reader. Developer-only text, so literals are not translated.
public static class EraDebugActions
{
    [DebugAction("Loot Scavenging", "Era: colony era and fallback", allowedGameStates = AllowedGameStates.Playing)]
    private static void LogColonyEra()
    {
        // not translated
        Log.Message("[Loot Scavenging] Colony era: " + EraReader.ColonyEra() + ", fallback setting: "
            + (LootScavengingMod.Settings?.eraFallback ?? EraFallback.CarrierFaction));
    }

    // Stands in for the settings page (ticket 11) so the fallback's effect on the readout can be checked.
    [DebugAction("Loot Scavenging", "Era: cycle fallback setting", allowedGameStates = AllowedGameStates.Playing)]
    private static void CycleFallback()
    {
        LootScavengingSettings settings = LootScavengingMod.Settings;
        settings.eraFallback = (EraFallback)(((int)settings.eraFallback + 1) % 3);
        settings.Write();
        Log.Message("[Loot Scavenging] Era fallback is now " + settings.eraFallback); // not translated
    }

    [DebugAction("Loot Scavenging", "Era: readout for an item...", allowedGameStates = AllowedGameStates.Playing)]
    private static void ReadoutForItem()
    {
        var options = new List<DebugMenuOption>();
        foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
        {
            if (def.IsWeapon || def.IsApparel)
            {
                ThingDef captured = def;
                options.Add(new DebugMenuOption(def.defName, DebugMenuOptionMode.Action, () => LogReadout(captured)));
            }
        }
        options.Sort((a, b) => string.CompareOrdinal(a.label, b.label));
        Find.WindowStack.Add(new Dialog_DebugOptionListLister(options));
    }

    private static void LogReadout(ThingDef def)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("[Loot Scavenging] Era readout for " + def.defName + " (colony era " + EraReader.ColonyEra() // not translated
            + ", item tech level " + def.techLevel + ")");
        AppendLine(sb, "no carrier", EraReader.ItemEra(def, null));
        foreach (Faction faction in Find.FactionManager.AllFactionsListForReading)
        {
            if (!faction.IsPlayer && faction.def != null && !faction.def.hidden)
            {
                AppendLine(sb, "carried by " + faction.def.defName + " (" + faction.def.techLevel + ")",
                    EraReader.ItemEra(def, faction));
            }
        }
        Log.Message(sb.ToString());
    }

    private static void AppendLine(System.Text.StringBuilder sb, string label, EraResult r)
    {
        sb.AppendLine("  " + label + ": era " + r.Era + " from " + r.Source + ", out of reach: " + r.OutOfReach
            + (r.BreakIntoComponents ? ", break into components" : "")); // not translated
    }
}
