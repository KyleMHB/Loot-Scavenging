# 04: Translation glossary and the two languages in translation.json

**What to build:** Two things that every translation chunk depends on:

- **Languages.** The mod declares ChineseSimplified and Russian in `translation.json` (`"languages": ["ChineseSimplified", "Russian"]`). `status` then lists both languages, each with every text untranslated.
- **Glossary.** A glossary in the mod's translation record folder (the tool's default, `translation/glossary.md`) gives each recurring term its Chinese and Russian rendering. Terms come from the game's own language archives. Follow the spec's "Workflow" step 2. Use the Lumen Stones glossary only for its shape: tables of English | ChineseSimplified | Russian, then per-language style notes.

**Blocked by:** None (can start immediately)

**Status:** resolved

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

## Comments

2026-11-22, completion note:

- `translation.json` now lists `ChineseSimplified` and `Russian`. `check` passes (exit 0) and reports 241 untranslated, 0 outdated per language as notes. `status` shows the same. The 1.7 Release build succeeds and runs the check.
- `translation/glossary.md` holds 113 rows (68 from vanilla, 45 mod terms). The "From vanilla" tables (eras, benches and bills, items and quality, research, people and loot events) copy the game's own text from the 1.7 Core, Royalty, Ideology, Biotech, Anomaly and Odyssey ChineseSimplified and Russian archives, with the Keyed key or DefInjected path for each row. The "Mod terms" table holds this mod's own choices (scrap, unfamiliar, drop mode, reach, outcomes, presets, lists, modpack, lock and recurring phrases). The Style section covers full-width punctuation, era and quality slots, Russian count agreement and names kept in English.
- Coverage was checked with a unigram and bigram count over the current English Keyed and def texts (241 texts, including ticket 01's in-progress keys). Every term in three or more texts has a row.
- Judgement calls: Russian hit points use `HitPointsBasic`/`MaxHitPoints` (прочность), not `HitPoints` (Целостность). "Study" follows Anomaly's `Study` (调查 / изучить), so it stays apart from 研究 (research). Russian "gear" is снаряжение, because vanilla's `Equipment` says Оружие.
- Archive extracts are under `work/vanilla-lang/` (ignored). No export or import was run.
