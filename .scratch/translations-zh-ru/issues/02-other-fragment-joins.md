# 02: Translatable unfamiliar marker, scope note and study message

**What to build:** Three more places stop joining translated text in C#, so Chinese and Russian control their own punctuation and grammar. See the spec's "Prep: fragment-joined sentences".

- **Unfamiliar marker.** The marker on a kept original item comes from one key with the item label as a slot (English `{0} (unfamiliar)`). Today the C# writes `label + " (" + marker + ")"`.
- **Unfamiliar-scope note.** The unfamiliar-scope explanation on the settings page no longer appends `UnfamiliarUnused` with a literal space. Each explanation, with and without the "unused" note, is its own whole key, or the key text carries the separator.
- **Study message.** The "studied" message passes the pawn as a named argument (`pawn.Named("PAWN")`), and the English key uses `{PAWN_labelShort}`. Russian can then inflect for the pawn's gender.

**Blocked by:** None (can start immediately)

**Status:** resolved

**Recommended model:** `sonnet`: three small, contained edits with clear English to preserve.

## Acceptance

- [ ] A kept original item's label reads exactly as before in English, for example "charge rifle (unfamiliar)".
- [ ] The unfamiliar-scope explanation reads exactly as before in English, in both scopes, with the drop mode on and off.
- [ ] The study-done message reads exactly as before in English and names the pawn.
- [ ] No literal separator around translated text remains in those three places.
- [ ] Both builds and the translation check pass, and `dotnet test` passes.

## Checks

- Both builds and `dotnet test`.
- The smoke, settings, ui and bench batches in one launch. Bench covers study bills and original items; settings and ui cover the scope note.

## Comments

**Completed 2026-11-22.**

**Changed:**

- `Languages/English/Keyed/LootScavenging.xml`: `UnfamiliarLabel` is now `{0} (unfamiliar)`. `UnfamiliarUnused` is removed; `UnfamiliarAllExplainUnused` and `UnfamiliarLootedExplainUnused` are whole sentences (the plain explanation, a space, the old note). `StudyDone` starts with `{PAWN_labelShort}`; its other slots keep their numbers.
- `Source/Unfamiliar/CompUnfamiliar.cs`: `TransformLabel` makes one `Translate(label)` call, with no brackets in C#.
- `Source/SettingsUI/SettingsPage.cs`: the unfamiliar-scope note picks one of four whole keys by scope and by whether the drop mode keeps the original. No literal space.
- `Source/Study/StudyRecipes.cs`: the message passes `pawn.Named("PAWN")` in place of `pawn.LabelShort`.
- `DevTests/SmokeBatch.cs`: the keep-original case read the old one-word marker. It now reads the marker words from the new key (`Translate("")`, trimmed) and still checks that the label contains them.

**Checks:**

- 1.7 and 1.6 Release builds pass; the translation check passes (242 untranslated per language, expected before translation, 0 outdated). `dotnet test`: 291 passed.
- One launch, seed 1: smoke `20261122-021117-lootscavenging-smoke-s1-fe53b5c9`, settings `20261122-021128-lootscavenging-settings-s1-2d39179d`, ui `20261122-021130-lootscavenging-ui-s1-f6931e6c`, bench `20261122-021133-lootscavenging-bench-s1-8348adfb`. All passed (bench skipped only the Royalty-only techprint case).
- English is identical by construction: the new keys hold the old fragments joined with the same single spaces and brackets. No separate byte-for-byte comparison case was run for the scope note or study message.
