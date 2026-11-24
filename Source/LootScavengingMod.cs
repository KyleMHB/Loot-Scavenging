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
        PatchEach(new Harmony(HarmonyId));
    }

    // PatchAll stops at the first patch class whose target is gone (a game update renaming a private method), leaving the
    // classes after it unpatched and the mod failing to load. Patched one class at a time, a broken target costs only its
    // own feature. A prefix that holds loot shares its class with the postfix that returns it, so they fail together.
    private static void PatchEach(Harmony harmony)
    {
        foreach (System.Type type in AccessTools.GetTypesFromAssembly(typeof(LootScavengingMod).Assembly))
        {
            // Only patch classes: the processor would otherwise run any static method named Prepare or Cleanup it finds,
            // such as Presets.Cleanup.
            if (!type.IsDefined(typeof(HarmonyPatch), false))
                continue;
            try
            {
                harmony.CreateClassProcessor(type).Patch();
            }
            catch (System.Exception e)
            {
                Log.Error($"[Loot Scavenging] Could not patch {type.FullName}; that part of the mod is off this session: {e}"); // not translated
            }
        }
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
