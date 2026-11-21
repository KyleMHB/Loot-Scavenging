# Auto Mod Config support

Status: ready-for-agent

Request (2026-11-21): "add auto config support, check how lumenstone does it." This spec covers support for Auto Mod Config (Continued) (packageId `Garethp.ModlistConfigurator`, Workshop 3586136174), the mechanism Lumen Stones uses. Facts about Auto Mod Config come from `E:\Coding\Rimworld\GAME.md` ("Auto Mod Config (Continued) presets", verified 2026-11-09 against mod version 1.0.4 by reading the source it ships). They have not been tested in game. This spec was written without opening that mod's files.

## Problem Statement

A modlist curator who ships settings for a whole list through Auto Mod Config cannot include Loot Scavenging. Lumen Stones supports this; Loot Scavenging does not.

As the code stands, an imported Loot Scavenging settings file would be discarded on the first launch:

- The saved flag `defaultsSeeded` falls back to `false` when the file has no value for it. A preset file written by hand, or copied from a fresh install's export, does not carry it.
- Once the defs load, `OnDefaultsLoaded` sees an unseeded config and replaces every value with the defaults from the Def. The imported values are lost, and the player sees the Progression preset as if nothing had been imported.
- Loot Scavenging does not load after `Garethp.ModlistConfigurator`. GAME.md says a target mod that loads first reads its old settings before the import and later writes them back over it.
- When a key is missing, the loader uses the compiled-in default, because the settings are read before the defs. So a partial preset ignores defaults a modpack has patched (recorded in ticket 13's comments).

Nothing tells a curator what the settings file is called, which keys it holds or what their values mean.

## How Lumen Stones does it (mechanically)

Lumen Stones has no Auto Mod Config code. Its support is made of four things:

1. **An example preset mod**, `Extras/Lumen-Stones-AutoConfig/`, shipped inside the main mod's folder:
   - `About/About.xml`: packageId `kylemhb.lumenstones.autoconfig`, supports 1.7. It requires Lumen Stones and Auto Mod Config and loads after both. Its description says that importing replaces the saved Lumen Stones settings.
   - `Defs/Preset.xml`: one `ModlistConfigurator.ModlistPresetDef` with a `defName`, a `presetLabel` and a `version`. The version equals the Lumen Stones version the preset was written for, a rule in Lumen Stones' AGENTS.md.
   - `Settings/Mod_Lumen-Stones_LumenstoneMod.xml`: an ordinary `ModSettings` block for `LumenStones.LumenstoneSettings`. It sets `settingsVersion`, one bool and a sparse `overrides` dictionary of eight availability keys, with an XML comment listing every effect-switch id.
2. **What Auto Mod Config does with it** (GAME.md):
   - It copies each file in the preset's `Settings/` folder into the game's Config folder as is, from its own `Mod` constructor.
   - A preset defName it has not seen before overwrites the files. A changed `version` merges three ways against the stored previous preset, so a player's own later changes survive. An unchanged version does nothing.
   - Files are named `Mod_<folder>_<ModClass>.xml`, where `<folder>` is the target's install folder: the local folder name, or for subscribers probably the Workshop id (unconfirmed). A preset that covers both installs ships one file per name.
3. **Main-mod cooperation:**
   - `About.xml` `loadAfter` lists `Garethp.ModlistConfigurator`, so the import happens before Lumen Stones reads its settings.
   - The settings format is sparse: a key left out of `overrides` means "use the active mod list's value". A partial preset is therefore safe.
   - A comment on the settings class says old fields are kept for presets written against older versions.
4. **Packaging and docs:**
   - `tools/deploy.py` lists `Extras` among its runtime folders, so the example is deployed inside the main mod.
   - The game never loads it there: RimWorld finds mods only at the top level of the Mods folder, and `LoadFolders.xml` does not name `Extras`.
   - The README and `docs/stone-settings.md` tell the player to copy the folder into `RimWorld/Mods` as a separate mod. They also give the file name, the format, a table of every key, and say that importing replaces saved settings.
   - There is no second Workshop item. The Workshop art says "Supports autoconfig"; the Steam description does not mention it.

## What Loot Scavenging already has, and the gap

Ticket 13, done, provides the **pack preset path**:

- `Defs/SettingsDefaults.xml` holds one `LootScavenging.SettingsDefaultsDef` (`LS_SettingsDefaults`): the full Progression values plus a `locked` list.
- A pack changes defaults by patching that Def, and locks rows by adding names from `SettingLocks.Lockable`.
- The defaults seed a config only once (`defaultsSeeded`). On every later load, locked settings are put back to the pack's value. The page draws locked rows disabled, with `LootScavenging_LockedReason`, and shows `LootScavenging_LockedSummary` in the presets section (commit 84c60f2).
- `packs/progression/ProgressionPreset.xml` is the Progression pack's patch, for the pack to copy into its own `Patches/`. `deploy.ps1` and `DevTests/devtests.json` copy `packs/`, and the game never loads it.

The two mechanisms do different jobs:

| | Pack preset (exists) | Auto Mod Config (this spec) |
| --- | --- | --- |
| Who writes it | A modpack author, as XML patches | A modlist curator, as a settings file |
| What it changes | Defaults, the baseline for Reset and the presets, and locks | The player's saved values |
| When | Every load (the Def), seeding once | Auto Mod Config's first import, and merges on a version change |
| Can lock | Yes | No |
| Needs another mod | No | Auto Mod Config |

The gap is everything in the Problem Statement:

- seeding wipes an import;
- no `loadAfter`;
- missing keys take compiled-in values;
- no example preset and no documented key list.

## Design

Chosen design (accepted 2026-11-21): **follow Lumen Stones. Ship an example preset folder with no code, and make the main mod accept an imported file.** Do not build a companion mod with code. Do not merge Auto Mod Config into the pack-preset path. Reasons:

- Lumen Stones' "companion mod" is only data: an About, a Def that Auto Mod Config defines, and a settings file. Auto Mod Config does the importing. Code of Loot Scavenging's own in a companion mod would duplicate it.
- The work that matters lives in the main mod in any case: the seeding rule, the missing-key baseline and `loadAfter`. No companion mod can fix those.
- The pack preset stays the way to set defaults and locks. Auto Mod Config writes values. They stack cleanly: the Def sets the baseline, Auto Mod Config writes the values, and locks win over both. Folding them together would give a curator two ways to set one value.

Where the example ships is Decision 3.

## Solution

A curator puts a Loot Scavenging settings file in an Auto Mod Config preset. On the first launch with that preset, the player's Loot Scavenging settings are the file's values: either a whole config or a few keys over a named preset. A pack's locked settings still hold the pack's values. The player can change any unlocked setting afterwards, and those changes stay unless the curator ships a new preset version, in which case Auto Mod Config merges. Players without Auto Mod Config notice no change.

## User Stories

1. As a modlist curator, I want Loot Scavenging to accept a settings file imported by Auto Mod Config, so that my list ships with the loot rules I chose.
2. As a modlist curator, I want a preset that names only the keys I change, so that I need not copy every setting and my file does not go stale when the mod adds settings.
3. As a modlist curator, I want the file's `activePreset` to set which preset the page shows, so that the page names the preset my list is based on.
4. As a modlist curator, I want the settings file name, every key and every value written down, so that I do not have to reverse-engineer a config.
5. As a modlist curator, I want an example preset folder to copy, so that I start from a file known to work.
6. As a modlist curator, I want Loot Scavenging to load after Auto Mod Config, so that the import is not overwritten by the mod's own earlier read.
7. As a modpack author, I want my locked settings to keep my values even when a curator's file sets them, so that the pack's balance holds.
8. As a modpack author, I want keys missing from the curator's file to take my patched defaults, not the compiled-in ones, so that both tools compose.
9. As a player on a curated list, I want my own later changes kept on later launches, so that the curator's preset is a starting point, not a cage.
10. As a player on a curated list, I want a new preset version to merge with my changes, not replace them, so that a list update does not reset my tweaks. (Auto Mod Config's behaviour; Loot Scavenging must not undo it.)
11. As a player, I want the settings page to describe the imported settings truthfully, so that I am not told I am on an unchanged preset when I am not. (Decision 5: the existing wording stays.)
12. As a player, I want the first-load message not to contradict the imported settings, so that I am not told the mod runs Progression when the curator chose Cleanup. (Decision 6: unchanged; a curator can set `firstLoadMessageShown`.)
13. As a player without Auto Mod Config, I want nothing to change, so that a fresh install still starts on Progression with the one-time message.
14. As a player upgrading from 0.2.0, I want my saved settings kept, so that this change does not reset me.
15. As a player who removes Auto Mod Config or the preset mod, I want my settings to stay as they are, so that removing a helper does not reset the mod.
16. As a player on RimWorld 1.6 or 1.7, I want the support to work on both, so that the version does not matter.
17. As a translator, I want every new player-facing line in the Keyed file, so that the zh-Hans and ru translations can cover it.
18. As the maintainer, I want the merge rule in `Source/Model/` under unit tests, so that a new setting cannot break partial imports unnoticed.
19. As the maintainer, I want one in-game check that reads a hand-written partial file through the game's own settings loader, so that the Scribe side is covered without new infrastructure.

## Behaviour

**What gets configured.** Every saved setting: each key in `SettingKeys.All`, plus `activePreset` and `firstLoadMessageShown`. Keys keep their current saved names (`weaponsOutcome`, `corpseHumanlike`, `dropMode` and so on), so the example and the docs list them as they appear in a saved config. The keep and remove lists can be imported but not locked, as today.

**When it is applied.** Auto Mod Config decides, as GAME.md describes: on the first sight of a preset defName, on a version change (merged three ways), or never. Loot Scavenging adds no timing of its own. It must only not undo an import, so the main mod changes in three ways:

1. **Seeding.** A config counts as seeded when its file holds any setting key, `activePreset` or `firstLoadMessageShown`, even without `defaultsSeeded`. When the step keeps such a file, it sets `defaultsSeeded` to true. Only a config with no file, or a file holding none of those keys, takes the Def defaults and the Progression preset. This is Decision 1. Side effect: a pre-0.2.0 config without the flag is now kept rather than reset once.
2. **Missing keys.** While loading, the settings record which keys the file held. They keep that set in memory until the defs load. In `OnDefaultsLoaded`, every key the file lacked is filled from a baseline. **Rule: a missing key is filled with the same value the save compared against.**
   - Why: `Scribe_Values.Look` does not write a value equal to its default (confirm against the game). When the game saves, the defs have loaded, so the default it compares against is the Def's Progression. So an ordinary saved config already leaves out every key that matches the Def's Progression.
   - So the baseline is the Def's Progression (Decision 2). This replaces the compiled-in fallback for every config, upgrades included, and closes the gap ticket 13 noted.
   - Filling from the file's `activePreset` instead, the rejected option, would silently revert a Cleanup player's edit that happens to equal Progression's value. That option is only safe if saving also compares against `Build(activePreset)`.
   - A curator who writes "Cleanup plus two keys" therefore lists Cleanup's rows too, or writes the full file. The example's comment says so.
3. **Load order.** `About/About.xml` `loadAfter` adds `Garethp.ModlistConfigurator`. This is not a dependency.

**User overrides and locks.**
- Locks are enforced last in `OnDefaultsLoaded`, as now. A locked key in an imported file is overwritten with the pack's value on every load and logs nothing. The page shows the row locked as usual.
- After import, the player's edits are ordinary saved values. Reset and the preset buttons behave as today: Reset returns to the Def's Progression, not to the curator's file.
- "(changed)" is worked out against `activePreset` as today. Decision 5: nothing more is shown for an import.

**Persistence.** Everything stays in the normal global mod settings file. Nothing is written to saves. The set of present keys lives only in memory, from load until defaults are applied. No new saved key is needed.

**Removal.**
- Removing the preset mod or Auto Mod Config leaves the Config file as it is, so the settings keep the imported values (plus the player's edits).
- Removing Loot Scavenging is unchanged by this spec, since the work adds no saved things or defs.
- If Auto Mod Config's manual Reset, Merge or Check-status buttons run after the Loot Scavenging config file was deleted, they throw (GAME.md). That is Auto Mod Config's bug: document it, do not work around it.

## Implementation Decisions

- **Model (plain C#, unit-tested):** one function on the settings model fills a loaded `SettingValues` from a baseline, for every saved key not in a given set of present keys. It walks `SettingKeys.All` and uses `ValueWalk.CopyPath`. It takes key names or field paths, chosen by the implementer. It knows nothing of Scribe. Seeding becomes a pure rule: "seeded if the flag is set, or if any setting key, `activePreset` or `firstLoadMessageShown` was present".
- **Settings class:** during `LoadingVars` it notes which keys the file held, from the Scribe loader's current XML node. In `OnDefaultsLoaded` it applies the seeding rule, then the fill from the baseline, then the locks, then the market value, in that order.
- **About.xml:** add the `loadAfter` entry.
- **Example preset (Decision 3, at `Extras/Loot-Scavenging-AutoConfig/`):** a folder with an `About/About.xml`, a `Defs/` file with one `ModlistConfigurator.ModlistPresetDef`, and a `Settings/` folder with one settings file per install-folder name.
  - packageId `kylemhb.lootscavenging.autoconfig`. Name in the form `Loot Scavenging - AutoConfig preset`.
  - `supportedVersions` 1.6 and 1.7. It has no assemblies, so it needs no `LoadFolders.xml`.
  - `modDependencies`: `kylemhb.lootscavenging` and `Garethp.ModlistConfigurator`, with `loadAfter` both.
  - Its description says that importing replaces saved Loot Scavenging settings, as Lumen Stones' does.
  - The settings file names the class `LootScavenging.LootScavengingSettings` and holds `activePreset` plus a short example set of keys. A comment lists every key and value; the full table goes in the docs.
- **Settings file name: unverified.**
  - It follows the pattern `Mod_<folder>_LootScavengingMod.xml`. The local deploy folder is `Loot Scavenging`, with a space. Whether the game keeps or changes the space in the file name has not been checked.
  - The implementer confirms the name by reading the file a local launch writes, before naming the preset file.
  - The Workshop-id copy cannot be written until the first upload gives an id. `About/` has no `PublishedFileId.txt` yet.
- **Preset `version`:** follow Lumen Stones. It equals the Loot Scavenging version the preset was written for, and changes only when the preset's content changes, not every release. If adopted, the AGENTS.md "Versioning" section gains a note that this is a separate, rarely-bumped value. It is not a fourth place that moves with every upload.
- **Packaging:**
  - The example lives under `Extras/` (Decision 3), so add `Extras` to the `deploy.ps1` folder list and to `DevTests/devtests.json` `runtimeFolders` together (the comment in `deploy.ps1` requires it), and add `Extras/` to the AGENTS.md Layout list.
  - In both cases `LoadFolders.xml` is unchanged: the game must never load the example from inside the main mod.
  - No second Workshop item (Decision 4).
- **Docs:**
  - README gains a short Auto Mod Config paragraph under the settings text: optional, where the example is, copy it as a separate mod, importing replaces settings, locks still win.
  - The key table lives in the header comment of `Defs/SettingsDefaults.xml`, or in a new `docs/settings-keys.md` if that comment grows too long (implementer's call; prefer the existing comment).
  - `steam-description.md` gains one sentence in its compatibility area. Follow `writing-copy.md`.
  - CHANGELOG gets an "Added" line in the next release. A new integration is a minor bump, done once per upload per AGENTS.md, not by this work.
- **Text:**
  - There is **no new Keyed string** (Decisions 5 and 6). The behaviour is silent, and the existing `LootScavenging_LockedReason` and `LootScavenging_LockedSummary` cover locked rows.
  - The example's `About.xml` description is English metadata, not Keyed.
  - `presetLabel` is a field on Auto Mod Config's def. Whether DefInjected can translate it is unverified; the translations spec should check before relying on it.

## Testing Decisions

A good test checks what a player or curator sees: the values after loading a given file under given defaults and locks. It does not check how they got there. The existing seams are enough, and no new infrastructure is needed.

- **Unit (`dotnet test`, `Tests/LootScavenging.Tests`):** extend the style of `PackDefaultsTests` and `SettingKeysTests`.
  - A file with two keys keeps those keys and takes the Def's Progression for the rest.
  - Missing keys take a patched Def default, not the compiled-in one.
  - A locked key in the file ends at the pack value.
  - A file holding none of the keys counts as unseeded. Any setting key, or `activePreset` alone, makes it seeded.
  - Every key in `SettingKeys.All` can be filled, so a new setting is covered automatically.
  - Everything stays in `Source/Model/` with no RimWorld references.
- **In game (DevTests `lootscavenging-settings` batch):** one new case alongside `missing-def-list-entry`, which already writes and reads through `LoadedModManager.WriteModSettings`/`ReadModSettings`.
  - Write a hand-made partial settings file, as Auto Mod Config would leave it: `activePreset` plus two keys, no `defaultsSeeded`.
  - Read it through the game's loader, run the defaults step, and observe the two keys, one baseline-filled key and one locked key.
  - Round trip: save a Cleanup config with one key set to Progression's value, read it back, and run the defaults step. The key must keep its value. Only the game's loader can catch the omit-on-default bug, so this check has to run in game.
  - Tier: quick (smoke, settings, ui), per TESTING.md's settings row. Add the case to TESTING.md's settings table.
- **Not automated:** a real Auto Mod Config import. Auto Mod Config is not in any DevTests mod set, and adding it would be new infrastructure. Leave it as a manual check in the end-of-work review: install the example as a separate mod with Auto Mod Config, launch, read the page, change a value, relaunch, and bump the preset version. Include one curator key whose value equals the default: Loot Scavenging's own rewrite drops it (the first-load message calls `Settings.Write()`), so check that the three-way merge still behaves. Record the check in TESTING.md when it is done.
- **Before the docs claim support:** confirm Auto Mod Config runs on 1.7. GAME.md read its `1.6/Source`, and Lumen Stones uses it on 1.7, but that has not been checked here.
- Both builds compile, and `rwtranslate check` passes after the docs and any strings change.

## Out of Scope

- A companion mod with code, or importing or exporting settings files from Loot Scavenging's own page.
- Locking through Auto Mod Config. Locks stay with the pack-preset Def.
- Detecting the Progression pack and applying `packs/progression` automatically (see Decision 7).
- Changing saved key names, or making the keep and remove lists lockable (ticket 12's area).
- Working around Auto Mod Config's NullReferenceException on missing files.
- zh-Hans and ru text: the translations spec that follows covers them.
- Publishing the example to the Workshop.

## Decisions

Kyle accepted every recommendation on 2026-11-21:

1. **Recognising an imported file:** a file holding any saved key (a setting key, `activePreset` or `firstLoadMessageShown`) counts as seeded.
2. **Filling missing keys:** from the Def's Progression, the same value the save compares against.
3. **Example preset:** ships at `Extras/Loot-Scavenging-AutoConfig/`, so `deploy.ps1` and `DevTests/devtests.json` gain `Extras`.
4. **Workshop:** no second Workshop item. The example ships inside the main mod and players copy it out.
5. **Settings page:** unchanged after an import ("Active preset: X (changed)"). No new string.
6. **First-load message:** unchanged. The example's comment says a curator can set `firstLoadMessageShown`.
7. **Progression pack detection:** detecting the pack and applying `packs/progression` automatically is out of scope.
8. **Example content:** `activePreset` Progression only, with every key listed in a comment.
9. **Seams:** a unit-tested model merge, the DevTests settings cases, and the real import checked by hand.

So no new Keyed strings are needed. The conditional strings drafted earlier (`LootScavenging_ImportedSettings`, `LootScavenging_PresetCustom`, `LootScavenging_FirstLoadTextImported`) are dropped.

## Open Questions

There are no open design choices. Three facts are unconfirmed, and each one has a check:

1. **Omit-on-default save.** Before relying on the "fill with the value the save compared against" rule, confirm that `Scribe_Values.Look` leaves out a value equal to its default. Check it either by reading the method with the Lumen Stones `ildump` tool (the GAME.md method) or with the DevTests round-trip case. If it writes every value, the fill rule is still correct, but the round-trip case loses its reason to exist.
2. **Settings file name.** Read the file name the game writes for Loot Scavenging after a local launch, before naming the example's settings file. The deploy folder has a space in its name. The Workshop-id copy can only be added after the first upload gives an id.
3. **Auto Mod Config on 1.7.** In the hand-run import check, confirm that Auto Mod Config loads and imports on RimWorld 1.7. GAME.md's notes come from its `1.6/Source`. Until that check passes, the docs do not name a game version for the import.

## Further Notes

- Overlapping work: ticket 13 (`.scratch/loot-scavenging/issues/13-pack-preset-and-locks.md`) built the seeding flag, locks and pack preset this spec changes, and its comment records the missing-key limitation. Ticket 12 owns the Lists section, which stays unlockable. The spec's "The Progression pack" section is the context for locks.
- Lumen Stones references: `Extras/Lumen-Stones-AutoConfig/`, `docs/stone-settings.md` ("Auto Mod Config"), README, `tools/deploy.py` `RUNTIME_FOLDERS`, AGENTS.md versioning rule.
- Once the settings file name is confirmed in game (local folder, and the Workshop id after the first upload), add it to GAME.md's Auto Mod Config section with source and date.
