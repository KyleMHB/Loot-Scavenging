# 04: Add the Workshop-id settings file to the example

Status: ready-for-human

**Recommended model:** `sonnet` - Copying one file and changing docs, once Kyle supplies the Workshop id after the first upload.

**Blocked by:** 02. The mod's first Workshop upload, which is Kyle's step, also gates it.

**What to build:** Subscribers' installs are probably named after the Workshop id, so the example preset also covers them. GAME.md marks the id-named file as expected but not confirmed. See the [spec](../spec.md), the settings file name check under "Open Questions".

- [x] Kyle uploads the mod and reports the Workshop id. Once an id exists, this ticket becomes `ready-for-agent`.
- [x] Add a second settings file to the example, named with the Workshop id in place of the folder name, with the same content as the local-folder file.
- [ ] Confirm the id-named file is imported for a subscribed copy, by Kyle in game, and record the result in GAME.md with the date. Until then, the README calls it expected, not confirmed.

## Checks

- Both builds and `rwtranslate check`, if any text changed.
- Deploy, and confirm both settings files are in the deployed example.

## Comments

### 2026-11-23: id-named file added

- The mod was published as Workshop item 3844457170. `Extras/Loot-Scavenging-AutoConfig/Settings/Mod_3844457170_LootScavengingMod.xml` is a byte-identical copy of the local-folder file, as ASF Wealth Adjuster already ships.
- README, AGENTS.md and GAME.md name both files; README calls the id-named one expected, not confirmed. The preset `version` is unchanged, since the settings content did not change.
- Left for Kyle: subscribe to the Workshop copy with Auto Mod Config and the example preset, launch, and confirm the id-named file is imported. Record the result in GAME.md.
