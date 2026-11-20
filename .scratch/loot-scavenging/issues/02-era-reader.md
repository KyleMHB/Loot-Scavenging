# 02: Era reader

**What to build:** One function returns the colony's current era and one returns an item's era. An item's era comes from the tech level of the research that unlocks it, then the item's own tech level, then the era of the faction that carried it. Two settings replace that last fallback: treat such gear as the colony's own era, or break it straight into components. "Out of reach" means the item's research is unfinished or its era is above the colony's. A dev readout shows the result for a chosen item. See the spec's "Loot rules" and "The Progression pack".

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

**Recommended model:** `sonnet` — A narrow slice that follows patterns set by earlier tickets.

- [ ] The dev readout names the right era and out-of-reach result for a vanilla researched weapon, a vanilla unresearched weapon, an uncraftable weapon and a modded item with no research.
- [x] The colony's era is read through a single function, so a later pack integration can supply the value.
- [ ] The fallback settings change the result the readout shows. (Ranking verified by unit tests; readout in-game pending.)
- [x] Both the 1.7 and 1.6 builds compile.

## Comments

### 2026-11-21: implemented on ls/02

Built:
- `Source/Model/EraRanking.cs`: `Era` (mirrors `TechLevel`), `EraFallback`, `EraSource`, `EraResult`, `EraRanking.Resolve` and `EraRanking.ColonyEra`. Unit-tested in `Tests/LootScavenging.Tests/EraRankingTests.cs`.
- `Source/Era/EraReader.cs`: `EraReader.ColonyEra()` and `EraReader.ItemEra(def, carrierFaction)`. Packs set `EraReader.ColonyEraOverride` (a `Func<Era?>`) to supply the colony era.
- `Source/LootScavengingSettings.cs`: `eraFallback` (CarrierFaction, ColonyEra, Components), saved. No settings UI yet (ticket 11).
- `Source/Era/EraDebugActions.cs`: dev actions under "Loot Scavenging": "Era: colony era and fallback", "Era: cycle fallback setting", "Era: readout for an item..." (pick any weapon or apparel def; logs the result with no carrier and for each visible faction).

Decisions the spec left open:
- Colony era = highest `techLevel` among finished research projects with `baseCost > 0`, floored at Animal (the pack starts as low as Animal, so the static player faction level is not used as a floor). Computed per call, no cache.
- Item research = the item's `recipeMaker` and `researchPrerequisites`, else the first recipe producing it that names research. With several prerequisites the highest tech level sets the era, and any unfinished one makes it out of reach.
- Unfinished research is out of reach whatever the era. Era above colony era is out of reach.
- The two fallback settings are one three-way enum, since they are mutually exclusive. ColonyEra gives such gear the colony's era (never out of reach). Components sets `BreakIntoComponents` (not out of reach; the applier ticket acts on the flag).
- Gear with no era from any step (e.g. no carrier) has source Unknown and is within reach.

Tests: `dotnet test` 11 passed; both builds clean, 0 warnings.

In-game pending: readout for a vanilla researched weapon, an unresearched weapon, an uncraftable weapon and a modded item with no research; fallback cycling changes the readout.
