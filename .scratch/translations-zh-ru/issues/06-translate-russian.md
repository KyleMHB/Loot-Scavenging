# 06: Russian translation

**What to build:** Every Keyed string and every translatable def field has a Russian translation. With the game in Russian, the mod's text shows in Russian and matches the glossary and vanilla's terms. Follow the `rimworld-translate` skill, steps 4 and 5, with agents translating (Decision 1).

**Blocked by:** 01, 02, 04

**Status:** resolved

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

## Comments

**2026-11-22, translation done, not committed.** `export Russian` wrote 242 texts in 3 chunks. One agent translated all three instead of one subagent per chunk: the rule frames (ids 93–108) span chunks 00 and 01, and the verb phrases that fill `{1}` (109–114) sit in chunk 01. Separate agents would have chosen grammar that does not fit together. `import Russian` wrote all 242 texts into `Languages/Russian/` (Keyed plus DefInjected for ThingDef, ThingCategoryDef and WorkGiverDef), along with `translation/record/Russian.json`.

- Rule sentences: the category labels differ in gender and number (Оружие, Одежда, Боеприпасы…), so `{0}` never acts as the subject: «{0}: [с шансом {4}] предметы с затронутых персонажей {1}, если …». Each Verb key is a plural predicate (повреждаются…, превращаются в лом своей эпохи, удаляются), so it agrees with «предметы» and with the tail «В остальных случаях они выпадают как обычно». Conditions follow the glossary style: «качество ниже «{2}»», «прочность ниже {3}», «вне досягаемости поселения».
- StudyDone: «{PAWN_labelShort} {PAWN_gender ? завершил : завершила} изучение: {2} ({1} шт.). …». `{2}` can be the label of a feminine original item, so it follows a colon instead of taking the accusative.
- The era words in the scrap and studied-scrap labels and descriptions are vanilla's `TechLevel` adjectives. StudyDescription quotes «Повторять до X шт».
- Checks: `status` reports Russian 0 untranslated, 0 outdated. `check` passed. The 1.7 and 1.6 Release builds passed. Placeholders and the literal `\n` in FirstLoadText were confirmed in the written Keyed file. The in-game check is left to ticket 08.
- Glossary gap: Ammunition (the category) has no row. I used «Боеприпасы», vanilla's word in its descriptions and in `AncientAmmoStack.label`. The glossary was not edited.
- Status stays open for one reason: the acceptance item "the record is committed with the language files" waits on the commit, which was not made in this session. Every other criterion is met.

2026-11-22, orchestrator: committed the language files and record together; Status resolved.
