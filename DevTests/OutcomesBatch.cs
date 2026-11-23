using System;
using System.Collections.Generic;
using System.Linq;
using LootScavenging.Model;
using RimWorld;
using RimWorldDevTests;
using Verse;
using static LootScavenging.DevTests.Fixtures;

namespace LootScavenging.DevTests;

// What an item becomes at death (tickets 01, 02, 03, 08, 09, 10, 15): one hostile per check, killed at its own site in a tribal
// colony, with the outcome read from what lies near the corpse. Everything happens between ticks, so nothing else acts.
public sealed class OutcomesBatch : Batch
{
    private const string P = "lootscavenging.outcomes.";
    private const string DeleteKeepsApparel = P + "delete-weapon-keep-apparel", Mech = P + "mechanoid-weapon", Fallback = P + "era-fallback",
        Amounts = P + "scrap-amounts-and-modes", Raid = P + "large-raid-piles", Quest = P + "quest-weapon-protected", Animal = P + "animal-corpse-removed",
        Degrade = P + "degrade", ItemBased = P + "disassembly-item-based", EraTypical = P + "disassembly-era-typical", FullStacks = P + "disassembly-full-stacks",
        Charges = P + "ejected-charges", Ammo = P + "ammunition-drop-mode", Sample = P + "recycle-sample", Pools = P + "tier-table", NoErrors = P + "no-mod-errors";

    public override string Id => "lootscavenging-outcomes";
    public override int SegmentCount => 1;
    public override int MapSize => 120;

    public override IReadOnlyList<PlannedCase> Plan => new[]
    {
        new PlannedCase(DeleteKeepsApparel, Levels.Mechanical, "weapon rule Delete with drop mode Off: weapon gone, apparel stays on the corpse"),
        new PlannedCase(Mech, Levels.Mechanical, "a hostile mechanoid's weapon becomes scrap of its era near the corpse"),
        new PlannedCase(Fallback, Levels.Mechanical, "gear with no research and no tech level follows each era fallback; components fallback disassembles it"),
        new PlannedCase(Amounts, Levels.Mechanical, "stone knife and charge rifle amounts match ScrapAmounts.Units; scrap everything scraps in-reach gear; Off keeps out-of-reach gear"),
        new PlannedCase(Raid, Levels.Mechanical, "ten pirates killed in a cluster leave fewer piles than bodies"),
        new PlannedCase(Quest, Levels.Mechanical, "a quest-tagged out-of-reach weapon stays whole"),
        new PlannedCase(Animal, Levels.Mechanical, "with animal corpse removal on, a wild hare's corpse is removed"),
        new PlannedCase(Degrade, Levels.Mechanical, "degraded hit points lie in the range; quality drops one step only with the option"),
        new PlannedCase(ItemBased, Levels.Mechanical, "item-based yield is the cost list times efficiency (0.5 and 1.0), forbidden"),
        new PlannedCase(EraTypical, Levels.Mechanical, "era-typical yield comes from the item's tier pool"),
        new PlannedCase(FullStacks, Levels.Mechanical, "a yield above the stack limit arrives as full stacks with nothing lost"),
        new PlannedCase(Charges, Levels.Mechanical, "a smokepop belt's chemfuel is ejected for scrap, delete and disassemble with the setting on, not with it off"),
        new PlannedCase(Ammo, Levels.Mechanical, "out-of-reach ammunition drops whole by default; with dropIncludesAmmunition on, a stack becomes scrap rounded once per stack (needs an ammo mod)"),
        new PlannedCase(Sample, Levels.Mechanical, "200 industrial units recycle to about 2200 silver of pool materials, components and chemfuel included; studied scrap pays half"),
        new PlannedCase(Pools, Levels.Mechanical, "every tier def loads with no config errors"),
        new PlannedCase(NoErrors, Levels.Mechanical, "no error naming the mod logged during the batch"),
    };

    public override IEnumerable<Wait> Segment(int index, BatchContext ctx)
    {
        long modErrorsAtStart = CaseErrors.ModMark;
        ModHarness.SaveSettings();
        var area = Kit.Area(ctx, 84, 22);
        yield return Wait.Ticks(1);
        var map = ctx.Map;
        MakeTribal(ctx);
        var sites = new Queue<IntVec3>(Enumerable.Range(0, 3).SelectMany(row => Sites(area, 12, 7).Select(c => c + new IntVec3(0, 0, -7 + row * 7))));
        var revolver = Kit.Def("Gun_Revolver");
        var industrial = Scrap(ScrapTier.Industrial);

        // Kills a fresh pirate at the next site and returns where its body lies.
        IntVec3 KillPirate(Action<Pawn> gear, out Pawn pawn)
        {
            var cell = sites.Dequeue();
            pawn = Spawn(ctx, Mercenary, Pirates, cell);
            gear(pawn);
            pawn.Kill(null);
            return pawn.Corpse?.Position ?? cell;
        }

        // ---- 01: Delete weapon, apparel kept ------------------------------------------------------------------------------
        {
            var c = ctx.Begin(DeleteKeepsApparel);
            ModHarness.RestoreSettings();
            Rules.dropMode = DropMode.Off;
            Rules.weapons = new CategoryRule(Outcome.Delete);
            c.Note("Weapon rule set to Delete, drop mode Off");
            ThingWithComps gun = null;
            Apparel shirt = null;
            KillPirate(p => { gun = Arm(p, revolver, QualityCategory.Normal); shirt = Dress(p, Kit.Def("Apparel_CollarShirt"), QualityCategory.Normal); }, out var pirate);
            c.Step(gun.Destroyed, "the revolver is removed");
            c.Step(!shirt.Destroyed && pirate.Corpse?.InnerPawn?.apparel?.Contains(shirt) == true, "the shirt stays on the corpse");
            c.Conclude();
        }

        // ---- 01: Mechanoid weapon --------------------------------------------------------------------------------------------
        {
            var c = ctx.Begin(Mech);
            ModHarness.RestoreSettings();
            var cell = sites.Dequeue();
            var mech = PawnGenerator.GeneratePawn(new PawnGenerationRequest(DefDatabase<PawnKindDef>.GetNamed("Mech_Lancer"), Faction.OfMechanoids, forceGenerateNewPawn: true));
            GenSpawn.Spawn(mech, cell, map);
            var weapon = mech.equipment?.Primary;
            if (!Kit.Require(ctx, Mech, (weapon != null, "the lancer spawned with a weapon"))) { }
            else
            {
                var era = EraReader.ItemEra(weapon.def, mech.Faction);
                var tierDef = Scrap(ScrapTiers.TierFor(era.Era, EraReader.ColonyEra()));
                int expected = ScrapAmounts.Units(weapon.MarketValue, 1);
                var before = Kit.StackIds(map, tierDef);
                mech.Kill(null);
                var made = NewNear(map, tierDef, before, mech.Corpse?.Position ?? cell);
                c.Observe("weapon", weapon.def.defName).Observe("era", era.Era).Observe("outOfReach", era.OutOfReach).Expect("units", expected).Observe("units", Units(made)).Observe("scrapDef", tierDef.defName);
                c.Step(era.OutOfReach, $"the {weapon.def.label} is out of reach for the tribal colony");
                c.Step(weapon.Destroyed && Units(made) == expected, $"it becomes {Units(made)} {tierDef.defName} beside the wreck (expected {expected})");
                c.Conclude();
            }
        }

        // ---- 02/08: era fallback and the components fallback -----------------------------------------------------------------
        {
            var noEra = DefDatabase<ThingDef>.AllDefsListForReading.Where(d => EraReader.IsGear(d) && d.techLevel == TechLevel.Undefined && !d.IsCorpse
                && EraReader.ItemEra(d, null, EraFallback.ColonyEra).Source == EraSource.ColonyFallback).ToList();
            if (noEra.Count == 0)
                ctx.Begin(Fallback).NotApplicable("no loaded weapon or apparel has neither research nor a tech level");
            else
            {
                var c = ctx.Begin(Fallback);
                var def = noEra.FirstOrDefault(d => d.IsRangedWeapon || d.IsMeleeWeapon) ?? noEra[0];
                var colony = EraReader.ItemEra(def, null, EraFallback.ColonyEra);
                var components = EraReader.ItemEra(def, null, EraFallback.Components);
                var carrier = EraReader.ItemEra(def, Pirates, EraFallback.CarrierFaction);
                c.Observe("def", def.defName).Observe("candidates", noEra.Count).Observe("colonyFallback", colony.Era).Observe("componentsFallback", components.Source)
                    .Observe("carrierFallback", carrier.Era + "/" + carrier.Source);
                c.Step(colony.Era == EraReader.ColonyEra(), "the colony-era fallback gives the colony's era");
                c.Step(components.BreakIntoComponents, "the components fallback marks it for components");
                c.Step(carrier.Era == (Era)Pirates.def.techLevel, "the carrier fallback gives the pirates' era");
                if (def.IsWeapon && def.equipmentType == EquipmentType.Primary)
                {
                    ModHarness.RestoreSettings();
                    Settings.values.eraFallback = EraFallback.Components;
                    ThingWithComps gun = null;
                    int units = 0;
                    var before = Kit.StackIds(map, ThingDefOf.ComponentIndustrial);
                    var at = KillPirate(p => { gun = Arm(p, def, null); units = ScrapAmounts.Units(gun.MarketValue, 1); }, out _);
                    var comps = NewNear(map, ThingDefOf.ComponentIndustrial, before, at);
                    c.Expect("components", Disassembly.ComponentsFor(units)).Observe("components", Units(comps));
                    c.Step(gun.Destroyed && Units(comps) == Disassembly.ComponentsFor(units), $"with the components fallback a pirate's {def.label} breaks into {Units(comps)} components");
                }
                else c.Note($"{def.defName} is not a primary weapon, so the components kill was not run");
                c.Conclude();
            }
        }

        // ---- 03: amounts and drop modes ----------------------------------------------------------------------------------
        {
            var c = ctx.Begin(Amounts);
            ModHarness.RestoreSettings();
            var rifleDef = Kit.Def("Gun_ChargeRifle");
            var spacer = Scrap(ScrapTier.Spacer);
            ThingWithComps rifle = null;
            int rifleUnits = 0;
            var before = Kit.StackIds(map, spacer);
            var at = KillPirate(p => { rifle = Arm(p, rifleDef, QualityCategory.Normal); rifleUnits = ScrapAmounts.Units(rifle.MarketValue, 1); }, out _);
            var made = NewNear(map, spacer, before, at);
            c.Expect("chargeRifleUnits", rifleUnits).Observe("chargeRifleUnits", Units(made));
            c.Step(rifle.Destroyed && Units(made) == rifleUnits, $"a charge rifle becomes {Units(made)} spacer scrap (expected {rifleUnits})");

            Rules.dropMode = DropMode.ScrapEverything;
            var neolithic = Scrap(ScrapTier.Neolithic);
            var knifeDef = Kit.Def("MeleeWeapon_Knife");
            ThingWithComps knife = null;
            int knifeUnits = 0;
            bool inReach = !EraReader.ItemEra(knifeDef, Pirates).OutOfReach;
            before = Kit.StackIds(map, neolithic);
            at = KillPirate(p =>
            {
                knife = (ThingWithComps)ThingMaker.MakeThing(knifeDef, Kit.Def("BlocksGranite"));
                knife.TryGetComp<CompQuality>()?.SetQuality(QualityCategory.Normal, ArtGenerationContext.Outsider);
                p.equipment.AddEquipment(knife);
                knifeUnits = ScrapAmounts.Units(knife.MarketValue, 1);
            }, out _);
            made = NewNear(map, neolithic, before, at);
            c.Expect("stoneKnifeUnits", knifeUnits).Observe("stoneKnifeUnits", Units(made)).Observe("knifeInReach", inReach);
            c.Step(inReach && knife.Destroyed && Units(made) == knifeUnits, $"with scrap everything an in-reach stone knife becomes {Units(made)} neolithic scrap (expected {knifeUnits})");

            Rules.dropMode = DropMode.Off;
            ThingWithComps kept = null;
            int scrapBefore = ScrapOnMap(map);
            KillPirate(p => kept = Arm(p, rifleDef, QualityCategory.Normal), out _);
            c.Step(!kept.Destroyed && kept.Spawned && ScrapOnMap(map) == scrapBefore, "with drop mode Off an out-of-reach charge rifle drops whole");
            c.Conclude();
        }

        // ---- 03: a large raid --------------------------------------------------------------------------------------------
        {
            var c = ctx.Begin(Raid);
            ModHarness.RestoreSettings();
            var centre = sites.Dequeue();
            sites.Dequeue();
            var before = Kit.StackIds(map, industrial);
            var raiders = new List<Pawn>();
            foreach (var cell in GenRadial.RadialCellsAround(centre, 2.5f, true).Where(x => x.Standable(map)).Take(10))
            {
                var p = Spawn(ctx, Mercenary, Pirates, cell);
                Arm(p, revolver, QualityCategory.Normal);
                raiders.Add(p);
            }
            int expected = raiders.Sum(p => ScrapAmounts.Units(p.equipment.Primary.MarketValue, 1));
            foreach (var p in raiders) p.Kill(null);
            var made = Kit.NewStacks(map, industrial, before).ToList();
            c.Observe("bodies", raiders.Count).Observe("piles", made.Count).Expect("units", expected).Observe("units", Units(made));
            c.Step(Units(made) == expected, $"the raid's revolvers become {Units(made)} industrial scrap (expected {expected})");
            c.Step(made.Count < raiders.Count, $"{raiders.Count} bodies leave {made.Count} piles");
            c.Conclude();
        }

        // ---- 09: quest-tagged weapon -------------------------------------------------------------------------------------
        {
            var c = ctx.Begin(Quest);
            ModHarness.RestoreSettings();
            ThingWithComps gun = null;
            KillPirate(p => { gun = Arm(p, revolver, QualityCategory.Normal); gun.questTags = new List<string> { "LootTest" }; }, out _);
            c.Step(!gun.Destroyed && gun.Spawned, "a quest-tagged out-of-reach revolver drops whole");
            c.Conclude();
        }

        // ---- 10: animal corpse removal -----------------------------------------------------------------------------------
        {
            var c = ctx.Begin(Animal);
            ModHarness.RestoreSettings();
            Settings.values.corpses.animal = true;
            var hare = Spawn(ctx, DefDatabase<PawnKindDef>.GetNamed("Hare"), null, sites.Dequeue());
            hare.Kill(null);
            c.Step(hare.Corpse == null || hare.Corpse.Destroyed, "with animal corpse removal on, the wild hare's corpse is removed");
            c.Conclude();
        }

        // ---- 08: degrade -------------------------------------------------------------------------------------------------
        {
            var c = ctx.Begin(Degrade);
            ModHarness.RestoreSettings();
            Rules.dropMode = DropMode.Off;
            Rules.weapons = new CategoryRule(Outcome.Degrade);
            var guns = new List<ThingWithComps>();
            var cell = sites.Dequeue();
            for (int i = 0; i < 8; i++)
            {
                var p = Spawn(ctx, Mercenary, Pirates, cell + new IntVec3(i % 4, 0, i / 4));
                guns.Add(Arm(p, revolver, QualityCategory.Normal));
                p.Kill(null);
            }
            int max = guns[0].MaxHitPoints;
            int lo = Math.Max(1, (int)Math.Round(max * Rules.degradeMin, MidpointRounding.AwayFromZero));
            int hi = (int)Math.Round(max * Rules.degradeMax, MidpointRounding.AwayFromZero);
            c.Expect("hitPoints", $"{lo}..{hi} of {max}").Observe("hitPoints", string.Join(",", guns.Select(g => g.HitPoints)));
            c.Step(guns.All(g => !g.Destroyed && g.HitPoints >= lo && g.HitPoints <= hi), "eight degraded revolvers all land inside the range");
            c.Step(guns.All(g => g.TryGetQuality(out var q) && q == QualityCategory.Normal), "and keep their quality with the option off");

            Rules.degradeLowersQuality = true;
            ThingWithComps lowered = null;
            KillPirate(p => lowered = Arm(p, revolver, QualityCategory.Normal), out _);
            lowered.TryGetQuality(out var after);
            c.Observe("qualityWithOption", after);
            c.Step(after == QualityCategory.Poor, "with the option on a normal revolver becomes poor");
            c.Conclude();
        }

        // ---- 08: item-based disassembly --------------------------------------------------------------------------------
        {
            var c = ctx.Begin(ItemBased);
            var costs = revolver.CostList.Where(x => x.count > 0).ToList();
            c.Observe("costList", string.Join(", ", costs.Select(x => x.count + " " + x.thingDef.defName)));
            foreach (float efficiency in new[] { 0.5f, 1f })
            {
                ModHarness.RestoreSettings();
                Rules.dropMode = DropMode.Off;
                Rules.weapons = new CategoryRule(Outcome.Disassemble);
                Rules.disassemblyYield = DisassemblyYield.ItemBased;
                Rules.disassemblyEfficiency = efficiency;
                var befores = costs.ToDictionary(x => x.thingDef, x => Kit.StackIds(map, x.thingDef));
                ThingWithComps gun = null;
                var at = KillPirate(p => gun = Arm(p, revolver, QualityCategory.Normal), out _);
                foreach (var cost in costs)
                {
                    var made = NewNear(map, cost.thingDef, befores[cost.thingDef], at);
                    double exact = cost.count * efficiency;
                    c.Expect($"{cost.thingDef.defName}@{efficiency}", exact).Observe($"{cost.thingDef.defName}@{efficiency}", Units(made));
                    c.Step(Units(made) == (int)Math.Floor(exact) || Units(made) == (int)Math.Ceiling(exact),
                        $"at efficiency {efficiency} the revolver returns {Units(made)} {cost.thingDef.label} (cost {cost.count})");
                    c.Step(made.All(t => t.IsForbidden(Faction.OfPlayer)), $"the {cost.thingDef.label} is forbidden");
                }
                c.Step(gun.Destroyed, $"the revolver is used up at efficiency {efficiency}");
            }
            c.Conclude();
        }

        // ---- 08: era-typical disassembly -------------------------------------------------------------------------------
        {
            var c = ctx.Begin(EraTypical);
            ModHarness.RestoreSettings();
            Rules.dropMode = DropMode.Off;
            Rules.weapons = new CategoryRule(Outcome.Disassemble);
            Rules.disassemblyYield = DisassemblyYield.EraTypical;
            var pool = TierDef(ScrapTier.Industrial).pool.Select(e => e.thing).ToList();
            // Every material any tier's pool can return, so a product from the wrong pool would show too.
            var befores = DefDatabase<ScrapTierDef>.AllDefsListForReading.SelectMany(t => t.pool).Select(e => e.thing).Where(d => d != null).Distinct()
                .ToDictionary(d => d, d => Kit.StackIds(map, d));
            // One revolver is one or two scrap units, and a unit whose pick costs more than the unit's value (a component) returns
            // nothing with probability 1 - value/price, so a single revolver can yield nothing by chance. The pirate also carries
            // 20 revolvers, and the chance that every unit comes back empty is computed from the pool below. A weapon carried in
            // the inventory is judged by the "other" inventory rule, so that rule disassembles too, and the pack is emptied first
            // so nothing but revolvers is disassembled.
            const int carried = 20;
            Rules.other = new CategoryRule(Outcome.Disassemble);
            var revolversBefore = Kit.StackIds(map, revolver);
            var tierDef = TierDef(ScrapTier.Industrial);
            double totalWeight = tierDef.pool.Where(e => e.thing != null && e.weight > 0f && e.thing.BaseMarketValue > 0f).Sum(e => (double)e.weight);
            double emptyPerUnit = tierDef.pool.Where(e => e.thing != null && e.weight > 0f && e.thing.BaseMarketValue > 0f)
                .Sum(e => e.weight / totalWeight * Math.Max(0.0, 1.0 - tierDef.valuePerUnit / e.thing.BaseMarketValue));
            double emptyChance = Math.Pow(emptyPerUnit, carried + 1);
            c.Observe("emptyPerUnit", emptyPerUnit.ToString("0.000")).Observe("emptyChance", emptyChance.ToString("0.0e0"));
            c.Step(emptyChance < 1e-9, $"{carried + 1} revolvers leave an empty yield with chance {emptyChance:0.0e0}");
            var at = KillPirate(p =>
            {
                p.inventory.innerContainer.ClearAndDestroyContents();
                Arm(p, revolver, QualityCategory.Normal);
                for (int i = 0; i < carried; i++)
                    p.inventory.innerContainer.TryAdd(Make(revolver, QualityCategory.Normal));
            }, out var pirate);
            var made = befores.SelectMany(kv => NewNear(map, kv.Key, kv.Value, at, 7f)).Where(t => !(t is Corpse)).ToList();
            int whole = NewNear(map, revolver, revolversBefore, at, 7f).Count + (pirate.Corpse?.InnerPawn?.inventory?.innerContainer.Count(t => t.def == revolver) ?? 0);
            c.Observe("products", Describe(made)).Observe("revolversLeft", whole);
            c.Step(whole == 0, $"all {carried + 1} revolvers are disassembled, none left whole");
            c.Step(made.Count > 0 && made.All(t => pool.Contains(t.def)), "era-typical revolvers return only industrial pool materials");
            c.Conclude();
        }

        // ---- 08: full stacks ---------------------------------------------------------------------------------------------
        {
            var c = ctx.Begin(FullStacks);
            ModHarness.RestoreSettings();
            Rules.dropMode = DropMode.Off;
            Rules.medicine = new CategoryRule(Outcome.Disassemble);
            Rules.disassemblyYield = DisassemblyYield.ItemBased;
            Rules.disassemblyEfficiency = 1f;
            // Industrial medicine has a cost list (cloth 3 a kit); 30 kits make 90 cloth, above cloth's stack limit of 75.
            var componentDef = Kit.Def("MedicineIndustrial");
            var cost = componentDef.CostList?.OrderByDescending(x => x.count).FirstOrDefault();
            if (!Kit.Require(ctx, FullStacks, (cost != null, "industrial medicine has a cost list"))) { }
            else
            {
                int expected = 30 * cost.count;
                var before = Kit.StackIds(map, cost.thingDef);
                var at = KillPirate(p => p.inventory.innerContainer.TryAdd(Stack(componentDef, 30)), out _);
                var made = NewNear(map, cost.thingDef, before, at, 7f);
                c.Expect(cost.thingDef.defName, expected).Observe(cost.thingDef.defName, Units(made)).Observe("stacks", string.Join(",", made.Select(t => t.stackCount)))
                    .Observe("stackLimit", cost.thingDef.stackLimit);
                c.Step(expected > cost.thingDef.stackLimit, $"the yield ({expected}) is above the stack limit ({cost.thingDef.stackLimit})");
                c.Step(Units(made) == expected, $"30 industrial medicine return {Units(made)} {cost.thingDef.label}, nothing lost");
                c.Step(made.All(t => t.stackCount <= cost.thingDef.stackLimit) && made.Count >= (expected + cost.thingDef.stackLimit - 1) / cost.thingDef.stackLimit,
                    "in stacks no larger than the limit");
                c.Conclude();
            }
        }

        // ---- 08: ejected charges -----------------------------------------------------------------------------------------
        {
            var c = ctx.Begin(Charges);
            var belt = Kit.Def("Apparel_SmokepopBelt");
            var chemfuel = ThingDefOf.Chemfuel;
            int costChemfuel = belt.CostList?.Where(x => x.thingDef == chemfuel).Sum(x => x.count) ?? 0;
            int Run(string label, Action setup, bool eject, out bool destroyed)
            {
                ModHarness.RestoreSettings();
                setup();
                Rules.ejectCharges = eject;
                var before = Kit.StackIds(map, chemfuel);
                Apparel worn = null;
                var at = KillPirate(p => worn = Dress(p, belt), out _);
                destroyed = worn.Destroyed;
                int got = Units(NewNear(map, chemfuel, before, at));
                c.Observe(label, got);
                return got;
            }
            int scrapOn = Run("scrapEjectOn", () => { }, true, out bool d1);
            int scrapOff = Run("scrapEjectOff", () => { }, false, out bool d2);
            int deleteOn = Run("deleteEjectOn", () => { Rules.dropMode = DropMode.Off; Rules.apparel = new CategoryRule(Outcome.Delete); }, true, out bool d3);
            int deleteOff = Run("deleteEjectOff", () => { Rules.dropMode = DropMode.Off; Rules.apparel = new CategoryRule(Outcome.Delete); }, false, out bool d4);
            int disOn = Run("disassembleEjectOn", () =>
            {
                Rules.dropMode = DropMode.Off; Rules.apparel = new CategoryRule(Outcome.Disassemble);
                Rules.disassemblyYield = DisassemblyYield.ItemBased; Rules.disassemblyEfficiency = 1f;
            }, true, out bool d5);
            c.Observe("beltCostChemfuel", costChemfuel);
            c.Step(d1 && d2 && d3 && d4 && d5, "each belt is used up");
            c.Step(scrapOn == 10 && scrapOff == 0, $"scrapped: 10 chemfuel ejected with the setting on ({scrapOn}), none off ({scrapOff})");
            c.Step(deleteOn == 10 && deleteOff == 0, $"deleted: 10 ejected on ({deleteOn}), none off ({deleteOff})");
            c.Step(disOn == 10 + costChemfuel, $"disassembled: 10 ejected plus {costChemfuel} from the cost list ({disOn})");
            c.Conclude();
        }

        // ---- ammunition and the drop mode ----------------------------------------------------------------------------------
        {
            // The mod recognises ammo by an item category with "Ammo" in its def name, up the category tree. Vanilla has none,
            // so this needs an ammo mod such as Progression: Ammunition (--mods compat-progression).
            static bool NamesAmmo(ThingCategoryDef cat) => cat.defName.IndexOf("Ammo", StringComparison.OrdinalIgnoreCase) >= 0;
            static bool IsAmmo(ThingDef def) => def.thingCategories != null && def.thingCategories.Any(cat => NamesAmmo(cat) || cat.Parents.Any(NamesAmmo));
            var ammoDefs = DefDatabase<ThingDef>.AllDefs.Where(d => d.category == ThingCategory.Item && d.stackLimit > 1 && !d.MadeFromStuff && IsAmmo(d)).ToList();
            var ammo = ammoDefs.Where(d => EraReader.ItemEra(d, Pirates).OutOfReach).OrderBy(d => d.BaseMarketValue).FirstOrDefault();
            if (ammo == null)
                ctx.Begin(Ammo).EnvironmentUnavailable(ammoDefs.Count == 0
                    ? "no ammunition is loaded; run with an ammo mod (--mods compat-progression)"
                    : "no loaded ammunition is out of reach of the tribal colony: " + string.Join(", ", ammoDefs.Select(d => d.defName)));
            else
            {
                var c = ctx.Begin(Ammo);
                ModHarness.RestoreSettings();
                int count = Math.Min(ammo.stackLimit, 50);
                var era = EraReader.ItemEra(ammo, Pirates);
                var tierDef = Scrap(ScrapTiers.TierFor(era.Era, EraReader.ColonyEra()));
                c.Observe("ammoDef", ammo.defName).Observe("ammoEra", era.Era).Observe("loadedAmmo", string.Join(", ", ammoDefs.Select(d => d.defName)));
                c.Step(!Rules.dropIncludesAmmunition && Rules.dropMode == DropMode.ScrapOutOfReach,
                    "by default the drop mode is out-of-reach and leaves ammunition out");

                var before = Kit.StackIds(map, ammo);
                var scrapBefore = Kit.StackIds(map, tierDef);
                var at = KillPirate(p => p.inventory.innerContainer.TryAdd(Stack(ammo, count)), out _);
                int whole = Units(NewNear(map, ammo, before, at)), scrapDefault = Units(NewNear(map, tierDef, scrapBefore, at));
                c.Expect("ammoDroppedDefault", count).Observe("ammoDroppedDefault", whole).Observe("scrapDefault", scrapDefault);
                c.Step(whole == count && scrapDefault == 0, $"a pirate's {count} {ammo.label} drop whole ({whole}), no scrap ({scrapDefault})");

                Rules.dropIncludesAmmunition = true;
                c.Note("The player turns on \"drop mode includes ammunition\"");
                var stack = Stack(ammo, count);
                int expected = ScrapAmounts.AmmoUnits(stack.MarketValue, count);
                int perItem = ScrapAmounts.Units(stack.MarketValue, count);
                before = Kit.StackIds(map, ammo);
                scrapBefore = Kit.StackIds(map, tierDef);
                at = KillPirate(p => p.inventory.innerContainer.TryAdd(stack), out _);
                int left = Units(NewNear(map, ammo, before, at)), scrap = Units(NewNear(map, tierDef, scrapBefore, at));
                c.Expect("scrapIncluded", expected).Observe("scrapIncluded", scrap).Observe("ammoLeft", left).Observe("perItemRounding", perItem);
                c.Step(stack.Destroyed && left == 0, $"a second pirate's {count} {ammo.label} are gone ({left} left)");
                c.Step(scrap == expected, $"they become {scrap} {tierDef.defName}, rounded once per stack (expected {expected}; per-item rounding gives {perItem})");
                ModHarness.RestoreSettings();
                c.Conclude();
            }
        }

        // ---- 04/15: recycling sample -------------------------------------------------------------------------------------
        {
            var c = ctx.Begin(Sample);
            var tier = TierDef(ScrapTier.Industrial);
            var pool = tier.pool.Where(e => e.thing != null).Select(e => new PoolEntry<ThingDef>(e.thing, e.weight)).ToList();
            var expected = LootPool.Expected(pool, tier.valuePerUnit, 200, t => t.BaseMarketValue);
            List<Thing> Recycle(ThingDef def, int? seed)
            {
                var scrap = (ScrapThing)Stack(def, 200);
                if (seed.HasValue) Rand.PushState(seed.Value);
                try { return scrap.SmeltProducts(1f).ToList(); }
                finally { if (seed.HasValue) Rand.PopState(); }
            }
            var seeded = Recycle(tier.scrap, 1);
            var unseeded = Recycle(tier.scrap, null);
            var studied = Recycle(tier.studiedScrap, 2);
            int Count(List<Thing> l, ThingDef d) => l.Where(t => t.def == d).Sum(t => t.stackCount);
            foreach (var e in expected)
                c.Expect(e.Item.defName, e.Amount.ToString("0.#")).Observe(e.Item.defName, Count(seeded, e.Item) + " seeded, " + Count(unseeded, e.Item) + " unseeded");
            c.Expect("value", 200 * tier.valuePerUnit).Observe("value", $"{BaseValue(seeded):0} seeded, {BaseValue(unseeded):0} unseeded")
                .Expect("studiedValue", 200 * tier.valuePerUnit * tier.studiedYieldFactor).Observe("studiedValue", BaseValue(studied));
            var component = ThingDefOf.ComponentIndustrial;
            var chemfuel = ThingDefOf.Chemfuel;
            c.Step(seeded.Concat(unseeded).All(t => pool.Any(p => p.Item == t.def)), "every product is an industrial pool material");
            c.Step(Count(seeded, component) > 0 && Count(unseeded, component) > 0, "components appear in both samples");
            c.Step(Count(seeded, chemfuel) > 0 && Count(unseeded, chemfuel) > 0, "chemfuel appears in both samples");
            float target = 200 * tier.valuePerUnit;
            c.Step(Math.Abs(BaseValue(seeded) - target) <= 0.15f * target && Math.Abs(BaseValue(unseeded) - target) <= 0.15f * target,
                "200 units return within 15% of their value");
            float ratio = BaseValue(studied) / Math.Max(1f, BaseValue(seeded));
            c.Observe("studiedRatio", ratio);
            c.Step(ratio >= 0.35f && ratio <= 0.65f, $"200 studied units return about half as much ({ratio:0.00})");
            c.Conclude();
        }

        // ---- 15: tier table loads cleanly --------------------------------------------------------------------------------
        {
            var c = ctx.Begin(Pools);
            var defs = DefDatabase<ScrapTierDef>.AllDefsListForReading;
            var errors = defs.SelectMany(d => d.ConfigErrors().Select(e => d.defName + ": " + e)).ToList();
            c.Observe("tiers", defs.Count).Observe("configErrors", string.Join("; ", errors));
            c.Step(defs.Count == Enum.GetValues(typeof(ScrapTier)).Length, "one tier def per scrap tier");
            c.Step(errors.Count == 0, "no tier def reports a config error");
            c.Note("No pool entry uses MayRequire yet, so the inactive-DLC entry case has nothing to load");
            c.Conclude();
        }

        {
            var c = ctx.Begin(NoErrors);
            c.Observe("modErrors", CaseErrors.ModMark - modErrorsAtStart);
            c.Step(CaseErrors.ModMark == modErrorsAtStart, "no error naming Loot Scavenging was logged during the batch");
            c.Conclude();
        }
    }
}
