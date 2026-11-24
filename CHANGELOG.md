# Changelog

<!-- Agent read route: read only the newest relevant release; locate older versions by searching `^## ` headings. -->

## 0.5.2 - 2026-11-24

### Added

- Two new settings choose what happens to the gear of pawns who join your colony: one for recruited or enslaved prisoners, one for wanderers, refugees, quest joiners and anyone else. Each can mark out-of-reach gear unfamiliar, follow the drop mode, or leave the gear alone. Both cover the joiner's pack too: following the drop mode sorts it, and marking marks the weapons and apparel in it but leaves his medicine and other items alone. A marked item stays on its wearer, and once it comes off, no colonist can use it until its research is finished. This works in every drop mode. Prisoners follow the drop mode by default and everyone else is marked. Modpacks can set and lock them as `rules.prisonerJoinGear` and `rules.otherJoinGear`.
- New setting, "When a non-colony pawn is downed": keep his weapon, or drop the weapon. Modpacks can set and lock it as `rules.downedGear`.

### Changed

- A downed raider now keeps his weapon in his hand by default, and nothing drops. His gear is not judged until he is stripped, arrested, joins your colony or dies, so a raider you capture and recruit is sorted once. If he gets back up, he is still armed. Choose "Drop the weapon" for the old behaviour.
- With "Drop the weapon" and "Everything stays on the body", a downed pawn's weapon stays in his hand again instead of going into his pack. If a downed pawn who kept his weapon is carried and put down, even into a bed when he is rescued, the game makes him drop it, and the mod sorts it as it lands. Loading a save no longer makes him drop it.
- "Also when downed or stripped" is now "Also when stripped or arrested". With it off, stripping and arresting leave gear whole, and so does downing if downed pawns drop the weapon. Pawns who join your colony follow the joiner settings whatever this setting says.
- Weapons and apparel in a pawn's pack are now judged by the weapon and apparel rules instead of the "Other inventory" rule.
- Scrap is cheaper, so gear gives about twice as many units, and cheap gear that used to leave nothing now leaves some. A unit is worth half as much, recycles into half as much and studies for half the research points. Scrap now stacks to 1,000.
- Scrap already in a save is doubled the first time the save loads, so it keeps its value. That covers scrap on your maps, in pawns' packs, in caravans and in transport pods.
- Research points per unit that you set yourself are halved once, so your doubled scrap still pays the same research in total. The settings page shows a note when this happens.
- New x10 recycle and study bills handle ten units in one job. They start only when at least ten units are in stock.
- Gear with no era that would break into components now gives scrap instead when it is worth less than one component, so it no longer hands out a component worth more than the gear.
- Every setting is now saved, including ones that match the default. A setting you chose stays put when a modpack or a mod update changes its default. Before, a choice that matched the default of the day was left out of the settings file and moved with the default.

### Fixed

- With "Also when stripped or arrested" on, arresting a standing pawn, such as a visitor, a trader's guard or a wild man, now sorts his weapon and pack. So does trading a pawn away, and a pawn dropping his weapon because he lost the use of his arms. Out-of-reach gear used to drop whole in these cases.
- A captured raider who is recruited or enslaved now has his gear handled as he joins, by the setting for prisoners. He used to join still wearing out-of-reach apparel, and could then take it off whole.
- Materials such as devilstrand, steel and modded stone are never beyond your colony's reach any more, so raiders drop them whole instead of turning them into scrap. A mod recipe that makes a material could give it that recipe's era, and a tribal raider's 53 devilstrand became 53 spacer scrap.
- Wood, beer, thrumbo horns, tusks and other items the game lets a pawn swing as a club no longer count as weapons. A raider's wood or beer used to be judged by his faction's era, so it could turn into scrap at a tribal colony, and a horn worth 800 silver could be scrapped or deleted by a weapon rule. Grenades and javelins that stack still count as weapons.
- A stack that does get scrapped is now counted as a whole and rounded once, as ammunition already was. Before, every item in the stack gave at least one scrap, so 53 devilstrand gave at least 53.
- Scrap is never worth more than the item or stack it came from. A unit counts as the larger of the silver it recycles into and its market value setting, and the scrap is cut back until it fits. 75 silver could give scrap that recycled into about 300 silver at a spacer colony. An item worth less than one scrap is removed and leaves nothing, and an unfamiliar original that cheap cannot be recycled or studied. Gear that was already worth more than its scrap is unchanged.
- With "Keep the original item", quest items, unique, persona and relic weapons and items on the always-keep list are never unfamiliar, so a quest-reward persona monosword can be used straight away.
- Items on the always-remove list are now removed in every drop mode. An out-of-reach item on the list used to be kept whole by "Keep the original item", or scrapped instead of deleted.
- Apparel the game locks on a pawn, as on some raiders and quest pawns, is now sorted at death like the rest. It used to drop whole from the corpse.
- With "Everything stays on the body", gear kept when a pawn is downed is no longer judged again when the pawn dies or is stripped. A kept weapon could be deleted at death by the "Other inventory" rule, and chance and degrade applied twice. After a save is loaded, that gear is judged once more.
- A pawn who has ever been a colonist now counts as the colony's, so a colonist who runs wild, or is raised as a shambler, keeps the colony's gear. A recruit who later turns hostile is left alone too.
- If a game update breaks one of the mod's patches, only that part of the mod stops, with an error naming it. Before, the whole mod could fail to load.

## 0.5.1 - 2026-11-23

### Changed

- With "Everything stays on the body", a downed pawn now keeps its weapon in its pack instead of its hands. A weapon in hand still dropped after a save was reloaded or the pawn was carried, and a raider who got back up was armed straight away. Stripping or killing the pawn still finds the weapon.

## 0.5.0 - 2026-11-23

### Added

- New setting, "Drop mode includes ammunition". Turn it on to have ammunition beyond your colony's reach handled by the drop mode like any other item, as it was before. Modpacks can set and lock it as `rules.dropIncludesAmmunition`.

### Changed

- Ammunition no longer turns into scrap just because it's beyond your colony's reach. It now follows its own item rule, which keeps it by default, so raiders drop their arrows and rounds whole.
- The Cleanup preset now deletes all ammunition from the hostile dead.
- "Everything stays on the body" now covers downed pawns too. A downed pawn the rules cover keeps its weapon in hand and its inventory and scrap in its pack, instead of dropping them. Gear the rules scrap or delete is still handled when the pawn is downed.

### Fixed

- Scrapped ammunition no longer gives one scrap per round. A stack now gives about what it's worth, with at least one scrap per stack, so 53 cheap arrows make around 4 scrap instead of 53. Other items are unchanged.

## 0.4.1 - 2026-11-23

### Removed

- Removed the one-time message shown over the main menu on first load. Auto Mod Config presets no longer need `firstLoadMessageShown`; a file that still sets it loads as before.

## 0.4.0 - 2026-11-23

### Added

- You can now set the research points each era's scrap pays when studied, and scale them all with one slider. Setting an era to 0 stops its scrap being studied. Modpacks can set and lock these values too.

### Changed

- Lowered the default research points from studying scrap, because a large raid could pay for several days of research at once. Industrial and higher eras now pay half as much: industrial 15 (was 30), spacer 25 (was 50), ultratech 35 (was 70) and archotech 50 (was 100). Medieval drops from 20 to 15, and neolithic stays at 10.
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
