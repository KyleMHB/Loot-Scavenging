# 12: Item picker for the lists

**What to build:** A searchable item menu on the settings page that adds items to the keep and remove lists and removes them again. Entries are stored by item name, so a removed mod does not break the settings. See the spec's "Settings page" section.

**Blocked by:** 09, 11

**Status:** done

**Recommended model:** `sonnet` — A narrow slice that follows patterns set by earlier tickets.

- [ ] Searching finds items by label, and picking one adds it to the chosen list.
- [x] An item added through the picker is kept or removed in game as the list says.
- [x] After removing the mod that added a listed item, the settings load without an error and the entry is kept for if the mod returns.
- [x] Picker text goes through translation keys and the translation check passes.
- [x] Both the 1.7 and 1.6 builds compile.

## Comments

### 2026-11-21: implemented on ls/12

- `Source/Model/ItemSearch.cs`: label/defName search (every typed word must match, case-insensitive; label-start matches first, then alphabetical). 5 unit tests in `ItemSearchTests`.
- `Source/SettingsUI/Dialog_ItemPicker.cs`: window with search box, count line, virtualised scroll list (first 200 matches shown), Add button per row or "On the list" if already present. Stays open so several items can be added. Item pool: `ThingCategory.Item` defs with a label, not corpses, and haulable, weapon or apparel; built once per session.
- `SettingsPage.cs`: `DrawAddByName` replaced by an "Add item..." button opening the picker for that list; the unused text-entry fields are gone. Existing entry rows, Remove buttons and "(not loaded)" display are untouched, so entries of missing defs stay stored.
- Keys added: `ListAddItem`, `PickerCount`, `PickerFirstShown`, `PickerOnList`; `ListAddExplain` reworded.
- Tests: `dotnet test` 227 passed; both builds clean, `translations: passed`.
- In-game pending: picker opens and searches by label; picked item is kept/removed per list; removing a mod leaves a "(not loaded)" entry and no error on load.

### 2026-11-21: closed

Merged on `loot-scavenging`. 270 unit tests, both builds and the translation check pass. Smoke run `20261121-094209-lootscavenging-smoke-s1-cd8ccd5d` (RimWorld 1.7, Harmony + Core + Loot Scavenging, 0 mod errors) observed: nothing. Still unobserved in game: the picker search and add, list effect in game, removed-mod entries.

### 2026-11-21: in-game coverage

Observed (settings): the picker's item list (by reflection) has silver and no buildings, and searching "revolver" finds Gun_Revolver; list entries act in game; a missing-def entry survives a settings save and load with no error. Still unobserved: picking an entry in the dialog. Runs (RimWorld 1.7, seed 1): default set `20261121-110125`…`110245` and `20261121-105946`/`105948`/`105952`; `dlcs` set `20261121-110142`…`110301`; full ids in TESTING.md.
