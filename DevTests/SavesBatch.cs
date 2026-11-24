using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Reflection;
using LootScavenging.Loot;
using LootScavenging.Model;
using RimWorld.Planet;
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
    private const string Before = P + "looted-marker-set", Reload = P + "reload-keeps-state", File_ = P + "save-contents",
        Doubled = P + "old-scrap-doubled", DoubledOnce = P + "old-scrap-doubled-once", DownedReload = P + "downed-reload-keeps-weapon",
        ColonistReload = P + "downed-colonist-reload-keeps-weapon";

    public override string Id => "lootscavenging-saves";
    public override int SegmentCount => 4;

    public override IReadOnlyList<PlannedCase> Plan => new[]
    {
        new PlannedCase(Before, Levels.Mechanical, "looted-only scope marks a looted charge rifle; an unlooted one stays equippable"),
        new PlannedCase(Reload, Levels.Mechanical, "after save and load: scrap and studied scrap stacks, the study bench and its bill, and the rifle's marker are intact"),
        new PlannedCase(File_, Levels.Mechanical, "the save holds the marker once and names no mod class for the bench"),
        new PlannedCase(DownedReload, Levels.Mechanical, "a pirate downed holding his revolver is saved and loaded: he still holds it, none lies on the ground, and he gets up holding it"),
        new PlannedCase(ColonistReload, Levels.Mechanical, "a colonist downed holding his revolver is saved and loaded: he still holds it and none lies on the ground"),
        new PlannedCase(Doubled, Levels.Mechanical, "a game at scrap scale 1 loads with its scrap doubled: 600 on the ground become 1000 and 200 beside them, 7 in a colonist's pack 14, 12 in a caravan 24"),
        new PlannedCase(DoubledOnce, Levels.Mechanical, "a second load doubles nothing"),
    };

    // Ticket 19's scale, private to the component: set to 1 here to stand for a save from before 0.5.2.
    private static readonly FieldInfo ScaleField = typeof(ScrapScaleComponent).GetField("scrapScale", BindingFlags.NonPublic | BindingFlags.Instance);
    private static int Scale => (int)ScaleField.GetValue(Current.Game.GetComponent<ScrapScaleComponent>());

    // Units of a def anywhere under a holder: a map's ground, storage, pawns and their packs, or a caravan's pawns.
    private static List<Thing> Under(IThingHolder holder, ThingDef def)
    {
        var all = new List<Thing>();
        if (holder is Map map) ThingOwnerUtility.GetAllThingsRecursively(map, ThingRequest.ForGroup(ThingRequestGroup.Everything), all);
        else ThingOwnerUtility.GetAllThingsRecursively(holder, all);
        return all.Where(t => t.def == def).Distinct().ToList();
    }

    private static Caravan CaravanById(string id) => int.TryParse(id, out int n) ? Find.WorldObjects.Caravans.FirstOrDefault(c => c.ID == n) : null;

    // The three places the doubling case watches, as units and stacks.
    private static (int ground, int largest, int pack, int caravan) Watched(Map map, BatchContext ctx)
    {
        var ground = Under(map, Scrap(ScrapTier.Medieval));
        var pawn = map.mapPawns.AllPawns.FirstOrDefault(p => p.thingIDNumber.ToString() == ctx.Recall("packPawn"));
        var caravan = CaravanById(ctx.Recall("caravan"));
        int pack = pawn == null ? -1 : Units(Under(pawn, StudiedScrap(ScrapTier.Neolithic)));
        int inCaravan = caravan == null ? -1 : Units(Under(caravan, Scrap(ScrapTier.Spacer)));
        return (Units(ground), ground.Count == 0 ? 0 : ground.Max(t => t.stackCount), pack, inCaravan);
    }

    private static Thing ById(Map map, string id) => int.TryParse(id, out int n) ? map.listerThings.AllThings.FirstOrDefault(t => t.thingIDNumber == n) : null;

    public override IEnumerable<Wait> Segment(int index, BatchContext ctx)
    {
        var map = ctx.Map;
        if (index == 0)
        {
            ModHarness.SaveSettings(ModHarness.LootOnGround);
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

            // Ticket 23: a downed pirate holding his weapon when the game is saved. Loading spawns him again, and the game drops
            // a downed pawn's weapon when he spawns outside a bed.
            var down = ctx.Begin(DownedReload);
            var raider = Spawn(ctx, Mercenary, Pirates, sites[0] + new IntVec3(0, 0, 4));
            var revolver = Arm(raider, Kit.Def("Gun_Revolver"), QualityCategory.Normal);
            HealthUtility.TryAnesthetize(raider);
            down.Subject(raider);
            down.Note("Downed pawns keep their weapon (the default). A pirate holding a revolver is downed, then the game is saved and loaded");
            down.Step(raider.Downed && raider.equipment.Primary == revolver && DownedGears.KeepsWeapon(Rules, OwnerGroup.Hostile, MapKind.Home),
                "before the save he is down holding his revolver, and the setting covers him");
            ctx.Remember("raider", raider.thingIDNumber.ToString());
            ctx.Remember("raiderGun", revolver.thingIDNumber.ToString());

            // Ticket 24: a downed colonist holding his weapon when the game is saved.
            var own = ctx.Begin(ColonistReload);
            var colonistDown = Spawn(ctx, PawnKindDefOf.Colonist, Faction.OfPlayer, sites[1] + new IntVec3(0, 0, 4));
            var ownGun = Arm(colonistDown, Kit.Def("Gun_Revolver"), QualityCategory.Normal);
            HealthUtility.TryAnesthetize(colonistDown);
            own.Subject(colonistDown);
            own.Note("Colonists keep their gear (the default). A colonist holding a revolver is downed, then the game is saved and loaded");
            own.Step(colonistDown.Downed && colonistDown.equipment.Primary == ownGun && Settings.values.colonistsKeepGear,
                "before the save he is down holding his revolver, and colonists keep their gear");
            ctx.Remember("colonistDown", colonistDown.thingIDNumber.ToString());
            ctx.Remember("colonistGun", ownGun.thingIDNumber.ToString());
            yield return Wait.Reload();
            yield break;
        }

        if (index == 1)
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
        if (index == 1)
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

        if (index == 1)
        {
            var c = ctx.Begin(DownedReload);
            var raider = map.mapPawns.AllPawnsSpawned.FirstOrDefault(p => p.thingIDNumber.ToString() == ctx.Recall("raider"));
            if (raider == null) { c.SetupFailed("the downed pirate is not on the loaded map"); }
            else
            {
                c.Subject(raider);
                var held = raider.equipment.Primary;
                var revolver = Kit.Def("Gun_Revolver");
                var onGround = map.listerThings.ThingsOfDef(revolver).Where(t => t.Spawned && t.Position.DistanceTo(raider.Position) <= 5f).ToList();
                c.Observe("primary", held == null ? "none" : held.def.defName + " " + held.thingIDNumber).Observe("revolversOnGround", onGround.Count);
                c.Step(raider.Downed && held != null && held.thingIDNumber.ToString() == ctx.Recall("raiderGun") && onGround.Count == 0,
                    "after the load he is still down holding the same revolver, and none lies on the ground");
                raider.health.RemoveHediff(raider.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Anesthetic));
                var up = Wait.Until(() => !raider.Downed, 600);
                yield return up;
                c.Observe("gets up: primary", raider.equipment.Primary?.def.defName ?? "none");
                c.Step(!up.TimedOut && raider.equipment.Primary == held && held != null && !held.Destroyed, "he gets up still holding it");
                c.Conclude();
                // Gone before he can attack the colonists.
                raider.Destroy();
                if (held != null && !held.Destroyed) held.Destroy();
            }
        }

        if (index == 1)
        {
            var c = ctx.Begin(ColonistReload);
            var colonist = map.mapPawns.AllPawnsSpawned.FirstOrDefault(p => p.thingIDNumber.ToString() == ctx.Recall("colonistDown"));
            if (colonist == null) { c.SetupFailed("the downed colonist is not on the loaded map"); }
            else
            {
                c.Subject(colonist);
                var held = colonist.equipment.Primary;
                var onGround = map.listerThings.ThingsOfDef(Kit.Def("Gun_Revolver")).Where(t => t.Spawned && t.Position.DistanceTo(colonist.Position) <= 5f).ToList();
                c.Observe("primary", held == null ? "none" : held.def.defName + " " + held.thingIDNumber).Observe("revolversOnGround", onGround.Count);
                c.Step(colonist.Downed && held != null && held.thingIDNumber.ToString() == ctx.Recall("colonistGun") && onGround.Count == 0,
                    "after the load he is still down holding the same revolver, and none lies on the ground");
                c.Conclude();
                colonist.Destroy();
                if (held != null && !held.Destroyed) held.Destroy();
            }
        }

        // ---- 19: a save from before 0.5.2 has its scrap doubled once -----------------------------------------------------
        if (index == 1)
        {
            int scale = Scale;
            var sites = Sites(Kit.Area(ctx, 30, 12), 4);
            var ground = SpawnStack(map, Scrap(ScrapTier.Medieval), 600, sites[0] + new IntVec3(0, 0, -3));
            var colonist = map.mapPawns.FreeColonistsSpawned.First();
            colonist.inventory.innerContainer.TryAdd(Stack(StudiedScrap(ScrapTier.Neolithic), 7), false);
            var traveller = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist, Faction.OfPlayer, forceGenerateNewPawn: true));
            traveller.inventory.innerContainer.TryAdd(Stack(Scrap(ScrapTier.Spacer), 12), false);
            var caravan = CaravanMaker.MakeCaravan(new[] { traveller }, Faction.OfPlayer, map.Tile, true);
            ScaleField.SetValue(Current.Game.GetComponent<ScrapScaleComponent>(), SettingsLoad.OldScrapScale);
            ctx.Remember("packPawn", colonist.thingIDNumber.ToString());
            ctx.Remember("caravan", caravan.ID.ToString());
            ctx.Remember("newGameScale", scale.ToString());
            var w = Watched(map, ctx);
            ctx.Setup($"Developer placed 600 medieval scrap on the ground (one stack: {ground.stackCount}), 7 studied neolithic scrap in {colonist.LabelShort}'s pack and 12 spacer scrap in a caravan, " +
                $"then set the save's scrap scale from {scale} to 1 (counted {w.ground}, {w.pack}, {w.caravan})");
            yield return Wait.Reload();
            yield break;
        }
        if (index == 2)
        {
            var c = ctx.Begin(Doubled);
            var w = Watched(map, ctx);
            c.Observe("newGameScale", ctx.Recall("newGameScale")).Observe("scale", Scale).Observe("ground", w.ground).Observe("largestStack", w.largest)
                .Observe("pack", w.pack).Observe("caravan", w.caravan);
            c.Step(ctx.Recall("newGameScale") == SettingsLoad.CurrentScrapScale.ToString(), "the new game started at scrap scale 2");
            c.Step(w.ground == 1200 && w.largest <= Scrap(ScrapTier.Medieval).stackLimit, $"the 600 on the ground are 1200, no stack over the limit ({w.ground}, largest {w.largest})");
            c.Step(w.pack == 14, $"the 7 in the colonist's pack are 14 ({w.pack})");
            c.Step(w.caravan == 24, $"the 12 in the caravan are 24 ({w.caravan})");
            c.Step(Scale == SettingsLoad.CurrentScrapScale, "the save's scrap scale is now 2");
            c.Conclude();
            ctx.Remember("afterDoubling", $"{w.ground}/{w.pack}/{w.caravan}");
            yield return Wait.Reload();
            yield break;
        }
        if (index == 3)
        {
            var c = ctx.Begin(DoubledOnce);
            var w = Watched(map, ctx);
            string now = $"{w.ground}/{w.pack}/{w.caravan}";
            c.Expect("units", ctx.Recall("afterDoubling")).Observe("units", now).Observe("scale", Scale);
            c.Step(now == ctx.Recall("afterDoubling") && now == "1200/14/24", $"a second load leaves the ground, pack and caravan at {now}");
            c.Conclude();
        }
    }
}
