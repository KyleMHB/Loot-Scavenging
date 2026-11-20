# 13: Pack preset and setting locks

**What to build:** The default settings ship as XML that a pack can patch, with an option to lock chosen settings so the page shows them but the player cannot change them. The Progression pack gets its own XML preset: the Progression defaults, with era-typical yields for instant disassembly. See the spec's "The Progression pack" section.

**Blocked by:** 11

**Status:** ready-for-agent

**Recommended model:** `sonnet` — A narrow slice that follows patterns set by earlier tickets.

- [ ] A test patch changes a default, and the page shows the patched value on a fresh config.
- [ ] A locked setting cannot be changed on the page, and the page says why.
- [ ] The pack preset sets disassembly to era-typical yields.
- [ ] Both the 1.7 and 1.6 builds compile.
