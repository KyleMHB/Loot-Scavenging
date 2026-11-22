# 04: Gallery: drop modes

Status: ready-for-agent

**Recommended model:** `sonnet`: crops ticket 01's stills into panels with the existing framing helpers.

**Blocked by:** 01

**What to build:** `docs/images/workshop/gallery-drop-modes.png`, 1920x1080. It has four panels of the same downed pirate, one per drop mode:

- scrap out-of-reach gear;
- scrap everything;
- keep the original item, marked unfamiliar;
- off.

Each panel carries a settings chip with the mode's in-game label and a one-line caption. The keep-original panel includes the inspect pane's unfamiliar line, legible.

## Scope

- `tools/workshop_gallery.py drop-modes`, using `framed()` from `tools/preview_story.py`. Crop by cells from the batch's layout, as `split()` does.
- Take mode labels from `Languages/English/Keyed/LootScavenging.xml`.

## Acceptance

- [ ] The four panels differ only in what the pirate's gear became.
- [ ] Chip labels match the settings page.
- [ ] At 640 px wide, each panel's outcome is clear without reading the caption.

## Comments
