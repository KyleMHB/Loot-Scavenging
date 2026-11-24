using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using LootScavenging.Model;
using RimWorld;
using RimWorld.Planet;
using RimWorldDevTests;
using Verse;
using Verse.AI;
using static LootScavenging.DevTests.Fixtures;

namespace LootScavenging.DevTests;

// The moments gear changes hands (tickets 01, 03, 10): downed and stripped pawns, a colony prisoner, scrap left on the body,
// butchering, a death off the map and a death on a map that is not home. Butchering runs a real bill job; the rest are
// direct calls between ticks. The away map is made last, and removed again.
public sealed class MomentsBatch : Batch
{
    private const string P = "lootscavenging.moments.";
    private const string DownStripOn = P + "downed-and-strip-on", DownStripOff = P + "downed-and-strip-off", Prisoner = P + "prisoner-strip",
        OnBody = P + "scrap-on-body", OnBodyRemoved = P + "scrap-on-body-corpse-removed", OffMap = P + "off-map-death",
        AllOnBody = P + "all-on-body", AllOnBodyDisassembled = P + "all-on-body-disassembled", AllOnBodyRemoved = P + "all-on-body-corpse-removed",
        AllOnBodySkips = P + "all-on-body-skips", AllOnBodyDowned = P + "all-on-body-downed",
        Butcher = P + "butchering-removes-apparel", HomeOnly = P + "home-map-only", NoErrors = P + "no-mod-errors",
        JudgedOnce = P + "downed-then-killed-judged-once", LockedApparel = P + "locked-apparel-at-death",
        FormerColonist = P + "former-colonist-keeps-gear", ProtectedFamiliar = P + "protected-never-unfamiliar",
        Arrest = P + "arrest-standing", Recruit = P + "recruit-downed-raider", Manipulation = P + "lost-manipulation",
        DropCallsOnce = P + "drop-calls-judged-once",
        JoinMark = P + "joiner-marked-unfamiliar", JoinLeave = P + "joiner-left-alone", JoinFollow = P + "joiner-follows-drop-mode",
        JoinPrisonerRules = P + "joiner-prisoner-setting", JoinProtected = P + "joiner-protected-never-marked", JoinFormer = P + "joiner-former-colonist-untouched";

    public override string Id => "lootscavenging-moments";
    public override int SegmentCount => 1;
    public override int MapSize => 100;

    public override IReadOnlyList<PlannedCase> Plan => new[]
    {
        new PlannedCase(DownStripOn, Levels.Mechanical, "downing scraps an out-of-reach weapon and stripping scraps out-of-reach apparel"),
        new PlannedCase(DownStripOff, Levels.Mechanical, "with downed and strip off, both drop whole"),
        new PlannedCase(Prisoner, Levels.Mechanical, "stripping a living colony prisoner scraps out-of-reach apparel"),
        new PlannedCase(Arrest, Levels.Mechanical, "arresting a standing pirate scraps his out-of-reach revolver; with downed and strip off it drops whole"),
        new PlannedCase(Recruit, Levels.Mechanical, "recruiting (and, with Ideology, enslaving) a downed, captured raider scraps his out-of-reach flak vest"),
        new PlannedCase(Manipulation, Levels.Mechanical, "a standing pirate who loses both arms has his out-of-reach revolver scrapped, not dropped whole"),
        new PlannedCase(DropCallsOnce, Levels.Mechanical, "a Degrade weapon rule judges the weapon once at death, downing then death, arrest and strip"),
        new PlannedCase(JoinMark, Levels.Mechanical, "a wanderer joins in out-of-reach armour and a revolver and keeps them; both are unfamiliar in the scrap drop mode and in Off, even off him"),
        new PlannedCase(JoinLeave, Levels.Mechanical, "with joiner gear left alone, a wanderer's vest carries no mark"),
        new PlannedCase(JoinFollow, Levels.Mechanical, "a wanderer, a recruited prisoner and (with Ideology) an enslaved one have vest and revolver scrapped once, with downed and strip off"),
        new PlannedCase(JoinPrisonerRules, Levels.Mechanical, "a recruit follows the prisoner setting, not the other one: marked or left alone"),
        new PlannedCase(JoinProtected, Levels.Mechanical, "a quest-tagged vest on a joiner is never marked or scrapped"),
        new PlannedCase(JoinFormer, Levels.Mechanical, "a former colonist rejoining keeps his vest unmarked and unsorted"),
        new PlannedCase(OnBody, Levels.Mechanical, "scrap left on the body is in the corpse; stripping it drops the scrap"),
        new PlannedCase(OnBodyRemoved, Levels.Mechanical, "with corpse removal on, scrap left on the body lands on the ground"),
        new PlannedCase(AllOnBody, Levels.Mechanical, "everything on the body: scrap, ejected charges and inventory stay in the corpse; stripping drops them"),
        new PlannedCase(AllOnBodyDisassembled, Levels.Mechanical, "everything on the body: disassembly products stay in the corpse"),
        new PlannedCase(AllOnBodyRemoved, Levels.Mechanical, "everything on the body with corpse removal on: the loot lands on the ground"),
        new PlannedCase(AllOnBodySkips, Levels.Mechanical, "everything on the body: a colonist's gear and a mechanoid's scrap drop as usual"),
        new PlannedCase(AllOnBodyDowned, Levels.Mechanical, "everything on the body: a downed pirate keeps his in-reach club and the rest in his pack, with downed handling on and off"),
        new PlannedCase(JudgedOnce, Levels.Mechanical, "everything on the body: a club kept at a downing is not judged again as other inventory when the pirate dies"),
        new PlannedCase(LockedApparel, Levels.Mechanical, "a pirate's locked out-of-reach vest is scrapped at death"),
        new PlannedCase(FormerColonist, Levels.Mechanical, "a colonist who ran wild (no faction) keeps an out-of-reach revolver when downed"),
        new PlannedCase(ProtectedFamiliar, Levels.Mechanical, "keep-original mode: a keep-listed (and, with Royalty, a persona) out-of-reach weapon is never unfamiliar"),
        new PlannedCase(OffMap, Levels.Mechanical, "a world pawn's death (no map, no corpse) logs no error"),
        new PlannedCase(Butcher, Levels.PlayerWorkflow, "a butcher bill destroys worn apparel with the option on and drops it with the option off"),
        new PlannedCase(HomeOnly, Levels.Mechanical, "home-map-only leaves gear whole on an away map; off, it is scrapped there"),
        new PlannedCase(NoErrors, Levels.Mechanical, "no error naming the mod logged during the batch"),
    };

    // Outcomes the mod applied per item, while the judged-once case patches OutcomeApplier.Apply.
    private static readonly Dictionary<Thing, int> judged = new Dictionary<Thing, int>();

    private static void CountJudged(Thing thing)
    {
        judged.TryGetValue(thing, out int n);
        judged[thing] = n + 1;
    }

    public override IEnumerable<Wait> Segment(int index, BatchContext ctx)
    {
        long modErrorsAtStart = CaseErrors.ModMark;
        ModHarness.SaveSettings();
        var area = Kit.Area(ctx, 80, 14);
        Kit.RemoveStartingColonists(ctx);
        yield return Wait.Ticks(1);
        var map = ctx.Map;
        MakeTribal(ctx);
        // Two rows 6 cells apart: the first row's sites ran out.
        var sites = new Queue<IntVec3>(Sites(area, 11, 7).Concat(Sites(area, 11, 7).Select(c => c + new IntVec3(0, 0, -6))));
        var revolver = Kit.Def("Gun_Revolver");
        var vest = Kit.Def("Apparel_FlakVest");
        var industrial = Scrap(ScrapTier.Industrial);

        // ---- 01: downed and strip on, then off ---------------------------------------------------------------------------
        foreach (bool on in new[] { true, false })
        {
            var c = ctx.Begin(on ? DownStripOn : DownStripOff);
            ModHarness.RestoreSettings();
            Rules.onDownedAndStrip = on;
            var cell = sites.Dequeue();
            var p = Spawn(ctx, Mercenary, Pirates, cell);
            var gun = Arm(p, revolver, QualityCategory.Normal);
            var worn = Dress(p, vest, QualityCategory.Normal);
            c.Subject(p);
            c.Step(EraReader.ItemEra(vest, Pirates).OutOfReach && EraReader.ItemEra(revolver, Pirates).OutOfReach, "the revolver and flak vest are out of reach");
            // Totals near the pawn, not new stacks: scrap made later can merge into the pile made at the downing.
            int Near() => map.listerThings.ThingsOfDef(industrial).Where(t => t.Spawned && t.Position.DistanceTo(cell) <= 5f).Sum(t => t.stackCount);
            int start = Near();
            HealthUtility.TryAnesthetize(p);
            if (!Kit.Require(ctx, on ? DownStripOn : DownStripOff, (p.Downed && !p.Dead, "the pirate is downed and alive"))) continue;
            int afterDown = Near() - start;
            if (on) c.Step(gun.Destroyed && afterDown > 0, $"downing turns the revolver into scrap ({afterDown} units)");
            else c.Step(!gun.Destroyed && gun.Spawned && afterDown == 0, "downing drops the revolver whole");
            c.Step(p.apparel.Contains(worn), "the vest stays on while downed");
            start = Near();
            p.Strip();
            int afterStrip = Near() - start;
            if (on) c.Step(worn.Destroyed && afterStrip > 0, $"stripping turns the vest into scrap ({afterStrip} units)");
            else c.Step(!worn.Destroyed && worn.Spawned && afterStrip == 0, "stripping drops the vest whole");
            c.Conclude();
            p.Destroy();
        }

        // ---- 01: living prisoner strip -----------------------------------------------------------------------------------
        {
            var c = ctx.Begin(Prisoner);
            ModHarness.RestoreSettings();
            var cell = sites.Dequeue();
            var p = Spawn(ctx, Mercenary, Pirates, cell);
            var worn = Dress(p, vest, QualityCategory.Normal);
            p.guest.SetGuestStatus(Faction.OfPlayer, GuestStatus.Prisoner);
            c.Subject(p);
            if (Kit.Require(ctx, Prisoner, (p.IsPrisonerOfColony && !p.Dead, "the pirate is a living colony prisoner")))
            {
                var before = Kit.StackIds(map, industrial);
                p.Strip();
                int made = Units(NewNear(map, industrial, before, cell));
                c.Step(worn.Destroyed && made > 0, $"stripping the prisoner turns the vest into {made} scrap");
                c.Conclude();
            }
            p.Destroy();
        }

        // ---- 18: arrest of a standing pawn -------------------------------------------------------------------------------
        // The game's arrest path (Pawn_GuestTracker.SetGuestStatus) drops the weapon and pack through DropAndForbidEverything.
        {
            var c = ctx.Begin(Arrest);
            foreach (bool on in new[] { true, false })
            {
                ModHarness.RestoreSettings();
                Rules.onDownedAndStrip = on;
                string tag = on ? "On" : "Off";
                var cell = sites.Dequeue();
                var p = Spawn(ctx, Mercenary, Pirates, cell);
                var gun = Arm(p, revolver, QualityCategory.Normal);
                int expected = ScrapAmounts.Units(gun.MarketValue, 1);
                p.inventory.innerContainer.TryAdd(Stack(ThingDefOf.Silver, 30));
                c.Subject(p);
                var before = Kit.StackIds(map, industrial);
                var beforeSilver = Kit.StackIds(map, ThingDefOf.Silver);
                p.guest.SetGuestStatus(Faction.OfPlayer, GuestStatus.Prisoner);
                int made = Units(NewNear(map, industrial, before, cell)), silverOut = Units(NewNear(map, ThingDefOf.Silver, beforeSilver, cell));
                c.Observe("scrap" + tag, made).Observe("silver" + tag, silverOut).Observe("revolver" + tag, gun.Destroyed ? "destroyed" : gun.Spawned ? "on ground" : "elsewhere");
                c.Step(p.IsPrisonerOfColony && !p.Downed, $"{tag}: the pirate is a standing colony prisoner");
                if (on) c.Step(gun.Destroyed && made == expected, $"{tag}: the revolver becomes {made} industrial scrap on the ground (expected {expected})");
                else c.Step(!gun.Destroyed && gun.Spawned && made == 0, $"{tag}: the revolver drops whole");
                c.Step(silverOut == 30, $"{tag}: the carried silver drops untouched ({silverOut})");
                p.Destroy();
            }
            c.Conclude();
        }

        // Recruiting and lost manipulation share a site: neither leaves anything on the ground, and both destroy their pawns.
        var sharedCell = sites.Dequeue();

        // ---- 18: recruiting or enslaving a downed raider -------------------------------------------------------------------
        // Downing never drops apparel, and a colony pawn's gear is never sorted, so a recruit's armour is sorted as he joins.
        {
            var c = ctx.Begin(Recruit);
            bool ready = true;
            var cell = sharedCell;
            var ways = ModsConfig.IdeologyActive ? new[] { "recruit", "enslave" } : new[] { "recruit" };
            if (!ModsConfig.IdeologyActive) c.Note("Enslaving needs Ideology; only recruiting runs on this mod set");
            foreach (string way in ways)
            {
                ModHarness.RestoreSettings();
                var p = Spawn(ctx, Mercenary, Pirates, cell);
                var worn = Dress(p, vest, QualityCategory.Normal);
                int expected = ScrapAmounts.Units(worn.MarketValue, 1);
                c.Subject(p);
                HealthUtility.TryAnesthetize(p);
                p.guest.SetGuestStatus(Faction.OfPlayer, GuestStatus.Prisoner);
                if (!Kit.Require(ctx, Recruit, (p.Downed && p.IsPrisonerOfColony && p.apparel.Contains(worn), "the pirate is a downed colony prisoner still wearing the vest")))
                {
                    ready = false;
                    break;
                }
                if (way == "recruit") RecruitUtility.Recruit(p, Faction.OfPlayer);
                else p.guest.SetGuestStatus(Faction.OfPlayer, GuestStatus.Slave); // what GenGuest.EnslavePrisoner calls
                int inPack = p.inventory.innerContainer.Where(t => t.def == industrial).Sum(t => t.stackCount);
                c.Observe(way + "Faction", p.Faction?.Name ?? "none").Observe(way + "ScrapInPack", inPack)
                    .Observe(way + "Vest", worn.Destroyed ? "destroyed" : p.apparel.Contains(worn) ? "worn" : "elsewhere");
                c.Step(p.Faction == Faction.OfPlayer, $"{way}: he joins the colony");
                c.Step(worn.Destroyed && inPack == expected, $"{way}: the vest becomes {inPack} industrial scrap in his pack (expected {expected})");
                p.Destroy();
            }
            if (ready) c.Conclude();
        }

        // ---- 18: lost manipulation ------------------------------------------------------------------------------------------
        // Pawn_HealthTracker drops the weapon of a pawn that can no longer manipulate, without downing him.
        {
            var c = ctx.Begin(Manipulation);
            ModHarness.RestoreSettings();
            var cell = sharedCell;
            var p = Spawn(ctx, Mercenary, Pirates, cell);
            var gun = Arm(p, revolver, QualityCategory.Normal);
            int expected = ScrapAmounts.Units(gun.MarketValue, 1);
            c.Subject(p);
            var before = Kit.StackIds(map, revolver);
            foreach (var shoulder in p.RaceProps.body.AllParts.Where(x => x.def == BodyPartDefOf.Shoulder).ToList())
            {
                var missing = (Hediff_MissingPart)HediffMaker.MakeHediff(HediffDefOf.MissingBodyPart, p, shoulder);
                missing.IsFresh = false;
                p.health.AddHediff(missing, shoulder);
            }
            ctx.Setup($"Developer removed both shoulders of {p.LabelShort} (not fresh, so no bleeding)");
            if (Kit.Require(ctx, Manipulation, (!p.Dead && !p.Downed, "the pirate is alive and standing"),
                (!p.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation), "the pirate can no longer manipulate")))
            {
                int inPack = p.inventory.innerContainer.Where(t => t.def == industrial).Sum(t => t.stackCount);
                int whole = NewNear(map, revolver, before, cell).Count;
                c.Observe("primary", p.equipment.Primary?.def.defName ?? "none").Observe("scrapInPack", inPack).Observe("revolversOnGround", whole);
                c.Step(p.equipment.Primary == null, "he no longer holds a weapon");
                c.Step(gun.Destroyed && inPack == expected && whole == 0, $"the revolver became {inPack} industrial scrap in his pack (expected {expected}), none dropped whole");
                c.Conclude();
            }
            p.Destroy();
        }

        // ---- 18: no item judged twice ---------------------------------------------------------------------------------------
        // Death and downing call DropAndForbidEverything and TryDropEquipment, and stripping calls TryDropEquipment, from inside
        // the moments the mod already sorted. Each outcome the mod applies is counted per item.
        {
            var c = ctx.Begin(DropCallsOnce);
            var club = Kit.Def("MeleeWeapon_Club");
            var apply = AccessTools.Method("LootScavenging.Loot.OutcomeApplier:Apply");
            if (apply == null) c.SetupFailed("OutcomeApplier.Apply not found");
            else
            {
                var harmony = new Harmony(ModHarness.HarmonyIdMoments);
                judged.Clear();
                harmony.Patch(apply, prefix: new HarmonyMethod(typeof(MomentsBatch), nameof(CountJudged)));
                try
                {
                    var cell = sites.Dequeue();
                    foreach (string way in new[] { "death", "downed then death", "arrest", "strip" })
                    {
                        ModHarness.RestoreSettings();
                        Settings.values.deathDrop = DeathDrop.Ground;
                        Rules.weapons = new CategoryRule(Outcome.Degrade);
                        var p = Spawn(ctx, Mercenary, Pirates, cell);
                        var weapon = Arm(p, club, QualityCategory.Normal);
                        c.Subject(p);
                        int downed = -1;
                        switch (way)
                        {
                            case "death": p.Kill(null); break;
                            case "downed then death":
                                HealthUtility.TryAnesthetize(p);
                                downed = judged.TryGetValue(weapon, out int n) ? n : 0;
                                p.Kill(null);
                                break;
                            case "arrest": p.guest.SetGuestStatus(Faction.OfPlayer, GuestStatus.Prisoner); break;
                            case "strip": p.Strip(); break;
                        }
                        int times = judged.TryGetValue(weapon, out int t) ? t : 0;
                        c.Observe(way, times);
                        if (downed >= 0) c.Observe("atDowning", downed);
                        c.Step(times == 1 && (downed < 0 || downed == 1), $"{way}: the club is judged once ({times}{(downed >= 0 ? $", {downed} at the downing" : "")})");
                        c.Step(!weapon.Destroyed && weapon.HitPoints < weapon.MaxHitPoints, $"{way}: the club is degraded ({weapon.HitPoints}/{weapon.MaxHitPoints}), not destroyed");
                        p.Corpse?.Destroy();
                        if (!p.Destroyed) p.Destroy();
                        if (weapon.Spawned) weapon.Destroy();
                    }
                    c.Conclude();
                }
                finally { harmony.UnpatchAll(ModHarness.HarmonyIdMoments); }
            }
        }

        // ---- 03/10: scrap on the body ------------------------------------------------------------------------------------
        {
            var c = ctx.Begin(OnBody);
            ModHarness.RestoreSettings();
            Settings.values.deathDrop = DeathDrop.ScrapOnBody;
            var cell = sites.Dequeue();
            var p = Spawn(ctx, Mercenary, Pirates, cell);
            var gun = Arm(p, revolver, QualityCategory.Normal);
            int expected = ExpectedScrap(gun, Pirates);
            var before = Kit.StackIds(map, industrial);
            p.Kill(null);
            var corpse = p.Corpse;
            int inCorpse = corpse?.InnerPawn?.inventory?.innerContainer.Where(t => t.def == industrial).Sum(t => t.stackCount) ?? 0;
            int onGround = Units(NewNear(map, industrial, before, cell));
            c.Expect("inCorpse", expected).Observe("inCorpse", inCorpse).Observe("onGroundAtDeath", onGround);
            c.Step(gun.Destroyed && inCorpse == expected && onGround == 0, $"the scrap ({inCorpse}) stays in the corpse's inventory");
            before = Kit.StackIds(map, industrial);
            corpse?.Strip();
            int dropped = Units(NewNear(map, industrial, before, cell));
            c.Observe("droppedByStrip", dropped);
            c.Step(dropped == expected, "stripping the corpse drops it");
            c.Conclude();
        }
        {
            var c = ctx.Begin(OnBodyRemoved);
            ModHarness.RestoreSettings();
            Settings.values.deathDrop = DeathDrop.ScrapOnBody;
            Settings.values.corpses.humanlike = true;
            var cell = sites.Dequeue();
            var p = Spawn(ctx, Mercenary, Pirates, cell);
            var gun = Arm(p, revolver, QualityCategory.Normal);
            int expected = ExpectedScrap(gun, Pirates);
            var before = Kit.StackIds(map, industrial);
            p.Kill(null);
            int onGround = Units(NewNear(map, industrial, before, cell));
            c.Expect("onGround", expected).Observe("onGround", onGround);
            c.Step(p.Corpse == null || p.Corpse.Destroyed, "the corpse is removed");
            c.Step(onGround == expected, "the scrap it carried lies on the ground");
            c.Conclude();
        }

        // ---- 03/08/10: everything stays on the body ----------------------------------------------------------------------
        var silver = ThingDefOf.Silver;
        var chemfuel = ThingDefOf.Chemfuel;
        int InCorpse(Pawn dead, ThingDef def) =>
            dead.Corpse?.InnerPawn?.inventory?.innerContainer.Where(t => t.def == def).Sum(t => t.stackCount) ?? 0;
        {
            var c = ctx.Begin(AllOnBody);
            ModHarness.RestoreSettings();
            Settings.values.deathDrop = DeathDrop.AllOnBody;
            Rules.ejectCharges = true;
            var cell = sites.Dequeue();
            var p = Spawn(ctx, Mercenary, Pirates, cell);
            var gun = Arm(p, revolver, QualityCategory.Normal);
            var belt = Dress(p, Kit.Def("Apparel_SmokepopBelt"));
            p.inventory.innerContainer.TryAdd(Stack(silver, 50));
            c.Subject(p);
            int expectedScrap = ExpectedScrap(gun, Pirates) + ExpectedScrap(belt, Pirates);
            var watched = AllScrap.Concat(new[] { silver, chemfuel }).ToList();
            var before = watched.ToDictionary(d => d, d => Kit.StackIds(map, d));
            int Ground() => watched.Sum(d => Units(NewNear(map, d, before[d], cell)));
            p.Kill(null);
            int scrapIn = AllScrap.Sum(d => InCorpse(p, d)), silverIn = InCorpse(p, silver), fuelIn = InCorpse(p, chemfuel);
            int ground = Ground();
            c.Expect("scrapInCorpse", expectedScrap).Observe("scrapInCorpse", scrapIn).Observe("silverInCorpse", silverIn)
                .Observe("chemfuelInCorpse", fuelIn).Observe("onGroundAtDeath", ground);
            c.Step(gun.Destroyed && belt.Destroyed, "the revolver and smokepop belt are scrapped");
            c.Step(scrapIn == expectedScrap, $"their scrap ({scrapIn}, expected {expectedScrap}) is in the corpse's inventory");
            c.Step(fuelIn == 10, $"the belt's ejected chemfuel ({fuelIn}) is in the corpse's inventory");
            c.Step(silverIn == 50, $"the carried silver ({silverIn}) is in the corpse's inventory");
            c.Step(ground == 0, "nothing lands on the ground at death");
            before = watched.ToDictionary(d => d, d => Kit.StackIds(map, d));
            p.Corpse?.Strip();
            int dropped = Ground();
            c.Observe("droppedByStrip", dropped);
            c.Step(dropped == scrapIn + fuelIn + silverIn, $"stripping the corpse drops all of it ({dropped})");
            c.Conclude();
        }
        {
            var c = ctx.Begin(AllOnBodyDisassembled);
            ModHarness.RestoreSettings();
            Settings.values.deathDrop = DeathDrop.AllOnBody;
            Rules.dropMode = DropMode.Off;
            Rules.weapons = new CategoryRule(Outcome.Disassemble);
            Rules.disassemblyYield = DisassemblyYield.ItemBased;
            Rules.disassemblyEfficiency = 1f;
            var costs = revolver.CostList.Where(x => x.count > 0).ToList();
            var cell = sites.Dequeue();
            var p = Spawn(ctx, Mercenary, Pirates, cell);
            var gun = Arm(p, revolver, QualityCategory.Normal);
            c.Subject(p);
            var before = costs.ToDictionary(x => x.thingDef, x => Kit.StackIds(map, x.thingDef));
            p.Kill(null);
            c.Step(gun.Destroyed, "the revolver is disassembled");
            foreach (var cost in costs)
            {
                int inside = InCorpse(p, cost.thingDef), ground = Units(NewNear(map, cost.thingDef, before[cost.thingDef], cell));
                c.Expect(cost.thingDef.defName, cost.count).Observe(cost.thingDef.defName, inside).Observe(cost.thingDef.defName + "OnGround", ground);
                c.Step(inside == cost.count && ground == 0, $"its {cost.count} {cost.thingDef.label} are in the corpse's inventory ({inside}), none on the ground");
            }
            c.Conclude();
        }
        {
            var c = ctx.Begin(AllOnBodyRemoved);
            ModHarness.RestoreSettings();
            Settings.values.deathDrop = DeathDrop.AllOnBody;
            Settings.values.corpses.humanlike = true;
            var cell = sites.Dequeue();
            var p = Spawn(ctx, Mercenary, Pirates, cell);
            var gun = Arm(p, revolver, QualityCategory.Normal);
            p.inventory.innerContainer.TryAdd(Stack(silver, 50));
            c.Subject(p);
            int expected = ExpectedScrap(gun, Pirates);
            var beforeScrap = Kit.StackIds(map, industrial);
            var beforeSilver = Kit.StackIds(map, silver);
            p.Kill(null);
            int scrap = Units(NewNear(map, industrial, beforeScrap, cell)), silverOut = Units(NewNear(map, silver, beforeSilver, cell));
            c.Expect("scrapOnGround", expected).Observe("scrapOnGround", scrap).Observe("silverOnGround", silverOut);
            c.Step(p.Corpse == null || p.Corpse.Destroyed, "the corpse is removed");
            c.Step(scrap == expected && silverOut == 50, $"its scrap ({scrap}) and silver ({silverOut}) lie on the ground");
            c.Conclude();
        }
        {
            var c = ctx.Begin(AllOnBodySkips);
            ModHarness.RestoreSettings();
            Settings.values.deathDrop = DeathDrop.AllOnBody;
            var cell = sites.Dequeue();
            var colonist = Spawn(ctx, PawnKindDefOf.Colonist, Faction.OfPlayer, cell);
            var ownGun = Arm(colonist, revolver, QualityCategory.Normal);
            colonist.Kill(null);
            c.Step(!ownGun.Destroyed && ownGun.Spawned && InCorpse(colonist, revolver) == 0, "a colonist's revolver drops whole on the ground, not into the corpse");

            // The wreck cannot be stripped, so scrap left in it would be stuck: both on-body options drop it as usual.
            cell = sites.Dequeue();
            bool ready = true;
            foreach (var mode in new[] { DeathDrop.AllOnBody, DeathDrop.ScrapOnBody })
            {
                Settings.values.deathDrop = mode;
                var mech = PawnGenerator.GeneratePawn(new PawnGenerationRequest(DefDatabase<PawnKindDef>.GetNamed("Mech_Lancer"), Faction.OfMechanoids, forceGenerateNewPawn: true));
                GenSpawn.Spawn(mech, cell, map);
                var weapon = mech.equipment?.Primary;
                if (!Kit.Require(ctx, AllOnBodySkips, (weapon != null, "the lancer spawned with a weapon"),
                    (mech.kindDef.destroyGearOnDrop && !mech.kindDef.canStrip, "the lancer's kind destroys its gear and cannot be stripped")))
                {
                    ready = false;
                    break;
                }
                var tierDef = Scrap(ScrapTiers.TierFor(EraReader.ItemEra(weapon.def, mech.Faction).Era, EraReader.ColonyEra()));
                int expected = ExpectedScrap(weapon, mech.Faction);
                // Totals near the cell, not new stacks: the second lancer's scrap merges into the first one's pile.
                int Near() => map.listerThings.ThingsOfDef(tierDef).Where(t => t.Spawned && t.Position.DistanceTo(cell) <= 5f).Sum(t => t.stackCount);
                int start = Near();
                mech.Kill(null);
                int made = Near() - start;
                int inWreck = InCorpse(mech, tierDef);
                c.Expect("mechScrap" + mode, expected).Observe("mechScrap" + mode, made).Observe("inWreck" + mode, inWreck);
                c.Step(weapon.Destroyed && made == expected && inWreck == 0,
                    $"{mode}: the lancer's weapon becomes {made} {tierDef.defName} beside the wreck (expected {expected}), {inWreck} inside it");
                mech.Corpse?.Destroy();
            }
            if (ready) c.Conclude();
        }

        // ---- everything stays on the body when downed ---------------------------------------------------------------------
        {
            var c = ctx.Begin(AllOnBodyDowned);
            var club = Kit.Def("MeleeWeapon_Club");
            bool ready = true;
            foreach (bool downedHandling in new[] { true, false })
            {
                ModHarness.RestoreSettings();
                Settings.values.deathDrop = DeathDrop.AllOnBody;
                Rules.onDownedAndStrip = downedHandling;
                string tag = downedHandling ? "On" : "Off";
                var cell = sites.Dequeue();
                var p = Spawn(ctx, Mercenary, Pirates, cell);
                var held = Arm(p, club, QualityCategory.Normal);
                var carried = Stack(revolver, 1);
                p.inventory.innerContainer.TryAdd(carried);
                p.inventory.innerContainer.TryAdd(Stack(silver, 50));
                c.Subject(p);
                c.Note($"Everything stays on the body; downed and strip {(downedHandling ? "on" : "off")}. A pirate with a club in hand and a revolver and 50 silver in his pack is downed, then despawned and spawned again");
                c.Step(!EraReader.ItemEra(club, Pirates).OutOfReach && EraReader.ItemEra(revolver, Pirates).OutOfReach,
                    "the club is in reach of the tribal colony and the revolver is not");
                int expectedScrap = ExpectedScrap(carried, Pirates);
                var watched = AllScrap.Concat(new[] { silver, club, revolver }).ToList();
                var before = watched.ToDictionary(d => d, d => Kit.StackIds(map, d));
                int Ground() => watched.Sum(d => Units(NewNear(map, d, before[d], cell)));
                int Pack(ThingDef def) => p.inventory.innerContainer.Where(t => t.def == def).Sum(t => t.stackCount);
                HealthUtility.TryAnesthetize(p);
                if (!Kit.Require(ctx, AllOnBodyDowned, (p.Downed && !p.Dead && p.Spawned, "the pirate is downed and alive")))
                {
                    ready = false;
                    break;
                }
                // A few ticks so anything the game drops later than MakeDowned shows too.
                yield return Wait.Ticks(60);
                int ground = Ground(), scrapIn = AllScrap.Sum(Pack), silverIn = Pack(silver), revolverIn = Pack(revolver);
                c.Observe("primary" + tag, p.equipment?.Primary?.def.defName ?? "none").Observe("clubInPack" + tag, Pack(club)).Observe("scrapInPack" + tag, scrapIn)
                    .Observe("revolverInPack" + tag, revolverIn).Observe("silverInPack" + tag, silverIn).Observe("onGround" + tag, ground);
                c.Step(p.Downed && p.equipment?.Primary == null && p.inventory.innerContainer.Contains(held), $"{tag}: the club is in his pack, not his hands");
                if (downedHandling)
                {
                    c.Expect("scrapInPack" + tag, expectedScrap);
                    c.Step(carried.Destroyed && scrapIn == expectedScrap && revolverIn == 0,
                        $"{tag}: the carried revolver is scrapped into his pack ({scrapIn} scrap, expected {expectedScrap})");
                }
                else
                    c.Step(!carried.Destroyed && revolverIn == 1 && scrapIn == 0, $"{tag}: the carried revolver stays whole in his pack");
                c.Step(silverIn == 50, $"{tag}: the 50 silver stay in his pack ({silverIn})");
                c.Step(ground == 0, $"{tag}: nothing lands on the ground ({ground})");
                // Spawning a downed pawn again (a reload, or being carried and set down) drops any weapon it holds
                // (Pawn_EquipmentTracker.Notify_PawnSpawned); one in the pack stays.
                p.DeSpawn();
                GenSpawn.Spawn(p, cell, map);
                int groundAfterRespawn = Ground();
                c.Observe("onGroundAfterRespawn" + tag, groundAfterRespawn);
                c.Step(p.Downed && p.inventory.innerContainer.Contains(held) && groundAfterRespawn == 0,
                    $"{tag}: spawned again, he still has the club in his pack and nothing lands on the ground ({groundAfterRespawn})");
                p.Destroy();
            }
            if (ready) c.Conclude();
        }

        // ---- review 2026-11-24: a thing kept at a downing is judged once ----------------------------------------------
        {
            var c = ctx.Begin(JudgedOnce);
            ModHarness.RestoreSettings();
            Settings.values.deathDrop = DeathDrop.AllOnBody;
            Rules.onDownedAndStrip = true;
            Rules.weapons.outcome = Outcome.Keep;
            Rules.other.outcome = Outcome.Delete;
            Rules.other.chance = 1f;
            var club = Kit.Def("MeleeWeapon_Club");
            var cell = sites.Dequeue();
            var p = Spawn(ctx, Mercenary, Pirates, cell);
            var held = Arm(p, club, QualityCategory.Normal);
            c.Subject(p);
            c.Note("Everything on the body, downed handling on, weapons Keep, other inventory Delete. A pirate holding a club is downed, then killed");
            c.Step(!EraReader.ItemEra(club, Pirates).OutOfReach, "the club is in reach of the tribal colony");
            HealthUtility.TryAnesthetize(p);
            if (Kit.Require(ctx, JudgedOnce, (p.Downed && !p.Dead, "the pirate is downed and alive")))
            {
                c.Step(p.inventory.innerContainer.Contains(held), "the club is kept and goes into his pack");
                p.Kill(null);
                bool inCorpse = p.Corpse?.InnerPawn?.inventory?.innerContainer.Contains(held) ?? false;
                c.Observe("clubDestroyed", held.Destroyed).Observe("clubInCorpse", inCorpse);
                c.Step(!held.Destroyed && inCorpse, "at death the club is not judged again as other inventory: it stays whole in the corpse");
                c.Conclude();
            }
            p.Corpse?.Destroy();
        }

        // ---- review 2026-11-24: locked apparel is sorted at death -----------------------------------------------------
        {
            var c = ctx.Begin(LockedApparel);
            ModHarness.RestoreSettings();
            Settings.values.deathDrop = DeathDrop.Ground;
            var cell = sites.Dequeue();
            var p = Spawn(ctx, Mercenary, Pirates, cell);
            var worn = Dress(p, vest, QualityCategory.Normal);
            p.apparel.Lock(worn);
            c.Subject(p);
            c.Note("Loot lands on the ground. A pirate wears a flak vest the game has locked on him, then dies");
            c.Step(EraReader.ItemEra(vest, Pirates).OutOfReach && p.apparel.IsLocked(worn), "the vest is out of reach and locked");
            var before = Kit.StackIds(map, industrial);
            p.Kill(null);
            int scrap = Units(NewNear(map, industrial, before, cell));
            c.Observe("vestDestroyed", worn.Destroyed).Observe("scrapNear", scrap);
            c.Step(worn.Destroyed && scrap > 0, $"the locked vest is scrapped at death ({scrap} scrap)");
            c.Conclude();
            p.Corpse?.Destroy();
        }

        // ---- review 2026-11-24: a former colonist keeps the colony's gear ---------------------------------------------
        {
            var c = ctx.Begin(FormerColonist);
            ModHarness.RestoreSettings();
            Rules.onDownedAndStrip = true;
            var cell = sites.Dequeue();
            var p = Spawn(ctx, PawnKindDefOf.Colonist, Faction.OfPlayer, cell);
            var gun = Arm(p, revolver, QualityCategory.Normal);
            // Time records only grow in the tracker's interval update (AddTo refuses them); one 80-tick update as a colonist
            // gives the record the game checks.
            p.records.RecordsTickMothballed(80);
            c.Step(PawnUtility.EverBeenColonistOrTameAnimal(p), "he has colonist time on record");
            p.SetFaction(null);
            ctx.Setup("Developer ran one 80-tick records update for the colonist and removed its faction (as a wild-man break does)");
            c.Subject(p);
            c.Step(p.Faction == null && Rules.IsGroupOn(OwnerGroup.NoFaction) && EraReader.ItemEra(revolver, null).OutOfReach,
                "he has no faction, the no-faction group is on and the revolver is out of reach");
            HealthUtility.TryAnesthetize(p);
            if (Kit.Require(ctx, FormerColonist, (p.Downed && !p.Dead, "he is downed and alive")))
            {
                c.Observe("revolverDestroyed", gun.Destroyed);
                c.Step(!gun.Destroyed, "downing leaves the colony's revolver whole");
                c.Conclude();
            }
            p.Destroy();
            if (!gun.Destroyed) gun.Destroy();
        }

        // ---- review 2026-11-24: protected items are never unfamiliar --------------------------------------------------
        {
            var c = ctx.Begin(ProtectedFamiliar);
            ModHarness.RestoreSettings();
            Rules.dropMode = DropMode.KeepOriginal;
            Settings.values.unfamiliarScope = UnfamiliarScope.AllOutOfReach;
            Unfamiliar.ResetCache();
            var cell = sites.Dequeue();
            var rifle = (ThingWithComps)GenSpawn.Spawn(Make(Kit.Def("Gun_ChargeRifle"), QualityCategory.Normal), cell, map);
            c.Note("Keep-original mode, all out-of-reach gear. A charge rifle lies on the map");
            bool before = Unfamiliar.IsUnfamiliar(rifle);
            Rules.keepList.Add(rifle.def.defName);
            bool listed = Unfamiliar.IsUnfamiliar(rifle);
            c.Observe("unfamiliar", before).Observe("unfamiliarKeepListed", listed);
            c.Step(before, "the charge rifle is unfamiliar at the tribal colony");
            c.Step(!listed, "on the keep list it is not unfamiliar");
            rifle.Destroy();
            var personaDef = DefDatabase<ThingDef>.GetNamedSilentFail("MeleeWeapon_MonoSwordBladelink");
            if (ModsConfig.RoyaltyActive && personaDef != null)
            {
                var persona = (ThingWithComps)GenSpawn.Spawn(Make(personaDef, QualityCategory.Normal), cell, map);
                bool unfamiliar = Unfamiliar.IsUnfamiliar(persona);
                c.Observe("personaUnfamiliar", unfamiliar);
                c.Step(EraReader.ItemEra(personaDef, null).OutOfReach && !unfamiliar, "a persona monosword is out of reach but not unfamiliar");
                persona.Destroy();
            }
            else
                c.Note("Royalty is not loaded, so the persona weapon was not checked");
            c.Conclude();
            Unfamiliar.ResetCache();
        }

        // ---- 20: pawns who join the colony -----------------------------------------------------------------------------------
        // A wanderer has no faction; a recruited prisoner keeps his old one. Both go through Pawn.SetFaction. Nothing lands on
        // the ground, so the cases share a site.
        {
            var c = ctx.Begin(JoinMark);
            ModHarness.RestoreSettings();
            Rules.dropMode = DropMode.ScrapOutOfReach;
            Rules.prisonerJoinGear = JoinerGear.FollowDropMode;
            Rules.otherJoinGear = JoinerGear.MarkUnfamiliar;
            var club = Kit.Def("MeleeWeapon_Club");
            var p = Spawn(ctx, Mercenary, null, sharedCell);
            var worn = Dress(p, vest, QualityCategory.Normal);
            var gun = Arm(p, revolver, QualityCategory.Normal);
            var control = Make(vest, QualityCategory.Normal);
            c.Subject(p);
            c.Step(EraReader.ItemEra(vest, null).OutOfReach && EraReader.ItemEra(revolver, null).OutOfReach && p.Faction == null,
                "a wanderer with no faction wears an out-of-reach vest and holds an out-of-reach revolver");
            p.SetFaction(Faction.OfPlayer);
            Unfamiliar.ResetCache();
            c.Observe("vest", worn.Destroyed ? "destroyed" : p.apparel.Contains(worn) ? "worn" : "elsewhere")
                .Observe("revolver", gun.Destroyed ? "destroyed" : p.equipment.Contains(gun) ? "held" : "elsewhere");
            c.Step(p.Faction == Faction.OfPlayer && p.apparel.Contains(worn) && p.equipment.Contains(gun), "he joins the colony still wearing the vest and holding the revolver");
            c.Step(Unfamiliar.IsUnfamiliar(worn) && Unfamiliar.IsUnfamiliar(gun), "both are unfamiliar although the drop mode is scrap out-of-reach");
            c.Step(Unfamiliar.Blocks(worn, p) && Unfamiliar.Blocks(gun, p), "no colonist, himself included, can equip them");
            c.Step(!Unfamiliar.IsUnfamiliar(control), "a vest nobody joined in is not unfamiliar");
            p.apparel.Remove(worn);
            c.Step(Unfamiliar.IsUnfamiliar(worn), "the vest stays unfamiliar once it is off him");
            Rules.dropMode = DropMode.Off;
            Unfamiliar.ResetCache();
            c.Step(Unfamiliar.IsUnfamiliar(worn), "and in drop mode Off");
            p.Destroy();
            worn.Destroy();
            control.Destroy();
            c.Conclude();
        }
        {
            var c = ctx.Begin(JoinLeave);
            ModHarness.RestoreSettings();
            Rules.otherJoinGear = JoinerGear.LeaveAlone;
            var p = Spawn(ctx, Mercenary, null, sharedCell);
            var worn = Dress(p, vest, QualityCategory.Normal);
            c.Subject(p);
            p.SetFaction(Faction.OfPlayer);
            Unfamiliar.ResetCache();
            c.Step(p.apparel.Contains(worn) && !worn.Destroyed, "he keeps the vest");
            c.Step(!Unfamiliar.IsUnfamiliar(worn) && worn.GetComp<CompUnfamiliar>().joinedEra == Era.Undefined, "and it carries no mark");
            p.Destroy();
            c.Conclude();
        }
        {
            var c = ctx.Begin(JoinFollow);
            bool ready = true;
            var ways = new List<string> { "wanderer" };
            ways.Add("recruit");
            if (ModsConfig.IdeologyActive) ways.Add("enslave");
            else c.Note("Enslaving needs Ideology; only the wanderer and the recruit run on this mod set");
            foreach (string way in ways)
            {
                ModHarness.RestoreSettings();
                Rules.onDownedAndStrip = false; // the join settings decide on their own
                Rules.prisonerJoinGear = JoinerGear.FollowDropMode;
                Rules.otherJoinGear = JoinerGear.FollowDropMode;
                var p = Spawn(ctx, Mercenary, way == "wanderer" ? null : Pirates, sharedCell);
                var worn = Dress(p, vest, QualityCategory.Normal);
                if (way != "wanderer")
                    p.guest.SetGuestStatus(Faction.OfPlayer, GuestStatus.Prisoner); // the arrest drops nothing: he holds nothing yet
                var gun = Arm(p, revolver, QualityCategory.Normal);
                int expectedVest = ScrapAmounts.Units(worn.MarketValue, 1), expectedGun = ScrapAmounts.Units(gun.MarketValue, 1);
                c.Subject(p);
                if (!Kit.Require(ctx, JoinFollow, (way == "wanderer" ? p.Faction == null : p.IsPrisonerOfColony, "he is a " + (way == "wanderer" ? "wanderer" : "colony prisoner")),
                    (p.apparel.Contains(worn) && p.equipment.Contains(gun), "he wears the vest and holds the revolver")))
                {
                    ready = false;
                    break;
                }
                if (way == "recruit") RecruitUtility.Recruit(p, Faction.OfPlayer);
                else if (way == "enslave") p.guest.SetGuestStatus(Faction.OfPlayer, GuestStatus.Slave);
                else p.SetFaction(Faction.OfPlayer);
                int inPack = p.inventory.innerContainer.Where(t => t.def == industrial).Sum(t => t.stackCount);
                c.Observe(way + "ScrapInPack", inPack).Observe(way + "Faction", p.Faction?.Name ?? "none");
                c.Step(p.Faction == Faction.OfPlayer, $"{way}: he joins the colony");
                c.Step(worn.Destroyed && gun.Destroyed && inPack == expectedVest + expectedGun,
                    $"{way}: vest and revolver become {inPack} industrial scrap in his pack, rolled once (expected {expectedVest + expectedGun})");
                p.Destroy();
            }
            if (ready) c.Conclude();
        }
        {
            var c = ctx.Begin(JoinPrisonerRules);
            // The prisoner setting, not the one for everyone else, decides for a recruit.
            foreach (JoinerGear setting in new[] { JoinerGear.MarkUnfamiliar, JoinerGear.LeaveAlone })
            {
                ModHarness.RestoreSettings();
                Rules.prisonerJoinGear = setting;
                Rules.otherJoinGear = JoinerGear.FollowDropMode;
                var p = Spawn(ctx, Mercenary, Pirates, sharedCell);
                var worn = Dress(p, vest, QualityCategory.Normal);
                p.guest.SetGuestStatus(Faction.OfPlayer, GuestStatus.Prisoner);
                c.Subject(p);
                RecruitUtility.Recruit(p, Faction.OfPlayer);
                Unfamiliar.ResetCache();
                bool marked = setting == JoinerGear.MarkUnfamiliar;
                c.Observe(setting.ToString(), worn.Destroyed ? "destroyed" : Unfamiliar.IsUnfamiliar(worn) ? "unfamiliar" : "plain");
                c.Step(!worn.Destroyed && p.apparel.Contains(worn), $"{setting}: the recruit keeps wearing the vest");
                c.Step(Unfamiliar.IsUnfamiliar(worn) == marked, $"{setting}: the vest is {(marked ? "" : "not ")}unfamiliar");
                p.Destroy();
            }
            c.Conclude();
        }
        {
            var c = ctx.Begin(JoinProtected);
            foreach (JoinerGear setting in new[] { JoinerGear.MarkUnfamiliar, JoinerGear.FollowDropMode })
            {
                ModHarness.RestoreSettings();
                Rules.otherJoinGear = setting;
                var p = Spawn(ctx, Mercenary, null, sharedCell);
                var worn = Dress(p, vest, QualityCategory.Normal);
                worn.questTags = new List<string> { "LootTest" };
                c.Subject(p);
                p.SetFaction(Faction.OfPlayer);
                Unfamiliar.ResetCache();
                c.Step(!worn.Destroyed && p.apparel.Contains(worn) && !Unfamiliar.IsUnfamiliar(worn), $"{setting}: a quest-tagged vest is kept and never marked");
                p.Destroy();
            }
            c.Conclude();
        }
        {
            var c = ctx.Begin(JoinFormer);
            foreach (JoinerGear setting in new[] { JoinerGear.MarkUnfamiliar, JoinerGear.FollowDropMode })
            {
                ModHarness.RestoreSettings();
                Rules.otherJoinGear = setting;
                var p = Spawn(ctx, PawnKindDefOf.Colonist, Faction.OfPlayer, sharedCell);
                var worn = Dress(p, vest, QualityCategory.Normal);
                p.records.RecordsTickMothballed(80);
                p.SetFaction(null);
                c.Subject(p);
                c.Step(PawnUtility.EverBeenColonistOrTameAnimal(p) && p.Faction == null, "he has colonist time on record and no faction");
                p.SetFaction(Faction.OfPlayer);
                Unfamiliar.ResetCache();
                c.Step(!worn.Destroyed && p.apparel.Contains(worn) && !Unfamiliar.IsUnfamiliar(worn), $"{setting}: a former colonist who rejoins keeps his vest, unmarked");
                p.Destroy();
            }
            c.Conclude();
        }

        // ---- 10: death off the map ---------------------------------------------------------------------------------------
        {
            var c = ctx.Begin(OffMap);
            ModHarness.RestoreSettings();
            long mark = CaseErrors.ModMark;
            var p = PawnGenerator.GeneratePawn(new PawnGenerationRequest(Mercenary, Pirates, forceGenerateNewPawn: true));
            Find.WorldPawns.PassToWorld(p);
            p.Kill(null);
            c.Observe("modErrors", CaseErrors.ModMark - mark);
            c.Step(p.Dead && p.Corpse == null, "a world pawn dies with no corpse on any map");
            c.Step(CaseErrors.ModMark == mark, "no mod error is logged");
            c.Conclude();
        }

        // ---- 10: butchering ------------------------------------------------------------------------------------------------
        {
            var c = ctx.Begin(Butcher);
            ModHarness.RestoreSettings();
            Rules.dropMode = DropMode.Off;
            var spotCell = sites.Dequeue();
            var spot = Kit.Place(ctx, "ButcherSpot", spotCell);
            var cook = Kit.Worker(ctx, spotCell + new IntVec3(0, 0, -3), DefDatabase<WorkTypeDef>.GetNamed("Cooking"), SkillDefOf.Cooking);
            c.Subject(cook);
            var recipe = DefDatabase<RecipeDef>.GetNamed("ButcherCorpseFlesh");
            var bill = (Bill_Production)recipe.MakeNewBill();
            bill.repeatMode = BillRepeatModeDefOf.Forever;
            // Everything the recipe can take, special filters (strangers' corpses) included.
            bill.ingredientFilter.SetAllowAll(recipe.fixedIngredientFilter);
            ((IBillGiver)spot).BillStack.AddBill(bill);
            var results = new List<(bool option, bool butchered, bool destroyed, bool spawned)>();
            foreach (bool option in new[] { true, false })
            {
                Settings.values.corpses.butcherRemovesApparel = option;
                var p = Spawn(ctx, Mercenary, Pirates, spotCell + new IntVec3(3, 0, 0));
                var shirt = Dress(p, Kit.Def("Apparel_CollarShirt"), QualityCategory.Normal);
                p.Kill(null);
                var corpse = p.Corpse;
                corpse?.SetForbidden(false, false);
                c.Note($"Butchering removes worn apparel: {(option ? "on" : "off")}; a pirate's corpse in a collared shirt lies by the butcher spot");
                var done = Wait.Until(() => corpse == null || corpse.Destroyed, 6000, Kit.KeepRested(cook));
                yield return done;
                results.Add((option, !done.TimedOut, shirt.Destroyed, shirt.Spawned));
                c.Observe(option ? "optionOn" : "optionOff", $"butchered {!done.TimedOut}, shirt destroyed {shirt.Destroyed}, on ground {shirt.Spawned}");
            }
            if (results.Any(r => !r.butchered))
                c.WorkflowInconclusive("the cook did not butcher a corpse within 6000 ticks",
                    (cook.Spawned && !cook.Dead, "the cook is alive"), (bill.ShouldDoNow(), "the bill wants doing"));
            else
            {
                c.Step(results[0].destroyed && !results[0].spawned, "with the option on the shirt is destroyed with the corpse");
                c.Step(!results[1].destroyed && results[1].spawned, "with the option off the shirt is left on the ground");
                c.Conclude();
            }
            bill.suspended = true;
        }

        // ---- 01/10: home-map-only on an away map -------------------------------------------------------------------------
        {
            Map away = null;
            MapParent parent = null;
            string problem = null;
            try
            {
                if (!TileFinder.TryFindPassableTileWithTraversalDistance(map.Tile, 2, 8, out PlanetTile tile, t => !Find.WorldObjects.AnyWorldObjectAt(t)))
                    problem = "no free tile near home";
                else
                {
                    away = GetOrGenerateMapUtility.GetOrGenerateMap(tile, new IntVec3(50, 1, 50), WorldObjectDefOf.Camp);
                    parent = away.Parent;
                }
            }
            catch (Exception e) { problem = "generating an away map failed: " + e.Message; }
            if (problem != null || away == null) ctx.Begin(HomeOnly).SetupFailed(problem ?? "no away map");
            else
            {
                var c = ctx.Begin(HomeOnly);
                ctx.Setup($"Developer generated a {away.Size.x}x{away.Size.z} camp map at tile {away.Tile}; IsPlayerHome {away.IsPlayerHome}");
                c.Step(!away.IsPlayerHome, "the camp map is not a home map");
                var cell = CellFinder.RandomClosewalkCellNear(away.Center, away, 5);
                foreach (bool homeOnly in new[] { true, false })
                {
                    ModHarness.RestoreSettings();
                    Rules.homeMapOnly = homeOnly;
                    var p = Spawn(ctx, Mercenary, Pirates, cell, away);
                    var gun = Arm(p, revolver, QualityCategory.Normal);
                    int scrapBefore = AllScrap.Sum(d => Kit.CountOnMap(away, d));
                    p.Kill(null);
                    int made = AllScrap.Sum(d => Kit.CountOnMap(away, d)) - scrapBefore;
                    if (homeOnly) c.Step(!gun.Destroyed && gun.Spawned && made == 0, "with home-map-only on, a pirate's revolver drops whole on the camp map");
                    else c.Step(gun.Destroyed && made > 0, $"with it off, the revolver becomes {made} scrap there");
                    cell = CellFinder.RandomClosewalkCellNear(away.Center, away, 8);
                }
                c.Conclude();
            }
            if (away != null)
            {
                Current.Game.DeinitAndRemoveMap(away, false);
                parent?.Destroy();
                if (Find.CurrentMap != map) Current.Game.CurrentMap = map;
            }
        }

        {
            var c = ctx.Begin(NoErrors);
            c.Observe("modErrors", CaseErrors.ModMark - modErrorsAtStart);
            c.Step(CaseErrors.ModMark == modErrorsAtStart, "no error naming Loot Scavenging was logged during the batch");
            c.Conclude();
        }
    }
}
