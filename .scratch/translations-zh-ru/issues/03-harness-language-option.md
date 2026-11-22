# 03: Harness option to launch a batch in a chosen language

**What to build:** A mod author can run a test batch with RimWorld set to a chosen language instead of English, for example `run <batch> --language Russian`. Without the option, every launch stays English, as it is now. Decision 4 of the spec.

**This ticket works in a different repository:** the shared harness at `E:\Coding\Rimworld\RimWorldDevTests`, not Loot Scavenging. Read its `AGENTS.md` first and only the topics it routes to (`authoring`, `environment`, and `maintaining` for recording the change). Every mod's checkouts share this harness, so the default behaviour must not change.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

**Recommended model:** `opus`: a change to shared tooling that every mod's runs depend on.

## Background

- The launcher pins `langFolderName` to `English` in the harness copy of `Prefs.xml` on every launch (`PINNED_PREFS` in `rimtest.py`; the authoring docs, "every launch then sets its language to English").
- Vanilla language folders carry a native suffix, such as `Russian (Русский)`. Before choosing what the option accepts, confirm which `langFolderName` value the game writes for a language, by reading a `Prefs.xml` the game wrote after a manual language change. The game also matches the short name, so the option can accept `Russian` and `ChineseSimplified`.
- Opening a session already open in a different language must relaunch it, as the launcher already does for a different mod set.

## Acceptance

- [ ] `run <batch> --language <name>` launches the game in that language. The batch report and the run's log record which language was used.
- [ ] Without `--language`, launches stay English, and an idle session is not relaunched when nothing changed.
- [ ] Moving between languages relaunches an idle session, so a batch never runs in the wrong language.
- [ ] An unknown language name is refused before launch, with a message listing the installed languages.
- [ ] The harness's own tests pass (`python -m unittest discover -s tests` in RimWorldDevTests), and the authoring docs describe the option.
- [ ] A Loot Scavenging smoke run with `--language Russian` launches, reports the language, and finishes. Failures caused by English-text assertions are expected until tickets 05 and 06 land; record them, do not fix them here.

## Checks

- RimWorldDevTests' unit tests.
- One real launch through Loot Scavenging's `tools/ls_test.py` wrapper with and without the option, then `close-session`.
- Record the finding through the harness's `maintaining` topic.
