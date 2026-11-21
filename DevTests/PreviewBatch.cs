using System;
using System.Collections.Generic;
using LootScavenging.Model;
using RimWorld;
using RimWorldDevTests;
using Verse;

namespace LootScavenging.DevTests;

// Films the scene for the Workshop preview image with --record --frames: the six scrap piles, neolithic to archotech, in
// front of the benches that recycle and study them. Not a gameplay test: the case passes when every pile and bench was
// placed. Not part of run-all.
public sealed class PreviewBatch : Batch
{
    public override string Id => "lootscavenging-preview";
    public override bool IsShowcase => true;
    public override int SegmentCount => 1;
    public override TimeSpan WallLimit => TimeSpan.FromMinutes(5);
    public override IReadOnlyList<PlannedCase> Plan { get; } = new[] { new PlannedCase("preview.filmed", Levels.Evidence) };

    // Left to right in era order: each tier's own recycling bench, then the research bench where any tier is studied.
    private static readonly (string def, string stuff)[] Benches =
    {
        ("CraftingSpot", null), ("FueledSmithy", "Steel"), ("TableMachining", null), ("FabricationBench", null), ("SimpleResearchBench", "Steel"),
    };

    // Bench centres from the area's left edge, spaced for their widths (the fabrication bench is the widest).
    private static readonly int[] BenchX = { 4, 8, 12, 17, 22 };

    // Camera size for the shot: about 28 cells across a 16:9 frame, the width of the paved area.
    private const float Zoom = 7.5f;

    public override IEnumerable<Wait> Segment(int index, BatchContext ctx)
    {
        var map = ctx.Map;
        map.weatherManager.curWeather = WeatherDefOf.Clear;
        map.weatherManager.lastWeather = WeatherDefOf.Clear;
        // Midday light, so the tinted piles read at their true colours.
        int hour = GenLocalDate.HourOfDay(map);
        Find.TickManager.DebugSetTicksGame(Find.TickManager.TicksGame + ((36 - hour) % 24) * GenDate.TicksPerHour);
        Find.TickManager.CurTimeSpeed = TimeSpeed.Paused;
        Find.Selector.ClearSelection();
        ctx.Setup("Developer set clear weather and moved the clock to midday");

        var area = Kit.Area(ctx, 28, 16);
        var floor = DefDatabase<TerrainDef>.GetNamed("Concrete");
        foreach (var c in area.Cells) map.terrainGrid.SetTerrain(c, floor);

        int benches = 0;
        for (int i = 0; i < Benches.Length; i++)
        {
            var (def, stuff) = Benches[i];
            var cell = new IntVec3(area.minX + BenchX[i], 0, area.minZ + 7);
            var bench = Kit.Place(ctx, def, cell, Rot4.North, stuff == null ? null : DefDatabase<ThingDef>.GetNamed(stuff));
            // Fuelled and powered, so no "out of fuel" or "no power" overlay sits on the shot.
            var fuel = bench.TryGetComp<CompRefuelable>();
            fuel?.Refuel(fuel.Props.fuelCapacity);
            if (bench.TryGetComp<CompPowerTrader>() != null) Kit.HoldPower(ctx, bench, true);
            benches++;
        }

        int piles = 0;
        var tiers = (ScrapTier[])Enum.GetValues(typeof(ScrapTier));
        for (int i = 0; i < tiers.Length; i++)
        {
            var def = Fixtures.Scrap(tiers[i]);
            var cell = new IntVec3(area.minX + 4 + i * 4, 0, area.minZ + 4);
            var pile = Fixtures.Stack(def, def.stackLimit);
            if (GenPlace.TryPlaceThing(pile, cell, map, ThingPlaceMode.Direct)) piles++;
        }
        ctx.Setup($"Developer laid concrete over {area}, built {benches} benches and placed {piles} full scrap stacks");

        // The camera clamps its zoom to config.sizeRange every frame (11 at the closest), so the range is lowered for the
        // shot and put back after it. The game stays paused, so nothing else moves the camera.
        var look = area.CenterCell.ToVector3Shifted();
        var config = Find.CameraDriver.config;
        var range = config.sizeRange;
        config.sizeRange = new FloatRange(Zoom, range.max);
        Find.CameraDriver.SetRootPosAndSize(look, Zoom);

        if (RunRecording.Enabled)
        {
            RunRecording.Ready();
            yield return Wait.Recorder();
        }
        Find.CameraDriver.SetRootPosAndSize(look, Zoom);
        yield return Wait.Seconds(4.0);
        config.sizeRange = range;

        ctx.Begin("preview.filmed").Expect("benches", Benches.Length).Expect("piles", tiers.Length)
            .Observe("benches", benches).Observe("piles", piles)
            .Check(benches == Benches.Length && piles == tiers.Length, "a bench or a scrap pile was not placed");
    }
}
