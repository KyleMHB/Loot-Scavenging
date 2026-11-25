# Workshop media: spec

The Workshop page has one image, `About/Preview.png`, and a text-only description. This adds four GIFs to the description body and five gallery images, so a player scrolling the page sees each feature happen instead of reading about it.

## Rules for every image

- **Facts:** every number in an image comes from `Defs/` or `Source/`, read by the script that draws it. Examples are study points, bench tiers and scrap units. Numbers are never typed from the description. Workshop page guide, step 1.
- **Settings:** footage shows the default Progression preset unless the shot is about another setting. When it is, the image names that setting with a settings chip.
- **Speed:** footage runs at normal game speed. A bill job that is too long at 1x is trimmed to its start and its product, never sped up. If a clip must be sped up, the description text beside it says so.
- **Type:** RimWordFont (`Configurable Quality/artifacts/steam-page-mockup/RimWordFont.ttf`), all text lowercased, because its capital O is a decorative glyph.
- **Settings switches:** the Lumen Stones switch, `switch()` and `setting_chip()` in `tools/preview_story.py`.
- **Taglines:** none in the "X • Y • Z" style.
- **Scenes:** filmed in the dev-test harness on clean staged scenes. Dead raiders are redressed, forbidden marks cleared and the camera framed by cells, as in `PreviewStoryBatch`.
- **Era tiles:** may come from UI Rethemed - Era Looks (`kylemhb.uirethemederalooks`), Kyle's own mod.
- **Originality:** nothing from Looting Manager, Non uno Pinata or Technology Blueprints & Reverse Engineering.
- **Files:**
  - Final images go in `docs/images/workshop/`, committed.
  - Raw footage, stills and previews stay in `work/`.
  - Hosted URLs take the form `https://raw.githubusercontent.com/KyleMHB/Loot-Scavenging/main/docs/images/workshop/<file>`.

## Body GIFs

All GIFs are 560 px wide at 12 fps, loop, use a palette of at most 96 colours, and are at most 6 MB each. These match Spearfishing's `render_workshop_gifs.py`. Each runs 4 to 7 seconds. Each sits under the `[h2]` feature subsection whose claim it proves, which means Features becomes illustrated subsections (ticket 06).

| File | Under | What it proves | Beat |
| --- | --- | --- | --- |
| `loot-scavenging-raid.gif` | Description (after the first paragraph) | Gear beyond your research becomes scrap; gear you could make drops as normal | A tribal colony on Progression downs a pirate and kills a tribal raider. The pirate's autopistol becomes industrial scrap where he falls; the tribal raider's club drops whole. |
| `loot-scavenging-recycle.gif` | Recycling at benches you have | A recycle bill at a bench you have turns scrap into its era's materials | A colonist at the crafting spot works a recycle bill on neolithic scrap; the materials appear on the ground. |
| `loot-scavenging-study.gif` | Study for research | A study bill turns scrap into points on the current project | A researcher works a study bill at a simple research bench; the scrap is used up and the game's message reports the points paid into Recurve Bow. |
| `loot-scavenging-on-body.gif` | Loot on the body | With "keep loot on the body", nothing drops until you strip the corpse | A pirate dies and nothing lands on the ground. A colonist strips the corpse and his tribalwear, the scrap from his gun and vest, his medicine and his silver spill out. |

The existing `lootscavenging-milestone` footage has a study, a recycle and a pirate beat. It runs on "scrap everything", and its bill jobs play at 4x. Ticket 01 films fresh clips that meet the rules above. It may reuse the milestone's scene setup code.

## Gallery

Gallery images are 1920x1080 PNG. Order:

1. **`About/Preview.png`** (done, 1280x720, also the thumbnail). Raider gear becomes scrap, kept on the body, studied or recycled.
2. **`gallery-scrap-tiers.png`**: the six scrap items with their in-game sprites on their era tiles. Each shows:
   - the era name;
   - the benches that recycle it;
   - the study points per unit.

   A caption says that a bench handles its own tier and every tier below it.
3. **`gallery-loot-styles.png`**: a fallen tribal warrior in four panels, one per loot style, cut from the promo footage. A chip above the panels shows the Hostiles "Don't drop your stuff" box off, and each panel has a chip naming the setting behind its style:
   - only gear beyond the colony's reach is scrapped;
   - everything he carried is scrapped;
   - hostile pawns' gear is marked unfamiliar, shown selected with the unfamiliar line in its inspect pane;
   - weapons and apparel left vanilla.
4. **`gallery-settings.png`**: an in-game screenshot of the settings page on Progression. It shows the preset line with its sentence, the three "Don't drop your stuff" boxes and the first Who rows with theirs, framed with a short title.
5. **`gallery-inspect.png`**: two scrap stacks, each selected with its inspect pane. One says it can be studied now and the other says why not (needs research). Side by side, with captions.

## Tickets

| # | Ticket | Blocked by |
| --- | --- | --- |
| 01 | Film the Workshop clips and stills | None |
| 02 | Render the body GIFs | 01 |
| 03 | Gallery: scrap tiers | None |
| 04 | Gallery: drop modes | 01 |
| 05 | Gallery: settings page and inspect text | 01 |
| 06 | Illustrated description and page preview | 02, 03, 04, 05 |
| 07 | Publish the images, description and gallery | 06 |
| 08 | Media for 0.5.2 | 06 |
| 09 | Workshop text fixes for 0.6.0 | None |
| 10 | Gallery settings image for 0.6.0 | None |
| 11 | Replace the drop-modes gallery image | Kyle's choice |
| 12 | Push and publish 0.6.0 | 09, 10, 11 |

## Left out

- A preset comparison image (Progression, Cleanup, Vanilla after the same raid). Add it if the Cleanup preset draws questions.
- A GIF of unfamiliar-marked gear. The loot-styles still covers it, and the unfamiliar line reads better still than moving.
