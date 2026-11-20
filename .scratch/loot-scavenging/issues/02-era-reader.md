# 02: Era reader

**What to build:** One function returns the colony's current era and one returns an item's era. An item's era comes from the tech level of the research that unlocks it, then the item's own tech level, then the era of the faction that carried it. Two settings replace that last fallback: treat such gear as the colony's own era, or break it straight into components. "Out of reach" means the item's research is unfinished or its era is above the colony's. A dev readout shows the result for a chosen item. See the spec's "Loot rules" and "The Progression pack".

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] The dev readout names the right era and out-of-reach result for a vanilla researched weapon, a vanilla unresearched weapon, an uncraftable weapon and a modded item with no research.
- [ ] The colony's era is read through a single function, so a later pack integration can supply the value.
- [ ] The fallback settings change the result the readout shows.
- [ ] Both the 1.7 and 1.6 builds compile.
