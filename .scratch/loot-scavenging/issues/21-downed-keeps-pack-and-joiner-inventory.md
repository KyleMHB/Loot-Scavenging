# 21: Downed pawns keep their gear in the pack; joiners' packs are covered

Status: ready

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
