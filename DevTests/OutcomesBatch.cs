using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
        ItemBased = P + "disassembly-item-based", EraTypical = P + "disassembly-era-typical", FullStacks = P + "disassembly-full-stacks",
        Charges = P + "ejected-charges", Ammo = P + "ammunition-drop-mode", Materials = P + "materials-stay-whole", ToolMaterials = P + "tool-materials-stay-whole", StackScrap = P + "stack-scrap-by-value", Sample = P + "recycle-sample", Pools = P + "tier-table", NoErrors = P + "no-mod-errors";

    public override string Id => "lootscavenging-outcomes";
    public override int SegmentCount => 1;
    public override int MapSize => 120;

    public override IReadOnlyList<PlannedCase> Plan => new[]
    {
        new PlannedCase(DeleteKeepsApparel, Levels.Mechanical, "weapon rule Delete with drop mode Off: weapon gone, apparel stays on the corpse"),
        new PlannedCase(Mech, Levels.Mechanical, "a hostile mechanoid's weapon becomes scrap of its era near the corpse"),
        new PlannedCase(Fallback, Levels.Mechanical, "gear with no research and no tech level takes its carrier's era"),
        new PlannedCase(Amounts, Levels.Mechanical, "stone knife and charge rifle amounts match the value-capped ScrapAmounts.Units; a weapons rule of plain Scrap scraps in-reach gear; Keep keeps out-of-reach gear"),
        new PlannedCase(Raid, Levels.Mechanical, "ten pirates killed in a cluster leave fewer piles than bodies"),
        new PlannedCase(Quest, Levels.Mechanical, "a quest-tagged out-of-reach weapon stays whole"),
        new PlannedCase(Animal, Levels.Mechanical, "with animal corpse removal on, a wild hare's corpse is removed"),
        new PlannedCase(ItemBased, Levels.Mechanical, "item-based yield is the cost list times efficiency (0.5 and 1.0), forbidden"),
        new PlannedCase(EraTypical, Levels.Mechanical, "colony-era yield comes from the colony era's tier pool, not the item's"),
        new PlannedCase(FullStacks, Levels.Mechanical, "a yield above the stack limit arrives as full stacks with nothing lost"),
        new PlannedCase(Charges, Levels.Mechanical, "a smokepop belt's chemfuel is ejected for scrap, delete and disassemble"),
        new PlannedCase(Ammo, Levels.Mechanical, "out-of-reach ammunition drops whole by default; with the ammunition rule on Scrap beyond reach, a stack becomes scrap rounded once per stack (needs an ammo mod)"),
        new PlannedCase(Materials, Levels.Mechanical, "a stack of devilstrand that a modded recipe puts behind unfinished spacer research drops whole, with steel and plasteel and no scrap, while the revolver still becomes scrap"),
        new PlannedCase(ToolMaterials, Levels.Mechanical, "wood, beer, thrumbo horns and tusks (melee tools, but not gear) are not gear; grenades and a revolver still are; wood behind the spacer recipe and carried horns drop whole"),
        new PlannedCase(StackScrap, Levels.Mechanical, "with the other-items rule on Scrap, 53 devilstrand or 50 cloth become StackUnits of the colony's tier, not one scrap per item; a lone revolver still gives Units"),
        new PlannedCase(Sample, Levels.Mechanical, "200 industrial units recycle to about 2200 silver of pool materials, components and chemfuel included; studied scrap pays half"),
        new PlannedCase(Pools, Levels.Mechanical, "every tier def loads with no config errors"),
        new PlannedCase(NoErrors, Levels.Mechanical, "no error naming the mod logged during the batch"),
    };

    public override IEnumerable<Wait> Segment(int index, BatchContext ctx)
    {
        long modErrorsAtStart = CaseErrors.ModMark;
        ModHarness.SaveSettings(ModHarness.GearDrops);
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
            ModHarness.ApplyStyle(Rules, ModHarness.LootStyle.Off);
            Rules.weapons = new CategoryRule(Outcome.Delete);
            c.Note("Weapon rule set to Delete, every other category to Keep");
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
                int expected = ExpectedScrap(weapon, mech.Faction);
                var before = Kit.StackIds(map, tierDef);
                mech.Kill(null);
                var made = NewNear(map, tierDef, before, mech.Corpse?.Position ?? cell);
                c.Observe("weapon", weapon.def.defName).Observe("era", era.Era).Observe("outOfReach", era.OutOfReach).Expect("units", expected).Observe("units", Units(made)).Observe("scrapDef", tierDef.defName);
                c.Step(era.OutOfReach, $"the {weapon.def.label} is out of reach for the tribal colony");
                c.Step(weapon.Destroyed && Units(made) == expected, $"it becomes {Units(made)} {tierDef.defName} beside the wreck (expected {expected})");
                c.Conclude();
            }
        }

        // ---- 02/25: gear with no era takes its carrier's ------------------------------------------------------------------
        {
            var noEra = DefDatabase<ThingDef>.AllDefsListForReading.Where(d => EraReader.IsGear(d) && d.techLevel == TechLevel.Undefined && !d.IsCorpse
                && EraReader.ItemEra(d, Pirates).Source == EraSource.CarrierFaction).ToList();
            if (noEra.Count == 0)
                ctx.Begin(Fallback).NotApplicable("no loaded weapon or apparel has neither research nor a tech level");
            else
            {
                var c = ctx.Begin(Fallback);
                var def = noEra.FirstOrDefault(d => d.IsRangedWeapon || d.IsMeleeWeapon) ?? noEra[0];
                var carrier = EraReader.ItemEra(def, Pirates);
                c.Observe("def", def.defName).Observe("candidates", noEra.Count).Observe("carrierEra", carrier.Era + "/" + carrier.Source);
                // The colony-era and components fallbacks were cut in 0.6.0 (ticket 25); the carrier's era is the only one left.
                c.Step(carrier.Era == (Era)Pirates.def.techLevel, "a pirate's copy takes the pirates' era");
                c.Conclude();
            }
        }

        // ---- 03: amounts and the weapons rule ----------------------------------------------------------------------------------
        {
            var c = ctx.Begin(Amounts);
            ModHarness.RestoreSettings();
            var rifleDef = Kit.Def("Gun_ChargeRifle");
            var spacer = Scrap(ScrapTier.Spacer);
            ThingWithComps rifle = null;
            int rifleUnits = 0;
            var before = Kit.StackIds(map, spacer);
            var at = KillPirate(p => { rifle = Arm(p, rifleDef, QualityCategory.Normal); rifleUnits = ExpectedScrapIn(rifle, ScrapTier.Spacer); }, out _);
            var made = NewNear(map, spacer, before, at);
            c.Expect("chargeRifleUnits", rifleUnits).Observe("chargeRifleUnits", Units(made));
            c.Step(rifle.Destroyed && Units(made) == rifleUnits, $"a charge rifle becomes {Units(made)} spacer scrap (expected {rifleUnits})");

            ModHarness.ApplyStyle(Rules, ModHarness.LootStyle.ScrapEverything);
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
                knifeUnits = ExpectedScrapIn(knife, ScrapTier.Neolithic);
            }, out _);
            made = NewNear(map, neolithic, before, at);
            c.Expect("stoneKnifeUnits", knifeUnits).Observe("stoneKnifeUnits", Units(made)).Observe("knifeInReach", inReach);
            c.Step(inReach && knife.Destroyed && Units(made) == knifeUnits, $"with the weapons rule on plain Scrap an in-reach stone knife becomes {Units(made)} neolithic scrap (expected {knifeUnits})");

            ModHarness.ApplyStyle(Rules, ModHarness.LootStyle.Off);
            ThingWithComps kept = null;
            int scrapBefore = ScrapOnMap(map);
            KillPirate(p => kept = Arm(p, rifleDef, QualityCategory.Normal), out _);
            c.Step(!kept.Destroyed && kept.Spawned && ScrapOnMap(map) == scrapBefore, "with the weapons rule on Keep an out-of-reach charge rifle drops whole");
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
            int expected = raiders.Sum(p => ExpectedScrap(p.equipment.Primary, Pirates));
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

        // ---- 08: item-based disassembly --------------------------------------------------------------------------------
        {
            var c = ctx.Begin(ItemBased);
            var costs = revolver.CostList.Where(x => x.count > 0).ToList();
            c.Observe("costList", string.Join(", ", costs.Select(x => x.count + " " + x.thingDef.defName)));
            foreach (float efficiency in new[] { 0.5f, 1f })
            {
                ModHarness.RestoreSettings();
                ModHarness.ApplyStyle(Rules, ModHarness.LootStyle.Off);
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

        // ---- 08/25: colony-era disassembly --------------------------------------------------------------------------------
        {
            var c = ctx.Begin(EraTypical);
            ModHarness.RestoreSettings();
            ModHarness.ApplyStyle(Rules, ModHarness.LootStyle.Off);
            Rules.weapons = new CategoryRule(Outcome.Disassemble);
            Rules.disassemblyYield = DisassemblyYield.EraTypical;
            // Since 0.6.0 the yield is the colony era's pool (ticket 25, decision 7): a tribal colony takes industrial revolvers
            // apart into neolithic materials.
            var colonyTier = ScrapTiers.TierFor(EraReader.ColonyEra(), EraReader.ColonyEra());
            var pool = TierDef(colonyTier).pool.Select(e => e.thing).ToList();
            var industrialOnly = TierDef(ScrapTier.Industrial).pool.Select(e => e.thing).Where(d => d != null && !pool.Contains(d)).ToList();
            c.Observe("colonyTier", colonyTier).Observe("industrialOnly", string.Join(", ", industrialOnly.Select(d => d.defName)));
            // Every material any tier's pool can return, so a product from the wrong pool would show too.
            var befores = DefDatabase<ScrapTierDef>.AllDefsListForReading.SelectMany(t => t.pool).Select(e => e.thing).Where(d => d != null).Distinct()
                .ToDictionary(d => d, d => Kit.StackIds(map, d));
            // One revolver is one or two scrap units, and a unit whose pick costs more than the unit's value (a component) returns
            // nothing with probability 1 - value/price, so a single revolver can yield nothing by chance. The pirate also carries
            // 20 revolvers, and the chance that every unit comes back empty is computed from the pool below. A weapon carried in
            // the inventory is judged by the weapons rule (ticket 21; before that by the "other" rule, which also disassembles
            // here), and the pack is emptied first so nothing but revolvers is disassembled.
            const int carried = 20;
            Rules.other = new CategoryRule(Outcome.Disassemble);
            var revolversBefore = Kit.StackIds(map, revolver);
            var tierDef = TierDef(colonyTier);
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
            c.Step(colonyTier == ScrapTier.Neolithic && industrialOnly.Count > 0, "the tribal colony's tier is neolithic, and its pool lacks some industrial materials");
            c.Step(made.Count > 0 && made.All(t => pool.Contains(t.def)), $"industrial revolvers return only {colonyTier} pool materials ({Describe(made)})");
            c.Conclude();
        }

        // ---- 08: full stacks ---------------------------------------------------------------------------------------------
        {
            var c = ctx.Begin(FullStacks);
            ModHarness.RestoreSettings();
            ModHarness.ApplyStyle(Rules, ModHarness.LootStyle.Off);
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
            int Run(string label, Action setup, out bool destroyed)
            {
                ModHarness.RestoreSettings();
                setup();
                var before = Kit.StackIds(map, chemfuel);
                Apparel worn = null;
                var at = KillPirate(p => worn = Dress(p, belt), out _);
                destroyed = worn.Destroyed;
                int got = Units(NewNear(map, chemfuel, before, at));
                c.Observe(label, got);
                return got;
            }
            // Charges are always ejected since 0.6.0 (ticket 25); the setting that turned it off is gone.
            int scrapped = Run("scrapped", () => { }, out bool d1);
            int deleted = Run("deleted", () => { ModHarness.ApplyStyle(Rules, ModHarness.LootStyle.Off); Rules.apparel = new CategoryRule(Outcome.Delete); }, out bool d2);
            int disassembled = Run("disassembled", () =>
            {
                ModHarness.ApplyStyle(Rules, ModHarness.LootStyle.Off); Rules.apparel = new CategoryRule(Outcome.Disassemble);
                Rules.disassemblyYield = DisassemblyYield.ItemBased; Rules.disassemblyEfficiency = 1f;
            }, out bool d3);
            c.Observe("beltCostChemfuel", costChemfuel);
            c.Step(d1 && d2 && d3, "each belt is used up");
            c.Step(scrapped == 10, $"scrapped: 10 chemfuel ejected ({scrapped})");
            c.Step(deleted == 10, $"deleted: 10 ejected ({deleted})");
            c.Step(disassembled == 10 + costChemfuel, $"disassembled: 10 ejected plus {costChemfuel} from the cost list ({disassembled})");
            c.Conclude();
        }

        // ---- ammunition and its rule ----------------------------------------------------------------------------------
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
                var ammoTier = ScrapTiers.TierFor(era.Era, EraReader.ColonyEra());
                var tierDef = Scrap(ammoTier);
                c.Observe("ammoDef", ammo.defName).Observe("ammoEra", era.Era).Observe("loadedAmmo", string.Join(", ", ammoDefs.Select(d => d.defName)));
                c.Step(Rules.ammunition.outcome == Outcome.Keep, "by default ammunition is kept");

                var before = Kit.StackIds(map, ammo);
                var scrapBefore = Kit.StackIds(map, tierDef);
                var at = KillPirate(p => p.inventory.innerContainer.TryAdd(Stack(ammo, count)), out _);
                int whole = Units(NewNear(map, ammo, before, at)), scrapDefault = Units(NewNear(map, tierDef, scrapBefore, at));
                c.Expect("ammoDroppedDefault", count).Observe("ammoDroppedDefault", whole).Observe("scrapDefault", scrapDefault);
                c.Step(whole == count && scrapDefault == 0, $"a pirate's {count} {ammo.label} drop whole ({whole}), no scrap ({scrapDefault})");

                Rules.ammunition = CategoryRule.ScrapBeyondReach();
                c.Note("The player sets ammunition to Scrap, only beyond reach");
                var stack = Stack(ammo, count);
                int expected = ExpectedScrapIn(stack, ammoTier);
                int perItem = ScrapAmounts.Units(stack.MarketValue, count);
                float stackValue = stack.MarketValue * count;
                before = Kit.StackIds(map, ammo);
                scrapBefore = Kit.StackIds(map, tierDef);
                at = KillPirate(p => p.inventory.innerContainer.TryAdd(stack), out _);
                int left = Units(NewNear(map, ammo, before, at)), scrap = Units(NewNear(map, tierDef, scrapBefore, at));
                c.Expect("scrapIncluded", expected).Observe("scrapIncluded", scrap).Observe("ammoLeft", left).Observe("perItemRounding", perItem);
                c.Step(stack.Destroyed && left == 0, $"a second pirate's {count} {ammo.label} are gone ({left} left)");
                c.Step(scrap == expected, $"they become {scrap} {tierDef.defName}, rounded once per stack and capped by value (expected {expected}; per-item rounding gives {perItem})");
                c.Expect("scrapWorthAtMostStackValue", true).Observe("scrapWorth", scrap * ScrapWorth(ammoTier)).Observe("stackValue", stackValue);
                c.Step(scrap * ScrapWorth(ammoTier) <= stackValue + 0.001f, $"the {scrap} scrap is worth {scrap * ScrapWorth(ammoTier):0.#}, no more than the stack's {stackValue:0.#}");
                ModHarness.RestoreSettings();
                c.Conclude();
            }
        }

        // ---- materials have no era (0.5.2) -------------------------------------------------------------------------------
        {
            var c = ctx.Begin(Materials);
            ModHarness.RestoreSettings();
            const BindingFlags priv = BindingFlags.NonPublic | BindingFlags.Static;
            var indexField = typeof(EraReader).GetField("recipeIndex", priv);
            var prerequisitesOf = typeof(EraReader).GetMethod("PrerequisitesOf", priv);
            var devilstrand = Kit.Def("DevilstrandCloth");
            var steel = ThingDefOf.Steel;
            var plasteel = Kit.Def("Plasteel");
            var wood = ThingDefOf.WoodLog;
            var spacerProject = Project("ChargedShot");
            if (indexField == null || prerequisitesOf == null)
            {
                c.Step(false, "EraReader's recipeIndex field and PrerequisitesOf method were not found (renamed: update this case)");
                c.Conclude();
                ctx.Begin(ToolMaterials).NotApplicable("EraReader's recipe index could not be reached by reflection");
            }
            else
            {
                // A player's report: a modded recipe producing devilstrand sits behind spacer research. EraReader indexes recipes by
                // product once and caches the index, so it is cleared before and after to make it see this recipe and then forget it.
                var recipe = new RecipeDef
                {
                    defName = "LS_DevTest_DevilstrandBehindSpacerResearch",
                    products = new List<ThingDefCountClass> { new ThingDefCountClass(devilstrand, 1), new ThingDefCountClass(wood, 1) },
                    researchPrerequisite = spacerProject,
                };
                DefDatabase<RecipeDef>.Add(recipe);
                indexField.SetValue(null, null);
                ctx.Setup("Developer added a RecipeDef producing DevilstrandCloth behind the unfinished spacer project ChargedShot and cleared EraReader's recipe index");
                try
                {
                    var found = (List<ResearchProjectDef>)prerequisitesOf.Invoke(null, new object[] { devilstrand });
                    c.Step(spacerProject.techLevel == TechLevel.Spacer && !spacerProject.IsFinished, "ChargedShot is an unfinished spacer project");
                    c.Step(found.Contains(spacerProject),
                        "the rebuilt recipe index ties devilstrand to that spacer project, which before 0.5.2 made it spacer-era and out of reach");
                    var era = EraReader.ItemEra(devilstrand, Pirates);
                    c.Observe("devilstrandEra", era.Era + "/" + era.Source).Observe("devilstrandOutOfReach", era.OutOfReach);
                    c.Step(era.Source == EraSource.Unknown && !era.OutOfReach, $"EraReader gives devilstrand no era and not out of reach ({era.Era}/{era.Source})");

                    var tiers = ((ScrapTier[])Enum.GetValues(typeof(ScrapTier))).ToDictionary(t => t, t => Kit.CountOnMap(map, Scrap(t)));
                    int devilBefore = Kit.CountOnMap(map, devilstrand), steelBefore = Kit.CountOnMap(map, steel), plasteelBefore = Kit.CountOnMap(map, plasteel);
                    ThingWithComps gun = null;
                    int gunUnits = 0;
                    KillPirate(p =>
                    {
                        gun = Arm(p, revolver, QualityCategory.Normal);
                        gunUnits = ExpectedScrapIn(gun, ScrapTier.Industrial);
                        p.inventory.innerContainer.TryAdd(Stack(devilstrand, 53));
                        p.inventory.innerContainer.TryAdd(Stack(steel, 30));
                        p.inventory.innerContainer.TryAdd(Stack(plasteel, 20));
                    }, out _);
                    int devil = Kit.CountOnMap(map, devilstrand) - devilBefore, steelGot = Kit.CountOnMap(map, steel) - steelBefore,
                        plasteelGot = Kit.CountOnMap(map, plasteel) - plasteelBefore;
                    var scrapGot = tiers.ToDictionary(kv => kv.Key, kv => Kit.CountOnMap(map, Scrap(kv.Key)) - kv.Value);
                    c.Expect("devilstrand", 53).Observe("devilstrand", devil).Observe("steel", steelGot).Observe("plasteel", plasteelGot)
                        .Expect("industrialScrap", gunUnits).Observe("scrapByTier", string.Join(", ", scrapGot.Where(kv => kv.Value != 0).Select(kv => kv.Value + " " + kv.Key)));
                    c.Step(devil == 53, $"the pirate's 53 devilstrand drop whole ({devil})");
                    c.Step(steelGot == 30 && plasteelGot == 20, $"his steel ({steelGot} of 30) and plasteel ({plasteelGot} of 20) drop whole too");
                    c.Step(scrapGot[ScrapTier.Spacer] == 0, $"no spacer scrap came from the devilstrand ({scrapGot[ScrapTier.Spacer]})");
                    c.Step(gun.Destroyed && scrapGot[ScrapTier.Industrial] == gunUnits && scrapGot.Where(kv => kv.Key != ScrapTier.Industrial).All(kv => kv.Value == 0),
                        $"his out-of-reach revolver still becomes {scrapGot[ScrapTier.Industrial]} industrial scrap (expected {gunUnits}) and nothing else is scrap");
                    c.Conclude();

                    // Wood is a material too, and Core gives it <tools> (a log can be swung), so the game counts it as a weapon. Gear
                    // means equippable weapons only, so behind the same recipe it drops whole as well.
                    c = ctx.Begin(ToolMaterials);
                    var woodEra = EraReader.ItemEra(wood, Pirates);
                    int woodBefore = Kit.CountOnMap(map, wood), spacerBefore = Kit.CountOnMap(map, Scrap(ScrapTier.Spacer));
                    KillPirate(p => p.inventory.innerContainer.TryAdd(Stack(wood, 50)), out _);
                    int woodGot = Kit.CountOnMap(map, wood) - woodBefore, woodSpacer = Kit.CountOnMap(map, Scrap(ScrapTier.Spacer)) - spacerBefore;
                    c.Observe("woodEra", woodEra.Era + "/" + woodEra.Source).Observe("woodIsGear", EraReader.IsGear(wood)).Observe("woodDropped", woodGot).Observe("woodSpacerScrap", woodSpacer);
                    ThingDef beer = DefDatabase<ThingDef>.GetNamedSilentFail("Beer");
                    c.Step(!EraReader.IsGear(wood) && (beer == null || !EraReader.IsGear(beer)), "wood and beer, which the game counts as melee weapons, are not gear");
                    // Thrumbo horns and tusks stack and carry no weapon tags: trophies, not gear. Grenades are tagged and stay gear.
                    ThingDef horn = Kit.Def("ThrumboHorn"), tusk = Kit.Def("ElephantTusk"), frag = Kit.Def("Weapon_GrenadeFrag");
                    c.Observe("hornIsGear", EraReader.IsGear(horn)).Observe("tuskIsGear", EraReader.IsGear(tusk)).Observe("fragIsGear", EraReader.IsGear(frag));
                    c.Step(!EraReader.IsGear(horn) && !EraReader.IsGear(tusk), "a thrumbo horn and an elephant tusk are not gear");
                    c.Step(EraReader.IsGear(frag) && EraReader.IsGear(revolver), "frag grenades and a revolver are still gear");
                    int hornBefore = Kit.CountOnMap(map, horn);
                    KillPirate(p => p.inventory.innerContainer.TryAdd(Stack(horn, 2)), out _);
                    int hornGot = Kit.CountOnMap(map, horn) - hornBefore;
                    c.Observe("hornsDropped", hornGot);
                    c.Step(hornGot == 2, $"a pirate's 2 thrumbo horns drop whole ({hornGot})");
                    c.Step(woodGot == 50 && woodSpacer == 0 && woodEra.Source == EraSource.Unknown,
                        $"a pirate's 50 wood behind the same spacer recipe drop whole ({woodGot} dropped, {woodSpacer} spacer scrap, era {woodEra.Era}/{woodEra.Source})");
                    RemoveRecipe();
                    c.Step(DefDatabase<RecipeDef>.GetNamedSilentFail(recipe.defName) == null && !DefDatabase<RecipeDef>.AllDefsListForReading.Contains(recipe),
                        "the injected recipe is removed again and the recipe index cleared");
                    c.Conclude();
                }
                finally { RemoveRecipe(); }

                void RemoveRecipe()
                {
                    DefDatabase<RecipeDef>.AllDefsListForReading.Remove(recipe);
                    typeof(DefDatabase<RecipeDef>).GetMethod("Remove", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)?.Invoke(null, new object[] { recipe });
                    indexField.SetValue(null, null);
                }
            }
        }

        // ---- stackable items scrap by stack value (0.5.2) --------------------------------------------------------------------
        {
            var c = ctx.Begin(StackScrap);
            ModHarness.RestoreSettings();
            Rules.other = new CategoryRule(Outcome.Scrap);
            c.Note("The player sets the other-items inventory rule to Scrap");
            int ScrapOf(ScrapTier t) => Kit.CountOnMap(map, Scrap(t));
            ThingWithComps gun = null;
            int gunUnits = 0;
            foreach (var (def, count, withGun) in new[] { (Kit.Def("DevilstrandCloth"), 53, true), (Kit.Def("Cloth"), 50, false) })
            {
                var stack = Stack(def, count);
                var era = EraReader.ItemEra(def, Pirates);
                var tier = ScrapTiers.TierFor(era.Era, EraReader.ColonyEra());
                int expected = ExpectedScrapIn(stack, tier);
                float stackValue = stack.MarketValue * count;
                int tierBefore = ScrapOf(tier), industrialBefore = ScrapOf(ScrapTier.Industrial);
                KillPirate(p =>
                {
                    p.inventory.innerContainer.TryAdd(stack);
                    if (withGun)
                    {
                        gun = Arm(p, revolver, QualityCategory.Normal);
                        gunUnits = ExpectedScrapIn(gun, ScrapTier.Industrial);
                    }
                }, out _);
                int got = ScrapOf(tier) - tierBefore;
                c.Expect(def.defName, expected).Observe(def.defName, got).Observe(def.defName + "UnitValue", stack.MarketValue.ToString("0.##")).Observe(def.defName + "Tier", tier);
                c.Step(era.Source == EraSource.Unknown, $"{def.label} has no era, so its scrap is the colony's tier ({tier})");
                c.Step(stack.Destroyed && got == expected, $"{count} {def.label} become {got} {tier} scrap (expected {expected} from the stack's value)");
                // Rounding each item would give at least one per item; since 0.5.2 a unit is cheap enough that a stack can give over half.
                c.Step(got < count, $"fewer than one scrap per item ({got} for {count})");
                c.Observe(def.defName + "ScrapWorth", got * ScrapWorth(tier)).Observe(def.defName + "StackValue", stackValue);
                c.Step(got * ScrapWorth(tier) <= stackValue + 0.001f, $"the {got} scrap is worth {got * ScrapWorth(tier):0.#}, no more than the stack's {stackValue:0.#}");
                if (withGun)
                {
                    int industrialGot = ScrapOf(ScrapTier.Industrial) - industrialBefore;
                    c.Expect("revolver", gunUnits).Observe("revolver", industrialGot);
                    c.Step(gun.Destroyed && industrialGot == gunUnits, $"the pirate's lone revolver still gives the capped ScrapAmounts.Units ({industrialGot}, expected {gunUnits}), at least one unit per item");
                }
            }
            ModHarness.RestoreSettings();
            c.Conclude();
        }

        // ---- 04/15: recycling sample -------------------------------------------------------------------------------------
        {
            var c = ctx.Begin(Sample);
            // 400, not 200: 0.5.2 halved a unit's value, and 200 units drew so few products that the unseeded sample fell
            // outside 15% by chance (1271 against 1100).
            const int Units = 400;
            var tier = TierDef(ScrapTier.Industrial);
            var pool = tier.pool.Where(e => e.thing != null).Select(e => new PoolEntry<ThingDef>(e.thing, e.weight)).ToList();
            var expected = LootPool.Expected(pool, tier.valuePerUnit, Units, t => t.BaseMarketValue);
            List<Thing> Recycle(ThingDef def, int? seed)
            {
                var scrap = (ScrapThing)Stack(def, Units);
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
            c.Expect("value", Units * tier.valuePerUnit).Observe("value", $"{BaseValue(seeded):0} seeded, {BaseValue(unseeded):0} unseeded")
                .Expect("studiedValue", Units * tier.valuePerUnit * tier.studiedYieldFactor).Observe("studiedValue", BaseValue(studied));
            var component = ThingDefOf.ComponentIndustrial;
            var chemfuel = ThingDefOf.Chemfuel;
            c.Step(seeded.Concat(unseeded).All(t => pool.Any(p => p.Item == t.def)), "every product is an industrial pool material");
            c.Step(Count(seeded, component) > 0 && Count(unseeded, component) > 0, "components appear in both samples");
            c.Step(Count(seeded, chemfuel) > 0 && Count(unseeded, chemfuel) > 0, "chemfuel appears in both samples");
            float target = Units * tier.valuePerUnit;
            c.Step(Math.Abs(BaseValue(seeded) - target) <= 0.15f * target && Math.Abs(BaseValue(unseeded) - target) <= 0.15f * target,
                $"{Units} units return within 15% of their value");
            float ratio = BaseValue(studied) / Math.Max(1f, BaseValue(seeded));
            c.Observe("studiedRatio", ratio);
            c.Step(ratio >= 0.35f && ratio <= 0.65f, $"{Units} studied units return about half as much ({ratio:0.00})");
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
