# 10: Gallery settings image for 0.6.0

Status: resolved

**Recommended model:** `opus`: film direction and a public image.

**Blocked by:** none.

**What to build:** `docs/images/workshop/gallery-settings.png` shows the 0.6.0 settings page. The current image dates from 2026-11-22. It shows Weapons and Apparel as "Keep" with the text "see drop mode", a setting 0.6.0 removed.

## Scope

- Film the settings page on the Progression preset with the ui batch under `--record --show-ui --frames N` (TESTING.md, `lootscavenging-ui`). Pick the still that shows the preset line, a Who row or an item rule row with its "what will happen" sentence, and the "Don't drop your stuff" boxes if they fit the crop.
- Render with `tools/workshop_gallery.py settings <frame>` (`settings()` at line 275). Update its crop and title for the new layout. The spec's rules for every image apply (`.scratch/workshop-media/spec.md`, "Rules for every image"): RimWordFont lowercased, numbers read from the game, no triad taglines.
- Open the result at reduced size and check the text is legible and matches the game.

## Acceptance

- [x] `gallery-settings.png` shows the 0.6.0 page; no mention of drop mode.
- [x] Spec item 4 updated if the image now shows different parts of the page.
- [x] Ticket records the run id and the command used.

## Comments

### 2026-11-25: filmed and rendered

- Run `20261125-174001-lootscavenging-ui-s1`, 6 of 6 passed: `python tools/ls_test.py run lootscavenging-ui --seed 1 --record --show-ui --frames 40 --quit-on-finish` from the `ticket-10` worktree after `deploy.ps1` and the DevTests build, display nudged awake first. RimWorld 1.7, default mod set, English.
- The frame is the video at 5.5 s (inside the case's 4 s hold at UI scale 1.5), cut with FFmpeg to `work/gallery/settings-frame.png`; other candidates are in `work/gallery/ui-0.6.0/`.
- Rendered with `python tools/workshop_gallery.py settings work/gallery/settings-frame.png`. Two bands: the preset line with its sentence and the three "Don't drop your stuff" boxes with theirs, then the Who heading with the Hostile and Non-hostile rows and their sentences. The page is taller now, so the script scales the bands to fit the height (about 0.98, against 1.52 before). Title unchanged; no drop mode anywhere.
- Checked at 960 and 640 px wide: at 960 every line reads; at 640 the headings, row labels, box labels and buttons read, the small sentences do not. The text is the game's own, from the frame.
