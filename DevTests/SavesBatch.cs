using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using LootScavenging.Model;
using RimWorld;
using RimWorldDevTests;
using Verse;
using static LootScavenging.DevTests.Fixtures;

namespace LootScavenging.DevTests;

// Save and reload (tickets 03, 05, 06): scrap, studied scrap, a study bill and a looted charge rifle carrying the looted-only
// marker survive the runner's save and load. The save file is then read for what names the mod. Loading the save without
// the mod is not possible here: the runner ships inside the mod; tools/removal-check loads the archived save without it.
public sealed class SavesBatch : Batch
{
    private const string P = "lootscavenging.saves.";
    private const string Before = P + "looted-marker-set", Reload = P + "reload-keeps-state", File_ = P + "save-contents";

    public override string Id => "lootscavenging-saves";
    public override int SegmentCount => 2;

    public override IReadOnlyList<PlannedCase> Plan => new[]
    {
        new PlannedCase(Before, Levels.Mechanical, "looted-only scope marks a looted charge rifle; an unlooted one stays equippable"),
        new PlannedCase(Reload, Levels.Mechanical, "after save and load: scrap and studied scrap stacks, the study bench and its bill, and the rifle's marker are intact"),
        new PlannedCase(File_, Levels.Mechanical, "the save holds the marker once and names no mod class for the bench"),
    };

    private static Thing ById(Map map, string id) => int.TryParse(id, out int n) ? map.listerThings.AllThings.FirstOrDefault(t => t.thingIDNumber == n) : null;

    public override IEnumerable<Wait> Segment(int index, BatchContext ctx)
    {
        var map = ctx.Map;
        if (index == 0)
        {
            ModHarness.SaveSettings();
            var area = Kit.Area(ctx, 30, 12);
            yield return Wait.Ticks(1);
            MakeTribal(ctx);
            Rules.dropMode = DropMode.KeepOriginal;
            Settings.values.unfamiliarScope = UnfamiliarScope.LootedOnly;
            Unfamiliar.ResetCache();
            var sites = Sites(area, 4);
            var c = ctx.Begin(Before);
            var rifleDef = Kit.Def("Gun_ChargeRifle");
            var p = Spawn(ctx, Mercenary, Pirates, sites[0]);
            var looted = Arm(p, rifleDef, QualityCategory.Normal);
            p.Kill(null);
            var unlooted = GenSpawn.Spawn(Make(rifleDef, QualityCategory.Normal), sites[1], map);
            var marker = ((ThingWithComps)looted).GetComp<CompUnfamiliar>()?.lootedEra ?? Era.Undefined;
            c.Observe("lootedEra", marker);
            c.Step(!looted.Destroyed && looted.Spawned, "the pirate's charge rifle drops whole in keep-original mode");
            c.Step(marker == Era.Spacer && Unfamiliar.IsUnfamiliar(looted), "it carries the looted marker and is unfamiliar");
            var colonist = map.mapPawns.FreeColonistsSpawned.First();
            c.Step(!Unfamiliar.IsUnfamiliar(unlooted) && EquipmentUtility.CanEquip(unlooted, colonist), "an unlooted charge rifle is familiar and equippable in looted-only scope");
            c.Conclude();

            var scrap = SpawnStack(map, Scrap(ScrapTier.Industrial), 20, sites[2]);
            var studied = SpawnStack(map, StudiedScrap(ScrapTier.Industrial), 10, sites[2] + new IntVec3(0, 0, 3));
            var bench = Kit.Place(ctx, "SimpleResearchBench", sites[3]);
            var bill = (Bill_Production)DefDatabase<RecipeDef>.GetNamed("LS_Study_Industrial").MakeNewBill();
            bill.repeatMode = BillRepeatModeDefOf.TargetCount;
            bill.targetCount = 7;
            ((IBillGiver)bench).BillStack.AddBill(bill);
            // For tools/removal-check: a vanilla bill and a recycle bill on one bench, to see which survive without the mod.
            var table = Kit.Place(ctx, "TableMachining", sites[3] + new IntVec3(0, 0, -5));
            ((IBillGiver)table).BillStack.AddBill(DefDatabase<RecipeDef>.GetNamed("Make_ComponentIndustrial").MakeNewBill());
            ((IBillGiver)table).BillStack.AddBill(DefDatabase<RecipeDef>.GetNamed("LS_Recycle_Industrial").MakeNewBill());
            ctx.Remember("looted", looted.thingIDNumber.ToString());
            ctx.Remember("unlooted", unlooted.thingIDNumber.ToString());
            ctx.Remember("scrap", scrap.thingIDNumber.ToString());
            ctx.Remember("studied", studied.thingIDNumber.ToString());
            ctx.Remember("bench", bench.thingIDNumber.ToString());
            yield return Wait.Reload();
            yield break;
        }

        {
            var c = ctx.Begin(Reload);
            var looted = ById(map, ctx.Recall("looted"));
            var unlooted = ById(map, ctx.Recall("unlooted"));
            var scrap = ById(map, ctx.Recall("scrap"));
            var studied = ById(map, ctx.Recall("studied"));
            var bench = ById(map, ctx.Recall("bench"));
            var bill = (bench as Building_StudyBench)?.billStack?.Bills.OfType<Bill_Production>().FirstOrDefault();
            Unfamiliar.ResetCache();
            c.Observe("scrap", scrap == null ? "missing" : scrap.def.defName + " x" + scrap.stackCount)
                .Observe("studied", studied == null ? "missing" : studied.def.defName + " x" + studied.stackCount)
                .Observe("bench", bench?.GetType().Name ?? "missing")
                .Observe("bill", bill == null ? "missing" : $"{bill.recipe.defName} {bill.repeatMode.defName} {bill.targetCount}")
                .Observe("lootedEra", (looted as ThingWithComps)?.GetComp<CompUnfamiliar>()?.lootedEra.ToString() ?? "missing");
            c.Step(scrap is ScrapThing && scrap.def.defName == "LS_Scrap_Industrial" && scrap.stackCount == 20, "the 20 industrial scrap are back");
            c.Step(studied != null && studied.def == StudiedScrap(ScrapTier.Industrial) && studied.stackCount == 10, "the 10 studied industrial scrap are back");
            c.Step(bench is Building_StudyBench, "the research bench loads as a study bench");
            c.Step(bill != null && bill.recipe.defName == "LS_Study_Industrial" && bill.repeatMode == BillRepeatModeDefOf.TargetCount && bill.targetCount == 7,
                "its study bill keeps its recipe and \"keep 7\"");
            c.Step((looted as ThingWithComps)?.GetComp<CompUnfamiliar>()?.lootedEra == Era.Spacer && Unfamiliar.IsUnfamiliar(looted),
                "the looted charge rifle keeps its marker and is still unfamiliar");
            c.Step(unlooted != null && !Unfamiliar.IsUnfamiliar(unlooted), "the unlooted one is still familiar");
            c.Conclude();
        }
        {
            var c = ctx.Begin(File_);
            // The runner moves its save into the run's evidence folder after loading it.
            var evidence = Path.Combine(GenFilePaths.SaveDataFolderPath, Harness.Settings.EvidenceFolder);
            var save = new DirectoryInfo(evidence).GetFiles("*.rws", SearchOption.AllDirectories).OrderByDescending(f => f.LastWriteTimeUtc).FirstOrDefault();
            if (save == null) { c.SetupFailed("no save file found under " + evidence); yield break; }
            string text = File.ReadAllText(save.FullName);
            int markers = Regex.Matches(text, "<lootScavengingLootedEra>").Count;
            var classes = Regex.Matches(text, "Class=\"(LootScavenging[^\"]*)\"").Cast<Match>().GroupBy(m => m.Groups[1].Value).Select(g => g.Key + " x" + g.Count()).ToList();
            c.Observe("save", save.Name).Observe("markers", markers).Observe("modClasses", string.Join(", ", classes))
                .Observe("lsDefNames", Regex.Matches(text, ">LS_[A-Za-z_]+<").Cast<Match>().Select(m => m.Value.Trim('<', '>')).Distinct().Count());
            c.Step(markers == 1, "only the looted rifle writes a marker (an unmarked item saves nothing)");
            c.Step(!text.Contains("Building_StudyBench"), "the bench is saved under the vanilla research bench class");
            c.Conclude();
        }
    }
}
