using System;
using System.Collections.Generic;
using System.Linq;
using LootScavenging.Model;
using RimWorld;
using RimWorldDevTests;
using Verse;
using static LootScavenging.DevTests.Fixtures;

namespace LootScavenging.DevTests;

// The smoke batch: one case per highest-value in-game check the tickets list as pending (01, 03, 04, 05, 06, 10, 11). It
// calls the mod's own functions and the game's death, recipe and research paths directly instead of waiting on pawn jobs.
// Research only ever goes up through the batch (tribal, then medieval, then the charge rifle's), so the cases run in order.
public sealed class SmokeBatch : Batch
{
    private const string P = "lootscavenging.smoke.";
    private const string Defaults = P + "progression-defaults", Fixture = P + "tribal-fixture", TribalScrap = P + "tribal-scrap",
        TribalMedicine = P + "tribal-medicine",
        Cleanup = P + "cleanup-preset", Recycling = P + "recycling", Study = P + "study", KeepOriginal = P + "keep-original",
        NoErrors = P + "no-mod-errors";

    public override string Id => "lootscavenging-smoke";
    public override int SegmentCount => 1;
    public override int MapSize => 100;

    public override IReadOnlyList<PlannedCase> Plan => new[]
    {
        new PlannedCase(Defaults, Levels.Mechanical, "a fresh config holds the Progression preset, not marked changed"),
        new PlannedCase(Fixture, Levels.Mechanical, "colony era Neolithic, revolver research unfinished, hostile pirates, no scrap"),
        new PlannedCase(TribalScrap, Levels.Mechanical, "a dead pirate's revolver becomes industrial scrap by ScrapAmounts.Units; colony and wild deaths change nothing"),
        new PlannedCase(TribalMedicine, Levels.Mechanical, "ticket 16: industrial medicine drops whole by default; scrap of its era with dropIncludesMedicine on"),
        new PlannedCase(Cleanup, Levels.Mechanical, "poor weapon deleted, good weapon kept, corpse removed, kept apparel on the ground"),
        new PlannedCase(Recycling, Levels.Mechanical, "machining table recycles three tiers, smithy no industrial, vanilla bills kept, products from the pool"),
        new PlannedCase(Study, Levels.Mechanical, "5 medieval units pay 100 points at factor 1; factor 0 and industrial scrap pay nothing"),
        new PlannedCase(KeepOriginal, Levels.Mechanical, "out-of-reach charge rifle unfamiliar and not equippable until ChargedShot finishes"),
        new PlannedCase(NoErrors, Levels.Mechanical, "no error naming the mod logged during the batch"),
    };

    public override IEnumerable<Wait> Segment(int index, BatchContext ctx)
    {
        long modErrorsAtStart = CaseErrors.ModMark, errorsAtStart = CaseErrors.Mark;
        ModHarness.SaveSettings();
        var area = Kit.Area(ctx, 50, 12);
        yield return Wait.Ticks(1);

        // Cells along the area, far enough apart that one death's drops never land near another's.
        var sites = Enumerable.Range(0, 8).Select(i => new IntVec3(area.minX + 3 + i * 6, 0, area.CenterCell.z)).ToList();

        // ---- 1. Progression defaults ---------------------------------------------------------------------------------------
        {
            var c = ctx.Begin(Defaults);
            var s = Settings;
            c.Observe("activePreset", s.activePreset).Observe("dropMode", s.values.rules.dropMode)
                .Observe("hostile", s.values.rules.hostile).Observe("corpses.humanlike", s.values.corpses.humanlike);
            c.Step(s.activePreset == PresetId.Progression, "the settings page shows the Progression preset");
            c.Step(!s.PresetChanged, "the preset is not marked changed");
            c.Step(s.values.SameAs(Presets.Build(PresetId.Progression)), "every value equals the Progression preset");
            c.Step(s.values.rules.dropMode == DropMode.ScrapOutOfReach, "out-of-reach gear becomes scrap");
            c.Conclude();
        }

        // ---- Fixture: a tribal colony ----------------------------------------------------------------------------------------
        var map = ctx.Map;
        var pirates = Pirates;
        var revolver = Kit.Def("Gun_Revolver");
        var medicine = Kit.Def("MedicineIndustrial");
        {
            MakeTribal(ctx);
            var c = ctx.Begin(Fixture);
            var era = EraReader.ItemEra(revolver, pirates);
            c.Observe("colonyEra", EraReader.ColonyEra()).Observe("revolverEra", era.Era).Observe("revolverOutOfReach", era.OutOfReach)
                .Observe("scrapOnMap", ScrapOnMap(map));
            c.Step(EraReader.ColonyEra() == Era.Neolithic, "the colony is in the neolithic era");
            c.Step(!Project("Gunsmithing").IsFinished, "gunsmithing is not researched");
            c.Step(pirates != null && pirates.HostileTo(Faction.OfPlayer), "a pirate faction exists and is hostile");
            c.Step(era.Era == Era.Industrial && era.OutOfReach, "a pirate's revolver is industrial and out of reach");
            var medEra = EraReader.ItemEra(medicine, pirates);
            c.Observe("medicineEra", medEra.Era).Observe("medicineOutOfReach", medEra.OutOfReach);
            c.Step(medEra.Era == Era.Industrial && medEra.OutOfReach, "a pirate's industrial medicine is industrial and out of reach");
            c.Step(ScrapOnMap(map) == 0, "no scrap is on the map yet");
            c.Conclude();
        }

        // ---- 2. Tribal scrap -------------------------------------------------------------------------------------------------
        if (!ctx.SkipUnlessPassed(TribalScrap, Fixture, Defaults))
        {
            var c = ctx.Begin(TribalScrap);
            var industrial = Scrap(ScrapTier.Industrial);

            var pirate = Spawn(ctx, DefDatabase<PawnKindDef>.GetNamed("Mercenary_Gunner"), pirates, sites[0]);
            var gun = Arm(pirate, revolver, null);
            pirate.inventory.innerContainer.TryAdd(Stack(ThingDefOf.Silver, 50));
            pirate.inventory.innerContainer.TryAdd(Stack(ThingDefOf.Steel, 30));
            c.Subject(pirate);
            float value = gun.MarketValue;
            int expected = ScrapAmounts.Units(value, 1);
            int silver = Kit.CountOnMap(map, ThingDefOf.Silver), steel = Kit.CountOnMap(map, ThingDefOf.Steel);
            var before = Kit.StackIds(map, industrial);
            c.Note($"A pirate with only a revolver (market value {value:0.#}), 50 silver and 30 steel dies");
            pirate.Kill(null);
            var corpse = pirate.Corpse;
            var near = corpse?.Position ?? sites[0];
            var made = Kit.NewStacks(map, industrial, before).ToList();
            int units = made.Sum(t => t.stackCount);
            c.Expect("industrialScrap", expected).Observe("industrialScrap", units)
                .Observe("scrapDistances", string.Join(",", made.Select(t => t.Position.DistanceTo(near).ToString("0.#"))));
            c.Step(gun.Destroyed, "the revolver is gone");
            c.Step(units == expected, $"industrial scrap appears: {units} units, expected {expected}");
            c.Step(made.Count > 0 && made.All(t => t.Position.DistanceTo(near) <= 4f), "the scrap lies near the corpse");
            c.Step(corpse != null && corpse.Spawned, "the pirate's corpse stays (Progression keeps corpses)");
            c.Step(Kit.CountOnMap(map, ThingDefOf.Silver) == silver && Kit.CountOnMap(map, ThingDefOf.Steel) == steel,
                "the silver and steel he carried drop untouched");

            // The same gun on a colonist: colony gear is never sorted.
            var colonist = Spawn(ctx, PawnKindDefOf.Colonist, Faction.OfPlayer, sites[1]);
            var ownGun = Arm(colonist, revolver, null);
            int scrapBefore = ScrapOnMap(map);
            colonist.Kill(null);
            c.Step(!ownGun.Destroyed && ownGun.Spawned && ownGun.def == revolver && ScrapOnMap(map) == scrapBefore,
                "a colonist who dies with the same revolver drops it whole, with no scrap");

            var hare = Spawn(ctx, DefDatabase<PawnKindDef>.GetNamed("Hare"), null, sites[2]);
            scrapBefore = ScrapOnMap(map);
            hare.Kill(null);
            c.Step(hare.Corpse != null && hare.Corpse.Spawned && ScrapOnMap(map) == scrapBefore, "a wild hare dies: its corpse stays and no scrap appears");
            c.Conclude();
        }

        // ---- 2b. Medicine and the drop mode (ticket 16) ----------------------------------------------------------------------
        if (!ctx.SkipUnlessPassed(TribalMedicine, Fixture, Defaults))
        {
            var c = ctx.Begin(TribalMedicine);
            var industrial = Scrap(ScrapTier.Industrial);
            var mercenary = DefDatabase<PawnKindDef>.GetNamed("Mercenary_Gunner");
            c.Step(!Settings.values.rules.dropIncludesMedicine, "by default the drop mode leaves medicine out");

            var medic = Spawn(ctx, mercenary, pirates, sites[6]);
            var gun = Arm(medic, revolver, null);
            var kit = Stack(medicine, 3);
            medic.inventory.innerContainer.TryAdd(kit);
            c.Subject(medic);
            int expectedGun = ScrapAmounts.Units(gun.MarketValue, 1);
            int medsBefore = Kit.CountOnMap(map, medicine);
            var before = Kit.StackIds(map, industrial);
            c.Note("A pirate with a revolver and 3 industrial medicine dies (default settings)");
            medic.Kill(null);
            int gunScrap = Kit.NewStacks(map, industrial, before).Sum(t => t.stackCount);
            int medsAfter = Kit.CountOnMap(map, medicine);
            c.Expect("medicineAfter", medsBefore).Observe("medicineAfter", medsAfter)
                .Expect("scrapDefault", expectedGun).Observe("scrapDefault", gunScrap);
            c.Step(gun.Destroyed && gunScrap == expectedGun, $"the revolver becomes {gunScrap} industrial scrap (expected {expectedGun})");
            c.Step(medsAfter == medsBefore && medsBefore >= 3, "the 3 industrial medicine drop whole");

            Settings.values.rules.dropIncludesMedicine = true;
            c.Note("The player turns on \"drop mode includes medicine\"");
            var medic2 = Spawn(ctx, mercenary, pirates, sites[7]);
            var kit2 = Stack(medicine, 3);
            float medValue = kit2.MarketValue;
            medic2.inventory.innerContainer.TryAdd(kit2);
            c.Subject(medic2);
            int expectedMed = ScrapAmounts.Units(medValue, 3);
            medsBefore = Kit.CountOnMap(map, medicine);
            before = Kit.StackIds(map, industrial);
            medic2.Kill(null);
            int medScrap = Kit.NewStacks(map, industrial, before).Sum(t => t.stackCount);
            medsAfter = Kit.CountOnMap(map, medicine);
            c.Expect("scrapIncluded", expectedMed).Observe("scrapIncluded", medScrap).Observe("medicineLost", medsBefore - medsAfter);
            c.Step(medsBefore - medsAfter == 3, "a second pirate's 3 industrial medicine are gone");
            c.Step(medScrap == expectedMed, $"they become {medScrap} industrial scrap (expected {expectedMed})");
            ModHarness.RestoreSettings();
            c.Conclude();
        }

        // ---- 3. Cleanup preset -----------------------------------------------------------------------------------------------
        {
            var c = ctx.Begin(Cleanup);
            Settings.Apply(Presets.Apply(PresetId.Cleanup, Settings.Capture()));
            Settings.activePreset = PresetId.Cleanup;
            c.Note("The player picks the Cleanup preset");
            c.Step(Settings.values.rules.dropMode == DropMode.Off && Settings.values.corpses.humanlike, "Cleanup turns era handling off and removes humanlike corpses");

            var poorOwner = Spawn(ctx, DefDatabase<PawnKindDef>.GetNamed("Mercenary_Gunner"), pirates, sites[3]);
            var poor = Arm(poorOwner, revolver, QualityCategory.Poor);
            var goodOwner = Spawn(ctx, DefDatabase<PawnKindDef>.GetNamed("Mercenary_Gunner"), pirates, sites[4]);
            var good = Arm(goodOwner, revolver, QualityCategory.Good);
            var shirtDef = Kit.Def("Apparel_CollarShirt");
            var shirt = (Apparel)ThingMaker.MakeThing(shirtDef, GenStuff.DefaultStuffFor(shirtDef));
            shirt.TryGetComp<CompQuality>()?.SetQuality(QualityCategory.Excellent, ArtGenerationContext.Outsider);
            shirt.HitPoints = shirt.MaxHitPoints;
            goodOwner.apparel.Wear(shirt);
            c.Subject(poorOwner, goodOwner);

            poorOwner.Kill(null);
            goodOwner.Kill(null);
            var poorCorpse = poorOwner.Corpse;
            var goodCorpse = goodOwner.Corpse;
            c.Observe("poorDestroyed", poor.Destroyed).Observe("goodSpawned", good.Spawned).Observe("shirtSpawned", shirt.Spawned)
                .Observe("corpsesLeft", (poorCorpse is { Destroyed: false } ? 1 : 0) + (goodCorpse is { Destroyed: false } ? 1 : 0));
            c.Step(poor.Destroyed, "the poor revolver is deleted");
            c.Step(!good.Destroyed && good.Spawned, "the good revolver is kept on the ground");
            c.Step((poorCorpse == null || poorCorpse.Destroyed) && (goodCorpse == null || goodCorpse.Destroyed), "both pirate corpses are removed");
            c.Step(!shirt.Destroyed && shirt.Spawned && shirt.Position.DistanceTo(sites[4]) <= 4f, "the excellent shirt the corpse wore lies on the ground");
            ModHarness.RestoreSettings();
            c.Conclude();
        }

        // ---- 4. Recycling recipes --------------------------------------------------------------------------------------------
        {
            var c = ctx.Begin(Recycling);
            var machining = Kit.Def("TableMachining").AllRecipes.Select(r => r.defName).ToHashSet();
            var smithy = Kit.Def("FueledSmithy").AllRecipes.Select(r => r.defName).ToHashSet();
            var craftingSpot = Kit.Def("CraftingSpot").AllRecipes.Select(r => r.defName).ToHashSet();
            c.Observe("machiningRecycle", string.Join(",", machining.Where(n => n.StartsWith("LS_Recycle_"))))
                .Observe("smithyRecycle", string.Join(",", smithy.Where(n => n.StartsWith("LS_Recycle_"))));
            c.Step(new[] { "LS_Recycle_Industrial", "LS_Recycle_Medieval", "LS_Recycle_Neolithic" }.All(machining.Contains),
                "the machining table offers industrial, medieval and neolithic recycling");
            c.Step(!smithy.Contains("LS_Recycle_Industrial") && smithy.Contains("LS_Recycle_Medieval") && smithy.Contains("LS_Recycle_Neolithic"),
                "the smithy recycles medieval and neolithic scrap but not industrial");
            c.Step(machining.Contains("ButcherCorpseMechanoid") && machining.Contains("Make_Gun_Revolver"),
                "the machining table keeps its own bills (shred mechanoid, make revolver)");
            c.Step(craftingSpot.Contains("LS_Recycle_Neolithic") && craftingSpot.Any(n => !n.StartsWith("LS_")),
                "the crafting spot recycles neolithic scrap and keeps its own bills");

            var recipe = DefDatabase<RecipeDef>.GetNamed("LS_Recycle_Industrial");
            var tier = DefDatabase<ScrapTierDef>.AllDefsListForReading.First(d => d.tier == ScrapTier.Industrial);
            var pool = tier.pool.Select(e => e.thing).ToHashSet();
            var worker = map.mapPawns.FreeColonistsSpawned.First();
            var products = new List<Thing>();
            int empty = 0;
            for (int i = 0; i < 20; i++)
            {
                var unit = Stack(Scrap(ScrapTier.Industrial), 1);
                Rand.PushState(1000 + i);
                try
                {
                    var made = GenRecipe.MakeRecipeProducts(recipe, worker, new List<Thing> { unit }, unit, null).ToList();
                    if (made.Count == 0) empty++;
                    products.AddRange(made);
                }
                finally { Rand.PopState(); }
            }
            float total = products.Sum(t => t.def.BaseMarketValue * t.stackCount);
            c.Expect("valueOf20Units", 20 * tier.valuePerUnit)
                .Observe("valueOf20Units", total)
                .Observe("products", string.Join(", ", products.GroupBy(t => t.def.defName).Select(g => g.Sum(t => t.stackCount) + " " + g.Key)))
                .Observe("unitsWithNothing", empty);
            c.Note("Twenty single units of industrial scrap go through the recycling bill's product step, one at a time");
            c.Step(products.Count > 0 && products.All(t => pool.Contains(t.def)), "every product is a material from the industrial pool");
            c.Step(total >= 10f * tier.valuePerUnit && total <= 30f * tier.valuePerUnit, $"the twenty units return about {20 * tier.valuePerUnit} silver of materials ({total:0})");
            c.Conclude();
        }

        // ---- 5. Study --------------------------------------------------------------------------------------------------------
        {
            var c = ctx.Begin(Study);
            Kit.Research(ctx, "Smithing");
            var project = Project("ComplexClothing");
            Find.ResearchManager.SetCurrentProject(project);
            var difficulty = Find.Storyteller.difficulty;
            float savedFactor = difficulty.researchSpeedFactor;
            var scholar = Spawn(ctx, PawnKindDefOf.Colonist, Faction.OfPlayer, sites[5]);
            c.Subject(scholar);
            try
            {
                difficulty.researchSpeedFactor = 1f;
                var medieval = Scrap(ScrapTier.Medieval);
                var studyMedieval = (RecipeWorker_Study)DefDatabase<RecipeDef>.GetNamed("LS_Study_Medieval").Worker;
                ScrapStudy.TryGetWorth(medieval, out StudyWorth worth);
                c.Observe("colonyEra", EraReader.ColonyEra()).Observe("project", project.defName).Observe("pointsPerUnit", worth.PointsPerUnit);
                c.Step(ScrapStudy.CurrentProject == project && ScrapStudy.Verdict(worth, project) == StudyVerdict.Eligible,
                    "complex clothing is the current project and medieval scrap may be studied for it");

                float before = Find.ResearchManager.GetProgress(project);
                var stack = SpawnStack(map, medieval, 5, sites[5]);
                studyMedieval.Notify_IterationCompleted(scholar, new List<Thing> { stack });
                float paid = Find.ResearchManager.GetProgress(project) - before;
                c.Expect("paidFactor1", 5 * worth.PointsPerUnit).Observe("paidFactor1", paid);
                c.Step(Kit.Near(paid, 5 * worth.PointsPerUnit, 0.01f), $"studying 5 units at research factor 1 adds {5 * worth.PointsPerUnit} points ({paid})");
                c.Step(stack.Destroyed, "the 5 units are used up");

                difficulty.researchSpeedFactor = 0f;
                before = Find.ResearchManager.GetProgress(project);
                stack = SpawnStack(map, medieval, 5, sites[5]);
                var verdictZero = ScrapStudy.Verdict(worth, project);
                studyMedieval.Notify_IterationCompleted(scholar, new List<Thing> { stack });
                float after = Find.ResearchManager.GetProgress(project);
                c.Observe("verdictFactor0", verdictZero).Observe("progressFactor0", after);
                c.Step(verdictZero == StudyVerdict.NoGain, "at research factor 0 the bill does not start (no gain)");
                c.Step(after == before && !float.IsNaN(after), "and nothing is written to the project, with no NaN");
                c.Step(!stack.Destroyed && stack.stackCount == 5, "and the scrap is left as it was");
                stack.Destroy();

                difficulty.researchSpeedFactor = 1f;
                var industrial = Scrap(ScrapTier.Industrial);
                ScrapStudy.TryGetWorth(industrial, out StudyWorth high);
                var verdictHigh = ScrapStudy.Verdict(high, project);
                before = Find.ResearchManager.GetProgress(project);
                stack = SpawnStack(map, industrial, 5, sites[5]);
                ((RecipeWorker_Study)DefDatabase<RecipeDef>.GetNamed("LS_Study_Industrial").Worker)
                    .Notify_IterationCompleted(scholar, new List<Thing> { stack });
                c.Observe("verdictIndustrial", verdictHigh);
                c.Step(verdictHigh == StudyVerdict.ScrapAboveColonyEra, "industrial scrap is above the medieval colony and cannot be studied");
                c.Step(Find.ResearchManager.GetProgress(project) == before && !stack.Destroyed, "studying it pays nothing and leaves it");
                stack.Destroy();
            }
            finally { difficulty.researchSpeedFactor = savedFactor; }
            c.Conclude();
        }

        // ---- 6. Keep-original mode -------------------------------------------------------------------------------------------
        {
            var c = ctx.Begin(KeepOriginal);
            var colonist = map.mapPawns.FreeColonistsSpawned.First();
            var shot = Project("ChargedShot");
            var rifle = GenSpawn.Spawn(ThingMaker.MakeThing(Kit.Def("Gun_ChargeRifle")), sites[2] + new IntVec3(0, 0, 2), map);
            c.Subject(colonist);
            c.Step(!shot.IsFinished, "charged shot is not researched");
            c.Step(EquipmentUtility.CanEquip(rifle, colonist), "in the default scrap mode a colonist can equip the charge rifle (control)");

            Settings.values.rules.dropMode = DropMode.KeepOriginal;
            Unfamiliar.ResetCache();
            c.Note("The player switches the drop mode to keep originals");
            bool equip = EquipmentUtility.CanEquip(rifle, colonist, out string reason);
            string tag = "LootScavenging_UnfamiliarLabel".Translate().Resolve();
            c.Observe("label", rifle.LabelNoCount).Observe("cantReason", reason);
            c.Step(Unfamiliar.IsUnfamiliar(rifle), "the charge rifle is unfamiliar");
            c.Step(rifle.LabelNoCount.Contains(tag), "its label says so");
            c.Step(!equip && !EquipmentUtility.CanEquip(rifle, colonist), "a colonist cannot equip it");
            c.Step(reason == "LootScavenging_CannotEquipUnfamiliar".Translate().Resolve(), "the equip option gives the unfamiliar reason");

            Find.ResearchManager.FinishProject(shot, doCompletionDialog: false, doCompletionLetter: false);
            c.Note("Charged shot research finishes");
            c.Step(!Unfamiliar.IsUnfamiliar(rifle) && EquipmentUtility.CanEquip(rifle, colonist), "the charge rifle is familiar and equippable");
            ModHarness.RestoreSettings();
            c.Conclude();
        }

        // ---- 7. No mod errors ------------------------------------------------------------------------------------------------
        {
            var c = ctx.Begin(NoErrors);
            long modErrors = CaseErrors.ModMark - modErrorsAtStart;
            c.Observe("modErrors", modErrors).Observe("allErrors", CaseErrors.Mark - errorsAtStart);
            c.Step(modErrors == 0, "no error naming Loot Scavenging was logged during the batch");
            c.Conclude();
        }
    }
}
