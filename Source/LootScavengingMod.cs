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
    }

    public override string SettingsCategory() => Content.Name;

    public override void DoSettingsWindowContents(Rect inRect) => page.Draw(inRect, Settings);

    public override void WriteSettings()
    {
        base.WriteSettings();
        ApplySettingsToDefs();
    }

    /// <summary>Copies the settings that defs carry onto them: scrap market value and the study bills' points.</summary>
    public static void ApplySettingsToDefs()
    {
        ScrapDefs.ApplyMarketValue();
        StudyRecipes.ApplyPoints();
    }
}
