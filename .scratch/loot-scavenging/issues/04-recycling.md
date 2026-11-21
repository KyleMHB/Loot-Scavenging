# 04: Recycling at existing benches

**What to build:** Scrap is recycled through a bill at a bench the colony already owns. One XML tier table names each tier's bench and its fixed era-typical yields, so a pack can patch both without code. A bench recycles its own tier and every tier below it, with no second check on the colony's era. Bills support the usual controls, including a keep-in-stock count. See the spec's "Recycling" section and its proposed table.

**Blocked by:** 03

**Status:** done

**Recommended model:** `sonnet` — A narrow slice that follows patterns set by earlier tickets.

- [x] A machining table recycles industrial and medieval scrap. (rule unit-tested; in-game: pending)
- [x] A smithy refuses industrial scrap. (rule unit-tested; in-game: pending)
- [x] Each unit returns the materials the tier table lists. (yield maths unit-tested; in-game: pending)
- [x] Recycling bills are added only to the named benches, and a bench from another mod still shows all its own bills. (in-game: pending)
- [x] Both the 1.7 and 1.6 builds compile.

## Comments

### 2026-11-21: implemented on ls/04

Built:
- `Defs/ScrapTiers.xml`: one `LootScavenging.ScrapTierDef` per tier (scrap, benches, yields with optional `chance`, `batchSize`, `workPerUnit`). Patchable by packs. Starting values: Neolithic CraftingSpot wood 3 + plain leather 1; Medieval FueledSmithy/ElectricSmithy steel 3 + cloth 2; Industrial TableMachining steel 4 + industrial component 10%; Spacer FabricationBench plasteel 3 + advanced component 3%; Ultra FabricationBench plasteel 4 + advanced component 6%; Archotech FabricationBench plasteel 5 + advanced component 10%. Batch 5 units, work 80 to 220 per unit.
- `Source/Model/Recycling.cs`: `RecycleBenches.UsersFor` (a bench recycles its highest named tier and below), `YieldMath.Fixed/RollChance/Expected`. Tests in `RecyclingTests.cs`.
- `Source/Recycling/`: `ScrapTierDef`, and `ScrapTierTable` (the reader for ticket 08: `Yields(tier)`, `MakeThings`, `ChanceProducts`; use `YieldMath.Expected` for averages); `RecycleRecipes` (Harmony postfix on `DefGenerator.GenerateImpliedDefs_PreResolve`, one RecipeDef `LS_Recycle_<Tier>` per tier, attached only through `recipeUsers`; never touches any bench's `recipes`); `ScrapThing` (scrap `thingClass`, rolls the chance lines in `SmeltProducts`); `RecipeWorkerCounter_Recycle`; `RecycleDebugActions`.
- `Scrap.xml` scrap base now uses `LootScavenging.ScrapThing`.

Decisions:
- Certain yield is the recipe `products`; chance lines come from the `Smelted` special product, so extras go through the bill's store mode like the rest. The game offers keep-in-stock only for one-product recipes without special products, so a custom counter enables it and counts the FIRST listed yield (wood, steel, steel, plasteel).
- One recipe per tier, not per bench; users are the benches of that tier and above. A bench named for two tiers counts at the higher.
- Bills use whole batches (default 5 units); fewer leftover units wait. Efficiency stats are ignored.
- Labels are translated in the postfix (vanilla does the same for its implied recipes).

Tests: `dotnet test` 110 passed; both builds 0 warnings; translation check passes.
In-game pending: dev action "Recycling: log recipes per bench" (Debug actions > Loot Scavenging) lists each named bench's recipes; check Machining table offers neolithic/medieval/industrial bills, smithy no industrial, a modded bench keeps its bills, a bill of 5 industrial scrap gives 20 steel and now and then a component, keep-in-stock option shows.

### 2026-11-21: review fixes

- A lone unit of scrap can now be recycled: recycling bills (scrap and studied scrap) take one whole stack of any size (`ignoreIngredientCountTakeEntireStacks`, as vanilla "burn drugs" does; the ingredient search returns after the first matching stack). Before, a bill needed a full batch of 5, so a stone knife's single unit could never be recycled. `batchSize` is now the most units one iteration uses: `BillWorkPatch` trims the placed count at work start, the game splits the stack down to that count at the finish, and the rest stays at the bench for the next iteration. Work is `workPerUnit` times the units used (`RecycleBatch`, tested). No XML sets `batchSize`, so Defs and the pack preset need no change. In-game: pending (a single unit recycles; a 40-unit stack runs as 8 iterations).

- Follow-up: the batch cap now applies when the ingredients are chosen (`BillIngredientCapPatch`, a postfix on the private `WorkGiver_DoBill.TryFindBestBillIngredients`, same signature in 1.6 and 1.7), so only the units one iteration uses are hauled. A capped whole-stack haul would have left the rest of the pile on the bench, and the bill giver clears its ingredient cells before each job (`WorkGiverUtility.HaulStuffOffBillGiverJob`), so the pile would have been carried off and back every iteration. The work-start trim stays as a guard. In-game: pending (the patch applies, and a large pile is fetched 5 units at a time with nothing hauled off the bench).

### 2026-11-21: closed

Merged on `loot-scavenging`. 270 unit tests, both builds and the translation check pass. Smoke run `20261121-094209-lootscavenging-smoke-s1-cd8ccd5d` (RimWorld 1.7, Harmony + Core + Loot Scavenging, 0 mod errors) observed: the machining table lists industrial, medieval and neolithic recycle recipes, the smithy has no industrial one, vanilla bills kept. Still unobserved in game: real recycle bill jobs (yields per unit, components and chemfuel), the machining table box (no actual bill was run), keep-in-stock in the UI.

### 2026-11-21: in-game coverage

Observed (bench, outcomes): a machining-table bill recycled 12 industrial units into pool materials; 200 seeded units returned 2287 silver of materials against 2200 expected (unseeded 2161), components and chemfuel included. FAILED on both mod sets: the bill hauled all 12 units and used them in one iteration (expected 5, 5, 2 with at most 5 carried), so the haul cap and batch size did not apply. Still unobserved: keep-in-stock in the bill UI (the counter reports none). Runs (RimWorld 1.7, seed 1): default set `20261121-110125`…`110245` and `20261121-105946`/`105948`/`105952`; `dlcs` set `20261121-110142`…`110301`; full ids in TESTING.md.

### 2026-11-21: bench fixes

Fixed in `ee1977a`: the generated recipes were stored in `tierOf` before `AddImpliedDef` set their `defNameHash`, which `Def.GetHashCode` returns, so every `TryGetTier` lookup missed; `BillRecipes.New` now resolves the hash first. The 12-unit bill now runs 12>7>2>0 with at most 5 carried on both mod sets (129 and 109 silver of materials against 132). Runs (RimWorld 1.7, seed 1): default set bench `20261121-111417-lootscavenging-bench-s1-356b2793` (smoke `20261121-111405-…-0920bef4`, saves `20261121-111421-…-83df63a9`, settings `20261121-111424-…-ed3fee1c`, all passed); `dlcs` bench `20261121-111438-lootscavenging-bench-s1-7c6eabd9` (passed).
