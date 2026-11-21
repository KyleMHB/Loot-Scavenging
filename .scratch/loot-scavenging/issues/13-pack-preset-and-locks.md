# 13: Pack preset and setting locks

**What to build:** The default settings ship as XML that a pack can patch, with an option to lock chosen settings so the page shows them but the player cannot change them. The Progression pack gets its own XML preset: the Progression defaults, with era-typical yields for instant disassembly. See the spec's "The Progression pack" section.

**Blocked by:** 11

**Status:** ready-for-agent

**Recommended model:** `sonnet` — A narrow slice that follows patterns set by earlier tickets.

- [ ] A test patch changes a default, and the page shows the patched value on a fresh config. (mapping unit-tested; the page check is in-game, pending)
- [ ] A locked setting cannot be changed on the page, and the page says why. (lock logic unit-tested; the disabled row and reason line are in-game, pending)
- [x] The pack preset sets disassembly to era-typical yields.
- [x] Both the 1.7 and 1.6 builds compile.

## Comments

### 2026-11-21: implemented on ls/13

**Built:**
- `Defs/SettingsDefaults.xml`: one `LootScavenging.SettingsDefaultsDef` (`LS_SettingsDefaults`). `values` is a `SettingValues` written out in full (the Progression preset); `locked` is a list of setting names, empty by default. Patch examples and the lockable names are in its header comment. `Source/SettingsDefaultsDef.cs` holds the Def and `SettingsDefaults`, a `[StaticConstructorOnStartup]` that reads it and calls `Presets.Configure`.
- `Source/Model/SettingLocks.cs`: `SettingLocks` (dotted field paths such as `rules.dropMode`, `Lockable` list, `Enforce` copies the pack's value over a locked field) and `PresetConfig` (Progression values plus locks). Locks live outside `SettingValues`, as ticket 11 asked.
- `Source/Model/Presets.cs`: every method now has an overload taking a `PresetConfig` (tests pass their own; no shared state). `Build(Progression)` is a clone of the Def values. Cleanup and Vanilla clone it too, then set every row of the presets table (all seven categories, corpses, groups, moment, drop mode), so a patched default flows into them without leaking into rows they own. Locks are enforced inside `Build`, so Apply, Reset and the "(changed)" check all agree and a locked setting never shows as changed. The compiled-in values stay as `CodeProgression()`, the fallback when no Def exists.
- `Source/LootScavengingSettings.cs`: new saved flag `defaultsSeeded`. The mod constructor runs before defs load, so `OnDefaultsLoaded()` (called from the startup hook) seeds a config that has not been seeded with the Def defaults and the Progression preset, and otherwise only puts locked settings back to the pack value. It also re-applies the scrap market value.
- `Source/SettingsUI/SettingsPage.cs`: locked rows are drawn disabled, with the reason line `LootScavenging_LockedReason` under them; the presets section adds a summary line when anything is locked. `EnforceLocks()` runs at the end of each draw as a backstop (the degrade range widget reads the mouse itself). A locked category disables its whole block. The Lists section is untouched (ticket 12), so the keep and remove lists and the four protection toggles are not lockable; they can still be defaulted in the Def.
- `packs/progression/ProgressionPreset.xml`: the Progression pack's preset as a patch, outside `Defs/` and `Patches/` so it is not active. It sets `disassemblyYield` to `EraTypical` and locks it. The Progression defaults already use era-typical yields and instant-disassembly settings; the file makes that explicit and locked. The pack's own benches and materials per era are unverified (spec "Open questions"), so it patches no tier table entries; its header says so. `deploy.ps1` now copies `packs/` too.

**Decisions the spec left open:**
- A config saved before this ticket has no `defaultsSeeded`, so it is reset to defaults once. Pre-release only.
- A key missing from an older saved config still takes the compiled-in default, because the Def is read after the settings load.
- Locked settings hold the Def value, not the player's saved one.
- The pack preset is a patch file rather than a second Def, since a second Def would give two defaults for the same settings.

**Tests:** `PackDefaultsTests` (patched default reaches a fresh config and Progression, is not "(changed)", carries into Cleanup and Vanilla without changing their table rows, locks survive presets, enforcement, unknown lock names, every lockable name is a real field) and `ShippedDefaultsTests` (the XML equals the compiled Progression preset; the pack patch only locks valid names and sets era-typical). `dotnet test`: 236 passed. Both builds succeed with no warnings; `rwtranslate check` prints `translations: passed`.

**In-game: pending.**
- Fresh config: delete `Config/Mod_LootScavenging_*`, start; the settings show Progression, not "(changed)".
- Test patch: put a patch changing `values/rules/dropMode` (or `scrapMarketValue`) into a test mod's `Patches/`; on a fresh config the page shows the patched value, not "(changed)"; Reset returns to it.
- Lock: copy `packs/progression/ProgressionPreset.xml` into a test mod's `Patches/`. The Disassembly yield row is disabled with the "Locked by the modpack" line, picking another preset leaves it at era-typical, and the presets section shows the summary line.
- Add a bad name (`rules.nope`) to `locked`: one config error naming it in the log, no crash.
