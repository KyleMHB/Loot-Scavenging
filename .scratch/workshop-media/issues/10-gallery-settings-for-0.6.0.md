# 10: Gallery settings image for 0.6.0

Status: ready-for-agent

**Recommended model:** `opus`: film direction and a public image.

**Blocked by:** none.

**What to build:** `docs/images/workshop/gallery-settings.png` shows the 0.6.0 settings page. The current image dates from 2026-11-22. It shows Weapons and Apparel as "Keep" with the text "see drop mode", a setting 0.6.0 removed.

## Scope

- Film the settings page on the Progression preset with the ui batch under `--record --show-ui --frames N` (TESTING.md, `lootscavenging-ui`). Pick the still that shows the preset line, a Who row or an item rule row with its "what will happen" sentence, and the "Don't drop your stuff" boxes if they fit the crop.
- Render with `tools/workshop_gallery.py settings <frame>` (`settings()` at line 275). Update its crop and title for the new layout. The spec's rules for every image apply (`.scratch/workshop-media/spec.md`, "Rules for every image"): RimWordFont lowercased, numbers read from the game, no triad taglines.
- Open the result at reduced size and check the text is legible and matches the game.

## Acceptance

- [ ] `gallery-settings.png` shows the 0.6.0 page; no mention of drop mode.
- [ ] Spec item 4 updated if the image now shows different parts of the page.
- [ ] Ticket records the run id and the command used.
