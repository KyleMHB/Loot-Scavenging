# Simplified Chinese and Russian translations

Status: ready-for-agent

Tickets: `issues/01` to `issues/09` in this folder.

Spec for adding Simplified Chinese and Russian to Loot Scavenging (`kylemhb.lootscavenging`, RimWorld 1.6 and 1.7). Written 2026-11-21 from the repo at `fcb922c`, the shared RimWorld Translate tool and the `rimworld-translate` skill, with Lumen Stones as the precedent.

## Decisions

Recorded 2026-11-21. Q1, Q3 and Q4 are Kyle's answers. The rest are the coordinator's defaults, which Kyle can still change.

1. **Who translates (Kyle):** agents do machine translation through the `rimworld-translate` skill's chunks, as in Lumen Stones. No native-speaker review is planned before release.
2. **When (default):** translate now. The auto-config spec (`.scratch/auto-config/spec.md`) adds no new Keyed string under its recommended options. Its example preset text is English metadata in a separate preset mod, not Keyed, so it is out of scope here. If auto-config or later work changes English, the re-export rule covers it.
3. **Rule sentence (Kyle):** whole-sentence keys, one for each combination of condition and chance. Each key holds the joining words, the punctuation and the "otherwise they drop as normal" tail. Values (category, outcome verb phrase, quality name, percentages) stay as slots. The fragment keys are removed once nothing uses them.
4. **In-game language check (Kyle):** a harness change. The shared RimWorldDevTests launcher gains an option to launch a batch in a chosen language. That work is in a different repository (`E:\Coding\Rimworld\RimWorldDevTests`), and the language-check ticket is blocked by it.
5. **Workshop and About.xml (default):** each gains one Languages line naming English, Simplified Chinese and Russian. The rest of both descriptions stays in English, untranslated.
6. **Disclosure (default):** say on the Workshop page and in the CHANGELOG entry that the translations are machine-made, and invite corrections.
7. **Version (default):** minor, 0.3.0, per the project rule. The bump happens once per Workshop upload, in the release ticket only.
8. **Test seams (default):** the seams under Testing Decisions are accepted: the tool's `check`/`status`, the existing builds and batches for the prep, and a harness-driven in-game pass for each language.

## Problem Statement

Loot Scavenging ships English only. Chinese and Russian players are a large share of RimWorld's audience, and the mod's main surface (a long settings page in which every row explains itself in a sentence, plus inspect text on scrap and the study bills) is text-heavy, so a player who does not read English cannot tell what a preset or rule will do. The English text is already fully translatable: every player-facing C# string goes through a `LootScavenging_` Keyed key, and the build fails on a hardcoded English literal. What is missing is the translations themselves, a glossary that keeps them consistent with the game's own Chinese and Russian, and a rule that keeps them current when the English changes.

A few settings-page sentences are assembled from translated fragments joined with literal spaces and brackets in C#. That works in English but breaks word order in Russian and puts stray spaces into Chinese, so those must be fixed before translating.

## Solution

Add `ChineseSimplified` and `Russian` language folders holding every Keyed string and every translatable def field, produced through the shared RimWorld Translate tool's export/import flow and the `rimworld-translate` skill, as Lumen Stones did. Before translating, rewrite the few fragment-joined sentences so each translatable unit is a whole sentence or carries its own separators, keeping English output byte-identical. Add a glossary that takes system terms from the game's own Chinese and Russian. Record the English each translation was made from, so a later English change shows as outdated, and add a release gate and a project rule that bring both languages back to zero untranslated and zero outdated before each Workshop upload.

From the player's side: with the game language set to Simplified Chinese or Russian, the settings page, first-load message, scrap and studied-scrap names and descriptions, bill labels and descriptions, inspect text, messages and the "unfamiliar" marker all appear in that language, using the same words the base game uses for research benches, eras, quality levels and bills.

## User Stories

1. As a Chinese-speaking player, I want the settings page in Simplified Chinese, so that I can choose a preset and rules without guessing.
2. As a Russian-speaking player, I want the settings page in Russian, so that I can choose a preset and rules without guessing.
3. As a Chinese or Russian player, I want each preset's explanation translated, so that I know what Progression, Cleanup and Vanilla do before I pick one.
4. As a Chinese or Russian player, I want the sentence under each item rule to read as natural grammar in my language, so that I can trust what it says the rule will do.
5. As a Russian player, I want rule sentences whose word order and case endings fit Russian, so that they are not English word order with Russian words.
6. As a Chinese player, I want no stray Latin spaces between translated phrases, so that the text looks like normal Chinese.
7. As a Chinese or Russian player, I want the six scrap items and six studied-scrap items named and described in my language, so that I recognise them in stockpiles, trade and the inspect pane.
8. As a Chinese or Russian player, I want the era words in scrap names to match the game's own words for neolithic, medieval, industrial, spacer, ultratech and archotech, so that "industrial scrap" and the research tab's industrial era read as the same thing.
9. As a Chinese or Russian player, I want the recycle and study bill labels, job reports and bill descriptions in my language, so that bill menus are not mixed-language.
10. As a Chinese or Russian player, I want the study bill description to quote the "Do until you have X" option exactly as my game's bill dialog names it, so that I can find it.
11. As a Chinese or Russian player, I want the scrap's inspect text explaining why it cannot be studied in my language, so that I can fix the reason.
12. As a Chinese or Russian player, I want bench names, quality names and "techprint" in the mod's text to match the base game's translation, so that the mod reads as part of the game.
13. As a Russian player, I want the "studied scrap" message to inflect for the pawn's gender, so that a female researcher's message is grammatical.
14. As a Chinese or Russian player, I want the "unfamiliar" marker on a kept original item in my language and punctuation, so that the item label reads naturally.
15. As a Chinese or Russian player, I want the first-load message and its buttons in my language, so that the first thing the mod shows me is readable.
16. As a Chinese or Russian player, I want the modpack lock reasons in my language, so that I know why a setting cannot be changed.
17. As a Chinese or Russian player, I want the item picker's labels and the keep/remove list text in my language, so that I can manage the lists.
18. As a Chinese or Russian player, I want long Russian sentences to fit the settings page without overlapping or being cut off, so that the page stays usable.
19. As a Chinese or Russian player, I want every placeholder (counts, item names, project names, percentages) to show the right value in my language's sentence, so that no `{0}` appears and no number is in the wrong place.
20. As a Chinese or Russian player, I want the translations to stay current after updates, so that new settings do not appear in English alongside translated ones.
21. As a player, I want the English text unchanged by this work, so that nothing I already know shifts.
22. As a player on RimWorld 1.6, I want the translations to load there too, so that the language works on both supported versions.
23. As a modpack author, I want translated text to work with my locks and preset patch, so that the Progression pack's locked settings show translated reasons.
24. As the author, I want a glossary of recurring terms per language, so that every chunk and every later update uses the same words.
25. As the author, I want the glossary's system terms taken from the game's own Chinese and Russian, so that the mod matches vanilla rather than my or a translator's guess.
26. As the author, I want a record of the English each translation was made from, so that `status` tells me which texts went stale after I edit English.
27. As the author, I want a release gate that stops a Workshop upload with untranslated or outdated text, so that I do not ship half-translated updates.
28. As the author, I want the project's AGENTS.md to say how to update translations after English changes, so that any agent editing English also updates both languages.
29. As the author, I want the build's existing translation check to cover the new languages, so that a broken placeholder or malformed file fails the build.
30. As the author, I want the deploy script to ship the new language folders, so that what I test is what players get.
31. As the author, I want the translation work to land after the auto-config feature's strings settle, or to re-run cheaply afterwards, so that I do not pay for translating text twice.
32. As the author, I want the fragment-sentence fixes to be ordinary code changes with English output byte-identical, so that existing tests keep passing.
33. As the author, I want to know whether the translations are machine-made or human-reviewed, so that I can say so honestly on the Workshop page.
34. As a native-speaker volunteer, I want the translations in plain LanguageData files beside a glossary, so that I can review and correct them by pull request.
35. As the author, I want the version bump and changelog entry for this work to follow the project's rule, so that the three version locations stay in step.

## Implementation Decisions

### Scope

- **Languages:** Simplified Chinese and Russian. The folder names come from the tool's localisation facts and match Lumen Stones: `ChineseSimplified` and `Russian` (the short legacy names mods use; vanilla's own folders carry a native suffix such as `ChineseSimplified (简体中文)`, which the game also matches). The `translation.json` `languages` field becomes `["ChineseSimplified", "Russian"]`, the format Lumen Stones uses; `keyPrefix` and `hardcodedCheck` stay as they are.
- **Keyed text:** every key in the one English Keyed file, 206 keys, about 10,500 characters and 1,800 words as of `fcb922c`. Translations go in a Keyed file of the same name under each language folder.
- **DefInjected text:** every translatable def field the tool lists with `defs`, 28 entries as of `fcb922c`:
  - ThingDef: six scrap items and six studied-scrap items, label and description each (24).
  - ThingCategoryDef: the scrap category label (1).
  - WorkGiverDef: the study work giver's label, verb and gerund (3).
  - The scrap-tier defs and the settings-defaults def carry no player-visible text, so they get none. Recycle and study bills are built in C# and take their text from Keyed keys, so they need no DefInjected.
  - Export merges identical English, so the number of texts translated is at most 206 + 28.
- **Load folders:** the mod loads the root folder plus the version folder. All English text lives in the root, so both translations go in the root's `Languages` and load on 1.6 and 1.7. The tool writes them there.
- **Not translated:** `// not translated` developer literals (debug actions, logs, config errors); the item picker's `(defName)` suffix; the `packs/` preset patch; the Workshop preview art; README, CHANGELOG and TESTING. The About.xml description and the Workshop page stay in English apart from one Languages line each (Decision 5).

### Prep: fragment-joined sentences (must land before translating)

The Templates RimWorld agent guide says to use one key per whole sentence and never join translated fragments. Four places break that, and they block translation:

- **Rule sentence on the settings page.** The sentence under each item rule is built from `RuleAct` with four slots (category, outcome verb, condition, chance). The condition comes from `CondWhen`, which wraps `CondReach`, `CondQuality` and `CondHitPoints` nested through `CondAnd` and `CondOr`. The C# adds literal spaces before the condition, before the chance, and before `RuleOtherwiseKept`. Russian needs different word order and case per combination, and Chinese must not have Latin spaces.
- **Unfamiliar-scope explanation.** The C# appends `UnfamiliarUnused` to another explanation with a literal space.
- **Unfamiliar item label.** The C# builds `label + " (" + unfamiliar + ")"`. Chinese prefers full-width brackets, and some languages put the marker first.
- **Study message.** `StudyDone` takes the pawn's short label as `{0}`. Russian inflects the verb for gender, so the pawn is passed with `pawn.Named("PAWN")` and the English key uses `{PAWN_labelShort}`.

Required outcome: every separator, bracket and joining word lives in Keyed text, so a translation controls it. English output stays byte for byte the same, and the build's translation check and the unit tests keep passing. The rule sentence uses whole-sentence keys, one for each combination of condition and chance (Decision 3). Any new or renamed keys change the English Keyed file, so this prep must finish before export.

### Workflow

Follow the `rimworld-translate` skill. Its steps map onto this mod as follows:

1. **Orient.** `status` and `check` pass today with no languages. The key prefix is `LootScavenging_`. Step 2, extraction, is already done (`hardcodedCheck` is on and passes), apart from the prep above.
2. **Glossary.** Write the mod's translation glossary (the tool's default `translation` record folder, committed and not shipped), using the Lumen Stones glossary's shape: tables of English, ChineseSimplified and Russian, then per-language style notes. System terms come from the game's own Chinese and Russian language archives in the installed game's `Data/Core/Languages` (and the DLC folders for techprint and DLC item names). These are the game's own data, so the originality rule allows reading them. They are opened read-only and extracted under the ignored `work/` folder. Terms the glossary must cover:
   - **Era words.** These must match the game's `TechLevel` labels, because inspect text shows `TechLevel.ToStringHuman()`, which vanilla already translates. The scrap labels must use the same words.
   - **Bench names:** crafting spot, smithy, machining table, fabrication bench, research bench.
   - **Quality names and hit points.**
   - **Research terms:** research points, research project, techprint, prerequisites.
   - **Bill terms:** recycle, study, the bill's repeat options. The "Do until you have X" option is quoted inside the study description, so it must match vanilla's translated text exactly.
   - **Loot terms:** stripped, downed, corpse, butcher.
   - **People:** colonist, prisoner, raider, faction, hostile.
   - **Storyteller** and **mod settings.**
   - **The mod's own terms:** scrap, studied scrap, unfamiliar, drop mode, out of reach / within reach, keep / degrade / scrap / disassemble / delete, the three preset names, keep list and remove list, modpack and lock.

   Style notes follow Lumen Stones: Chinese uses full-width punctuation and no spaces around numbers or placeholders; Russian avoids number agreement where a count is a placeholder. The mod's name, "Loot Scavenging", stays in English in the first-load title, as Lumen Stones keeps DLC and mod names in English.

   Done when every term that appears in three or more English texts has a row for both languages.
3. **Translate.** Run `export ChineseSimplified` and `export Russian`. These write chunks to the ignored `work/translation`. Agents translate each chunk into a done file, one subagent per chunk (Decision 1).
4. **Import and verify.** Run `import` for each language, then `status`, `check` and both builds. `import` refuses to write if a translation drops or changes a placeholder. Done when each language shows 0 untranslated and 0 outdated, `check` passes, and the 1.7 and 1.6 builds pass.
5. **Wire.**
   - `deploy.ps1` already copies the root `Languages`, so the new folders deploy with no change.
   - The build already runs `check` after every build.
   - The project's AGENTS.md gains a rule like Lumen Stones' rule: when English text changes, update ChineseSimplified and Russian with the `rimworld-translate` skill and the glossary. It also names `status` beside the check command.

### Keeping translations current

- `import` writes the record (the English each translation came from) under the translation folder. After an English change, `status` and `check` report the affected texts as outdated, and `export` re-exports only the missing and outdated texts, each with its previous translation beside it. Re-running for a later English change costs only the changed texts.
- **The build does not catch drift.** `check` reports untranslated and outdated counts as notes, not failures, so the build passes with stale translations. The gate is a release step: before each Workshop upload, `status` must show 0 untranslated and 0 outdated for both languages, or the update is run first. Add this to the release checklist the project uses, beside the version bump.
- If a translation is edited by hand (for example a native-speaker fix), run `baseline <Language>` afterwards so the record accepts it as current.

### Ordering with other work

- Tickets 01 to 13, 15 and 16 in the `loot-scavenging` feature are done. Ticket 14 (compatibility runs) is `ready-for-human`, in-game only, and adds no strings. Nothing open in that feature blocks this work.
- The auto-config feature is being specced in parallel under `.scratch/auto-config/` (`.scratch/auto-config/spec.md`). It will add English Keyed strings and may change existing settings-page text. Its spec now exists and adds no Keyed string under its recommended options, so translation goes ahead now (Decision 2). Any later English change is re-exported by the flow above, and the release gate stops an upload with untranslated text.
- The prep (fragment sentences) comes first within this feature, because it changes keys.

### Packaging and versioning

- **Files players get:** two new language folders under the root `Languages`. No new defs, textures or patches.
- **DLL change:** the prep changes C#, so this is not a text-only release. It needs both builds, the quick tier for the settings, ui and bench areas, and a new release-check perf run before upload. (A version bump alone does not touch the DLL, but the prep does.)
- **Version:** one bump at the Workshop upload, across About.xml `<modVersion>`, the Workshop description's mod-version line and the newest CHANGELOG heading together. Minor, 0.3.0, in the release ticket (Decision 7).
- **CHANGELOG:** an "Added" entry naming both languages. It says they are machine translations and invites corrections (Decision 6). The Workshop page says the same.
- **Committed but not shipped:** the translation folder (glossary and record). Not committed: `work/translation` chunks and any vanilla archive extracts under `work/`, which git already ignores. `deploy.ps1` does not ship the translation folder: it copies About, 1.6, 1.7, Defs, Patches, Languages, Textures and packs.

## Testing Decisions

- A good test checks what the player sees in the language, not the tool's internals. The tool has its own unit tests. The seams here are the tool's `check`/`status` (file structure, placeholders, completeness, staleness), the existing builds and tests (English unchanged by the prep), and the game itself with the language switched.
- **Every change:** both builds (the 1.7 build runs `check`), `dotnet test`, and `status` showing 0 untranslated and 0 outdated for both languages.
- **The prep:**
  - The quick tier for `Source/SettingsUI` (smoke, settings, ui).
  - Bench, for the study message and the unfamiliar label.
  - English text must be byte-identical: the ui batch records drawn labels, and the existing tests that compare English text keep passing unchanged.
  - Prior art: the ui batch's label recording.
- **In-game language check.** The test harness sets the game to English on every launch (RimWorldDevTests authoring docs), so the existing batches cannot show the translations. So the harness gains a language option (Decision 4), and a Loot Scavenging batch run in each language checks the following:
  - The settings page with each preset, especially Russian line length against the page's fixed widths and the one-column layout.
  - A rule sentence with quality, hit-point, reach and chance conditions together.
  - A locked row's reason, using the Progression pack preset.
  - The first-load message.
  - The machining table's and research bench's Bills tabs.
  - The recycle and study bill descriptions.
  - A scrap stack's inspect text, including a "cannot be studied" reason.
  - The study-done message for a male and a female pawn.
  - An unfamiliar item's label.
  - The item picker.
  - No errors naming the mod in the log.
- **Quality review:** check translations against the glossary and the vanilla terms by spot check. No native-speaker review is planned (Decision 1).
- TESTING.md gains a short note on the language check and the release gate, and a result entry when the check is run.

## Out of Scope

- Languages other than Simplified Chinese and Russian, including Traditional Chinese.
- Translating README, CHANGELOG, TESTING, the spec, the Workshop preview art or the `packs/` patch.
- Changing the RimWorld Translate tool. The harness language option is in scope only as ticket 03, in the RimWorldDevTests repository.
- Opening, copying or comparing against any other mod's text or translations, including community translation packs and the inherited languages in the user's forked mods.
- Translating the auto-config feature's strings before they exist. They are covered by the update flow.
- Steam Workshop publication itself. That is a separate release step.

## Further Notes

- **Tool location.** The tool lives at `E:\Coding\Rimworld\RimWorldTranslate`. The build and the project's AGENTS.md use it. The Templates copy under `tools/rimworld-translate`, which the skill names, is identical as of 2026-11-21.
- **Auto Mod Config `presetLabel`.** The auto-config spec asks whether DefInjected can translate this field. It lives in the separate curator preset mod, not in Loot Scavenging's load folders, so this spec leaves it untranslated.
- **Commands to run (read-only).** `check`, `status` and `defs` were run for this spec. `export` was not, because it writes files.
- **Precedent.** Lumen Stones commit `88f9b5a7` added both languages in one commit with:
  - Keyed and DefInjected files per load folder.
  - A glossary of stones, systems and style notes.
  - A record per language.
  - An AGENTS.md rule to update both languages with the skill.

  Its `status` shows 0 untranslated and 0 outdated today.
- **Vanilla's own translation.** Inspect text and settings already show vanilla's translation for era names (`TechLevel.ToStringHuman()`), quality names (`QualityCategory.GetLabel()`), and def labels for items, benches and projects. That is why the glossary must take those words from vanilla.
