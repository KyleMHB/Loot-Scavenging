# Changelog

<!-- Agent read route: read only the newest relevant release; locate older versions by searching `^## ` headings. -->

## 0.4.0 - 2026-11-23

### Added

- You can now set the research points each era's scrap pays when studied, and scale them all with one slider. Setting an era to 0 stops its scrap being studied. Modpacks can set and lock these values too.

### Changed

- Research points per unit moved from the scrap item defs into the settings. A patch on a scrap def's `StudyPoints` extension no longer has any effect; patch `studyPoints<Era>` in `Defs/SettingsDefaults.xml` instead.

## 0.3.1 - 2026-11-23

### Added

- The Auto Mod Config example in `Extras/Loot-Scavenging-AutoConfig` now also carries a settings file named for the Workshop copy, so a preset can cover subscribers as well as local installs.

### Changed

- Replaced the preview image with a title card showing the six scrap items.

## 0.3.0 - 2026-11-22

### Added

- Added Simplified Chinese and Russian translations of all in-game text. They are machine translations, and corrections are welcome.
- Added support for Auto Mod Config (Continued). An imported settings file is kept, keys it leaves out take the default settings, and locks still apply. An example preset is in `Extras/Loot-Scavenging-AutoConfig`.

## 0.2.0 - 2026-11-21

### Added

- Added the loot rules: gear of non-colony pawns is sorted when they die and, optionally, when they are downed or stripped, by owner group, map, protection, item category, conditions and chance.
- Added six scrap items, neolithic to archotech, sized by the value, quality and condition of the gear they came from.
- Gave each scrap era its own art, with a bigger pile as the stack grows. Studied scrap uses the same art, darker.
- Added recycle bills at the crafting spot, smithies, machining table and fabrication bench, which turn scrap into materials picked from a weighted pool for its era.
- Added study bills at research benches, which pay a fixed number of points per unit into the current project and say on the scrap why it cannot be studied.
- Added four drop modes: scrap out-of-reach gear, scrap everything, keep the original item as unfamiliar gear that cannot be used until researched, and off.
- Added the degrade, disassemble and delete outcomes, with era-typical or item-based disassembly yields.
- Added protection for quest items, unique weapons, persona weapons and relics, and always-keep and always-remove lists picked from a searchable item menu.
- Added optional corpse removal for humanlike, animal and mechanoid corpses, and an option to destroy worn apparel when a corpse is butchered.
- Added the settings page with the Progression, Cleanup and Vanilla presets and a one-time message on first load.
- Added patchable XML for the scrap tiers and default settings, with locks a modpack can set, and a preset patch in `packs/progression` for the Progression pack to copy into its own Patches folder.
- Exempted medicine from the drop mode by default, with a setting to include it.
- Added a choice of where loot lands at death: on the ground, scrap on the body, or everything on the body. Mechanoids and other pawns that cannot be stripped always drop on the ground.
- With VFE Tribals or Node Research active, as in the Progression pack, the colony's era is the era those mods advance it to. Gear and scrap of a new era are within reach from the advance on.
- Every setting a modpack locks shows a line saying so on the settings page.

## 0.1.0 - 2026-11-20

### Internal

- Set up the project: mod metadata, load folders, a build for RimWorld 1.6 and 1.7, and the deploy script.
- Added the design spec at `.scratch/loot-scavenging/spec.md`.
