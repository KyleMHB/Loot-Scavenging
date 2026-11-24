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
    private const string PresetsCase = P + "presets-and-changed", Vanilla = P + "vanilla-preset-in-game",
        Locks = P + "locked-setting", Lists = P + "keep-and-remove-lists", RoundTrip = P + "missing-def-list-entry", Picker = P + "item-picker-search",
        Partial = P + "imported-partial-file", CleanupTrip = P + "cleanup-round-trip", NoErrors = P + "no-mod-errors";

    private const string DevMod = "lootscavenging-devtest"; // not translated

    public override string Id => "lootscavenging-settings";
    public override int SegmentCount => 1;

    public override IReadOnlyList<PlannedCase> Plan => new[]
    {
        new PlannedCase(PresetsCase, Levels.Mechanical, "each preset applies unchanged; editing a value marks it changed; Reset returns to Progression"),
        new PlannedCase(Vanilla, Levels.Mechanical, "under Vanilla a pirate's out-of-reach revolver drops whole and the corpse stays"),
        new PlannedCase(Locks, Levels.Mechanical, "the pack's lock names are known; a locked setting keeps the pack value through another preset and a saved config"),
        new PlannedCase(Lists, Levels.Mechanical, "a keep-list revolver stays whole out of reach; remove-list silver is deleted"),
        new PlannedCase(RoundTrip, Levels.Mechanical, "a list entry for a missing def saves, loads and is evaluated with no error"),
        new PlannedCase(Picker, Levels.Mechanical, "the item picker's search finds the revolver and skips buildings"),
        new PlannedCase(Partial, Levels.Mechanical, "a hand-written partial file with no seeded flag keeps its keys, fills the rest from a patched Def default and ends a locked key at the pack value"),
        new PlannedCase(CleanupTrip, Levels.Mechanical, "a Cleanup config with one key at Progression's value saves that key too, and keeps it when the pack's default changes"),
        new PlannedCase(NoErrors, Levels.Mechanical, "no error naming the mod logged during the batch"),
    };

    // The Def's Progression as a pack might patch it: two defaults that differ from the compiled-in ones.
    private static SettingValues PatchedProgression()
    {
        var v = Presets.Config.Progression.Clone();
        v.scrapMarketValue = 0.5f;
        v.rules.dropMode = DropMode.KeepOriginal;
        return v;
    }

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

        // ---- auto-config 01: a partial file another tool wrote ----------------------------------------------------------
        // The read runs under the compiled-in config, as at startup, and the defaults step under a patched one, so a fill
        // that did nothing would leave the compiled-in values and fail.
        {
            var c = ctx.Begin(Partial);
            var pack = new PresetConfig(PatchedProgression(), new SettingLocks(new[] { SettingNames.DropMode }));

            // The envelope comes from a real write; its body is replaced with what an Auto Mod Config preset could hold.
            LoadedModManager.WriteModSettings(DevMod, "Partial", new LootScavengingSettings());
            var file = Directory.GetFiles(GenFilePaths.ConfigFolderPath, "Mod_" + DevMod + "_Partial*.xml").FirstOrDefault();
            if (file == null) c.SetupFailed("WriteModSettings left no file to edit");
            else
            {
                var doc = XDocument.Load(file);
                var body = doc.Root.Element("ModSettings");
                body.RemoveNodes();
                body.Add(new XElement("activePreset", "Cleanup"), new XElement("corpseChance", "0.25"),
                    new XElement("deathDrop", "AllOnBody"), new XElement("dropMode", "Off"));
                doc.Save(file);
                c.Observe("file", body.ToString(SaveOptions.DisableFormatting));

                Presets.Configure(PresetConfig.Code());
                var read = LoadedModManager.ReadModSettings<LootScavengingSettings>(DevMod, "Partial");
                Presets.Configure(pack);
                read.OnDefaultsLoaded();
                var v = read.values;
                bool seeded = (bool)typeof(LootScavengingSettings).GetField("defaultsSeeded", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(read);
                c.Observe("activePreset", read.activePreset).Observe("corpseChance", v.corpses.chance).Observe("deathDrop", v.deathDrop)
                    .Observe("dropMode", v.rules.dropMode).Observe("scrapMarketValue", v.scrapMarketValue).Observe("defaultsSeeded", seeded);
                c.Step(read.activePreset == PresetId.Cleanup && Math.Abs(v.corpses.chance - 0.25f) < 0.0001f && v.deathDrop == DeathDrop.AllOnBody,
                    "the file's preset and two setting keys are kept although it has no seeded flag");
                c.Step(Math.Abs(v.scrapMarketValue - 0.5f) < 0.0001f, "a key the file lacks takes the patched Def default, not the compiled-in one");
                var expected = Presets.Defaults();
                expected.corpses.chance = 0.25f;
                expected.deathDrop = DeathDrop.AllOnBody;
                c.Step(v.SameAs(expected), "every other key holds the Def's Progression value");
                c.Step(v.rules.dropMode == DropMode.KeepOriginal && Presets.Config.Locks.IsLocked(SettingNames.DropMode),
                    "the locked drop mode the file set ends at the pack's value, and the page's lock set names it");
                c.Step(seeded, "the seeded flag is set after the defaults step");
            }
            foreach (var f in Directory.GetFiles(GenFilePaths.ConfigFolderPath, "Mod_" + DevMod + "_*")) File.Delete(f);
            ModHarness.RestoreSettings();
            c.Conclude();
        }

        // ---- auto-config 01 / 18: Cleanup round trip -----------------------------------------------------------------------
        // The save writes every key, one equal to its default (the Def's Progression) too, so a pack that later changes that
        // default does not move the saved value.
        {
            var c = ctx.Begin(CleanupTrip);
            var pack = new PresetConfig(PatchedProgression(), SettingLocks.None);

            Presets.Configure(pack);
            var written = new LootScavengingSettings();
            written.OnDefaultsLoaded();
            written.Apply(Presets.Apply(PresetId.Cleanup, written.Capture()));
            written.activePreset = PresetId.Cleanup;
            written.values.rules.dropMode = pack.Progression.rules.dropMode; // Cleanup sets Off
            // Ticket 20: one joiner setting equal to its default and one set by Cleanup, both read back as saved.
            written.values.rules.prisonerJoinGear = pack.Progression.rules.prisonerJoinGear;
            written.values.rules.otherJoinGear = JoinerGear.LeaveAlone;
            // Tickets 21 and 23: the downed setting away from its default.
            written.values.rules.downedGear = DownedGear.DropWeapon;
            LoadedModManager.WriteModSettings(DevMod, "Cleanup", written);
            var file = Directory.GetFiles(GenFilePaths.ConfigFolderPath, "Mod_" + DevMod + "_Cleanup*.xml").FirstOrDefault();
            string text = file != null ? File.ReadAllText(file) : "";

            // Read back with the compiled-in config, then the defaults step under a pack whose drop mode default changed.
            Presets.Configure(PresetConfig.Code());
            var read = LoadedModManager.ReadModSettings<LootScavengingSettings>(DevMod, "Cleanup");
            read.OnDefaultsLoaded();
            string savedMode = "<dropMode>" + pack.Progression.rules.dropMode + "</dropMode>";
            c.Observe("dropModeSaved", text.Contains(savedMode)).Observe("dropModeRead", read.values.rules.dropMode)
                .Observe("dropModeDefaultAtRead", Presets.Config.Progression.rules.dropMode)
                .Observe("activePreset", read.activePreset).Observe("corpseHumanlike", read.values.corpses.humanlike);
            c.Step(Presets.Config.Progression.rules.dropMode != pack.Progression.rules.dropMode, "the default drop mode at the read differs from the one at the save");
            c.Step(file != null && text.Contains(savedMode) && text.Contains("<activePreset>Cleanup</activePreset>"),
                "the save writes the drop mode, although it equals its default, and the Cleanup preset");
            c.Step(SettingKeys.All.All(k => text.Contains("<" + k.Key + ">") || text.Contains("<" + k.Key + " />") || text.Contains("<" + k.Key + "/>")),
                "the save holds every setting key");
            c.Step(read.activePreset == PresetId.Cleanup && read.values.SameAs(written.values),
                "read back and through the defaults step, every value matches what was saved, the drop mode included");
            c.Observe("prisonerJoinGearRead", read.values.rules.prisonerJoinGear).Observe("otherJoinGearRead", read.values.rules.otherJoinGear);
            c.Step(text.Contains("<prisonerJoinGear>" + pack.Progression.rules.prisonerJoinGear + "</prisonerJoinGear>")
                && text.Contains("<otherJoinGear>LeaveAlone</otherJoinGear>")
                && read.values.rules.prisonerJoinGear == pack.Progression.rules.prisonerJoinGear && read.values.rules.otherJoinGear == JoinerGear.LeaveAlone,
                "both joiner settings are saved, the one equal to its default too, and read back as saved");
            c.Observe("downedGearRead", read.values.rules.downedGear);
            c.Step(text.Contains("<downedGear>DropWeapon</downedGear>") && read.values.rules.downedGear == DownedGear.DropWeapon,
                "the downed setting is saved as Drop the weapon and read back as saved");
            // Ticket 23: the default's saved name is KeepWeapon (KeepInPack before it, never released).
            written.values.rules.downedGear = DownedGear.KeepWeapon;
            LoadedModManager.WriteModSettings(DevMod, "CleanupKeep", written);
            var keepFile = Directory.GetFiles(GenFilePaths.ConfigFolderPath, "Mod_" + DevMod + "_CleanupKeep*.xml").FirstOrDefault();
            string keepText = keepFile != null ? File.ReadAllText(keepFile) : "";
            var keepRead = LoadedModManager.ReadModSettings<LootScavengingSettings>(DevMod, "CleanupKeep");
            c.Observe("downedGearDefaultRead", keepRead.values.rules.downedGear);
            c.Step(keepText.Contains("<downedGear>KeepWeapon</downedGear>") && keepRead.values.rules.downedGear == DownedGear.KeepWeapon,
                "the default, Keep his weapon, is saved as KeepWeapon and read back as saved");
            foreach (var f in Directory.GetFiles(GenFilePaths.ConfigFolderPath, "Mod_" + DevMod + "_*")) File.Delete(f);
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
