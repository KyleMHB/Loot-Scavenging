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
        DropCallsOnce = P + "drop-calls-judged-once", KeepsWeapon = P + "downed-keeps-weapon-in-hand",
        Carried = P + "downed-carried-keeps-weapon", Rescued = P + "downed-rescued-keeps-weapon", CapturedGround = P + "downed-captured-weapon-sorted",
        CapturedInBed = P + "downed-in-bed-captured-weapon-sorted", HospitalityGuest = P + "hospitality-downed-guest",
        JoinMark = P + "joiner-marked-unfamiliar", JoinLeave = P + "joiner-left-alone", JoinFollow = P + "joiner-follows-drop-mode",
        JoinPrisonerRules = P + "joiner-prisoner-setting", JoinProtected = P + "joiner-protected-never-marked", JoinFormer = P + "joiner-former-colonist-untouched",
        ColonistRescued = P + "colonist-downed-rescued-keeps-gear", ColonistKilled = P + "colonist-killed-keeps-gear", ColonistToggleOff = P + "colonist-gear-toggle-off",
        VisitorDropped = P + "visitor-drop-weapon-sorted";

    public override string Id => "lootscavenging-moments";
    public override int SegmentCount => 1;
    public override int MapSize => 100;

    public override IReadOnlyList<PlannedCase> Plan => new[]
    {
        new PlannedCase(DownStripOn, Levels.Mechanical, "with downed pawns dropping the weapon, downing scraps an out-of-reach weapon and stripping scraps out-of-reach apparel"),
        new PlannedCase(DownStripOff, Levels.Mechanical, "with downed pawns dropping the weapon and sorting when stripped off, downing still scraps the weapon and the strip drops the vest whole"),
        new PlannedCase(Prisoner, Levels.Mechanical, "stripping a living colony prisoner scraps out-of-reach apparel"),
        new PlannedCase(Arrest, Levels.Mechanical, "arresting a standing pirate scraps his out-of-reach revolver, also with sorting when stripped off; with sorting when arrested off it drops whole"),
        new PlannedCase(Recruit, Levels.Mechanical, "recruiting (and, with Ideology, enslaving) a downed, captured raider scraps his out-of-reach flak vest"),
        new PlannedCase(Manipulation, Levels.Mechanical, "a standing pirate who loses both arms has his out-of-reach revolver scrapped, not dropped whole"),
        new PlannedCase(DropCallsOnce, Levels.Mechanical, "a Degrade weapon rule judges the weapon once at death, downing then death (kept in hand, and dropped, each also with everything on the body), downing then capture, arrest and strip"),
        new PlannedCase(KeepsWeapon, Levels.Mechanical, "a downed pirate keeps his out-of-reach revolver whole in hand and his medicine, nothing on the ground; a strip, a capture or his death scraps it, with stripping off a strip drops it whole, and he gets up still holding it"),
        new PlannedCase(Carried, Levels.PlayerWorkflow, "a drafted colonist carries a downed pirate and is undrafted, which sets him down, twice: he keeps his out-of-reach revolver in hand, nothing lands, and he gets up holding it"),
        new PlannedCase(Rescued, Levels.PlayerWorkflow, "a colonist rescues a downed visitor from a friendly faction into a medical bed: he keeps his out-of-reach revolver in hand and nothing is scrapped"),
        new PlannedCase(CapturedGround, Levels.PlayerWorkflow, "a colonist captures a downed pirate into a prison bed: the arrest scraps his out-of-reach revolver"),
        new PlannedCase(CapturedInBed, Levels.PlayerWorkflow, "a colonist captures the rescued visitor from his medical bed into a prison bed: the arrest scraps his out-of-reach revolver"),
        new PlannedCase(HospitalityGuest, Levels.PlayerWorkflow, "with Hospitality (--mods compat-hospitality): a downed friendly visitor keeps his revolver in hand and is rescued into a bed holding it; with downed pawns dropping the weapon his out-of-reach revolver drops as scrap; no error is logged"),
        new PlannedCase(ColonistRescued, Levels.PlayerWorkflow, "with downed pawns dropping the weapon, a downed colonist keeps his out-of-reach revolver and medicine, is rescued into a medical bed holding it, still holds it when set down again, and gets up holding it; nothing is scrapped"),
        new PlannedCase(ColonistKilled, Levels.Mechanical, "with loot on the ground at death, a killed colonist keeps his out-of-reach revolver and medicine in the corpse, whole; nothing lands"),
        new PlannedCase(ColonistToggleOff, Levels.Mechanical, "with colonists keeping their gear off and downed pawns dropping the weapon, a downed colonist and a killed one drop the revolver whole"),
        new PlannedCase(VisitorDropped, Levels.Mechanical, "with downed pawns dropping the weapon and friendly factions on, a downed visitor's out-of-reach revolver drops as scrap"),
        new PlannedCase(JoinMark, Levels.Mechanical, "a wanderer joins in out-of-reach armour and a revolver and keeps them; both are unfamiliar in the scrap drop mode and in Off, even off him; a revolver in his pack is marked and his medicine is not"),
        new PlannedCase(JoinLeave, Levels.Mechanical, "with joiner gear left alone, a wanderer's vest carries no mark"),
        new PlannedCase(JoinFollow, Levels.Mechanical, "a wanderer, a recruited prisoner and (with Ideology) an enslaved one have vest, revolver and a pack revolver scrapped once and their medicine kept, with arrest and strip sorting off"),
        new PlannedCase(JoinPrisonerRules, Levels.Mechanical, "a recruit follows the prisoner setting, not the other one: marked or left alone"),
        new PlannedCase(JoinProtected, Levels.Mechanical, "a quest-tagged vest on a joiner is never marked or scrapped"),
        new PlannedCase(JoinFormer, Levels.Mechanical, "a former colonist rejoining keeps his vest unmarked and unsorted"),
        new PlannedCase(OnBody, Levels.Mechanical, "scrap left on the body is in the corpse; stripping it drops the scrap"),
        new PlannedCase(OnBodyRemoved, Levels.Mechanical, "with corpse removal on, scrap left on the body lands on the ground"),
        new PlannedCase(AllOnBody, Levels.Mechanical, "everything on the body: scrap, ejected charges and inventory stay in the corpse; stripping drops them"),
        new PlannedCase(AllOnBodyDisassembled, Levels.Mechanical, "everything on the body: disassembly products stay in the corpse"),
        new PlannedCase(AllOnBodyRemoved, Levels.Mechanical, "everything on the body with corpse removal on: the loot lands on the ground"),
        new PlannedCase(AllOnBodySkips, Levels.Mechanical, "everything on the body, colonists keeping their gear off: a colonist's gear and a mechanoid's scrap drop as usual"),
        new PlannedCase(AllOnBodyDowned, Levels.Mechanical, "everything on the body, downed pawns dropping the weapon: the death setting does not apply at a downing, so a downed pirate's in-reach club drops whole, his carried revolver as scrap and his silver beside him, with arrest and strip sorting on and off"),
        new PlannedCase(JudgedOnce, Levels.Mechanical, "everything on the body, downed pawns dropping the weapon: a club kept and dropped at a downing is not judged again when the pirate dies"),
        new PlannedCase(LockedApparel, Levels.Mechanical, "a pirate's locked out-of-reach vest is scrapped at death"),
        new PlannedCase(FormerColonist, Levels.Mechanical, "with downed pawns dropping the weapon, a colonist who ran wild (no faction) keeps an out-of-reach revolver whole when downed"),
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
        ModHarness.SaveSettings(ModHarness.LootOnGround);
        // The sites take the first 14 rows; the ward for the carry, rescue and capture cases takes the rest.
        var layout = Kit.Area(ctx, 94, 24);
        var area = new CellRect(layout.minX, layout.minZ, 94, 14);
        var ward = new CellRect(layout.minX, layout.minZ + 14, 30, 10);
        Kit.RemoveStartingColonists(ctx);
        yield return Wait.Ticks(1);
        var map = ctx.Map;
        MakeTribal(ctx);
        // Two rows 6 cells apart: the first row's sites ran out.
        var sites = new Queue<IntVec3>(Sites(area, 13, 7).Concat(Sites(area, 13, 7).Select(c => c + new IntVec3(0, 0, -6))));
        var revolver = Kit.Def("Gun_Revolver");
        var vest = Kit.Def("Apparel_FlakVest");
        var industrial = Scrap(ScrapTier.Industrial);

        // ---- 01: downed and strip on, then off ---------------------------------------------------------------------------
        // Ticket 24: under DropWeapon a downing always sorts the weapon; only the strip follows its toggle.
        foreach (bool on in new[] { true, false })
        {
            var c = ctx.Begin(on ? DownStripOn : DownStripOff);
            ModHarness.RestoreSettings();
            Rules.sortWhenStripped = on;
            Rules.downedGear = DownedGear.DropWeapon;
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
            c.Step(gun.Destroyed && afterDown > 0, $"downing turns the revolver into scrap ({afterDown} units)");
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
            foreach (var (on, strip) in new[] { (true, true), (true, false), (false, true) })
            {
                ModHarness.RestoreSettings();
                Rules.sortWhenArrested = on;
                Rules.sortWhenStripped = strip;
                string tag = (on ? "On" : "Off") + (strip ? "" : "StripOff");
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
                    // Kept in hand, a downed pawn's weapon is not judged at the downing; dropped, it is judged then. With everything
                    // on the body a dropped-way weapon is judged at the downing and kept in hand, and death must not judge it again.
                    foreach (string way in new[] { "death", "downed then death", "downed then capture", "dropped: downed then death",
                        "on body: downed then death", "dropped, on body: downed then death", "arrest", "strip" })
                    {
                        ModHarness.RestoreSettings();
                        Settings.values.deathDrop = way.Contains("on body") ? DeathDrop.AllOnBody : DeathDrop.Ground;
                        Rules.weapons = new CategoryRule(Outcome.Degrade);
                        bool dropped = way.StartsWith("dropped");
                        if (dropped) Rules.downedGear = DownedGear.DropWeapon;
                        int expectedAtDowning = dropped ? 1 : 0;
                        var p = Spawn(ctx, Mercenary, Pirates, cell);
                        var weapon = Arm(p, club, QualityCategory.Normal);
                        c.Subject(p);
                        int downed = -1;
                        switch (way)
                        {
                            case "death": p.Kill(null); break;
                            case "downed then death":
                            case "dropped: downed then death":
                            case "on body: downed then death":
                            case "dropped, on body: downed then death":
                                HealthUtility.TryAnesthetize(p);
                                downed = judged.TryGetValue(weapon, out int n) ? n : 0;
                                p.Kill(null);
                                break;
                            case "downed then capture":
                                HealthUtility.TryAnesthetize(p);
                                downed = judged.TryGetValue(weapon, out int m) ? m : 0;
                                p.guest.SetGuestStatus(Faction.OfPlayer, GuestStatus.Prisoner);
                                break;
                            case "arrest": p.guest.SetGuestStatus(Faction.OfPlayer, GuestStatus.Prisoner); break;
                            case "strip": p.Strip(); break;
                        }
                        int times = judged.TryGetValue(weapon, out int t) ? t : 0;
                        c.Observe(way, times);
                        if (downed >= 0) c.Observe("atDowning", downed);
                        c.Step(times == 1 && (downed < 0 || downed == expectedAtDowning), $"{way}: the club is judged once ({times}{(downed >= 0 ? $", {downed} at the downing, expected {expectedAtDowning}" : "")})");
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
            // With colonists keeping their gear on (the default), the colonist's gear stays in his corpse: ColonistKilled.
            Settings.values.colonistsKeepGear = false;
            c.Note("Colonists keep their gear: off");
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

        // ---- the death setting does not apply at a downing (ticket 24) -------------------------------------------------------
        {
            var c = ctx.Begin(AllOnBodyDowned);
            var club = Kit.Def("MeleeWeapon_Club");
            bool ready = true;
            foreach (bool toggles in new[] { true, false })
            {
                ModHarness.RestoreSettings();
                Settings.values.deathDrop = DeathDrop.AllOnBody;
                Rules.sortWhenArrested = toggles;
                Rules.sortWhenStripped = toggles;
                Rules.downedGear = DownedGear.DropWeapon;
                string tag = toggles ? "On" : "Off";
                var cell = sites.Dequeue();
                var p = Spawn(ctx, Mercenary, Pirates, cell);
                var held = Arm(p, club, QualityCategory.Normal);
                var carried = Stack(revolver, 1);
                p.inventory.innerContainer.TryAdd(carried);
                p.inventory.innerContainer.TryAdd(Stack(silver, 50));
                c.Subject(p);
                c.Note($"Everything stays on the body at death; downed pawns drop the weapon; sorting when arrested and stripped {(toggles ? "on" : "off")}. A pirate with a club in hand and a revolver and 50 silver in his pack is downed");
                c.Step(!EraReader.ItemEra(club, Pirates).OutOfReach && EraReader.ItemEra(revolver, Pirates).OutOfReach,
                    "the club is in reach of the tribal colony and the revolver is not");
                int expectedScrap = ExpectedScrap(carried, Pirates);
                var before = AllScrap.Concat(new[] { silver, club, revolver }).ToDictionary(d => d, d => Kit.StackIds(map, d));
                int Ground(ThingDef def) => Units(NewNear(map, def, before[def], cell));
                HealthUtility.TryAnesthetize(p);
                if (!Kit.Require(ctx, AllOnBodyDowned, (p.Downed && !p.Dead && p.Spawned, "the pirate is downed and alive")))
                {
                    ready = false;
                    break;
                }
                // A few ticks so anything the game drops later than MakeDowned shows too.
                yield return Wait.Ticks(60);
                int scrapOut = AllScrap.Sum(Ground), silverOut = Ground(silver), clubOut = Ground(club), revolverOut = Ground(revolver);
                c.Expect("scrapOnGround" + tag, expectedScrap);
                c.Observe("primary" + tag, p.equipment?.Primary?.def.defName ?? "none").Observe("packCount" + tag, p.inventory.innerContainer.Count)
                    .Observe("clubOnGround" + tag, clubOut).Observe("scrapOnGround" + tag, scrapOut).Observe("revolverOnGround" + tag, revolverOut)
                    .Observe("silverOnGround" + tag, silverOut).Observe("droppedWeapon" + tag, p.mindState.droppedWeapon?.def.defName ?? "none");
                c.Step(p.equipment?.Primary == null && !held.Destroyed && held.Spawned && clubOut == 1, $"{tag}: the club drops whole on the ground");
                c.Step(carried.Destroyed && revolverOut == 0 && scrapOut == expectedScrap,
                    $"{tag}: the carried revolver becomes {scrapOut} scrap on the ground (expected {expectedScrap})");
                c.Step(silverOut == 50 && p.inventory.innerContainer.Count == 0, $"{tag}: the 50 silver lie beside him and his pack is empty ({silverOut})");
                p.Destroy();
                foreach (var def in before.Keys)
                    foreach (var t in NewNear(map, def, before[def], cell)) t.Destroy();
            }
            if (ready) c.Conclude();
        }

        // ---- review 2026-11-24: a thing kept at a downing is judged once ----------------------------------------------
        {
            var c = ctx.Begin(JudgedOnce);
            ModHarness.RestoreSettings();
            Settings.values.deathDrop = DeathDrop.AllOnBody;
            Rules.downedGear = DownedGear.DropWeapon;
            Rules.weapons.outcome = Outcome.Keep;
            Rules.other.outcome = Outcome.Delete;
            Rules.other.chance = 1f;
            var club = Kit.Def("MeleeWeapon_Club");
            var cell = sites.Dequeue();
            var p = Spawn(ctx, Mercenary, Pirates, cell);
            var held = Arm(p, club, QualityCategory.Normal);
            c.Subject(p);
            c.Note("Everything on the body, downed pawns drop the weapon, weapons Keep, other inventory Delete. A pirate holding a club is downed, then killed");
            c.Step(!EraReader.ItemEra(club, Pirates).OutOfReach, "the club is in reach of the tribal colony");
            HealthUtility.TryAnesthetize(p);
            if (Kit.Require(ctx, JudgedOnce, (p.Downed && !p.Dead, "the pirate is downed and alive")))
            {
                c.Step(p.equipment?.Primary == null && !held.Destroyed && held.Spawned, "the club is kept and dropped whole at the downing");
                p.Kill(null);
                bool inCorpse = p.Corpse?.InnerPawn?.inventory?.innerContainer.Contains(held) ?? false;
                c.Observe("clubDestroyed", held.Destroyed).Observe("clubInCorpse", inCorpse).Observe("clubOnGround", held.Spawned);
                c.Step(!held.Destroyed && held.Spawned && !inCorpse, "at death the club is not judged again: it stays whole on the ground");
                c.Conclude();
            }
            p.Corpse?.Destroy();
            if (held.Spawned) held.Destroy();
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
            Rules.downedGear = DownedGear.DropWeapon;
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

        // ---- 24: colonists keep their gear, and the toggle off ------------------------------------------------------------
        var medicineDef = ThingDefOf.MedicineIndustrial;
        bool InCorpseWhole(Pawn dead, Thing thing)
        {
            var inner = dead.Corpse?.InnerPawn;
            return inner != null && !thing.Destroyed && (inner.equipment?.Primary == thing || inner.inventory.innerContainer.Contains(thing));
        }
        {
            var c = ctx.Begin(ColonistKilled);
            var cell = sites.Dequeue();
            c.Note("Colonists keep their gear (the default). A colonist with a revolver and 3 industrial medicine is killed under each death setting");
            foreach (var drop in new[] { DeathDrop.Ground, DeathDrop.ScrapOnBody, DeathDrop.AllOnBody })
            {
                ModHarness.RestoreSettings();
                Settings.values.deathDrop = drop;
                var p = Spawn(ctx, PawnKindDefOf.Colonist, Faction.OfPlayer, cell);
                var gun = Arm(p, revolver, QualityCategory.Normal);
                p.inventory.innerContainer.TryAdd(Stack(medicineDef, 3));
                c.Subject(p);
                c.Step(Settings.values.colonistsKeepGear, $"{drop}: colonists keep their gear");
                var before = Kit.StackIds(map, industrial);
                var beforeGun = Kit.StackIds(map, revolver);
                var beforeMed = Kit.StackIds(map, medicineDef);
                p.Kill(null);
                int scrap = Units(NewNear(map, industrial, before, cell)) + InCorpse(p, industrial), guns = NewNear(map, revolver, beforeGun, cell).Count,
                    med = Units(NewNear(map, medicineDef, beforeMed, cell));
                bool gunIn = InCorpseWhole(p, gun);
                c.Observe(drop + ": revolverInCorpse", gunIn).Observe(drop + ": medicineInCorpse", InCorpse(p, medicineDef)).Observe(drop + ": scrap", scrap)
                    .Observe(drop + ": revolversOnGround", guns).Observe(drop + ": medicineOnGround", med);
                c.Step(gunIn, $"{drop}: the revolver stays whole in his corpse");
                c.Step(InCorpse(p, medicineDef) == 3, $"{drop}: his medicine stays in his corpse");
                c.Step(scrap == 0 && guns == 0 && med == 0, $"{drop}: nothing lands on the ground and nothing is scrapped");
                p.Corpse?.Destroy();
            }
            c.Conclude();
        }
        {
            var c = ctx.Begin(ColonistToggleOff);
            ModHarness.RestoreSettings();
            Settings.values.colonistsKeepGear = false;
            Rules.downedGear = DownedGear.DropWeapon;
            c.Note("Colonists keep their gear: off; downed pawns drop the weapon; loot lands on the ground at death. One colonist with a revolver is downed, another is killed");
            var cell = sites.Dequeue();
            bool ready = true;
            foreach (bool kill in new[] { false, true })
            {
                string tag = kill ? "killed" : "downed";
                var p = Spawn(ctx, PawnKindDefOf.Colonist, Faction.OfPlayer, cell);
                var gun = Arm(p, revolver, QualityCategory.Normal);
                c.Subject(p);
                var before = Kit.StackIds(map, industrial);
                if (kill) p.Kill(null);
                else
                {
                    HealthUtility.TryAnesthetize(p);
                    if (!Kit.Require(ctx, ColonistToggleOff, (p.Downed && !p.Dead, "the colonist is downed and alive"))) { ready = false; p.Destroy(); break; }
                    yield return Wait.Ticks(60);
                }
                int scrap = Units(NewNear(map, industrial, before, cell));
                c.Observe(tag + ": revolver", gun.Destroyed ? "destroyed" : gun.Spawned ? "on ground" : "elsewhere").Observe(tag + ": scrap", scrap);
                c.Step(!gun.Destroyed && gun.Spawned && scrap == 0, $"{tag}: the colonist's revolver drops whole on the ground");
                p.Corpse?.Destroy();
                if (!p.Destroyed) p.Destroy();
                if (gun.Spawned) gun.Destroy();
            }
            if (ready) c.Conclude();
        }

        // ---- 24: under DropWeapon a downed visitor's gear is sorted --------------------------------------------------------
        {
            var c = ctx.Begin(VisitorDropped);
            var visitorFaction = Find.FactionManager.AllFactionsListForReading.FirstOrDefault(f => !f.IsPlayer && !f.Hidden && !f.temporary
                && f.def.humanlikeFaction && !f.def.permanentEnemy && !f.HostileTo(Faction.OfPlayer));
            if (visitorFaction == null) c.EnvironmentUnavailable("the world has no faction friendly to the colony");
            else
            {
                ModHarness.RestoreSettings();
                Rules.nonHostileFaction = true;
                Rules.downedGear = DownedGear.DropWeapon;
                var cell = sites.Dequeue();
                var p = Spawn(ctx, visitorFaction.def.basicMemberKind ?? PawnKindDefOf.Villager, visitorFaction, cell);
                var gun = Arm(p, revolver, QualityCategory.Normal);
                c.Subject(p);
                c.Note($"Downed pawns drop the weapon; friendly factions on. A visitor of {visitorFaction.Name} with a revolver is downed");
                c.Step(EraReader.ItemEra(revolver, visitorFaction).OutOfReach, "the revolver is out of reach");
                var before = Kit.StackIds(map, industrial);
                HealthUtility.TryAnesthetize(p);
                if (Kit.Require(ctx, VisitorDropped, (p.Downed && !p.Dead, "the visitor is downed and alive")))
                {
                    yield return Wait.Ticks(60);
                    int scrap = Units(NewNear(map, industrial, before, cell));
                    c.Observe("revolver", gun.Destroyed ? "destroyed" : gun.Spawned ? "on ground" : p.equipment?.Primary == gun ? "in hand" : "elsewhere").Observe("scrap", scrap);
                    c.Step(gun.Destroyed && scrap > 0, $"the revolver becomes {scrap} industrial scrap at the downing");
                    c.Conclude();
                }
                p.Destroy();
                if (!gun.Destroyed) gun.Destroy();
            }
        }

        // ---- review 2026-11-24: protected items are never unfamiliar --------------------------------------------------
        // ---- 21/23: a downed pawn keeps his weapon in hand ------------------------------------------------------------------
        // The default (ticket 23): nothing drops or is judged at the downing; the gear is sorted when he is stripped, captured
        // or dies. Being carried and set down, rescued and captured through the game's own jobs follow this case.
        {
            var c = ctx.Begin(KeepsWeapon);
            bool ready = true;
            var cell = sites.Dequeue();
            foreach (string way in new[] { "strip", "capture", "kill", "strip with stripping off", "gets up" })
            {
                ModHarness.RestoreSettings();
                Settings.values.deathDrop = DeathDrop.Ground;
                bool stripOff = way == "strip with stripping off";
                if (stripOff) Rules.sortWhenStripped = false;
                var p = Spawn(ctx, Mercenary, Pirates, cell);
                var gun = Arm(p, revolver, QualityCategory.Normal);
                var medicine = Stack(ThingDefOf.MedicineIndustrial, 3);
                p.inventory.innerContainer.TryAdd(medicine);
                c.Subject(p);
                c.Note($"Downed pawns keep their weapon (the default); sorting when stripped {(stripOff ? "off" : "on")}. A pirate with a revolver and 3 industrial medicine in his pack is downed, then: {way}");
                if (way == "strip") c.Step(EraReader.ItemEra(revolver, Pirates).OutOfReach && Rules.downedGear == DownedGear.KeepWeapon, "the revolver is out of reach and the downed setting is its default, keep his weapon");
                var before = Kit.StackIds(map, industrial);
                var beforeGun = Kit.StackIds(map, revolver);
                int Medicine() => p.inventory.innerContainer.Where(t => t.def == ThingDefOf.MedicineIndustrial).Sum(t => t.stackCount);
                int ScrapInPack() => p.inventory.innerContainer.Where(t => t.def == industrial).Sum(t => t.stackCount);
                HealthUtility.TryAnesthetize(p);
                if (!Kit.Require(ctx, KeepsWeapon, (p.Downed && !p.Dead && p.Spawned, "the pirate is downed and alive")))
                {
                    ready = false;
                    break;
                }
                // A few ticks so anything the game drops later than MakeDowned shows too.
                yield return Wait.Ticks(60);
                int scrapAtDowning = Units(NewNear(map, industrial, before, cell)), gunsOnGround = NewNear(map, revolver, beforeGun, cell).Count;
                c.Observe(way + ": primary", p.equipment.Primary?.def.defName ?? "none").Observe(way + ": scrapAtDowning", scrapAtDowning)
                    .Observe(way + ": revolversOnGround", gunsOnGround).Observe(way + ": medicine", Medicine());
                c.Step(p.equipment.Primary == gun && !gun.Destroyed && !p.inventory.innerContainer.Contains(gun),
                    $"{way}: downed, his revolver is whole in his hands");
                c.Step(scrapAtDowning == 0 && gunsOnGround == 0 && Medicine() == 3, $"{way}: nothing lands on the ground and his medicine stays in his pack");
                int expected = ScrapAmounts.Units(gun.MarketValue, 1);
                before = Kit.StackIds(map, industrial);
                int packBefore = ScrapInPack();
                switch (way)
                {
                    case "capture": p.guest.SetGuestStatus(Faction.OfPlayer, GuestStatus.Prisoner); break;
                    case "kill": p.Kill(null); break;
                    case "gets up":
                        p.health.RemoveHediff(p.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Anesthetic));
                        yield return Wait.Ticks(60);
                        break;
                    default: p.Strip(); break;
                }
                int made = Units(NewNear(map, industrial, before, cell)) + (p.Dead ? 0 : ScrapInPack() - packBefore);
                c.Observe(way + ": revolver", gun.Destroyed ? "destroyed" : gun.Spawned ? "on ground" : p.equipment?.Primary == gun ? "in hand" : "elsewhere")
                    .Observe(way + ": scrap", made);
                if (way == "gets up")
                    c.Step(!p.Downed && !p.Dead && p.equipment.Primary == gun && !gun.Destroyed && made == 0 && Medicine() == 3,
                        $"{way}: he is up, still holding the whole revolver, with his medicine");
                else if (stripOff) c.Step(!gun.Destroyed && gun.Spawned && made == 0, $"{way}: the strip drops the revolver whole");
                else c.Step(gun.Destroyed && made == expected, $"{way}: the revolver becomes {made} industrial scrap (expected {expected})");
                p.Corpse?.Destroy();
                if (!p.Destroyed) p.Destroy();
                if (gun.Spawned) gun.Destroy();
                foreach (var t in NewNear(map, industrial, before, cell)) t.Destroy();
                foreach (var t in map.listerThings.ThingsOfDef(ThingDefOf.MedicineIndustrial).Where(t => t.Spawned && t.Position.DistanceTo(cell) <= 5f).ToList()) t.Destroy();
            }
            if (ready) c.Conclude();
        }

        // ---- 23: carried, rescued and captured through the game's own jobs ---------------------------------------------------
        // A colonist with only cleaning on is handed the player's orders: carry while drafted, rescue, capture. The game drops a
        // downed pawn's weapon as he is set down outside a bed; under "Keep his weapon" the mod skips that drop.
        if (new[] { Carried, Rescued, CapturedGround, CapturedInBed, HospitalityGuest, ColonistRescued }.Any(ctx.Selected))
        {
            IntVec3 W(int dx, int dz) => new IntVec3(ward.minX + dx, 0, ward.minZ + dz);
            Building_Bed prisonBed = null, prisonBed2 = null, medBed = null, medBed2 = null;
            Pawn worker = null;
            string wardProblem = null;
            try
            {
                Kit.Room(ctx, new CellRect(ward.minX, ward.minZ, 6, 6), W(5, 2));
                prisonBed = (Building_Bed)Kit.Place(ctx, "Bed", W(1, 2));
                prisonBed2 = (Building_Bed)Kit.Place(ctx, "Bed", W(3, 2));
                prisonBed.ForPrisoners = true;
                prisonBed2.ForPrisoners = true;
                // Setting ForPrisoners in code only marks the room's stats dirty; the room decides it is a prison cell when its
                // shape or beds change, as when the player's bed button or a newly built bed notifies it.
                prisonBed.GetRoom()?.Notify_RoomShapeChanged();
                medBed = (Building_Bed)Kit.Place(ctx, "Bed", W(9, 2));
                medBed2 = (Building_Bed)Kit.Place(ctx, "Bed", W(11, 2));
                medBed.Medical = true;
                medBed2.Medical = true;
                ctx.Setup("Developer set the two beds in the room for prisoners, told the room its beds changed, and set the two outside as medical beds");
                worker = Kit.Worker(ctx, W(14, 5), DefDatabase<WorkTypeDef>.GetNamed("Cleaning"));
            }
            catch (SetupException e) { wardProblem = e.Message; }
            yield return Wait.Ticks(1);
            var friendly = Find.FactionManager.AllFactionsListForReading.FirstOrDefault(f => !f.IsPlayer && !f.Hidden && !f.temporary
                && f.def.humanlikeFaction && !f.def.permanentEnemy && !f.HostileTo(Faction.OfPlayer));
            var downCell = W(20, 5);
            float anaesthetic = 1f;

            // Holds a pawn under anaesthetic for as long as a job takes, so he stays down whatever its length.
            Action KeepDown(Pawn pawn) => () =>
            {
                var h = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Anesthetic);
                if (h != null && h.Severity < anaesthetic) h.Severity = anaesthetic;
            };
            Pawn Downed(BatchContext context, PawnKindDef kind, Faction faction, ThingWithComps[] armed)
            {
                var pawn = Spawn(context, kind, faction, downCell);
                armed[0] = Arm(pawn, revolver, QualityCategory.Normal);
                pawn.inventory.innerContainer.TryAdd(Stack(ThingDefOf.MedicineIndustrial, 3));
                HealthUtility.TryAnesthetize(pawn);
                anaesthetic = pawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Anesthetic)?.Severity ?? 1f;
                return pawn;
            }
            int MedicineOf(Pawn pawn) => pawn.inventory.innerContainer.Where(t => t.def == ThingDefOf.MedicineIndustrial).Sum(t => t.stackCount);
            bool Ordered(JobDef def, Pawn takee, LocalTargetInfo b)
            {
                var job = b.IsValid ? JobMaker.MakeJob(def, takee, b) : JobMaker.MakeJob(def, takee);
                job.count = 1;
                return worker.jobs.TryTakeOrderedJob(job, JobTag.Misc);
            }
            // What the capture job checks of its prison bed, recorded so a stalled capture says why.
            string PrisonBedState(Building_Bed bed, Pawn pawn) =>
                $"forPrisoners={bed.ForPrisoners}, inPrisonCell={bed.Position.IsInPrisonCell(map)}, room={bed.GetRoom()?.Role?.defName ?? "none"}, "
                + $"usableAsPrisoner={RestUtility.CanUseBedNow(bed, pawn, false, false, GuestStatus.Prisoner)}, "
                + $"validAsPrisoner={RestUtility.IsValidBedFor(bed, pawn, worker, false, guestStatus: GuestStatus.Prisoner)}";
            void Retire(Pawn pawn)
            {
                if (pawn == null || pawn.Destroyed) return;
                if (pawn.Spawned) pawn.DeSpawn();
                // Decide, not Discard: a captured pawn is named in a tale and the battle log, and discarding him logs a warning.
                Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.Decide);
            }

            // -- carried and set down twice: by the drafted carrier's drop button, then by undrafting him --
            Pawn rescued = null;
            ThingWithComps rescuedGun = null;
            if (ctx.Selected(Carried))
            {
                var c = ctx.Begin(Carried);
                if (wardProblem != null) c.SetupFailed(wardProblem);
                else
                {
                    ModHarness.RestoreSettings();
                    var armed = new ThingWithComps[1];
                    var p = Downed(ctx, Mercenary, Pirates, armed);
                    var gun = armed[0];
                    c.Subject(p, worker);
                    c.Note("Downed pawns keep their weapon (the default). A pirate with a revolver and 3 industrial medicine is downed; a drafted colonist carries him and sets him down, twice");
                    c.Step(p.Downed && DownedGears.KeepsWeapon(Rules, OwnerGroup.Hostile, MapKind.Home) && EraReader.ItemEra(revolver, Pirates).OutOfReach,
                        "he is down, the setting covers him and his revolver is out of reach");
                    int scrapBefore = Kit.CountOnMap(map, industrial);
                    var gunsBefore = Kit.StackIds(map, revolver);
                    worker.drafter.Drafted = true;
                    bool finished = true;
                    foreach (var (round, dest) in new[] { ("the drop button", W(26, 8)), ("undrafting", W(26, 2)) })
                    {
                        c.Note($"The colonist is ordered to carry him to {dest}, then sets him down by {round}");
                        if (!Ordered(JobDefOf.CarryDownedPawnDrafted, p, LocalTargetInfo.Invalid)) { finished = false; break; }
                        var picked = Wait.Until(() => worker.carryTracker.CarriedThing == p, 1500, Kit.All(KeepDown(p), Kit.KeepRested(worker)));
                        yield return picked;
                        if (picked.TimedOut) { finished = false; break; }
                        c.Step(p.ParentHolder is Pawn_CarryTracker && p.equipment.Primary == gun && !gun.Spawned, $"{round}: carried, he holds his revolver");
                        worker.jobs.TryTakeOrderedJob(JobMaker.MakeJob(JobDefOf.Goto, dest), JobTag.Misc);
                        var walked = Wait.Until(() => worker.Position == dest, 1500, Kit.All(KeepDown(p), Kit.KeepRested(worker)));
                        yield return walked;
                        if (walked.TimedOut || worker.carryTracker.CarriedThing != p) { finished = false; break; }
                        if (round == "the drop button")
                        {
                            var drop = worker.carryTracker.GetGizmos().OfType<Command_Action>().FirstOrDefault(g => g.icon == TexCommand.DropCarriedPawn);
                            c.Step(drop != null, "the drafted carrier has the drop button");
                            drop?.action();
                        }
                        else worker.drafter.Drafted = false;
                        if (worker.carryTracker.CarriedThing == p)
                        {
                            c.Note("still carried after " + round + "; set down with the same call the game makes");
                            worker.carryTracker.TryDropCarriedThing(worker.Position, ThingPlaceMode.Near, out _);
                        }
                        yield return Wait.Ticks(30, KeepDown(p));
                        int scrap = Kit.CountOnMap(map, industrial) - scrapBefore, guns = Kit.NewStacks(map, revolver, gunsBefore).Count();
                        c.Observe(round + ": primary", p.equipment.Primary?.def.defName ?? "none").Observe(round + ": scrap", scrap).Observe(round + ": revolversOnGround", guns);
                        c.Step(p.Spawned && p.Downed && !p.InBed() && p.equipment.Primary == gun && !gun.Spawned && !gun.Destroyed && p.mindState.droppedWeapon == null,
                            $"{round}: set down on the ground, he still holds his revolver");
                        c.Step(scrap == 0 && guns == 0 && MedicineOf(p) == 3, $"{round}: nothing is scrapped or lands, and his medicine stays in his pack");
                    }
                    if (!finished)
                        c.WorkflowInconclusive("the colonist did not carry or set down the pirate in time",
                            (worker.Spawned && !worker.Downed, "the colonist is spawned and able"), (p.Spawned || p.ParentHolder is Pawn_CarryTracker, "the pirate is on the map"),
                            (worker.CanReach(p, PathEndMode.ClosestTouch, Danger.Deadly) || p.ParentHolder is Pawn_CarryTracker, "the colonist can reach him"));
                    else
                    {
                        // Gets up with no colonist about: a standing pirate would otherwise attack.
                        if (worker.Drafted) worker.drafter.Drafted = false;
                        worker.DeSpawn();
                        p.health.RemoveHediff(p.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Anesthetic));
                        var up = Wait.Until(() => !p.Downed, 600);
                        yield return up;
                        c.Observe("gets up: primary", p.equipment.Primary?.def.defName ?? "none");
                        c.Step(!up.TimedOut && !p.Downed && p.equipment.Primary == gun && !gun.Destroyed, "he gets up still holding his revolver");
                        c.Conclude();
                        p.Destroy();
                        GenSpawn.Spawn(worker, W(14, 5), map);
                    }
                    if (!p.Destroyed) p.Destroy();
                    if (!gun.Destroyed) gun.Destroy();
                    if (worker.Drafted) worker.drafter.Drafted = false;
                    worker.jobs.StopAll();
                }
            }

            // -- a friendly visitor rescued into a medical bed --
            if (ctx.Selected(Rescued) || ctx.Selected(CapturedInBed))
            {
                var c = ctx.Begin(Rescued);
                if (wardProblem != null) c.SetupFailed(wardProblem);
                else if (friendly == null) c.EnvironmentUnavailable("the world has no faction friendly to the colony");
                else
                {
                    ModHarness.RestoreSettings();
                    Rules.nonHostileFaction = true;
                    var armed = new ThingWithComps[1];
                    var p = Downed(ctx, friendly.def.basicMemberKind ?? PawnKindDefOf.Villager, friendly, armed);
                    var gun = armed[0];
                    c.Subject(p, worker);
                    c.Note($"Downed pawns keep their weapon; friendly factions on. A visitor of {friendly.Name} with a revolver and 3 industrial medicine is downed; a colonist rescues him into a medical bed");
                    c.Step(p.Downed && p.equipment.Primary == gun && DownedGears.KeepsWeapon(Rules, OwnerGroup.NonHostileFaction, MapKind.Home) && EraReader.ItemEra(revolver, friendly).OutOfReach,
                        "he is down holding the revolver, the setting covers him and the revolver is out of reach");
                    int scrapBefore = Kit.CountOnMap(map, industrial);
                    var gunsBefore = Kit.StackIds(map, revolver);
                    if (Kit.Require(ctx, Rescued, (RestUtility.IsValidBedFor(medBed, p, worker, false), "the medical bed is valid for him")))
                    {
                        bool taken = Ordered(JobDefOf.Rescue, p, medBed);
                        var inBed = Wait.Until(() => p.CurrentBed() == medBed, 2500, Kit.All(KeepDown(p), Kit.KeepRested(worker)));
                        yield return inBed;
                        if (inBed.TimedOut)
                            c.WorkflowInconclusive("the colonist did not rescue the visitor in time", (taken, "the colonist took the rescue order"),
                                (worker.CanReach(p, PathEndMode.ClosestTouch, Danger.Deadly), "the colonist can reach him"));
                        else
                        {
                            yield return Wait.Ticks(30, KeepDown(p));
                            int scrap = Kit.CountOnMap(map, industrial) - scrapBefore, guns = Kit.NewStacks(map, revolver, gunsBefore).Count();
                            c.Observe("primary", p.equipment.Primary?.def.defName ?? "none").Observe("scrap", scrap).Observe("revolversOnGround", guns)
                                .Observe("guestStatus", p.GuestStatus?.ToString() ?? "none");
                            c.Step(p.CurrentBed() == medBed && p.equipment.Primary == gun && !gun.Spawned && !gun.Destroyed && p.mindState.droppedWeapon == null,
                                "in the medical bed, he still holds his revolver");
                            c.Step(scrap == 0 && guns == 0 && MedicineOf(p) == 3, "nothing is scrapped or lands, and his medicine stays in his pack");
                            c.Conclude();
                            rescued = p;
                            rescuedGun = gun;
                        }
                    }
                    if (rescued == null) { if (!p.Destroyed) p.Destroy(); if (!gun.Destroyed) gun.Destroy(); }
                    worker.jobs.StopAll();
                }
            }

            // -- Hospitality: friendly gear drops are off by default there too; runs only with --mods compat-hospitality --
            if (ctx.Selected(HospitalityGuest))
            {
                var c = ctx.Begin(HospitalityGuest);
                var settingsType = AccessTools.TypeByName("Hospitality.ModSettings_Hospitality");
                if (!ModsConfig.IsActive("Orion.Hospitality") || settingsType == null)
                    c.EnvironmentUnavailable("Hospitality is not loaded; run with --mods compat-hospitality");
                else if (wardProblem != null) c.SetupFailed(wardProblem);
                else if (friendly == null) c.EnvironmentUnavailable("the world has no faction friendly to the colony");
                else
                {
                    long errors = CaseErrors.Mark;
                    var drops = AccessTools.Field(settingsType, "disableFriendlyGearDrops")?.GetValue(null);
                    c.Observe("hospitality.disableFriendlyGearDrops", drops ?? "missing");
                    c.Note("Hospitality is loaded with its friendly gear drops setting " + (drops ?? "missing"));
                    foreach (var keep in new[] { true, false })
                    {
                        ModHarness.RestoreSettings();
                        Rules.nonHostileFaction = true;
                        Rules.downedGear = keep ? DownedGear.KeepWeapon : DownedGear.DropWeapon;
                        string label = keep ? "keep his weapon" : "drop the weapon";
                        var armed = new ThingWithComps[1];
                        int scrapBefore = Kit.CountOnMap(map, industrial);
                        var gunsBefore = Kit.StackIds(map, revolver);
                        var p = Downed(ctx, friendly.def.basicMemberKind ?? PawnKindDefOf.Villager, friendly, armed);
                        var gun = armed[0];
                        c.Subject(p);
                        yield return Wait.Ticks(30, KeepDown(p));
                        string Where() => gun.Destroyed ? "destroyed" : gun.Spawned ? "on ground" : p.equipment.Primary == gun ? "in hand" : "elsewhere";
                        c.Observe(label + ": revolver at downing", Where()).Observe(label + ": scrap at downing", Kit.CountOnMap(map, industrial) - scrapBefore);
                        if (keep)
                        {
                            c.Step(p.Downed && p.equipment.Primary == gun && !gun.Spawned, $"{label}: downed, the visitor holds his revolver");
                            bool taken = Kit.Require(ctx, HospitalityGuest, (RestUtility.IsValidBedFor(medBed2, p, worker, false), "the second medical bed is valid for him"))
                                && Ordered(JobDefOf.Rescue, p, medBed2);
                            if (!taken) { if (!p.Destroyed) p.Destroy(); break; }
                            var inBed = Wait.Until(() => p.CurrentBed() == medBed2, 2500, Kit.All(KeepDown(p), Kit.KeepRested(worker)));
                            yield return inBed;
                            c.Observe(label + ": revolver after rescue", Where());
                            c.Step(!inBed.TimedOut && p.equipment.Primary == gun && !gun.Spawned && Kit.CountOnMap(map, industrial) == scrapBefore
                                && !Kit.NewStacks(map, revolver, gunsBefore).Any(), $"{label}: rescued into a bed, he still holds it and nothing is scrapped or dropped");
                            worker.jobs.StopAll();
                        }
                        // Ticket 24: under DropWeapon the mod drops a covered pawn's gear itself when Hospitality skips the drop.
                        else c.Step(gun.Destroyed && Kit.CountOnMap(map, industrial) > scrapBefore, $"{label}: at the downing the out-of-reach revolver drops as scrap");
                        if (!p.Destroyed) p.Destroy();
                        if (!gun.Destroyed) gun.Destroy();
                        foreach (var t in Kit.NewStacks(map, industrial, new HashSet<int>()).Where(t => t.Position.DistanceTo(downCell) <= 5f).ToList()) t.Destroy();
                    }
                    c.Observe("errorsLogged", CaseErrors.Mark - errors);
                    c.Step(CaseErrors.Mark == errors, "no error is logged");
                    if (ctx.ResultOf(HospitalityGuest) == Results.Running || ctx.ResultOf(HospitalityGuest) == null) c.Conclude();
                }
            }

            // -- 24: a downed colonist keeps his gear, through a rescue and being set down again --
            if (ctx.Selected(ColonistRescued))
            {
                var c = ctx.Begin(ColonistRescued);
                if (wardProblem != null) c.SetupFailed(wardProblem);
                else
                {
                    ModHarness.RestoreSettings();
                    Rules.downedGear = DownedGear.DropWeapon;
                    var armed = new ThingWithComps[1];
                    int scrapBefore = Kit.CountOnMap(map, industrial);
                    var gunsBefore = Kit.StackIds(map, revolver);
                    var p = Downed(ctx, PawnKindDefOf.Colonist, Faction.OfPlayer, armed);
                    var gun = armed[0];
                    c.Subject(p, worker);
                    c.Note("Colonists keep their gear (the default); downed pawns drop the weapon. A colonist with a revolver and 3 industrial medicine is downed, "
                        + "rescued into a medical bed by another colonist, then despawned and spawned again outside the bed (as a carry or a load sets him down)");
                    yield return Wait.Ticks(30, KeepDown(p));
                    int Scrap() => Kit.CountOnMap(map, industrial) - scrapBefore;
                    int Guns() => Kit.NewStacks(map, revolver, gunsBefore).Count();
                    bool Holds() => p.equipment.Primary == gun && !gun.Spawned && !gun.Destroyed;
                    c.Observe("downing: primary", p.equipment.Primary?.def.defName ?? "none").Observe("downing: scrap", Scrap()).Observe("downing: revolversOnGround", Guns());
                    c.Step(Settings.values.colonistsKeepGear && p.Downed && p.IsColonist, "he is a downed colonist and colonists keep their gear");
                    c.Step(Holds() && Scrap() == 0 && Guns() == 0 && MedicineOf(p) == 3, "downed, he holds his revolver, nothing lands and his medicine stays in his pack");
                    if (Kit.Require(ctx, ColonistRescued, (RestUtility.IsValidBedFor(medBed2, p, worker, false), "the second medical bed is valid for him")))
                    {
                        bool taken = Ordered(JobDefOf.Rescue, p, medBed2);
                        var inBed = Wait.Until(() => p.CurrentBed() == medBed2, 2500, Kit.All(KeepDown(p), Kit.KeepRested(worker)));
                        yield return inBed;
                        if (inBed.TimedOut)
                            c.WorkflowInconclusive("the colonist did not rescue him in time", (taken, "the colonist took the rescue order"),
                                (worker.CanReach(p, PathEndMode.ClosestTouch, Danger.Deadly), "the colonist can reach him"));
                        else
                        {
                            yield return Wait.Ticks(30, KeepDown(p));
                            c.Observe("rescue: primary", p.equipment.Primary?.def.defName ?? "none").Observe("rescue: scrap", Scrap()).Observe("rescue: revolversOnGround", Guns());
                            c.Step(p.CurrentBed() == medBed2 && Holds() && Scrap() == 0 && Guns() == 0 && MedicineOf(p) == 3,
                                "in the medical bed, he still holds his revolver and nothing is scrapped or lands");
                            worker.jobs.StopAll();
                            p.DeSpawn();
                            GenSpawn.Spawn(p, downCell, map);
                            yield return Wait.Ticks(30, KeepDown(p));
                            c.Observe("respawn: primary", p.equipment.Primary?.def.defName ?? "none").Observe("respawn: scrap", Scrap()).Observe("respawn: revolversOnGround", Guns())
                                .Observe("respawn: droppedWeapon", p.mindState.droppedWeapon?.def.defName ?? "none");
                            c.Step(p.Spawned && p.Downed && !p.InBed() && Holds() && Scrap() == 0 && Guns() == 0 && MedicineOf(p) == 3,
                                "set down again outside a bed, he still holds his revolver and nothing is scrapped or lands");
                            var anesthetic = p.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Anesthetic);
                            if (anesthetic != null) p.health.RemoveHediff(anesthetic);
                            var up = Wait.Until(() => !p.Downed, 600);
                            yield return up;
                            c.Observe("gets up: primary", p.equipment.Primary?.def.defName ?? "none");
                            c.Step(!up.TimedOut && !p.Downed && Holds() && Scrap() == 0 && Guns() == 0, "he gets up still holding his revolver");
                            c.Conclude();
                        }
                    }
                    if (!p.Destroyed) p.Destroy();
                    if (!gun.Destroyed) gun.Destroy();
                    worker.jobs.StopAll();
                }
            }

            // -- the rescued visitor captured from his medical bed --
            if (ctx.Selected(CapturedInBed) && !ctx.SkipUnlessPassed(CapturedInBed, Rescued))
            {
                var c = ctx.Begin(CapturedInBed);
                var p = rescued;
                var gun = rescuedGun;
                ModHarness.RestoreSettings();
                Rules.nonHostileFaction = true;
                c.Subject(p, worker);
                c.Note("The rescued visitor, still holding his revolver in the medical bed, is captured into a prison bed");
                int expected = ScrapAmounts.Units(gun.MarketValue, 1);
                int scrapBefore = Kit.CountOnMap(map, industrial);
                var gunsBefore = Kit.StackIds(map, revolver);
                c.Observe("prisonBed", PrisonBedState(prisonBed, p));
                if (Kit.Require(ctx, CapturedInBed, (p.Downed && p.CurrentBed() == medBed && p.equipment.Primary == gun, "he is down in the medical bed holding the revolver"),
                    (RestUtility.IsValidBedFor(prisonBed, p, worker, false, guestStatus: GuestStatus.Prisoner), "the prison bed is valid for him as a prisoner")))
                {
                    bool taken = Ordered(JobDefOf.Capture, p, prisonBed);
                    var inBed = Wait.Until(() => p.CurrentBed() == prisonBed, 2500, Kit.All(KeepDown(p), Kit.KeepRested(worker)));
                    yield return inBed;
                    if (inBed.TimedOut)
                        c.WorkflowInconclusive("the colonist did not capture the visitor in time", (taken, "the colonist took the capture order"),
                            (worker.CanReach(prisonBed, PathEndMode.OnCell, Danger.Deadly), "the colonist can reach the prison bed"));
                    else
                    {
                        yield return Wait.Ticks(30, KeepDown(p));
                        int made = Kit.CountOnMap(map, industrial) - scrapBefore;
                        c.Observe("revolver", gun.Destroyed ? "destroyed" : gun.Spawned ? "on ground" : "in hand").Observe("scrap", made).Expect("scrap", expected);
                        c.Step(p.IsPrisonerOfColony, "he is the colony's prisoner");
                        c.Step(gun.Destroyed && made == expected && !Kit.NewStacks(map, revolver, gunsBefore).Any(), $"the arrest turns the revolver into {made} industrial scrap (expected {expected})");
                        c.Conclude();
                    }
                }
                Retire(p);
                if (!gun.Destroyed) gun.Destroy();
                worker.jobs.StopAll();
            }
            else Retire(rescued);

            // -- a downed pirate captured from the ground --
            if (ctx.Selected(CapturedGround))
            {
                var c = ctx.Begin(CapturedGround);
                if (wardProblem != null) c.SetupFailed(wardProblem);
                else
                {
                    ModHarness.RestoreSettings();
                    var armed = new ThingWithComps[1];
                    var p = Downed(ctx, Mercenary, Pirates, armed);
                    var gun = armed[0];
                    c.Subject(p, worker);
                    c.Note("Downed pawns keep their weapon. A downed pirate holding a revolver is captured into a prison bed");
                    int expected = ScrapAmounts.Units(gun.MarketValue, 1);
                    int scrapBefore = Kit.CountOnMap(map, industrial);
                    var gunsBefore = Kit.StackIds(map, revolver);
                    c.Step(p.Downed && p.equipment.Primary == gun, "he is down holding the revolver");
                    c.Observe("prisonBed", PrisonBedState(prisonBed2, p));
                    bool valid = Kit.Require(ctx, CapturedGround, (RestUtility.IsValidBedFor(prisonBed2, p, worker, false, guestStatus: GuestStatus.Prisoner), "the prison bed is valid for him as a prisoner"));
                    bool taken = valid && Ordered(JobDefOf.Capture, p, prisonBed2);
                    var inBed = Wait.Until(() => !valid || p.CurrentBed() == prisonBed2, 2500, Kit.All(KeepDown(p), Kit.KeepRested(worker)));
                    yield return inBed;
                    if (!valid) { }
                    else if (inBed.TimedOut)
                        c.WorkflowInconclusive("the colonist did not capture the pirate in time", (taken, "the colonist took the capture order"),
                            (worker.CanReach(p, PathEndMode.ClosestTouch, Danger.Deadly), "the colonist can reach him"));
                    else
                    {
                        yield return Wait.Ticks(30, KeepDown(p));
                        int made = Kit.CountOnMap(map, industrial) - scrapBefore;
                        c.Observe("revolver", gun.Destroyed ? "destroyed" : gun.Spawned ? "on ground" : "in hand").Observe("scrap", made).Expect("scrap", expected);
                        c.Step(p.IsPrisonerOfColony, "he is the colony's prisoner");
                        c.Step(gun.Destroyed && made == expected && !Kit.NewStacks(map, revolver, gunsBefore).Any(), $"the arrest turns the revolver into {made} industrial scrap (expected {expected})");
                        c.Conclude();
                    }
                    Retire(p);
                    if (!gun.Destroyed) gun.Destroy();
                }
            }
            if (worker != null) Retire(worker);
            foreach (var t in map.listerThings.ThingsOfDef(industrial).Where(t => t.Spawned && ward.Contains(t.Position)).ToList()) t.Destroy();
            foreach (var t in map.listerThings.ThingsOfDef(ThingDefOf.MedicineIndustrial).Where(t => t.Spawned && ward.Contains(t.Position)).ToList()) t.Destroy();
        }

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
            var packGun = Make(revolver, QualityCategory.Normal);
            p.inventory.innerContainer.TryAdd(packGun);
            p.inventory.innerContainer.TryAdd(Stack(ThingDefOf.MedicineIndustrial, 5));
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
            var medicineIn = p.inventory.innerContainer.Where(t => t.def == ThingDefOf.MedicineIndustrial).ToList();
            c.Observe("packRevolver", packGun.Destroyed ? "destroyed" : p.inventory.innerContainer.Contains(packGun) ? "in pack" : "elsewhere")
                .Observe("medicineInPack", medicineIn.Sum(t => t.stackCount));
            c.Step(!packGun.Destroyed && p.inventory.innerContainer.Contains(packGun) && Unfamiliar.IsUnfamiliar(packGun), "the revolver in his pack stays there and is unfamiliar");
            c.Step(medicineIn.Sum(t => t.stackCount) == 5 && medicineIn.All(t => t.TryGetComp<CompUnfamiliar>() == null || t.TryGetComp<CompUnfamiliar>().joinedEra == Era.Undefined),
                "his 5 industrial medicine stay in his pack, unmarked");
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
                // The join settings decide on their own.
                Rules.sortWhenArrested = false;
                Rules.sortWhenStripped = false;
                Rules.prisonerJoinGear = JoinerGear.FollowDropMode;
                Rules.otherJoinGear = JoinerGear.FollowDropMode;
                var p = Spawn(ctx, Mercenary, way == "wanderer" ? null : Pirates, sharedCell);
                var worn = Dress(p, vest, QualityCategory.Normal);
                if (way != "wanderer")
                    p.guest.SetGuestStatus(Faction.OfPlayer, GuestStatus.Prisoner); // the arrest drops nothing: he holds nothing yet
                var gun = Arm(p, revolver, QualityCategory.Normal);
                var packGun = Make(revolver, QualityCategory.Normal);
                p.inventory.innerContainer.TryAdd(packGun);
                p.inventory.innerContainer.TryAdd(Stack(ThingDefOf.MedicineIndustrial, 5));
                int expectedVest = ScrapAmounts.Units(worn.MarketValue, 1), expectedGun = ScrapAmounts.Units(gun.MarketValue, 1) + ScrapAmounts.Units(packGun.MarketValue, 1);
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
                int medicine = p.inventory.innerContainer.Where(t => t.def == ThingDefOf.MedicineIndustrial).Sum(t => t.stackCount);
                c.Observe(way + "ScrapInPack", inPack).Observe(way + "Faction", p.Faction?.Name ?? "none").Observe(way + "Medicine", medicine);
                c.Step(p.Faction == Faction.OfPlayer, $"{way}: he joins the colony");
                c.Step(worn.Destroyed && gun.Destroyed && packGun.Destroyed && inPack == expectedVest + expectedGun,
                    $"{way}: vest, revolver and the revolver in his pack become {inPack} industrial scrap in his pack, rolled once (expected {expectedVest + expectedGun})");
                c.Step(medicine == 5, $"{way}: his 5 industrial medicine stay in his pack ({medicine})");
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
