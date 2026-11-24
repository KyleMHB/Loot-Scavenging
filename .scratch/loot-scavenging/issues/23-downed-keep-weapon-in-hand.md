# 23: A downed pawn keeps his weapon in hand

Status: done

**Recommended model:** `opus`. It changes what the core downed hook does with a pawn's gear, and a mistake loses loot, judges it twice or leaves a raider unarmed when he should not be.

**Blocked by:** None. Ships in 0.5.2, on top of ticket 21, which it revises.

**Revised by ticket 24 (2026-11-24):** under "Drop the weapon" a downing always drops and sorts the weapon and pack, also with everything on the body, so choices 2 and 4 and `BodyLoot.WasJudgedAtDowning` no longer apply; colonists keep their gear through the same spawn block; "Also when stripped or arrested" is now two toggles.

**Source:** Kyle, 2026-11-24.

## Decision (Kyle)

A downed non-colony pawn keeps his weapon in his hand. Ticket 21 put it whole into his pack; that left a raider who got back up unarmed, and it showed the weapon under Inventory instead of Equipment.

Later the same day (Kyle, 2026-11-24): under "Keep his weapon" he also keeps it when he is spawned again, whether carried and set down, tucked into a bed after a rescue, or loaded. The game's drop at that spawn is blocked, and the sorting that served only that case is gone.

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
- `SpawnDropHook` on `Pawn_EquipmentTracker.Notify_PawnSpawned`: under "Keep his weapon" (`BodyLoot.KeepsWeaponAtDowning`) the game's drop is skipped, so he holds his weapon wherever he is set down and gets up with it. Under "Drop the weapon", only "everything stays on the body" leaves a weapon in hand; for that pawn (`BodyLoot.KeptGearWhenDowned`) the drop goes ahead while the game is playing, is sorted by `DropEquipmentHook` with the downed moment, and the postfix sets `mindState.droppedWeapon` if the weapon landed whole. On a reload or map load (not `ProgramState.Playing`) the drop is skipped.
- `DownedGears.KeepsWeapon` and `KeepsGear` in `Source/Model`, unit-tested (`dotnet test` 358).
- Moments: `downed-keeps-weapon-in-hand` replaces `downed-keeps-weapon-in-pack`. Its ways are strip, capture, kill, strip with stripping off and gets up (anaesthetic removed, 60 ticks). Real game paths, with a colonist taking ordered jobs: `downed-carried-keeps-weapon` (carried while drafted, set down by the drop button and by undrafting, then gets up armed), `downed-rescued-keeps-weapon` (a friendly visitor rescued into a medical bed), `downed-in-bed-captured-weapon-sorted` (that visitor captured from his bed) and `downed-captured-weapon-sorted` (a pirate captured from the ground). Saves: `downed-reload-keeps-weapon`. With `--mods compat-hospitality`: `hospitality-downed-guest`. `drop-calls-judged-once` gained downing then death with everything on the body under both values. `all-on-body-downed` and `downed-then-killed-judged-once` expect the club in hand. The settings round trip covers `KeepWeapon`. The Workshop clip checks the weapon is in hand.
- Refilmed: Workshop clips `20261124-161503-lootscavenging-workshop-clips-s1`, `docs/images/workshop/loot-scavenging-downed.gif`, and the promo's downed segment, captioned "a downed raider keeps his weapon".
- Runs are in TESTING.md (2026-11-24, ticket 23).

## Choices made while building

1. **The spawn drop (build item 2, revised by Kyle's later decision).** Under "Keep his weapon" the game's drop is blocked for every pawn the setting covers, so being carried, rescued or loaded never moves his weapon. Under "Drop the weapon" the game's drop goes ahead. Only "everything stays on the body" leaves him a weapon to drop; that drop is sorted as the downed moment, so it lands whole if the downing judged it, and the mod sets `mindState.droppedWeapon` so a raider who gets up picks it up again (vanilla's spawn drop does not). On a load the drop is skipped, because a save and load does not move his gear. Anyone else's held weapon is the game's business, as before.
2. **"Drop the weapon" with everything on the body (build item 4).** The weapon stays in hand, as it did before ticket 21's stow. It is judged at the downing, and with "Also when stripped or arrested" on it is marked judged, so a later strip, spawn drop or death leaves it as it is. With that setting off the downing judges nothing and marks nothing, so his death judges it once. Before, with the setting off, gear kept at a downing was marked anyway and death skipped it, so it was never judged at all; that is fixed here.
3. **Rescue and capture.** A rescue drops the carried pawn on the bed's cell before he lies down (`RestUtility.TuckIntoBed`), so the spawn drop used to run there and scrap a rescued visitor's out-of-reach weapon under Progression. With the drop blocked he lies in the bed still holding it, and nothing is scrapped (moments `downed-rescued-keeps-weapon`). A capture is an arrest: `CapturedBy` runs `DropAndForbidEverything` while he is still carried, and `DropEverythingHook` sorts his weapon there, whether he was taken from the ground or from a medical bed (moments `downed-captured-weapon-sorted` and `downed-in-bed-captured-weapon-sorted`, 3 industrial scrap from a revolver).
4. **Hospitality.** Its `disableFriendlyGearDrops` (default on) wraps the same game calls the mod patches. When it stops the drop, the mod's hooks see nothing drop and do nothing. Under "Keep his weapon" a downed guest keeps his revolver through a rescue into a bed, with no errors; under "Drop the weapon" the mod's downing prefix still judges it (scrapped at the downing in the run), as before this ticket (moments `hospitality-downed-guest`, `--mods compat-hospitality`).
5. **Old settings.** A settings file saved with `KeepInPack` may log a parse error for that one key and take the default, which is the same choice in its new form. No migration, as asked.
6. **Vanilla safety.** A downed pawn holding a weapon outside a bed already happens in the game (a pawn downed in a caravan, and with Hospitality's defaults), and the pawn renderer has no downed checks. A carried pawn is carried again with his weapon in hand, and he gets up holding it (moments `downed-carried-keeps-weapon`, saves `downed-reload-keeps-weapon`).
7. **Not run in game:** a downed pawn with his weapon in a caravan or shuttle (read in the decompile only), 1.6 (compile only).
