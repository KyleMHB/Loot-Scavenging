# 01: Core rules: death, downed and strip hooks

**What to build:** When a non-colony pawn dies, is downed or is stripped, its gear is sorted just before the game drops it. Owner groups (hostile, non-hostile faction, no faction, prisoners), the home-map-only check and the moment setting decide whether the rules run at all. Weapons and apparel each get a category rule with keep or delete. Each decision writes one dev-mode log line. Settings exist as code defaults with no settings page yet. See the spec's "Loot rules" and "Code structure" sections.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

**Recommended model:** `opus` — Every later ticket builds on the hook choice, the call order found in the game code, and the evaluator and applier split.

- [ ] The signatures and call order of the death, downed and strip methods are confirmed against the 1.7 game code before any hook is written, and the result is recorded in the spec's Unverified list.
- [ ] Killing a spawned raider removes its weapon and keeps its apparel, with the default test rules.
- [ ] Killing a wild animal changes nothing while the "no faction" group is off.
- [ ] Colony pawns are never affected.
- [ ] Rules run on downed and on strip only when the moment setting allows it.
- [ ] The home-map-only setting leaves gear alone on caravan, quest and outpost maps.
- [ ] A pawn whose gear cannot be stripped (for example a weapon-carrying mechanoid) is still sorted at death.
- [ ] Both the 1.7 and 1.6 builds compile.
