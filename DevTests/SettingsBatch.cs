using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using LootScavenging.Model;
using RimWorld;
using RimWorldDevTests;
using Verse;
using static LootScavenging.DevTests.Fixtures;

namespace LootScavenging.DevTests;

// Settings (tickets 09, 11, 12, 13) through the same calls the settings page makes (Presets.Apply, Settings.Apply,
// PresetChanged); the page itself is not drawn. Locks are tested by configuring the presets with the pack's lock names
// directly, since the pack's patch file is not loaded by the game.
public sealed class SettingsBatch : Batch
{
    private const string P = "lootscavenging.settings.";
    private const string PresetsCase = P + "presets-and-changed", Vanilla = P + "vanilla-preset-in-game", FirstLoad = P + "first-load-flag",
        Locks = P + "locked-setting", Lists = P + "keep-and-remove-lists", RoundTrip = P + "missing-def-list-entry", Picker = P + "item-picker-search",
        NoErrors = P + "no-mod-errors";

    public override string Id => "lootscavenging-settings";
    public override int SegmentCount => 1;

    public override IReadOnlyList<PlannedCase> Plan => new[]
    {
        new PlannedCase(PresetsCase, Levels.Mechanical, "each preset applies unchanged; editing a value marks it changed; Reset returns to Progression"),
        new PlannedCase(Vanilla, Levels.Mechanical, "under Vanilla a pirate's out-of-reach revolver drops whole and the corpse stays"),
        new PlannedCase(FirstLoad, Levels.Mechanical, "the first-load flag is set and saved, so the message does not return"),
        new PlannedCase(Locks, Levels.Mechanical, "the pack's lock names are known; a locked setting keeps the pack value through another preset and a saved config"),
        new PlannedCase(Lists, Levels.Mechanical, "a keep-list revolver stays whole out of reach; remove-list silver is deleted"),
        new PlannedCase(RoundTrip, Levels.Mechanical, "a list entry for a missing def saves, loads and is evaluated with no error"),
        new PlannedCase(Picker, Levels.Mechanical, "the item picker's search finds the revolver and skips buildings"),
        new PlannedCase(NoErrors, Levels.Mechanical, "no error naming the mod logged during the batch"),
    };

    public override IEnumerable<Wait> Segment(int index, BatchContext ctx)
    {
        long modErrorsAtStart = CaseErrors.ModMark;
        ModHarness.SaveSettings();
        var area = Kit.Area(ctx, 40, 12);
        yield return Wait.Ticks(1);
        var map = ctx.Map;
        MakeTribal(ctx);
        var sites = new Queue<IntVec3>(Sites(area, 6));
        var revolver = Kit.Def("Gun_Revolver");

        // ---- 11: presets and "(changed)" -----------------------------------------------------------------------------------
        {
            var c = ctx.Begin(PresetsCase);
            foreach (PresetId preset in Enum.GetValues(typeof(PresetId)))
            {
                ModHarness.RestoreSettings();
                Settings.Apply(Presets.Apply(preset, Settings.Capture()));
                Settings.activePreset = preset;
                bool clean = !Settings.PresetChanged;
                Settings.values.corpses.chance = 0.5f;
                bool marked = Settings.PresetChanged;
                c.Step(clean && marked, $"{preset}: applies unchanged, and editing the corpse chance marks it changed");
            }
            ModHarness.RestoreSettings();
            Settings.values.rules.keepList.Add("Gun_Revolver");
            Settings.Apply(Presets.Apply(PresetId.Cleanup, Settings.Capture()));
            c.Step(Settings.values.rules.keepList.Contains("Gun_Revolver"), "applying a preset keeps the player's keep list");
            Settings.Apply(Presets.Defaults());
            Settings.activePreset = PresetId.Progression;
            c.Step(!Settings.PresetChanged && Settings.values.SameAs(Presets.Build(PresetId.Progression)), "Reset returns every value to Progression");
            ModHarness.RestoreSettings();
            c.Conclude();
        }

        // ---- 11: Vanilla in game -------------------------------------------------------------------------------------------
        {
            var c = ctx.Begin(Vanilla);
            Settings.Apply(Presets.Apply(PresetId.Vanilla, Settings.Capture()));
            Settings.activePreset = PresetId.Vanilla;
            var p = Spawn(ctx, Mercenary, Pirates, sites.Dequeue());
            var gun = Arm(p, revolver, QualityCategory.Poor);
            int scrapBefore = ScrapOnMap(map);
            p.Kill(null);
            c.Step(!gun.Destroyed && gun.Spawned && ScrapOnMap(map) == scrapBefore, "a pirate's out-of-reach poor revolver drops whole");
            c.Step(p.Corpse != null && p.Corpse.Spawned, "the corpse stays");
            ModHarness.RestoreSettings();
            c.Conclude();
        }

        // ---- 11: first-load flag -------------------------------------------------------------------------------------------
        {
            var c = ctx.Begin(FirstLoad);
            var file = Directory.GetFiles(GenFilePaths.ConfigFolderPath, "Mod_*_LootScavengingMod.xml").FirstOrDefault();
            string text = file != null ? File.ReadAllText(file) : "";
            c.Observe("settingsFile", file != null ? Path.GetFileName(file) : "none").Observe("flagInMemory", Settings.firstLoadMessageShown);
            c.Step(Settings.firstLoadMessageShown, "the first-load message is marked shown");
            c.Step(text.Contains("<firstLoadMessageShown>True</firstLoadMessageShown>"), "the saved settings file records it, so it is not shown on the next launch");
            c.Conclude();
        }

        // ---- 13: locks ---------------------------------------------------------------------------------------------------
        {
            var c = ctx.Begin(Locks);
            var packFile = Path.Combine(LoadedModManager.GetMod<LootScavengingMod>().Content.RootDir, "packs", "progression", "ProgressionPreset.xml");
            var names = File.Exists(packFile)
                ? XDocument.Load(packFile).Descendants("Operation").Where(o => (string)o.Element("xpath") != null && ((string)o.Element("xpath")).EndsWith("/locked"))
                    .SelectMany(o => o.Descendants("li")).Select(li => li.Value.Trim()).ToList()
                : new List<string>();
            var packLocks = new SettingLocks(names);
            c.Observe("packLocks", string.Join(",", names)).Observe("unknown", string.Join(",", packLocks.Unknown));
            c.Step(names.Count > 0 && packLocks.Unknown.Count == 0, "every setting the pack locks is a lockable setting");

            var original = Presets.Config;
            var locked = new SettingLocks(names.Concat(new[] { SettingNames.DropMode }));
            Presets.Configure(new PresetConfig(original.Progression, locked));
            c.Note("The presets are configured with the pack's locks plus the drop mode (applied directly; the pack's patch is not loaded)");
            Settings.Apply(Presets.Apply(PresetId.Cleanup, Settings.Capture()));
            Settings.activePreset = PresetId.Cleanup;
            c.Observe("dropModeAfterCleanup", Rules.dropMode).Observe("corpsesAfterCleanup", Settings.values.corpses.humanlike);
            c.Step(Rules.dropMode == original.Progression.rules.dropMode && Settings.values.corpses.humanlike,
                "applying Cleanup sets its other rows but leaves the locked drop mode at the pack value");
            Rules.dropMode = DropMode.Off;
            Rules.disassemblyYield = DisassemblyYield.ItemBased;
            Settings.OnDefaultsLoaded();
            c.Observe("dropModeAfterLoad", Rules.dropMode).Observe("yieldAfterLoad", Rules.disassemblyYield);
            c.Step(Rules.dropMode == original.Progression.rules.dropMode && Rules.disassemblyYield == original.Progression.rules.disassemblyYield,
                "a saved config holding other values returns to the pack's values for locked settings when the defaults load");
            ModHarness.RestoreSettings();
            c.Conclude();
        }

        // ---- 09/12: lists in game ------------------------------------------------------------------------------------------
        {
            var c = ctx.Begin(Lists);
            ModHarness.RestoreSettings();
            Rules.keepList.Add("Gun_Revolver");
            Rules.removeList.Add("Silver");
            var p = Spawn(ctx, Mercenary, Pirates, sites.Dequeue());
            var gun = Arm(p, revolver, QualityCategory.Normal);
            p.inventory.innerContainer.TryAdd(Stack(ThingDefOf.Silver, 40));
            int silver = Kit.CountOnMap(map, ThingDefOf.Silver);
            p.Kill(null);
            int lost = silver - Kit.CountOnMap(map, ThingDefOf.Silver);
            c.Observe("silverLost", lost);
            c.Step(!gun.Destroyed && gun.Spawned, "a keep-list revolver stays whole although it is out of reach");
            c.Step(lost == 40, "remove-list silver in the inventory is deleted");
            ModHarness.RestoreSettings();
            c.Conclude();
        }

        // ---- 12: missing def in a list -------------------------------------------------------------------------------------
        {
            var c = ctx.Begin(RoundTrip);
            long mark = CaseErrors.ModMark, all = CaseErrors.Mark;
            const string missing = "LootTest_RemovedModItem";
            var written = new LootScavengingSettings();
            written.values.rules.keepList.Add(missing);
            written.values.rules.removeList.Add("Gun_Revolver");
            LoadedModManager.WriteModSettings("lootscavenging-devtest", "RoundTrip", written);
            var read = LoadedModManager.ReadModSettings<LootScavengingSettings>("lootscavenging-devtest", "RoundTrip");
            foreach (var f in Directory.GetFiles(GenFilePaths.ConfigFolderPath, "Mod_lootscavenging-devtest_*")) File.Delete(f);
            c.Observe("keepListRead", string.Join(",", read.values.rules.keepList));
            c.Step(read.values.rules.keepList.Contains(missing) && read.values.rules.removeList.Contains("Gun_Revolver"), "the missing def name saves and loads as written");

            ModHarness.RestoreSettings();
            Rules.keepList.Add(missing);
            var p = Spawn(ctx, Mercenary, Pirates, sites.Dequeue());
            var gun = Arm(p, revolver, QualityCategory.Normal);
            p.Kill(null);
            c.Step(gun.Destroyed, "with only the missing entry on the keep list, a pirate's revolver is still scrapped");
            c.Observe("errors", CaseErrors.Mark - all);
            c.Step(CaseErrors.ModMark == mark && CaseErrors.Mark == all, "no error is logged");
            ModHarness.RestoreSettings();
            c.Conclude();
        }

        // ---- 12: picker search ---------------------------------------------------------------------------------------------
        {
            var c = ctx.Begin(Picker);
            var picker = typeof(LootScavengingMod).Assembly.GetType("LootScavenging.SettingsUI.Dialog_ItemPicker");
            var allItems = picker?.GetMethod("AllItems", BindingFlags.NonPublic | BindingFlags.Static);
            if (allItems == null) c.SetupFailed("Dialog_ItemPicker.AllItems not found");
            else
            {
                var items = (List<ItemSearchEntry>)allItems.Invoke(null, null);
                var found = ItemSearch.Filter(items, "revolver");
                c.Observe("items", items.Count).Observe("revolverResults", string.Join(",", found.Take(5).Select(e => e.DefName)));
                c.Step(found.Any(e => e.DefName == "Gun_Revolver"), "searching \"revolver\" finds the revolver");
                c.Step(!items.Any(e => e.DefName == "TableMachining") && items.Any(e => e.DefName == "Silver"), "the picker lists loot (silver) but no buildings");
                c.Conclude();
            }
        }

        {
            var c = ctx.Begin(NoErrors);
            c.Observe("modErrors", CaseErrors.ModMark - modErrorsAtStart);
            c.Step(CaseErrors.ModMark == modErrorsAtStart, "no error naming Loot Scavenging was logged during the batch");
            c.Conclude();
        }
    }
}
