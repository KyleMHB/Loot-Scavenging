# Loot Scavenging Testing

Updated through: the `loot-scavenging` branch on 2026-11-21

## Agent read route

- Headings: `rg -n '^#' TESTING.md`.
- Writing cases or changing the harness: the [harness agent guide](../RimWorldDevTests/AGENTS.md).
- What to run for a change: [Test tiers](#test-tiers).

## Test prerequisites

- Unit tests: `dotnet test` from the repo root (`Tests/LootScavenging.Tests`, plain C#, no game needed).
- In-game batches: deploy with `powershell -ExecutionPolicy Bypass -File deploy.ps1`, build `dotnet build DevTests/LootScavenging.DevTests.csproj -c Release`, then `python tools/ls_test.py run lootscavenging-smoke --seed 1`, and `python tools/ls_test.py close-session` when finished. The launcher refuses to run until the deployed mod matches the working tree.
- RimWorld 1.6 is checked by compiling only: the installed game is 1.7.
- The default mod set is Harmony, Core and this mod. The unmapped `dlcs` set adds all five DLCs (`--mods dlcs`); no run has used it yet.

## The install lease

The game and `RimWorld/Mods/Loot Scavenging` are shared by every checkout and by the other mods' checkouts. `deploy.ps1` and every `ls_test.py run` claim the install lease; `close-session` releases it. Run `python tools/ls_test.py status` to see the holder, whether RimWorld is running and whether the installed mod matches this checkout. End every loop with `close-session`.

## Test tiers

| Tier | When | Runs |
| --- | --- | --- |
| Quick | Every code change, each ticket and fix-up | `dotnet test`, plus `lootscavenging-smoke` when the change touches game glue (hooks, recipes, study, unfamiliar) |
| Full | Seams: branch to main, a game or Harmony update, a release | `dotnet test`, `lootscavenging-smoke`, then `lootscavenging-smoke --mods dlcs` |

## Smoke batch (`lootscavenging-smoke`)

One fresh Crashlanded colony; the batch resets research to tribal first and raises it only upward. Each case calls the mod's own functions and the game's death, recipe and research paths directly, with no pawn-job waits.

| Case | Checks | Tickets |
| --- | --- | --- |
| `progression-defaults` | A fresh config holds the Progression preset and is not marked changed | 11 |
| `tribal-fixture` | Canary: colony era Neolithic, revolver research unfinished, pirates hostile, no scrap | - |
| `tribal-scrap` | A dead pirate's revolver becomes industrial scrap (`ScrapAmounts.Units`) beside the corpse; carried silver and steel drop untouched; a colonist's and a wild hare's deaths change nothing | 01, 03 |
| `cleanup-preset` | Poor weapon deleted, good weapon kept, corpse removed with its kept apparel on the ground | 01, 10, 11 |
| `recycling` | Machining table recycles three tiers, smithy no industrial, vanilla bills kept; 20 single units through the recipe's product step give pool materials | 04 |
| `study` | 5 medieval units pay 100 points at research factor 1; factor 0 and industrial scrap pay nothing | 05 |
| `keep-original` | An out-of-reach charge rifle is unfamiliar and not equippable until Charged Shot finishes | 06 |
| `no-mod-errors` | No error naming the mod during the batch | - |

Not covered: real bill jobs (hauling, the batch cap, work time), the downed and strip moments, scrap left on the body, save and load, the settings page, DLC content and a performance (`perf`) run.

## Result recording

### 2026-11-21 - lootscavenging-smoke

- Version/environment: RimWorld 1.7, default mod set (Harmony, Core, Loot Scavenging), seed 1.
- Observed: 8 of 8 cases passed, no errors logged. The revolver (market value 135) gave 1 industrial unit. Twenty single industrial units gave 166 silver of materials against an expected 220; none was a component or chemfuel, and 5 gave nothing.
- Evidence: run `20261121-094209-lootscavenging-smoke-s1-cd8ccd5d` (in the harness's `work/userdata/loot`).
- Status: Passed.
