# 11: Replace the drop-modes gallery image

Status: resolved

**Recommended model:** `opus`: film direction and a public image.

**Blocked by:** Kyle's choice below.

**What to build:** `docs/images/workshop/gallery-drop-modes.png` (ticket 04) is titled "A fallen raider's gear, four drop modes" and labels each panel "Drop mode: ...". 0.6.0 removed drop mode, so the image describes a setting that no longer exists.

## Kyle's choice

One of:

- **A. Drop it.** Leave the image out of the Workshop gallery, delete `drop_modes()` from `tools/workshop_gallery.py` and the file from `docs/images/workshop/`, and update the spec. Quickest.
- **B. Replace it with the "Don't drop your stuff" boxes.** The same downed or dead raider in two panels, Hostiles box on (gear stays on him) and off (it drops and is sorted as it drops). Each panel gets a settings chip naming the box. Needs a new beat in `DevTests/WorkshopClipsBatch.cs` and a refilm.
- **C. Replace it with the loot styles.** The promo footage already shows a tribal warrior dying under each of four loot styles (`lootscavenging-promo-clips`). Cut four stills from that run instead of refilming.

Recommended: C if the promo run's stills frame well, since it needs no new filming; otherwise A for this upload and B later.

**Chosen (2026-11-25): C.**

## Acceptance

- [x] Kyle's choice recorded here.
- [x] No gallery image or spec line refers to drop mode.
- [x] Any new image follows the spec's rules for every image and was opened and checked at reduced size.

## Comments

### 2026-11-25: rendered from the promo run

- `python tools/workshop_gallery.py loot-styles` writes `docs/images/workshop/gallery-loot-styles.png` (1920x1080) and replaces `gallery-drop-modes.png`, which is removed. `drop_modes()` became `loot_styles()`.
- Source run: `20261123-004719-lootscavenging-promo-clips-s1`. Stills are cut at each `drop-X-dead` mark + 1.8 s (`drop-keeporiginal-selected` for the unfamiliar panel) and cropped in frame pixels, since the run has no `layout.json`.
- The footage was filmed under 0.5.2 drop modes. Chips name the 0.6.0 setting that gives the same result, as `ModHarness.ApplyStyle` maps them: "Only gear beyond the colony's reach" on and off, "Hostile pawns: Mark unfamiliar", and "Weapons, Apparel: Vanilla".
- A centred chip shows the Hostiles "Don't drop your stuff" box off, since it defaults on in 0.6.0 and a dead raider would otherwise drop nothing.
- The spec's ticket table keeps the original titles of tickets 04 and 11, which name drop modes.
- As in ticket 04, the four warriors are different pawns, so pose and clothes vary slightly.
- Checked: viewed at full size and at 640 px; each outcome reads (scrap block and whole club, wood and leather scrap, selected autopistol with its unfamiliar line, autopistol and club loose).
