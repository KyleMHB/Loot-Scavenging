# 14: Compatibility runs

**What to build:** In-game runs with the mods the spec names, to confirm Loot Scavenging works beside them: Progression: Ammunition, Progression: Hospitality, Research Papers, Semi Random Research: P-Fork and Non uno Pinata. This needs those Workshop mods installed and play in game, so it is for a human. See the spec's "Compatibility" and "The Progression pack" sections, and resolve the matching Unverified items.

**Blocked by:** 13

**Status:** ready-for-human

**Recommended model:** `none` — in-game runs by a human.

- [ ] Each run loads and plays through a raid with no errors.
- [ ] ~~With Non uno Pinata, gear that stays on the body still follows the loot rules.~~ Out of scope (Kyle, 2026-11-21): Pinata is not in the Progression pack. The harness run is kept below for anyone who adds it.
- [ ] With Progression: Ammunition, ammo is recognised and follows the ammunition rule.
- [x] With Semi Random Research, study pays only into the project picked from the offer. (`lootscavenging-progression`, 2026-11-21)
- [x] With Research Papers, a project completed by study behaves like bench research, and losing a paper is checked against partial progress. (`lootscavenging-progression`, 2026-11-21)
- [ ] With Progression: Hospitality, guest drops are unaffected unless their owner group is on.
- [ ] The spec's Unverified list is updated with what the runs showed. (Era, benches, Research Papers and Semi Random Research done 2026-11-21; Ammunition and Hospitality remain.)

## Harness runs, 2026-11-21

The batches ran with each mod beside all DLCs (TESTING.md, "compatibility runs"). They tick no box above: they play no raid, spawn no Progression ammo, use no paper items or guests, and pick no project through Semi Random Research's offer. What they showed, for the hand runs to settle:

- **Research Papers:** all batches passed. Still to play: a project finished by study, and losing a paper.
- **Semi Random Research:** study paid 0 points in every study case. The cases set the project directly, which this mod may override. In play: pick a project from its offer, then check that a study bill pays into it.
- **Progression: Ammunition and Hospitality:** batches passed except moments, which could not find clear ground on the map. Still to play: ammo drops and guest drops.
- **Non uno Pinata:** at death and when downed nothing reached the ground, including a colonist's revolver. In play: kill a raider with out-of-reach gear and open the corpse's gear tab. Scrap there means the rules ran; the original item there means they did not. Pinata declares RimWorld 1.6 at most.

## Progress 2026-11-21

The automated part runs in `lootscavenging-progression` with `--mods progression`: VFE Tribals, Progression: Core, Node Research, Progression: Production, Research Papers and Semi Random Research. It found that the colony's era lagged the pack's era after each advance, now fixed (spec, "Unverified"). Still for a human: a played raid with the pack, Progression: Ammunition and Progression: Hospitality. Non uno Pinata's code must not be read (AGENTS.md, "Originality").
