# 04: Recycling at existing benches

**What to build:** Scrap is recycled through a bill at a bench the colony already owns. One XML tier table names each tier's bench and its fixed era-typical yields, so a pack can patch both without code. A bench recycles its own tier and every tier below it, with no second check on the colony's era. Bills support the usual controls, including a keep-in-stock count. See the spec's "Recycling" section and its proposed table.

**Blocked by:** 03

**Status:** ready-for-agent

**Recommended model:** `sonnet` — A narrow slice that follows patterns set by earlier tickets.

- [ ] A machining table recycles industrial and medieval scrap.
- [ ] A smithy refuses industrial scrap.
- [ ] Each unit returns the materials the tier table lists.
- [ ] Recycling bills are added only to the named benches, and a bench from another mod still shows all its own bills.
- [ ] Both the 1.7 and 1.6 builds compile.
