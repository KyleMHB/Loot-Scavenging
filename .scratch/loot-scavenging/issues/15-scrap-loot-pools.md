# 15: Scrap loot pools

**What to build:** Replace each scrap tier's fixed yield with a weighted loot pool and a value per unit. Each unit of scrap picks one material by weight. The amount is the tier's value per unit divided by that material's market value, rounded up or down at random so the average is exact. Recycling and the era-typical yield of instant disassembly both use the pools. Recycling bills lose the keep-in-stock count, and their text names no materials. See the spec's "Recycling" section and the "Recycling yields" decision. The decision was settled 2026-11-21 because fixed yields oversupplied basic materials, steel above all.

**Blocked by:** 04, 08

**Status:** ready-for-agent

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

- [ ] Each unit of scrap returns exactly one pool material, and its average value matches the tier's value per unit. (maths unit-tested)
- [ ] A material's long-run share of value matches its weight. (maths unit-tested)
- [ ] A material worth more than the value per unit drops as a single item with the matching chance, for example a component at 11 silver per unit about a third of the time. (maths unit-tested)
- [ ] A bill of 5 industrial scrap at a machining table returns a mix of pool materials. (in-game: pending)
- [ ] The recycling bill offers no keep-in-stock option, and its description names no materials or amounts. (in-game: pending)
- [ ] Instant disassembly with the era-typical yield returns pool materials scaled by the item's scrap units. (in-game: pending)
- [ ] A `MayRequire` pool entry for an inactive DLC is skipped without errors. (in-game: pending)
- [ ] Both the 1.7 and 1.6 builds compile, the unit tests pass and the translation check passes.

## Comments
