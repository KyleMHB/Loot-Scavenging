# 06: Illustrated description and page preview

Status: ready-for-agent

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

- [ ] The description stays under 8,000 characters (the preview tool reports it).
- [ ] Every image resolves locally in the preview.
- [ ] Each GIF sits under the text whose claim it shows, and no feature claim changed meaning.
- [ ] Kyle has reviewed the preview page.

## Comments
