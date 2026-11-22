using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Linq;
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
        Bills = P + "bills-tabs", FirstLoad = P + "first-load-message", Translated = P + "translated-text", NoErrors = P + "no-mod-errors";

    public override string Id => "lootscavenging-ui";
    public override int SegmentCount => 1;

    public override IReadOnlyList<PlannedCase> Plan => new[]
    {
        new PlannedCase(Page, Levels.Mechanical, "the mod's settings window opens and draws all six sections, the active preset and the four preset buttons"),
        new PlannedCase(Switch, Levels.Mechanical, "after each preset switch the page still draws all six sections in one column, top to bottom"),
        new PlannedCase(Locks, Levels.Mechanical, "with the pack's locks the page shows the locked summary and reasons, and drawing leaves a locked value at the pack's value"),
        new PlannedCase(Bills, Levels.Mechanical, "the machining table's and research bench's Bills tabs draw their recycle and study bills; the study bench offers only study bills"),
        new PlannedCase(FirstLoad, Levels.Mechanical, "the first-load message opens with its title, text and two buttons, and marks itself shown"),
        new PlannedCase(Translated, Levels.Mechanical, "no text the mod drew or built in the batch holds a raw LootScavenging_ key, an unfilled { slot, or English left in a key whose translation differs"),
        new PlannedCase(NoErrors, Levels.Mechanical, "no error naming the mod logged during the batch"),
    };

    private static readonly List<(string text, Rect rect)> drawn = new List<(string, Rect)>();
    // Every label drawn in the batch, for the translated-text case: Draw clears `drawn` each time.
    private static readonly List<(string text, Rect rect, GameFont font)> seen = new List<(string, Rect, GameFont)>();
    private static bool recording;
    private static Harmony harmony;

    private static void RecordLabel(Rect rect, string label)
    {
        if (!recording || string.IsNullOrEmpty(label)) return;
        drawn.Add((label, rect));
        seen.Add((label, rect, Text.Font));
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
        // Under --record, hold each window long enough for stills to show it; its labels are already recorded.
        if (RunRecording.Enabled) yield return Wait.Seconds(1.5);
    }

    // The English text of each of the mod's keys, from its own English Keyed file: the game holds only the active language.
    private static Dictionary<string, string> EnglishKeyed(ModContentPack content)
    {
        string file = System.IO.Path.Combine(content.RootDir, "Languages", "English", "Keyed", "LootScavenging.xml");
        return XDocument.Load(file).Root.Elements().ToDictionary(e => e.Name.LocalName, e => e.Value.Replace("\\n", "\n"));
    }

    // English pieces between slots and line breaks, long enough not to match by chance.
    private static IEnumerable<string> Fragments(string english) =>
        Regex.Split(english, @"\{[^}]*\}|\n").Select(f => f.Trim(' ', '.', ',', ':', ';', '(', ')', '"'))
            .Where(f => f.Length >= 10 && f.Any(char.IsLetter));

    private static bool NeedsMoreHeight(string text, Rect rect, GameFont font)
    {
        var before = Text.Font;
        Text.Font = font;
        float height = Text.CalcHeight(text, rect.width);
        Text.Font = before;
        return height > rect.height + 1f;
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
        seen.Clear();
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

        // ---- translated text: no raw key, unfilled slot or English left (translations ticket 08) -------------------------
        {
            var c = ctx.Begin(Translated);
            var built = new List<(string where, string text)>();

            // A weapons rule with every condition and a chance, so the longest rule sentence is drawn on the page.
            Settings.Apply(Presets.Apply(PresetId.Progression, Settings.Capture()));
            Rules.weapons = new CategoryRule(Outcome.Scrap) { onlyBeyondReach = true, limitQuality = true, limitHitPoints = true, chance = 0.5f };
            ctx.Setup("Developer gave the weapons rule scrap, beyond reach, quality and hit-point limits and a 50% chance");
            var page = new Dialog_ModSettings(mod);
            Find.WindowStack.Add(page);
            foreach (var w in Draw()) yield return w;
            // The drawn label holding every piece of that key's text between its slots.
            var pieces = Regex.Split("LootScavenging_RuleActReachQualityOrHitPointsChance".Translate().RawText, @"\{[^}]*\}")
                .Select(f => f.Trim()).Where(f => f.Length > 0).ToList();
            string weapons = "LootScavenging_CategoryWeapons".Translate();
            built.Add(("ruleSentenceAllConditions", drawn.Select(d => d.text).FirstOrDefault(t => t.Contains(weapons) && pieces.All(t.Contains))));
            Find.WindowStack.TryRemove(page, doCloseSound: false);
            ModHarness.RestoreSettings();

            // The item picker, drawn like the other windows.
            var pickerType = typeof(LootScavengingMod).Assembly.GetType("LootScavenging.SettingsUI.Dialog_ItemPicker");
            var picker = (Window)Activator.CreateInstance(pickerType, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null,
                new object[] { (string)"LootScavenging_KeepList".Translate(), new List<string>() }, null);
            Find.WindowStack.Add(picker);
            foreach (var w in Draw()) yield return w;
            c.Observe("pickerLabels", string.Join(" | ", drawn.Select(d => d.text).Distinct().Take(12)));
            Find.WindowStack.TryRemove(picker, doCloseSound: false);

            // Text the batch does not draw: bill labels and descriptions, scrap inspect text, the unfamiliar label, the study message.
            foreach (var r in DefDatabase<RecipeDef>.AllDefsListForReading.Where(r => r.defName.StartsWith("LS_")))
            {
                built.Add((r.defName + ".label", r.label));
                built.Add((r.defName + ".jobString", r.jobString));
                built.Add((r.defName + ".description", r.description));
            }
            var project = DefDatabase<ResearchProjectDef>.AllDefsListForReading
                .FirstOrDefault(p => !p.IsFinished && p.CanStartNow && p.techLevel <= TechLevel.Neolithic);
            if (project != null) Find.ResearchManager.SetCurrentProject(project);
            ctx.Setup($"Developer set the current project to {project?.defName ?? "none"}");
            foreach (var tier in new[] { ScrapTier.Neolithic, ScrapTier.Industrial })
            {
                var stack = SpawnStack(map, Scrap(tier), 5, new IntVec3(area.minX + 30, 0, area.minZ + 7));
                built.Add(("inspect." + tier, stack.GetInspectString()));
                stack.Destroy();
            }
            Rules.dropMode = DropMode.KeepOriginal;
            Unfamiliar.ResetCache();
            var rifle = GenSpawn.Spawn(ThingMaker.MakeThing(Kit.Def("Gun_ChargeRifle")), new IntVec3(area.minX + 32, 0, area.minZ + 7), map);
            ctx.Setup("Developer set the drop mode to keep originals and spawned a charge rifle at the tribal colony");
            built.Add(("unfamiliarLabel", rifle.LabelNoCount));
            rifle.Destroy();
            ModHarness.RestoreSettings();
            Unfamiliar.ResetCache();
            string scrapLabel = Scrap(ScrapTier.Medieval).label, projectLabel = project?.LabelCap.Resolve() ?? "";
            foreach (var gender in new[] { Gender.Male, Gender.Female })
            {
                var pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist, Faction.OfPlayer, fixedGender: gender,
                    forceGenerateNewPawn: true, canGeneratePawnRelations: false));
                // The call the study bill makes when an iteration finishes (StudyRecipes.Study).
                built.Add(("studyDone." + gender, "LootScavenging_StudyDone".Translate(pawn.Named("PAWN"), "5", scrapLabel, "100", projectLabel).Resolve()));
                pawn.Destroy();
            }
            foreach (var b in built) c.Observe(b.where, b.text);

            var english = EnglishKeyed(mod.Content);
            var leftovers = english
                .Where(kv => kv.Key.Translate().RawText != kv.Value)
                .SelectMany(kv => Fragments(kv.Value).Where(f => !kv.Key.Translate().RawText.Contains(f)).Select(f => (key: kv.Key, f)))
                .ToList();
            var texts = seen.Select(s => (where: "drawn", text: s.text)).Concat(built.Select(b => (where: b.where, text: b.text ?? ""))).ToList();
            var rawKeys = texts.Where(t => t.text.Contains("LootScavenging_")).Select(t => t.where + ": " + t.text).Distinct().ToList();
            var slots = texts.Where(t => t.text.Contains("{")).Select(t => t.where + ": " + t.text).Distinct().ToList();
            var englishLeft = texts.SelectMany(t => leftovers.Where(l => t.text.Contains(l.f)).Select(l => $"{t.where}: '{l.f}' ({l.key})")).Distinct().ToList();
            // Evidence for clipping, not a check: a label needing more height than its rect at the font it was drawn in.
            var tall = seen.Where(s => s.rect.width > 1f && s.rect.height > 1f && NeedsMoreHeight(s.text, s.rect, s.font))
                .Select(s => $"{s.text} ({s.rect.width:0}x{s.rect.height:0})").Distinct().ToList();
            c.Observe("language", LanguageDatabase.activeLanguage?.folderName).Observe("textsChecked", texts.Count)
                .Observe("englishFragmentsChecked", leftovers.Count).Observe("rawKeys", string.Join(" || ", rawKeys.Take(20)))
                .Observe("unfilledSlots", string.Join(" || ", slots.Take(20))).Observe("englishLeft", string.Join(" || ", englishLeft.Take(20)))
                .Observe("labelsTallerThanRect", string.Join(" || ", tall.Take(30)));
            c.Step(built.All(b => !string.IsNullOrEmpty(b.text)) && seen.Count > 0, "the batch drew labels and built every listed text");
            c.Step(rawKeys.Count == 0, "no text holds a raw LootScavenging_ key");
            c.Step(slots.Count == 0, "no text holds an unfilled { slot");
            c.Step(englishLeft.Count == 0, "no text holds English from a key whose translation differs");
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
