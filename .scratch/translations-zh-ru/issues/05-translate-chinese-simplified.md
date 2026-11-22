# 05: Simplified Chinese translation

**What to build:** Every Keyed string and every translatable def field has a Simplified Chinese translation. With the game in Simplified Chinese, the mod's text shows in Chinese and matches the glossary and vanilla's terms. Follow the `rimworld-translate` skill, steps 4 and 5, with agents translating (Decision 1).

**Blocked by:** 01, 02, 04

**Status:** ready-for-agent

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
