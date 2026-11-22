# 07: Keep translations current: project rule and release gate

**What to build:** Any agent or person who changes English text in Loot Scavenging is told to update both translations. A Workshop upload cannot go out with untranslated or outdated text. See the spec's "Keeping translations current".

**Blocked by:** 05, 06

**Status:** resolved

**Recommended model:** `sonnet`: short documentation edits that follow an existing precedent.

## Scope

- **AGENTS.md.**
  - The "Text" section gains a rule, as in Lumen Stones: when English text changes, update ChineseSimplified and Russian with the `rimworld-translate` skill, using the translation glossary.
  - "Commands" names `rwtranslate status` beside the existing check.
  - "Layout" names the `translation/` record folder (committed, not shipped).
- **TESTING.md.**
  - Add the release gate to the release (Full) tier: `status` must show 0 untranslated and 0 outdated for both languages.
  - Add a short note on running a batch with `--language` (ticket 03).
- **Record edits.** After a hand edit to a translation, run `baseline <Language>`. Note this in the same AGENTS.md rule.

## Acceptance

- [x] An agent reading only AGENTS.md knows to re-export and import both languages after an English edit, and which command shows stale text.
- [x] TESTING.md's Full tier includes the translation `status` gate.
- [x] No file outside AGENTS.md and TESTING.md changes.

## Checks

- Read the two sections back against the spec. No build is needed for documentation only.

## Comments

2026-11-22: AGENTS.md gained the `translation/` layout line, the `status` command line and the update rule (re-export and import both languages, `baseline <Language>` after a hand edit, glossary row for new terms, release gate). TESTING.md's Full tier names the `status` gate and its prerequisites list a `--language` note. `check` passes; `status` shows 0 untranslated and 0 outdated for both languages. Beyond the ticket, at the coordinator's request, `translation/glossary.md` gained rows for butcher table, plasteel, ammunition, tainted apparel, caravan, outpost and OK, each checked against the 1.7 language archives under `work/vanilla-lang`. Vanilla has no ammunition category: Chinese comes from `CommandExtractShellDesc`, Russian from `Uranium.description`. Vanilla Russian for outpost is "лагерь" and for OK is "OK".
