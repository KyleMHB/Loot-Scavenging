# Loot Scavenging: review of 0.6.0

Reviewed: [KyleMHB/Loot-Scavenging](https://github.com/KyleMHB/Loot-Scavenging), branch `main` at
[`06cf5d9`](https://github.com/KyleMHB/Loot-Scavenging/commit/06cf5d95cd2a868e263a88410c0e24bd9cd36704),
on 2026-11-27. The Workshop upload (item 3844457170, version 0.6.0) is a build of that commit: its
defs, language files and textures match it apart from line endings. Line links point at `06cf5d9`.

Every game test ran on RimWorld 1.7.5021, in a separate copy of the game with its own mods folder
and save folder: Harmony, Core, all five DLCs and Loot Scavenging. Lab-only mods rode along: a bridge
and a console that let a script drive the game, and a small helper that spawns pawns with
set gear, kills or downs them, has a warg eat a corpse one bite at a time through the call the
eating job makes, sets a corpse alight, presses the Gear tab's drop button, and asks Loot
Scavenging's own methods for their verdicts. The map was a quicktest colony: a Crashlanded start with
research at its starting projects, on a temperate tile, with incidents, fire spread and wild animals
turned off so nothing else touched the test ground. Settings were the defaults unless a finding says
otherwise.

Three builds took turns: no Loot Scavenging at all (the control, to show what the base game does),
the Workshop version, and a local copy of `06cf5d9` carrying the fix suggested in findings 1 and 2,
used only to check that fix. RimWorld 1.6 was not run. We did this review with Claude Code. Every
finding was checked against the code, and the ones marked "run in game" were reproduced in the lab.

## Summary

Outside the findings below, the rules did what the settings page says in every case we ran. A
raider's beyond-reach revolver and flak armour became scrap and his in-reach gear stayed whole. Each
item was judged once, and a downed raider kept his weapon in hand. Ejected charges came back as
chemfuel (10 from a smokepop belt, 30 from a firefoampop pack). A joiner's gear was marked or
scrapped by his row. No session logged an error from the mod apart from the one in finding 2. All
403 unit tests pass.

The first two findings come from keeping loot on the body, which 0.6.0 made the default. Findings 3
to 6 concern unfamiliar gear and joiners, and 7 and 8 come from the benches:

1. Loot kept on a body is destroyed with the corpse. A predator, a fire or the weather takes the
   scrap, silver and medicine with it, where the base game had already dropped them on the ground.
2. A dead colonist's weapon and pack now stay in the corpse, and the Gear tab's drop button for them
   logs an error and does nothing.
3. Setting any loot row to Mark locks the colony's own starting rifle, revolver and flak armour.
4. Anomaly gear unlocked by a knowledge project stays beyond reach after its research is done.
5. The walk-in wanderer, the joiner players see most, skips "Other joiners: Apply".
6. On the defaults, the sentence under "Unfamiliar label" describes a setting that does nothing.
7. Recycling an unfamiliar item re-rolls it when the roll comes up empty.
8. Study ignores the research cost penalty a tribal colony pays at the bench.

Smaller issues, release packaging, documentation and the test suite follow.

## 1. Loot kept on a body is destroyed with the corpse

A raid leaves bodies by the killbox. Before anyone strips them, a pack of wargs eats two. With Loot
Scavenging the scrap from those raiders' rifles, their silver and their medicine are gone. In the
base game the wargs would have eaten only the bodies, because everything the raiders carried was
already on the ground.

With the defaults a pawn who dies keeps everything on the body. The kill prefix holds the gear back
([GearHooks.cs L54-L85](https://github.com/KyleMHB/Loot-Scavenging/blob/06cf5d95cd2a868e263a88410c0e24bd9cd36704/Source/Loot/GearHooks.cs#L54-L85)),
and the postfix puts the weapon, the scrap made from it and the pack into the dead pawn's inventory
([BodyLoot.cs L140-L161](https://github.com/KyleMHB/Loot-Scavenging/blob/06cf5d95cd2a868e263a88410c0e24bd9cd36704/Source/Loot/BodyLoot.cs#L140-L161)).
The setting's text says it stays there "until the corpse is stripped, butchered or cremated"
([Keyed L61](https://github.com/KyleMHB/Loot-Scavenging/blob/06cf5d95cd2a868e263a88410c0e24bd9cd36704/Languages/English/Keyed/LootScavenging.xml#L61)).
A corpse can go other ways, and on every one of them the game destroys the corpse's inventory with
it: a predator finishing the corpse, fire, and the weather wearing an outdoor corpse down to nothing.

Run in game. Five pawns were killed side by side on the Workshop build with the defaults, then one
corpse each was eaten, burnt or stripped. The control build ran the same steps:

| Corpse | Held in the corpse | Afterwards | Control build |
|---|---|---|---|
| Raider, eaten by a warg in 5 bites | 3 industrial scrap (his revolver), 40 silver, 2 industrial medicine | all destroyed | revolver, silver and medicine on the ground, forbidden; only the body eaten |
| Raider, burnt | 10 industrial scrap, 10 chemfuel from his smokepop belt, 40 silver, 2 industrial medicine | all destroyed | his gear on the ground, only the body burnt (fire could not spread in the lab) |
| Raider, stripped | 15 industrial scrap, 40 silver, 2 industrial medicine | all on the ground | all on the ground |
| A trade caravan's pack muffalo, from a non-hostile faction, eaten in 7 bites | 8 spacer scrap (a charge rifle among its trade goods), 300 silver, 5 industrial medicine, a parka, 4 components | all destroyed | all on the ground |

The muffalo row shows a second thing: the animal's trade goods are judged as its gear, so the
caravan's charge rifle became scrap when the muffalo died on the colony's map. Whether trade goods
belong under the item rules is your call. Losing them to a warg is the bug.

Predators are the clear case. In a real fire, loot lying on the ground can burn in the base game too,
though the fire has to reach it; loot inside a burning corpse goes with the corpse every time. The
weather case was not run: corpses outdoors lose hit points like other items, and one worn to nothing
goes through the same destroy call as the fire, while silver on the ground never wears out. A
colonist's corpse holds that colonist's weapon and pack by default (finding 2), and the same steps
destroy those.

The fix we ran is a Harmony prefix on `Corpse.Destroy`. When the corpse is spawned and the dead
pawn's inventory holds anything, it drops that inventory beside the corpse, forbidden unless the
pawn was the colony's. Strip, butcher and cremate bills strip first, and so does the mod's own
corpse removal, so they reach it empty. On the fix build the same steps left every item on the
ground where the corpse had been, the muffalo's 300 silver and parka included, and the strip came
out unchanged. The patch is `fix-corpse-loot.patch` in the evidence zip.

## 2. A dead colonist's Gear tab drop button does nothing

A colonist dies in the field. Their rifle is not on the ground beside them: it is in the corpse, and
the Gear tab lists it under Inventory with a drop arrow (figure 1). Pressing the arrow does nothing,
and the debug log gets an error. The rifle stays in the corpse until somebody strips it, and a
colonist carried to a grave unstripped is buried with it.

Since 0.6.0 "Colonists keep their gear" is on by default and keeps a dead colonist's weapon and
pack in the corpse
([BodyLoot.cs L114-L127](https://github.com/KyleMHB/Loot-Scavenging/blob/06cf5d95cd2a868e263a88410c0e24bd9cd36704/Source/Loot/BodyLoot.cs#L114-L127),
[Keyed L58](https://github.com/KyleMHB/Loot-Scavenging/blob/06cf5d95cd2a868e263a88410c0e24bd9cd36704/Languages/English/Keyed/LootScavenging.xml#L58)).
The game's Gear tab lets the player handle a dead colonist's things and draws a drop button on each
inventory row. That button drops the item on the dead pawn's own map, and a pawn inside a corpse has
no map: only the corpse has one. The drop fails with `Dropped Gun_BoltActionRifle38388 in a null
map.` and the rifle stays put. In the base game the tab's inventory list is empty by then, because
the rifle and pack dropped at death, so players never met this button.

Run in game on the Workshop build. The tab reported it could control the dead colonist and drew
three inventory rows with drop buttons. Pressing the rifle's logged the error above, and the rifle
was still in the corpse. On the control build the same colonist's rifle, medicine and meals lay
beside the body, and the tab had no inventory rows.

The fix we ran is a prefix on the tab's drop. For an item in a dead pawn's inventory whose corpse is
spawned, it drops the item beside the corpse. On the fix build the rifle landed next to the corpse
and nothing was logged. It is in the same patch file as finding 1.

![Figure 1. A dead colonist's Gear tab on the Workshop build: the rifle, medicine and meals are in the corpse, each with a drop arrow that does nothing.](loot-scavenging-review/01-gear-tab-dead-colonist.png)

## 3. A Mark row locks the colony's own starting gear

A player wants raiders' unfamiliar guns kept whole instead of scrapped, so they set the Hostile row
to Mark. On a Crashlanded start the colony's own bolt-action rifle, revolver, flak vest, flak pants
and flak helmet now read "(unfamiliar)", and colonists can't equip or wear them ("unfamiliar
technology").

The scope setting defaults to "All gear beyond your research". Under it, any beyond-reach item is
unfamiliar as soon as one loot row is on Mark
([Unfamiliar.cs L49-L62](https://github.com/KyleMHB/Loot-Scavenging/blob/06cf5d95cd2a868e263a88410c0e24bd9cd36704/Source/Unfamiliar/Unfamiliar.cs#L49-L62),
[UnfamiliarRule.cs L22-L25](https://github.com/KyleMHB/Loot-Scavenging/blob/06cf5d95cd2a868e263a88410c0e24bd9cd36704/Source/Model/UnfamiliarRule.cs#L22-L25)).
Beyond reach includes unfinished research, and a Crashlanded colony starts without Gunsmithing or
Flak armor, so five of its seven starting items qualify. The sentence under the scope warns about
trader and quest gear
([Keyed L163](https://github.com/KyleMHB/Loot-Scavenging/blob/06cf5d95cd2a868e263a88410c0e24bd9cd36704/Languages/English/Keyed/LootScavenging.xml#L163))
but not about what the colony starts with. By the scenario defs, the Rich Explorer's starting charge
rifle is locked the same way.

Run in game on the Workshop build. With the defaults all seven starting items were familiar. With
the Hostile row on Mark, five read unfamiliar and the colonist could not equip them. After
Gunsmithing was finished the rifle and revolver cleared, and the flak pieces stayed locked.

If the default scope is meant to cover the colony's own gear, the sentence should say so and name
starting gear. If it is not, the simpler change is to make "Looted only" the default scope. That
scope saves a marker on looted items and leaves the rest alone.

## 4. Anomaly gear unlocked by a knowledge project stays beyond reach

An Anomaly colony finishes Insanity weaponry and crafts shard insanity lances. Under a Mark row the
lances read "(unfamiliar)" and nobody can wear them. Under the defaults, a cultist's shard lance
still becomes Archotech scrap when he dies.

An item's era comes from the tech level of its research project
([EraReader.cs L68-L87](https://github.com/KyleMHB/Loot-Scavenging/blob/06cf5d95cd2a868e263a88410c0e24bd9cd36704/Source/Era/EraReader.cs#L68-L87)).
Anomaly's knowledge projects, Insanity weaponry and Mutation weaponry among them, have no tech level
and no research cost. The colony-era reading skips them on purpose
([L106-L117](https://github.com/KyleMHB/Loot-Scavenging/blob/06cf5d95cd2a868e263a88410c0e24bd9cd36704/Source/Era/EraReader.cs#L106-L117)),
but the item reading does not handle them: with no research era it falls through to the item's own
tech level
([EraRanking.cs L83-L104](https://github.com/KyleMHB/Loot-Scavenging/blob/06cf5d95cd2a868e263a88410c0e24bd9cd36704/Source/Model/EraRanking.cs#L83-L104)),
which is Archotech for the shard lances. No research project in the base game or the DLCs is
Archotech, so the colony era never gets there and the lance is beyond reach for good, research done
or not.

Run in game on the Workshop build. The shard insanity lance read "Archotech, from the item, out of
reach" with the colony at Industrial, before Insanity weaponry was finished and after. With the
Hostile row on Mark a colonist could not wear one after the research either. With the defaults, a
raider wearing one was killed after the research and the lance became 6 Archotech scrap. As a
control, the bolt-action rifle cleared as soon as Gunsmithing was finished.

A fix: when every prerequisite of an item is finished and none has a tech level, treat the item as
in reach. Royalty's psychic shock and insanity lances raise a related design point. They are
Archotech with no recipe, so under a Mark row they can never become familiar, while the inspect line
says gear is unfamiliar "until its research is finished"
([Keyed L4](https://github.com/KyleMHB/Loot-Scavenging/blob/06cf5d95cd2a868e263a88410c0e24bd9cd36704/Languages/English/Keyed/LootScavenging.xml#L4)).

## 5. The walk-in wanderer skips "Other joiners: Apply"

A player sets Other joiners to Apply so that a joiner's beyond-reach gear becomes scrap like a
raider's. The next wanderer walks in holding a charge rifle, whole and not marked.

The game's "wanderer joins" quest makes the wanderer as a world pawn with no faction. When the
player accepts, the quest sets his faction to the colony first and only then brings him onto the
map. Under Apply the join hook sorts his gear with the item rules
([JoinerHooks.cs L44-L48](https://github.com/KyleMHB/Loot-Scavenging/blob/06cf5d95cd2a868e263a88410c0e24bd9cd36704/Source/Loot/JoinerHooks.cs#L44-L48)),
and the sorter returns at once for a pawn on no map
([GearSorter.cs L18-L22](https://github.com/KyleMHB/Loot-Scavenging/blob/06cf5d95cd2a868e263a88410c0e24bd9cd36704/Source/Loot/GearSorter.cs#L18-L22)).
The file's header comment names that limit
([L19-L21](https://github.com/KyleMHB/Loot-Scavenging/blob/06cf5d95cd2a868e263a88410c0e24bd9cd36704/Source/Loot/JoinerHooks.cs#L19-L21)),
but its list of covered paths starts with "the quests of wanderers"
([L9-L10](https://github.com/KyleMHB/Loot-Scavenging/blob/06cf5d95cd2a868e263a88410c0e24bd9cd36704/Source/Loot/JoinerHooks.cs#L9-L10)).
Mark is unaffected, since marking needs no map.

Run in game on the Workshop build, through the game's own incident and the letter's accept, with
the wanderer's weapon swapped for a charge rifle before accepting:

- Other joiners on Mark (the default): he walked in holding it, marked unfamiliar.
- Other joiners on Apply: he walked in holding it, unmarked, and nothing was sorted.
- Apply, with a factionless pawn already on the map made a colonist: the rifle became 8 spacer
  scrap. This is how the suite's wanderer case runs
  ([MomentsBatch.cs L1349](https://github.com/KyleMHB/Loot-Scavenging/blob/06cf5d95cd2a868e263a88410c0e24bd9cd36704/DevTests/MomentsBatch.cs#L1349),
  [L1367](https://github.com/KyleMHB/Loot-Scavenging/blob/06cf5d95cd2a868e263a88410c0e24bd9cd36704/DevTests/MomentsBatch.cs#L1367)),
  which is why it passes.

A fix: when the joiner is on no map, leave a one-shot flag on him and sort his gear when he next
spawns, which for this quest is a moment later at the map edge.

## 6. The "Unfamiliar label" sentence is wrong on the defaults

On the defaults, the settings page explains "Unfamiliar label" with the sentence for an active
setting: any gear beyond your research is unfamiliar, wherever it came from (figure 2). Nothing is
unfamiliar on the defaults. In game a charge rifle, power armour and an assault rifle each read as
familiar, and a colonist could equip all three.

The page picks the sentence with a check that counts all six Who rows
([SettingsPage.cs L503](https://github.com/KyleMHB/Loot-Scavenging/blob/06cf5d95cd2a868e263a88410c0e24bd9cd36704/Source/SettingsUI/SettingsPage.cs#L503),
[L512-L520](https://github.com/KyleMHB/Loot-Scavenging/blob/06cf5d95cd2a868e263a88410c0e24bd9cd36704/Source/SettingsUI/SettingsPage.cs#L512-L520)),
and the default Other joiners row is on Mark. The label itself goes by `AnyLootRowMarks`, which
counts the four loot rows only
([LootRules.cs L138-L139](https://github.com/KyleMHB/Loot-Scavenging/blob/06cf5d95cd2a868e263a88410c0e24bd9cd36704/Source/Model/LootRules.cs#L138-L139),
[Unfamiliar.cs L49](https://github.com/KyleMHB/Loot-Scavenging/blob/06cf5d95cd2a868e263a88410c0e24bd9cd36704/Source/Unfamiliar/Unfamiliar.cs#L49)),
because a joiner's mark is saved on each item and holds whatever the scope says. The variant ending
"Only used for groups set to Mark unfamiliar"
([Keyed L165](https://github.com/KyleMHB/Loot-Scavenging/blob/06cf5d95cd2a868e263a88410c0e24bd9cd36704/Languages/English/Keyed/LootScavenging.xml#L165))
is the one that should show. Checking `r.AnyLootRowMarks` at L503 makes the page agree with the
game.

![Figure 2. The settings page on the defaults (Workshop build): the scope sentence reads as active, though no loot row is on Mark.](loot-scavenging-review/02-settings-unfamiliar.png)

## 7. Recycling an unfamiliar item re-rolls it when the roll comes up empty

A recycle bill on a damaged unfamiliar item sometimes ends with the item back on the floor and
nothing made. The pawn picks it up and works it again until a roll yields something.

Each scrap unit picks one material and rounds its amount at random, so a unit can return nothing
([Recycling.cs L85-L92](https://github.com/KyleMHB/Loot-Scavenging/blob/06cf5d95cd2a868e263a88410c0e24bd9cd36704/Source/Model/Recycling.cs#L85-L92),
[L109-L111](https://github.com/KyleMHB/Loot-Scavenging/blob/06cf5d95cd2a868e263a88410c0e24bd9cd36704/Source/Model/Recycling.cs#L109-L111)).
When a whole roll is empty, the original-item worker puts the item back instead of destroying it
([OriginalItemRecipes.cs L127-L144](https://github.com/KyleMHB/Loot-Scavenging/blob/06cf5d95cd2a868e263a88410c0e24bd9cd36704/Source/Unfamiliar/OriginalItemRecipes.cs#L127-L144)).
The class comment says the give-back is for an item that stopped being unfamiliar before the finish,
but an empty roll takes the same branch. Scrap, by contrast, is used up on an empty roll.

Items worth one scrap unit roll empty often, and a flak vest at half its hit points is one. We rolled
`ScrapEquivalent.RecycleProducts` 20,000 times per item in game:

| Item | Hit points | Scrap units | Empty rolls | Yield per item used, against one roll |
|---|---|---|---|---|
| Flak vest | 100 of 200 | 1 | 20.8% | ×1.26 |
| Revolver | 50 of 100 | 1 | 20.5% | ×1.26 |
| Charge rifle | 15 of 100 | 1 | 21.8% | ×1.28 |
| Charge rifle | 25 of 100 | 2 | 4.3% | ×1.05 |
| Charge rifle | 100 of 100 | 8 | 0% | ×1.00 |

So a one-unit item returns about a quarter more than the scrap it stands for. Each empty try costs a
full job, and on a "Do X times" bill each one uses up a count, because the game counts the iteration
before the worker runs. No pawn worked the bill in the lab: the rolls were run in game and the
give-back branch was read. A fix: give the item back only when `ScrapEquivalent.TryGet` fails, and
destroy it on an empty roll as scrap would be.

## 8. Study pays the same points whatever the colony's tech level

Read, not run. At the research bench, progress is divided by the project's cost factor for the
researcher's faction. A project above the faction's tech level costs half as much again for each
level, up to Industrial, which is what keeps a tribal colony slow. Study pays through `AddProgress`
([StudyRecipes.cs L204](https://github.com/KyleMHB/Loot-Scavenging/blob/06cf5d95cd2a868e263a88410c0e24bd9cd36704/Source/Study/StudyRecipes.cs#L204)),
which adds the points as they are, and the points depend only on the scrap tier and the
storyteller's research factor
([Study.cs L151-L155](https://github.com/KyleMHB/Loot-Scavenging/blob/06cf5d95cd2a868e263a88410c0e24bd9cd36704/Source/Model/Study.cs#L151-L155)).

On a New Tribe start the faction is Neolithic, so study is worth 1.5 times bench research on a
Medieval project and twice on an Industrial or Spacer one. The "+N" message also disagrees with the
research tab, which scales progress by the same factor: 100 studied points show as 150 on a Medieval
project. VFE Tribals and Node Research raise the faction's tech level as the colony advances, which
narrows the gap in the Progression pack.

If this is intended, the bill description could say so. If not, divide the paid points by the
project's cost factor for `Faction.OfPlayer`. Either way the message could show the tab's figure.

## Smaller issues

Behaviour:

- The corpse texts say corpses go for "groups set to Apply item rules"
  ([Keyed L170-L176](https://github.com/KyleMHB/Loot-Scavenging/blob/06cf5d95cd2a868e263a88410c0e24bd9cd36704/Languages/English/Keyed/LootScavenging.xml#L170-L176)),
  but Mark rows count too; only Exclude keeps a corpse
  ([CorpseEvaluator.cs L82](https://github.com/KyleMHB/Loot-Scavenging/blob/06cf5d95cd2a868e263a88410c0e24bd9cd36704/Source/Model/CorpseEvaluator.cs#L82)).
- Mechanoid weapons become scrap. The game destroys a dead lancer's or centipede's weapon, so in the
  base game a mech raid yields none. Here each charge lance, heavy charge blaster or inferno cannon
  becomes spacer scrap
  ([GearHooks.cs L76](https://github.com/KyleMHB/Loot-Scavenging/blob/06cf5d95cd2a868e263a88410c0e24bd9cd36704/Source/Loot/GearHooks.cs#L76)).
  The suite asserts it, so it looks deliberate. If it is, the Steam text could say mech raids now pay
  in spacer scrap.
- Stacked gear loses or spreads its looted or join marker. `CompUnfamiliar` has no `PostSplitOff`,
  `AllowStackWith` or `PreAbsorbStack`, so a hauler splitting a marked stack of grenades gets an
  unmarked piece, and marked and unmarked stacks merge. This only matters under "Looted only" or a
  join mark.
- Studying an unfamiliar item uses the whole item even when the project needs a few points. A
  charge rifle, worth eight spacer units, studied into a project five points short is destroyed for
  five points
  ([Study.cs L195-L201](https://github.com/KyleMHB/Loot-Scavenging/blob/06cf5d95cd2a868e263a88410c0e24bd9cd36704/Source/Model/Study.cs#L195-L201)),
  where eight units of scrap would spend one and keep seven.
- If the project finishes or changes while a study job runs, the job pays nothing, puts the scrap
  back and still uses a "Do X times" count
  ([StudyRecipes.cs L172-L183](https://github.com/KyleMHB/Loot-Scavenging/blob/06cf5d95cd2a868e263a88410c0e24bd9cd36704/Source/Study/StudyRecipes.cs#L172-L183)).
  Two researchers on one nearly finished project will meet it.
- "Keep X in stock" on a study bill counts scrap anywhere on the map, ignoring the bill's "count in"
  stockpile choice
  ([StudyRecipes.cs L233-L245](https://github.com/KyleMHB/Loot-Scavenging/blob/06cf5d95cd2a868e263a88410c0e24bd9cd36704/Source/Study/StudyRecipes.cs#L233-L245)).
- The work bar reads the recipe's nominal work, while the job's real work is set at the start
  ([BillWorkPatch.cs L53-L72](https://github.com/KyleMHB/Loot-Scavenging/blob/06cf5d95cd2a868e263a88410c0e24bd9cd36704/Source/BillWorkPatch.cs#L53-L72)).
  The bar stays empty for most of a study job and opens part-full on an original-item job.
- The study bench's bills tab draws no paste button
  ([Building_StudyBench.cs L86-L93](https://github.com/KyleMHB/Loot-Scavenging/blob/06cf5d95cd2a868e263a88410c0e24bd9cd36704/Source/Study/Building_StudyBench.cs#L86-L93)),
  so a copied study bill can't be pasted.
- Study points never reach the pawn's "research points researched" record, which bench research
  adds to and `AddProgress` does not.
- The scrap inspect line says it "can be studied now" when no bench on the map can research the
  current project
  ([CompStudyInfo.cs L18-L25](https://github.com/KyleMHB/Loot-Scavenging/blob/06cf5d95cd2a868e263a88410c0e24bd9cd36704/Source/Study/CompStudyInfo.cs#L18-L25));
  the bill does check the bench.
- The chance step of the item rules draws from an unseeded `System.Random`
  ([LootEvaluator.cs L43](https://github.com/KyleMHB/Loot-Scavenging/blob/06cf5d95cd2a868e263a88410c0e24bd9cd36704/Source/Model/LootEvaluator.cs#L43),
  [L56](https://github.com/KyleMHB/Loot-Scavenging/blob/06cf5d95cd2a868e263a88410c0e24bd9cd36704/Source/Model/LootEvaluator.cs#L56)),
  because the game's caller passes no roll
  ([GearSorter.cs L101](https://github.com/KyleMHB/Loot-Scavenging/blob/06cf5d95cd2a868e263a88410c0e24bd9cd36704/Source/Loot/GearSorter.cs#L101)).
  Everything else uses `Rand.Value`. Under Multiplayer a chance below 100% would desync; passing
  `() => Rand.Value` there fixes it.

Settings page:

- Scrap market value is rounded to two places on every frame
  ([SettingsPage.cs L370-L374](https://github.com/KyleMHB/Loot-Scavenging/blob/06cf5d95cd2a868e263a88410c0e24bd9cd36704/Source/SettingsUI/SettingsPage.cs#L370-L374))
  while the text box keeps what was typed, so typing 0.015 leaves "0.015" on the page and 0.01 in
  the setting.
- Locked checkboxes still flip for one frame before the lock puts them back. The page greys them with
  `GUI.enabled`
  ([L573-L580](https://github.com/KyleMHB/Loot-Scavenging/blob/06cf5d95cd2a868e263a88410c0e24bd9cd36704/Source/SettingsUI/SettingsPage.cs#L573-L580)),
  which the game's checkbox ignores; it skips the toggle only when its own `disabled` argument is
  set.
- The two Disassemble categories share one efficiency value but keep a text buffer each
  ([L262-L263](https://github.com/KyleMHB/Loot-Scavenging/blob/06cf5d95cd2a868e263a88410c0e24bd9cd36704/Source/SettingsUI/SettingsPage.cs#L262-L263)),
  so after editing one, the other box shows the old number.
- A pack that locked 0.5.2's death drop (`deathDrop` or `rules.dropMode`) now locks the 0.6.0
  keep-gear boxes at their defaults
  ([SettingLocks.cs L65](https://github.com/KyleMHB/Loot-Scavenging/blob/06cf5d95cd2a868e263a88410c0e24bd9cd36704/Source/Model/SettingLocks.cs#L65),
  [L68](https://github.com/KyleMHB/Loot-Scavenging/blob/06cf5d95cd2a868e263a88410c0e24bd9cd36704/Source/Model/SettingLocks.cs#L68)),
  which may be the opposite of what the pack locked. We know of no pack that does this.

Cost (none measured; each matters only on a large research tree):

- `EraReader.ColonyEra` walks every research project on each call
  ([EraReader.cs L29-L42](https://github.com/KyleMHB/Loot-Scavenging/blob/06cf5d95cd2a868e263a88410c0e24bd9cd36704/Source/Era/EraReader.cs#L29-L42)).
  The scrap inspect line calls it every frame, and `ScrapEquivalent.TryGet` calls it twice per item
  ([ScrapEquivalent.cs L32-L34](https://github.com/KyleMHB/Loot-Scavenging/blob/06cf5d95cd2a868e263a88410c0e24bd9cd36704/Source/Unfamiliar/ScrapEquivalent.cs#L32-L34)),
  which the original-item bills' ingredient search runs per candidate. `Unfamiliar` already keeps a
  250-tick cache of the colony era that these could share.
- `Enum.GetValues` allocates on every scrap tier lookup
  ([ScrapStudy.cs L77](https://github.com/KyleMHB/Loot-Scavenging/blob/06cf5d95cd2a868e263a88410c0e24bd9cd36704/Source/Study/ScrapStudy.cs#L77));
  a static array of the tiers exists in `SettingValues`.
- The two-argument `CanEquip` postfix repeats the four-argument one, since the game's two-argument
  overload calls the other.

## Release packaging

- The upload has no licence file. The repository is GPLv3, and the GPL expects its text to travel
  with the binary; copying `LICENSE` into the upload covers it.
- Texture paths start at `Things/Item/Scrap/`
  ([Scrap.xml L56](https://github.com/KyleMHB/Loot-Scavenging/blob/06cf5d95cd2a868e263a88410c0e24bd9cd36704/Defs/ThingDefs/Scrap.xml#L56)),
  a folder every mod shares, so another mod's file at the same path would replace these. A
  `LootScavenging/` root avoids that.
- `AGENTS.md` points at files on an `E:` drive that are not in the repository, and still says "No
  gameplay code exists yet"
  ([L3-L5](https://github.com/KyleMHB/Loot-Scavenging/blob/06cf5d95cd2a868e263a88410c0e24bd9cd36704/AGENTS.md#L3-L5)).
- The project file's game and Harmony paths default to the same drive
  ([LootScavenging.csproj L23](https://github.com/KyleMHB/Loot-Scavenging/blob/06cf5d95cd2a868e263a88410c0e24bd9cd36704/Source/LootScavenging.csproj#L23),
  [L32](https://github.com/KyleMHB/Loot-Scavenging/blob/06cf5d95cd2a868e263a88410c0e24bd9cd36704/Source/LootScavenging.csproj#L32)).
  The build takes `-p:RimWorldPath=` and `-p:HarmonyPath=` and works on a Mac with them; a line in
  the README would save a contributor the search.

## Documentation

- The README says the Workshop settings file name "is not confirmed yet"
  ([README.md L26](https://github.com/KyleMHB/Loot-Scavenging/blob/06cf5d95cd2a868e263a88410c0e24bd9cd36704/README.md#L26)).
  The game names the file `Mod_<folder>_<class>.xml` from the mod's folder name, and a Workshop
  subscription's folder is its item number, so `Mod_3844457170_LootScavengingMod.xml` is right.
- The same paragraph says importing a preset replaces the player's settings, while the example
  preset's comment says Auto Mod Config merges a changed version into them
  ([Preset.xml L3-L4](https://github.com/KyleMHB/Loot-Scavenging/blob/06cf5d95cd2a868e263a88410c0e24bd9cd36704/Extras/Loot-Scavenging-AutoConfig/Defs/Preset.xml#L3-L4)).
  One of the two is wrong.
- The example settings files still use 0.5-era names: "Don't drop your stuff" for what the page now
  calls "Keep gear on the body", and "Keep (shown as Vanilla)" for the outcome now shown as "Drop as
  normal"
  ([L26](https://github.com/KyleMHB/Loot-Scavenging/blob/06cf5d95cd2a868e263a88410c0e24bd9cd36704/Extras/Loot-Scavenging-AutoConfig/Settings/Mod_3844457170_LootScavengingMod.xml#L26),
  [L34](https://github.com/KyleMHB/Loot-Scavenging/blob/06cf5d95cd2a868e263a88410c0e24bd9cd36704/Extras/Loot-Scavenging-AutoConfig/Settings/Mod_3844457170_LootScavengingMod.xml#L34)).
  The old name also appears in code comments and TESTING.md.
- The Progression preset's explanation leaves out recruited prisoners
  ([Keyed L47](https://github.com/KyleMHB/Loot-Scavenging/blob/06cf5d95cd2a868e263a88410c0e24bd9cd36704/Languages/English/Keyed/LootScavenging.xml#L47)).

## What checked out

- The Workshop upload matches `06cf5d9`.
- Every XML file parses, and no ThingDef defName ends in a digit. The twelve new ThingDefs (six scrap
  tiers and six studied scrap) take no save number from a terrain, rock, ore or other number-stored
  def. We checked against Core and the DLCs, and against a 201-mod list. Saves store those defs by
  number, so a clash would swap terrain in existing maps.
- All 403 unit tests pass under `dotnet test` on macOS.
- Kill, downing and strip ran with no errors from the mod, and each item was judged once per moment.
- The joiner, equip, apparel-score and research-finish hooks find their targets, each with one
  overload or named by its arguments, in both 1.6.4871 and 1.7.5013 (read, not run on 1.6).
- The settings file keys, lock names and presets agree with each other, and the English, Chinese
  and Russian language files carry the same keys.
- A marked item saves two integers, only when they are non-zero, so removing the mod leaves nodes the
  game ignores.

## Not covered

- RimWorld 1.6 in game. TESTING.md says the 1.6 build is checked by compiling only, and we did not
  run it either.
- Multiplayer.
- Bills worked by pawns. The study and recycle jobs were read, and the recycle rolls were run
  directly.
- The Progression pack's modlist, VFE Tribals, Node Research, Combat Extended and Hospitality.
- A corpse destroyed by the weather (read only).
- Removing the mod from a save in progress.

## Tests

The suite does more than most mods have. 403 unit tests cover the pure model: era ranking, the item
and corpse rules, presets, and the study and recycling maths. TESTING.md lists the in-game batches
(smoke, outcomes, moments, settings, bench, saves, ui and progression) on your own runner, with what
each case asserts.

Most of the findings slip through for one of two reasons. The moments batch asserts that gear sits
in the corpse (`colonist-killed-keeps-gear` and the hostiles-box cases) and stops there; no case
destroys a corpse another way or presses the Gear tab on one. Other cases build the scene the
convenient way. The wanderer is spawned on the map before he joins. Smoke resets research on a
Crashlanded colony, so the faction stays Industrial (finding 8). The Mark-row cases use the
"Looted only" scope (finding 3). The original-item recycle bound was also widened to allow for
random rounding
([TESTING.md L431](https://github.com/KyleMHB/Loot-Scavenging/blob/06cf5d95cd2a868e263a88410c0e24bd9cd36704/TESTING.md#L431)),
which hides finding 7's extra yield.

Cases that fail today and pass with the fixes:

- A raider killed with loot kept on the body, his corpse then eaten to nothing through
  `Thing.Ingested`, and another burnt: every item on the ground.
- The Gear tab's drop pressed on a dead colonist's rifle: the rifle beside the corpse, no error.
- The Hostile row on Mark with the default scope on a Crashlanded start: the starting rifle
  equippable, or the documented behaviour asserted.
- Insanity weaponry finished: a shard insanity lance in reach.
- The wanderer incident accepted through its letter under Other joiners on Apply: his gear sorted.
- A one-unit original-item recycle with an empty roll: the item used up.
- A Neolithic faction studying into a Medieval project: points scaled, if that is the intent.
- A marked stack of grenades split: the piece keeps the marker.

## Evidence

`loot-scavenging-review-evidence.zip` holds the lab's scenario outputs and the Player.log of every
run (the control, the Workshop build and the fix build), the recycle roll counts, the hash checks,
the unit test output, the fix patch, the screenshots, and the helper mod's source with the scripts
that drove it.
