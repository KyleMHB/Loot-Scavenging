# 15: Scrap loot pools

**What to build:** Replace each scrap tier's fixed yield with a weighted loot pool and a value per unit. Each unit of scrap picks one material by weight. The amount is the tier's value per unit divided by that material's market value, rounded up or down at random so the average is exact. Recycling and the era-typical yield of instant disassembly both use the pools. Recycling bills lose the keep-in-stock count, and their text names no materials. See the spec's "Recycling" section and the "Recycling yields" decision. The decision was settled 2026-11-21 because fixed yields oversupplied basic materials, steel above all.

**Blocked by:** 04, 08

**Status:** done

**Recommended model:** `sonnet`: a contained rework of code from tickets 04 and 08, with the design settled.

## Scope

- `Source/Model/Recycling.cs`: replace `YieldEntry`'s count and chance with a pool entry (item, weight) and the pick maths: a weighted pick per unit, an amount from value per unit and market value with random rounding, and the expected share. Plain C#, with market value passed in. Remove `YieldMath.Fixed`, `RollChance` and `Expected` once nothing uses them.
- `Source/Recycling/ScrapTierDef.cs`: `valuePerUnit` and `pool` (thing, weight) replace `yields`. Config errors cover a missing thing, a weight of 0 or less, a `valuePerUnit` of 0 or less, and an empty pool. `ScrapTierTable` reads the pool and gives the products for N units.
- `Source/Recycling/RecycleRecipes.cs`: no `products`. Every output comes through the `Smelted` special product, which `ScrapThing` rolls from the pool. Drop `YieldSummary`. The description says the scrap is recycled into materials of its era.
- `Source/Recycling/RecipeWorkerCounter_Recycle.cs`: delete it and the `workerCounterClass` line.
- `Source/Recycling/ScrapThing.cs`: `SmeltProducts` returns the pool roll for `stackCount` units.
- `Source/Loot/DisassembleApplier.cs`: the era-typical yield rolls the pool for the item's scrap units. The components fallback and the item-based yield are unchanged.
- `Defs/ScrapTiers.xml`: rewrite the four tiers from the spec's table. Set weights so lower-tier materials total about 15% of a tier's weight. Add `MayRequire` entries only for DLC materials that fit an era. Update the patching example in the header comment.
- `Languages/English/Keyed/LootScavenging.xml`: remove `LootScavenging_YieldFixed` and `LootScavenging_YieldChance`, and reword `LootScavenging_RecycleDescription`.
- Tests: replace the yield tests in `RecyclingTests.cs` and the era-typical tests in `DisassemblyTests.cs` where they assume fixed lines.

Out of scope: the Progression pack's materials (ticket 13), colony-aware pools, per-entry count ranges.

## Acceptance

- [x] Each unit of scrap returns exactly one pool material, and its average value matches the tier's value per unit. (maths unit-tested)
- [x] A material's long-run share of value matches its weight. (maths unit-tested)
- [x] A material worth more than the value per unit drops as a single item with the matching chance, for example a component at 11 silver per unit about a third of the time. (maths unit-tested)
- [ ] A bill of 5 industrial scrap at a machining table returns a mix of pool materials. (in-game: pending)
- [ ] The recycling bill offers no keep-in-stock option, and its description names no materials or amounts. (in-game: pending)
- [ ] Instant disassembly with the era-typical yield returns pool materials scaled by the item's scrap units. (in-game: pending)
- [ ] A `MayRequire` pool entry for an inactive DLC is skipped without errors. (in-game: pending)
- [x] Both the 1.7 and 1.6 builds compile, the unit tests pass and the translation check passes.

## Comments

### 2026-11-21: implemented on ls/15

- Built: `Model/Recycling.cs` now holds `PoolEntry<T>` and `LootPool` (`TryPick`, `Amount`, `Roll`, `Expected`); `YieldEntry` and `YieldMath` are gone. Tests in `RecyclingTests.cs` (pick by weight, random rounding, a component at 11 silver about a third of the time, long-run value and share of value, value factor).
- `ScrapTierDef`: `valuePerUnit` and `pool` (thing, weight) replace `yields`, with config errors for no thing, weight 0 or less, `valuePerUnit` 0 or less and an empty pool. `ScrapTierTable.RollProducts(ScrapTierDef def, int units, float valueFactor = 1f)` is the one entry point; ticket 07 calls it for an unfamiliar original item counted as its scrap equivalent. `ScrapTierTable.ValueFactor(def, scrapDef)` gives 1 for scrap and `studiedYieldFactor` (capped at 1) for studied scrap.
- Studied scrap uses the same pool at value per unit times `studiedYieldFactor`, with random rounding keeping the average exact. `StudiedYield.cs` and its tests were deleted, since the pool maths supersedes them.
- `ScrapThing.SmeltProducts` rolls the pool; `DisassembleApplier` era-typical yield rolls it for the item's scrap units (components fallback and item-based yield unchanged). Recycling recipes have no `products`, so a bill has no keep-in-stock option; `RecipeWorkerCounter_Recycle` is deleted. Study's counter is untouched.
- `Defs/ScrapTiers.xml`: values 6, 9, 11, 33. Weights are shares of value; lower-tier materials total 15% per tier. No `MayRequire` entries: no DLC material fits an era cleanly. The header comment shows the syntax and a patch example.
- Text: `LootScavenging_YieldFixed` and `_YieldChance` removed; `_RecycleDescription` names no materials.
- Tests: `dotnet test` 173 passed; both builds 0 warnings; translation check passes.
- In-game: pending (all four in-game boxes).

### 2026-11-21: closed

Merged on `loot-scavenging`. 270 unit tests, both builds and the translation check pass. Smoke run `20261121-094209-lootscavenging-smoke-s1-cd8ccd5d` (RimWorld 1.7, Harmony + Core + Loot Scavenging, 0 mod errors) observed: single-unit recipe products all came from the tier pool (20 seeded rolls). Still unobserved in game: a real 5-unit bill, components and chemfuel appearing, keep-in-stock absence and description in the UI, instant disassembly, MayRequire entry for an inactive DLC.
