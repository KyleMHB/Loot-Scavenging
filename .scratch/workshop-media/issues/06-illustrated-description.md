# 06: Illustrated description and page preview

Status: resolved

**Recommended model:** `opus`: a public copy change under the copy rules, plus a layout restructure.

**Blocked by:** 02, 03, 04, 05

**What to build:** `steam-description.md` with the four GIFs placed as in the spec's "Body GIFs" table. The Features list stays as a short scannable list. The GIFs' subjects (raid outcome, recycling, study, loot on the body) become `[h2]` subsections, each with its GIF and a short paragraph. A local page preview shows the description with the gallery in order.

## Scope

- Before writing, read `E:\Coding\Templates\writing-copy.md` and `template-routing.md`, then run `unslop` on the changed prose.
- Image URLs: `https://raw.githubusercontent.com/KyleMHB/Loot-Scavenging/main/docs/images/workshop/<file>`.
- Preview with `Steam/preview-workshop.py`:
  - `--image-map` from that prefix to `docs/images/workshop/`;
  - `--thumbnail About/Preview.png`;
  - `--gallery` for each gallery image in order;
  - output into `work/preview/`.
- Leave the mod version line alone; publishing does not bump it.

## Acceptance

- [x] The description stays under 8,000 characters (the preview tool reports it).
- [x] Every image resolves locally in the preview.
- [x] Each GIF sits under the text whose claim it shows, and no feature claim changed meaning.
- [x] Kyle has reviewed the preview page.

## Comments

### 2026-11-22: description restructured, preview built (review open)

- `steam-description.md`: the raid GIF follows the first paragraph. The Features list keeps all nine items; the recycling, study and loot-on-body items are cut to one line each, and their full text moved unchanged into new `[h2]` subsections (Recycling at benches you have, Study for research, Loot on the body) placed after the scrap-value note, each with its GIF above the paragraph. Headings stay `[h2]` like the rest of the file. No other text changed; `unslop` found nothing in the three new bullets.
- Preview: `work/preview/workshop-page-preview.html`, built with `--image-map` to `docs/images/workshop/`, `--thumbnail About/Preview.png` and the gallery in order scrap-tiers, drop-modes, settings, inspect. The tool reports 6,578 of 8,000 characters. All ten local image paths exist; the only remote image is the existing Ko-fi badge.
- The desktop app's browser pane opens local HTML as a `data:` snapshot, so its `file://` images do not load there; open the file in a normal browser to review.


### 2026-11-22: reviewed

Kyle reviewed the description and gallery and approved them. The on-body GIF is being refilmed with the Gear tab shown; its URL stays the same.
