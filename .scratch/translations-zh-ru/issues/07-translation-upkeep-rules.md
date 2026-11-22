# 07: Keep translations current: project rule and release gate

**What to build:** Any agent or person who changes English text in Loot Scavenging is told to update both translations. A Workshop upload cannot go out with untranslated or outdated text. See the spec's "Keeping translations current".

**Blocked by:** 05, 06

**Status:** ready-for-agent

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

- [ ] An agent reading only AGENTS.md knows to re-export and import both languages after an English edit, and which command shows stale text.
- [ ] TESTING.md's Full tier includes the translation `status` gate.
- [ ] No file outside AGENTS.md and TESTING.md changes.

## Checks

- Read the two sections back against the spec. No build is needed for documentation only.
