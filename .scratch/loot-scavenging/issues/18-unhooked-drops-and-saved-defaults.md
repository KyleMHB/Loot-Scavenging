# 18: Gear that leaves a living pawn without the rules seeing it, and settings that follow a changed default

Status: done

**Recommended model:** `opus`. Part 2 changes how settings are saved, and a mistake would silently move players' choices.

**Blocked by:** None. It touches `SettingsLoad` and `LootScavengingSettings`, which ticket 17 also changes, so do it after 17 or on top of it.

**Source:** the review of 2026-11-24 (`work/reviews/2026-11-24-mod-review.md`, §2.2 and §4.2), confirmed against the 1.7 assembly.

Not included: loot on a body that is destroyed with the corpse (burned, eaten, rotted away). Kyle ruled it expected on 2026-11-24, so a corpse destroyed before it is stripped loses what it carries, as in vanilla.

## Part 1: unhooked ways gear leaves a living pawn

The mod sorts gear at death, downing and strip. The game also drops gear in other places, and there out-of-reach gear comes out whole. This breaks the promise in `LootScavenging_DownedAndStripOn`: "cannot be taken intact from a downed raider or a stripped prisoner".

- **Arrest of a standing pawn.** `Pawn_GuestTracker.SetGuestStatus(Prisoner)` calls `Pawn.DropAndForbidEverything()` directly. Arresting a visitor, a trader's guard or a wild man who isn't downed drops his weapon and inventory unsorted.
- **Trade.** `Pawn.PreTraded` drops gear. Minor.
- **Lost manipulation.** `Pawn_HealthTracker` drops the primary weapon with `Pawn_EquipmentTracker.TryDropEquipment` when a pawn can no longer hold it, without going through `MakeDowned`.
- **Recruit or enslave a downed raider.** Apparel isn't sorted at a downing. A captured raider recruited or enslaved becomes a colony pawn still wearing out-of-reach armour, and stripping him then drops it whole, since colony pawns are never sorted.

Suggested build:
- Prefix `Pawn.DropAndForbidEverything` with the same guards as the downed hook, at `Moment.Downed` (or a new moment if the rules need to tell them apart). That covers arrest and trade. Check that downing and death, which call it too, aren't sorted twice: the downed hook already ran, and its things are marked.
- For recruit and enslave: sort worn apparel at the moment the faction changes to the player's (a `Pawn.SetFaction` prefix, non-colony to player, moment Strip). Or, if Kyle prefers, state the loophole in the downed-and-strip text.
- Lost manipulation: a prefix on `Pawn_EquipmentTracker.TryDropEquipment` filtered to non-colony pawns, or accept it and say so here.

## Part 2: a value equal to the default is not saved

`Scribe_Values.Look(ref value, key, defaultValue)` (`Source/LootScavengingSettings.cs` ~118) leaves out a value equal to the current baseline. A missing key is later filled from whatever the baseline is then (`Source/Model/SettingsLoad.cs`). A player's choice that happens to equal the default isn't recorded as a choice. When a pack update, a pack removal or a mod update changes the baseline, the setting moves with it silently.

Example: a player on Cleanup has `corpseAnimal=false` left out. A pack that later defaults it to true turns animal corpse removal on for him, while the page still says Cleanup.

**Decision needed (Kyle):** should the mod's own save write every key (`forceSave: true`), so a player's file never follows a changed default? Or is following the baseline intended, so a pack update reaches players who never touched a setting? Partial files from other tools keep working either way, because the fill step is separate. Ticket 17's settings move reads "file lacks the key" as "take the baseline", so the answer changes that ticket's migration too.

## Decisions (2026-11-24)

Kyle asked for all of C (Part 1 and Part 2) to be built and ruled corpse loss expected. The hook choices below came from the coordinator's brief, not from Kyle.

- Part 1: build all of it. Arrest of a standing pawn and trade: hook `Pawn.DropAndForbidEverything`. Recruit or enslave of a downed raider: sort worn apparel (strip semantics) when a non-colony pawn's faction changes to the player's, before it changes, so he is judged as the group he was. Lost manipulation: prefix `Pawn_EquipmentTracker.TryDropEquipment`, non-colony pawns only. All count as the downed-and-strip moment, so that toggle turns them off.
- Part 2: the mod's own save writes every key (`forceSave: true`), so a player's file never follows a changed default. Partial files from other tools, and files from before 0.5.2, still fill missing keys from the baseline.
- Corpse loss stays expected. The list below stays unscheduled.

## What was built

- `Source/Loot/GearHooks.cs`:
  - `DropEverythingHook` (prefix and finalizer on `Pawn.DropAndForbidEverything`): sorts weapon and inventory at the downed moment. Covers arrest (`SetGuestStatus(Prisoner)`) and trade (`PreTraded`). Skips the carried thing, an in-bed call with the keep flag, and dead or downed pawns.
  - `DropEquipmentHook` (prefix on `TryDropEquipment`): sorts the one weapon; when the outcome removes it, it skips the drop and returns true, so callers such as `MakeRoomFor` see the room made.
  - `RecruitHook` (prefix on `RecruitUtility.Recruit`) and `EnslaveHook` (prefix on `Pawn_GuestTracker.SetGuestStatus`, Slave only): worn apparel of a colony prisoner joining the player's faction, at the strip moment. `Recruit` clears the prisoner status before `SetFaction`, so a `SetFaction` prefix would have judged him as hostile; these two run while he is still a prisoner. Pawns who join through a quest are not prisoners and are not sorted. This prisoner-only scope is the implementer's reading of the decision, not Kyle's ruling: open for Kyle to confirm or widen.
  - `SortedDrop`: no item is judged twice. Kill, MakeDowned, Strip and DropAndForbidEverything mark the pawn from prefix to finalizer, and the two new drop hooks skip a marked pawn. They also skip dead and downed pawns, since Kill and MakeDowned set those states before their own drop.
- `Source/LootScavengingSettings.cs`: `forceSave: true` on every setting key and `activePreset`. `SettingsLoad` is unchanged: a missing key still takes the baseline.

## Other review findings, not scheduled

Recorded so they aren't lost. Each needs Kyle's call before it becomes work.
- **Downed raiders and allies lose an out-of-reach weapon for good** (§3.3). The game would let them pick it up on recovery. One option is to leave the non-hostile group out of the downed moment. Since ticket 21 a downed pawn keeps his weapon whole in his pack by default, so this holds only under "Drop the weapon".
- **Death acidifier pawns and mechanoids** (§3.4). They pay scrap for gear the game destroys, and mechanoid scrap stops once the colony researches the weapon.
- **Quest pawns' untagged gear is sorted** (§2.5). The corpse rule checks the pawn's quest tags; protection checks only the item's.
- **Hunted animals' corpses** (§2.4). With animal corpse removal on, a hunter's kill is removed.
- **First recipe wins in the era index** (§1.5). An item with two recipes, one already researched, can read as out of reach.
- **The colony's era jumps on one cheap project** (§1.5). Growth vats or the shield belt make the colony Spacer.
- **Small settings texts** (§4.2):
  - Reset restores a pack's lists rather than emptying them.
  - The degrade range isn't locked with a locked Degrade rule.
  - The drop mode sits below the rules it overrides.
- **`AGENTS.md` overstates the Auto Mod Config files** (§4.1). It says they "list every saved key and value", but they hold `activePreset` and a comment.

## Acceptance

- [x] Arresting a standing pirate-faction visitor with an out-of-reach revolver at a tribal colony gives scrap, not the revolver.
- [x] A recruited or enslaved downed raider's out-of-reach armour is sorted, or the loophole is stated in the setting text (Kyle's choice). Sorted. Since tickets 20 and 21 a joining prisoner's gear and pack follow the prisoner joiner setting instead.
- [x] The Part 2 decision is recorded in the spec's decision table and implemented; if it is `forceSave`, a test shows a value equal to the default is written and survives a baseline change.
- [x] Harness cases for each hooked path, added to `TESTING.md` with the ticket number. The Full tier before the release that carries it is still to run.

## Comments

- 2026-11-24: built and run in game (TESTING.md, "Unhooked drops and saved defaults"). Lost manipulation is tested through the real path: both shoulders removed (not fresh), the pirate stays standing and `Pawn_HealthTracker` drops the weapon. Enslaving runs only with Ideology (`--mods dlcs`).
