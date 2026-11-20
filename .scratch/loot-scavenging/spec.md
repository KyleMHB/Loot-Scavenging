# Loot Scavenging: spec

Status: needs-triage

Written 2026-11-20 from the design doc at https://claude.ai/artifact/WFrwwPtCFzMNo7rbTxdsCK. This file is the copy that implementation works from. If the two disagree, raise it before coding.

## Goal and scope

Loot Scavenging decides what happens to gear when a non-colony pawn drops it: keep, degrade, scrap, disassemble or delete. Scrap can then be recycled for materials or studied for research at benches the colony already has. It is an original mod, informed by what the comments on Looting Manager show players want and where they hit problems.

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

Degrade, scrap, recycling, study, protection lists, presets and the era rules have no counterpart in Looting Manager.

## Loot rules

Every item a pawn drops passes the same ordered checks, and the first check that says "leave it" ends the run with the item untouched.

1. **Owner.** Colony pawns are never affected. Other pawns fall into four groups, each with its own toggle: hostile, non-hostile faction, no faction (wild animals, wild people), and the colony's prisoners.
2. **Place.** An optional "home map only" setting leaves loot alone on caravan, quest and outpost maps.
3. **Moment.** Rules always apply on death. A setting extends them to gear dropped when a pawn is downed or stripped. The Progression preset turns it on and switches the prisoner group on, so out-of-reach gear cannot be taken intact from a downed raider or a stripped prisoner. The Cleanup preset leaves it off.
4. **Protection.** Quest-tagged items, unique weapons, persona weapons, relics and anything on the player's keep list are never touched.
5. **Category.** The item is matched to one category rule: weapons, apparel, or an inventory group (ammunition, drugs, medicine, food, other).
6. **Conditions.** The category rule can be limited to items beyond the colony's reach, below a quality, or below a hit-point percentage.
7. **Chance.** The category's own chance is rolled.
8. **Outcome.** The category's outcome is applied.

A whole dropped stack is treated as one item, and a partial drop acts on the piece that left the pawn.

| Outcome | What the player sees |
| --- | --- |
| Keep | Vanilla behaviour. |
| Degrade | The item survives at a random hit-point level inside a set range, 10% to 40% by default. An option also lowers its quality one step. |
| Scrap | The item becomes stackable scrap of its era, to be recycled or studied at a bench. This is the default for out-of-reach gear. |
| Disassemble | The item is replaced on the spot by materials, with no bench work. A setting chooses the yield: era-typical materials from the scrap table, or the item's own ingredients at a set efficiency. |
| Delete | The item vanishes. |

Loaded charges in reloadable gear follow their own setting: eject, or go with the item.

**Out of reach** means the item's research is unfinished or its era is above the colony's current era. An item's era is found in this order:

1. The tech level of the research project that unlocks it.
2. The item's own tech level.
3. The era of the faction that carried it.

Step 3 covers modded and uncraftable gear with no era of its own. Two settings replace it for players who prefer a blunter fallback: treat such gear as the colony's own era, or break it straight into components.

**Drop mode** is one setting that decides what out-of-reach gear becomes.

| Drop mode | Effect |
| --- | --- |
| Scrap out-of-reach gear (default) | Gear within reach follows its category rule. Gear beyond it becomes scrap. |
| Scrap everything | All gear enemies drop becomes scrap. Maximum cleanup, no usable loot. |
| Keep the original item | Out-of-reach gear drops whole but cannot be used until researched. It can be recycled or studied as if it were scrap. |
| Off | No era handling. Out-of-reach gear follows its category rule like any other gear. Used by the Cleanup and Vanilla presets. |

All four modes ship in the first release.

## Scrap, recycling and study

Out-of-reach gear becomes scrap: one stackable item per era, where the amount carries the gear's value. Scrap is recycled for materials or studied for research, at benches the colony already owns.

### Scrap

- **One item per era.** Scrap is generic. It does not remember the item it came from, so it stacks fully and a large raid leaves a few piles.
- **Amount is value.** A stone knife becomes one unit of neolithic scrap and a charge rifle becomes several units of spacer scrap, scaled by the item's worth, quality and condition.
- **Low market value.** Scrap is worth close to nothing by default, so a stockpile kept for a later era does not raise colony wealth or sell for silver. A setting changes this.
- **Where it appears.** On the ground by default, like other drops. A setting leaves it on the body, to be collected when the corpse is stripped, butchered or cremated.
- **Original-item mode.** When the drop mode keeps the original item, that item counts as its scrap equivalent at the bench: the same era, amount, yields and points. It is labelled unfamiliar and cannot be equipped until its research finishes. A setting chooses which gear gets the label: all out-of-reach gear from any source (default), or only gear looted from pawns. The default is worked out from research and saves nothing. Looted-only needs a marker saved on each looted item.

### Recycling

Recycling is a bill at an existing bench, with no new building.

- **The bench is the gate.** Each scrap tier names a bench. A bench recycles its own tier and every tier below it. There is no second check on the colony's era.
- **Fixed yields per tier.** Each unit of scrap returns era-typical materials from a table.
- **One XML file.** The table holds the bench and the yields for each tier, so a pack can patch both without code.

| Scrap tier | Proposed bench | Proposed yield |
| --- | --- | --- |
| Neolithic | Crafting spot | Wood and leather |
| Medieval | Smithy | Steel and cloth |
| Industrial | Machining table | Steel, with a chance of components |
| Spacer and above | Fabrication bench | Plasteel, with a small chance of advanced components |

These are vanilla starting values. The Progression pack's own benches and materials need checking before the table is fixed.

### Study

Study is a job in the Research work type: a researcher carries scrap to a research bench and uses it up for research points.

| Question | Answer |
| --- | --- |
| Where points go | The project currently being researched, if it is of the scrap's era or lower. |
| Finishing a project | Study can finish a project, through the game's normal completion. |
| Nothing eligible | When no project is active, or the active project is a higher era than the scrap, the study job waits and the scrap stays in storage. Its inspect text says why. |
| Which scrap can be studied | A setting. By default the colony's current era and below, so higher scrap waits until the colony advances. |
| Recycle or study | A setting. By default each unit is used for one or the other. The alternative studies it first and then recycles the remains at reduced value. |
| How much | A fixed number of points per unit for each tier. |
| Techprint projects | A setting, off by default. Off, projects that need techprints receive nothing. On, study pays into them even before the techprints are applied. Projects with a special study source never receive anything. |
| Time and skill | Study time scales with the points paid. Research speed applies and the researcher gains Intellectual experience. |

In the two scrap modes the mod saves nothing in the game file and adds no data to items, so it can be removed mid-game by first clearing the scrap.

### Requirements learned from Technology Blueprints & Reverse Engineering

That mod has paid research from items since 2020, and its comments show where the idea goes wrong.

- **Flat points, not a percentage.** Project costs vary widely, so a share of the cost is unfair. Fixed points per unit of scrap already does this.
- **Never divide by the research speed setting.** With the storyteller's research factor at zero, that mod wrote an infinite or invalid value into project progress. Study adds points through the game's research manager and is tested at research factors of zero and above 100%.
- **No current project is a normal state.** The job does not start.
- **Say what the player gets.** The scrap's inspect text shows the points per unit and whether it can be studied now, and finishing a study shows the gain and the project.
- **No clicking per item.** Study and recycling are bench work with the usual bill controls, including a keep-in-stock count.
- **Never consume what the player wants to keep.** In original-item mode, an item is studied or recycled only when the player marks it or a filter allows it.
- **Add bills without touching other recipes.** Reports describe other mods' bench recipes disappearing while that mod was installed. Recycling bills are added to the named benches only, and the recycling stage checks that a bench from another mod still shows all its own bills.

## Corpses and butchering

Corpse cleanup is its own rule with a chance and three separate toggles: humanlike, animal and mechanoid.

- The corpse's gear goes through the loot rules first, so nothing bypasses them.
- The owner and place checks apply to corpses too. A wild animal's corpse is only removed if the "no faction" group is switched on.
- A pawn that dies without leaving a corpse is skipped quietly.

A separate option removes worn apparel when a corpse is butchered, so tainted clothing stops piling up in the butchering room while raiders still drop gear normally.

## Settings page

The page is grouped into six sections.

| Section | Controls |
| --- | --- |
| Presets | Vanilla, Cleanup, Progression, and Reset to defaults |
| Who and where | Owner groups, prisoners, home map only, on death or on any drop |
| Item rules | One row per category: outcome, chance, conditions |
| Scrap and study | Drop mode, which eras can be studied, recycle or study, scrap value, where scrap appears, era fallback, disassembly yield, techprint projects, unfamiliar label scope |
| Corpses | Humanlike, animal, mechanoid, chance, butchering option |
| Lists | Always keep, always remove, protection toggles |

- **Default preset is Progression.** A one-time message on first load says what the mod is doing and where the presets are.

The presets set these values. Cleanup's numbers are proposed and can change before stage 8.

| Setting | Progression | Cleanup | Vanilla |
| --- | --- | --- | --- |
| Drop mode | Scrap out-of-reach gear | Off | Off |
| Moment | Death, downed and strip | Death only | Not used |
| Owner groups | Hostile, non-hostile faction, no faction, prisoners | Hostile only | None |
| Weapons and apparel | Keep within reach | Delete below Normal quality or below 50% hit points, otherwise keep | Keep |
| Inventory groups | Keep within reach | Keep | Keep |
| Corpses | Off | Humanlike on at 100%; animal and mechanoid off | Off |
| Butchering removes worn apparel | Off | On | Off |

Cleanup leaves animal corpses for food and mechanoid corpses for smelting, so it removes only what a colony has no use for.
- **Presets** set every setting at once. The page shows which preset is active and whether it has been changed.
- **Each row says what will happen** in a sentence under it.
- **Lists** are picked from a searchable item menu and stored by item name, so a removed mod does not break the settings.
- **Outcome sliders** for degrade range and disassemble efficiency sit beside the outcome they affect and are hidden when unused.

Settings are global. All text is translatable from the first release.

## Code structure

The mod sorts a pawn's gear just before the game drops it.

The rules run at the moments a pawn's gear changes hands: when it dies (`Pawn.Kill`), when it is downed (`Pawn_HealthTracker.MakeDowned`), and when it is stripped (`Pawn.Strip`). A hook before each of those walks the pawn's equipment, apparel and inventory, applies each item's outcome, and leaves the rest to the game.

The rules do not depend on the drop itself. Non uno Pinata keeps gear on the body by replacing the game's drop-everything call, so rules attached to that call would never run beside it. Acting on the gear before any drop works either way.

**Hook rule:** use the simplest hook that reaches the right moment, and rewrite a game method only where a plain hook cannot. If the hook before death proves too early, for example when another mod cancels the death, rewriting the death method is the fallback and the overlap with Non uno Pinata is handled explicitly.

| Part | Job |
| --- | --- |
| Settings | Holds the rules, lists and presets, read once and cached. |
| Rule evaluator | Takes a pawn, an item and the map, returns an outcome. No side effects, so it can be reasoned about and logged. |
| Outcome applier | Degrades, scraps, disassembles or deletes. Owns material placement and forbidding. |
| Hooks | Thin Harmony patches that call the evaluator and applier. |
| Settings window | Draws the page and the item picker. |
| Era reader | One function returns the colony's era and one returns an item's era, with the fallbacks from the loot rules. Everything era-related calls these two. |
| Scrap items and tier table | XML only: one scrap item per era, plus the bench and yields for each tier. |
| Recycling bills | Generated from the tier table at startup and attached to the named benches. |
| Study job | A work giver and job in the Research work type, modelled on how the game handles techprints. |

Item-based disassembly uses the mod's own calculation from the item's cost list and material, not the vanilla smelt function. Materials are placed nearby in full stacks and forbidden like other enemy drops.

A dev-mode log line per decision makes bug reports answerable.

The method names above, and `ResearchManager`, `JobDriver_ApplyTechprint` and `EquipmentUtility.CanEquip`, exist in the installed 1.7.5014 game assembly. Their exact signatures and the order of calls on death still need confirming in a decompiler before coding.

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
| Progression: Arsenal (3798837810) | Lists uncraftable weapons. They have no research behind them, so the era fallback decides their fate. |
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
| Workshop title | Loot Scavenging (scrap, recycle, study) |
| Package id | `kylemhb.lootscavenging` |
| Originality | Built from this spec and the game's code only. |
| Drop mode | A setting with four modes, defaulting to scrap for out-of-reach gear. "Off" was added so Cleanup and Vanilla can run without era handling. All four ship first. |
| Scrap form | Generic per era. The amount carries the value. |
| Recycling yields | Fixed era-typical table in XML. |
| Recycling gate | The bench alone. A bench handles its tier and below. |
| Recycling place | Existing benches, no new building. |
| Original-item mode | The item counts as its scrap equivalent at the bench. |
| Unfamiliar label | A setting: all out-of-reach gear (default) or looted gear only. |
| Study location | At a research bench, as a job. |
| Study target | The project currently being researched, if of the scrap's era or lower. |
| Study completion | Study can finish a project. |
| Nothing eligible | The study job waits. |
| Study eras | A setting, defaulting to the colony's era and below. |
| Recycle or study | A setting, defaulting to one or the other. |
| Techprint projects | A setting, off by default. |
| Scrap value | A setting, defaulting to near zero. |
| Where scrap appears | Ground by default, with a setting for the body. |
| Unknown era | The carrying faction's era, with settings for the colony's era or straight to components. |
| Instant disassembly | Stays, with a setting for era-typical or item-based yields. |
| Default preset | Progression, with a one-time first-load message. |
| Downed, stripped and prisoner gear | The Progression preset applies the rules on downed and on strip, with the prisoner group on. Cleanup applies them on death only. |
| Cleanup preset | No era handling. Deletes worn-out or low-quality weapons and apparel from hostile dead, removes humanlike corpses and strips apparel at butchering. Exact values in the presets table are proposed. |
| Game versions | 1.6 and 1.7. |
| Scrap art | Made or commissioned by Kyle. Placeholder icons until then. |
| Credit | An inspired-by line on the Workshop page naming Looting Manager and Non uno Pinata. |
| Hooks | Simplest hook that reaches the right moment. Rewrite a method only where a plain hook cannot. |

## Build order

Nine stages, each ending in a check that can be run in dev mode on a test colony.

| Stage | Delivers | Check |
| --- | --- | --- |
| 1. Core rules | Hooks at death, downed and strip. Owner and place checks. Weapons and apparel categories with keep and delete. | Kill a spawned raider: weapon gone, apparel kept. Kill a wild animal: nothing changes. |
| 2. Era reader | Colony era, item era and the three fallbacks. | A dev readout names the right era for vanilla, modded and uncraftable gear. |
| 3. Scrap | Scrap items per era, the amount calculation, and the two scrap drop modes. | A tribal colony kills a pirate and gets a stack of industrial scrap sized by the gear. |
| 4. Recycling | Tier table in XML and bills on the named benches. | A machining table recycles industrial and medieval scrap. A smithy refuses industrial. A bench from another mod still shows all its own bills. |
| 5. Study | The study job, points to the current project, the era setting. | Studying same-era scrap advances the current project. Higher scrap is refused. Research factor zero writes no invalid value. |
| 6. Original-item mode | Unfamiliar label, equip block, bench processing as scrap equivalent. | A looted rifle cannot be equipped, recycles for the same yield as its scrap, and becomes usable once researched. |
| 7. Other outcomes and filters | Degrade, instant disassembly with both yields, inventory groups, protection, lists, corpses and butchering. | A raider carrying medicine and a quest weapon drops both untouched. A pawn killed off-map logs no error. |
| 8. Settings page | Six sections, presets, item picker, translation keys. | Each preset produces the behaviour its description states. |
| 9. Pack preset and compatibility | Pack XML preset. Runs with Progression: Ammunition, Progression: Hospitality, Research Papers, Semi Random Research and Non uno Pinata. | No errors in each run, and research completed by study behaves like bench research. |

Stages 1 to 5 make a playable mod in the default mode. Stage 6 is the costliest single stage, because original-item mode is a second way of handling the same gear.

## Unverified

- Exact signatures and call order of the death, downed and strip methods on 1.6 and 1.7.
- Where the Progression pack stores the colony's era, and which benches and materials it has per era.
- How the game treats points added to a project whose techprints are missing.
- How Research Papers and Semi Random Research react to a project completed by study.
- Whether any Workshop mod is already called Loot Scavenging. A web search found none, which is not a full check.
