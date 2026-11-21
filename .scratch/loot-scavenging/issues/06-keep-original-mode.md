# 06: "Keep the original item" mode: unfamiliar label and equip block

**What to build:** The fourth drop mode keeps out-of-reach gear whole. Such gear is labelled unfamiliar and cannot be equipped until its research finishes. A setting chooses the scope: all out-of-reach gear from any source (default, worked out from research and saving nothing), or only gear looted from pawns (which saves a marker on each looted item). See the spec's "Original-item mode" paragraph.

**Blocked by:** 01, 02

**Status:** done

**Recommended model:** `sonnet` — A narrow slice that follows patterns set by earlier tickets.

- [x] A looted charge rifle in a tribal colony shows as unfamiliar and cannot be equipped.
- [x] Once its research finishes, the same rifle can be equipped.
- [x] With the looted-only scope, an out-of-reach item that was not looted can be equipped, and the marker survives a save and reload.
- [x] The default scope adds nothing to the save file. (By design: the comp saves only when a marker is set; save check in game pending.)
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

### 2026-11-21: review fixes

- The "break it into components" era fallback now applies in keep-original mode too: gear with no era of its own becomes components at the drop, as in the two scrap modes. Before, it fell through to its category rule and was kept whole and never unfamiliar. Only the Off mode still ignores the fallback. Test: `DropModeTests.ComponentsFallbackBreaksGearIntoComponentsWheneverEraHandlingIsOn`.

- Default scope and uncraftable gear: with no saved marker, an item met later has no carrier, so gear with no research and no tech level (Progression: Arsenal's uncraftable weapons) was never unfamiliar, though it was kept whole at the drop because its carrier was above the colony. `EraReader.ItemEra` with no carrier now uses the era of the factions that typically carry the item: every non-player faction's pawn group kinds, matched by the kind's weapon tags, apparel tags and required apparel against the item's tags (`Model/CarrierEras.cs`, built once). Where several factions carry it the highest era wins, so gear kept whole because of a high carrier never reads as familiar. The cost: a weapon carried by both tribals and pirates stays unfamiliar to a tribal colony even when taken from tribals. Gear no faction generates with stays Unknown, within reach. The label, equip block, bench tier and dev readout all read this one value. Unit-tested in `CarrierErasTests`; the faction walk is glue (in-game: pending; check with the dev readout on an uncraftable modded weapon).

### 2026-11-21: closed

Merged on `loot-scavenging`. 270 unit tests, both builds and the translation check pass. Smoke run `20261121-094209-lootscavenging-smoke-s1-cd8ccd5d` (RimWorld 1.7, Harmony + Core + Loot Scavenging, 0 mod errors) observed: the charge rifle was labelled "(unfamiliar)", CanEquip was false with reason "unfamiliar technology", and it was equippable after Charged Shot finished. Still unobserved in game: looted-only scope and marker surviving save and reload, default scope save check.

### 2026-11-21: in-game coverage

Observed (saves): in looted-only scope a pirate's charge rifle carried a Spacer marker and was unfamiliar while an unlooted one was equippable; after save and reload the marker and unfamiliarity held, and the save held exactly one `lootScavengingLootedEra` (an unmarked item writes nothing). Runs (RimWorld 1.7, seed 1): default set `20261121-110125`…`110245` and `20261121-105946`/`105948`/`105952`; `dlcs` set `20261121-110142`…`110301`; full ids in TESTING.md.
