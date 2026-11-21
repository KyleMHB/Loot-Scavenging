using System;
using System.Collections.Generic;
using LootScavenging.Model;
using RimWorldDevTests;

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
    public override string[] RuntimeFolders => new[] { "About", "1.6", "1.7", "Defs", "Patches", "Languages", "Textures", "packs" };
    public override string[] RuntimeFiles => new[] { "LoadFolders.xml" };

    public override IEnumerable<Batch> CreateBatches() => new Batch[] { new SmokeBatch(), new OutcomesBatch(), new MomentsBatch(), new SettingsBatch(), new BenchBatch(), new SavesBatch() };

    public override IEnumerable<string> RunAll => new[] { "lootscavenging-smoke", "lootscavenging-outcomes", "lootscavenging-moments", "lootscavenging-settings", "lootscavenging-bench", "lootscavenging-saves" };

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

    public override void OnBatchEnd()
    {
        RestoreSettings();
        savedValues = null;
    }
}
