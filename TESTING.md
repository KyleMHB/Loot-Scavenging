# Loot Scavenging Testing

Updated through: `main` on 2026-11-24

## Agent read route

- Headings: `rg -n '^#' TESTING.md`.
- Writing cases or changing the harness: the [harness agent guide](../RimWorldDevTests/AGENTS.md).
- What to run for a change: [Test tiers](#test-tiers).

## Test prerequisites

- Unit tests: `dotnet test` from the repo root (`Tests/LootScavenging.Tests`, plain C#, no game needed).
- In-game batches: deploy with `powershell -ExecutionPolicy Bypass -File deploy.ps1`, build `dotnet build DevTests/LootScavenging.DevTests.csproj -c Release`, then `python tools/ls_test.py run run-all --seed 1 --timeout 3600` (or name batches), and `python tools/ls_test.py close-session` when finished. The launcher refuses to run until the deployed mod matches the working tree.
- RimWorld 1.6 is checked by compiling only: the installed game is 1.7.
- A batch in another language: add `--language Russian` (or `ChineseSimplified`) to `run`. Short or full names work; without it every launch is English. A case that asserts English text can fail in another language; that is the case, not the translation.
- The default mod set is Harmony, Core and this mod. The unmapped `dlcs` set adds all five DLCs (`--mods dlcs`); the unmapped `perf` set adds Dubs Performance Analyzer for the shared `perf` batch (`run perf --mods perf`); the unmapped `progression` set adds Ideology, Vanilla Expanded Framework, VFE Tribals, Progression: Core, Node Research, Progression: Production, Research Papers and Semi Random Research: P-Fork Continued for `run lootscavenging-progression --mods progression`. On any other set that batch reports its cases as environment unavailable.
- Loading a save without the mod: run the saves batch, then `python tools/removal-check/removal_check.py` (newest archived saves-batch save by default) with the install lease taken (`python tools/ls_test.py lease take`, then `lease release`). It installs a developer-only probe mod for one launch with Harmony and Core only, writes PASS/FAIL lines to `work/removal-check/report.txt` and exits 1 on any FAIL. Adapted from Spearfishing's `tools/removal-check`.

## The install lease

The game and `RimWorld/Mods/Loot Scavenging` are shared by every checkout and by the other mods' checkouts. `deploy.ps1` and every `ls_test.py run` claim the install lease; `close-session` releases it. Run `python tools/ls_test.py status` to see the holder, whether RimWorld is running and whether the installed mod matches this checkout. End every loop with `close-session`.

## Test tiers

| Tier | When | Runs |
| --- | --- | --- |
| Quick | Every code change, each ticket and fix-up | `dotnet test`, plus the batches for the changed area (table below) in one launch |
| Full | Seams: branch to main, a game or Harmony update, a release | `dotnet test`, `run run-all`, `run run-all --mods dlcs`, `run lootscavenging-progression --mods progression`, the removal check, then `run perf --mods perf`. Release only: `python ../RimWorldTranslate/rwtranslate.py --mod . status` must show 0 untranslated and 0 outdated for ChineseSimplified and Russian (the build does not fail on stale text) |

| Area | Batches |
| --- | --- |
| `Source/Loot` (hooks, outcomes, corpses) | smoke, outcomes, moments |
| `Source/Recycling`, `Source/Study`, `BillWorkPatch.cs`, `Source/Unfamiliar` | smoke, bench, saves |
| Settings, presets, locks, lists, picker, `Source/SettingsUI` | smoke, settings, ui |
| `Source/Era`, study eligibility, research hooks | smoke, outcomes, bench, and progression `--mods progression` |
| `LootScavengingMod.cs` (startup) | startup |
| Saves, `CompUnfamiliar`, `StudyBenchSaveCompat`, new saved things or bills | saves, then the removal check |

Run-all's eight batches run in one launch; the order is startup, smoke, outcomes, moments, settings, bench, saves, ui. Startup goes first because the game's log keeps only the newest 1000 messages, and it reports not applicable when the log is already full.

## Smoke batch (`lootscavenging-smoke`)

One fresh Crashlanded colony; the batch resets research to tribal first and raises it only upward. Each case calls the mod's own functions and the game's death, recipe and research paths directly, with no pawn-job waits.

| Case | Checks | Tickets |
| --- | --- | --- |
| `progression-defaults` | A fresh config holds the Progression preset and is not marked changed | 11 |
| `tribal-fixture` | Canary: colony era Neolithic, revolver research unfinished, pirates hostile, no scrap | - |
| `tribal-scrap` | A dead pirate's revolver becomes industrial scrap (`ScrapAmounts.Units`) beside the corpse; carried silver and steel drop untouched; a colonist's and a wild hare's deaths change nothing | 01, 03 |
| `tribal-medicine` | A pirate's industrial medicine drops whole by default while his revolver becomes scrap; with `rules.dropIncludesMedicine` on, a second pirate's 3 medicine become industrial scrap, rounded once per stack (`ScrapAmounts.StackUnits`) | 16 |
| `cleanup-preset` | Poor weapon deleted, good weapon kept, corpse removed with its kept apparel on the ground | 01, 10, 11 |
| `recycling` | Machining table recycles three tiers, smithy no industrial, vanilla bills kept; 20 single units through the recipe's product step give pool materials | 04 |
| `study` | 5 medieval units pay 75 points at research factor 1; factor 0 and industrial scrap pay nothing | 05 |
| `keep-original` | An out-of-reach charge rifle is unfamiliar and not equippable until Charged Shot finishes | 06 |
| `no-mod-errors` | No error naming the mod during the batch | - |

## Other batches

- `lootscavenging-outcomes`: what an item becomes at death. Delete keeps apparel on the corpse, a mechanoid's weapon, era fallbacks and the components fallback, scrap amounts and the drop modes, a ten-pirate raid's piles, a quest-tagged weapon, animal corpse removal, degrade, item-based and era-typical disassembly, full stacks, ejected charges, `materials-stay-whole` (a modded recipe puts devilstrand behind unfinished spacer research, added at runtime with EraReader's cached recipe index cleared before and after: a pirate's 53 devilstrand, 30 steel and 20 plasteel drop whole with no spacer scrap while his revolver still becomes scrap), `tool-materials-stay-whole` (the same recipe also makes wood, which has melee `<tools>`: 50 wood must drop whole with no spacer scrap, since `EraReader.IsGear` requires a primary equipment type and excludes stuff, ingestibles, and stackable items with no weapon tags; it also checks that a thrumbo horn and an elephant tusk are not gear while frag grenades and a revolver are, and that a pirate's 2 thrumbo horns drop whole), `stack-scrap-by-value` (with the "other" rule on Scrap, 53 devilstrand and 50 cloth become `ScrapAmounts.StackUnits` of the colony's tier, far fewer than one per item, and a lone revolver still gives `ScrapAmounts.Units`), a 200-unit recycling sample against `LootPool.Expected`, and the tier defs' config errors, and ammunition and the drop mode (needs an ammo mod: environment unavailable on the default set; run it with `--mods compat-progression`).
- `lootscavenging-moments`: downed and strip with the option on and off, a living prisoner's strip, scrap on the body (kept and with corpse removal), everything on the body (scrap, ejected charges, inventory and disassembly products in the corpse; with corpse removal; a colonist's gear and a mechanoid's scrap under both on-body options dropping as usual; a downed pirate keeping his in-reach club and the rest in his pack, with downed handling on and off), a world pawn's death, butchering with a real bill job, and home-map-only on a generated camp map. Ticket 18's paths the moments above do not cover: `arrest-standing` (a standing pirate arrested through `SetGuestStatus(Prisoner)`: his out-of-reach revolver becomes scrap and his silver drops whole; with downed and strip off the revolver drops whole), `recruit-downed-raider` (a downed, captured pirate recruited through `RecruitUtility.Recruit`, and with Ideology one enslaved through `SetGuestStatus(Slave)`: his flak vest becomes scrap in his pack), `lost-manipulation` (both shoulders removed, not fresh: the standing pirate's revolver becomes scrap in his pack, none dropped whole) and `drop-calls-judged-once` (a Harmony prefix on `OutcomeApplier.Apply` counts outcomes per item: a club under a Degrade rule, with the drop on the ground, is judged once at death, at downing then death, at arrest and at a living strip).
- `lootscavenging-settings`: through the calls the page makes, not the page. Presets and "(changed)", Vanilla in game, locks (the pack's lock names applied to `Presets.Configure` directly; the pack's patch is not loaded), keep and remove lists, a missing-def list entry through a settings save and load, and the picker's search. Two cases go through `LoadedModManager.WriteModSettings`/`ReadModSettings`, reading under the compiled-in config as at startup and running the defaults step under a patched one. `imported-partial-file` reads a hand-written file (`activePreset` Cleanup, two setting keys and a locked drop mode, no `defaultsSeeded`), as an Auto Mod Config preset could leave it: the two keys are kept, a missing key takes the patched Def default, the locked key ends at the pack value and the seeded flag is set. `cleanup-round-trip` (tickets auto-config 01 and 18) saves a Cleanup config with the drop mode at the patched Progression's value, then reads it back and runs the defaults step under the compiled-in config, whose default drop mode differs: the file holds every setting key, the drop mode included, and every value matches what was saved.
- `lootscavenging-bench`: real bill jobs. Recycling and studied-scrap bills at a machining table, the study bench and its Bills tab, the study bill with no project, keep-X, work and XP, finishing a project, original items at the fabrication bench and research bench, and a techprint project (Royalty only).
- `lootscavenging-saves`: a looted-only marked rifle, scrap, studied scrap and a study bill through the runner's save and reload, and the save file's contents.
- `lootscavenging-ui`: the settings window opened through the window stack, and the machining table's and research bench's Bills tabs through the inspect pane, each drawn for a few frames while a `Widgets.Label` postfix records what was drawn and where. Checks the six sections, the preset line and buttons, one column top to bottom after each preset switch (the collapse fixed in `e7efc62`), the locked summary and a locked row's reason, both bills in their tabs, the study bench's add-bill options. It draws and reads; it does not click. `translated-text` also draws the page with a weapons rule that has every condition and a chance, draws the item picker, and builds the bill labels and descriptions, a scrap stack's inspect text, an unfamiliar item's label and the study-done message for a male and a female pawn. It fails on a raw `LootScavenging_` key, an unfilled `{` slot, or an English fragment (10 or more characters between slots) from a key whose translation differs, read from the English Keyed file. It observes each built text and any label taller than its rect. Under `--record` each window is held 1.5 s so `--frames` stills show it; run it with `--record --show-ui`.
- `lootscavenging-startup`: the whole in-memory log since startup has no error naming the mod (the runner's own count starts at the first batch).
- `lootscavenging-progression` (`--mods progression`, outside run-all): the colony's era after Node Research's medieval emergence, study of medieval scrap for a medieval project, a paper from a project finished by study, a lost paper's retained progress and a second finish, Semi Random Research's picked offer, and every tier's recycling bench and pool material in the pack set. The pack mods are reached by reflection.
- `lootscavenging-preview-story`: a showcase, outside run-all, that films the stills for `About/Preview.png` with the game paused. A downed pirate is shown with his rifle, vest and helmet on the ground (downed handling off), then with scrap in their place, then dead with "everything stays on the body" and nothing on the ground. A workshop shows scrap at a research bench and a machining table. It also films a story scene and a raid aftermath. The case fails if a raider leaves no scrap, or if scrap lands on the ground under everything on the body. Run it with `--record --frames 1 --quit-on-finish`, then `python tools/preview_story.py <run folder>`. That writes the candidates to `work/preview`; `split.png` is the one copied to `About/Preview.png`. Text uses RimWordFont from `Configurable Quality/artifacts/steam-page-mockup`.
- `lootscavenging-workshop-clips`: a showcase, outside run-all, that films the Workshop description GIFs and gallery stills (`.scratch/workshop-media`) on the Progression preset at a tribal colony, at normal speed. It films:
  - a pirate and a tribal warrior attacking a sandbag line, where the pirate's autopistol becomes industrial scrap and the club drops whole;
  - one recycle bill at a crafting spot and one study bill at a research bench;
  - a pirate who dies with everything on the body, his corpse selected with its Gear tab open, then stripped by a colonist who steps away from the loot;
  - paused stills of a tribal warrior (club, autopistol in his pack) killed under each of the four drop modes;
  - paused stills of neolithic and spacer scrap selected.

  The case fails if a beat does not happen. Run it with `--record --show-ui --frames 12 --quit-on-finish`. It writes `shots.json` (marks for cutting) and `layout.json` (each shot's camera) to the run folder.
- `lootscavenging-promo-clips`: a showcase, outside run-all, that films the promo video's live footage in place of its gallery cards, at normal speed on the Progression preset. A tribal warrior (club, autopistol in his pack) dies on camera under each of the four drop modes, 16 cells apart; under keep-original the autopistol is then selected so its pane reads "unfamiliar". The settings window then switches through Progression, Cleanup and Vanilla. The case fails if a death, the unfamiliar selection or a preset is missing. Run it with `--record --show-ui --frames 12 --quit-on-finish`, set the run id as `PROMO_RUN` in `tools/render_promo.py`, then run that script.
- `lootscavenging-preview`: the earlier showcase of the six scrap piles in front of their benches, filmed for the first preview image (`tools/preview_image.py`). No longer used for `About/Preview.png`.
- `lootscavenging-stacks`: a scene outside run-all for looking at the scrap art by hand. Each era gets piles of 1, 10 and 500 (the small, mid and full textures) and a full studied stack. Run it with `--keep-open`.

Not covered: clicking in the settings page and bill tabs (the ui batch only draws them), a played raid with the Progression pack, Non uno Pinata, Progression: Ammunition and Progression: Hospitality (ticket 14), a `MayRequire` pool entry (none exists), and pawn-job recycling at a smithy or crafting spot.

## Result recording

### 2026-11-24 - Full tier on the 0.5.2 release candidate

- Change: everything for 0.5.2 together: the materials and horn rules, the review fixes (remove list before the drop mode, protected items never unfamiliar, locked apparel at death, judged once after an on-body downing, former colonists, patching class by class, with four new moments cases), the scrap value cap, and ticket 18 (entries below). Tested `8117a64`.
- Version/environment: RimWorld 1.7.5014, seed 1, English. `deploy.ps1` and the DevTests build first, the display woken with a mouse nudge before each run-all.
- Runs:
  - `dotnet test`: 331 passed. 1.7, 1.6 and DevTests builds clean. `rwtranslate status`: 0 untranslated, 0 outdated for ChineseSimplified and Russian.
  - Run-all default `20261124-123313-run-all-s1-21f64a7d`: 81 pass, 0 fail, 2 skip (ammunition needs an ammo mod; techprints need Royalty).
  - Run-all `dlcs` `20261124-123346-run-all-s1-2d21a7be`: 82 pass, 0 fail, 1 skip (ammunition).
  - Progression `20261124-123428-lootscavenging-progression-s1-38cfc98e`: 6/6. Perf `20261124-123446-perf-s1-6e313f3a`: passed.
  - Removal check on the default run-all's saves-batch save: exit 0, all PASS, only the expected missing-def errors.
  - The review fixes alone (`054260b`) passed the same tier earlier: run-all default `20261124-120158-run-all-s1-fcb0249e` 77 pass, 2 skip; `dlcs` `20261124-120219-run-all-s1-db0d2070` 78 pass, 1 skip; progression `20261124-120304-lootscavenging-progression-s1-c82d37ed` 6/6; perf `20261124-120323-perf-s1-3c886699`; removal check all PASS. The new moments cases' first run (`20261124-115836-lootscavenging-moments-s1-1cca2793`) failed `former-colonist-keeps-gear` because the case could not add to a time record, so the pawn had no colonist time and his revolver was scrapped, as before the fix. With the record advanced through the tracker's update the batch passed 18/18 (`20261124-115929-lootscavenging-moments-s1-ab6b58f0`). That run also showed patching every class made Harmony run `Presets.Cleanup` as a patch hook; only classes with `[HarmonyPatch]` are processed now.
- Status: Passed. Not run in game: the trade path (same hook as arrest), the zero-unit scrap path (unit-tested), and the ammunition case's new worth check (needs an ammo mod).

### 2026-11-24 - Unhooked drops and saved defaults (ticket 18)

- Change: new hooks on `Pawn.DropAndForbidEverything` (arrest, trade), `Pawn_EquipmentTracker.TryDropEquipment` (lost manipulation), `RecruitUtility.Recruit` and `Pawn_GuestTracker.SetGuestStatus(Slave)` (a prisoner joining keeps no out-of-reach apparel), with a per-pawn mark from the Kill, MakeDowned, Strip and DropAndForbidEverything prefixes to their finalizers so no item is judged twice. Settings save every key (`forceSave`). Four new moments cases; `settings.cleanup-round-trip` now expects the key equal to the default in the file and a changed default not to move it.
- Version/environment: RimWorld 1.7, seed 1, English, the branch rebased on `bcbe8ea`. `deploy.ps1` first.
- Ran: `dotnet test` 331 passed; 1.7, 1.6 and DevTests builds clean. Moments `20261124-122500-lootscavenging-moments-s1-9ecb1ef6` 22/22, smoke `20261124-122514-lootscavenging-smoke-s1-38bb1265` 9/9, outcomes `20261124-122516-lootscavenging-outcomes-s1-e70034b5` 18 pass, 1 skip (ammunition needs an ammo mod), settings `20261124-122519-lootscavenging-settings-s1-39adeefb` 9/9, saves `20261124-122520-lootscavenging-saves-s1-a5df1470` 3/3. Moments `--mods dlcs` `20261124-122535-lootscavenging-moments-s1-49dd45da` 22/22, with enslaving.
- Observed: arrest gave 1 industrial scrap and 30 silver whole (off: revolver on the ground, no scrap); recruiting and enslaving each turned the vest into 2 industrial scrap in the pack; lost manipulation left no weapon in hand, 1 scrap in the pack and no revolver on the ground; the club was judged once in all four ways and degraded (14 to 34 of 100 hit points).
- Not run: the Full tier, and trade (`PreTraded`), which goes through the same `DropAndForbidEverything` hook as arrest.
- Status: Passed.

### 2026-11-24 - Scrap never worth more than its source (0.5.2)

- Change: `ScrapAmounts.CapUnits`/`CappedUnits`/`CappedStackUnits` and `WorthPerUnit` cap scrap units at the item's or stack's value; `ItemClassifier.ScrapUnits` takes the tier. 0 units makes no scrap. Harness expectations now come from `Fixtures.ExpectedScrap*` (the capped model functions), and `stack-scrap-by-value` and the ammunition case assert scrap worth is at most the stack's value.
- Ran: `dotnet test` 329 passed; both Release builds and the DevTests build clean. Deployed, then outcomes `20261124-121128-lootscavenging-outcomes-s1-e4b02cfb` (18 pass, 0 fail, 1 skip: ammunition needs an ammo mod, so the run reports NOT PASSED for coverage), smoke `20261124-121140-lootscavenging-smoke-s1-6f8319bb` 9/9, moments `20261124-121143-lootscavenging-moments-s1-80c2ea96` 18/18. Full tier not run. The ammunition case's new assertion and the 0-unit path are unit-tested only, not run in game.

### 2026-11-24 - Materials stay whole, stacks scrap by value (0.5.2)

- Change: three new outcomes cases and two case fixes. Tested the 0.5.2 code (`2232e6f`); the commits after it touch only `.scratch/`.
  - `materials-stay-whole`: at the tribal fixture a RecipeDef producing DevilstrandCloth behind the unfinished spacer project Charged Shot is added at runtime and `EraReader`'s cached `recipeIndex` is cleared by reflection, so it rebuilds; both are undone afterwards. A pirate carries 53 devilstrand, 30 steel, 20 plasteel and a revolver under the default Progression preset.
  - `tool-materials-stay-whole`: the same recipe also makes wood, and a pirate carries 50 wood. It first failed as a known gap (below), was skipped as `known-gap`, and is a normal step now that the wood fix is in. After the horn rule it also checks `IsGear` for ThrumboHorn, ElephantTusk, Weapon_GrenadeFrag and the revolver, and a pirate carrying 2 thrumbo horns.
  - `stack-scrap-by-value`: with the "other" inventory rule on Scrap, 53 devilstrand and 50 cloth, and a lone revolver.
  - Existing cases that 0.5.2 had broken: the ammunition case no longer compiled (`ScrapAmounts.AmmoUnits` was renamed `StackUnits`), and smoke `tribal-medicine` expected per-item rounding (3 medicine now give 2 scrap, not 3).
- Version/environment: RimWorld 1.7.5014, seed 1, English. `deploy.ps1` first. Sets: default, `dlcs`, `progression`, `perf`.
- Observed:
  - `materials-stay-whole` passed. The rebuilt index tied devilstrand to Charged Shot, so before 0.5.2 it would have been spacer-era; now it reads Undefined/Unknown and not out of reach. The 53 devilstrand, 30 steel and 20 plasteel dropped whole, no spacer scrap, and the revolver gave 1 industrial scrap (`ScrapAmounts.Units`).
  - `stack-scrap-by-value` passed. 53 devilstrand (5.5 each) gave 16 neolithic scrap and 50 cloth (1.5 each) gave 8, both the colony's tier and equal to `ScrapAmounts.StackUnits`, against 53 and 50 under the old per-item rule; the revolver gave 1.
  - Wood bug, found by the case and fixed: wood gave 7 spacer scrap and dropped none. Core gives `WoodLog` melee `<tools>`, so `EraReader.IsGear` counted it as gear and the 0.5.2 check (`def.IsStuff && !IsGear(def)`) skipped it; behind the same recipe it took the recipe's era (Spacer/Research). In vanilla, wood has no such recipe, so it only shows with a mod that adds one. The fix (uncommitted, `Source/Era/EraReader.cs`): `IsGear` now requires equipmentType Primary, not ingestible, not stuff, so wood and beer (also weapons only by melee tools) are not gear. After it, 50 wood dropped whole with no spacer scrap.
  - Horn rule: thrumbo horns, elephant and mastodon tusks and modded trophies share wood's parent `ResourceVerbBase` (equipmentType Primary, melee tools), so after the wood fix they still counted as gear. `IsRealWeapon` now also excludes items that stack and have no `weaponTags`. A scan of 1,559 Workshop mods' defs found 37 such trophies and 25 stackable weapons that all carry weapon tags (CE grenades and pila, Mechanoid Upgrades grenades and others), which stay gear. In the case, the horn and tusk were not gear, the grenade and revolver were, and the 2 horns dropped whole.
- Runs:
  - Quick: outcomes cases only `20261124-104405-lootscavenging-outcomes-s1-cases-233651ba` (2/2, before the wood addition); smoke `20261124-104500-lootscavenging-smoke-s1-ff138535` 9/9 after the medicine fix (the first run, `20261124-104430-lootscavenging-smoke-s1-53308e9f`, failed `tribal-medicine` with 2 scrap against 3 expected); outcomes `20261124-104433-lootscavenging-outcomes-s1-4e439080` 17 pass, 1 skip (ammunition needs an ammo mod); moments `20261124-104435-lootscavenging-moments-s1-a1a92215` pass; bench `20261124-104437-lootscavenging-bench-s1-e80efd2e` 9 pass, 1 skip (techprints need Royalty); progression `20261124-104513-lootscavenging-progression-s1-7e19e720` 6/6.
  - Wood case: `20261124-105830-lootscavenging-outcomes-s1-cases-2c3e9877` (known gap), then `20261124-105923-lootscavenging-outcomes-s1-cases-5ee9744d` with the case split in two (2 pass, 1 known-gap skip).
  - Rerun after the wood fix (deployed and DevTests rebuilt first): outcomes `20261124-110820-lootscavenging-outcomes-s1-cce0877f` 18 pass, 1 skip (ammunition needs an ammo mod); smoke `20261124-110833-lootscavenging-smoke-s1-b9689528` 9/9; moments `20261124-110836-lootscavenging-moments-s1-d8653405` 14/14. `tool-materials-stay-whole` passed.
  - Full tier: `dotnet test` 291 passed after the wood fix. Rerun after the wood fix: progression `20261124-111033-lootscavenging-progression-s1-957d8cf7` 6/6; perf `20261124-111052-perf-s1-be09a096` passed; removal check on the newest default run-all save (`20261124-110844-run-all-s1-9a8e33fd`): exit 0, all PASS, only the expected missing-def errors. Run-all default `20261124-110844-run-all-s1-9a8e33fd`: 73 pass, 0 fail, 2 skip (ammunition needs an ammo mod; techprints need Royalty). Run-all `dlcs` `20261124-110905-run-all-s1-6f393354`: 74 pass, 0 fail, 1 skip (ammunition). The ui batch's six cases pass in both. Progression `20261124-110046-lootscavenging-progression-s1-3359f7de` 6/6, removal check (default run `20261124-105944-run-all-s1-74a7b8c3`'s saves-batch save: exit 0, all PASS, the same 14 expected missing-def errors as before) and perf `20261124-110140-perf-s1-e746fa71` (0 ms/tick over 1800 ticks) were run earlier in the day, before the wood fix; the fix touches only the era check. `rwtranslate status`: 0 untranslated, 0 outdated for ChineseSimplified and Russian.
  - Full tier after the horn rule (deployed and DevTests rebuilt first): `dotnet test` 291 passed; run-all default `20261124-112000-run-all-s1-08f95768` 73 pass, 0 fail, 2 skip (ammunition; techprints); run-all `dlcs` `20261124-112033-run-all-s1-c150485d` 74 pass, 0 fail, 1 skip (ammunition), ui six cases passing in both; progression `20261124-112149-lootscavenging-progression-s1-5751aeba` 6/6; perf `20261124-112207-perf-s1-ee396e43` passed; removal check on the default run-all's saves-batch save: exit 0, all PASS, only the expected missing-def errors.
- Ui batch failures earlier the same day, explained: the five drawing cases (`settings-page-draws`, `settings-after-preset-switch`, `settings-locked-rows`, `bills-tabs`, `translated-text`) failed with `labelsDrawn` 0 from 10:46 on (ten ui runs, two run-alls) while frames advanced. The cause was the environment, not the mod. The game window was not minimized (not iconic, visible, 2560x1440) and Player.log has no "Rendering resumed" or "Focus regained" line, but the desktop had had no user input for about 48 minutes (last input about 10:21, `GetLastInputInfo`), so the display was presumably asleep and nothing was presented, and `OnGUI`, hence `Widgets.Label`, did not run. Evidence: the 0.5.1 code (`7ca30e0`, built in a throwaway sibling checkout, deployed, then removed), whose ui batch passed 6/6 on 2026-11-23, failed the same five cases at 11:07 (`20261124-110719-lootscavenging-ui-s1-256a4a06`); after one 1-pixel mouse nudge (no power or lock setting changed) it passed 6/6 (`20261124-110757-lootscavenging-ui-s1-343aed43`), and the current code then passed ui inside both run-alls. The display's sleep state itself was not read directly. Run the ui batch, and any batch asserting what was drawn, with the display awake; a mouse nudge before the run is enough.
- Status: Passed. Skips are documented: ammunition needs an ammo mod (run it with `--mods compat-progression`), and techprints need Royalty.

### 2026-11-23 - Era-typical disassembly case no longer fails by chance

- Change: `outcomes.disassembly-era-typical` failed now and then because one revolver is one or two industrial units, and a unit whose pick costs more than the unit (a component) returns nothing with probability 1 - value/price. The pirate now also carries 20 revolvers, with its pack emptied first and the "other" inventory rule set to Disassemble, because carried weapons fall under that rule. The case computes the empty-yield chance from the industrial pool (0.164 per unit, 3.3e-17 for 21 revolvers) and requires it below 1e-9. It also requires that no revolver is left whole, and still that every product comes from the industrial pool and none from another tier's pool. Test-only; no mod code changed.
- Version/environment: RimWorld 1.7, default mod set, English, seed 1. `deploy.ps1` first.
- Runs: outcomes `20261123-212546-lootscavenging-outcomes-s1-9f3e0a68` 15 pass, 1 skip (ammunition needs an ammo mod). Products: 18 wood, 10 leather, 8 cloth, 30 steel, 3 components, 15 chemfuel; 0 revolvers left.
- Status: Passed.

### 2026-11-23 - Downed weapon into the pack (0.5.1)

- Version/environment: RimWorld 1.7, default mod set, English, seed 1. `deploy.ps1` first; `dotnet test` 291 passed.
- Runs: smoke `20261123-211911-lootscavenging-smoke-s1-e541193e` 9/9; moments `20261123-211936-lootscavenging-moments-s1-5e23f716` 14/14; ui `20261123-211938-lootscavenging-ui-s1-3c9c742b` 6/6; outcomes `20261123-211933-lootscavenging-outcomes-s1-168635bf` failed `disassembly-era-typical` (no products), and the rerun `20261123-212005-lootscavenging-outcomes-s1-7ef584c3` passed (15 pass, 1 skip: ammunition needs an ammo mod).
- Observed: `all-on-body-downed` found the club in the pirate's pack, not his hands, with downed handling on and off. After `DeSpawn` and `GenSpawn.Spawn` (the path a reload or a carry takes, where `Pawn_EquipmentTracker.Notify_PawnSpawned` drops a downed pawn's held weapon) nothing landed on the ground.
- The era-typical failure is chance, not this change: a revolver is 1 industrial unit, and when the pool pick is worth more than that unit, `LootPool.Amount` rounds up to one item only with probability value/price, so the yield can be empty.
- Status: Passed.

### 2026-11-23 - Everything on the body when downed; ammunition and the drop mode

- Version/environment: RimWorld 1.7, seed 1, English, the working tree after `8a50293` with the two new cases. `deploy.ps1` first.
- Runs, default set: smoke passed; moments `20261123-191508-lootscavenging-moments-s1-3074b5bf` 14/14; outcomes `20261123-191505-lootscavenging-outcomes-s1-f4eb2e5b` 15 pass, 1 skip (the ammunition case needs an ammo mod).
- Runs, `--mods compat-progression`: outcomes `20261123-191529-lootscavenging-outcomes-s1-20c9c44e` 16/16. Moments stopped at setup again ("no clear 80x14 area near the map centre"), as on 2026-11-21.
- Observed:
  - `all-on-body-downed`: with downed handling on, the pirate kept his club in hand, his carried revolver became 1 industrial scrap in his pack, his 50 silver stayed in his pack, and nothing landed on the ground after 60 ticks. With downed handling off, the club stayed in hand and the revolver stayed whole in his pack.
  - `ammunition-drop-mode`: Progression: Ammunition loads `PA_ArrowRefill`, `PA_AmmoRefill` and `PA_ChargeRefill`; the case used `PA_AmmoRefill` (industrial). By default 25 refills dropped whole with no scrap. With `rules.dropIncludesAmmunition` on, 25 became 18 industrial scrap, rounded once per stack (per-item rounding would give 25).
- Status: Passed.

### 2026-11-23 - Promo clips filmed

- Version/environment: RimWorld 1.7, default mod set, English, seed 1. `deploy.ps1` first, then `python tools/ls_test.py run lootscavenging-promo-clips --seed 1 --record --show-ui --frames 12 --quit-on-finish`.
- Runs: the first two did not film: FFmpeg captured no frame within 20 s and gave up without retrying. The harness on Templates `main` now stops and retries such an attempt. `20261123-004719-lootscavenging-promo-clips-s1` passed 1 of 1, each drop mode on its own spot: out-of-reach left the club and 1 industrial scrap; everything left 2 neolithic and 1 industrial scrap; keep-original and off left the club and autopistol.
- `tools/render_promo.py` renders a 61.7 s promo with no still images: the drop-mode and settings sections use this run, and the title and end text sit over blurred footage. The scrap-tiers card was dropped.

### 2026-11-23 - On-body clip refilmed (workshop-media ticket 02)

- Version/environment: RimWorld 1.7, default mod set, English, seed 1. `deploy.ps1` first, then `python tools/ls_test.py run lootscavenging-workshop-clips --seed 1 --record --show-ui --frames 12 --quit-on-finish`.
- Runs: `20261123-000121-lootscavenging-workshop-clips-s1` did not film: FFmpeg captured no frame within 20 s on the first attempt, and the batch ended early. The rerun `20261123-000545-lootscavenging-workshop-clips-s1` passed 1 of 1 with every beat, including the new gear-tab check.
- Observed: the on-body beat now opens the corpse's Gear tab after the death (mark `on-body-gear`). The tab listed cloth tribalwear, 3 industrial scrap, 3 medicine and 40 silver; the autopistol and flak vest were already scrap, so no weapon shows. The stripper, spawned drafted 2 cells east after the death, stripped the corpse 1.5 s after the order, then walked a few cells off. Tribalwear, scrap, medicine and silver lay around the corpse. Only `loot-scavenging-on-body.gif` uses this run.
- Status: Passed.

### 2026-11-22 - Settings still for the Workshop gallery (workshop-media ticket 05)

- Version/environment: RimWorld 1.7, default mod set, English, seed 1. `deploy.ps1` first, then `python tools/ls_test.py run lootscavenging-ui --seed 1 --record --show-ui --frames 40 --quit-on-finish`.
- Run: `20261122-234344-lootscavenging-ui-s1`, 7 of 7 passed. Under `--record` the ui batch's first case now sets the UI scale to 1.5 and stretches the settings window to the screen's height for 4 s, then restores both, so one frame shows the preset line and the first item rules with their sentences. `tools/workshop_gallery.py settings` cuts `work/gallery/settings-frame.png` (the video at 5.5 s) into the gallery image.
- Status: Passed.

### 2026-11-22 - Full tier for 0.3.0 (translations ticket 09)

- Version/environment: RimWorld 1.7.5014 rev1195, seed 1, at `e1ca95b`, shipping DLL (tickets 01 and 02 changed it). `perf` set: Dubs Performance Analyzer added; `dlcs`: all five DLCs added.
- Runs, all passed: `dotnet test` 291 passed; run-all default `20261122-024556-run-all-s1-6fc117ae` (68 pass, 1 skip: techprints need Royalty); run-all `--mods dlcs` `20261122-024639-run-all-s1-e11e73e5` (69 pass); progression `20261122-024715-lootscavenging-progression-s1-bdeb446d` (6/6); perf `20261122-070157-perf-s1-19c265ae` (0 ms/tick).
- Removal check on the default run's save (`lootscavenging-saves-0.rws` of `20261122-024556-run-all-s1-6fc117ae`): exit 0, all PASS, the same 14 expected missing-def errors as 2026-11-21. An earlier attempt timed out because it picked the DLC run's save; the script now skips DLC saves (no DLL change).
- Release-check passed on the deployed DLL: no developer runner, patch roots and preview size, perf.mod-tick 0 ms/tick in the run above. Usual warning: no targeted `perf.*` cases. `rwtranslate status` showed 0 untranslated and 0 outdated for ChineseSimplified and Russian.
- Status: Passed.

### 2026-11-22 - In-game language check, Simplified Chinese and Russian (translations ticket 08)

- Version/environment: RimWorld 1.7.5014 rev1195, default mod set (Harmony, Core, Loot Scavenging), seed 1. Each language: firstload, smoke, bench and ui in one launch, then ui alone with `--record --show-ui --frames 30`.
- First runs, all passed: ChineseSimplified firstload `20261122-022602-…-c852cba1`, smoke `20261122-022615-…-2475a16a`, bench `20261122-022617-…-b72448b6`, ui `20261122-022621-…-afd5123a`, recorded ui `20261122-022650-lootscavenging-ui-s1`; Russian firstload `20261122-022800-…-ad1ca6ee`, smoke `20261122-022812-…-a533acf3`, bench `20261122-022814-…-43dadb8f`, ui `20261122-022818-…-08f667be`, recorded ui `20261122-022849-lootscavenging-ui-s1`.
- Found and fixed, each through `baseline`; `status` shows 0 untranslated and 0 outdated for both languages, and `check` passed:
  - Russian, capital after a colon. The game's `Formatted` (`GenText.CapitalizeSentences`, 1.7.5014 decompile) upper-cases the first letter after any `:`, even after a no-break space. Every rule sentence on the settings page read "Оружие: Предметы с затронутых персонажей …". The study message read "завершил изучение: Средневековый лом", and the inspect reasons read "Пока нельзя изучить: Поселению нужен …".
    - The 17 `RuleKeep`/`RuleAct*` keys now read "Предметы категории «{0}» с затронутых персонажей …", or "С шансом {4} предметы категории «{0}» …".
    - `StudyDone`, `StudyNow` and five `StudyWhy*` reasons use a dash; `StudyWhyProjectEra` uses "так как".
    - `StudyKeepInStock` reads "{0} в запасе".
  - ChineseSimplified: the first-load text broke after "Loot", leaving it alone on the first line. Both "Loot Scavenging" in `FirstLoadText` now use a no-break space, and the still shows the name on one line.
- Final runs, all passed with no logged or mod errors:
  - Russian firstload `20261122-023902-…-8285dadb`, smoke `20261122-023913-…-0b537103` (9/9), bench `20261122-023916-…-9c2fc65b` (9 pass, 1 skip: techprints need Royalty), ui `20261122-023919-…-9e9fe335` (7/7), recorded ui `20261122-023923-lootscavenging-ui-s1`.
  - ChineseSimplified firstload `20261122-023406-…-e6c85a80`, smoke `20261122-023417-…-46aaa5fa`, bench `20261122-023420-…-ba4ed8c3`, ui `20261122-024002-…-c1f17e1f` (7/7, with the rule sentence added), recorded ui `20261122-023427-lootscavenging-ui-s1`.
  - English: firstload `20261122-024030-…-a28ba5bf`, smoke `20261122-024041-…-34787256`, settings `20261122-024043-…-1720baa2`, ui `20261122-024045-…-de3fd830`, bench `20261122-024048-…-46eb9f79`.
- Observed:
  - `translated-text` found no raw key, unfilled slot or English fragment in about 9,000 drawn and built texts per language (240 English fragments checked).
  - The all-conditions rule sentence, Russian: "С шансом 50% предметы категории «Оружие» с затронутых персонажей превращаются в лом своей эпохи, если они вне досягаемости поселения и их качество ниже «нормально» или прочность ниже 50%. В остальных случаях они выпадают как обычно." Chinese: "来自受影响角色的武器在超出殖民地能力范围，且品质低于一般或耐久度低于50%时有50%的几率变为其时代的废料，否则照常掉落。"
  - Russian study-done: "… завершил изучение — средневековый лом (5 шт.). Очки исследования для «Выращивание деревьев»: +100." and "… завершила …".
  - In Chinese and Russian, no mod text was taller than its rect; only the runner's own progress panel was. In English one row was: the unfamiliar-scope note "Any gear beyond the colony's research is unfamiliar … keeps the original item." measured taller than its 824x18 rect. English layout is left as it is.
  - Stills show the following without overlap or clipping in either language: the settings page under each preset in one column, the preset buttons ("Восстановить по умолчанию" fits), the locked summary, both Bills tabs, the first-load box and the item picker.
- Gaps:
  - Stills show only the top of the scrolled settings page. Lower rows, the rule sentences included, were checked as recorded text and by the taller-than-rect observation, not by eye.
  - The locked row's reason comes from the ui batch's direct lock under the Progression preset. No mod set loads the pack's preset patch, so `--mods progression` was not run.
  - English sentences with a colon get the same engine capital ("Cannot be studied yet: The colony …"). English is not changed here.
  - The Chinese unfamiliar label keeps vanilla's ASCII brackets around the quality: "电荷步枪 (一般)（陌生）".
- Status: Passed.

### 2026-11-22 - Imported settings files (auto-config ticket 01)

- Version/environment: RimWorld 1.7.5014 rev1195, default mod set, seed 1; `dotnet test` 291 passed.
- Runs, one launch: firstload `20261122-014550-lootscavenging-firstload-s1-f54ad1a8`, smoke `20261122-014602-lootscavenging-smoke-s1-6e34987e`, settings `20261122-014604-lootscavenging-settings-s1-321efb97` (10/10), ui `20261122-014606-lootscavenging-ui-s1-1adc1563` (6/6).
- Observed: the partial file kept Cleanup, corpse chance 0.25 and all-on-body, filled the scrap value from the patched default (0.5), ended the locked drop mode at the pack value and set the seeded flag. The Cleanup round trip's file had no `<dropMode>` (equal to its default) and read back unchanged.
- Status: Passed.

### 2026-11-21 - Workshop clips and stills

- Version/environment: RimWorld 1.7, default mod set, seed 1.
- Run: workshop-clips `20261121-223036-lootscavenging-workshop-clips-s1` (`--record --show-ui --frames 12`), passed.
- Observed:
  - The pirate's autopistol became industrial scrap and the club dropped whole. The pirate was downed and the warrior killed by developer at their 5 and 7 second deadlines.
  - The recycle and study bills finished at normal speed; study paid 60 points into Recurve Bow.
  - Nothing landed at the on-body death, and the strip dropped the loot.
  - All four drop-mode stills and both inspect stills were filmed. Mark times are in ticket 01 of `.scratch/workshop-media`.
- Status: Passed.

### 2026-11-21 - Workshop preview image

- Version/environment: RimWorld 1.7, default mod set, seed 1.
- Run: preview-story `20261121-184244-lootscavenging-preview-story-s1` (`--record --frames 1`), passed: every raider left scrap, and none landed on the ground when the pirate died with everything on the body.
- Observed: `About/Preview.png` (1280x720, 734 KB) composed from that run's drop and workshop shots. Chosen by Kyle from the story, aftermath, panels and split layouts.
- Status: Passed.

### 2026-11-21 - Progression research mods, UI, first load and mod removal

- Version/environment: RimWorld 1.7, seed 1; `dotnet test` 281 passed. Progression: Core, Node Research and VFE Tribals load their 1.6 folders on 1.7, as does Research Papers' 1.6 assembly.
- Before the fix: progression `20261121-164448-lootscavenging-progression-s1-948320f2` failed the era case. After Node Research's medieval emergence the faction read Medieval and the colony era Neolithic, and medieval scrap for Complex Furniture was refused as above the colony's era. The Semi Random Research case failed on its own setup: the tracker's `SetCurrentProject` rejects a pick it is not applying, so the case now picks through `SetCurrentProjectByKey`, as the research window does.
- After the fix: progression `20261121-164613-lootscavenging-progression-s1-06b61c2f` and `20261121-165226-lootscavenging-progression-s1-1aca51e4` (6/6). The era went Neolithic, then Medieval after the advance, and medieval scrap was eligible. Two medieval units finished Complex Furniture and left a paper 1.4 cells from the researcher. Destroying it left 225 of 300 (75%), and two more units finished it again with a new paper. One neolithic unit paid 10 to the picked offer VFET_Tribalwear and nothing to VFET_Agriculture.
- First UI run (`20261121-164638-run-all-s1-4bdeaca1`) failed two steps:
  - A locked drop mode row drew no reason. Choice rows and sliders now draw it, as toggles did.
  - The add-bill check left out "study original items" (`LS_StudyOriginal`); the case filter now includes it.
- First load: the first two runs passed on a settings-file write time, which also changes for other reasons. The batch now reads the flag while mods are constructed: it was unset at each launch, startup wrote it, and no error named the mod.
- Removal check on saves `20261121-164805-lootscavenging-saves-s1-8344f069`: all PASS. The research bench loaded as a plain research bench; the machining table kept Make_ComponentIndustrial and lost its recycle bill; scrap and studied scrap were gone; the looted charge rifle stayed and could be equipped. The load logged 14 errors, all for the missing scrap things, the scrap def and the recycle recipe.
- Full tier: run-all default `20261121-165127-run-all-s1-99cc4ad1` (65 pass, 1 skip: techprints need Royalty). Run-all `dlcs` `20261121-165153-run-all-s1-879218f5`: 65 pass, 1 fail. `outcomes.disassembly-era-typical` got no products, because a one-unit revolver's single pick can return nothing; that set does not load the changed era code. The rerun `20261121-165301-lootscavenging-outcomes-s1-728a3a15` passed 15/15. The case is flaky by chance and is left as it is. Perf `20261121-165319-perf-s1-de9e9614` 0 ms/tick; release-check passed with the usual no-`perf.*`-cases warning.
- Study may pay for an era advance (Kyle, 2026-11-21). The era case now checks it: progression `20261121-170846-lootscavenging-progression-s1-05f8d324` (6/6). Neolithic scrap was refused for `BRM_Emergence_Medieval` until its seven prerequisites were finished, and was eligible after, with the colony still neolithic.
- Status: Passed, with the flaky era-typical case noted.

### 2026-11-21 - compatibility runs (ticket 14)

- Version/environment: RimWorld 1.7, seed 1, at `cb5ad4e`. New unmapped sets in `DevTests/devtests.json`, each the `dlcs` set plus: `compat` (the four below together), `compat-papers` (Research Papers), `compat-srr` (Semi Random Research: P-Fork Continued), `compat-progression` (Progression: Ammunition and Hospitality), `compat-pinata` (Non uno Pinata, which declares 1.6 at most). Batches: smoke, outcomes, moments and bench on each; settings and saves on `compat`.
- Runs, `compat`: smoke `20261121-162550-…-a62abec7`, outcomes `20261121-162555-…-2defb853`, moments `20261121-162558-…-80482726`, settings `20261121-162600-…-ce6ac179` (8/8), bench `20261121-162602-…-5f627841`, saves `20261121-162606-…-e774fff5` (3/3).
- Runs, single mods: papers `20261121-162644` to `162707` (all pass); srr `20261121-162711` to `162732`; progression `20261121-162738` to `162800`; pinata `20261121-162804` to `162827`.
- Observed:
  - Research Papers: smoke, outcomes, moments and bench passed.
  - Semi Random Research: every study step paid 0 points (smoke `study`; bench `original-study`, `techprint-project`, `study-bill`, `study-finishes-project`). Outcomes and moments passed. The cases set the project with `ResearchManager.SetCurrentProject`, which this mod may override with its own pick, so this is either setup drift or study never paying under it. Not yet told apart.
  - Progression: Ammunition and Hospitality: smoke, outcomes and bench passed. Moments stopped at setup: "no clear 80x14 area near the map centre", so its cases did not run. On `compat`, a charge lance gave 6 spacer scrap instead of 5; it gave 5 on every single-mod set.
  - Non uno Pinata: at death, downed and home-map-only, nothing reached the ground, scrap or whole items, including a colonist's revolver; bench passed. The cases look only on the ground, so whether the gear left on the body was sorted is not observed.
  - No errors in any launch's startup log; the runner counted no mod errors.
- Status: Partially passed. Ticket 14 stays `ready-for-human`.

### 2026-11-21 - full tier after the scrap art and on-body fix

- Version/environment: RimWorld 1.7, seed 1, at `64b73e4`; `dotnet test` 280 passed.
- Runs: run-all default `20261121-152459-run-all-s1-b5121bf9` (57 pass, 1 skip), run-all `dlcs` `20261121-152528-run-all-s1-924f755f` (58 pass), perf `20261121-152603-perf-s1-6935b3ca` (1/1).
- Observed: no failures on either set. The default set's skip is the techprint project, which needs Royalty; it passed with the DLCs. Perf: 0 ms/tick for the mod over 1800 ticks.
- Status: Passed.

### 2026-11-21 - everything on the body

- Version/environment: RimWorld 1.7, default mod set, seed 1; `dotnet test` 280 passed.
- First run: moments `20261121-150605-lootscavenging-moments-s1-ebdb3cf5` failed the new `all-on-body-skips`: a lancer's 5 spacer scrap went into its wreck ("kept on the body"), which cannot be stripped. `ScrapApplier` kept scrap on the body under either on-body option without the mechanoid and no-strip skip that "everything on the body" applies; both now share `BodyLoot.CanHoldLoot`.
- Runs after the fix: smoke `20261121-150723-lootscavenging-smoke-s1-33f31cfa` (9/9), outcomes `20261121-150734-lootscavenging-outcomes-s1-aadc1781` (15/15), moments `20261121-150753-lootscavenging-moments-s1-5d042b65` (13/13).
- Observed: with everything on the body, a pirate's revolver and smokepop belt scrap, 10 ejected chemfuel and 50 silver were in the corpse with nothing on the ground, and stripping dropped all of it; a disassembled revolver's cost list was in the corpse; with corpse removal the scrap and silver lay on the ground; a colonist's revolver dropped whole; a lancer's scrap lay beside the wreck under both on-body options.
- Status: Passed.

### 2026-11-21 - scrap art

- Version/environment: RimWorld 1.7, default mod set, seed 1.
- Run: preview `20261121-144907-lootscavenging-preview-s1` (`--record --frames 1`), passed.
- Observed: all six full stacks drew the new art untinted; the log had no texture, graphic or mod errors.
- Observed by hand (Kyle): piles of 1, 10 and 500 of each era and full studied stacks in stacks `20261121-145653-lootscavenging-stacks-s1` (`--keep-open`): "looks good".
- Status: Passed.

### 2026-11-21 - where loot lands at death

- Version/environment: RimWorld 1.7, default mod set, seed 1; `dotnet test` 280 passed.
- Runs: smoke, moments and settings in one launch (moments `20261121-132706-lootscavenging-moments-s1-b79e51d1`, settings `20261121-132709-lootscavenging-settings-s1-072bab76`), all passed; the moments cases now set `deathDrop` to ScrapOnBody.
- Observed by hand (Kyle, with SimpleCameraSetting and all DLCs): "Everything stays on the body" kept a dead pawn's weapon and inventory on the corpse. No batch case covers that option yet.
- Status: Passed.
- Release-check after the commit: perf `20261121-141122-perf-s1-86e0a1a6` (0 ms/tick), then release-check passed all three lines, with the usual warning that the mod has no targeted `perf.*` cases.

### 2026-11-21 - release-check for release prep

- Version/environment: RimWorld 1.7, seed 1, `perf` mod set, after deploying `bfffcf5` plus the new `About/About.xml` description.
- First check: failed only because the newest `perf.mod-tick` run (`20261121-110350-perf-s1-3094df1a`) measured the DLL from before `ee1977a` and `8d1fa4d`.
- Runs: perf `20261121-120209-perf-s1-d1a2d5d2` (1/1), then `close-session` and `release-check`.
- Observed: perf.mod-tick 0 ms/tick; no developer runner in the package; patch roots and preview size passed. Warning: the mod defines no targeted `perf.*` cases.
- Status: Passed.
- Later the same day, after adding `About/Preview.png` (483 KB): release-check failed again on perf, because the build stamps the git commit into the DLL's informational version (`1.0.0+<sha>`), so the two docs commits changed its hash. Perf `20261121-121735-perf-s1` (0 ms/tick) and release-check then passed, preview size included. `Source/LootScavenging.csproj` now sets `IncludeSourceRevisionInInformationalVersion` to false, so the version reads `1.0.0` and a commit that leaves `Source/` alone keeps the DLL byte-identical. Perf `20261121-122640-perf-s1` (0 ms/tick) and release-check passed on that build.

### 2026-11-21 - bench fixes: smoke, bench, saves, settings; bench with DLCs

- Version/environment: RimWorld 1.7, seed 1. Default set in one launch; bench again with `--mods dlcs`.
- Runs: smoke `20261121-111405-lootscavenging-smoke-s1-0920bef4`, bench `20261121-111417-lootscavenging-bench-s1-356b2793`, saves `20261121-111421-lootscavenging-saves-s1-83df63a9`, settings `20261121-111424-lootscavenging-settings-s1-ed3fee1c`; `dlcs` bench `20261121-111438-lootscavenging-bench-s1-7c6eabd9`. After the fix, outcomes `20261121-111719-lootscavenging-outcomes-s1-0fa45aa8` (15/15) and moments `20261121-111733-lootscavenging-moments-s1-f5019141` (9/9).
- Observed: all passed, no mod errors. Recycling 12 units ran 12>7>2>0 with at most 5 carried; with no project the study bill did not start; keep 3 of 8 studied 5 for 100 points at work 3000 with 105 Intellectual XP and 3 left; 30 points short it studied 2 units at work 900 and finished Complex Clothing; the techprint case passed with Royalty. No `ShowFirstLoadMessage` NullReferenceException at startup.
- Case fixes: the study XP step now counts progress to the next level, and the original-item recycle bound allows the pool's random rounding (a 4-unit rifle can return one 200-silver advanced component).
- Status: Passed.

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
