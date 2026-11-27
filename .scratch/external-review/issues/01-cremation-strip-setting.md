# 01: Setting: strip corpses when cremating

Status: ready-for-agent

**Recommended model:** `opus`: a new saved setting must load cleanly from saves and settings files without it, and its lock and presets must follow the existing pattern.

**Blocked by:** None (can start immediately)

**What to build:** Kept gear is destroyed with its corpse (decision 5). A new toggle, off by default, has a cremation bill strip the corpse before it burns, so the kept gear drops and the item rules judge it as they do at a butcher strip. Spec, finding 1 and decision 5.

The first build of this ticket dropped a destroyed corpse's inventory beside it. Kyle reversed that on 2026-11-27 and it was reverted.

## Scope

- A bool setting, default off, saved like the other settings, with its Def node and lock in `Defs/SettingsDefaults.xml` and its value in each preset (off in every preset unless a preset's purpose clearly says otherwise).
- When on, a cremation bill (`autoStripCorpses` false on a recipe that takes corpses) strips the corpse before it is consumed, the way the game strips for butchering. Find the game's strip point for `autoStripCorpses` in the decompile and match it.
- A row on the settings page beside the "Don't drop your stuff" boxes, with its explanation, in English, ChineseSimplified and Russian.
- The keep-gear text says kept gear is lost if the body is destroyed, and that cremation strips first only with the new setting on. Keep it short.

## Done when

- With the setting off, cremating a corpse with kept gear burns the gear, as in 0.6.0.
- With it on, the kept gear lands by the crematorium and the item rules have judged it.
- A settings file without the new value loads with it off.
