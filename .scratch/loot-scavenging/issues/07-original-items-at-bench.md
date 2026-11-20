# 07: Original items at the bench

**What to build:** An unfamiliar item counts as its scrap equivalent at the bench: the same era, amount, recycling yields and study points. It is studied or recycled only when the player marks it or a bill filter allows it, so nothing the player wants to keep is used up. See the spec's "Original-item mode" paragraph and the requirement "Never consume what the player wants to keep".

**Blocked by:** 04, 05, 06

**Status:** ready-for-agent

**Recommended model:** `sonnet` — A narrow slice that follows patterns set by earlier tickets.

- [ ] A looted rifle recycles for the same yield as the scrap it would have become.
- [ ] Studying it pays the same points as that scrap.
- [ ] An unfamiliar item that is neither marked nor allowed by a filter is never picked up for recycling or study.
- [ ] Both the 1.7 and 1.6 builds compile.
