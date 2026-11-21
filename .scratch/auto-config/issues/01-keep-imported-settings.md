# 01: Keep an imported settings file on load

Status: resolved

**Recommended model:** `opus` - This changes the load order of seeding, the missing-key fill and lock enforcement in the settings class, where a mistake silently loses a player's settings.

**Blocked by:** None (can start immediately).

**What to build:** A Loot Scavenging settings file written by someone else, such as an Auto Mod Config preset, survives the first launch. It may hold only some keys and no `defaultsSeeded`. A file holding any saved key is kept, and the flag is then set. Keys the file lacks take the Def's Progression value, so a pack's patched default applies, not the compiled-in one. Locked settings still end at the pack's value. Loot Scavenging loads after Auto Mod Config. A fresh install with no file behaves as today. See the [spec](../spec.md), sections "Behaviour" and "Decisions" 1, 2 and 9.

- [x] Check the save rule first: confirm whether `Scribe_Values.Look` leaves out a value equal to its default. Read the method with the Lumen Stones `ildump` tool, as GAME.md describes, or rely on the round-trip case below. Record the answer in a comment on this ticket. Add it to `E:\Coding\Rimworld\GAME.md` only with its source, version and date.
- [x] A file holding `activePreset` plus two setting keys, with no `defaultsSeeded`, loads with those values. Every other key holds the Def's Progression value. `defaultsSeeded` is true after the defaults step.
- [x] A file holding only `activePreset` counts as seeded and is not reset.
- [x] A missing key takes a patched Def default, not the compiled-in value. This applies to an upgraded config that already has `defaultsSeeded`, too.
- [x] A locked key set in the file ends at the pack's value, and the page draws that row locked.
- [x] With no config file, a fresh launch still starts on Progression, unchanged, and shows the first-load message once.
- [x] Round trip: a Cleanup config with one key set to Progression's value saves, reads back and passes the defaults step with that key unchanged.
- [x] `About/About.xml` `loadAfter` lists `Garethp.ModlistConfigurator`. It is not added to `modDependencies`.
- [x] The merge and seeding rules live in `Source/Model/` with no RimWorld references. The settings class only records which keys the file held, during loading, and calls the rules in `OnDefaultsLoaded`. The order is seed, fill, locks, market value.
- [x] The `Defs/SettingsDefaults.xml` header comment and the comment in the settings class no longer say that missing keys take the compiled-in default.

## Checks

- `dotnet test`: new unit tests in the style of `PackDefaultsTests` and `SettingKeysTests`:
  - partial file plus Def baseline;
  - a patched Def default fills a missing key;
  - a locked key in the file;
  - no keys counts as unseeded, while `activePreset` alone counts as seeded;
  - every key in `SettingKeys.All` can be filled.
- Both builds: `dotnet build Source/LootScavenging.csproj -c Release`, and the same with `-p:RimWorldVersion=1.6`.
- `rwtranslate check` passes. It runs after the build.
- Two new cases in the `lootscavenging-settings` batch, written like `missing-def-list-entry`, which uses `LoadedModManager.WriteModSettings`/`ReadModSettings`:
  - a hand-written partial file;
  - the Cleanup round trip.
- Add both cases to the settings table in TESTING.md.
- Quick tier: deploy, then `python tools/ls_test.py run lootscavenging-smoke lootscavenging-settings lootscavenging-ui --seed 1`, plus `lootscavenging-firstload` because the startup path changes, then `close-session`.
- No CHANGELOG line here. Ticket 02 adds the one entry for the feature.

## Comments

### 2026-11-22 - implemented (agent)

**Save rule (checked first):** confirmed. `Verse.Scribe_Values.Look` writes nothing unless `forceSave` is set or the value differs from its default (`Equals`). On loading, a missing node takes the default passed at load time. I read this with Lumen Stones `tools/ildump` on the 1.7 `Assembly-CSharp.dll` (1.7.5014 rev1195). The `cleanup-round-trip` case then saw it in game: the saved file had no `<dropMode>`. It is recorded in `E:\Coding\Rimworld\GAME.md` ("Mod settings omit default values"). It is not checked on 1.6, because the reference assemblies have no method bodies. The ticket's assumption holds.

**Changed:**
- `Source/Model/SettingsLoad.cs` (new, plain C#): `IsSeeded`, `FillMissing`, `Baseline` (the Def's Progression, which is also the save's default) and `Resolve` (seed, fill, locks).
- `Source/LootScavengingSettings.cs`: during `LoadingVars` it records which keys the file held. `OnDefaultsLoaded` calls `SettingsLoad.Resolve`, sets the flag, clears the key set and then applies the market value. A later call, such as the `locked-setting` case, only enforces locks. The comments are updated.
- `About/About.xml` `loadAfter` now lists `Garethp.ModlistConfigurator`. `Defs/SettingsDefaults.xml` header comment updated.
- `Tests/LootScavenging.Tests/SettingsLoadTests.cs` (10 tests).
- `DevTests/SettingsBatch.cs` gains `imported-partial-file` and `cleanup-round-trip`. Both read under the compiled-in config and run the defaults step under a patched one, so a fill that did nothing would fail. Both are in TESTING.md.

**Checks:**
- `dotnet test`: 291 passed.
- 1.7 and 1.6 Release builds: 0 warnings. Translation check passed after each build.
- Deployed, then firstload, smoke, settings (10/10) and ui (6/6) in one launch: all passed. Then `close-session`. The runs are listed in TESTING.md.

**Left for Kyle:**
- Nothing is committed.
- The deployed mod now matches this working tree, which includes the uncommitted work from other sessions.
- Per the spec, a pre-0.2.0 config without the flag is now kept rather than reset once.
