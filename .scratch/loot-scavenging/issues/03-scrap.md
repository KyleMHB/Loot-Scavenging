# 03: Scrap and the two scrap drop modes

**What to build:** Out-of-reach gear becomes generic, fully stackable scrap of its era. The amount scales with the item's worth, quality and condition. Scrap is worth close to nothing by default, with a setting to change that. It appears on the ground by default, with a setting to leave it on the body. The drop mode setting offers "scrap out-of-reach gear" (default), "scrap everything" and "Off". Placeholder icons are fine. See the spec's "Scrap" section and the drop mode table.

**Blocked by:** 01, 02

**Status:** done

**Recommended model:** `sonnet` — A narrow slice that follows patterns set by earlier tickets.

- [x] A tribal colony that kills a pirate gets a stack of industrial scrap, sized by the gear.
- [x] A stone knife becomes about one unit of neolithic scrap, and a charge rifle becomes several units of spacer scrap.
- [x] "Scrap everything" turns all dropped gear into scrap, and "Off" leaves out-of-reach gear to its category rule.
- [x] Scrap from a large raid lands in a few piles, not one per item.
- [x] The scrap modes save nothing in the game file and add no data to items. (by construction; in-game save check pending)
- [x] Both the 1.7 and 1.6 builds compile.

## Comments

### 2026-11-21: implemented on ls/03

Built:
- `Source/Model/LootEvaluator.cs`: drop mode step `ApplyDropMode`, between the moment step and the category step (ticket 09's protection goes before it). New `DecisionReason`s: `OutOfReachScrap`, `ScrapEverything`, `ComponentsFallback`, `OutOfReachKept`. `DropCandidate.ItemEra` is an `EraResult` filled from `EraReader.ItemEra(def, pawn.Faction)`, only read when the mode is not Off.
- `Source/Model/Scrap.cs`: `ScrapTier` (Neolithic, Medieval, Industrial, Spacer, Ultra, Archotech; new tiers go at the end), `ScrapTiers.TierFor`, `ScrapAmounts.Units` and `SplitIntoStacks`.
- `Defs/ThingDefs/Scrap.xml`: four scrap defs (`LS_Scrap_<tier>`, stack limit 500, vanilla placeholder textures tinted per tier) and an `LS_Scrap` category.
- `Source/Loot/ScrapApplier.cs`: scrap def lookup, the applier, and a `Pawn.Kill` postfix for the on-body setting. `GearSorter` calls it for `Outcome.Scrap` and skips existing scrap.
- Settings `scrapMarketValue` (default 0.01) and `scrapOnGround` (default true), saved in mod settings only. No UI yet (ticket 11, which should call `ScrapDefs.ApplyMarketValue()` on change).
- Dev actions under "Loot Scavenging": "Scrap: cycle drop mode", "Scrap: toggle on ground or on body", "Scrap: cycle scrap market value".

Decisions the spec left open:
- Modes: ScrapOutOfReach scraps out-of-reach gear and leaves the rest to its category rule. ScrapEverything scraps all gear. Off skips the step. KeepOriginal leaves out-of-reach gear as Keep (`OutOfReachKept`); within-reach gear follows its category rule. Ticket 06 adds the label and equip block elsewhere.
- Components fallback: in the two scrap modes the evaluator returns `Outcome.Disassemble` (`ComponentsFallback`), so such gear is never scrapped. The Disassemble applier is ticket 08's; until then it warns and keeps. In Off and KeepOriginal the flag is ignored and the category rule applies.
- Tier: Animal and Neolithic give neolithic scrap; Ultra and Archotech have their own scrap (split from spacer 2026-11-21). Gear with no era (only scrapped in "scrap everything") becomes scrap of the colony's era.
- Amount: per item max(1, round(sqrt(market value) / 8)), times the stack count. Market value already carries quality and hit points. A stone knife gives 1, a charge rifle about 5. Divisor: `ScrapAmounts.ValueScale`.
- Placement: scrap is added to the pawn's inventory, so the game drops it with the gear and merges it (one pile per tier near each body). With "on body" and a death, it is held in memory and added to the dead pawn's inventory after `Kill`; stripping the corpse drops it. At downed and strip moments the game drops inventory anyway, so the setting has no effect there.

Tests: `dotnet test` 61 passed. Both builds clean, 0 warnings; translation check passes.

In-game pending (dev mode, Debug actions > "Loot Scavenging"): kill a pirate at a tribal-era colony and look for an industrial scrap stack (log `became N LS_Scrap_Industrial`); stone knife about 1 neolithic unit, charge rifle several spacer; cycle the drop mode to check scrap everything and Off; a big raid gives a few piles; the on-body toggle leaves scrap in the corpse and stripping drops it; the market value toggle changes a stack's value; save and load with scrap on the map.

### 2026-11-21: review fixes

- Scrap left on the body (see ticket 10's review note): the set-aside scrap is now placed before corpse removal, so removal drops it to the ground. A pawn with no inventory gets it on the ground instead of having it destroyed. After a cancelled death the pawn keeps the scrap in its inventory, and a finalizer clears the entry when Kill throws.

### 2026-11-21: closed

Merged on `loot-scavenging`. 270 unit tests, both builds and the translation check pass. Smoke run `20261121-094209-lootscavenging-smoke-s1-cd8ccd5d` (RimWorld 1.7, Harmony + Core + Loot Scavenging, 0 mod errors) observed: a tribal colony's pirate revolver became 1 industrial scrap 1 cell from the corpse, matching ScrapAmounts.Units, with his silver and steel untouched. Still unobserved in game: stone knife and charge rifle amounts, scrap everything and Off modes, large-raid piles, scrap on body, save and load.

### 2026-11-21: in-game coverage

Observed (outcomes, saves): a stone knife became 1 neolithic unit under scrap everything and a charge rifle 4 spacer units, both matching `ScrapAmounts.Units`; Off left an out-of-reach charge rifle whole; ten pirates killed in a cluster left 10 units in 5 piles; scrap survived save and reload. Scrap on body is covered in ticket 10's comment. Runs (RimWorld 1.7, seed 1): default set `20261121-110125`…`110245` and `20261121-105946`/`105948`/`105952`; `dlcs` set `20261121-110142`…`110301`; full ids in TESTING.md.
