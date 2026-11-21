# 09: Inventory groups, protection and lists

**What to build:** Category rules for the inventory groups: ammunition (recognised by item category, with no dependency on any ammo mod), drugs, medicine, food and other. Each category's conditions (beyond reach, below a quality, below a hit-point percentage) and chance work. Quest-tagged items, unique weapons, persona weapons, relics and items on the keep list are never touched; items on the remove list are targeted. Lists are stored by item name, and there is no picker yet. A whole dropped stack is one item, and a partial drop acts on the piece that left the pawn. Mechanoid reward materials outside the inventory are left alone. See the spec's "Loot rules" section.

**Blocked by:** 01

**Status:** ready-for-agent

**Recommended model:** `sonnet` — A narrow slice that follows patterns set by earlier tickets.

- [ ] A raider carrying medicine and a quest weapon drops both untouched with the default rules.
- [x] An item on the keep list is never touched, and one on the remove list is acted on.
- [x] A partial stack drop is never partly deleted.
- [x] Each category's chance is rolled on its own.
- [x] A list entry naming an item that no longer exists is ignored without an error.
- [x] Both the 1.7 and 1.6 builds compile.

## Comments

### 2026-11-21: implemented on ls/09

Built in `Source/Model` (plain C#, unit-tested) and thin glue in `Source/Loot`.

- `ItemCategory` now has Ammunition, Drugs, Medicine, Food and Other, each with its own `CategoryRule` in `LootRules`. `CategoryRule` gained conditions (`onlyBeyondReach`, `limitQuality`/`belowQuality`, `limitHitPoints`/`belowHitPointFraction`) and `chance`. They apply to weapons and apparel too.
- Evaluator: step 4 `CheckProtection` (quest-tagged, unique, persona, relic, keep list) sits after the moment check; step 6 conditions and step 7 chance are in `ApplyCategory`. `Evaluate(item, rules, roll)` takes an injectable roll (`Func<double>`, [0,1)); it is only called when the category chance is below 100%, so each category rolls on its own and only for items that qualify.
- `DropCandidate` gained `DefName`, `OutOfReach` (filled from `EraReader.ItemEra(def, pawn.Faction).OutOfReach`), `QuestTagged`, `UniqueWeapon`, `PersonaWeapon`, `Relic`. `ItemClassifier` reads them from the game: quest tags via `Thing.questTags`, relic via `IsRelic()`, unique via `CompUniqueWeapon`, persona via `CompBladelinkWeapon`. Ammunition is any item whose thing category (or an ancestor) has "Ammo" in its def name; then medicine (`IsMedicine`), drugs (`IsDrug`), food (`IsNutritionGivingIngestible`), else other.
- Lists: `LootRules.keepList` and `removeList`, def-name strings, Scribed with `Scribe_Collections`. Names are only compared as strings, so an entry for a missing item does nothing and raises nothing. Protection toggles (`protect*`, default on) exist in the rules and settings for ticket 11 to expose.
- Decisions the spec left open: (1) reach is a gate and quality/hit-point limits are alternatives (item qualifies below either), which is what the Cleanup preset's "below Normal quality or below 50% hit points" needs; an item with no quality or hit points never meets that limit. (2) The remove list skips conditions and chance and uses the category's outcome, or Delete when that outcome is Keep. It sits in step 5, after protection (the keep list wins) and after the owner, place and moment checks. Ticket 03's drop-mode step goes between step 4 and `ApplyCategory`, so it runs first.
- Partial stacks: the hooks sort `inventory.innerContainer` before the game drops anything, so every judged item is a whole stack and `OutcomeApplier.Delete` removes the whole thing. No hook sees a partial drop (manual or caravan drops are not hooked), so nothing can partly delete a stack. A comment in `GearSorter.Judge` records this.
- Mechanoid reward materials are not in the pawn's inventory container, so they are never visited.
- Tests: `dotnet test` 52 passed (`InventoryRulesTests`, plus `ItemsWithoutACategoryRuleAreKept` became `InventoryGroupsAreKeptByDefault` because Other now has a rule). Both builds clean, translation check passed.
- In-game pending: the first acceptance line (raider with medicine and a quest weapon) and a real keep/remove list pass. Dev actions under "Loot Scavenging": "Inventory groups: toggle delete", "Keep list: toggle an item", "Remove list: toggle an item". Log lines show `Protected`, `RemoveList`, `ConditionsNotMet`, `ChanceFailed`.

### 2026-11-21: review fixes

- The acceptance line "a raider carrying medicine ... drops both untouched" holds under the default rules only for herbal medicine, or once Medicine Production is finished. Industrial medicine is gated by that research, and the default drop mode scraps out-of-reach inventory too. This is an open spec question; see ticket 02's review note. Raw resources (silver, steel) no longer take the carrier's era, so they stay untouched.
