# 09: Multiplayer roll and cost

Status: ready-for-agent

**Recommended model:** `sonnet`

**Blocked by:** None (can start immediately)

**What to build:** Loot rolls use the game's random numbers, and era checks stop walking every project per call. Spec, finding 9 and "Cost".

## Scope

- Every game-side `LootEvaluator.Evaluate` caller passes `() => Rand.Value`.
- Cache `EraReader.ColonyEra()` per 250 ticks, cleared when a project finishes.
- Cache the `ScrapTier` array in `ScrapStudy`.

## Done when

- The existing tests pass.
- No `System.Random` roll is reached from game code.
