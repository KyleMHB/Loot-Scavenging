# 16: Exempt medicine from the drop mode, with an option to include it

**What to build:** Out-of-reach medicine no longer becomes scrap by default. The drop-mode step skips the medicine category, so medicine follows its own category rule (Keep in every preset). A new setting, "Drop mode includes medicine", off by default, puts medicine back under the drop mode like any other item. See the spec's "Loot rules" paragraph on medicine and the presets table row "Drop mode includes medicine".

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

**Recommended model:** `sonnet`: a small, contained change to one evaluator step plus one setting.

## Scope

- `Source/Model/LootEvaluator.cs`, `ApplyDropMode`: when the setting is off and `item.Category == ItemCategory.Medicine`, return null so the category rule decides. This covers "Scrap out-of-reach gear" and "Scrap everything". "Keep the original item" and "Off" are unchanged for medicine in effect, since medicine is not gear.
- Medicine's own category rule, conditions and chance still apply. A player can still scrap or delete medicine through that rule.
- `Source/Model/SettingValues.cs`: a new `bool` field for the setting, default false, with its saved key through `SettingKeys` and a lock name in `SettingNames`. Existing configs load unchanged and take the default.
- `Source/Model/Presets.cs` and `Defs/SettingsDefaults.xml`: off in Progression, Cleanup and Vanilla. The test that the shipped XML equals the compiled Progression preset must still pass.
- `Source/SettingsUI/SettingsPage.cs`: a row in the Scrap and study section, next to the drop mode, with its "what will happen" sentence. Hidden or disabled when the drop mode is Off, matching how the page treats other drop-mode-only rows. Lockable like the other rows.
- `Languages/English/Keyed/LootScavenging.xml`: the label and sentence keys.
- Tests: evaluator cases for medicine out of reach with the setting off (falls to the category rule) and on (scrapped), in both scrap modes; non-medicine inventory unchanged; preset rows assert the new value.

Out of scope: exempting other inventory groups (drugs, food), and any change to how medicine is classified (`ItemClassifier` uses `def.IsMedicine`).

## Acceptance

- [ ] With default settings at a tribal colony, a raider's industrial medicine drops untouched while his out-of-reach gun becomes scrap. (maths unit-tested; in-game: the smoke batch can add this to its tribal-colony case)
- [ ] With "Drop mode includes medicine" on, the same medicine becomes scrap of its era.
- [ ] "Scrap everything" leaves medicine to its category rule unless the setting is on.
- [ ] The setting is off in all three presets, saved under its own key, lockable, and shown on the settings page with a sentence saying what will happen.
- [ ] Both the 1.7 and 1.6 builds compile, the unit tests pass and the translation check passes.

## Comments
