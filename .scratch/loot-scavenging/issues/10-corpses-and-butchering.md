# 10: Corpses and butchering

**What to build:** A corpse rule with its own chance and separate humanlike, animal and mechanoid toggles. The corpse's gear goes through the loot rules first, and the owner and place checks apply to corpses too. A separate option removes worn apparel when a corpse is butchered. See the spec's "Corpses and butchering" section.

**Blocked by:** 01

**Status:** ready-for-agent

**Recommended model:** `sonnet` — A narrow slice that follows patterns set by earlier tickets.

- [ ] With humanlike removal on, a dead raider's corpse is removed after its gear has gone through the loot rules.
- [ ] A wild animal's corpse stays unless the "no faction" group is on.
- [ ] A pawn that dies without leaving a corpse, or is killed off-map, is skipped with no error.
- [ ] With the butchering option on, butchering a corpse leaves no worn apparel behind, while gear dropped at death is unaffected.
- [ ] Both the 1.7 and 1.6 builds compile.
