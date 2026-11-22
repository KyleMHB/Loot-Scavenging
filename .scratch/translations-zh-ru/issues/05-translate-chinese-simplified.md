# 05: Simplified Chinese translation

**What to build:** Every Keyed string and every translatable def field has a Simplified Chinese translation. With the game in Simplified Chinese, the mod's text shows in Chinese and matches the glossary and vanilla's terms. Follow the `rimworld-translate` skill, steps 4 and 5, with agents translating (Decision 1).

**Blocked by:** 01, 02, 04

**Status:** resolved

**Recommended model:** `opus`: quality of the translation and keeping placeholders intact. The skill dispatches one subagent per chunk.

## Scope

- Run `rwtranslate export ChineseSimplified`. This writes chunks under the ignored `work/translation`. As of `fcb922c` there were up to 206 Keyed and 28 DefInjected texts, plus the keys 01 and 02 add.
- Write each chunk's `.done.json` with the Write tool, not a shell heredoc. Literal `\n` must stay as the two characters.
- Run `import ChineseSimplified`. It writes the Keyed and DefInjected files under the root `Languages/ChineseSimplified`, and the record under the translation folder.
- Use the glossary's terms. Use full-width punctuation. Keep every `{0}`, `{PAWN_...}` and `\n`.

## Acceptance

- [ ] `rwtranslate status` shows `ChineseSimplified: 0 untranslated, 0 outdated`.
- [ ] `rwtranslate check` passes, and both builds pass.
- [ ] The era words in the scrap labels and descriptions equal vanilla's Chinese `TechLevel` labels. The study description quotes vanilla's Chinese "Do until you have X" text.
- [ ] The record for ChineseSimplified exists and is committed with the language files. The `work/translation` chunks are not committed.

## Checks

- `rwtranslate status`, `check`, and both builds.
- A spot check of ten texts against the glossary, including a rule sentence with every condition and a "cannot be studied" reason.

## Comments

2026-11-22, translation agent: translated all 242 texts (214 Keyed, 28 DefInjected) in three chunks and imported them. Written: `Languages/ChineseSimplified/Keyed/LootScavenging.xml`, `DefInjected/{ThingDef/Scrap.xml, ThingDef/Study.xml, ThingCategoryDef/Scrap.xml, WorkGiverDef/Study.xml}` and `translation/record/ChineseSimplified.json`. Not committed, so Status stays open until the language files and record are committed together.

- `status`: ChineseSimplified 0 untranslated, 0 outdated. `check` passes. The 1.7 and 1.6 Release builds pass.
- Era words in the 12 scrap labels and descriptions use vanilla's `TechLevel` labels (石器时代, 中世纪, 工业时代, 太空时代, 极致时代, 超凡时代). The study description quotes 「维持 X 个」 exactly.
- Spot check against the glossary: `UnfamiliarLabel` `{0}（陌生）`; `StudyDone` keeps `{PAWN_labelShort}` and `{1}`–`{4}`; `RuleActReachQualityOrHitPointsChance` keeps `{0}`–`{4}`; `StudyWhyProjectEra`, `StudyWhyColonyEra` (达到{0}, no trailing 时代); `FirstLoadText` keeps the literal `\n\n` and 选项 > Mod 设置; WorkGiver label/verb/gerund follow vanilla's `DoBills*` pattern (调查于). No text holds `TargetA`.
- Terms used that the glossary lacks (vanilla Chinese): ammunition 弹药 (`TabShells`), OK 确定 (`OK`), caravan 远行队 (`Caravan`), outpost 哨所 (`Outpost.label`), plasteel 玻璃钢 (`Plasteel.label`), tainted apparel 死者衣物 (`IncludeTainted`), butcher table 屠宰台 (`TableButcher.label`). The glossary was not edited.

2026-11-22, orchestrator: committed the language files and record together; Status resolved.
