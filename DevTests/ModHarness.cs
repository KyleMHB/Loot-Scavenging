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

    public override IEnumerable<Batch> CreateBatches() => new Batch[] { new SmokeBatch(), new OutcomesBatch(), new MomentsBatch(), new SettingsBatch(), new BenchBatch(), new SavesBatch(), new PreviewBatch(), new PreviewStoryBatch(), new MilestoneBatch(), new WorkshopClipsBatch(), new PromoClipsBatch(), new StacksBatch(), new ProgressionBatch(), new UiBatch(), new StartupBatch() };

    // startup first: it reads startup from the game's log, which keeps only the newest 1000 messages.
    public override IEnumerable<string> RunAll => new[] { "lootscavenging-startup", "lootscavenging-smoke", "lootscavenging-outcomes", "lootscavenging-moments", "lootscavenging-settings", "lootscavenging-bench", "lootscavenging-saves", "lootscavenging-ui" };

    // Mod settings live in the process and outlive the colony, so a batch that changes them gets them back here, even when it
    // stops early. Nothing is written to the settings file.
    private static SettingValues savedValues;
    private static PresetId savedPreset;
    private static PresetConfig savedConfig;
    // A batch's own starting point over the saved values, applied by every restore during the batch but not at its end.
    private static Action<SettingValues> baseline;

    public static void SaveSettings(Action<SettingValues> batchBaseline = null)
    {
        savedValues = LootScavengingMod.Settings.Capture().Clone();
        savedPreset = LootScavengingMod.Settings.activePreset;
        savedConfig = Presets.Config;
        baseline = batchBaseline;
        baseline?.Invoke(LootScavengingMod.Settings.values);
    }

    // The cases written before 0.5.2 count loot on the ground at a death and a downing. The default is now that hostile and
    // other non-colony pawns keep their gear (tickets 24 and 25); the cases about where loot lands set the boxes themselves.
    public static void GearDrops(SettingValues v)
    {
        v.hostilesKeepGear = false;
        v.nonHostilesKeepGear = false;
    }

    /// <summary>The three "Don't drop your stuff" boxes at once.</summary>
    public static void KeepGear(SettingValues v, bool colonists, bool hostiles, bool nonHostiles)
    {
        v.colonistsKeepGear = colonists;
        v.hostilesKeepGear = hostiles;
        v.nonHostilesKeepGear = nonHostiles;
    }

    /// <summary>Sets the four Who rows for gear that drops (hostile, non-hostile, wild, prisoners); the joiner rows stay.</summary>
    public static void LootRows(LootRules rules, WhoRule rule)
    {
        rules.whoHostile = rule;
        rules.whoNonHostile = rule;
        rules.whoWild = rule;
        rules.whoPrisoners = rule;
    }

    /// <summary>Sets every Who row, the joiner rows included.</summary>
    public static void AllRows(LootRules rules, WhoRule rule)
    {
        foreach (OwnerGroup group in LootRules.RowGroups)
            rules.SetRow(group, rule);
    }

    /// <summary>Sets every category to one outcome, with no conditions.</summary>
    public static void AllCategories(LootRules rules, Outcome outcome)
    {
        foreach (CategoryInfo category in ItemCategories.All)
            rules.SetRule(category, new CategoryRule(outcome));
    }

    /// <summary>A category rule for an outcome, optionally only for items beyond the colony's reach.</summary>
    public static CategoryRule Rule(Outcome outcome, bool onlyBeyondReach = false) =>
        new CategoryRule(outcome) { onlyBeyondReach = onlyBeyondReach };

    /// <summary>
    /// The four drop modes of 0.5.2, as 0.6.0's rows and category rules express them, for the cases and clips that show
    /// them side by side. Medicine and ammunition were left out of the old drop modes and keep their own rules.
    /// </summary>
    public enum LootStyle { ScrapOutOfReach, ScrapEverything, KeepOriginal, Off }

    public static void ApplyStyle(LootRules rules, LootStyle style)
    {
        LootRows(rules, style == LootStyle.KeepOriginal ? WhoRule.Mark : WhoRule.Apply);
        foreach (CategoryInfo category in ItemCategories.All)
        {
            bool gear = category.Category == ItemCategory.Weapon || category.Category == ItemCategory.Apparel;
            bool ownRule = category.Category == ItemCategory.Medicine || category.Category == ItemCategory.Ammunition;
            CategoryRule rule = style == LootStyle.ScrapEverything && !ownRule ? new CategoryRule(Outcome.Scrap)
                : style == LootStyle.ScrapOutOfReach && gear ? CategoryRule.ScrapBeyondReach()
                : new CategoryRule(Outcome.Keep);
            rules.SetRule(category, rule);
        }
    }

    public static void RestoreSettings()
    {
        if (savedValues == null) return;
        Presets.Configure(savedConfig);
        LootScavengingMod.Settings.Apply(savedValues);
        baseline?.Invoke(LootScavengingMod.Settings.values);
        LootScavengingMod.Settings.activePreset = savedPreset;
        Unfamiliar.ResetCache();
    }

    // The player faction's tech level lives on its def, which outlives the colony; the progression batch changes it.
    private static TechLevel? savedPlayerTechLevel;

    public static void SavePlayerTechLevel() => savedPlayerTechLevel = Faction.OfPlayer.def.techLevel;

    public const string HarmonyIdUi = "kylemhb.lootscavenging.devtests.ui";
    public const string HarmonyIdMoments = "kylemhb.lootscavenging.devtests.moments";

    public override void OnBatchEnd()
    {
        UiBatch.Unpatch();
        baseline = null;
        RestoreSettings();
        savedValues = null;
        if (savedPlayerTechLevel.HasValue && Current.Game != null)
            ProgressionBatch.SetPlayerTechLevel(savedPlayerTechLevel.Value);
        savedPlayerTechLevel = null;
    }
}
