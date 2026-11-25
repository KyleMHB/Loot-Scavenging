# 09: Workshop text fixes for 0.6.0

Status: resolved

**Recommended model:** `sonnet`: short copy edits against a checklist.

**Blocked by:** none.

**What to build:** the text the 0.6.0 upload carries is correct and fits Steam. Found by the 0.6.0 release review on 2026-11-25. No change to `Source/`, so the release check at `d0ba2f9` stays valid.

## Scope

1. **Description length.** `steam-description.md` is 8,104 characters, over Steam's 8,000 limit. It was 7,950 before the Discord badge under Links (`c6ce0ee`). Keep the Discord badge. Cut at least 150 characters, aiming for about 7,800 so later edits have room. Candidates are the License paragraph (one shorter sentence, keeping GPLv3) and the plain "Support me on Ko-fi." line if the Ko-fi badge says the same. Re-measure the raw file and check the page in `work/preview/workshop-page-preview.html` if the preview tool still exists.
2. **Change note.** `workshop-changenote.md` is still the 0.5.2 note. Write the 0.6.0 note from the `## 0.6.0` section of `CHANGELOG.md`, in the same plain style and at about the same length (the 0.5.2 note is 836 characters). Cover:
   - the Who list;
   - the three "Don't drop your stuff" boxes, all on by default;
   - the drop mode setting removed;
   - 0.5.2 settings carried over once, with defaults reset where they no longer map.
   Check each claim against `CHANGELOG.md`.
3. **CHANGELOG.md:9.** The sentence says the Hostiles box and the Non-hostiles box both "cover non-hostile factions and wild people". Make the Hostiles box cover hostile pawns and the Non-hostiles box cover non-hostile factions and wild people.
4. **CHANGELOG.md:43.** It lists `rules.downedGear` among old lock names. That setting never shipped (the 0.5.2 release branch, `70ffa31`, has no such node). Remove it from the list. Leave the alias in `Source/Model/SettingLocks.cs:69` alone.
5. **TESTING.md:82.** "Not covered" still lists Non uno Pinata. The `compat-pinata` runs recorded on 2026-11-25 cover it; remove or reword the item.
6. **README.md Discord link** (optional). The Steam description links the Discord server and the README does not. Add the same link under the README's links if it has a links section, following `E:\Coding\Templates\writing-copy.md`.

Read `E:\Coding\Templates\writing-copy.md` before editing reader-facing copy, and apply the `unslop` skill to the description, the change note and the CHANGELOG lines.

## Acceptance

- [x] `steam-description.md` is under 8,000 characters with the Discord badge kept.
- [x] `workshop-changenote.md` describes 0.6.0 and agrees with `CHANGELOG.md`.
- [x] CHANGELOG lines 9 and 43 and TESTING.md line 82 are corrected.
- [x] Nothing in `Source/` changed; one commit per file group (description and change note; CHANGELOG; TESTING.md; README).

## Comments

2026-11-25: Description cut from 8,104 to 7,809 characters (CRLF counted as one) with the Discord badge kept: shorter License and cleanup paragraphs, the plain Ko-fi line and two small phrases removed. Change note rewritten for 0.6.0 (910 characters). CHANGELOG Hostiles/Non-hostiles sentence split and `rules.downedGear` removed; TESTING.md no longer lists Non uno Pinata as not covered; README links the Discord server. `Source/` untouched. The preview tool is gone, so the page was not previewed.
