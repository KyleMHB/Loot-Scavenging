# 02: Render the body GIFs

Status: ready-for-agent

**Recommended model:** `sonnet`: a port of Spearfishing's `render_workshop_gifs.py` with new crop values.

**Blocked by:** 01

**What to build:** `tools/render_workshop_gifs.py` cuts the four GIFs in the spec's "Body GIFs" table from ticket 01's clip and its `shots.json` marks. It writes them to `docs/images/workshop/`.

## Scope

- Model it on `E:\Coding\Rimworld\Spearfishing\tools\render_workshop_gifs.py`:
  - one table row per GIF (shot, mark, seconds before and after, crop);
  - 560 px wide at 12 fps, `palettegen max_colors=96`, looping;
  - FFmpeg found the same way the harness finds it.
- Crop each GIF around its subject, so the beat reads at 560 px.
- `python tools/render_workshop_gifs.py <name>` renders one GIF.

## Acceptance

- [ ] All four GIFs exist, each 4 to 7 seconds and at most 6 MB.
- [ ] Each shows its whole beat at normal speed. For each GIF, three stills (start, middle, end) are checked at 560 px.
- [ ] Rerunning the script reproduces the files from the same footage.

## Comments
