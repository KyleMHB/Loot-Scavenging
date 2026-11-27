# Changelog

<!-- Agent read route: read only the newest relevant release; locate older versions by searching `^## ` headings. -->

## 0.7.0 - 2026-11-27

### Added

- A "Strip corpses before cremating" setting, off by default and in every preset. With it on, a cremation bill strips each corpse first, so gear kept on the body drops by the crematorium and follows the item rules as it would at a butcher. With it off, kept gear burns with the body. Modpacks can set and lock it as `cremationStripsCorpses`.

### Changed

- The Colonists box of "Keep gear on the body" also covers your colony's animals, so a downed or killed pack animal keeps its load instead of scattering it.
- Studying an unfamiliar item pays like research work. Its points are divided by the project's cost factor for your faction and count toward the researcher's research record. A study job leaves an unfamiliar item alone when the project needs less than half of what the item would pay.
- The "All gear beyond your research" scope sentence now says that starting gear beyond your research becomes unfamiliar too.
- The corpse removal texts say they cover groups set to Mark unfamiliar as well as Apply.

### Fixed

- "Drop" on a dead colonist's Gear tab did nothing and logged an error. It now drops the item beside the corpse.
- Gear tied to an Anomaly knowledge project stayed unfamiliar after the project was finished.
- Gear with no recipe and no research said it waited on research. It now says it waits on the colony's tech level.
- A wanderer's or other walk-in joiner's gear was never sorted under "Other joiners: Apply". It is now sorted once, when they first arrive on a map.
- A recycled unfamiliar item that gave no scrap was handed back to be rolled again. It is now used up, unless it has no scrap equivalent.
- A stack split from unfamiliar items lost its mark, and marked and unmarked items stacked together.
- A finished study job that paid nothing still counted toward the bill. It no longer counts and says why it paid nothing.
- The study bill's stock count follows its count settings, the work bar shows the job's real total work, the bills tab has the paste button, and "can be studied now" shows only when a bench can research the project.
- Locked checkboxes on the settings page could still be toggled. The category boxes under a locked category are locked too.
- The unfamiliar scope sentence counted the joiner rows, so on the default settings it described a label that was not in use.
- The scrap market value box showed a different value from the rounded one it stores.
- A 0.5.2 modpack that locked the death drop on the ground got both keep-gear boxes locked on, the opposite of its choice. They are now locked off.
- Loot rolls used their own random numbers, which desynced games under Multiplayer. They now use the game's.
- Scrap textures moved to their own folder, so another mod's texture at the same path no longer replaces them.

### Internal

- The colony's era is cached between research changes, so sorting gear does less work.
- The mod ships its LICENSE file.

## 0.6.0 - 2026-11-25

### Added

- Three "Keep gear on the body" boxes, all on by default, choose where a pawn's gear lands when they are downed or killed. "Colonists" covers colonists and colony slaves, "Hostiles" covers hostile pawns, and "Non-hostiles and wild people" covers non-hostile factions and wild people. With a box on, a downed pawn keeps their weapon in hand and their inventory, also when carried, rescued into a bed or loaded from a save, and a dead one keeps everything on the body until the corpse is stripped, butchered or cremated. A raider's gear now goes through the rules only when it changes hands: when they are stripped, arrested, join your colony or die. With a box off, that group drops as in the base game, and what drops follows the item rules. Colony gear never goes through the item rules either way. The Vanilla preset turns all three off. Modpacks can set and lock them as `colonistsKeepGear`, `hostilesKeepGear` and `nonHostilesKeepGear`.
- A Who list with one row per group of pawns: hostile pawns, non-hostile pawns, wild people and animals, prisoners, recruited or enslaved prisoners, and other joiners. Each row is "Apply item rules", "Leave alone" or "Mark unfamiliar". The first five default to Apply and other joiners to Mark. Prisoners covers gear that comes off a prisoner, when a prisoner is stripped or when a standing pawn is arrested. Mark unfamiliar keeps the group's gear whole and marks gear beyond your research unfamiliar, and replaces the item rules for that group. Gear that comes off a group set to Apply always follows the item rules. Modpacks can set and lock the rows as `rules.whoHostile`, `rules.whoNonHostile`, `rules.whoWild`, `rules.whoPrisoners`, `rules.whoRecruitedPrisoners` and `rules.whoOtherJoiners`.
- Scrap market value is set per era, six values that each default to the old single value. Modpacks can set and lock them as `scrapMarketValueNeolithic` and so on up to `scrapMarketValueArchotech`.

### Changed

- The drop mode is gone. The item rules now decide everything, for the groups the Who list names. Each category chooses Scrap, Disassemble on the spot, Delete or Drop as normal ("Keep" is now called Drop as normal), and "Chance to scrap", "Chance to disassemble" or "Chance to delete" is typed as 0 to 100 and hidden under Drop as normal. Weapons and apparel default to Scrap with "Only gear beyond your research" ticked, which is what the Progression preset did before. Everything else defaults to Drop as normal, so drugs, food and other items beyond your research are no longer scrapped by default.
- Settings and bills use plainer words. "Only gear beyond the colony's reach" is now "Only gear beyond your research", the "All out-of-reach gear" unfamiliar label choice is "All gear beyond your research", and the "recycle original items" and "study original items" bills are "recycle unfamiliar items" and "study unfamiliar items".
- Every slider is now a text box that takes a typed number. Percentages are typed as 0 to 100.
- Instant disassembly with "Materials of the colony's era" now rolls the colony's era, as it says. It used to roll the era of the item. A tribal colony that disassembles a spacer rifle now gets neolithic materials instead of spacer ones.
- Butchering a humanlike corpse always strips it first, and what comes off follows the item rules under the owner's Who row. A corpse of a group set to Leave alone, or a colonist's, is butchered as in the base game. "Butchering removes worn apparel" is gone.
- A ticked corpse type is always removed. The corpse chance is gone.
- Weapons and apparel in a pawn's inventory follow the weapon and apparel rules instead of the "Other inventory" rule.
- The two joiner rows cover the joiner's inventory too. Apply runs it through the item rules, and Mark marks the weapons and apparel in it and leaves their medicine and other items alone.
- "Also when downed or stripped" is gone. Arresting a standing pawn, stripping a prisoner and a downing all run gear through the item rules when the owner's row is Apply, and drop it whole when the row is Leave alone.
- "Drop mode includes medicine" and "Drop mode includes ammunition" are gone. The medicine and ammunition rules decide, and both default to Drop as normal.

### Removed

- The settings below no longer exist and are fixed at these behaviours.
  - Degrade, with its range and "lowers quality". A Degrade rule becomes Drop as normal.
  - "Break it into components" for gear with no era. Such gear uses the era of the faction that carried it, which was the default.
  - "Which scrap can be studied". The colony's era and below can be studied, as before by default.
  - "Eject loaded charges". Charges are always ejected.
  - The research points multiplier. A multiplier other than 1 is multiplied into each era's points once.
  - The corpse chance and "Butchering removes worn apparel".

### Settings from 0.5.2

- A setting file that still holds a 0.5.2 value is read once and translated. A saved value that equals its 0.5.2 default moves to the 0.6.0 default, because 0.5.2 saved every value and a default can't be told from a choice. Only values you changed carry over. If you chose a 0.5.2 default on purpose, such as dropping loot on the ground at death, it is reset and you need to set it again.
- Carried over: a group you had turned off becomes Leave alone. A joiner setting set to "Follow the drop mode" becomes Apply, "Mark out-of-reach gear unfamiliar" becomes Mark and "Leave their gear alone" becomes Leave alone. "Scrap everything" becomes Scrap on every category. "Keep the original item" turns every Who row that was on to Mark. A death drop that kept loot on the body ticks the matching boxes. The scrap market value is copied to every era.
- A category that had its own rule under a scrap drop mode keeps that rule and loses the scrapping of its items beyond your research. Weapons set to Delete, for example, no longer scrap the ones beyond your research.
- Modpack patches and locks on the old names still work. A changed value is translated into the new settings, and a lock on an old name locks everything that replaced it:
  - `rules.hostile`, `rules.nonHostileFaction`, `rules.noFaction`, `rules.prisoners`, `rules.prisonerJoinGear` and `rules.otherJoinGear` become `rules.whoHostile`, `rules.whoNonHostile`, `rules.whoWild`, `rules.whoPrisoners`, `rules.whoRecruitedPrisoners` and `rules.whoOtherJoiners`.
  - `rules.dropMode` locks every Who row and item rule. `rules.dropIncludesMedicine` and `rules.dropIncludesAmmunition` lock `rules.medicine` and `rules.ammunition`.
  - `deathDrop` locks `hostilesKeepGear` and `nonHostilesKeepGear`.
  - `scrapMarketValue` and `studyPointsMultiplier` lock the six per-era values and the six per-era study points.
  - Locks on removed settings are accepted and ignored.

## 0.5.2 - 2026-11-25

### Added

- Two new settings choose what happens to the gear of pawns who join your colony: one for recruited or enslaved prisoners, one for wanderers, refugees, quest joiners and anyone else. Each can mark out-of-reach gear unfamiliar, follow the drop mode, or leave the gear alone. A marked item stays on its wearer, and once it comes off, no colonist can use it until its research is finished. This works in every drop mode. Prisoners follow the drop mode by default and everyone else is marked. Modpacks can set and lock them as `rules.prisonerJoinGear` and `rules.otherJoinGear`.

### Changed

- Scrap is cheaper, so gear gives about twice as many units, and cheap gear that used to leave nothing now leaves some. A unit is worth half as much, recycles into half as much and studies for half the research points. Scrap now stacks to 1,000.
- Scrap already in a save is doubled the first time the save loads, so it keeps its value. That covers scrap on your maps, in pawns' packs, in caravans and in transport pods.
- Research points per unit that you set yourself are halved once, so your doubled scrap still pays the same research in total. The settings page shows a note when this happens.
- New x10 recycle and study bills handle ten units in one job. They start only when at least ten units are in stock.
- Gear with no era that would break into components now gives scrap instead when it is worth less than one component, so it no longer hands out a component worth more than the gear.
- Every setting is now saved, including ones that match the default. A setting you chose stays put when a modpack or a mod update changes its default. Before, a choice that matched the default of the day was left out of the settings file and moved with the default.

### Fixed

- With "Also when downed or stripped" on, arresting a standing pawn, such as a visitor, a trader's guard or a wild man, now sorts his weapon and pack as a downing does. So does trading a pawn away, and a pawn dropping his weapon because he lost the use of his arms. Out-of-reach gear used to drop whole in these cases.
- With the same setting on, a captured raider who is recruited or enslaved now has his out-of-reach apparel sorted as he joins, as stripping him would. He used to join still wearing it, and could then take it off whole.
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
