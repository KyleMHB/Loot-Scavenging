# 21: Downed pawns keep their gear in the pack; joiners' packs are covered

Status: done

**Recommended model:** `opus`. It changes when gear is judged at the core hooks, and a mistake loses loot or judges it twice.

**Blocked by:** None. Ships in 0.5.2, on top of tickets 18, 19 and 20, which are all merged on main.

**Source:** Kyle, 2026-11-24.

## Kyle's model of when gear is sorted

- **Death:** the pawn's gear is sorted.
- **Downing:** the weapon goes into the pawn's pack and nothing is sorted. Sorting happens later, when the pawn is stripped.
- **Arrest or enslavement:** the pawn's gear is sorted.
- **Joining:** the joiner's gear and pack are sorted.
- **Defaults:** stripping and arresting apply the preset's rules (usually scrapping gear an era above the colony's), and players can change those rules. Joiners have their gear and items marked unfamiliar by default.

## Decisions (agreed with Kyle)

1. **New setting, "When a non-colony pawn is downed",** with two choices:
   - **Keep the weapon in his pack** (default, every preset): nothing is judged at the downing. Equipment and inventory stay on the pawn; equipment moves into the pack. Nothing drops. Everything is sorted later, when the pawn is stripped, arrested, recruited or enslaved, or dies.
   - **Drop the weapon:** what the mod does today with downed handling on. Gear drops as in vanilla and is sorted as it drops.

   The principle: anything that leaves a pawn is sorted at that moment. Nothing drops unsorted.
2. **"Also when downed or stripped" becomes "Also when stripped or arrested"** (`onDownedAndStrip`; keep the key so saved files and pack locks still work). With it off, strip and arrest leave gear whole. Joining follows the two joiner settings whatever this says (ticket 20, kept as built). With it off and "Drop the weapon" chosen, downing drops gear whole, as today.
3. **Joiners' packs are covered** (ticket 20 did worn apparel and equipment only):
   - **Follow the drop mode:** the pack is sorted too, like a stripped prisoner's.
   - **Mark unfamiliar:** weapons and apparel in the pack are marked. Other items (components, medicine, drugs) are left as they are, because unfamiliar applies only to gear.
   - **Leave alone:** the pack isn't touched.
4. The "loot on the body" setting at death (`deathDrop`) is unchanged, default included.

## Build

- `PawnDownedHook`: under "Keep in pack", move equipment into the inventory and stop the game's downed drop for covered pawns (owner groups, home-map-only and the protections as today), with no judging. Mechanoids and pawns with no inventory drop as before. Colony pawns are never touched. Read the 1.7 `Pawn_HealthTracker.MakeDowned` and `Pawn.DropAndForbidEverything` paths to see what actually drops and stop exactly that.
- Once downing never judges in the default, check whether `BodyLoot.WasJudgedAtDowning` and the held-gear path are still needed. They are for "Drop the weapon" together with "everything stays on the body". Keep the code only where a path still uses it, and keep "judged once" true in every combination.
- Arrest of a downed pawn: capture drops gear through `DropAndForbidEverything` (ticket 18's `DropEverythingHook`). Check that a downed pawn whose weapon is in his pack is sorted at capture, and that a pawn downed in a bed and then arrested is sorted too. That path was unverified in ticket 18.
- A downed raider who gets back up keeps his pack. Check that he doesn't re-equip in a way that breaks anything; vanilla behaviour is fine.
- Settings page: the new choice sits beside "Also when stripped or arrested". It is saved, lockable, in `SettingsDefaults.xml`, in each preset and in both `Extras/Loot-Scavenging-AutoConfig` settings files. Put the new key in `Source/Model/SettingKeys` or wherever ticket 20 put its keys. Translate it (ChineseSimplified and Russian, `rimworld-translate` skill and `translation/glossary.md`).
- Joiners' packs: extend `JoinerHooks` and the mark (`CompUnfamiliar.joinedEra`) to inventory weapons and apparel.

## Tests

- Unit tests for any new plain rule in `Source/Model`.
- DevTests (MomentsBatch). Rework the downed cases for the new default:
  - a downed pirate's out-of-reach revolver ends in his pack whole, and nothing is on the ground;
  - stripping him then scraps it;
  - capturing him scraps it;
  - killing him scraps it;
  - "Drop the weapon" keeps today's behaviour;
  - "judged once" holds for downing then death, and for downing then capture;
  - with stripping off, strip leaves the revolver whole;
  - a joiner's pack revolver is marked under "Mark unfamiliar", and his medicine is untouched;
  - a joiner's pack is scrapped under "Follow".
- Update the SettingsBatch round trip.
- Run `dotnet test`, both builds and the translation check, plus the moments, settings, outcomes and smoke batches. Kyle runs the full tier after the merge, so don't run it.

## Docs

CHANGELOG 0.5.2 (no version bump). Read `E:\Coding\Templates\writing-copy.md` first.
- Under Added: the downed setting.
- Under Changed: downed pawns keep their weapon in the pack by default and aren't sorted until stripped, arrested or killed.
- "Also when downed or stripped" is now "Also when stripped or arrested".
- Joiners' packs are covered.
- Recruiting now follows its own setting even with stripping off.

Rewrite the ticket 18 and 20 lines where they would contradict these. Update `spec.md` (moments and settings), `steam-description.md` where it describes downing, and add a TESTING.md entry.

## What was built

- `rules.downedGear` (`DownedGear`: `KeepInPack`, `DropWeapon`), saved, lockable, in `SettingsDefaults.xml`, every preset (KeepInPack), both Auto Mod Config files and the settings page beside "Also when stripped or arrested" (the renamed `onDownedAndStrip`). ChineseSimplified and Russian text, with glossary rows for arrest and pack.
- `PawnDownedHook` under KeepInPack: for a covered pawn the held weapon goes whole into his pack and the game's downed drop is skipped; nothing is judged. The weapon the game drops when a downed pawn is spawned again also goes into the pack. DropWeapon is the old path.
- Pack weapons and apparel are classed as weapon or apparel for the item rules (`EraReader.IsGear`), not as other inventory, so a stowed weapon is judged at strip, capture, joining or death by the rule that would have judged it in his hands.
- `JoinerHooks`: Follow sorts the pack too; Mark marks out-of-reach weapons and apparel in the pack and leaves other items alone.
- Tests: unit tests for the Model rules (`dotnet test` 357). Moments: `downed-keeps-weapon-in-pack` (new: strip, capture, kill, and strip with stripping off), `drop-calls-judged-once` (downing then death and downing then capture, under both values), and the pack checks in `joiner-marked-unfamiliar` and `joiner-follows-drop-mode`. The settings round trip covers `downedGear`.
- Every DevTests case written for downed gear on the ground now sets "Drop the weapon": moments `downed-and-strip-on`, `downed-and-strip-off`, `all-on-body-downed`, `downed-then-killed-judged-once`, `former-colonist-keeps-gear`; the milestone batch; the showcases `workshop-clips` and `preview-story` (compiled, not filmed).
- Fix found on the way: the ticket 19 defaults step saved settings read straight from a file, which belong to no mod, and threw in `settings.imported-partial-file`. It now saves only the mod's own settings.
- Runs are in TESTING.md (2026-11-24, ticket 21).

## Choices made while building

1. Pack gear is a weapon or apparel when `EraReader.IsGear` says so; everything else in the pack keeps its item rule.
2. A trader's goods keep their not-for-sale mark, and a weapon stowed in a downed trader's pack is marked not for sale while it is there, so it is never offered in trade.
3. "Keep the weapon in his pack" applies whatever "Also when stripped or arrested" says; that setting only decides whether the later strip or arrest sorts.
4. A pawn downed in bed keeps his weapon in hand, as the game does. A later arrest sorts it through `DropEverythingHook`: `Pawn_GuestTracker.SetGuestStatus(Prisoner)` calls `DropAndForbidEverything()` without the keep-in-bed flag, so the hook runs. Read in the 1.7 decompile, not run in game.
5. Pawns the rules do not cover, mechanoids, pawns with no inventory and kinds that cannot be stripped follow "Drop the weapon", so nothing is hidden in a pack nobody can reach.
6. `BodyLoot.WasJudgedAtDowning` stays, used only by "Drop the weapon" with "everything stays on the body", the one path that still judges at the downing.
7. The Auto Mod Config preset `<version>` stays 0.5.2, set by ticket 20 for the same release.
8. A downed raider who gets back up has his weapon in his pack, not on the ground, so the game's `JobGiver_PickupDroppedWeapon` (which looks for `mindState.droppedWeapon`) finds nothing, and he stays unarmed until something equips him. The game has no job that takes a weapon out of a non-colony pawn's pack. Read in the 1.7 decompile, not run in game.
9. Classing pack weapons and apparel by their own rules reaches past downing: a raider's spare weapons and apparel carried in his pack are now judged by the weapon and apparel rules at every death, strip and arrest, not by "Other inventory". It follows from the decision that gear stowed at a downing is judged later as it would have been in his hands; Kyle may want to confirm it.
