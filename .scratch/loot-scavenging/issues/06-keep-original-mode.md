# 06: "Keep the original item" mode: unfamiliar label and equip block

**What to build:** The fourth drop mode keeps out-of-reach gear whole. Such gear is labelled unfamiliar and cannot be equipped until its research finishes. A setting chooses the scope: all out-of-reach gear from any source (default, worked out from research and saving nothing), or only gear looted from pawns (which saves a marker on each looted item). See the spec's "Original-item mode" paragraph.

**Blocked by:** 01, 02

**Status:** ready-for-agent

**Recommended model:** `sonnet` — A narrow slice that follows patterns set by earlier tickets.

- [ ] A looted charge rifle in a tribal colony shows as unfamiliar and cannot be equipped.
- [ ] Once its research finishes, the same rifle can be equipped.
- [ ] With the looted-only scope, an out-of-reach item that was not looted can be equipped, and the marker survives a save and reload.
- [ ] The default scope adds nothing to the save file. (By design: the comp saves only when a marker is set; save check in game pending.)
- [x] Both the 1.7 and 1.6 builds compile.

## Comments

### 2026-11-21: implemented on ls/06

Built:
- `Source/Model/UnfamiliarRule.cs`: `UnfamiliarScope` (AllOutOfReach default, LootedOnly) and `UnfamiliarRule` (`IsUnfamiliar`, `ShouldMark`, `MarkerEra`). Unit-tested in `Tests/LootScavenging.Tests/UnfamiliarRuleTests.cs`.
- `Source/Unfamiliar/Unfamiliar.cs`: `Unfamiliar.IsUnfamiliar(Thing)` (the helper ticket 07 calls at the bench), `Blocks(thing, pawn)`, `NotifyKept(pawn, thing)`. A startup static constructor adds `CompProperties_Unfamiliar` to every weapon and apparel def.
- `Source/Unfamiliar/CompUnfamiliar.cs`: the label suffix "(unfamiliar)" (`TransformLabel`), the inspect line, and the looted marker (`lootedEra`, saved as `lootScavengingLootedEra`, written only when set).
- `Source/Unfamiliar/UnfamiliarHooks.cs`: postfixes on both `EquipmentUtility.CanEquip` overloads (greyed "cannot equip: unfamiliar technology" menu entry), `JobGiver_OptimizeApparel.ApparelScoreGain` (colonists never auto-wear it) and `ResearchManager.FinishProject` (cache reset).
- `LootScavengingSettings.unfamiliarScope` with its Scribe line. `OutcomeApplier` Keep case calls `Unfamiliar.NotifyKept`, which sets the marker when the scope is looted-only.
- `Languages/English/Keyed/LootScavenging.xml` created with three keys (ticket 03 may create the same file; merge by keeping both sets of keys).

Decisions the spec left open:
- Unfamiliar only applies when the drop mode is KeepOriginal, to weapons and apparel, and blocks colony pawns only (raiders and generation are untouched).
- "Out of reach" for the test uses `EraReader.ItemEra(def, null)`: no carrier, so research, item tech level and the fallback setting decide. Gear whose era came only from its carrier faction is within reach in the default scope.
- The looted marker stores the era the item had when looted (carrier included). Looted-only scope: unfamiliar while marked and (out of reach by research or marker era above the colony era), so carrier-era gear clears when the colony catches up.
- Marker is set for any kept weapon or apparel from a non-colony pawn that is out of reach at that moment. A comp (not a GameComponent) holds it, since a GameComponent writes an element into every save.
- Per-def reach and the colony era are cached for 250 ticks and cleared on research finish, game change and fallback change, because labels ask every frame.

Tests: `dotnet test` 39 passed; both builds clean, 0 warnings; translation check passed.

In-game pending (dev actions under "Loot Scavenging": "Unfamiliar: toggle keep-original mode", "cycle scope", "log selected item", "mark selected item as looted"): tribal colony shows a spawned charge rifle labelled unfamiliar with the greyed equip option and apparel not auto-worn; finishing its research clears it; looted-only scope leaves an unmarked rifle equippable and the marker survives save and reload; default scope save has no `lootScavengingLootedEra`.

