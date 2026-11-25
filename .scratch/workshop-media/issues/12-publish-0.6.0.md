# 12: Push and publish 0.6.0

Status: ready-for-human

**Recommended model:** `none`: pushing to GitHub and uploading to the Workshop each need Kyle's approval at the time.

**Blocked by:** 09, 10, 11

**What to build:** 0.6.0 is on the Workshop with a description, change note and gallery that match it.

## Scope

- Push `main`. The description's GIFs hot-link `raw.githubusercontent.com/KyleMHB/Loot-Scavenging/main/docs/images/workshop/`. `main` was 99 commits ahead of `origin/main` on 2026-11-25, and `loot-scavenging-raid.gif` and `loot-scavenging-study.gif` differ from the pushed copies, so the page shows the old clips until the push.
- If 09 to 11 changed nothing in `Source/`, the release check at `d0ba2f9` still holds. If they did, rerun the TESTING.md release tier first.
- Upload with the `steam-workshop-utils` skill: the mod, `workshop-changenote.md` as the change note, `steam-description.md` as the description, and the gallery images from 10 and 11.
- Post the Discord update from `work/posts/2026-11-25-0.6.0-update/post.md`.

## Acceptance

- [ ] `main` pushed with Kyle's yes.
- [ ] Workshop item shows 0.6.0, the new change note, description and gallery.
- [ ] GIFs on the live page show the 0.6.0 footage.
