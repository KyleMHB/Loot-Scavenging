# 03: Gallery: scrap tiers

Status: resolved

**Recommended model:** `sonnet`: Pillow composition from data in Defs, with helpers that already exist.

**Blocked by:** None (can start immediately)

**What to build:** `docs/images/workshop/gallery-scrap-tiers.png`, a 1920x1080 card. It shows the six scrap items, each on its era tile with its era name, the benches that recycle it and its study points per unit. A caption says a bench handles its own tier and every tier below it.

## Scope

- `tools/workshop_gallery.py`, a new script that renders the gallery images and grows with tickets 04 and 05:
  - `scrap-tiers` draws this card;
  - it imports `font`, `shadowed`, `title` and `setting_chip` from `tools/preview_story.py` instead of copying them.
- Read the benches from `Defs/ScrapTiers.xml` and the study points from `Defs/Study.xml` (or whichever def holds them).
- Use bench labels from the game's defs (`E:/Steam/steamapps/common/RimWorld/Data`), not defNames.
- Scrap sprites come from this mod's `Textures/`. Era tiles come from UI Rethemed - Era Looks, Kyle's own mod.
- Animal-era gear becomes neolithic scrap. Say so in a small caption if room allows.

## Acceptance

- [x] Every bench and point value on the card matches the Defs, checked by reading them in the script.
- [x] The card is legible at 640 px wide, about the gallery-strip view: era names and point values read.
- [x] All text is RimWordFont, lowercase, with no triad tagline.

## Comments

### 2026-11-22: rendered

- `tools/workshop_gallery.py scrap-tiers` writes `docs/images/workshop/gallery-scrap-tiers.png` (1920x1080). Benches come from `Defs/ScrapTiers.xml`, points per unit from `Defs/ThingDefs/Scrap.xml` (where they live, not `Study.xml`), era names from the scrap labels, bench labels from the game's `Buildings_Production.xml`.
- Era tiles use Era Looks' palette and pattern for each era (rock art/fieldstone, tapestry/cut stone, engraving/tread plate, spacer/hull panels, ultratech/hex tiles, archotech/etched circuits), read from its `EraLooks.xml`; the sprites are the mod's `_c` textures.
- The card shows each tier's own benches only; the caption says a bench handles its own tier and below. The animal-era note is a second small line.
- Checked: viewed at full size and at 640 px wide; era names, point values and bench names read at 640.
