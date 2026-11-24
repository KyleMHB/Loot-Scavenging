# 19: Cheaper scrap, with old saves and settings converted once

Status: done

**Recommended model:** `opus`. It changes saved games and saved settings, and a mistake would silently double or halve a player's scrap or research.

**Blocked by:** None. Ships in 0.5.2, the same release that starts saving every setting (ticket 18). Ticket 20 runs in parallel and also touches the settings files.

**Source:** Kyle, 2026-11-24. The scrap cap (scrap is never worth more than the item or stack it came from) deletes an item worth less than one scrap unit. A spacer unit is worth 33 silver, so a lot of cheap gear left nothing. Cheaper units mean more scrap per item and less loss to rounding.

## Decisions (Kyle's)

- Scrap is made cheaper: each unit is worth less, recycles into less and studies for fewer research points, so players get more of it. Bigger stacks and bulk recipes make up for the bulk.
- Scrap already in a save is doubled once when that save first loads, so nobody loses value.
- A player's own research-point values are halved once too, with a note telling them so.
- Component fallback (disassembly giving components for an item worth less than one component): no free scrap. Scrap must never be worth more than the item. Kyle asked whether one scrap of the era could be given; it can't under the cap, so the item goes through the same capped scrap rule.

## Build

1. **Halve each tier's `valuePerUnit`** in `Defs/ScrapTiers.xml`: 6→3, 9→4.5, 11→5.5, 33→16.5, 48→24, 65→32.5. Check every comment and doc that quotes these numbers.
2. **Halve the default research points per unit** in `Defs/SettingsDefaults.xml` and `SettingValues.DefaultStudyPoints`: 10→5, 15→7.5, 15→7.5, 25→12.5, 35→17.5, 50→25. Also halve them in the Progression pack preset under `packs/progression`, in `Presets.cs` where a preset sets them, and in both settings files of `Extras/Loot-Scavenging-AutoConfig`. Set that preset's `<version>` to 0.5.2, since its content changes. The settings page sliders must still allow the new values; check their ranges and step.
3. **Raise the scrap stack limit** (`Defs/ThingDefs/Scrap.xml`) from 500 to 1000, so that a full stack still fits after an old save is doubled.
4. **Recycle work:** keep the work per silver the same. If `ScrapEquivalent.RecycleWork` or the recipe's work is per unit, halve it so a stack takes the same total time per value as before.
5. **Bulk bills:** add a ×10 variant of each recycle bill and each study bill, at every bench that has the ×1 bill. It takes ten units per job and pays ten times as much. Translate its label. A stack under ten units still uses the ×1 bill.
6. **Components fallback** (`DisassembleApplier`): when the item is worth less than one component, give scrap capped by `ScrapAmounts.CapUnits` instead of components. That may be 0, and then nothing drops. The cheaper units make 0 rarer.
7. **Old saves:** add a small `GameComponent` that saves a scrap-scale marker (for example `Scribe_Values.Look(ref scrapScale, "scrapScale", 1)`, with a new game starting at 2). When a loaded game has scale 1, double the `stackCount` of every raw and studied scrap thing once, then set it to 2. Scrap can be in many places, and every one must be covered or none:
   - on maps: ground, storage, shelves, corpses, pawn inventories, carried things, minified or other containers, and transporters (`ThingOwnerUtility.GetAllThingsRecursively` on each map);
   - off the map: caravans, travelling transport pods, and world pawns' inventories.
   A doubled stack over the limit is split or spawned beside it; never lose units. Log one dev-mode line with the count changed.
8. **Old settings:** saved settings files carry a marker in the same way. When a file without it loads, halve each `studyPoints<Tier>` value the player saved (not the multiplier), then set the marker. A value a pack locks is skipped: it comes from the pack, not the player. Values missing from the file already take the new halved default. If any value was halved, the settings page shows a one-line note under the research points until the player next changes those settings. Wording, in plain English: "Scrap was made cheaper in 0.5.2, so your research points per unit were halved to keep the same total." Translate it.

## Tests

- Unit tests (`Tests/`) for the settings halving: an old file with custom values is halved once; one with the marker is not; locked values are left alone; missing values take the new default.
- Update the scrap and recycling unit tests and DevTests fixtures (`ExpectedScrap*`) for the new values.
- A DevTests case in the matching batch: a game at scrap scale 1 with scrap on the ground, in a pawn's pack and in a caravan loads with every stack doubled once, and a second load doubles nothing.
- A DevTests case for a ×10 recycle bill.
- Run `dotnet test`, both builds, and the translation check. Kyle runs the full tier after merge, so don't run it.

## Docs

CHANGELOG 0.5.2 (no version bump), under Changed: scrap is cheaper and comes in larger numbers; existing scrap is doubled once so it keeps its value; custom research values are halved once; ×10 bills. Read `E:\Coding\Templates\writing-copy.md` first. Update `spec.md` wherever it quotes scrap values or study points, and add a TESTING.md line for the new cases.

## What was built

- `valuePerUnit` halved in `Defs/ScrapTiers.xml`; `ScrapAmounts` divides the square root of the value by 4 instead of 8, so an item gives about twice the units. Scrap stacks to 1,000 and weighs half as much per unit.
- Default study points halved in `Defs/SettingsDefaults.xml` and `SettingValues.DefaultStudyPoints` (5, 7.5, 7.5, 12.5, 17.5, 25).
- Recycle work stays the same per silver: each tier's `workPerUnit` is halved (80 to 40 through 220 to 110).
- x10 bills: `LS_Recycle_Bulk_<tier>`, `LS_RecycleStudied_Bulk_<tier>` and `LS_Study_Bulk_<tier>` at every bench with the x1 bill, with translated labels and a description. Ten units per job, ten times the yield or points and work.
- `DisassembleApplier`: gear with no era worth less than one industrial component gives capped scrap of its tier (possibly none) instead of components. `Disassembly.ComponentsFor` takes a quarter of the units instead of half, so dearer gear gives the same components as before.
- `ScrapScaleComponent` (GameComponent, key `scrapScale`): a new game starts at 2. A loaded game at 1 doubles every raw and studied scrap stack once (maps recursively, the world's holders including caravans and travelling pods, and world pawns), places overflow beside the stack or in the same container, logs one dev-mode line and sets 2.
- Settings: a file without `scrapScale` has each saved, unlocked `studyPoints<Tier>` above 0 halved once, is written back at once, and shows the translated note under the research points until the player changes those settings. The Extras preset version is 0.5.2 and both its settings files carry `scrapScale` 2.
- Tests: four settings-halving unit tests; scrap, recycling and disassembly tests updated. DevTests: `saves.old-scrap-doubled`, `saves.old-scrap-doubled-once`, `bench.recycle-bulk-bill`; bench study and smoke values halved; `outcomes.components-fallback` checks the cheap-gear scrap path.

## Choices made while building

- Units use `sqrt(value)/4`; the scrap market value setting is unchanged, since it already sits below every tier's new value.
- The settings note shows only when a value above 0 was actually halved.
- x10 bills start only with at least ten units and take up to ten units from the stacks the game picks. Original-item bills get no x10 variant. Labels use "x10", not "×10", to match the game's own label text.
- The Extras settings files hold no study-point values, so nothing in them was halved; `packs/progression` and `Presets.cs` use `DefaultStudyPoints` and needed no edit.
- `ComponentsFor` went from a half to a quarter of the units, so a components-fallback item gives the same components as before the cheaper units. Not in the ticket; it keeps the player's yield.
- Overflow is placed only after every stack is doubled, so a new stack that merges into a neighbour is never doubled twice. If no spot or container takes it, the stack goes over its limit rather than lose units.
- The scale is set to 2 even if the doubling throws, so a save is never doubled twice; the error is logged.
- The game component is the mod's first data in the game file, so loading a save without the mod logs one more missing-class error. `spec.md` and the Workshop text say so.
- The `outcomes.stack-scrap-by-value` bound "fewer than half a unit per item" became "fewer than one per item": cheaper units make 53 devilstrand give 31, which is correct.
- The Workshop scrap gallery image was not regenerated.
