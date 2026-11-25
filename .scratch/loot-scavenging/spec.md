# Loot Scavenging: spec

Status: needs-triage

Written 2026-11-20 from the design doc at https://claude.ai/artifact/WFrwwPtCFzMNo7rbTxdsCK. This file is the copy that implementation works from. If the two disagree, raise it before coding.

## Goal and scope

Loot Scavenging decides what happens to gear when a non-colony pawn drops it: leave it as the game does (vanilla), scrap, disassemble or delete. Scrap can then be recycled for materials or studied for research at benches the colony already has. It is an original mod, informed by what the comments on Looting Manager show players want and where they hit problems.

It serves two kinds of player:

- **Cleanup players** want large raids to leave less clutter and fewer corpses.
- **Progression players** want raid loot to stop outpacing their research, especially on tribal starts.

Three things define the first release:

- **Same needs.** The mod serves cleanup and progression through its own rule system. It does not mirror Looting Manager's option list.
- **Correctness.** The faults found in the review of Looting Manager do not exist here: wild animals treated as friendly, an efficiency slider with no effect, lost refund stacks, partial-stack deletion and an error when a pawn leaves no corpse.
- **Finer rules.** Each item category gets its own outcome and chance, and players can protect or target specific items.

## Originality rules

Loot Scavenging is built from this spec and the game's own code. Nothing from Looting Manager, Non uno Pinata or Technology Blueprints & Reverse Engineering is copied or consulted during the build. None of them states a licence, so their code and text are not ours to reuse.

- **No copied code.** No class, method, patch or snippet from those mods is copied, translated or adapted.
- **No copied text.** Setting names, tooltips, translation keys, the description and preview art are all written new.
- **No source on hand.** Their source is not kept in this project and is not opened while coding. The copies used for the review were deleted.
- **Spec first.** Implementation works from the behaviours described here, in plain words. Where the spec is silent, the answer comes from the game's code or a new design decision, not from another mod.
- **Fresh context.** The review that produced this spec read Looting Manager's source and the structure of Non uno Pinata's. Coding sessions start from this document alone, so that reading does not carry into the code.
- **Own identity.** The Workshop page does not present the mod as an update, fork or continuation.

Ideas and game mechanics are free to learn from. The table records the ideas taken from Looting Manager and how this design does each one its own way.

| Idea learned | How Looting Manager does it | How Loot Scavenging does it |
| --- | --- | --- |
| Act on enemy gear, leave the colony's alone | Checks the pawn's faction on every dropped item | Sorts pawns into four owner groups, with factionless pawns separate |
| Decide by item type | Three switches: weapons, apparel, everything else | One rule per category, each with its own outcome, chance and conditions |
| Keep loot within the colony's research | Looks up the item's research and treats recipe-less items as researched | "Out of reach" test using research, with era as the fallback |
| Return materials instead of deleting | Calls the game's smelt function, which ignores the efficiency value | Own calculation from the item's cost list, with a working efficiency setting |
| Remove corpses after a fight | Strips and removes the corpse when a pawn dies | A separate corpse rule with its own chance, pawn types and place checks |
| Roll a chance | One global chance | A chance per category |
| When to act | Reacts after any item leaves a pawn's gear | Sorts the pawn's gear before the game drops it |

Scrap, recycling, study, protection lists, presets and the era rules have no counterpart in Looting Manager.

## Loot rules

Ticket 25 (2026-11-25, Kyle's decisions) replaced the drop mode and the per-moment toggles with one system: the per-category item rules, applied to the groups of pawns the Who list names. Earlier versions of this section described a drop mode, `rules.downedGear`, `deathDrop`, "Sort when arrested" and "Sort when stripped". None of them exists any more.

Every item a pawn drops passes the same ordered checks, and the first check that says "leave it" ends the run with the item untouched.

1. **Owner.** Colony pawns are never affected, and neither is a pawn that has ever been a colonist (a colonist who ran wild, a shambler raised from one). Their gear is never scrapped or sorted. Other pawns fall into six groups, each with a Who row (see "Settings page"): hostile pawns, non-hostile pawns, wild people and animals, prisoners, recruited or enslaved prisoners, and other joiners. A row is **Apply item rules**, **Exclude** or **Mark unfamiliar**. Exclude ends the run. Mark unfamiliar keeps the gear whole and labels gear beyond the colony's reach unfamiliar (see "Original-item mode"). It replaces the item rules for that group and ends the run.
2. **Place.** An optional "home map only" setting leaves loot alone on caravan, quest and outpost maps.
3. **Protection.** Quest-tagged items, unique weapons, persona weapons, relics and anything on the player's keep list are never touched, and never marked unfamiliar.
4. **Remove list.** An item on it gets its category's outcome, or Delete when that outcome is Vanilla, skipping the conditions and chance.
5. **Category.** The item is matched to one category rule: weapons, apparel, or an inventory group (ammunition, drugs, medicine, food, other).
6. **Conditions.** The category rule can be limited to items beyond the colony's reach, below a quality, or below a hit-point percentage.
7. **Chance.** The category's own chance is rolled. It is typed as 0 to 100 and shown as "Chance to scrap", "Chance to disassemble" or "Chance to delete".
8. **Outcome.** The category's outcome is applied.

There is no moment step. Gear that comes off a pawn whose row is Apply is always sorted, at the moment it comes off: death, strip, arrest, joining, or a drop the game makes. The "Don't drop your stuff" boxes only choose whether gear comes off at a downing or a death at all (see "Settings page").

A whole dropped stack is treated as one item, and a partial drop acts on the piece that left the pawn. Each item is judged once. Locked apparel stays on a stripped living pawn, but is sorted at death.

| Outcome | What the player sees |
| --- | --- |
| Vanilla | The base game's behaviour. (Called Keep in earlier versions.) |
| Scrap | The item becomes stackable scrap of its era, to be recycled or studied at a bench. |
| Disassemble | The item is replaced on the spot by materials, with no bench work. A setting chooses the yield: materials picked from the colony era's loot pool, or the item's own ingredients at a set efficiency (shown only under that yield). |
| Delete | The item vanishes. |

Degrade was cut in 0.6.0, with its range and "lowers quality". Loaded charges in reloadable gear are always ejected.

**Defaults:** weapons and apparel are Scrap with "Only gear beyond the colony's reach" ticked. Every other category is Vanilla, so out-of-reach drugs, food and other items are not scrapped, and neither are medicine and ammunition. (Medicine and ammunition used to be exempt from the drop mode by a setting each. They now follow their own rule like any category.)

**Out of reach** means the item's research is unfinished or its era is above the colony's current era. An item's era is found in this order:

1. The tech level of the research project that unlocks it.
2. The item's own tech level.
3. The era of the faction that carried it.

Step 3 covers modded and uncraftable gear with no era of its own. It is fixed. The "colony's era" and "break into components" alternatives were cut in 0.6.0, and `ComponentsFallback` and the cheap-gear rule in `DisassembleApplier` went with them.

**Inventory categories added 2026-11-24**, from a survey of 1,559 downloaded Workshop mods and the DLCs ([pack-item-categories.md](pack-item-categories.md)). Each keeps its items by default:
- **Knowledge:** books, schematics, techprints, neurotrainers and research papers.
- **Genetics:** genepacks, xenogerms, embryos and ova, and the gene items of Vanilla Genetics Expanded and Alpha Genes.
- **Body parts:** the game's body parts tree, which holds prosthetics, bionics and implants.

The survey also settled two classification rules. Mortar shells and modded bolts count as ammunition. Anything under the game's drugs category counts as drugs, including serums with no drug effect. Usable items (loot boxes, weapon kits, quest cards, shards) count as other, never as raw materials. Named exceptions (an item or a thing category that should count as a given inventory category) live in an XML list that packs can patch, not in code, so a renamed or refiled item is fixed with a patch.

**Raw materials** is an inventory category: anything the game counts as a building material (stuff), and items filed under the game's raw resources or manufactured categories (`ResourcesRaw`, `Manufactured`), except mortar shells. It covers devilstrand, steel, plasteel, wood, stone blocks, silver, gold, chemfuel, components and neutroamine. Materials have no era and are never out of reach, because any colony can build and craft with them. A beyond-reach rule never touches them, and a rule with no reach condition applies to them like any item.

## Scrap, recycling and study

Out-of-reach gear becomes scrap: one stackable item per era, where the amount carries the gear's value. Scrap is recycled for materials or studied for research, at benches the colony already owns.

### Scrap

- **One item per era.** Six tiers: neolithic, medieval, industrial, spacer, ultratech and archotech. Animal-era gear becomes neolithic scrap, since no bench sits below the crafting spot. Scrap is generic. It does not remember the item it came from, so it stacks fully and a large raid leaves a few piles.
- **Amount is value.** A stone knife becomes one unit of neolithic scrap and a charge rifle becomes several units of spacer scrap, scaled by the item's worth, quality and condition.
- **Low market value.** Scrap is worth close to nothing by default, so a stockpile kept for a later era does not raise colony wealth or sell for silver. A setting changes this.
- **Where it appears.** Where a pawn's loot and scrap land is chosen by the three "Don't drop your stuff" boxes (see "Settings page"). With a box on, a dead pawn of that group keeps everything on the body until the corpse is stripped, butchered or cremated, and a downed one keeps his weapon in hand. With it off, the loot drops on the ground like other drops, sorted as it drops. Pawns whose gear the game destroys at death, and pawns that cannot be stripped, drop as usual. The earlier "where loot lands at death" setting (`deathDrop`) and "when a non-colony pawn is downed" setting (`rules.downedGear`) were replaced by the boxes in 0.6.0.
- **Original-item mode.** When a Who row is set to Mark unfamiliar, that group's gear stays whole and out-of-reach gear counts as its scrap equivalent at the bench: the same era, amount, yields and points. It is labelled unfamiliar and cannot be equipped until its research finishes. The unfamiliar label setting chooses which gear gets the label: all out-of-reach gear from any source (default), or only gear looted from pawns. The default is worked out from research and saves nothing. Looted-only needs a marker saved on each looted item.

### Recycling

Recycling is a bill at an existing bench, with no new building.

- **The bench is the gate.** Each scrap tier names a bench. A bench recycles its own tier and every tier below it. There is no second check on the colony's era.
- **A loot pool per tier.** Each tier lists the materials it can return, each with a weight, and a value per unit in silver. A fixed bundle per unit floods the colony with the same basic material, steel above all. A pool spreads the same value across more materials.
- **One pick per unit.** Each unit of scrap picks one material from its pool by weight. The amount is the tier's value per unit divided by that material's market value, rounded up or down at random so the average is exact. Every pick is worth the same, so a material's weight is its share of the tier's value. Expensive materials come in small amounts without a separate chance line. A bill of five units can come out lumpy, and that is intended.
- **No fixed lines.** A pool has no guaranteed yield and no chance lines.
- **Lower tiers at a low weight.** A tier's pool also holds materials from the tiers below it, at about 15% of its weight in total. This keeps the pools realistic without bringing back the flood of basic materials.
- **Static.** The pool is exactly what the XML says. It does not react to the colony's research or stock.
- **Vague bill text.** A recycling bill says the scrap is recycled into materials of its era. It names no materials, amounts or shares.
- **One XML file.** The table holds the bench, the value per unit and the pool for each tier, so a pack can patch all three without code. Pool entries for DLC materials carry `MayRequire`. The Progression pack's materials go in its preset patch, not in this mod's table.

| Scrap tier | Proposed bench | Proposed pool | Proposed value per unit |
| --- | --- | --- | --- |
| Neolithic | Crafting spot | Wood, leather, cloth | About 3 silver |
| Medieval | Smithy | Steel, cloth, wood, silver, with neolithic materials | About 4.5 silver |
| Industrial | Machining table | Steel, chemfuel, silver, components, with lower-tier materials | About 5.5 silver |
| Spacer | Fabrication bench | Plasteel, gold, uranium, advanced components, with steel and components | About 16.5 silver |
| Ultratech | Fabrication bench | Plasteel, uranium, gold, advanced components, with spacer materials | About 24 silver |
| Archotech | Fabrication bench | Plasteel, gold, advanced components, with spacer materials | About 32.5 silver |

Vanilla has no ultratech or archotech bench, so both name the fabrication bench until a pack supplies its own. These are vanilla starting values, set close to the total value of the earlier fixed yields, then halved in 0.5.2 so cheap gear makes scrap instead of nothing (ticket 19). A unit stacks to 1,000. Each recycle and study bill has a x10 variant that handles ten units per job. Weights are set when the table is written. The Progression pack's own benches and materials need checking before the table is fixed.

### Study

Study is a job in the Research work type: a researcher carries scrap to a research bench and uses it up for research points.

| Question | Answer |
| --- | --- |
| Where points go | The project currently being researched, if it is of the scrap's era or lower. |
| Finishing a project | Study can finish a project, through the game's normal completion. |
| Nothing eligible | When no project is active, or the active project is a higher era than the scrap, the study job waits and the scrap stays in storage. Its inspect text says why. |
| Which scrap can be studied | Fixed: the colony's current era and below, so higher scrap waits until the colony advances. (The setting was cut in 0.6.0.) |
| Recycle or study | A setting. By default each unit is used for one or the other. The alternative studies it first and then recycles the remains at reduced value. |
| How much | A set number of points per unit for each tier, a setting per tier typed in a text box (default 5, 7.5, 7.5, 12.5, 17.5, 25; 10, 15, 15, 25, 35, 50 before 0.5.2; 10, 20, 30, 50, 70, 100 before 2026-11-23). Zero stops that tier being studied. The one multiplier for all was cut in 0.6.0 (ticket 25): a saved multiplier other than 1 is multiplied into each tier's points once. |
| Techprint projects | A setting, off by default. Off, projects that need techprints receive nothing. On, study pays into them even before the techprints are applied. Projects with a special study source never receive anything. |
| Time and skill | Study time scales with the points paid. Research speed applies and the researcher gains Intellectual experience. |

In the two scrap modes the mod adds no data to items, so it can be removed mid-game by first clearing the scrap. From 0.5.2 the game file holds one game component with the scrap scale, so a save from before 0.5.2 has its scrap doubled once when it loads; without the mod that component is dropped with one error.

### Requirements learned from Technology Blueprints & Reverse Engineering

That mod has paid research from items since 2020, and its comments show where the idea goes wrong.

- **Flat points, not a percentage.** Project costs vary widely, so a share of the cost is unfair. Fixed points per unit of scrap already does this.
- **Never divide by the research speed setting.** With the storyteller's research factor at zero, that mod wrote an infinite or invalid value into project progress. Study adds points through the game's research manager and is tested at research factors of zero and above 100%.
- **No current project is a normal state.** The job does not start.
- **Say what the player gets.** The scrap's inspect text shows the points per unit and whether it can be studied now, and finishing a study shows the gain and the project.
- **No clicking per item.** Study and recycling are bench work with the usual bill controls. Recycling bills run "forever" or a set number of times. They have no keep-in-stock count, because a pool has no single product to count, and scrap is normally recycled until it is gone.
- **Never consume what the player wants to keep.** In original-item mode, an item is studied or recycled only when the player marks it or a filter allows it.
- **Add bills without touching other recipes.** Reports describe other mods' bench recipes disappearing while that mod was installed. Recycling bills are added to the named benches only, and the recycling stage checks that a bench from another mod still shows all its own bills.

## Corpses and butchering

Corpse cleanup is three separate checkboxes: humanlike, animal and mechanoid. A ticked kind is always removed. The details are under "Settings page", "Corpses".

- The corpse's gear goes through the loot rules first, so nothing bypasses them.
- The owner and place checks apply to corpses too.
- A pawn that dies without leaving a corpse is skipped quietly.
- Butchering a humanlike corpse always strips and sorts it first.

## Settings page

The page has these sections, top to bottom. Every slider is a numeric text box clamped to its range (ticket 25), and percentages are typed as 0 to 100.

| Section | Controls |
| --- | --- |
| Presets | Progression, Cleanup, Vanilla, and Reset to defaults |
| Don't drop your stuff | Three boxes: colonists, hostiles, non-hostiles |
| Who | One row per group of pawns: Apply item rules, Exclude or Mark unfamiliar |
| Item rules | One row per category: outcome, chance, conditions |
| Scrap | Research points per era, "Study pays into techprint projects", scrap market value per era, recycle or study |
| Corpses | Humanlike, animal, mechanoid |
| Lists | The four protect toggles, always keep, always remove |
| Other | Instant disassembly yield (and its efficiency), unfamiliar label scope, apply on the home map only |

- **Default preset is Progression.** There is no first-load message. The Workshop description and the settings page explain the presets.
- **Presets** set every setting at once. The page shows which preset is active and whether it has been changed.
- **Each row says what will happen** in a sentence under it.
- **Lists** are picked from a searchable item menu and stored by item name, so a removed mod does not break the settings.
- **Disassembly efficiency** shows only under "Materials of the item".

Settings are global. All text is translatable.

### Don't drop your stuff

Three checkboxes, all on by default (`colonistsKeepGear`, `hostilesKeepGear`, `nonHostilesKeepGear`). They choose where items land. Whether they are sorted is the Who row's job.

- **Colonists keep their weapons when downed or killed.** A downed colonist or colony slave keeps his weapon in hand and his pack. Nothing drops at the downing, and the game's drop as he is spawned again (carried and set down, rescued into a bed, loaded) is blocked. A dead one keeps his weapon and pack in his corpse. A resurrected colonist finds his weapon in his pack, as the game puts it there. Colony gear is never sorted either way, so colonists have no Who row. Colony animals and mechanoids are left to the game. A kidnapped colonist keeps his weapon, and a buried colonist's corpse keeps his gear in the grave.
- **Hostiles don't drop their gear, weapons and loot on the floor when downed or killed.** On, a downed hostile keeps his weapon in hand, his apparel and his pack, and nothing is judged until his gear changes hands: stripped, arrested or captured, joining, or death. A dead hostile keeps everything on the body (weapon, inventory, scrap, disassembly products and ejected charges) until the corpse is stripped, butchered or cremated. Off, the game drops his weapon and inventory (never his apparel), and what drops is sorted as it drops. What the rules keep lands whole on the ground.
- **Non-hostiles don't drop their gear, weapons and loot on the floor when downed or killed.** The same for pawns of non-hostile factions and wild people. A prisoner follows his faction: a hostile faction's prisoner uses the hostile box, anyone else the non-hostile one.

Mechanoids and other pawns that cannot be stripped still drop on the ground. Pawns the rules do not cover drop as in the game. A pawn downed in bed keeps his weapon in hand, as the game does, and an arrest then sorts it. A trader's goods keep their not-for-sale mark. Hospitality stops the game's drop for a friendly guest. With the box off, the mod drops the guest's gear itself, sorted. A weapon or apparel in a pack counts as a weapon or apparel for the item rules, so gear in a pack is judged by the rule that would have judged it in his hands. A pawn dropping his weapon because he lost the use of his arms is sorted when his row is Apply.

Each item is judged once. A downing that keeps his weapon judges nothing, and a downing that drops it sorts what it drops. After a save is loaded, gear a box kept is judged once more when it comes off.

### Who

One row per group, each **[Apply item rules | Exclude | Mark unfamiliar]**. Saved as `rules.whoHostile`, `rules.whoNonHostile`, `rules.whoWild`, `rules.whoPrisoners`, `rules.whoRecruitedPrisoners` and `rules.whoOtherJoiners`.

| Row | Covers | Default |
| --- | --- | --- |
| Hostile pawns | Pawns of hostile factions | Apply |
| Non-hostile pawns | Pawns of non-hostile factions: traders, visitors, allies | Apply |
| Wild people and animals | Pawns with no faction. Also gates wild animal corpse removal | Apply |
| Prisoners | Gear that comes off a prisoner: stripping him, or arresting a standing pawn | Apply |
| Recruited or enslaved prisoners | Gear as the prisoner joins the colony | Apply |
| Other joiners | Wanderers, refugees, quest joiners, rescued pawns, creepjoiners | Mark |

- A raider's gear is judged under the Hostile row when he is downed or killed. The Prisoners row covers what comes off him later.
- **Mark unfamiliar** keeps the group's gear whole and marks out-of-era gear unfamiliar. For the first four rows the label follows the unfamiliar scope setting. For a joiner row the mark is saved on the item.
- There are no "Sort when arrested" or "Sort when stripped" toggles. Gear that comes off a group set to Apply is always sorted.
- When every row is Exclude, the item rules change no loot and no corpses.

#### Pawns who join the colony

The two joiner rows decide a pawn's gear at the moment `Join` (ticket 20, extended by tickets 21 and 25). Recruited or enslaved prisoners have their own row. The other row covers wanderers, refugees, quest joiners, rescued pawns, creepjoiners and the like.

- **Mark unfamiliar.** The joiner keeps wearing and holding his gear. Each out-of-reach worn apparel and held equipment item, and each out-of-reach weapon or apparel in his pack, gets a join mark (`CompUnfamiliar.joinedEra`, saved only when set). A marked item is unfamiliar until its research is finished or the colony's era catches up. Once it is off him, no colonist can equip or wear it, and it can be recycled or studied like other unfamiliar gear. Protected items are never marked, and neither is apparel the game has locked on him. Medicine and other pack items are never marked.
- **Apply item rules.** His worn apparel, held equipment and pack are sorted at the moment `Join`, with the evaluator's ordinary steps and his owner group from before he joined. Locked apparel stays.
- **Exclude.** His gear is not touched as he joins.

Not joining: a pawn who was ever a colonist, animals, mechanoids and other non-humanlike pawns, quest lodgers, pawns who start in the colony or are born in it, and the pawns of a game-end "new wanderers" incident, which are generated straight into the player's faction.

Built as a prefix and finalizer on `Pawn.SetFaction` (new faction is the player's, the pawn is not), plus the same on `RecruitUtility.Recruit`. A pawn handled by the `Recruit` hook is marked in `SortedDrop` until the call ends, so the `SetFaction` inside skips him and nothing is judged twice. A joiner with no map cannot be sorted. The mark still applies.

### Item rules

Each category (weapons, apparel, ammunition, drugs, medicine, food, other inventory) chooses **[Scrap | Disassemble | Delete | Vanilla]**, with the conditions and chance described under "Loot rules". The chance is hidden under Vanilla. The defaults are in "Loot rules".

### Scrap

- **Research points per era.** Six values, default 5, 7.5, 7.5, 12.5, 17.5, 25. Zero stops that era being studied. The multiplier setting was cut, and a saved multiplier is multiplied into each era's points once.
- **Scrap market value per era.** Six values (`scrapMarketValueNeolithic` to `scrapMarketValueArchotech`), each defaulting to 0.01. `ScrapApplier` reads the scrapped item's tier.
- **Which scrap can be studied** is fixed: the colony's era and below.
- Study pays into techprint projects (off by default), and recycle or study is [one or the other | both].

### Corpses

Humanlike, animal and mechanoid, each a checkbox (default off). A ticked kind is always removed, after its gear has been through the rules. The removal chance was cut. The owner and place checks apply to corpses too. A wild animal's corpse is only removed while the wild row is Apply. A pawn that dies without leaving a corpse is skipped quietly.

**Butchering** a humanlike corpse always strips it first, and what comes off is judged by the item rules as if the corpse had been stripped by hand, under the corpse owner's Who row. A row set to Exclude, or a colonist's corpse, gets the game's own butchering. "Butchering removes worn apparel" was cut.

### Other

- **Instant disassembly yield:** [materials of the colony's era | materials of the item]. The first rolls the colony era's recycling pool, scaled by the item's scrap amount. Before 0.6.0 it rolled the item's own era. The second is the item's cost list at the efficiency setting.
- **Unfamiliar label** for gear outside the era: [looted gear only | all gear] (`unfamiliarScope`).
- **Apply on the home map only** (`rules.homeMapOnly`).

### Presets

| Setting | Progression | Cleanup | Vanilla |
| --- | --- | --- | --- |
| Don't drop your stuff | All on | All on | All off |
| Hostile pawns | Apply | Apply | Exclude |
| Non-hostile pawns, wild, prisoners | Apply | Exclude | Exclude |
| Recruited or enslaved prisoners | Apply | Exclude | Exclude |
| Other joiners | Mark | Exclude | Exclude |
| Weapons | Scrap, only beyond reach | Delete below Normal quality or below 50% hit points | Vanilla |
| Apparel | Scrap, only beyond reach | Delete below Good quality or below 50% hit points | Vanilla |
| Ammunition | Vanilla | Delete | Vanilla |
| Other categories | Vanilla | Vanilla | Vanilla |
| Corpses | None removed | Humanlike removed | None removed |

Cleanup leaves animal corpses for food and mechanoid corpses for smelting, so it removes only what a colony has no use for. Apparel has a higher quality bar than weapons because everything worn at death is tainted, so Normal apparel from the dead is clutter while Good or better armour is still worth wearing or selling. Cleanup and Vanilla leave the other settings at the Progression values, except that Vanilla turns the three boxes off.

### Migration from 0.5.2 and earlier

`SettingsLoad` reads the old keys once and stops writing them. The mapping is in ticket 25, decision 11, and `SettingLocks.Renamed` maps old lock names to the settings that replaced them. Decision 13 matters most. Since 0.5.2 saves every value, a saved value equal to its 0.5.2 default moves to the 0.6.0 default, and only a changed value is translated. A deliberate choice of a 0.5.2 default is reset, and that can't be detected.

## Code structure

The mod sorts a pawn's gear just before the game drops it.

The rules run at the moments a pawn's gear changes hands: when it dies (`Pawn.Kill`), when it is downed (`Pawn_HealthTracker.MakeDowned`, where by default nothing drops; see "Don't drop your stuff"), and when it is stripped (`Pawn.Strip`). A hook before each of those walks the pawn's equipment, apparel and inventory, applies each item's outcome, and leaves the rest to the game.

The rules do not depend on the drop itself. Non uno Pinata keeps gear on the body by replacing the game's drop-everything call, so rules attached to that call would never run beside it. Acting on the gear before any drop works either way.

**Hook rule:** use the simplest hook that reaches the right moment, and rewrite a game method only where a plain hook cannot. If the hook before death proves too early, for example when another mod cancels the death, rewriting the death method is the fallback and the overlap with Non uno Pinata is handled explicitly.

| Part | Job |
| --- | --- |
| Settings | Holds the rules, lists and presets, read once and cached. |
| Rule evaluator | Takes a pawn, an item and the map, returns an outcome. No side effects, so it can be reasoned about and logged. |
| Outcome applier | Scraps, disassembles or deletes. Owns material placement and forbidding. |
| Hooks | Thin Harmony patches that call the evaluator and applier. |
| Settings window | Draws the page and the item picker. |
| Era reader | One function returns the colony's era and one returns an item's era, with the fallbacks from the loot rules. Everything era-related calls these two. |
| Scrap items and tier table | XML only: one scrap item per era, plus the bench, value per unit and loot pool for each tier. |
| Recycling bills | Generated from the tier table at startup and attached to the named benches. |
| Study job | A work giver and job in the Research work type, modelled on how the game handles techprints. |

Item-based disassembly uses the mod's own calculation from the item's cost list and material, not the vanilla smelt function. Materials are placed nearby in full stacks and forbidden like other enemy drops.

A dev-mode log line per decision makes bug reports answerable.

The method names above, and `ResearchManager`, `JobDriver_ApplyTechprint` and `EquipmentUtility.CanEquip`, exist in the installed 1.7.5014 game assembly. Their exact signatures and the order of calls on death still need confirming in a decompiler before coding. The death, downed and strip methods are now confirmed (see "Unverified").

## Compatibility

- **Game versions.** 1.6 and 1.7.
- **Ammunition mods.** Ammo is recognised by item category, with no code dependency on any ammo mod. The Progression pack uses Progression: Ammunition, so that is tested first. Combat Extended and Yayo also eject loaded rounds themselves on death, and that path needs an in-game test before any claim of support.
- **Looting Manager.** The two mods must not run together. `About.xml` declares `CarnySenpai.LootingManager` incompatible.
- **Non uno Pinata.** The two mods should work together: its gear-stays-on-the-body behaviour and our rules on what that gear becomes.
- **Hospitality.** It also changes what guests drop. Progression: Hospitality is in the pack, so it is tested there.
- **Pawns that cannot be stripped.** Weapon-carrying mechanoids, warcaskets and some modded races. The rules act at death, so they must not rely on stripping to reach gear.
- **Loot that is not inventory.** Mechanoid reward materials are not held in the pawn's inventory. The rules leave them alone.
- **Alien races and modded gear.** Rules read standard item properties, so no special handling is planned.

## The Progression pack

The pack is the Progression collection, Workshop id 3521297585, about 900 mods. Its preset uses the Progression defaults and era-typical yields for instant disassembly, because the item-based yield would hand a tribal colony plasteel on the battlefield with no bench.

The table is drawn from the pack mods' Workshop descriptions. Their code has not been read.

| Pack mod | What it means here |
| --- | --- |
| Progression: Core (3301868772) and Node Research (3729878405) | The colony moves through eras, starting as low as Animal tech, and foundation projects gate each advance. Confirm where these mods store the colony's era. |
| Peto Progression (3791277458) | Players may sit in one era for a long time by choice, so loot rules shape most of the playthrough. |
| Progression: Ammunition (3798845290) | The pack's own ammo capacity and refill packs. The ammo system to test first. |
| Progression: Arsenal (3798837810) | Lists uncraftable weapons. They have no research behind them, so the carrier faction's era decides their fate. |
| Semi Random Research: P-Fork (3778002533) | Research is chosen from a few offered projects. Study pays only into the project the player picked from that offer. |
| Research Papers (3492739424) | Finished research lives in papers that can be lost. Check how it reacts to a project completed by study, and whether losing a paper wipes partial progress. |
| Visual Progression Framework (3834891951) | Already works out the colony's age and each faction's age for looted gear. Read the era the same way so the two mods agree. |
| World Tech Level (3414187030) | Worlds capped at a tech level produce little out-of-reach loot, so the rules should stay quiet there with no setup. |
| [FSF] Better Exploration Loot (3526957922) | Adds loot away from raids. Scrap modes ignore it. Original-item mode blocks its out-of-reach gear. |

Pack tweaks: the tier table and default settings ship as XML that the pack can patch, with an option to lock chosen settings. The colony's era is read through one function so the pack's era system can supply the value.

## Left out of the first release

| Item | Why not now |
| --- | --- |
| Scrap that remembers its source item | Generic scrap per era was chosen because it stacks. Exact materials are available through instant disassembly. |
| Filtering quest rewards | A different system from pawn drops. Original-item mode already blocks out-of-reach rewards from being used. |
| Keeping gear on pawns | That is Non uno Pinata's behaviour and a different mod. This one works beside it. |
| Android Tiers and Project RimFactory support | Each needs mod-specific code. Revisit if users ask after release. |

## Decisions

| Decision | Outcome |
| --- | --- |
| Workshop title | Loot Scavenging |
| Package id | `kylemhb.lootscavenging` |
| Originality | Built from this spec and the game's code only. |
| Drop mode | Replaced 2026-11-25 (ticket 25) by one rule system: the item rules, applied to the groups of the Who list. A row can Apply item rules, Exclude, or Mark unfamiliar (the old "Keep the original item"). The four modes, their include switches and "Off" are gone. |
| Scrap form | Generic per era. The amount carries the value. |
| Scrap tiers | Six, neolithic to archotech, with Animal folded into neolithic. Ultratech and archotech were split from spacer 2026-11-21, so their research can be studied and their gear yields more. |
| Medicine and ammunition | Follow their own item rules, which default to Vanilla. Settled 2026-11-25 (ticket 25); until then each was exempt from the drop mode by a setting, from 2026-11-21 (medicine) and 2026-11-23 (ammunition). |
| Knowledge, genetics and body parts categories | Added 2026-11-24 from the pack survey, each kept by default with its switch off. Classification order: gear, ammunition, medicine, drugs, food, knowledge, genetics, body parts, usable items to other, raw materials, other. |
| Raw materials category | Stuff plus the `ResourcesRaw` and `Manufactured` thing categories (mortar shells excluded; components, chemfuel and neutroamine sit under `Manufactured`), a new inventory category kept by default. Settled 2026-11-24. |
| Materials and eras | Stuff (devilstrand, steel, stone) has no era and is never out of reach, because any colony can build and craft with it. Settled 2026-11-24, after a player's tribal raider dropped 53 spacer scrap from a stack of devilstrand a modded recipe had given a spacer era. |
| Scrap amount for stacks | Anything that stacks is rounded once per stack, at least one scrap per stack. Single items keep at least one scrap each. Settled 2026-11-24; ammunition alone had this from 2026-11-23. |
| Scrap is never worth more than its source | Settled 2026-11-24 (Kyle). A unit of a tier is worth the larger of the tier's `valuePerUnit` and the scrap market value setting. After the curve, units are cut to `floor(item or stack value / worth per unit)`, where the value is the thing's market value times its count. Zero means no scrap: the item is removed as with Delete, era-typical and components disassembly yield nothing, and an unfamiliar original has no scrap equivalent to recycle or study. Gear already worth more than its scrap (a 1,650 charge rifle gives 10 spacer units, 165) is unchanged. Studied scrap's research points are not part of its worth. |
| Recycling yields | A weighted loot pool per tier in XML, with a value per unit. One pick per unit, amount by market value. Lower tiers at about 15% weight. Static. No fixed lines, no keep-in-stock count, and bill text that names no materials. Changed from a fixed table 2026-11-21, because fixed yields oversupplied basic materials. |
| Recycling gate | The bench alone. A bench handles its tier and below. |
| Recycling place | Existing benches, no new building. |
| Original-item mode | The item counts as its scrap equivalent at the bench. |
| Unfamiliar label | A setting: all out-of-reach gear (default) or looted gear only. |
| Study location | At a research bench, as a job. |
| Study target | The project currently being researched, if of the scrap's era or lower. |
| Study completion | Study can finish a project. |
| Era advances | Study can pay for an era advance project, such as Node Research's emergence, like any other project. Settled 2026-11-21. |
| Nothing eligible | The study job waits. |
| Study eras | Fixed at the colony's era and below (ticket 25). |
| Recycle or study | A setting, defaulting to one or the other. |
| Study points | Settings, not XML: one per tier, lockable by a pack, typed in text boxes. Moved from the scrap defs 2026-11-23 at Kyle's request, after a Workshop report that a 3000-point raid gave about 70 industrial scrap, 2100 points. The multiplier was cut 2026-11-25. |
| Techprint projects | A setting, off by default. |
| Scrap value | One setting per era, defaulting to near zero (ticket 25). |
| Where scrap appears | The three "Don't drop your stuff" boxes: on the body when on, on the ground when off (ticket 25). |
| Unknown era | The carrying faction's era. The colony's era and components alternatives were cut (ticket 25). |
| Instant disassembly | Stays, with a setting for the colony era's pool or the item's own ingredients. "Materials of the colony's era" rolls the colony's era since 0.6.0 (ticket 25). |
| Default preset | Progression, with no first-load message. |
| Downed, stripped and prisoner gear | Replaced 2026-11-25 (ticket 25). No moment toggles: gear that comes off a group set to Apply is always sorted, and the Prisoners row covers a stripped prisoner and an arrest. A downed pawn keeps his weapon in hand when his group's "Don't drop your stuff" box is on, in every preset but Vanilla. |
| Other ways gear leaves a living pawn | Settled 2026-11-24, ticket 18 (Kyle asked for it built; the hook design is the implementation's). They count as the stripped-or-arrested moment, which since ticket 25 follows the Prisoners and group rows (the toggle that turned them off is gone). Arresting a standing pawn and trading one sort his weapon and inventory as he drops them (`Pawn.DropAndForbidEverything`). A weapon a non-colony pawn drops on its own, as when he loses the use of his arms, is sorted as it drops (`Pawn_EquipmentTracker.TryDropEquipment`). A pawn whose faction changes to the player's has his worn apparel sorted with strip's rules before the change, judged as the group he was. Joiners are now covered by their own settings (ticket 20, see "Pawns who join the colony"), no longer by the downed-and-strip setting. A drop the mod is already sorting (death, downing, strip) is never sorted again by these. Gear lost with a corpse that burns, rots away or is eaten is expected and stays lost. |
| Settings that equal the default | Settled 2026-11-24 (Kyle asked for it fixed), ticket 18. The mod's own save writes every key, a value equal to the default too, so a player's choice never follows a later change to a pack's or the mod's default. A file that lacks keys, written before 0.5.2 or by another tool such as Auto Mod Config, still takes the baseline for each missing key. |
| Cleanup preset | Hostile row Apply, every other row Exclude. From hostile dead, deletes weapons below Normal quality, apparel below Good, and either one below 50% hit points, and all ammunition. Removes every hostile humanlike corpse. Settled 2026-11-21; ammunition added 2026-11-23; rows rewritten 2026-11-25 (ticket 25). |
| Game versions | 1.6 and 1.7. |
| Scrap art | Made or commissioned by Kyle. Placeholder icons until then. |
| Credit | An inspired-by line on the Workshop page naming Looting Manager and Non uno Pinata. |
| Licence | GPLv3, as in `LICENSE`. Confirmed by Kyle 2026-11-21. |
| Hooks | Simplest hook that reaches the right moment. Rewrite a method only where a plain hook cannot. |

## Build order

Nine stages, each ending in a check that can be run in dev mode on a test colony.

| Stage | Delivers | Check |
| --- | --- | --- |
| 1. Core rules | Hooks at death, downed and strip. Owner and place checks. Weapons and apparel categories with keep and delete. | Kill a spawned raider: weapon gone, apparel kept. Kill a wild animal: nothing changes. |
| 2. Era reader | Colony era, item era and the three fallbacks. | A dev readout names the right era for vanilla, modded and uncraftable gear. |
| 3. Scrap | Scrap items per era, the amount calculation, and the Scrap outcome. | A tribal colony kills a pirate and gets a stack of industrial scrap sized by the gear. |
| 4. Recycling | Tier table in XML and bills on the named benches. | A machining table recycles industrial and medieval scrap. A smithy refuses industrial. A bench from another mod still shows all its own bills. |
| 5. Study | The study job, points to the current project, the era setting. | Studying same-era scrap advances the current project. Higher scrap is refused. Research factor zero writes no invalid value. |
| 6. Original-item mode | Unfamiliar label, equip block, bench processing as scrap equivalent. | A looted rifle cannot be equipped, recycles for the same yield as its scrap, and becomes usable once researched. |
| 7. Other outcomes and filters | Instant disassembly with both yields, inventory groups, protection, lists, corpses and butchering. | A raider carrying medicine and a quest weapon drops both untouched. A pawn killed off-map logs no error. |
| 8. Settings page | Six sections, presets, item picker, translation keys. | Each preset produces the behaviour its description states. |
| 9. Pack preset and compatibility | Pack XML preset. Runs with Progression: Ammunition, Progression: Hospitality, Research Papers, Semi Random Research and Non uno Pinata. | No errors in each run, and research completed by study behaves like bench research. |

Stages 1 to 5 make a playable mod in the default mode. Stage 6 is the costliest single stage, because original-item mode is a second way of handling the same gear.

## Unverified

- ~~Exact signatures and call order of the death, downed and strip methods on 1.6 and 1.7.~~ Confirmed 2026-11-21 from a decompile of the 1.7.5014 game assembly and the 1.6.4871 reference assembly. The signatures are the same in both. The call order comes from 1.7 only, because the 1.6 reference assembly has no method bodies.
  - **Death:** `public override void Kill(DamageInfo? dinfo, Hediff exactCulprit = null)` on `Pawn`. Kill records whether the pawn was spawned, then runs the pre-death changes: worn apparel takes damage and is marked as worn by a corpse. Next it despawns the pawn and makes the corpse. Only then, and only if the pawn was spawned, does `DropAndForbidEverything()` drop equipment and inventory, forbidden. Apparel is never dropped and stays on the corpse. A pawn with no map, in a caravan or with no corpse drops nothing. Kill has no early return, so only another mod's prefix can cancel a death. The hook is a prefix with the lowest priority. At that point the gear is still on the pawn and the apparel is still undamaged. The prefix does not depend on stripping.
  - **Downed:** `private void MakeDowned(DamageInfo? dinfo, Hediff hediff)` on `Pawn_HealthTracker` (the pawn is its private field `pawn`). It returns with an error if the pawn is already downed. Otherwise, if the pawn is spawned, it calls `DropAndForbidEverything(keepInventoryAndEquipmentIfInBed: true, rememberPrimary: true)`, which drops equipment and inventory but never apparel, and drops nothing if the pawn is in bed. The hook is a prefix with the same conditions.
  - **Strip:** `public void Strip(bool notifyFaction = true)` on `Pawn`. `Corpse.Strip` forwards to it, as do the strip job, the butchering ingredient step and caravan forming. In a caravan, gear moves to other caravan members. Otherwise equipment, apparel and inventory drop unforbidden; locked apparel drops only from a dead pawn. Then the faction is notified. The hook is a prefix that acts only on living pawns outside a caravan, because a corpse's gear was already sorted at death.
- ~~Where the Progression pack stores the colony's era, and which benches and materials it has per era.~~ Read 2026-11-21 from the installed mods' code (Progression: Core 1.6 source, Node Research 1.6 source, VFE Tribals 1.6 assembly, Visual Progression source; the 1.7 game loads their 1.6 folders), and run in `lootscavenging-progression`.
  - **Era:** the pack keeps it in `Faction.OfPlayer.def.techLevel`. Node Research generates `BRM_Emergence_<era>` projects, each one tech level below its target; finishing one sets the faction's tech level. VFE Tribals' advancement writes it too, saves it in its game component and puts it back on load. Visual Progression reads it. Node Research stops the player starting projects above it. Progression: Core puts every player faction def back to its startup value on a new game.
  - **Before the fix:** the colony's era was the highest finished research. Right after an advance it stayed one era behind until the new era's first project was finished, so that project refused the new era's scrap ("scrap above colony era") and item-era gear of the new era stayed out of reach.
  - **Now:** with VFE Tribals or Node Research active, the colony's era is the higher of the two readings (`EraReader.FactionEraMods`). Without them the faction's tech level is not read, because vanilla's is static: Crashlanded stays Industrial with its research reset.
  - **Emergence projects:** to the study gate they are ordinary projects, and study may pay for an advance (Kyle, 2026-11-21). Neolithic scrap is refused for `BRM_Emergence_Medieval` while its foundation prerequisites are unfinished, and eligible once they are done.
  - **Benches:** Progression: Production moves Smithing to Neolithic and puts the research bench behind its own `Ferny_ResearchBench` project. None of the installed pack mods' patches remove or rename a recycling bench. With Core, Production and Node Research, every tier's bench exists, can be built and carries its recycle bill, and every pool material is a loaded def. The pools themselves are this mod's own, unchanged by the pack. Only the installed pack mods were checked, not the whole collection.
- How the game treats points added to a project whose techprints are missing.
- ~~How Research Papers and Semi Random Research react to a project completed by study.~~ Read 2026-11-21 from Research Papers' 1.6 assembly (the 1.7 game loads it) and Semi Random Research: P-Fork Continued's 1.7 assembly, and run in `lootscavenging-progression`.
  - **Research Papers:** study pays through `ResearchManager.AddProgress(project, points, pawn)`, which calls `FinishProject` with the researcher, so Research Papers' prefix leaves a paper at the researcher, as bench research does. Destroying the paper sets progress to `lostResearchRetainPercent` of the cost (default 75%; observed 225 of 300). Study can then finish the project again, and a new paper is made. Research Papers ignores emergence projects and VFE Tribals' projects.
  - **Semi Random Research:** it rejects `ResearchManager.SetCurrentProject` unless its tracker applies the pick, so the current project is always the picked offer, and study pays only into it. Its `AddProgress` prefix may add a project to the offers, but study only pays the current project, so the offers did not change. A project finished by study goes through its finish handling with no error.
- ~~Whether any Workshop mod is already called Loot Scavenging.~~ Checked 2026-11-21 on the RimWorld Workshop search: no item is called Loot Scavenging or Loot Scavenger. The nearest titles are "Scavenging" (Romyashi) and "Neolithic Scavenging" (Mythos, and Mlie's continuation).
