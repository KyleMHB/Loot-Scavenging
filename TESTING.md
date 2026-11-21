# Loot Scavenging Testing

Updated through: `main` on 2026-11-21

## Agent read route

- Headings: `rg -n '^#' TESTING.md`.
- Writing cases or changing the harness: the [harness agent guide](../RimWorldDevTests/AGENTS.md).
- What to run for a change: [Test tiers](#test-tiers).

## Test prerequisites

- Unit tests: `dotnet test` from the repo root (`Tests/LootScavenging.Tests`, plain C#, no game needed).
- In-game batches: deploy with `powershell -ExecutionPolicy Bypass -File deploy.ps1`, build `dotnet build DevTests/LootScavenging.DevTests.csproj -c Release`, then `python tools/ls_test.py run run-all --seed 1 --timeout 3600` (or name batches), and `python tools/ls_test.py close-session` when finished. The launcher refuses to run until the deployed mod matches the working tree.
- RimWorld 1.6 is checked by compiling only: the installed game is 1.7.
- The default mod set is Harmony, Core and this mod. The unmapped `dlcs` set adds all five DLCs (`--mods dlcs`); the unmapped `perf` set adds Dubs Performance Analyzer for the shared `perf` batch (`run perf --mods perf`).

## The install lease

The game and `RimWorld/Mods/Loot Scavenging` are shared by every checkout and by the other mods' checkouts. `deploy.ps1` and every `ls_test.py run` claim the install lease; `close-session` releases it. Run `python tools/ls_test.py status` to see the holder, whether RimWorld is running and whether the installed mod matches this checkout. End every loop with `close-session`.

## Test tiers

| Tier | When | Runs |
| --- | --- | --- |
| Quick | Every code change, each ticket and fix-up | `dotnet test`, plus the batches for the changed area (table below) in one launch |
| Full | Seams: branch to main, a game or Harmony update, a release | `dotnet test`, `run run-all`, the same six batches with `--mods dlcs`, then `run perf --mods perf` |

| Area | Batches |
| --- | --- |
| `Source/Loot` (hooks, outcomes, corpses) | smoke, outcomes, moments |
| `Source/Recycling`, `Source/Study`, `BillWorkPatch.cs`, `Source/Unfamiliar` | smoke, bench, saves |
| Settings, presets, locks, lists, picker | smoke, settings |
| Saves, `CompUnfamiliar`, `StudyBenchSaveCompat` | saves |

The six batches take about 25 s of game time in one launch (2026-11-21); the run-all order is smoke, outcomes, moments, settings, bench, saves.

## Smoke batch (`lootscavenging-smoke`)

One fresh Crashlanded colony; the batch resets research to tribal first and raises it only upward. Each case calls the mod's own functions and the game's death, recipe and research paths directly, with no pawn-job waits.

| Case | Checks | Tickets |
| --- | --- | --- |
| `progression-defaults` | A fresh config holds the Progression preset and is not marked changed | 11 |
| `tribal-fixture` | Canary: colony era Neolithic, revolver research unfinished, pirates hostile, no scrap | - |
| `tribal-scrap` | A dead pirate's revolver becomes industrial scrap (`ScrapAmounts.Units`) beside the corpse; carried silver and steel drop untouched; a colonist's and a wild hare's deaths change nothing | 01, 03 |
| `tribal-medicine` | A pirate's industrial medicine drops whole by default while his revolver becomes scrap; with `rules.dropIncludesMedicine` on, a second pirate's medicine becomes industrial scrap | 16 |
| `cleanup-preset` | Poor weapon deleted, good weapon kept, corpse removed with its kept apparel on the ground | 01, 10, 11 |
| `recycling` | Machining table recycles three tiers, smithy no industrial, vanilla bills kept; 20 single units through the recipe's product step give pool materials | 04 |
| `study` | 5 medieval units pay 100 points at research factor 1; factor 0 and industrial scrap pay nothing | 05 |
| `keep-original` | An out-of-reach charge rifle is unfamiliar and not equippable until Charged Shot finishes | 06 |
| `no-mod-errors` | No error naming the mod during the batch | - |

## Other batches

- `lootscavenging-outcomes`: what an item becomes at death. Delete keeps apparel on the corpse, a mechanoid's weapon, era fallbacks and the components fallback, scrap amounts and the drop modes, a ten-pirate raid's piles, a quest-tagged weapon, animal corpse removal, degrade, item-based and era-typical disassembly, full stacks, ejected charges, a 200-unit recycling sample against `LootPool.Expected`, and the tier defs' config errors.
- `lootscavenging-moments`: downed and strip with the option on and off, a living prisoner's strip, scrap on the body (kept and with corpse removal), a world pawn's death, butchering with a real bill job, and home-map-only on a generated camp map.
- `lootscavenging-settings`: through the calls the page makes, not the page. Presets and "(changed)", Vanilla in game, the first-load flag, locks (the pack's lock names applied to `Presets.Configure` directly; the pack's patch is not loaded), keep and remove lists, a missing-def list entry through a settings save and load, and the picker's search.
- `lootscavenging-bench`: real bill jobs. Recycling and studied-scrap bills at a machining table, the study bench and its Bills tab, the study bill with no project, keep-X, work and XP, finishing a project, original items at the fabrication bench and research bench, and a techprint project (Royalty only).
- `lootscavenging-saves`: a looted-only marked rifle, scrap, studied scrap and a study bill through the runner's save and reload, and the save file's contents.

Not covered: the settings page and bill-tab UI, the first-load dialog on screen, loading a save without the mod (the runner ships inside the mod; Spearfishing uses a separate probe for this), a `MayRequire` pool entry (none exists), and pawn-job recycling at a smithy or crafting spot.

## Result recording

### 2026-11-21 - all batches, default and DLC sets, and perf

- Version/environment: RimWorld 1.7, seed 1. Default set: Harmony, Core, Loot Scavenging. `dlcs`: all five DLCs added. `perf`: Dubs Performance Analyzer 1.6.0.1 added.
- Runs, default: smoke `20261121-110125-lootscavenging-smoke-s1-40f5e757`, outcomes `20261121-110138-lootscavenging-outcomes-s1-60def0ae`, moments `20261121-105946-lootscavenging-moments-s1-1808d014`, settings `20261121-105948-lootscavenging-settings-s1-aa133488`, bench `20261121-110245-lootscavenging-bench-s1-bdf8e134`, saves `20261121-105952-lootscavenging-saves-s1-b42ecf36`.
- Runs, `dlcs`: smoke `20261121-110142-…-9e9418c9`, outcomes `20261121-110158-…-7e6877ca`, moments `20261121-110209-…-2162b7a2`, settings `20261121-110211-…-f0a3f586`, bench `20261121-110301-…-e19e732b`, saves `20261121-110216-…-dab575ef`. Perf `20261121-110350-perf-s1-3094df1a`.
- Observed: smoke, outcomes, moments, settings and saves passed on both sets. Bench failed on both:
  - The recycling bill on 12 industrial units hauled all 12 and used them in one iteration (expected 5, 5, 2 with at most 5 carried).
  - With no current project the researcher started the study bill (expected no job).
  - With Complex Clothing current and "keep 3" of 8, one iteration used all 8 for 160 points (expected 5 for 100), its work was the nominal 600 (expected 3000), Intellectual XP gained 0 and none was left (expected 3). The finishing-iteration case then had no scrap to study and is not observed.
  - One default run's original-item recycle of a 4-unit charge rifle returned 310 silver of materials (11 gold, 1 advanced component), more than 4 picks can give; the other runs gave 132 and 102.
- Recycling sample, 200 industrial units (expected value 2200): seeded 2287, unseeded 2161; components 24 and 15 (expected 17.2), chemfuel 105 and 108 (expected 95.7). 200 studied units: 1199 (ratio 0.52).
- Startup: every launch logs a NullReferenceException in `LootScavengingMod.ShowFirstLoadMessage` (the runner's error count starts after startup, so no run counted it).
- Perf: 0 ms/tick for the mod on a bare quickstart colony (460 methods patched, none called in 1800 ticks).
- Status: Partially passed.

### 2026-11-21 - lootscavenging-smoke with ticket 16's medicine case

- Version/environment: RimWorld 1.7, default mod set, seed 1.
- Observed: 9 of 9 cases passed, no errors logged. Industrial medicine read Industrial and out of reach at the tribal colony. By default 3 medicine dropped whole and the revolver gave 1 scrap unit; with the setting on, 3 medicine became 3 industrial scrap units.
- Evidence: run `20261121-102035-lootscavenging-smoke-s1-c93c7261`.
- Status: Passed.

### 2026-11-21 - lootscavenging-smoke

- Version/environment: RimWorld 1.7, default mod set (Harmony, Core, Loot Scavenging), seed 1.
- Observed: 8 of 8 cases passed, no errors logged. The revolver (market value 135) gave 1 industrial unit. Twenty single industrial units gave 166 silver of materials against an expected 220; none was a component or chemfuel, and 5 gave nothing.
- Evidence: run `20261121-094209-lootscavenging-smoke-s1-cd8ccd5d` (in the harness's `work/userdata/loot`).
- Status: Passed.
