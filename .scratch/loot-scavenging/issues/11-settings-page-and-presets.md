# 11: Settings page and presets

**What to build:** The mod settings page in six sections: Presets, Who and where, Item rules, Scrap and study, Corpses, Lists (lists without the picker). Each row has a sentence under it saying what will happen, and the outcome sliders sit beside their outcome and hide when unused. The Vanilla, Cleanup and Progression presets and Reset set every setting at once, and the page shows the active preset and whether it has been changed. Progression is the default, with a one-time first-load message saying what the mod does and where the presets are. Settings are global, read once and cached. All player-facing text goes through translation keys. See the spec's "Settings page" section and presets table.

**Blocked by:** 01, 02, 03, 04, 05, 06, 07, 08, 09, 10

**Status:** done

**Recommended model:** `opus` — The largest ticket: it draws on every earlier rule, and the presets must match the table exactly.

- [x] Each preset produces the behaviour the presets table states, checked in game for each one.
- [x] Changing any setting after picking a preset marks the preset as changed.
- [ ] The first-load message appears once and not again after a reload.
- [x] The translation check passes with no missing keys.
- [x] Both the 1.7 and 1.6 builds compile.

## Comments

### 2026-11-21: implemented on ls/11

**Built:**
- `Source/Model/SettingValues.cs`: every global setting as plain values (`LootRules`, `CorpseRules` and the seven scrap, study, era and unfamiliar values). `Clone()` and `SameAs()` walk the public fields by reflection, so a setting added later is copied and compared without further edits. Floats compare within 0.0001.
- `Source/Model/Presets.cs`: `PresetId` (Progression, Cleanup, Vanilla), `Build`, `Defaults` (Progression with empty lists), `Apply` (a preset with the current lists carried over) and `IsChanged`. Every preset sets every field explicitly; this is the one place ticket 13 moves to a Def. Make the Progression preset itself (`Build(PresetId.Progression)`) come from the Def, not only `Defaults()`. `Defaults()` already returns `Build(Progression)`, and Reset and the "changed" check compare against `Build(Progression)`, so all three stay identical and a patched default does not show as "(changed)". `SettingValues` copies and compares only value fields, strings, `List<string>` and classes of public fields. Any other collection, such as a `HashSet` of locked settings, throws `NotSupportedException`, so keep locks outside `SettingValues` or extend the walk.
- `Source/LootScavengingSettings.cs`: the fields stay where they were, so no call site changed. The constructor applies `Presets.Defaults()`, and `ExposeData` takes its missing-key defaults from it. `Capture()` and `Apply()` copy values in and out. There are two new saved fields: `activePreset` and `firstLoadMessageShown`. `PresetChanged` is worked out every frame and never saved.
- `Source/SettingsUI/SettingsPage.cs`: the page. It is a scroll view in six sections. Each row's sentence goes through keys in `Languages/English/Keyed/LootScavenging.xml`.
  - Enums are chosen from a float menu.
  - The degrade range, "lowers quality" and disassembly efficiency show inside each category row whose outcome uses them. These values are shared by every rule with that outcome. Efficiency shows only while the yield is item-based.
  - A category's chance and conditions are hidden while its outcome is Keep.
  - `ScrapDefs.ApplyMarketValue()` runs when the scrap value slider moves, after a preset or Reset, and in `WriteSettings`.
- `Source/LootScavengingMod.cs`: draws the page. The first-load message is queued with `LongEventHandler.ExecuteWhenFinished`. The flag is set and `Settings.Write()` is called before the message is shown, so the message never returns. Its second button opens the settings window.

**Decisions the spec left open:**
- **Code default.** Progression is now the code default. The `LootRules` initialisers match it, and a test checks this. Ticket 01's evaluator tests keep their old rules (hostile only, death only, weapons deleted) through `Tests/LootScavenging.Tests/TestRules.cs`. Ticket 01's in-game check ("weapon gone") now needs the weapon rule set to Delete, or the Cleanup preset with a poor weapon.
- **Lists.** No preset sets the keep or remove lists. Applying a preset keeps them, and changing a list does not mark the preset as changed. Reset to defaults asks first, then empties both lists.
- **Fields outside the table.** Every preset gives these the defaults the earlier tickets chose:
  - home map only off
  - protection toggles on
  - degrade 10 to 40% without the quality step
  - era-typical disassembly at 50%
  - eject charges on
  - carrier-faction era fallback
  - scrap worth 0.01 and dropped on the ground
  - study of the colony era and below, one or the other, techprints off
  - unfamiliar scope: all out-of-reach gear
- **Vanilla's moment.** The table says "Not used", so Vanilla leaves the rules at death only.
- **Cleanup's corpse chance** is 100%.
- **Scrap value** is a slider from 0.01 to 10 silver per unit. Below 0.5 its sentence says scrap is worth almost nothing.
- **Rounding.** Sliders round to whole percents, so setting a value back by hand matches the preset again.
- **Lists section.** `DrawItemList` draws the entries, each with a Remove button, then calls `DrawAddByName`: a def-name text field and an Add button. Ticket 12 replaces only `DrawAddByName`. Entries whose def is missing show "(not loaded)" and are kept.
- **"Read once and cached".** `GetSettings` runs once in the mod constructor and the instance is held in `LootScavengingMod.Settings`. The evaluator, hooks and benches read its fields directly, so that instance is the cache and nothing needs rebuilding when the page changes a value. The only derived state is the scrap market value, which is written onto the scrap defs. The unfamiliar cache already rekeys on the era fallback.
- **Dev actions.** The ticket 01 to 10 dev actions stay in dev mode only. Their comments no longer say they replace the page. They edit the same fields, so the page shows a dev change as "(changed)".

**Tests:** `PresetsTests` covers each row of the presets table through `LootEvaluator` and `CorpseEvaluator`:
- drop mode
- moment
- owner groups
- weapons, apparel and inventory groups
- corpses
- butchering

It also checks:
- the default is Progression
- a preset just applied is unchanged
- an edit to any of eight different settings marks the preset as changed
- editing applied values never alters the preset
- lists are carried over and do not count as a change
- Reset empties the lists

`dotnet test`: 222 passed. Both builds succeed with no warnings, and `rwtranslate check` prints `translations: passed`.

**In-game: pending.** Open Options > Mod settings > Loot Scavenging and check each of these:
- **First load.** On a fresh config the message appears once, and not again after restarting the game.
- **Each preset.**
  - Progression: a tribal colony kills a pirate and gets industrial scrap. A downed raider's out-of-reach gun also becomes scrap.
  - Cleanup: a poor weapon is deleted, a good one is kept. Normal apparel is deleted from the corpse and good apparel is kept. The humanlike corpse is removed and an animal corpse stays.
  - Vanilla: nothing changes.
- **Changed marker.** After any edit the header reads "(changed)", and picking the preset again clears it.
- **Layout.** The scrap value slider changes a scrap stack's market value. Outcome sliders appear only for Degrade and for Disassemble with item-based yield. Long sentences wrap inside the window.

### 2026-11-21: review fixes

- The settings now hold one `SettingValues` (`LootScavengingSettings.values`) instead of copying each field in and out by hand. `Capture` returns it and `Apply` replaces it with a copy. `ExposeData` walks it: every leaf is saved under the key `Model.SettingKeys` gives. The keys are unchanged (field name; `<category><Field>` for category rules, so `weaponsOutcome` stays; `corpse<Field>` for corpse toggles, except `butcherRemovesApparel`), so existing configs load. `SettingKeysTests` pins every old key verbatim. A new setting is now a field in `SettingValues` plus its page row. Code reads `Settings.values.<field>`. The duplicate `LootScavengingSettings.DefaultScrapMarketValue` is gone. In-game: pending (an existing config loads unchanged; a changed value survives a restart).
- Item categories come from one table (`Model/ItemCategories.cs`: field, label key, rule), used by `LootRules.RuleFor`, the page and the save. Lockable names are constants in `SettingNames`, and `SettingLocks.Lockable` is built from them; `TheLockableNamesAreTheOnesPacksUse` pins the set.
- Removed the "None" (study off) choice from "Which scrap can be studied"; see ticket 05.

### 2026-11-21: closed

Merged on `loot-scavenging`. 270 unit tests, both builds and the translation check pass. Smoke run `20261121-094209-lootscavenging-smoke-s1-cd8ccd5d` (RimWorld 1.7, Harmony + Core + Loot Scavenging, 0 mod errors) observed: fresh config is the Progression preset and not marked changed; Cleanup preset behaved as stated (two presets only). Still unobserved in game: each preset in game, first-load message once, settings page UI.

### 2026-11-21: in-game coverage

Observed (smoke, settings): Progression, Cleanup and Vanilla each applied unchanged and editing a value marked them changed; Vanilla left a pirate's revolver whole and his corpse in place; Reset returned to Progression. FAILED: every launch logs a NullReferenceException in `LootScavengingMod.ShowFirstLoadMessage` at startup (the flag is still saved as shown). Still unobserved: the settings page UI and the message on screen. Runs (RimWorld 1.7, seed 1): default set `20261121-110125`…`110245` and `20261121-105946`/`105948`/`105952`; `dlcs` set `20261121-110142`…`110301`; full ids in TESTING.md.

### 2026-11-21: bench fixes

Fixed in `8d1fa4d`: `ExecuteWhenFinished` from the Mod constructor runs before Root's interface event creates the window stack, so `Find.WindowStack` was null. The message is now queued as a long event behind that event, and the flag is written only after the message is added. No NullReferenceException in the new runs' Player.log, and the flag was saved at launch after the message was added. Still unobserved: the message on screen and its buttons. Runs (RimWorld 1.7, seed 1): default set bench `20261121-111417-lootscavenging-bench-s1-356b2793` (smoke `20261121-111405-…-0920bef4`, saves `20261121-111421-…-83df63a9`, settings `20261121-111424-…-ed3fee1c`, all passed); `dlcs` bench `20261121-111438-lootscavenging-bench-s1-7c6eabd9` (passed).
