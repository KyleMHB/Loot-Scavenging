# 06: Russian translation

**What to build:** Every Keyed string and every translatable def field has a Russian translation. With the game in Russian, the mod's text shows in Russian and matches the glossary and vanilla's terms. Follow the `rimworld-translate` skill, steps 4 and 5, with agents translating (Decision 1).

**Blocked by:** 01, 02, 04

**Status:** ready-for-agent

**Recommended model:** `opus`: quality of the translation, Russian case and gender agreement, and keeping placeholders intact. The skill dispatches one subagent per chunk.

## Scope

- Run `rwtranslate export Russian`, translate each chunk into its `.done.json` with the Write tool, then run `import Russian`.
- Use the glossary's terms.
- The study-done message uses `{PAWN_...}` gender forms (ticket 02).
- The 16 rule sentences (ticket 01) each read as natural Russian. Do not translate them word for word from English.
- Avoid number agreement where a count is a placeholder, per the glossary's style notes.
- Settings text should stay concise, because Russian runs longer than English and the page has fixed widths.

## Acceptance

- [ ] `rwtranslate status` shows `Russian: 0 untranslated, 0 outdated`.
- [ ] `rwtranslate check` passes, and both builds pass.
- [ ] The era words in the scrap labels and descriptions equal vanilla's Russian `TechLevel` labels. The study description quotes vanilla's Russian "Do until you have X" text.
- [ ] The record for Russian exists and is committed with the language files. The `work/translation` chunks are not committed.

## Checks

- `rwtranslate status`, `check`, and both builds.
- A spot check of ten texts against the glossary, including a rule sentence with every condition, the study-done message and a "cannot be studied" reason.
