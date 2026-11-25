using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using HarmonyLib;
using LootScavenging.Model;
using RimWorld;
using RimWorldDevTests;
using UnityEngine;
using Verse;
using static LootScavenging.DevTests.Fixtures;

namespace LootScavenging.DevTests;

// Films the promo video's live footage that replaced its gallery cards, with --record --show-ui at normal game speed, on the
// Progression preset at a tribal colony:
//   drop-*      the same tribal warrior (club, an autopistol in his pack) dies on camera under each drop mode, and what he
//               carried lands; under keep-original the autopistol is then selected so its inspect pane reads "unfamiliar"
//   settings-*  the mod's settings window, switched through Progression, Cleanup and Vanilla the way its buttons do it
// Writes shots.json (marks on the game's monotonic clock) to the run folder for tools/render_promo.py. Kept apart from
// lootscavenging-workshop-clips, whose marks the GIF and gallery tools cut from. Not a gameplay test: the case passes when each
// beat happened on film. Not part of run-all.
public sealed class PromoClipsBatch : Batch
{
    public override string Id => "lootscavenging-promo-clips";
    public override bool IsShowcase => true;
    public override int SegmentCount => 1;
    public override TimeSpan WallLimit => TimeSpan.FromMinutes(6);
    public override IReadOnlyList<PlannedCase> Plan { get; } = new[] { new PlannedCase("promo-clips.filmed", Levels.Evidence) };

    private const float Zoom = 5f;

    private readonly List<(string name, double t)> marks = new();
    private static double Now() => Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency;
    private void Mark(string name) => marks.Add((name, Now())); // not translated
    private static string Num(double v) => v.ToString("0.###", CultureInfo.InvariantCulture);

    private static string RunDir()
    {
        string pointer = Path.Combine(TestRunner.EvidenceRoot, "active.txt");
        string dir = File.Exists(pointer) ? File.ReadAllText(pointer).Trim() : null;
        return !string.IsNullOrEmpty(dir) && Directory.Exists(dir) ? dir : TestRunner.EvidenceRoot;
    }

    private static Faction Tribe => Find.FactionManager.FirstFactionOfDef(DefDatabase<FactionDef>.GetNamedSilentFail("TribeSavage"))
        ?? Find.FactionManager.AllFactionsListForReading.FirstOrDefault(f => !f.IsPlayer && !f.Hidden && f.def.humanlikeFaction
            && f.def.techLevel == TechLevel.Neolithic && f.HostileTo(Faction.OfPlayer));

    private static Wait Play(double seconds, Action everyTick)
    {
        var wait = Wait.Seconds(seconds, true);
        wait.EveryTick = everyTick;
        return wait;
    }

    private static void Unforbid(Map map, CellRect rect)
    {
        foreach (var t in map.listerThings.AllThings.Where(t => t.Spawned && rect.Contains(t.Position)).ToList())
            t.SetForbidden(false, warnOnFail: false);
    }

    public override IEnumerable<Wait> Segment(int index, BatchContext ctx)
    {
        var map = ctx.Map;
        Kit.RemoveStartingColonists(ctx);
        yield return Wait.Ticks(1);
        foreach (var w in Find.WindowStack.Windows.Where(w => w is Dialog_MessageBox || w is Dialog_NodeTree).ToList()) w.Close(false);
        Find.TickManager.CurTimeSpeed = TimeSpeed.Paused;
        MakeTribal(ctx);

        map.weatherManager.curWeather = WeatherDefOf.Clear;
        map.weatherManager.lastWeather = WeatherDefOf.Clear;
        int hour = GenLocalDate.HourOfDay(map);
        Find.TickManager.DebugSetTicksGame(Find.TickManager.TicksGame + ((34 - hour) % 24) * GenDate.TicksPerHour);
        ctx.Setup("Developer set clear weather and moved the clock to 10 am");

        ModHarness.SaveSettings();
        Settings.Apply(Presets.Apply(PresetId.Progression, Settings.Capture()));
        Settings.activePreset = PresetId.Progression;
        // The drop scenes show what lands beside the body, as before ticket 24 made keeping it on the body the default.
        ModHarness.GearDrops(Settings.values);
        ctx.Setup("Developer applied the Progression preset and turned off the hostile and non-hostile keep-gear boxes, so loot lands beside the body");

        var tribe = Tribe ?? throw new SetupException("no hostile neolithic faction in this world");
        var tribal = Kit.Def("Apparel_TribalA");

        // Four drop-mode scenes along one strip, 16 cells apart, so no earlier body shows in a later shot (the camera shows about
        // 18 cells across). Separate Kit.Area calls returned the same empty area, since nothing was built on it yet.
        // The four drop modes of 0.5.2, as rows and category rules.
        var dropModes = new[] { ModHarness.LootStyle.ScrapOutOfReach, ModHarness.LootStyle.ScrapEverything, ModHarness.LootStyle.KeepOriginal, ModHarness.LootStyle.Off };
        var strip = Kit.Area(ctx, 64, 9);
        var dropAreas = dropModes.Select((_, i) => new CellRect(strip.minX + i * 16, strip.minZ, 16, 9)).ToList();

        var config = Find.CameraDriver.config;
        var range = config.sizeRange;
        config.sizeRange = new FloatRange(Zoom, range.max);
        bool tutor = Prefs.AdaptiveTrainingEnabled;
        Prefs.AdaptiveTrainingEnabled = false;
        ctx.Setup("Developer turned off the learning helper for the shots");
        Vector3 look = dropAreas[0].CenterCell.ToVector3Shifted();
        bool holdSelection = true;
        void Camera()
        {
            Find.CameraDriver.SetRootPosAndSize(look, Zoom);
            if (holdSelection && Find.Selector.NumSelected > 0) Find.Selector.ClearSelection();
        }
        int tick = 0;
        void Tidy() { if (++tick % 15 == 0) foreach (var r in dropAreas) Unforbid(map, r); }
        var keep = Kit.All(Camera, Tidy);

        var drops = new List<string>();
        bool unfamiliarSelected = false;
        var presetsShown = new List<PresetId>();
        float scale = Prefs.UIScale;
        Dialog_ModSettings window = null;
        try
        {
            Find.Selector.ClearSelection();
            Camera();
            if (RunRecording.Enabled)
            {
                RunRecording.Ready();
                yield return Wait.Recorder();
            }
            Find.TickManager.CurTimeSpeed = TimeSpeed.Normal;

            // ----- Drop modes: he stands a beat, dies, and what he carried lands -----
            for (int i = 0; i < dropModes.Length; i++)
            {
                ModHarness.ApplyStyle(Rules, dropModes[i]);
                Unfamiliar.ResetCache();
                var cell = dropAreas[i].CenterCell;
                var w = Spawn(ctx, DefDatabase<PawnKindDef>.GetNamed("Tribal_Warrior"), tribe, cell);
                Dress(w, tribal, QualityCategory.Normal);
                Arm(w, Kit.Def("MeleeWeapon_Club"), QualityCategory.Normal);
                w.inventory.innerContainer.TryAdd(Make(Kit.Def("Gun_Autopistol"), QualityCategory.Normal));
                w.Rotation = Rot4.East;
                look = cell.ToVector3Shifted() + new Vector3(0.5f, 0f, 0f);
                Camera();
                Mark("drop-" + dropModes[i].ToString().ToLowerInvariant());
                yield return Play(1.2, keep);
                // He may have stepped; the camera stays where it was framed.
                var at = w.Position;
                w.Kill(null);
                // Scrap everything takes the tribalwear too; dressed again in the same tick, before a frame draws him naked,
                // so the body reads as a tribal raider.
                if (w.Corpse != null && !w.apparel.WornApparel.Any())
                {
                    Dress(w, tribal, QualityCategory.Normal);
                    w.Drawer.renderer.SetAllGraphicsDirty();
                }
                FilthMaker.TryMakeFilth(at, map, ThingDefOf.Filth_Blood, 2);
                Unforbid(map, dropAreas[i]);
                Mark("drop-" + dropModes[i].ToString().ToLowerInvariant() + "-dead");
                var near = map.listerThings.AllThings.Where(t => t.Spawned && t.def.category == ThingCategory.Item && t.Position.DistanceTo(at) <= 3f).ToList();
                drops.Add($"{dropModes[i]}: {Describe(near)}");
                ctx.Setup($"Drop mode {dropModes[i]}: warrior killed at {at}; beside him: {Describe(near)}");
                var gun = near.FirstOrDefault(t => t.def.defName == "Gun_Autopistol");
                if (dropModes[i] == ModHarness.LootStyle.KeepOriginal && gun != null)
                {
                    yield return Play(0.8, keep);
                    holdSelection = false;
                    Find.Selector.Select(gun, playSound: false, forceDesignatorDeselect: false);
                    unfamiliarSelected = gun.LabelCap.ToString().Contains("LootScavenging_UnfamiliarLabel".Translate("").ToString().Trim());
                    Mark("drop-keeporiginal-selected");
                    yield return Play(3.0, keep);
                    Find.Selector.ClearSelection();
                    holdSelection = true;
                }
                else yield return Play(2.5, keep);
            }
            ModHarness.ApplyStyle(Rules, ModHarness.LootStyle.ScrapOutOfReach);

            // ----- Settings: the page under each preset, switched the way its buttons do it -----
            Find.TickManager.CurTimeSpeed = TimeSpeed.Paused;
            var mod = LoadedModManager.GetMod<LootScavengingMod>();
            var applyMarketValue = AccessTools.Method(AccessTools.TypeByName("LootScavenging.ScrapDefs"), "ApplyMarketValue");
            window = new Dialog_ModSettings(mod);
            Find.WindowStack.Add(window);
            // UI scale 1.5 so the page's small text survives the 1080p downscale; the window as tall as the screen so the preset
            // line and the first item rules show together (as lootscavenging-ui's gallery still).
            Prefs.UIScale = 1.5f;
            yield return Wait.Frames(2);
            window.windowRect = new Rect((UI.screenWidth - 820f) / 2f, 20f, 820f, UI.screenHeight - 40f);
            foreach (var preset in new[] { PresetId.Progression, PresetId.Cleanup, PresetId.Vanilla, PresetId.Progression })
            {
                Settings.Apply(Presets.Apply(preset, Settings.Capture()));
                Settings.activePreset = preset;
                applyMarketValue?.Invoke(null, null);
                Mark("settings-" + preset.ToString().ToLowerInvariant());
                presetsShown.Add(preset);
                yield return Wait.Seconds(2.5);
            }
            Mark("end");
            // The recorder can lose the last seconds when the game quits.
            yield return Wait.Seconds(2.0);
        }
        finally
        {
            if (window != null) window.Close(false);
            Prefs.UIScale = scale;
            config.sizeRange = range;
            Prefs.AdaptiveTrainingEnabled = tutor;
            Find.TickManager.CurTimeSpeed = TimeSpeed.Paused;
            File.WriteAllText(Path.Combine(RunDir(), "shots.json"),
                "{\"marks\":[" + string.Join(",", marks.Select(m => $"{{\"name\":\"{m.name}\",\"t\":{Num(m.t)}}}")) + "]}");
        }

        ctx.Begin("promo-clips.filmed")
            .Observe("drops", string.Join(" | ", drops)).Observe("unfamiliarSelected", unfamiliarSelected)
            .Observe("presetsShown", string.Join(", ", presetsShown))
            .Observe("marks", string.Join(", ", marks.Select(m => m.name)))
            .Check(drops.Count == 4 && unfamiliarSelected && presetsShown.Count == 4,
                "a beat did not happen on film: a drop-mode death, the unfamiliar autopistol selected, or a preset on the settings page");
    }
}
