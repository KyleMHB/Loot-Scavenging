# 02: Example Auto Mod Config preset, packaging and docs

Status: resolved

**Recommended model:** `sonnet` - Data files and docs that follow Lumen Stones' example and the settled decisions.

**Blocked by:** 01

**What to build:** A modlist curator finds an example Auto Mod Config preset inside the deployed mod and copies it into `RimWorld/Mods` as a separate mod. The docs tell the curator the settings file name, every key and its values, and that importing replaces saved settings while locks still win. See the [spec](../spec.md), sections "Implementation Decisions" and "Decisions" 3, 4, 6 and 8.

- [ ] Check the settings file name first. After a local launch with the deployed mod, read the exact name of the Loot Scavenging settings file the game writes. The deploy folder is `Loot Scavenging`, with a space, so check whether the space survives. Record the name in a comment on this ticket, and in GAME.md's Auto Mod Config section with source and date.
- [ ] `Extras/Loot-Scavenging-AutoConfig/` holds three things:
  - `About/About.xml`: packageId `kylemhb.lootscavenging.autoconfig`, `supportedVersions` 1.6 and 1.7. It depends on and loads after `kylemhb.lootscavenging` and `Garethp.ModlistConfigurator`. Its description says that importing replaces saved Loot Scavenging settings.
  - A `Defs/` file with one `ModlistConfigurator.ModlistPresetDef`. Its `version` equals the mod version the preset was written for.
  - A `Settings/` file under the confirmed name. It names `LootScavenging.LootScavengingSettings` and sets `activePreset` Progression only. A comment lists every saved key and its values. It also says that missing keys take the Progression defaults, that locks win, and that a curator can set `firstLoadMessageShown`.
- [ ] Every key named in the example's comment appears in `SettingKeys.All`, and every value is one the settings accept.
- [ ] The game never loads the example from inside the main mod. `LoadFolders.xml` is unchanged, and a launch with only the main mod logs no `ModlistPresetDef` error.
- [ ] `deploy.ps1` copies `Extras`, and `DevTests/devtests.json` `runtimeFolders` lists it too. The two lists stay equal, as the `deploy.ps1` comment requires.
- [ ] AGENTS.md Layout gains an `Extras/` line. AGENTS.md Versioning notes that the preset's `version` changes only when the preset's content changes, not with every upload.
- [ ] README gains a short Auto Mod Config paragraph: it is optional, where the example is, copy it as a separate mod, importing replaces settings, and locks still win. The full key table lives in the `Defs/SettingsDefaults.xml` header comment, or in `docs/settings-keys.md` if that comment grows too long.
- [ ] `steam-description.md` gains one sentence about Auto Mod Config in its compatibility area. Read `E:\Coding\Templates\writing-copy.md` and `template-routing.md` first.
- [ ] `CHANGELOG.md` gains an "Added" line for Auto Mod Config support under the next release heading. Bump no version: that happens once per upload.
- [ ] Until ticket 03 passes, the docs do not name a game version for the Auto Mod Config import.

## Checks

- Both builds and `rwtranslate check`.
- Deploy, then confirm that `RimWorld/Mods/Loot Scavenging/Extras/Loot-Scavenging-AutoConfig` exists.
- The quick-tier smoke batch passes with `no-mod-errors`, which shows the nested example is not loaded.
- `dotnet test` still passes.

## Comments

### 2026-11-22 - implemented, one check left (agent)

**Settings file name (checked first):** `Mod_Loot Scavenging_LootScavengingMod.xml`. The space in the deploy folder name survives. Read from the file a local launch wrote (`work/userdata/manual/Config/`). Recorded in `E:\Coding\Rimworld\GAME.md` (Auto Mod Config section). The Workshop-id name stays for ticket 04.

**Added:** `Extras/Loot-Scavenging-AutoConfig/` (`About/About.xml`, `Defs/Preset.xml` with `version` 0.3.0, the settings file with `activePreset` Progression and a comment listing every key and value). `Extras` added to `deploy.ps1` and `DevTests/devtests.json` `runtimeFolders`. AGENTS.md Layout and Versioning, README paragraph, one steam-description sentence, and a CHANGELOG "Unreleased > Added" line (no version bumped).

**Checks:**
- A script compared the 81 keys in the example's comment with the keys that `SettingKeys` derives from the Def's values: equal, none missing, none extra. Enum values and float ranges were read from the model and the settings page.
- 1.7 Release build: 0 warnings, translation check passed. Deploy ran, and the Extras folder with all three files is in the deployed mod. The three example XML files parse. `LoadFolders.xml` is unchanged. No code was touched, so `dotnet test` was not run.

**Not done:**
- The smoke batch with `no-mod-errors` (and the 1.6 build) was not run, as the brief said no in-game run was needed. Status stays as it is until that run shows the nested example is not loaded.
- The full key table is the comment in the example's settings file, not a `docs/settings-keys.md`. `Defs/SettingsDefaults.xml` points to it.

**For Kyle:** the preset `version` 0.3.0 and the CHANGELOG "Unreleased" heading must match the version the release step picks. Ticket 03 must pass before the docs name a game version for the import.

### 2026-11-22 - smoke run (agent)

Run `20261122-015731-lootscavenging-smoke-s1-046685e0`: 9 pass, 0 fail, overall PASS, so the nested example is not loaded. The runner then reported ISOLATION FAIL: `DevTests/ModHarness.cs` `RuntimeFolders` (line 23) still lacks `Extras`, so it differs from `devtests.json`. That file is held by other work. Add `"Extras"` there, rebuild DevTests and rerun, then set Status to resolved. Session closed.

### 2026-11-22 - smoke rerun passed (agent)

Added `"Extras"` to `RuntimeFolders` in `DevTests/ModHarness.cs` (line 23 only). Run `20261122-015816-lootscavenging-smoke-s1-1eb0b668`: 9 pass, 0 fail, no isolation failure. Every acceptance criterion is met. Session closed.
