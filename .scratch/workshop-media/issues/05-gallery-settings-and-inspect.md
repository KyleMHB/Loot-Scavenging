# 05: Gallery: settings page and inspect text

Status: ready-for-agent

**Recommended model:** `sonnet`: two framed screenshots.

**Blocked by:** 01 (the inspect stills). The settings screenshot can come from the existing `lootscavenging-ui` batch.

**What to build:** two 1920x1080 images.

- `gallery-settings.png`: the settings page on Progression, cropped to the preset line and one category rule row with its "what will happen" sentence, under a short title. Mark nothing up that the page itself does not show.
- `gallery-inspect.png`: two scrap stacks side by side, each selected with its inspect pane. One says it can be studied now; the other says why not. Each has a one-line caption.

## Scope

- `tools/workshop_gallery.py settings` and `inspect`.
- Capture the settings page at 2560x1440 so its text survives the downscale. If the UI batch has no full-page still, add one shot to it.

## Acceptance

- [ ] The settings page's text reads at 1920x1080. The preset name and one sentence read at 640 px.
- [ ] The inspect lines are the game's real text, not redrawn.

## Comments
