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
- [x] Chip labels match the settings page.
- [x] At 640 px wide, each panel's outcome is clear without reading the caption.

## Comments

### 2026-11-22: rendered (first criterion open)

- `tools/workshop_gallery.py drop-modes` writes `docs/images/workshop/gallery-drop-modes.png`. Stills are cut from the ticket 01 run at mark + 1.6 s and cropped by cells with `framed()`; `preview_story.SIZE` is set to 1920x1080 so its helpers size to the canvas.
- Chips read "Drop mode: <label>" with the labels from the English Keyed file; the switch is on for the three active modes and off for Off. The keep-original panel carries a 0.8x crop of the game's inspect pane with the unfamiliar line.
- Judgment call: the four stills are four different filmed pirates (each mode has its own staged area), so pose, clothes and the position of the club vary slightly. The gear outcomes differ as intended. A single-pirate re-film would need the batch to switch modes on one scene.
- Checked: viewed at full size and at 640 px; each panel's outcome reads (scrap block, wood and leather scrap, autopistol with selection bracket, autopistol loose).
