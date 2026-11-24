# 24: Colonists keep their gear; loot stays on the body by default; arrest and strip split

Status: ready-for-agent

**Recommended model:** `opus`. It changes saved settings (a split key and a new default) and when gear is judged at the core hooks; a mistake silently changes a player's choice or scraps a colonist's gun.

**Blocked by:** None. Ships in 0.5.2 (unreleased), on top of tickets 21 and 23, merged on main.

**Source:** Kyle, 2026-11-24, in conversation.

## Decisions (Kyle's)

1. **Two separate loot settings stay**, both defaulting to keep:
   - "Where loot lands at death" (`deathDrop`: Ground / ScrapOnBody / AllOnBody). **Default becomes AllOnBody** ("Everything stays on the body") in `SettingsDefaults.xml`, every preset, `packs/progression` and both `Extras/Loot-Scavenging-AutoConfig` settings files.
   - "When a non-colony pawn is downed" (`downedGear`: KeepWeapon / DropWeapon), default KeepWeapon, as now.
   - Both cover only the pawns the rules cover (owner groups, home map), as now.
2. **Under "Drop the weapon" it must drop**, and what drops from a covered pawn is sorted by the drop mode (e.g. scrapped in a scrap mode), as for any other drop. This includes friendly visitors and allies when friendly factions are covered, Hospitality guests included.
3. **New setting "Colonists keep their gear"** (bool, default on), separate from the loot settings:
   - On: a downed colonist or colony slave keeps his weapon in hand, also when carried and set down, rescued into a bed, or loaded (the same block as ticket 23's `SpawnDropHook`). A dead colonist or colony slave keeps his weapon and pack on the corpse, as "Everything stays on the body" does for covered pawns.
   - Off: colonists drop as in the base game.
   - Colony animals and mechanoids are not covered; the game decides.
4. **Colonists' gear is never scrapped or sorted**, downed or dead, under any setting. (Already true: colony pawns are never covered. Keep it true.)
5. **"Also when stripped or arrested" (`onDownedAndStrip`) splits into two toggles**, both default on:
   - "Sort when arrested": the weapon and pack the game takes off a pawn as he is taken prisoner (`DropAndForbidEverything`, ticket 18's `DropEverythingHook`) are sorted. Off: they drop whole. Apparel stays worn, as in the game.
   - "Sort when stripped": what comes off at a strip is sorted. Off: it comes off whole.
   - Migration: a settings file or pack that saved or locked `onDownedAndStrip` false gives both new toggles false; a lock on the old key locks both. Use the ticket 19 settings-file conversion pattern. Keep reading the old key; don't write it any more.

## Build notes

- Wherever the downing under "Drop the weapon" was gated by `onDownedAndStrip`, it is now always sorted by decision 2 (a downing is neither an arrest nor a strip). Check `LootEvaluator.CheckMoment` and the "judged once" paths (`BodyLoot.WasJudgedAtDowning`) still hold in every combination; remove what no path uses any more.
- With `deathDrop` defaulting to AllOnBody, "Drop the weapon" + AllOnBody becomes a likely combination; ticket 23 choice 4 describes it. Keep it correct.
- The colonist setting is its own hook path or a clear branch in the existing hooks: colony pawns never go through `GearSorter`.
- Settings page: the colonist toggle near the downed setting; the two toggles replace "Also when stripped or arrested". All saved (`forceSave`), lockable, in `SettingKeys`, `SettingsDefaults.xml`, presets, both Auto Mod Config files (their content changes, so the preset `<version>` stays 0.5.2, the release it was written for). Explain text for each.
- Text: plain English in Keyed, ChineseSimplified and Russian via the `rimworld-translate` skill and `translation/glossary.md`; `status` ends 0/0.

## Tests

- Unit tests for the new plain rules and the key migration in `Tests/` (old file with `onDownedAndStrip` false → both false; locked → both locked; missing → both true).
- DevTests, real game paths as in ticket 23 (`downed-carried-keeps-weapon` is the model):
  - a downed colonist keeps his weapon in hand, through a carry and a rescue into a bed, and gets up armed; nothing is scrapped;
  - a killed colonist keeps his weapon and pack on the corpse; nothing is scrapped;
  - with "Colonists keep their gear" off, a downed colonist drops his weapon whole (not scrapped);
  - a colonist's out-of-reach weapon is never scrapped under "Drop the weapon" or any death setting;
  - "Sort when arrested" off: an arrested raider's weapon drops whole; on: scrapped;
  - "Sort when stripped" off/on: the same for a strip;
  - "Drop the weapon": a downed visitor's out-of-reach weapon is scrapped as it drops (friendly factions covered);
  - the default `deathDrop` is AllOnBody; existing cases that expect loot on the ground set Ground explicitly;
  - the settings round trip covers the new keys and the migration.
- Run `dotnet test`, both builds, the translation check, and the moments, saves, settings, smoke and outcomes batches. Not the full tier.

## Docs

CHANGELOG 0.5.2 (no version bump; read `E:\Coding\Templates\writing-copy.md`), `spec.md` (moments and settings), `steam-description.md` where it describes downing, death or the strip/arrest setting (it is near the 8,000-character cap), TESTING.md. Rewrite ticket 21/23 notes they contradict with a one-line pointer here.

Filming the raid GIF again under the new defaults is a separate step after this merges.
