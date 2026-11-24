# 23: A downed pawn keeps his weapon in hand

Status: done

**Recommended model:** `opus`. It changes what the core downed hook does with a pawn's gear, and a mistake loses loot, judges it twice or leaves a raider unarmed when he should not be.

**Blocked by:** None. Ships in 0.5.2, on top of ticket 21, which it revises.

**Source:** Kyle, 2026-11-24.

## Decision (Kyle)

A downed non-colony pawn keeps his weapon in his hand. Ticket 21 put it whole into his pack; that left a raider who got back up unarmed, and it showed the weapon under Inventory instead of Equipment.

## Build

1. Rename `DownedGear.KeepInPack` to `KeepWeapon`, with no migration. Under it nothing drops and nothing is judged at the downing: stop only `MakeDowned`'s drop and remove the stow code. "Drop the weapon" stays as it is.
2. The game drops the weapon a downed pawn holds when he is spawned again outside a bed. Sort that drop, and decide the gating.
3. Strip, arrest, death and joining still sort. Arresting a pawn who still holds his weapon must sort it.
4. "Drop the weapon" with "everything stays on the body": decide whether the weapon stays in hand, and keep "judged once" true in every combination.
5. Do not fight Hospitality's `disableFriendlyGearDrops`.
6. Keep ticket 21's behaviour for joiners' packs, pack gear rules and the not-for-sale mark.
7. Labels "Keep his weapon" and "Drop the weapon"; update the explain text, ChineseSimplified and Russian, the presets, `SettingsDefaults.xml` and both Auto Mod Config files.

## Tests

Rework the moments keep-weapon case, keep `drop-calls-judged-once` meaningful, and round-trip the new value in the settings batch. Refilm the Workshop downed beat and its GIF, and the promo's downed segment.

## What was built

- `DownedGear.KeepWeapon` ("Keep his weapon", default in every preset), in `SettingsDefaults.xml`, `Presets.cs`, the settings page and both Auto Mod Config files. ChineseSimplified 保留他的武器 and Russian «Оставить ему оружие», with the explain texts; `rwtranslate status` 0 untranslated and 0 outdated for both.
- `PawnDownedHook`: when `BodyLoot.KeepsWeaponAtDowning` holds (a covered pawn on a map who can hold loot), it only blocks the game's drop for the length of `MakeDowned`. `KeepEquipmentAtDowningHook` (`DropAllEquipment`) and `KeepInventoryAtDowningHook` (`DropAllNearPawn`) skip while the pawn is marked as keeping his gear. Under "Drop the weapon" with everything on the body, the downing sorts as before, the kept gear is marked judged, and the drops are blocked the same way.
- The stow code is gone: `BodyLoot.Stow`, `StowEquipment`, the stowing set and the not-for-sale mark on stowed weapons. A trader's own goods keep their mark, as the game sets it.
- `SpawnDropHook` on `Pawn_EquipmentTracker.Notify_PawnSpawned`: for a downed pawn outside a bed holding a weapon, whose gear the mod kept at his downing (`BodyLoot.KeptGearWhenDowned`), the game's drop goes ahead while the game is playing and is sorted by `DropEquipmentHook` with the downed moment; the postfix then sets `mindState.droppedWeapon` if the weapon landed whole. On a reload or map load (not `ProgramState.Playing`) the drop is skipped, so he keeps holding it.
- `DownedGears.KeepsWeapon` and `KeepsGear` in `Source/Model`, unit-tested (`dotnet test` 358).
- Moments: `downed-keeps-weapon-in-hand` replaces `downed-keeps-weapon-in-pack`. Its ways are strip, capture, kill, strip with stripping off, carried and put down (`DeSpawn` then `GenSpawn.Spawn`) and gets up (anaesthetic removed, 60 ticks). `drop-calls-judged-once` gained downing then death with everything on the body under both values. `all-on-body-downed` and `downed-then-killed-judged-once` expect the club in hand. The settings round trip covers `KeepWeapon`. The Workshop clip checks the weapon is in hand.
- Refilmed: Workshop clips `20261124-161503-lootscavenging-workshop-clips-s1`, `docs/images/workshop/loot-scavenging-downed.gif`, and the promo's downed segment, captioned "a downed raider keeps his weapon".
- Runs are in TESTING.md (2026-11-24, ticket 23).

## Choices made while building

1. **Gating the spawn drop (build item 2).** The drop is sorted only for a pawn whose gear the mod kept at his downing, under "Keep his weapon" or under everything on the body; anyone else's held weapon is the game's business, as before. It is sorted as the downed moment, so "Also when stripped or arrested" decides whether it is judged, as it does for any other drop from a living pawn. Vanilla's spawn drop does not set `mindState.droppedWeapon`; the mod sets it when the weapon lands whole, so a raider who gets up can pick it up again. The drop is skipped outside `ProgramState.Playing`, because a reload is not the pawn being moved, and judging his weapon then would turn a save and load into a sort.
2. **"Drop the weapon" with everything on the body (build item 4).** The weapon stays in hand, as it did before ticket 21's stow. It is judged at the downing, and with "Also when stripped or arrested" on it is marked judged, so a later strip, spawn drop or death leaves it as it is. With that setting off the downing judges nothing and marks nothing, so his death judges it once. Before, with the setting off, gear kept at a downing was marked anyway and death skipped it, so it was never judged at all; that is fixed here.
3. **Capture.** Carrying a downed pawn to a prison bed runs `DropAndForbidEverything` while he is carried; `DropEverythingHook` sorts it, so an arrest of a pawn holding his weapon sorts it (moments `capture` way). `TuckIntoBed` drops (spawns) him first, but the arrest's drop runs on a carried pawn. A rescue is different: `RestUtility.TuckIntoBed` drops the carried pawn on the bed's cell before he lies down, so the spawn drop runs with him not yet in bed, and his kept weapon is sorted there. Under Progression (non-hostile group and stripped-or-arrested on) a rescued visitor or trader's guard loses an out-of-reach weapon to scrap at the bed. Ticket 21's stow avoided that, and ticket 18 raised the same worry for allies; Kyle may want the spawn drop sorted only for hostile pawns, or left to fall whole.
4. **Hospitality.** Its `disableFriendlyGearDrops` wraps the same game calls the mod patches. When it stops the drop, the mod's hooks see nothing drop and do nothing; under "Drop the weapon" guests are still judged by the downed prefix, as before this ticket. Read in its patch targets, not run in game.
5. **Old settings.** A settings file saved with `KeepInPack` may log a parse error for that one key and take the default, which is the same choice in its new form. No migration, as asked.
6. **Not run in game:** a real carry (staged with `DeSpawn` and `GenSpawn.Spawn`), a reload of a downed pawn holding his weapon (the skip was read in the decompile), Hospitality, 1.6 (compile only).
