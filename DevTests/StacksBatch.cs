using System;
using System.Collections.Generic;
using LootScavenging.Model;
using RimWorld;
using RimWorldDevTests;
using Verse;

namespace LootScavenging.DevTests;

// A scene for looking at the scrap art by hand with --keep-open: one column per era, neolithic to archotech, with piles
// of 1, 10 and 500 (the three textures Graphic_StackCount picks between) and a full stack of studied scrap on top.
// Not a gameplay test: the case passes when every pile was placed. Not part of run-all.
public sealed class StacksBatch : Batch
{
    public override string Id => "lootscavenging-stacks";
    public override bool IsShowcase => true;
    public override int SegmentCount => 1;
    public override TimeSpan WallLimit => TimeSpan.FromMinutes(5);
    public override IReadOnlyList<PlannedCase> Plan { get; } = new[] { new PlannedCase("stacks.placed", Levels.Evidence) };

    private static readonly int[] Counts = { 1, 10, 500 };

    // Camera size: the 26 by 16 cell area fills a 16:9 frame.
    private const float Zoom = 8f;

    public override IEnumerable<Wait> Segment(int index, BatchContext ctx)
    {
        var map = ctx.Map;
        map.weatherManager.curWeather = WeatherDefOf.Clear;
        map.weatherManager.lastWeather = WeatherDefOf.Clear;
        int hour = GenLocalDate.HourOfDay(map);
        Find.TickManager.DebugSetTicksGame(Find.TickManager.TicksGame + ((36 - hour) % 24) * GenDate.TicksPerHour);
        Find.TickManager.CurTimeSpeed = TimeSpeed.Paused;
        Find.Selector.ClearSelection();
        ctx.Setup("Developer set clear weather and moved the clock to midday");

        var area = Kit.Area(ctx, 26, 16);
        var floor = DefDatabase<TerrainDef>.GetNamed("Concrete");
        foreach (var c in area.Cells) map.terrainGrid.SetTerrain(c, floor);

        int piles = 0, expected = 0;
        var tiers = (ScrapTier[])Enum.GetValues(typeof(ScrapTier));
        for (int i = 0; i < tiers.Length; i++)
        {
            int x = area.minX + 3 + i * 4;
            var fresh = Fixtures.Scrap(tiers[i]);
            for (int r = 0; r < Counts.Length; r++)
            {
                expected++;
                if (GenPlace.TryPlaceThing(Fixtures.Stack(fresh, Counts[r]), new IntVec3(x, 0, area.minZ + 3 + r * 3), map, ThingPlaceMode.Direct)) piles++;
            }
            var studied = Fixtures.StudiedScrap(tiers[i]);
            expected++;
            if (GenPlace.TryPlaceThing(Fixtures.Stack(studied, studied.stackLimit), new IntVec3(x, 0, area.minZ + 12), map, ThingPlaceMode.Direct)) piles++;
        }
        ctx.Setup($"Developer laid concrete over {area} and placed {piles} scrap piles: 1, 10 and 500 of each era, and a full studied stack");

        var look = area.CenterCell.ToVector3Shifted();
        var config = Find.CameraDriver.config;
        var range = config.sizeRange;
        config.sizeRange = new FloatRange(Zoom, range.max);
        Find.CameraDriver.SetRootPosAndSize(look, Zoom);
        yield return Wait.Seconds(1.0);
        config.sizeRange = range;

        ctx.Begin("stacks.placed").Expect("piles", expected).Observe("piles", piles)
            .Check(piles == expected, "a scrap pile was not placed");
    }
}
