# 25: Settings redesign: one rule system, "Don't drop your stuff" and a Who list

Status: ready-for-agent

**Recommended model:** `opus`. It removes the drop mode and migrates saved settings, Def patches, pack presets and locks; a mistake silently changes a player's choice or scraps gear the player meant to keep.

**Blocked by:** None. Replaces the settings added by tickets 21, 23 and 24 (on main, unreleased). Ships in 0.6.0.

**Source:** Kyle, 2026-11-25, in conversation. The "Where items land" rows in the 0.6 handoff note are superseded by this ticket.

## Why

Two systems decide the same item: the drop mode (scrap or mark out-of-reach gear) and the per-category item rules. The page needs notes saying which wins, several settings duplicate each other (scrap out-of-reach vs a Scrap rule with "only beyond reach"), and some override others ("Drop the weapon" sorts whatever the rest says). The redesign leaves one system, the item rules, applied to the groups the Who list names.

## Decisions (Kyle's)

### 1. Page layout, top to bottom

1. **Presets:** Progression, Cleanup, Vanilla stay (decision 10).
2. **Don't drop your stuff:** three checkboxes (decision 2).
3. **Who:** one row per pawn group (decision 3).
4. **Item rules:** one row per category (decision 4).
5. **Scrap:** research points per era, "Study pays into techprint projects", scrap market value per era, study and recycle [one or the other | both].
6. **Corpses:** humanlike, animal, mechanoid, each a delete checkbox (decision 6).
7. **Lists:** as now: the four protect toggles, the always-keep and always-remove lists.
8. **Other:**
   - Instant disassembly yields [materials of the colony's era | materials of the item] (decision 7).
   - Unfamiliar status for gear outside the era [looted gear only | all gear] (`unfamiliarScope`, as now).
   - Apply on the home map only (`homeMapOnly`, as now).

Every slider becomes a numeric text box (`Widgets.TextFieldNumeric` or the `Listing_Standard` equivalent), clamped to today's ranges. Percentages are typed as 0 to 100.

### 2. Don't drop your stuff

Three checkboxes, all on by default:

- **Colonists keep their weapons when downed or killed.** Today's `colonistsKeepGear`: a downed colonist or colony slave keeps his weapon in hand, through a carry, a rescue or a load. A dead one keeps his weapon and pack in the corpse. Colony gear is never sorted, so colonists have no Who row.
- **Hostiles don't drop their gear, weapons and loot on the floor when downed or killed.** On: a downed hostile keeps his weapon in hand (today's `downedGear` KeepWeapon), and a dead one keeps everything on the body (today's `deathDrop` AllOnBody). Off: he drops as in the base game, and what drops is sorted as it drops.
- **Non-hostiles don't drop their gear, weapons and loot on the floor when downed or killed.** The same for non-hostile faction pawns and wild people.

These checkboxes choose where items land. Whether they are sorted is the Who row's job. Mechanoids and other pawns that cannot be stripped still drop on the ground.

### 3. Who

One row per group, each a choice of **[Apply item rules | Exclude | Mark unfamiliar]**:

| Row | Covers | Default |
|---|---|---|
| Hostile pawns | today's `hostile` | Apply |
| Non-hostile pawns | today's `nonHostileFaction` | Apply |
| Wild people and animals | today's `noFaction`; also gates animal corpse removal, as now | Apply |
| Prisoners | gear that comes off a prisoner: stripping him, or arresting a standing pawn | Apply |
| Recruited or enslaved prisoners | today's `prisonerJoinGear` | Apply |
| Other joiners | wanderers, refugees, quest joiners; today's `otherJoinGear` | Mark |

- **Mark unfamiliar** keeps the group's gear whole and marks out-of-era gear unfamiliar (today's KeepOriginal and the joiner Mark). It replaces the item rules for that group; it does not add to them.
- A raider's gear is judged under the Hostile row when he is downed or killed. The Prisoner row covers what comes off him later.
- "Sort when arrested" and "Sort when stripped" are gone. Gear that comes off a group set to Apply is always sorted.

### 4. Item rules

Each category (weapons, apparel, ammunition, drugs, medicine, food, other inventory) chooses **[Scrap | Disassemble | Delete | Vanilla]**. Vanilla is today's Keep.

- Each keeps today's conditions: only beyond the colony's reach, below a quality, below a hit-point fraction.
- The chance becomes **"Chance to <outcome>"**, a 0 to 100 text box. It is hidden under Vanilla.
- Defaults: weapons and apparel **Scrap with "Only beyond the colony's reach" ticked**. Everything else is Vanilla. This is today's Progression behaviour for weapons and apparel. Out-of-reach drugs, food and other items are no longer scrapped by default.
- Gear with no era falls back to the carrier faction's era (decision 8). "Break it into components" is gone, so `ComponentsFallback` leaves `LootEvaluator`. The cheap-gear rule in `DisassembleApplier` goes with it.

### 5. Removed outcome

**Degrade** is cut, together with its range and "lowers quality".

### 6. Corpses

- The corpse removal chance is cut: a ticked type is always removed.
- **"Butchering removes worn apparel" is cut.** Butchering a humanlike corpse always strips it first. What comes off is judged by the item rules as if the corpse had been stripped by hand, under the corpse owner's Who row. A row set to Exclude, or a colonist, gets the game's own butchering.

### 7. Disassembly yield

- **"Materials of the colony's era"** means the colony's era, as Kyle wrote it. Today's era-typical yield rolls the item's own tier pool (`ScrapTiers.TierFor(itemEra, colonyEra)` in `DisassembleApplier`). Change it to roll the colony era's pool. This is a behaviour change, so it goes in the changelog.
- "Materials of the item" keeps today's cost-list yield. Disassembly efficiency shows only under it, as a text box.

### 8. Fixed, no longer settings

| Setting | Fixed at |
|---|---|
| Gear with no era (`eraFallback`) | carrier faction |
| Which scrap can be studied (`studyEras`) | colony's era and below |
| Eject loaded charges (`rules.ejectCharges`) | always eject |
| Research points multiplier (`studyPointsMultiplier`) | cut; see migration |
| Drop mode includes medicine / ammunition | cut; each category's own rule decides |

### 9. Scrap market value per era

`scrapMarketValue` becomes one value per scrap era. Each defaults to today's single default, and `ScrapApplier` reads the scrapped item's tier.

### 10. Presets

- **Progression** = the new defaults.
- **Cleanup:** Hostile row Apply, the other rows Exclude. Today's delete rules: weapons below Normal or 50% hit points, apparel below Good or 50% hit points, all ammunition. Weapons and apparel have no beyond-reach scrap, and humanlike corpses are removed.
- **Vanilla:** every row Exclude, every rule Vanilla, all three "Don't drop your stuff" checkboxes off, no corpses removed.

### 11. Migration (settings files, `Defs/SettingsDefaults.xml` patches, `packs/progression`, both Auto Mod Config files)

Read the old keys once and stop writing them, using the ticket 19 and ticket 24 pattern (`SettingsLoad`). A lock on an old key locks every new key it maps to.

| Old | New |
|---|---|
| `rules.dropMode` ScrapOutOfReach | a category whose rule is Keep becomes Scrap with beyond reach. Medicine and ammunition follow their include toggles |
| `rules.dropMode` ScrapEverything | the same, without beyond reach |
| `rules.dropMode` KeepOriginal | every Who row that was on becomes Mark |
| `rules.dropMode` Off | rules as saved |
| a category with its own non-Keep rule under a scrap drop mode | keeps its own rule. Its out-of-reach scrapping is lost; say so in the changelog |
| `dropIncludesMedicine` / `dropIncludesAmmunition` true | that category becomes Scrap with beyond reach, if its rule was Keep |
| Degrade rules | Vanilla |
| `hostile` / `nonHostileFaction` / `noFaction` / `prisoners` | on → Apply, off → Exclude (before the KeepOriginal step) |
| `prisonerJoinGear` / `otherJoinGear` | FollowDropMode → Apply, MarkUnfamiliar → Mark, LeaveAlone → Exclude |
| `deathDrop` | see decision 13 |
| `studyPointsMultiplier` ≠ 1 | multiply it into each era's points once, then drop it |
| `scrapMarketValue` | copied to every era |
| `corpses.chance`, `butcherRemovesApparel`, `eraFallback`, `studyEras`, `ejectCharges`, `degrade*` | dropped |
| `rules.downedGear`, `colonistsKeepGear`, `sortWhenArrested`, `sortWhenStripped` | never shipped: drop them without migrating. `colonistsKeepGear` maps to the colonist checkbox where present |
| `rules.onDownedAndStrip` (0.5.1 and earlier) | dropped |

### 12. Unchanged

The protect toggles, the keep and remove lists (the remove list still runs before the category rule), the home-map toggle, the unfamiliar scope, recycle and study bills, and scrap sizing.

### 13. 0.5.2 saves every value, including defaults

Since 0.5.2, every value is written to the settings file. A saved `deathDrop` Ground (the 0.5.2 default) can't be told apart from a player who chose Ground. With the mapping agreed in conversation (Ground → unticked, now replaced), almost no existing player gets the new "stays on the body" default. `dropMode` ScrapOutOfReach has the same problem: mapped literally, drugs, food and other items keep scrapping for every existing player, against the new default.

**Decision (Kyle, 2026-11-25):** a saved value that equals its 0.5.2 default migrates to the 0.6.0 default. Only a non-default value is translated by the table in decision 11: ScrapOnBody → ticked, ScrapEverything → every category Scrap. The changelog says so. A player who deliberately chose a 0.5.2 default is reset; that can't be detected and is accepted.

## Build notes

- `LootEvaluator`: drop the drop-mode step and `CheckMoment`. The order becomes owner row → place → protection → remove list → category rule. A Mark row short-circuits to "keep whole and mark if out of reach", which is today's KeepOriginal branch.
- `BodyLoot` and the downing and death hooks read the three checkboxes instead of `downedGear`, `deathDrop` and `colonistsKeepGear`. The checkbox follows the pawn's group: hostile, or non-hostile (non-hostile faction and no faction).
- `JoinerGears` reads the two joiner rows.
- Keep `Source/Model/` plain C#. Update `SettingLocks`, `SettingNames` and `SettingKeys` for the new keys.
- English Keyed text changes a lot: remove unused keys, then run `rimworld-translate` `status`/`export`/`import` for ChineseSimplified and Russian. Status must show 0/0.

## Tests

- Unit tests: the evaluator order, Mark rows, each migration row in decision 11 and the decision 13 rule, lock mapping, preset values, and the per-era market value and points.
- DevTests (batches moments, outcomes, settings, saves, smoke):
  - the three checkboxes on and off at downing and death;
  - a Mark row keeps gear whole and unfamiliar;
  - an Exclude row leaves gear alone;
  - butchering strips and sorts, and an Excluded corpse is butchered as in the game;
  - the colony-era disassembly yield;
  - an old 0.5.2 settings file loads into the expected new values.
- `dotnet test`, both builds and the translation check. Not the full tier; that runs at release.

### Rewrite the existing tests (Kyle, 2026-11-25)

Existing tests that set or assert removed settings are rewritten to the new design, not deleted. A test survives as long as the behaviour it covers still exists. Delete a test only when its behaviour is cut: Degrade, the corpse chance, the research multiplier, the "break into components" fallback, and the arrest and strip toggles. Note each deleted test in "What was built".

- **Mod DevTests in this repo's `DevTests/`.** These batches set or read `dropMode`, `deathDrop`, `downedGear`, `sortWhen*`, the joiner settings, Degrade, `colonistsKeepGear` and the other removed keys: Bench, Milestone, Moments, Outcomes, PreviewStory, PromoClips, Saves, Settings, Smoke, Ui and WorkshopClips.
  - `ModHarness.cs` helpers such as `LootOnGround` become helpers for the new checkboxes, Who rows and category rules.
  - Cases written for a drop mode set the matching category rule or Who row instead.
- **Unit tests in `Tests/LootScavenging.Tests`:** CorpseEvaluator, Disassembly, DownedGear, DropMode, InventoryRules, JoinerGear, LootEvaluator, PackDefaults, Presets, SettingKeys, SettingsLoad and `TestRules.cs`. DropModeTests becomes Who-row and beyond-reach rule tests.
- **Filming batches** (PreviewStory, PromoClips, WorkshopClips) must compile and keep their scenes under the new defaults. Refilming is a separate step.
- **Don't edit the shared harness** `E:\Coding\Rimworld\RimWorldDevTests`, because other people have uncommitted work there. If a rewrite needs a harness change, stop and report it.
- After the rewrite, run the moments, outcomes, settings, saves and smoke batches. Record the run ids in TESTING.md. Update any `TESTING.md` procedure that names a removed setting.

## Docs

`CHANGELOG.md` 0.6.0: rewrite the unreleased tickets 21, 23 and 24 lines into this design. `spec.md` (settings, moments, loot rules), `steam-description.md` (near the 8,000-character cap), `About.xml` description, `TESTING.md`, `packs/progression`, both Auto Mod Config files and the preset `<version>`. Add a one-line pointer to this ticket in tickets 21, 23 and 24. Read `E:\Coding\Templates\writing-copy.md` first.

## Comments
