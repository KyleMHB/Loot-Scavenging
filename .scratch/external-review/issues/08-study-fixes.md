# 08: Study fixes

Status: ready-for-agent

**Recommended model:** `sonnet`

**Blocked by:** None (can start immediately)

**What to build:** Study pays the way research does and tells the player when it can't. Spec, finding 8 and "Study and recycle".

## Scope

- Divide the points by the project's cost factor for the player's faction (decision 2); add the points to the researcher's `ResearchPointsResearched` record; the message shows research-tab points.
- Don't spend the bill's count when nothing was paid, and show a message saying why.
- Leave an unfamiliar item unused when the project needs only a small share of its worth.
- `CountProducts` honours `bill.includeSlotGroup`.
- The work bar's total matches the real work for study, recycle and unfamiliar-item bills.
- `ITab_StudyBills` gets the game's bill paste button.
- "Can be studied now" only when the map has a bench that can research the project.
- New or changed Keyed text in all three languages.

## Done when

- Each bullet above is visible in a game with a study bench.
