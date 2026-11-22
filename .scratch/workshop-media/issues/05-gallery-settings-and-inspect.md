# 05: Gallery: settings page and inspect text

Status: resolved

**Recommended model:** `sonnet`: two framed screenshots.

**Blocked by:** 01 (the inspect stills). The settings screenshot can come from the existing `lootscavenging-ui` batch.

**What to build:** two 1920x1080 images.

- `gallery-settings.png`: the settings page on Progression, cropped to the preset line and one category rule row with its "what will happen" sentence, under a short title. Mark nothing up that the page itself does not show.
- `gallery-inspect.png`: two scrap stacks side by side, each selected with its inspect pane. One says it can be studied now; the other says why not. Each has a one-line caption.

## Scope

- `tools/workshop_gallery.py settings` and `inspect`.
- Capture the settings page at 2560x1440 so its text survives the downscale. If the UI batch has no full-page still, add one shot to it.

## Acceptance

- [x] The settings page's text reads at 1920x1080. The preset name and one sentence read at 640 px.
- [x] The inspect lines are the game's real text, not redrawn.

## Comments

### 2026-11-22: rendered

- Settings: no English still existed (earlier ui runs were Russian and Chinese, 640 px frames or the default small window), so I filmed one. The ui batch's first case now, under `--record`, sets the UI scale to 1.5 and enlarges the settings window for 4 s (run `20261122-234344-lootscavenging-ui-s1`, logged in TESTING.md). `tools/workshop_gallery.py settings` cuts a 2560x1440 frame into two bands of the same frame (preset line with its sentence; the Item rules heading with the Weapons and Apparel rows and their sentences), stacked with a plain gap and nothing drawn on them.
- Inspect: `tools/workshop_gallery.py inspect` shows the two stills from the ticket 01 run with the game's own inspect pane cropped from the frame (not redrawn). The pane's lower lines are hidden in the stills by the Architect tooltip, so the crop stops above it.
- Checked: viewed at full size and at 640 px; "Active preset: Progression" and the pane lines read at 640, the small sentences are soft there but read at full size.
