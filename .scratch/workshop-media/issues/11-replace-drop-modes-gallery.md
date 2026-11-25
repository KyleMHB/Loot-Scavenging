# 11: Replace the drop-modes gallery image

Status: needs-info

**Recommended model:** `opus`: film direction and a public image.

**Blocked by:** Kyle's choice below.

**What to build:** `docs/images/workshop/gallery-drop-modes.png` (ticket 04) is titled "A fallen raider's gear, four drop modes" and labels each panel "Drop mode: ...". 0.6.0 removed drop mode, so the image describes a setting that no longer exists.

## Kyle's choice

One of:

- **A. Drop it.** Leave the image out of the Workshop gallery, delete `drop_modes()` from `tools/workshop_gallery.py` and the file from `docs/images/workshop/`, and update the spec. Quickest.
- **B. Replace it with the "Don't drop your stuff" boxes.** The same downed or dead raider in two panels, Hostiles box on (gear stays on him) and off (it drops and is sorted as it drops). Each panel gets a settings chip naming the box. Needs a new beat in `DevTests/WorkshopClipsBatch.cs` and a refilm.
- **C. Replace it with the loot styles.** The promo footage already shows a tribal warrior dying under each of four loot styles (`lootscavenging-promo-clips`). Cut four stills from that run instead of refilming.

Recommended: C if the promo run's stills frame well, since it needs no new filming; otherwise A for this upload and B later.

## Acceptance

- [ ] Kyle's choice recorded here.
- [ ] No gallery image or spec line refers to drop mode.
- [ ] Any new image follows the spec's rules for every image and was opened and checked at reduced size.
