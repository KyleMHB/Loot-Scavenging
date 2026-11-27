# External review fixes: spec

An outside reviewer tested 0.6.0 (Workshop build `06cf5d9`) and sent `review.md`, with a corpse fix in `reviewer-fix/`. This spec lists what we fix from it. Every item below was checked against this branch's code and the 1.7 `Assembly-CSharp.dll` on 2026-11-27. Where the review and the code disagree, the spec follows the code and says so.

## Rules for every fix

- **Saves:** a fix never removes or renames a saved field. A new saved value has a default that matches 0.6.0 behaviour for a save that lacks it.
- **Settings:** a player's 0.6.0 and 0.5.2 settings load unchanged unless this spec names the change. Mod settings save a value only when it differs from its default (`GAME.md`, "Mod settings omit default values"), so changing a default moves every player who never touched it.
- **Text:** a changed or new Keyed string is updated in English, ChineseSimplified and Russian, with terms from `translation/glossary.md`. `Log.Error` lines stay English and marked `// not translated`.
- **Reviewer's code:** `reviewer-fix/CorpseLootFixes.cs` is a starting point for tickets 01 and 02, not code to paste. Read it, then write the patch in this project's style.
- **Tests:** the review's "Tests" section lists cases that fail today. Add tests only where a ticket says so.
- **Originality:** the rules in `AGENTS.md` apply as usual.

## Findings

| # | Finding | Fix |
| --- | --- | --- |
| 1 | Gear kept on a body (pack and held weapon in `pawn.inventory`) is destroyed with the corpse when a predator eats it, fire burns it or it rots away. `Corpse.Destroy` clears the corpse, then `PostCorpseDestroy` destroys the pawn's equipment, inventory and apparel. | Prefix `Corpse.Destroy`: when the corpse is spawned and the inner pawn's inventory holds anything, drop it near the corpse, forbidden unless the pawn was a colonist. Strip and butcher already empty the inventory first (`Pawn.Strip`, and `autoStripCorpses` on butcher recipes), so the prefix finds nothing there. Cremation does not strip (`Recipes_Cremation.xml` sets `autoStripCorpses` false), so the prefix skips a corpse destroyed by a cremation bill (decision 5). A pawn kind with `canStrip` false butchered at a bench would drop at the bench, but `BodyLoot.CanHoldLoot` already keeps those pawns from holding loot. |
| 2 | A dead colonist's Gear tab drop button does nothing and logs "Dropped X in a null map", because the item sits in a dead pawn's inventory with no map. | Prefix `ITab_Pawn_Gear.InterfaceDrop`: when the item's holder is a dead pawn's inventory and the corpse is spawned, drop it at the corpse. |
| 3 | With any loot row on Mark and the default "All gear beyond your research" scope, the colony's own starting gear beyond its research becomes unfamiliar. | Intended (decision 1). The scope sentence says starting gear counts. |
| 4 | Gear whose only prerequisites are Anomaly knowledge projects stays beyond reach after the research is done. Knowledge projects have no tech level, so `EraReader` falls back to the item's own tech level (Archotech for shard gear). | When every prerequisite is finished and none has a tech level, the item is in reach. Gear with no recipe and no research (Royalty psychic lances) keeps its own tech level, and its inspect text no longer says "until its research is finished". |
| 5 | A wanderer who walks in under "Other joiners: Apply" is not sorted. They join off the map, and `GearSorter.Sort` returns at once when `pawn.MapHeld` is null. | When a joiner is skipped for having no map, set a saved one-shot flag on them and sort their gear the first time they spawn. |
| 6 | On the default settings the "Unfamiliar label" sentence reads as if it applies, because `SettingsPage.cs` L497 uses `AnyRowMarks`, which counts the joiner rows too. | Use `v.rules.AnyLootRowMarks`, as `Unfamiliar.cs` L49 and L123 do. |
| 7 | When recycling an unfamiliar item rolls no products, the item is given back and can be re-rolled until it pays. | Give the item back only when `ScrapEquivalent.TryGet` fails. An empty roll uses it up, as scrap does. |
| 8 | Study adds points with `ResearchManager.AddProgress`, which only adds and caps. Bench research (`ResearchPerformed`) divides by `CostFactor(researcher.Faction.def.techLevel)` and adds to the pawn's `ResearchPointsResearched` record. Study does neither, so a tribe studying industrial scrap gets the points an industrial colony gets, and the researcher's record never counts it. | Divide the points by the cost factor (decision 2). Add the points to the researcher's record, and the study message shows the points as the research tab counts them. |
| 9 | `LootEvaluator` falls back to `System.Random` when no roll is passed, and `GearSorter` passes none, which desyncs under Multiplayer. | Pass `() => Rand.Value` from every game-side caller. Keep the parameter so tests can pass a fixed roll. |

## Study and recycle

- **Project changed mid-job:** `Notify_IterationCompleted` (`StudyRecipes.cs` L171-184) reads the project when the job ends. If it finished, was cleared or is not eligible, the item goes back and nothing is paid, but the bill's "Do X times" count is still spent. Do not spend the count when nothing was paid. A changed but eligible project is paid, which is fine. The review is right here and the earlier spec wording ("uses up the item") was wrong.
- **No message:** in the same case, and when `Study()` returns early with no points, nothing tells the player. Show a message naming why.
- **Whole unfamiliar item for a few points:** scrap stacks use only the units needed (`StudyRules.UnitsUsed`). An unfamiliar item is always used whole, even when the project needs 5 points. Leave the item unused when the project needs less than some share of its worth; the ticket picks the share and says it in the bill text.
- **Stock count:** `RecipeWorkerCounter_Study.CountProducts` (`StudyRecipes.cs` L233-244) counts every thing of the def on the map, ignoring the bill's "count in" stockpile. Honour `bill.includeSlotGroup` as the game's counter does. Recycle bills use the game's counter and are fine.
- **Work bar:** `BillWorkPatch` sets `workLeft` at the start, but the bar divides by the recipe's nominal work amount, so it starts part-filled or overflows whenever real work differs. Make the bar's total match, for study, recycle and unfamiliar-item bills.
- **Paste button:** `ITab_StudyBills` has no paste button. Add the game's copy and paste of bills (`BillUtility.Clipboard`).
- **Inspect line:** "can be studied now" shows when the verdict is Eligible, with no check that the map has a bench that can research the project. Say it only when one exists.

## Settings page

- **Locked boxes flip:** `Toggle` (`SettingsPage.cs` L574-580) wraps the box in `SetDisabled`, which only sets `GUI.enabled`. The game's checkbox ignores that and flips the value; only its own `disabled` argument stops it. Pass `disabled`. The category boxes at L266, L268 and L270 are not greyed at all under a locked category; give them the same treatment.
- **Market value:** L372-374 rounds to two places and stores it, while the text buffer keeps the typed text, so "0.015" shows but 0.01 is saved. Round on display too, or store what the buffer shows.
- **Disassemble buffers:** the efficiency buffers are keyed per category (L262), but the value is shared, so the other box shows its old text. Use one buffer for the shared value.
- **0.5.2 lock on `deathDrop`:** `SettingLocks.cs` L68 maps a lock on `deathDrop` to the hostile and non-hostile keep-gear boxes, and a locked box takes its Def default, which is on. A pack that locked `deathDrop` at Ground (drop as in the base game) gets both boxes locked on, the opposite of what it set. Lock the boxes to the value the pack's `deathDrop` maps to. `rules.downedGear` (L69) has the same problem. This can silently change a modpack's locked behaviour, so the ticket checks it against a 0.5.2 settings file with that lock.

## Smaller fixes

- **Corpse text:** `CorpseHumanlikeOn`, `CorpseAnimalOn` and `CorpseMechanoidOn` say "Apply", but `CorpseEvaluator.CheckOwnerAndPlace` lets Mark rows through too. Reword in all three languages.
- **Stacking:** the unfamiliar mark is two per-thing fields on `CompUnfamiliar`. A split piece is a new thing and loses the mark, and marked and unmarked stacks merge. Copy the mark in `PostSplitOff` and refuse in `AllowStackWith` when the marks differ. Only stackable gear with weapon tags (grenades) is affected.
- **Duplicate patch:** `UnfamiliarHooks.cs` patches both `CanEquip` overloads, and the 2-argument one calls the 4-argument one. Drop the 2-argument postfix.

## Cost

- **`ColonyEra`:** walks every research project on each call, with no cache. It runs on every inspect redraw, per study ingredient candidate, and per scrapped or disassembled item. Cache it the way `Unfamiliar.cs` L156 stamps by 250 ticks, and clear the cache when a project finishes.
- **`Enum.GetValues`:** `ScrapStudy.TryGetScrapTier` (L77) allocates on each call, which runs per inspect redraw, per ingredient candidate and in `BillWorkPatch`. Cache the array.

## Packaging and docs

- **LICENSE:** `deploy.ps1` and `DevTests/devtests.json` deploy no LICENSE, so the Workshop upload has none. Add the file to both lists.
- **Textures:** decision 4. A move edits the 12 `texPath` lines in `Defs/ThingDefs/Scrap.xml` and `Defs/Study.xml`, the comment at `Scrap.xml` L5, the folders under `Textures/Things/Item/Scrap/`, and `tools/preview_icon.py` L18. `tools/workshop_gallery.py` reads `texPath` from the XML and needs no change. No C# refers to the path.
- **AGENTS.md:** says "No gameplay code exists yet" and points only at `E:` paths. Bring it up to date.
- **Build paths:** the csproj paths default to `E:`. Add a README line on the `-p:` overrides.
- **README settings file:** `Mod_3844457170_LootScavengingMod.xml` is the Workshop name. Drop "is not confirmed yet".
- **Preset import:** the README and the example's `About.xml` say importing replaces the player's settings, while `Preset.xml` says Auto Mod Config merges a changed version. Check what Auto Mod Config does and make all three say the same.
- **Extras names:** the example settings comments still say Exclude, Keep and Vanilla. Use Leave alone and Drop as normal, keeping the saved key values the code reads.
- **TESTING.md:** uses the old names. Update through `template-routing.md`.
- **Progression text:** `PresetProgressionExplain` never names recruited prisoners, though the preset sets them to Apply. Add them.
- **Mech weapons:** mechanoid weapons become scrap by design (`.scratch/loot-scavenging/issues/18-...` L56: such pawns "pay scrap for gear the game destroys"). Say so in the README.

## Decisions

Kyle answered these on 2026-11-27.

1. **Starting gear (finding 3): works as intended.** Gear beyond the colony's research is unusable until researched, starting gear included, because the colonists would not know how to use it. The default scope stays "All gear beyond your research". The scope sentence says starting gear counts, so a player is not surprised.
2. **Study and cost factor (finding 8): divide.** Study points are divided by the project's cost factor for the player's faction, as bench research does.
3. **Pack animal cargo: leave it alone.** A pack animal's trade goods are a caravan's stock, not gear, and are not run through the item rules. The ticket finds how the game marks caravan cargo before choosing the check.
4. **Texture path: move.** The scrap textures move to `Things/Item/LootScavenging/Scrap/`. Saves store the def by name, not the texture path.
5. **Cremation (finding 1): keep burning.** The corpse prefix skips a corpse destroyed by a cremation bill, so kept gear burns with it and the setting text ("until stripped, butchered or cremated") stays true.

## Tickets

| # | Ticket | Blocked by | Recommended model |
| --- | --- | --- | --- |
| 01 | Keep corpse loot when the corpse is destroyed (finding 1) | None | opus |
| 02 | Drop from a dead colonist's Gear tab (finding 2) | None | sonnet |
| 03 | Say that starting gear counts in the scope sentence (finding 3) | None | sonnet |
| 04 | Knowledge-project and no-recipe gear eras (finding 4) | None | sonnet |
| 05 | Sort walk-in joiners when they spawn (finding 5) | None | opus |
| 06 | Settings page fixes (finding 6, "Settings page") | None | opus |
| 07 | Recycling and stacking (finding 7, stacking, duplicate patch) | None | sonnet |
| 08 | Study fixes (finding 8, "Study and recycle") | None | sonnet |
| 09 | Multiplayer roll and cost (finding 9, "Cost") | None | sonnet |
| 10 | Pack animal cargo | None | sonnet |
| 11 | Packaging: LICENSE, textures, AGENTS.md, build paths | None | sonnet |
| 12 | Docs and texts (corpse text, "Packaging and docs" doc items) | 08 | sonnet |
| 13 | Changelog and 0.6.1 release | 01 to 12 | sonnet |

Ticket 06 is opus because the lock fix can change a modpack's locked behaviour.

## Left out

- The review's lab helper and lab runs stay with the review, not in this repo.
- Mods the review did not test (Combat Extended, Multiplayer itself) are not checked here beyond finding 9.
