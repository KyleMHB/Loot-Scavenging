using HarmonyLib;
using LootScavenging.Loot;
using LootScavenging.SettingsUI;
using RimWorld;
using UnityEngine;
using Verse;

namespace LootScavenging;

public class LootScavengingMod : Mod
{
    public const string HarmonyId = "kylemhb.lootscavenging";

    public static LootScavengingSettings Settings { get; private set; }

    private readonly SettingsPage page = new SettingsPage();

    public LootScavengingMod(ModContentPack content) : base(content)
    {
        Settings = GetSettings<LootScavengingSettings>();
        new Harmony(HarmonyId).PatchAll(typeof(LootScavengingMod).Assembly);
        // ExecuteWhenFinished runs when the play data has loaded, before the queued interface event creates the window
        // stack, so the message goes in a long event queued behind that one.
        if (!Settings.firstLoadMessageShown)
            LongEventHandler.ExecuteWhenFinished(() => LongEventHandler.QueueLongEvent(ShowFirstLoadMessage, null, false, null));
    }

    public override string SettingsCategory() => Content.Name;

    public override void DoSettingsWindowContents(Rect inRect) => page.Draw(inRect, Settings);

    public override void WriteSettings()
    {
        base.WriteSettings();
        ScrapDefs.ApplyMarketValue();
    }

    // Shown once ever: the flag is written as soon as the message is up, so it does not return on the next launch.
    private void ShowFirstLoadMessage()
    {
        Find.WindowStack.Add(new Dialog_MessageBox(
            "LootScavenging_FirstLoadText".Translate(),
            "LootScavenging_FirstLoadOk".Translate(),
            null,
            "LootScavenging_FirstLoadOpenSettings".Translate(),
            () => Find.WindowStack.Add(new Dialog_ModSettings(this)),
            "LootScavenging_FirstLoadTitle".Translate()));
        Settings.firstLoadMessageShown = true;
        Settings.Write();
    }
}
