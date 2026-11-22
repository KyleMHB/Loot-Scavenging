# 03: Harness option to launch a batch in a chosen language

**What to build:** A mod author can run a test batch with RimWorld set to a chosen language instead of English, for example `run <batch> --language Russian`. Without the option, every launch stays English, as it is now. Decision 4 of the spec.

**This ticket works in a different repository:** the shared harness at `E:\Coding\Rimworld\RimWorldDevTests`, not Loot Scavenging. Read its `AGENTS.md` first and only the topics it routes to (`authoring`, `environment`, and `maintaining` for recording the change). Every mod's checkouts share this harness, so the default behaviour must not change.

**Blocked by:** None (can start immediately)

**Status:** resolved

**Recommended model:** `opus`: a change to shared tooling that every mod's runs depend on.

## Background

- The launcher pins `langFolderName` to `English` in the harness copy of `Prefs.xml` on every launch (`PINNED_PREFS` in `rimtest.py`; the authoring docs, "every launch then sets its language to English").
- Vanilla language folders carry a native suffix, such as `Russian (Русский)`. Before choosing what the option accepts, confirm which `langFolderName` value the game writes for a language, by reading a `Prefs.xml` the game wrote after a manual language change. The game also matches the short name, so the option can accept `Russian` and `ChineseSimplified`.
- Opening a session already open in a different language must relaunch it, as the launcher already does for a different mod set.

## Acceptance

- [x] `run <batch> --language <name>` launches the game in that language. The batch report and the run's log record which language was used.
- [x] Without `--language`, launches stay English, and an idle session is not relaunched when nothing changed.
- [x] Moving between languages relaunches an idle session, so a batch never runs in the wrong language.
- [x] An unknown language name is refused before launch, with a message listing the installed languages.
- [x] The harness's own tests pass (`python -m unittest discover -s tests` in RimWorldDevTests), and the authoring docs describe the option.
- [x] A Loot Scavenging smoke run with `--language Russian` launches, reports the language, and finishes. Failures caused by English-text assertions are expected until tickets 05 and 06 land; record them, do not fix them here.

## Checks

- RimWorldDevTests' unit tests.
- One real launch through Loot Scavenging's `tools/ls_test.py` wrapper with and without the option, then `close-session`.
- Record the finding through the harness's `maintaining` topic.

## Comments

**2026-11-22, completion (agent).**

**Correction to Background.** I decompiled the installed 1.7.5014 rev1195 build (`LanguageDatabase`, `LoadedLanguage`, `Prefs`, `TarDirectory`). The game writes the full folder name to `Prefs.xml`, such as `Russian (Русский)`. It selects a language only when Prefs holds that exact name. With a short name such as `Russian`, the game quietly falls back to English. The short-name match applies only when a mod's `Languages/Russian` folder is merged into the vanilla language. So `--language` accepts short or full names, case-insensitively, and writes the full name.

**Changed (RimWorldDevTests).**

- `rimtest.py`:
  - `run --language NAME`. Names are resolved against `Data/Core/Languages`. An unknown name is refused (exit 3) with a list of the installed languages.
  - Language is part of the session identity. A `session-mods.json` with no language counts as English.
  - A run fails as an isolation problem when the language the game loaded differs from the one requested.
  - stdout and stderr use `backslashreplace`, so a full name cannot crash piped output.
- `Harness/Runner.cs`: `environment.language` in `report.json`, and the language on the run-start line in `Player.log`.
- `Harness/Report.cs`: a `Language:` line in `summary.txt`.
- Docs: `docs/authoring.md` (the option), `docs/game-facts.md#languages` (the finding), `docs/recipes/index.md` (a row).

**Checks.**

- `python -m unittest discover -s tests` passes (14 tests). The harness and Loot Scavenging DevTests builds pass.
- `--language Klingon`: refused (exit 3) with the language list.
- `run lootscavenging-smoke --language Russian`, run `20261122-020914-lootscavenging-smoke-s1-a52a069e`: PASS, 9/9, `Language: Russian (Русский)`. The harness Prefs held the full name and `Player.log` says "started in Russian (Русский)". No English-text assertion failed in smoke. Vanilla Russian logged a warning that its translation data has 38 errors under the 3-mod set.
- Normal run `20261122-020937-...-5256cfca`: relaunched ("loaded ... in Russian (Русский); relaunching ... in English"), then PASS, `Language: English`.
- Normal run `20261122-020953-...-b03b6d83`: reused the idle session, PASS, English.
- `close-session` was run, and the lease was released.

**Follow-up outside this ticket's paths.** `E:\Coding\Rimworld\GAME.md` line 164 says the game also matches the short name. That holds for mod folders, not for Prefs, and the line needs that caveat.
