using System;
using System.Collections.Generic;
using LootScavenging.Model;
using RimWorld;
using RimWorldDevTests;
using Verse;

namespace LootScavenging.DevTests;

public sealed class ModHarness : HarnessSettings
{
    public override string DisplayName => "Loot Scavenging";
    public override string ArgPrefix => "loot";
    public override string PackageId => "kylemhb.lootscavenging";
    public override Type ProductionType => typeof(LootScavengingMod);
    public override string IdentityPrefix => "LootTest-";
    public override string EvidenceFolder => "LootScavengingTests";
    public override string ColonyName => "Loot Scavenging Test Colony";
    // Every mod log line starts with this; the display name alone would also match the runner's own "[Loot Scavenging tests]".
    public override string ErrorMarker => "[Loot Scavenging]";
    public override bool FailOnModErrors => true;
    // Keep equal to deploy.ps1's folders and devtests.json's runtimeFolders.
    public override string[] RuntimeFolders => new[] { "About", "1.6", "1.7", "Defs", "Patches", "Languages", "Textures", "packs", "Extras" };
    public override string[] RuntimeFiles => new[] { "LoadFolders.xml" };

    public override IEnumerable<Batch> CreateBatches() => new Batch[] { new SmokeBatch(), new OutcomesBatch(), new MomentsBatch(), new SettingsBatch(), new BenchBatch(), new SavesBatch(), new PreviewBatch(), new PreviewStoryBatch(), new MilestoneBatch(), new WorkshopClipsBatch(), new PromoClipsBatch(), new StacksBatch(), new ProgressionBatch(), new UiBatch(), new FirstLoadBatch() };

    // firstload first: it reads startup from the game's log, which keeps only the newest 1000 messages.
    public override IEnumerable<string> RunAll => new[] { "lootscavenging-firstload", "lootscavenging-smoke", "lootscavenging-outcomes", "lootscavenging-moments", "lootscavenging-settings", "lootscavenging-bench", "lootscavenging-saves", "lootscavenging-ui" };

    // Mod settings live in the process and outlive the colony, so a batch that changes them gets them back here, even when it
    // stops early. Nothing is written to the settings file.
    private static SettingValues savedValues;
    private static PresetId savedPreset;
    private static PresetConfig savedConfig;

    public static void SaveSettings()
    {
        savedValues = LootScavengingMod.Settings.Capture().Clone();
        savedPreset = LootScavengingMod.Settings.activePreset;
        savedConfig = Presets.Config;
    }

    public static void RestoreSettings()
    {
        if (savedValues == null) return;
        Presets.Configure(savedConfig);
        LootScavengingMod.Settings.Apply(savedValues);
        LootScavengingMod.Settings.activePreset = savedPreset;
        Unfamiliar.ResetCache();
    }

    // The player faction's tech level lives on its def, which outlives the colony; the progression batch changes it.
    private static TechLevel? savedPlayerTechLevel;

    public static void SavePlayerTechLevel() => savedPlayerTechLevel = Faction.OfPlayer.def.techLevel;

    public const string HarmonyIdUi = "kylemhb.lootscavenging.devtests.ui";

    public override void OnBatchEnd()
    {
        UiBatch.Unpatch();
        RestoreSettings();
        savedValues = null;
        if (savedPlayerTechLevel.HasValue && Current.Game != null)
            ProgressionBatch.SetPlayerTechLevel(savedPlayerTechLevel.Value);
        savedPlayerTechLevel = null;
    }
}
