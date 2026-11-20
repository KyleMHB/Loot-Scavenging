# 12: Item picker for the lists

**What to build:** A searchable item menu on the settings page that adds items to the keep and remove lists and removes them again. Entries are stored by item name, so a removed mod does not break the settings. See the spec's "Settings page" section.

**Blocked by:** 09, 11

**Status:** ready-for-agent

- [ ] Searching finds items by label, and picking one adds it to the chosen list.
- [ ] An item added through the picker is kept or removed in game as the list says.
- [ ] After removing the mod that added a listed item, the settings load without an error and the entry is kept for if the mod returns.
- [ ] Picker text goes through translation keys and the translation check passes.
- [ ] Both the 1.7 and 1.6 builds compile.
