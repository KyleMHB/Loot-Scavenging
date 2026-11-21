# Changelog

<!-- Agent read route: read only the newest relevant release; locate older versions by searching `^## ` headings. -->

## 2026-11-21

### Added

- Added the loot rules: gear of non-colony pawns is sorted when they die and, optionally, when they are downed or stripped, by owner group, map, protection, item category, conditions and chance.
- Added six scrap items, neolithic to archotech, sized by the value, quality and condition of the gear they came from.
- Added recycle bills at the crafting spot, smithies, machining table and fabrication bench, which turn scrap into materials picked from a weighted pool for its era.
- Added study bills at research benches, which pay a fixed number of points per unit into the current project and say on the scrap why it cannot be studied.
- Added four drop modes: scrap out-of-reach gear, scrap everything, keep the original item as unfamiliar gear that cannot be used until researched, and off.
- Added the degrade, disassemble and delete outcomes, with era-typical or item-based disassembly yields.
- Added protection for quest items, unique weapons, persona weapons and relics, and always-keep and always-remove lists picked from a searchable item menu.
- Added optional corpse removal for humanlike, animal and mechanoid corpses, and an option to destroy worn apparel when a corpse is butchered.
- Added the settings page with the Progression, Cleanup and Vanilla presets and a one-time message on first load.
- Added patchable XML for the scrap tiers and default settings, with locks a modpack can set, and a preset patch in `packs/progression` for the Progression pack to copy into its own Patches folder.
- Exempted medicine from the drop mode by default, with a setting to include it.
- Added a choice of where loot lands at death: on the ground, scrap on the body, or everything on the body.

## 2026-11-20

### Internal

- Set up the project: mod metadata, load folders, a build for RimWorld 1.6 and 1.7, and the deploy script.
- Added the design spec at `.scratch/loot-scavenging/spec.md`.
