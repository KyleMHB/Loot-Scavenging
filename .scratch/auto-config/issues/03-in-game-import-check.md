# 03: Check a real Auto Mod Config import on 1.7

Status: ready-for-human

**Recommended model:** `none` - This needs Auto Mod Config installed and Kyle at the settings page. No DevTests mod set includes it, and adding one would be new test infrastructure.

**Blocked by:** 02

**What to build:** Confirm in the real game that an Auto Mod Config preset sets Loot Scavenging's settings, keeps the player's edits, and merges on a version change. See the [spec](../spec.md), "Testing Decisions" and the Auto Mod Config check under "Open Questions".

- [ ] On RimWorld 1.7, Auto Mod Config loads with Loot Scavenging and the example preset and imports with no error. This answers whether Auto Mod Config works on 1.7.
- [ ] First launch: the Loot Scavenging page shows the preset's values.
- [ ] Change one unlocked setting and relaunch: the change is kept.
- [ ] Add a curator key whose value equals the default, then bump the preset's `version` and relaunch. Auto Mod Config merges, the player's edit survives, and the default-valued key behaves sensibly. Write down what happens if Loot Scavenging's own rewrite dropped it.
- [ ] With the Progression pack's locks copied into a test mod's `Patches/`, a locked key set in the preset shows locked at the pack's value.
- [ ] Remove the preset mod and Auto Mod Config, then relaunch: the settings keep their values.

## Checks

- Record the observations and the game build in TESTING.md, and in GAME.md's Auto Mod Config section with the date.
- If 1.7 fails, ask an agent to change the README and Steam text so they don't claim support, before release.

## Comments
