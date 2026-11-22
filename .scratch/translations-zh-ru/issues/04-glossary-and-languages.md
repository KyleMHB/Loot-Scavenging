# 04: Translation glossary and the two languages in translation.json

**What to build:** Two things that every translation chunk depends on:

- **Languages.** The mod declares ChineseSimplified and Russian in `translation.json` (`"languages": ["ChineseSimplified", "Russian"]`). `status` then lists both languages, each with every text untranslated.
- **Glossary.** A glossary in the mod's translation record folder (the tool's default, `translation/glossary.md`) gives each recurring term its Chinese and Russian rendering. Terms come from the game's own language archives. Follow the spec's "Workflow" step 2. Use the Lumen Stones glossary only for its shape: tables of English | ChineseSimplified | Russian, then per-language style notes.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

**Recommended model:** `opus`: choosing terms needs judgement in both languages.

## Scope

- **Source of terms.** Read terms from the installed game's `Data/Core/Languages/ChineseSimplified (简体中文).tar` and `Russian (Русский).tar`, and the DLC folders' archives where a term is DLC-specific, such as techprint. Extract only under the ignored `work/` folder. These are the game's own data. Never open another mod's or a community pack's translations.
- **Terms to cover.**
  - Era words: these must equal vanilla's `TechLevel` labels.
  - Bench names: crafting spot, smithy, machining table, fabrication bench, research bench.
  - Quality and hit points.
  - Research points, project, techprint and prerequisites.
  - Recycle and study.
  - Vanilla's exact text for the "Do until you have X" bill option.
  - Stripped, downed, corpse and butcher.
  - Colonist, prisoner, raider, faction and hostile.
  - Storyteller and mod settings.
  - The mod's own terms: scrap, studied scrap, unfamiliar, drop mode, out of reach and within reach, the five outcomes, the three preset names, the keep and remove lists, modpack and lock.
- **Style notes.**
  - Chinese uses full-width punctuation, with no spaces around numbers or placeholders.
  - Russian avoids number agreement on placeholder counts.
  - "Loot Scavenging" and DLC names stay in English.

## Acceptance

- [ ] `translation.json` lists both languages. `rwtranslate check` still passes; the untranslated counts appear only as notes.
- [ ] Every term that appears in three or more English texts has a row for both languages.
- [ ] Each vanilla-derived row matches the game's own translation. The glossary says which rows come from vanilla.
- [ ] No vanilla archive extract or other temporary file is left outside `work/`.

## Checks

- `python ../RimWorldTranslate/rwtranslate.py --mod . check` and `status`.
- The 1.7 build, which runs the check.
