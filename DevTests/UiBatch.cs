using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using LootScavenging.Model;
using RimWorld;
using RimWorldDevTests;
using UnityEngine;
using Verse;
using static LootScavenging.DevTests.Fixtures;

namespace LootScavenging.DevTests;

// The settings page, the bills tabs and the first-load message drawn for real (tickets 04, 05, 11, 13): each window is opened
// through the window stack or the inspect pane and left to draw for a few frames, while a Widgets.Label postfix records every
// label drawn and where. The cases read that record and the game's state; nothing is clicked. The settings page's preset
// switch is applied the way its button does it, then drawn. A label outside the scroll view's visible area is still drawn
// (Listing_Standard does not cull), so a whole page's labels are read without scrolling.
public sealed class UiBatch : Batch
{
    private const string P = "lootscavenging.ui.";
    private const string Page = P + "settings-page-draws", Switch = P + "settings-after-preset-switch", Locks = P + "settings-locked-rows",
        Bills = P + "bills-tabs", FirstLoad = P + "first-load-message", NoErrors = P + "no-mod-errors";

    public override string Id => "lootscavenging-ui";
    public override int SegmentCount => 1;

    public override IReadOnlyList<PlannedCase> Plan => new[]
    {
        new PlannedCase(Page, Levels.Mechanical, "the mod's settings window opens and draws all six sections, the active preset and the four preset buttons"),
        new PlannedCase(Switch, Levels.Mechanical, "after each preset switch the page still draws all six sections in one column, top to bottom"),
        new PlannedCase(Locks, Levels.Mechanical, "with the pack's locks the page shows the locked summary and reasons, and drawing leaves a locked value at the pack's value"),
        new PlannedCase(Bills, Levels.Mechanical, "the machining table's and research bench's Bills tabs draw their recycle and study bills; the study bench offers only study bills"),
        new PlannedCase(FirstLoad, Levels.Mechanical, "the first-load message opens with its title, text and two buttons, and marks itself shown"),
        new PlannedCase(NoErrors, Levels.Mechanical, "no error naming the mod logged during the batch"),
    };

    private static readonly List<(string text, Rect rect)> drawn = new List<(string, Rect)>();
    private static bool recording;
    private static Harmony harmony;

    private static void RecordLabel(Rect rect, string label)
    {
        if (recording && !string.IsNullOrEmpty(label)) drawn.Add((label, rect));
    }

    private static void RecordTagged(Rect rect, TaggedString label) => RecordLabel(rect, label.RawText);

    private static void Patch()
    {
        harmony = new Harmony(ModHarness.HarmonyIdUi);
        harmony.Patch(AccessTools.Method(typeof(Widgets), nameof(Widgets.Label), new[] { typeof(Rect), typeof(string) }),
            postfix: new HarmonyMethod(typeof(UiBatch), nameof(RecordLabel)));
        harmony.Patch(AccessTools.Method(typeof(Widgets), nameof(Widgets.Label), new[] { typeof(Rect), typeof(TaggedString) }),
            postfix: new HarmonyMethod(typeof(UiBatch), nameof(RecordTagged)));
    }

    public static void Unpatch()
    {
        recording = false;
        harmony?.UnpatchAll(ModHarness.HarmonyIdUi);
        harmony = null;
    }

    // Clears the record, draws a few frames and returns the labels drawn in them.
    private static IEnumerable<Wait> Draw(int frames = 4)
    {
        drawn.Clear();
        recording = true;
        yield return Wait.Frames(frames);
        recording = false;
    }

    private static bool Drew(string text) => drawn.Any(d => d.text == text);
    private static bool DrewPart(string part) => drawn.Any(d => d.text.Contains(part));
    private static IEnumerable<Rect> Where(string text) => drawn.Where(d => d.text == text).Select(d => d.rect);

    private static readonly string[] SectionKeys =
    {
        "LootScavenging_SectionPresets", "LootScavenging_SectionWho", "LootScavenging_SectionItems", "LootScavenging_SectionScrap",
        "LootScavenging_SectionCorpses", "LootScavenging_SectionLists",
    };

    // Each section title's first drawn rect this frame, in page order.
    private static List<(string key, Rect? rect)> Sections() =>
        SectionKeys.Select(k => (k, Where(k.Translate()).Select(r => (Rect?)r).FirstOrDefault())).ToList();

    private static bool OneColumn(List<(string key, Rect? rect)> sections, out string layout)
    {
        layout = string.Join("; ", sections.Select(s => s.rect.HasValue ? $"{s.key.Substring("LootScavenging_Section".Length)} {s.rect.Value.x:0},{s.rect.Value.y:0}" : s.key + " missing"));
        if (sections.Any(s => !s.rect.HasValue)) return false;
        var rects = sections.Select(s => s.rect.Value).ToList();
        bool sameX = rects.All(r => Mathf.Abs(r.x - rects[0].x) < 1f);
        bool downward = rects.Zip(rects.Skip(1), (a, b) => b.y > a.y).All(x => x);
        return sameX && downward;
    }

    public override IEnumerable<Wait> Segment(int index, BatchContext ctx)
    {
        long modErrorsAtStart = CaseErrors.ModMark;
        ModHarness.SaveSettings();
        var area = Kit.Area(ctx, 40, 14);
        yield return Wait.Ticks(1);
        var map = ctx.Map;
        MakeTribal(ctx);
        Kit.Research(ctx, "Smithing");
        Patch();
        var mod = LoadedModManager.GetMod<LootScavengingMod>();
        var applyMarketValue = AccessTools.Method(AccessTools.TypeByName("LootScavenging.ScrapDefs"), "ApplyMarketValue");

        // ---- the settings page draws -------------------------------------------------------------------------------------
        Dialog_ModSettings window = null;
        {
            var c = ctx.Begin(Page);
            Settings.Apply(Presets.Apply(PresetId.Progression, Settings.Capture()));
            Settings.activePreset = PresetId.Progression;
            window = new Dialog_ModSettings(mod);
            Find.WindowStack.Add(window);
            foreach (var w in Draw()) yield return w;
            c.Observe("labelsDrawn", drawn.Count).Observe("windowOpen", Find.WindowStack.IsOpen(window));
            c.Step(Find.WindowStack.IsOpen(window), "the Loot Scavenging settings window is open");
            var missing = SectionKeys.Where(k => !Drew(k.Translate())).ToList();
            c.Observe("missingSections", string.Join(", ", missing));
            c.Step(missing.Count == 0, "all six section titles were drawn");
            c.Step(Drew("LootScavenging_PresetActive".Translate("LootScavenging_PresetProgression".Translate())), "the active preset line names Progression");
            var buttons = new[] { "LootScavenging_PresetProgression", "LootScavenging_PresetCleanup", "LootScavenging_PresetVanilla", "LootScavenging_PresetReset" };
            c.Step(buttons.All(b => Drew(b.Translate())), "the three preset buttons and Reset were drawn");
            c.Conclude();
        }

        // ---- preset switches (the collapse fixed in e7efc62) -------------------------------------------------------------
        {
            var c = ctx.Begin(Switch);
            if (window == null || !Find.WindowStack.IsOpen(window)) { c.SetupFailed("the settings window did not open"); }
            else
            {
                // Cleanup then Progression: Progression adds rows, which is when the page used to wrap and collapse.
                foreach (var preset in new[] { PresetId.Cleanup, PresetId.Progression, PresetId.Vanilla, PresetId.Progression })
                {
                    Settings.Apply(Presets.Apply(preset, Settings.Capture()));
                    Settings.activePreset = preset;
                    applyMarketValue?.Invoke(null, null);
                    foreach (var w in Draw()) yield return w;
                    bool ok = OneColumn(Sections(), out string layout);
                    c.Observe("after" + preset + drawn.Count, layout);
                    c.Step(ok, $"after switching to {preset} all six sections are drawn in one column, top to bottom");
                }
                c.Conclude();
            }
        }

        // ---- locks ---------------------------------------------------------------------------------------------------------
        {
            var c = ctx.Begin(Locks);
            if (window == null || !Find.WindowStack.IsOpen(window)) { c.SetupFailed("the settings window did not open"); }
            else
            {
                var original = Presets.Config;
                var locked = new SettingLocks(new[] { SettingNames.DropMode });
                Presets.Configure(new PresetConfig(original.Progression, locked));
                c.Note("The presets are configured with the drop mode locked (applied directly, as the settings batch does)");
                Settings.Apply(Presets.Apply(PresetId.Progression, Settings.Capture()));
                Settings.activePreset = PresetId.Progression;
                Rules.dropMode = DropMode.Off;
                c.Note("Developer set the drop mode to Off behind the lock, as a widget that slipped past it would");
                foreach (var w in Draw()) yield return w;
                c.Observe("dropModeAfterDraw", Rules.dropMode).Observe("packDropMode", original.Progression.rules.dropMode);
                c.Step(Drew("LootScavenging_LockedSummary".Translate()), "the page says some settings are locked by the modpack");
                c.Step(Drew("LootScavenging_LockedReason".Translate()), "the locked drop mode row (a choice row, like the pack's locked disassembly yield) shows its reason");
                c.Step(Rules.dropMode == original.Progression.rules.dropMode, "drawing the page put the locked drop mode back to the pack's value");
                Presets.Configure(original);
                ModHarness.RestoreSettings();
                c.Conclude();
            }
            if (window != null) Find.WindowStack.TryRemove(window, doCloseSound: false);
        }

        // ---- bills tabs ----------------------------------------------------------------------------------------------------
        {
            var c = ctx.Begin(Bills);
            var table = Kit.Place(ctx, "TableMachining", new IntVec3(area.minX + 6, 0, area.minZ + 7));
            var bench = Kit.Place(ctx, "SimpleResearchBench", new IntVec3(area.minX + 20, 0, area.minZ + 7));
            var recycle = DefDatabase<RecipeDef>.GetNamed("LS_Recycle_Industrial");
            var study = DefDatabase<RecipeDef>.GetNamed("LS_Study_Medieval");
            ((IBillGiver)table).BillStack.AddBill(recycle.MakeNewBill());
            ((IBillGiver)bench).BillStack.AddBill(study.MakeNewBill());

            Find.Selector.ClearSelection();
            Find.Selector.Select(table, playSound: false);
            yield return Wait.Frames(2);
            InspectPaneUtility.OpenTab(typeof(ITab_Bills));
            foreach (var w in Draw()) yield return w;
            c.Observe("tableLabels", drawn.Count);
            c.Step(DrewPart(recycle.LabelCap), "the machining table's Bills tab draws its recycling bill");

            Find.Selector.ClearSelection();
            Find.Selector.Select(bench, playSound: false);
            yield return Wait.Frames(2);
            var tab = InspectPaneUtility.OpenTab(typeof(ITab_StudyBills));
            foreach (var w in Draw()) yield return w;
            c.Observe("studyTabOpen", tab != null).Observe("benchLabels", drawn.Count);
            c.Step(tab != null && DrewPart(study.LabelCap), "the research bench's Bills tab draws its study bill");

            var options = (List<FloatMenuOption>)AccessTools.Method(typeof(ITab_StudyBills), "Options").Invoke(null, new object[] { bench });
            var studyLabels = DefDatabase<RecipeDef>.AllDefsListForReading.Where(r => r.defName.StartsWith("LS_Study")).Select(r => r.LabelCap.ToString()).ToHashSet();
            c.Observe("addBillOptions", string.Join(", ", options.Select(o => o.Label)));
            c.Step(options.Count > 0 && options.All(o => studyLabels.Contains(o.Label)), "the study bench's add-bill menu offers study bills (scrap and original items) and nothing else");
            Find.Selector.ClearSelection();
            c.Conclude();
        }

        // ---- the first-load message ------------------------------------------------------------------------------------
        {
            var c = ctx.Begin(FirstLoad);
            bool flagBefore = Settings.firstLoadMessageShown;
            Settings.firstLoadMessageShown = false;
            int before = Find.WindowStack.Windows.Count;
            AccessTools.Method(typeof(LootScavengingMod), "ShowFirstLoadMessage").Invoke(mod, null);
            ctx.Setup("Developer called the mod's first-load message directly (the startup path runs once per launch; see the firstload batch)");
            foreach (var w in Draw()) yield return w;
            var box = Find.WindowStack.Windows.OfType<Dialog_MessageBox>().LastOrDefault();
            string title = "LootScavenging_FirstLoadTitle".Translate();
            string firstLine = ((string)"LootScavenging_FirstLoadText".Translate()).Split('\n')[0].Trim();
            c.Observe("box", box == null ? "none" : $"title '{box.title}', buttonA '{box.buttonAText}', buttonB '{box.buttonBText}'")
                .Observe("textFirstLine", firstLine);
            c.Step(box != null && box.title == title, "a message box titled with the first-load title is open");
            c.Step(box != null && box.buttonAText == "LootScavenging_FirstLoadOk".Translate() && box.buttonBText == "LootScavenging_FirstLoadOpenSettings".Translate(),
                "it offers OK and Open settings");
            c.Step(DrewPart(firstLine.Length > 40 ? firstLine.Substring(0, 40) : firstLine), "its text was drawn on screen");
            c.Step(Settings.firstLoadMessageShown, "the flag is set as soon as the message is up");
            if (box != null) Find.WindowStack.TryRemove(box, doCloseSound: false);
            Settings.firstLoadMessageShown = flagBefore || Settings.firstLoadMessageShown;
            c.Conclude();
        }

        Unpatch();
        {
            var c = ctx.Begin(NoErrors);
            c.Observe("modErrors", CaseErrors.ModMark - modErrorsAtStart);
            c.Step(CaseErrors.ModMark == modErrorsAtStart, "no error naming Loot Scavenging was logged during the batch");
            c.Conclude();
        }
    }
}
