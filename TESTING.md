# Loot Scavenging Testing

Updated through: `main` on 2026-11-22

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
| `LootScavengingMod.cs` (startup, first-load message) | firstload |
| Saves, `CompUnfamiliar`, `StudyBenchSaveCompat`, new saved things or bills | saves, then the removal check |

Run-all's eight batches run in one launch; the order is firstload, smoke, outcomes, moments, settings, bench, saves, ui. Firstload goes first because the game's log keeps only the newest 1000 messages, and it reports not applicable when the log is already full.

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
- `lootscavenging-moments`: downed and strip with the option on and off, a living prisoner's strip, scrap on the body (kept and with corpse removal), everything on the body (scrap, ejected charges, inventory and disassembly products in the corpse; with corpse removal; a colonist's gear and a mechanoid's scrap under both on-body options dropping as usual), a world pawn's death, butchering with a real bill job, and home-map-only on a generated camp map.
- `lootscavenging-settings`: through the calls the page makes, not the page. Presets and "(changed)", Vanilla in game, the first-load flag, locks (the pack's lock names applied to `Presets.Configure` directly; the pack's patch is not loaded), keep and remove lists, a missing-def list entry through a settings save and load, and the picker's search. Two cases go through `LoadedModManager.WriteModSettings`/`ReadModSettings`, reading under the compiled-in config as at startup and running the defaults step under a patched one. `imported-partial-file` reads a hand-written file (`activePreset` Cleanup, two setting keys and a locked drop mode, no `defaultsSeeded`), as an Auto Mod Config preset could leave it: the two keys are kept, a missing key takes the patched Def default, the locked key ends at the pack value and the seeded flag is set. `cleanup-round-trip` saves a Cleanup config with the drop mode at Progression's value: the file leaves it out, and after reading back and the defaults step every value matches.
- `lootscavenging-bench`: real bill jobs. Recycling and studied-scrap bills at a machining table, the study bench and its Bills tab, the study bill with no project, keep-X, work and XP, finishing a project, original items at the fabrication bench and research bench, and a techprint project (Royalty only).
- `lootscavenging-saves`: a looted-only marked rifle, scrap, studied scrap and a study bill through the runner's save and reload, and the save file's contents.
- `lootscavenging-ui`: the settings window opened through the window stack, and the machining table's and research bench's Bills tabs through the inspect pane, each drawn for a few frames while a `Widgets.Label` postfix records what was drawn and where. Checks the six sections, the preset line and buttons, one column top to bottom after each preset switch (the collapse fixed in `e7efc62`), the locked summary and a locked row's reason, both bills in their tabs, the study bench's add-bill options, and the first-load message box called directly. It draws and reads; it does not click. `translated-text` also draws the page with a weapons rule that has every condition and a chance, draws the item picker, and builds the bill labels and descriptions, a scrap stack's inspect text, an unfamiliar item's label and the study-done message for a male and a female pawn. It fails on a raw `LootScavenging_` key, an unfilled `{` slot, or an English fragment (10 or more characters between slots) from a key whose translation differs, read from the English Keyed file. It observes each built text and any label taller than its rect. Under `--record` each window is held 1.5 s so `--frames` stills show it; run it with `--record --show-ui`.
- `lootscavenging-firstload`: the first-load message on the real startup path. The launcher deletes every `Mod_*.xml` before a launch, so each harness launch starts with the flag unset; a developer-only `Mod` reads the settings file while mods are constructed to confirm it. Checks that startup set the flag and wrote the file, and that the whole in-memory log since startup has no error naming the mod (the runner's own count starts at the first batch).
- `lootscavenging-progression` (`--mods progression`, outside run-all): the colony's era after Node Research's medieval emergence, study of medieval scrap for a medieval project, a paper from a project finished by study, a lost paper's retained progress and a second finish, Semi Random Research's picked offer, and every tier's recycling bench and pool material in the pack set. The pack mods are reached by reflection.
- `lootscavenging-preview-story`: a showcase, outside run-all, that films the stills for `About/Preview.png` with the game paused. A downed pirate is shown with his rifle, vest and helmet on the ground (downed handling off), then with scrap in their place, then dead with "everything stays on the body" and nothing on the ground. A workshop shows scrap at a research bench and a machining table. It also films a story scene and a raid aftermath. The case fails if a raider leaves no scrap, or if scrap lands on the ground under everything on the body. Run it with `--record --frames 1 --quit-on-finish`, then `python tools/preview_story.py <run folder>`. That writes the candidates to `work/preview`; `split.png` is the one copied to `About/Preview.png`. Text uses RimWordFont from `Configurable Quality/artifacts/steam-page-mockup`.
- `lootscavenging-workshop-clips`: a showcase, outside run-all, that films the Workshop description GIFs and gallery stills (`.scratch/workshop-media`) on the Progression preset at a tribal colony, at normal speed. It films:
  - a pirate and a tribal warrior attacking a sandbag line, where the pirate's autopistol becomes industrial scrap and the club drops whole;
  - one recycle bill at a crafting spot and one study bill at a research bench;
  - a pirate who dies with everything on the body, then is stripped by a colonist;
  - paused stills of a tribal warrior (club, autopistol in his pack) killed under each of the four drop modes;
  - paused stills of neolithic and spacer scrap selected.

  The case fails if a beat does not happen. Run it with `--record --show-ui --frames 12 --quit-on-finish`. It writes `shots.json` (marks for cutting) and `layout.json` (each shot's camera) to the run folder.
- `lootscavenging-preview`: the earlier showcase of the six scrap piles in front of their benches, filmed for the first preview image (`tools/preview_image.py`). No longer used for `About/Preview.png`.
- `lootscavenging-stacks`: a scene outside run-all for looking at the scrap art by hand. Each era gets piles of 1, 10 and 500 (the small, mid and full textures) and a full studied stack. Run it with `--keep-open`.

Not covered: clicking in the settings page and bill tabs (the ui batch only draws them), a played raid with the Progression pack, Non uno Pinata, Progression: Ammunition and Progression: Hospitality (ticket 14), a `MayRequire` pool entry (none exists), and pawn-job recycling at a smithy or crafting spot.

## Result recording

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
