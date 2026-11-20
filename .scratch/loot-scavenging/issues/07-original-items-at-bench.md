# 07: Original items at the bench

**What to build:** An unfamiliar item counts as its scrap equivalent at the bench: the same era, amount, recycling yields and study points. It is studied or recycled only when the player marks it or a bill filter allows it, so nothing the player wants to keep is used up. See the spec's "Original-item mode" paragraph and the requirement "Never consume what the player wants to keep".

**Blocked by:** 04, 05, 06

**Status:** ready-for-agent

**Recommended model:** `sonnet` — A narrow slice that follows patterns set by earlier tickets.

- [ ] (in-game: pending) A looted rifle recycles for the same yield as the scrap it would have become.
- [ ] (in-game: pending) Studying it pays the same points as that scrap.
- [x] An unfamiliar item that is neither marked nor allowed by a filter is never picked up for recycling or study. (default filters allow nothing; code-checked, in-game pending)
- [x] Both the 1.7 and 1.6 builds compile.

## Comments

### 2026-11-21: implemented on ls/07

Built: `Source/Unfamiliar/ScrapEquivalent.cs` (item -> tier and units, recycle products, work, bench rule) and `Source/Unfamiliar/OriginalItemRecipes.cs` (two recipes, recycle worker, Harmony patches). `ScrapStudy.TryGetWorth(Thing)` now returns an unfamiliar item's worth (its tier's points, units per thing); `StudyBillPatch` waits an original study bill when there is no project or the bench cannot research it.

Decisions the spec left open:
- **Filter, not marking.** Two recipes, `LS_RecycleOriginal` (smithy and other recycling benches) and `LS_StudyOriginal` (research benches), take one item per iteration. Their ingredient filter lists every weapon and apparel def but the default allows none, so the player ticks in what they will use up. No designation or gizmo was added.
- A bill takes an item only while `Unfamiliar.IsUnfamiliar` is true. Recycling also needs a bench that recycles the item's tier (the scrap bench rule); study needs the current project to be payable by that tier (the scrap verdict).
- Tier: the looted marker's era when the item has one, else `EraReader.ItemEra(def, null)`; units: `ScrapAmounts.Units(MarketValue, stackCount)`, as `ScrapApplier` does.
- Work per item is its units at the tier's per-unit work (set when the worker starts, `OriginalWorkPatch`); the bill list shows a nominal value. No keep-in-stock for these bills.
- Products and remains are placed near the worker rather than carried to the bill's store.
- Merge with ticket 15: recycling yield goes through the single `ScrapEquivalent.RecycleProducts(Thing)`; swap its body for `ScrapTierTable.RollProducts(tier, units, valueFactor)`.

Tests: `dotnet test` 163 passed; both builds 0 warnings; translation check passes. No new unit tests (all new code is game glue; tier, units and yields reuse tested model code).

In-game pending: a looted unfamiliar rifle ticked into a recycle bill at a smithy returns the yield of its scrap and is destroyed; ticked into a study bill it pays the same points as its scrap; an unticked unfamiliar item is never taken; a familiar item is never taken; a higher-tier item is refused at a lower bench; filter UI shows the gear tree with nothing ticked; work time scales with units.
