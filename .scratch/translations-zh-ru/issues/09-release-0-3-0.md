# 09: Release copy and the 0.3.0 bump

**What to build:** The Workshop page and the mod's About.xml tell players that the mod is available in Simplified Chinese and Russian, and that these are machine translations open to corrections. The version moves to 0.3.0, and the release checks pass on the DLL being shipped. See Decisions 5, 6 and 7.

**Blocked by:** 07, 08

**Status:** ready-for-agent

**Recommended model:** `sonnet`: short copy edits, a version bump and running the existing release checks.

## Scope

- **Read first.** `writing-copy.md` from the Templates root and its `template-routing.md` entry for CHANGELOG and the Workshop description. Apply `unslop` to the new lines.
- **steam-description.md.**
  - One Languages line: English, 简体中文 (Simplified Chinese), Русский (Russian).
  - One or two sentences, in or next to the existing "AI?" section, saying that the Chinese and Russian text is machine-translated and inviting corrections.
  - The rest of the page stays English.
- **About.xml.** The description gains the same Languages line. Nothing else in it changes.
- **CHANGELOG.** An "Added" entry: Simplified Chinese and Russian translations of all in-game text, machine-translated, corrections welcome.
  - If auto-config or other work is in the same upload, it shares the 0.3.0 heading.
  - The rule-sentence and fragment changes from tickets 01 and 02 are internal and do not change English, so they get no player-facing line unless the CHANGELOG template routes them under "Internal".
- **Version.** Bump to 0.3.0 once, in all three places together: About.xml `<modVersion>`, the `[b]Mod version:[/b]` line and the newest CHANGELOG heading. If another feature in the same upload already bumped to 0.3.0, do not bump again.
- **Release gates.**
  - `rwtranslate status`: 0 untranslated and 0 outdated for both languages.
  - The Full tier from TESTING.md, including the perf run on this DLL. Tickets 01 and 02 changed the DLL, so a previous perf run does not count.
  - `release-check`.
- **Upload.** The Workshop upload itself is not part of this ticket. Hand it to Kyle with the steam-workshop-utils skill's release route when he asks.

## Acceptance

- [ ] The Workshop description and About.xml each show one Languages line, and the Workshop page states the machine translation and invites corrections.
- [ ] The three version locations read 0.3.0, and the CHANGELOG has the translations entry under that heading.
- [ ] `status` is clean for both languages, the Full tier passes, and `release-check` passes on the DLL to be shipped.
- [ ] Nothing is uploaded.

## Checks

- Both builds and `dotnet test`.
- The Full tier and `release-check` (TESTING.md).
- `rwtranslate status`.

## Comments

2026-11-22, agent: Release copy and version done; status stays ready-for-agent because the in-game gates are not run.

- Done: `steam-description.md` has one Languages line and a machine-translation note with an invitation to correct, in the AI section. The About.xml description has the same Languages line. CHANGELOG `## Unreleased` is now `## 0.3.0 - 2026-11-22` with the translations entry above the auto-config entry. The three version locations read 0.3.0, and the AutoConfig example preset already carried 0.3.0.
- Checks run: both Release builds pass (the 1.7 build ran the translation check, passed), `status` shows 0 untranslated and 0 outdated for ChineseSimplified and Russian, `dotnet test` 291 passed.
- Not run, for Kyle: the Full tier's in-game runs (`run run-all`, `run run-all --mods dlcs`, `run lootscavenging-progression --mods progression`, the removal check, `run perf --mods perf`) and then `release-check`, all on the deployed DLL from tickets 01 and 02. Nothing was uploaded.
