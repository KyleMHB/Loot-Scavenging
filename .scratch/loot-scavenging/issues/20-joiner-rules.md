# 20: Rules for gear on pawns who join the colony

Status: done

**Recommended model:** `sonnet`

**Blocked by:** None. Ships in 0.5.2. Ticket 19 runs in parallel and also touches the settings files.

**Source:** Kyle, 2026-11-24. Ticket 18 sorts the apparel of a recruited or enslaved prisoner through the drop mode (`JoiningPrisoner.Sort` in `Source/Loot/GearHooks.cs`). Every other joiner keeps out-of-reach gear whole. Kyle wants gear sorted for every joiner, chosen by players.

## Decisions (Kyle's)

- Two settings, one rule each: **recruited or enslaved prisoners and slaves**, and **everyone else who joins** (wanderers, refugees, quest joiners, rescued pawns and the like).
- Each has the same three choices: **Mark out-of-reach gear unfamiliar**, **Follow the drop mode**, **Leave their gear alone**.
- Default for everyone else: **Mark unfamiliar**, as the safe choice.

## Choices made in the brief (not Kyle's; flag them in the report so Kyle can overrule)

- Default for prisoners and slaves: **Follow the drop mode**, which is what 0.5.2 already does.
- **Mark unfamiliar** must work in every drop mode, not only "Keep the original item". The joiner keeps wearing and holding the gear. The item is marked, and once it comes off, no colonist can equip or wear it until its research is done, as with looted unfamiliar gear. Use `CompUnfamiliar` (it already saves a looted era) or a sibling marker, and make `Unfamiliar.IsUnfamiliar` honour a join mark whatever the drop mode is. Protected items (quest, unique, persona, relic, keep list) are never marked.
- What is covered: worn apparel and equipment. Inventory isn't covered.
- Not counted as joining: pawns who were ever colonists (`PawnUtility.EverBeenColonistOrTameAnimal`), animals, mechanoids, babies born in the colony, and pawns at game start.

## Build

1. Settings: two enum values in `SettingValues`, saved like the rest (`forceSave`), lockable by packs, set in `Defs/SettingsDefaults.xml` and each preset. Add them to both settings files in `Extras/Loot-Scavenging-AutoConfig` and set the preset `<version>` to 0.5.2. Show them on the settings page near "Also when downed or stripped". The prisoner rule replaces the current gating: if prisoners are sorted today only with downed-and-strip on, "Follow the drop mode" now decides that on its own. Text: plain English, with every string in Keyed and translated into ChineseSimplified and Russian (`rimworld-translate` skill and `translation/glossary.md`).
2. Prisoners and slaves: route `JoiningPrisoner.Sort` through the new setting. Equipment is sorted as well as apparel.
3. Everyone else: find the faction change to the player for a living humanlike pawn who is not a prisoner of the colony (`Pawn.SetFaction`, prefix, `__state` if needed). Check the 1.7 assembly for the common join paths: wanderer join, refugee and quest joiners, Anomaly creepjoiners, and pawns generated straight into the player's faction by a quest, which never call `SetFaction`. Cover what is realistic and list in the ticket any path that isn't covered. Don't sort twice when a prisoner path also calls `SetFaction`.
4. "Follow the drop mode" uses `GearSorter.Sort` at `Moment.Strip`, with the pawn's owner group as before they joined.
5. Follow `PatchEach`: patch prefix, postfix and finalizer in one `[HarmonyPatch]` class. Check the 1.6 member names compile.

## Tests

- Unit tests for any new plain rule in `Source/Model`.
- DevTests cases (MomentsBatch or a new joiners batch): a wanderer joining in out-of-reach armour keeps wearing it and it's unfamiliar afterwards in the Scrap-out-of-reach drop mode; "Leave alone" leaves it unmarked; a recruited prisoner set to follow the drop mode has his gear scrapped; a quest-protected item is never marked; a former colonist rejoining is untouched. Update the SettingsBatch round trip.
- Run `dotnet test`, both builds, and the translation check. Kyle runs the full tier after merge, so don't run it.

## Docs

CHANGELOG 0.5.2 (no version bump): a line under Added for the two settings, replacing or adjusting the ticket 18 recruit line. Read `E:\Coding\Templates\writing-copy.md` first. Update `spec.md` (moments and settings) and add a TESTING.md line for the new cases.

## What was built

- `rules.prisonerJoinGear` and `rules.otherJoinGear` (`JoinerGear`: `MarkUnfamiliar`, `FollowDropMode`, `LeaveAlone`), in `LootRules`, `SettingsDefaults.xml`, the presets (Cleanup and Vanilla leave both alone), the lock names, both Auto Mod Config files (preset version 0.5.2), two rows on the settings page after "Also when downed or stripped", and ChineseSimplified and Russian text.
- `Source/Loot/JoinerHooks.cs`: one prefix and finalizer on `Pawn.SetFaction` and one on `RecruitUtility.Recruit` (it unlocks apparel and clears the prisoner status first), both through `JoiningGear.Begin`. `EnslaveHook` and `JoiningPrisoner` are gone: `SetGuestStatus(Slave)` calls `SetFaction` while the pawn is still a prisoner. A pawn handled by `Recruit` is marked in `SortedDrop` so the inner `SetFaction` skips him.
- Follow uses a new `Moment.Join`, not `Moment.Strip`, so the downed-and-strip toggle no longer gates it; the owner groups and home-map-only still do. Equipment and apparel are sorted, inventory is not; locked apparel stays.
- Mark: `CompUnfamiliar.joinedEra`, saved only when set. `Unfamiliar.IsUnfamiliar` honours it in every drop mode (`UnfamiliarRule.JoinMarkHolds`); `ScrapEquivalent` reads either marker's era. Protected items and locked apparel are not marked.
- Tests: `JoinerGearTests` (unit) and six cases in the moments batch, plus the cleanup round trip in the settings batch (TESTING.md, 2026-11-24).

## Join paths not covered

- A pawn generated straight into the player's faction with no `SetFaction`: the "game ended, new wanderers" incident (`IncidentWorker_WandererJoin`), and pawns at game start or born in the colony (the latter two are meant to be left alone).
- "Follow the drop mode" for a joiner with no map (some quest arrivals still in pods or in a quest's holding): the sorter needs a map, so nothing is sorted; the mark still works there.
- Quest lodgers are left out on purpose, since they leave again.
- The two new settings-page rows were not seen drawn: the ui batch could not draw on the test machine (TESTING.md).
