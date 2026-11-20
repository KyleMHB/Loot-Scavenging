# 11: Settings page and presets

**What to build:** The mod settings page in six sections: Presets, Who and where, Item rules, Scrap and study, Corpses, Lists (lists without the picker). Each row has a sentence under it saying what will happen, and the outcome sliders sit beside their outcome and hide when unused. The Vanilla, Cleanup and Progression presets and Reset set every setting at once, and the page shows the active preset and whether it has been changed. Progression is the default, with a one-time first-load message saying what the mod does and where the presets are. Settings are global, read once and cached. All player-facing text goes through translation keys. See the spec's "Settings page" section and presets table.

**Blocked by:** 01, 02, 03, 04, 05, 06, 07, 08, 09, 10

**Status:** ready-for-agent

**Recommended model:** `opus` — The largest ticket: it draws on every earlier rule, and the presets must match the table exactly.

- [ ] Each preset produces the behaviour the presets table states, checked in game for each one.
- [ ] Changing any setting after picking a preset marks the preset as changed.
- [ ] The first-load message appears once and not again after a reload.
- [ ] The translation check passes with no missing keys.
- [ ] Both the 1.7 and 1.6 builds compile.
