# 08: In-game check in Simplified Chinese and Russian

**What to build:** A harness-driven run in each language confirms that players see the translations in game, readable and complete:

- no raw keys or `{0}` slots
- no English left in the mod's text
- no text overlapping or cut off
- no errors naming the mod

See the spec's "Testing Decisions", "In-game language check".

**Blocked by:** 03, 05, 06

**Status:** resolved

**Recommended model:** `opus`: judging rendered text and layout from recordings, in two languages.

## Scope

- **Batches.** Run with `--language ChineseSimplified` and with `--language Russian`: the firstload, smoke, ui and bench batches, and the settings page with each preset. Add `--record` where the case draws the settings page or the bill tabs, so the frames can be looked at.
- **What to look at.**
  - The settings page under each preset, especially Russian line lengths and the one-column layout.
  - A rule sentence with the reach, quality, hit-point and chance conditions together.
  - A locked row's reason under the Progression pack preset (`--mods progression`).
  - The first-load message and its buttons.
  - The machining table's and research bench's Bills tabs.
  - The recycle and study bill descriptions.
  - A scrap stack's inspect text, including a "cannot be studied" reason.
  - The study-done message for a male and a female pawn.
  - An unfamiliar item's label.
  - The item picker.
- **Cases that assert English text.** Where a case compares English, it either skips that comparison for a non-English language or compares against the translated key. Do not weaken the English run.
- **Allowed new case.** Add at most one case that fails when any text the mod draws still contains `{`, a raw `LootScavenging_` key, or is identical to its English for a key whose translation differs. The ui batch already records drawn labels, so this needs no new harness.
- **Fixes.** Fix any untranslated, overlapping or ungrammatical text found: shorten the translation, or widen the layout without changing the English layout. Re-run the affected language.

## Acceptance

- [ ] Each language's run passes, with no error naming the mod in the log.
- [ ] None of the listed surfaces shows a raw key, an unfilled slot, English mod text, or overlapping or clipped text, in either language.
- [ ] The Russian study-done message is grammatical for a male and a female pawn.
- [ ] The English quick-tier batches still pass.
- [ ] TESTING.md gains a dated result entry with the game build, language, mod set, run IDs and any gaps.

## Checks

- Deploy, build the DevTests project, then run `python tools/ls_test.py run ... --language <name>` per language, and `close-session` when finished.
- The English smoke, settings and ui batches after any fix.

## Comments

**2026-11-22, completion (agent).** All acceptance criteria are met; nothing is committed. Details and run IDs are in TESTING.md, under "2026-11-22 - In-game language check".

- **New case:** `lootscavenging.ui.translated-text` in `DevTests/UiBatch.cs`. It scans every label the ui batch draws, plus the following:
  - the settings page with a weapons rule that has every condition and a chance;
  - the item picker;
  - every `LS_` bill's label, job string and description;
  - neolithic and industrial scrap inspect text, with eligible and "cannot be studied" reasons;
  - an unfamiliar charge rifle's label;
  - `StudyDone` for a male and a female pawn.

  It fails on a raw `LootScavenging_` key, a `{`, or an English fragment from a key whose translation differs. The English reference is read from the English Keyed file. Under `--record`, `Draw()` holds each window for 1.5 s. No other case changed, and `ModHarness.cs` is untouched: the case is part of the existing ui batch.
- **Runs:** firstload, smoke, bench and ui in ChineseSimplified, then Russian, plus a recorded ui per language (`--record --show-ui --frames 30`). English ran firstload, smoke, settings, ui and bench. All passed with no logged or mod errors; bench skips techprints without Royalty.
- **Fixes:**
  - Russian: the game capitalizes the letter after any `:` in formatted text (`GenText.CapitalizeSentences`). In response:
    - the 17 `RuleKeep`/`RuleAct*` keys were reworded to "Предметы категории «{0}» …";
    - `StudyDone`, `StudyNow` and five `StudyWhy*` keys use a dash;
    - `StudyWhyProjectEra` uses "так как";
    - `StudyKeepInStock` reads "{0} в запасе".
  - ChineseSimplified: `FirstLoadText` uses a no-break space in "Loot Scavenging", so the name no longer wraps after "Loot".
  - Both languages were baselined; `status` shows 0 untranslated and 0 outdated.
- **Gaps:**
  - Stills show only the top of the settings page. Lower rows were checked as recorded text and with a taller-than-rect observation.
  - The locked reason uses the ui batch's direct lock under the Progression preset. `--mods progression` does not load the pack's preset patch.
  - English has the same capital after a colon, and its unfamiliar-scope note measures taller than its rect. Neither was changed.
