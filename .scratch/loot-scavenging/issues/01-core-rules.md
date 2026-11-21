# 01: Core rules: death, downed and strip hooks

**What to build:** When a non-colony pawn dies, is downed or is stripped, its gear is sorted just before the game drops it. Owner groups (hostile, non-hostile faction, no faction, prisoners), the home-map-only check and the moment setting decide whether the rules run at all. Weapons and apparel each get a category rule with keep or delete. Each decision writes one dev-mode log line. Settings exist as code defaults with no settings page yet. See the spec's "Loot rules" and "Code structure" sections.

**Blocked by:** None (can start immediately)

**Status:** done

**Recommended model:** `opus` — Every later ticket builds on the hook choice, the call order found in the game code, and the evaluator and applier split.

- [x] The signatures and call order of the death, downed and strip methods are confirmed against the 1.7 game code before any hook is written, and the result is recorded in the spec's Unverified list.
- [ ] Killing a spawned raider removes its weapon and keeps its apparel, with the default test rules.
- [x] Killing a wild animal changes nothing while the "no faction" group is off.
- [x] Colony pawns are never affected.
- [ ] Rules run on downed and on strip only when the moment setting allows it.
- [ ] The home-map-only setting leaves gear alone on caravan, quest and outpost maps.
- [ ] A pawn whose gear cannot be stripped (for example a weapon-carrying mechanoid) is still sorted at death.
- [x] Both the 1.7 and 1.6 builds compile.

## Comments

### 2026-11-21: implemented on ls/01

**Built:**
- `Source/Model/` (plain C#, unit-tested):
  - `OwnerGroups.Classify`: the colony comes first, then prisoners, then no faction, then hostile or non-hostile.
  - `DropCandidate`: a plain description of one item and pawn (owner, moment, map kind, category, quality, hit-point fraction). Later tickets add fields.
  - `LootRules`: the code defaults.
  - `LootEvaluator.Evaluate`: owner, place, moment, then category. It returns an outcome and a reason, and each step is its own method so later tickets can insert theirs in spec order.
- `Source/Loot/`:
  - `GearHooks`: prefixes on `Pawn.Kill`, `Pawn_HealthTracker.MakeDowned` and `Pawn.Strip`, with the lowest priority, `__runOriginal` guards and a try/catch so a failure never stops the game method.
  - `GearSorter`: builds the facts, logs one dev-mode line per item and calls the applier.
  - `OutcomeApplier`: Keep and Delete. Other outcomes warn and keep.
  - `LootDebugActions`: dev-mode toggles.
- `LootScavengingSettings.rules`: saves the fields with Scribe.

**Decisions the spec left open:**
- **Code defaults (the "default test rules"):** the hostile group is on and the other groups are off. Rules apply at death only, and home map only is off. Weapons are deleted and apparel is kept. Ticket 11 replaces these with the Progression preset.
- **Each hook sorts only what its moment drops,** so no item is judged twice:
  - **Death:** equipment, apparel and inventory. Apparel stays on the corpse, so death is its last sorting.
  - **Downed:** equipment and inventory, only for a spawned pawn not in bed, as the game does.
  - **Strip:** living pawns outside a caravan only. A corpse's gear was already sorted at death, so corpse strips, including the strip at butchering, are left alone. Ticket 10 handles butchering. A living pawn's locked apparel is skipped because the game does not drop it.
- **Category comes from where the item is held:** equipment counts as Weapon and worn apparel as Apparel. Inventory is `ItemCategory.Other`, which has no rule yet and is kept. Ticket 09 splits it into the inventory groups.
- **No map at all** (a world pawn): skipped before any logging. The evaluator also returns `NoMap` for such items.
- **Owner hostility** uses the faction-level `Faction.HostileTo(Faction.OfPlayer)`, because `Thing.HostileTo` is false for destroyed pawns.

**Tests:** `dotnet test`, 21 passed. The 1.7 and 1.6 builds pass with no warnings, and the translation check passes.

**In-game checks pending** (dev mode, Debug actions > "Loot Scavenging"):
- **Raider, defaults:** kill a spawned raider. The weapon is gone and the apparel stays on the corpse.
- **Wild animal:** kill one. It has no gear, so it logs nothing and nothing changes; that is the pass. For a visible check, kill an armed wild man: with the defaults the log shows `OwnerGroupOff`, and after "Toggle no-faction group" it shows `CategoryRule` lines and the weapon is deleted.
- **Colonist:** kill one. The log shows `ColonyPawn`.
- **Downed raider:** with the defaults the log shows `MomentOff`. With "Toggle downed and strip" on, the weapon is deleted (`CategoryRule`).
- **Stripped prisoner:** the owner check runs first.
  1. With the defaults the log shows `OwnerGroupOff`.
  2. After "Toggle prisoner group" it shows `MomentOff`.
  3. With "Toggle downed and strip" also on, the apparel lines show `Keep (CategoryRule)`.
- **Home map only:** turn on "Toggle home map only", then kill a raider on a quest or caravan map. The log shows `NotHomeMap`.
- **Mechanoid:** kill a weapon-carrying mechanoid. The log shows a death decision for its weapon. Vanilla mech weapons are also destroyed when dropped, so the visible result is the same either way.
- **Log rules:** prints the current values.

### 2026-11-21: closed

Merged on `loot-scavenging`. 270 unit tests, both builds and the translation check pass. Smoke run `20261121-094209-lootscavenging-smoke-s1-cd8ccd5d` (RimWorld 1.7, Harmony + Core + Loot Scavenging, 0 mod errors) observed: colonist death dropped its revolver whole; a wild hare's death changed nothing. Still unobserved in game: raider weapon removed with apparel kept under the default test rules, downed and strip moments, home-map-only on non-home maps, mechanoid weapon at death.
