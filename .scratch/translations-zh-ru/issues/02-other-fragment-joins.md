# 02: Translatable unfamiliar marker, scope note and study message

**What to build:** Three more places stop joining translated text in C#, so Chinese and Russian control their own punctuation and grammar. See the spec's "Prep: fragment-joined sentences".

- **Unfamiliar marker.** The marker on a kept original item comes from one key with the item label as a slot (English `{0} (unfamiliar)`). Today the C# writes `label + " (" + marker + ")"`.
- **Unfamiliar-scope note.** The unfamiliar-scope explanation on the settings page no longer appends `UnfamiliarUnused` with a literal space. Each explanation, with and without the "unused" note, is its own whole key, or the key text carries the separator.
- **Study message.** The "studied" message passes the pawn as a named argument (`pawn.Named("PAWN")`), and the English key uses `{PAWN_labelShort}`. Russian can then inflect for the pawn's gender.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

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
