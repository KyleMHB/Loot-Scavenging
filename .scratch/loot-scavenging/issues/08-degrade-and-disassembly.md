# 08: Degrade and instant disassembly

**What to build:** Two more outcomes for category rules. Degrade leaves the item at a random hit-point level inside a set range (10% to 40% by default), with an option to also lower its quality one step. Disassemble replaces the item on the spot with materials. A setting picks the yield: era-typical materials from the tier table, or the item's own cost list and material at a set efficiency, computed by the mod rather than the vanilla smelt function. Materials are placed nearby in full stacks and forbidden like other enemy drops. Loaded charges in reloadable gear follow their own setting: eject, or go with the item. See the spec's outcome table and "Code structure".

**Blocked by:** 04

**Status:** done

**Recommended model:** `sonnet` — A narrow slice that follows patterns set by earlier tickets.

- [x] A degraded item lands inside the set range, and the quality option lowers its quality one step.
- [x] Changing the efficiency setting visibly changes the item-based yield.
- [x] The era-typical yield matches the tier table.
- [x] No refund stack is lost when materials exceed one stack.
- [x] Loaded charges eject or stay with the item according to the setting.
- [x] Both the 1.7 and 1.6 builds compile.

The first five boxes are covered by unit tests of the maths but need the game to confirm, so they stay unticked (in-game: pending).

## Comments

### 2026-11-21: implemented on ls/08

- Built: `Source/Model/Disassembly.cs` (degrade hit points, quality step, item-based yield, components fallback, ejected ammo maths; unit tests in `Tests/LootScavenging.Tests/DisassemblyTests.cs`), `Source/Loot/DisassembleApplier.cs` (game glue). `OutcomeApplier.Apply` now takes the era and reason. New `LootRules` fields with Scribe lines: `degradeMin/Max` (0.1/0.4), `degradeLowersQuality` (off), `disassemblyYield` (EraTypical), `disassemblyEfficiency` (0.5), `ejectCharges` (on).
- Decisions the spec left open: degrade never repairs (an item already below the picked level keeps its hit points) and never drops below 1 HP. Item-based yield is `CostList` plus `CostStuffCount / stuff.VolumePerUnit` of the item's stuff, times efficiency, fractions rounded at random; an item with no cost list or material falls back to the era-typical yield. Era-typical yield is the tier table scaled by the item's scrap units. The era "Components" fallback gives `ComponentIndustrial` x max(1, scrapUnits / 2) in either yield mode. Charges are ejected only on disassembly (a degraded item keeps them), as `AmmoDef` stacks via props `ammoCountPerCharge`, or a proportional share of `ammoCountToRefill`.
- Tests: `dotnet test` 119 passed; both builds and the translation check pass.
- In-game: pending. Degrade range and quality, disassembly placement and forbidding, yields, charge ejection (needs a reloadable item such as a shield belt or charge lance).

### 2026-11-21: review fixes

- Charge ejection now covers every outcome that destroys or replaces the item: scrap, disassemble and delete (`Disassembly.EjectsCharges`, tested). Before, only disassembly ejected, so a scrapped or deleted reloadable item lost its loaded ammo with the setting on. `OutcomeApplier.Apply` reads the charges and the pawn's place before the outcome and puts the ammo on the ground near the pawn, forbidden, once. Not into the inventory, where the inventory pass would judge it again. Keep and degrade leave charges loaded. The row's On/Off sentences now say this. In-game: pending (scrap a raider's loaded reloadable item).

### 2026-11-21: closed

Merged on `loot-scavenging`. 270 unit tests, both builds and the translation check pass. Smoke run `20261121-094209-lootscavenging-smoke-s1-cd8ccd5d` (RimWorld 1.7, Harmony + Core + Loot Scavenging, 0 mod errors) observed: nothing. Still unobserved in game: degrade range and quality option, efficiency setting, era-typical yield, refund stacks, loaded charges.

### 2026-11-21: in-game coverage

Observed (outcomes): eight degraded revolvers landed at 24-38 of 100 hit points (range 10-40), quality kept with the option off and Normal became Poor with it on; item-based yield gave 15 steel and 1 component at efficiency 0.5, 30 and 2 at 1.0, all forbidden; era-typical yield returned only industrial pool materials; 30 industrial medicine returned 90 cloth as 75 + 15; a smokepop belt ejected 10 chemfuel when scrapped, deleted and disassembled with the setting on (50 with its 40 cost-list chemfuel) and none with it off. Runs (RimWorld 1.7, seed 1): default set `20261121-110125`…`110245` and `20261121-105946`/`105948`/`105952`; `dlcs` set `20261121-110142`…`110301`; full ids in TESTING.md.
