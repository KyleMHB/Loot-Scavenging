# 02: Render the body GIFs

Status: resolved

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

- [x] All four GIFs exist, each 4 to 7 seconds and at most 6 MB.
- [x] Each shows its whole beat at normal speed. For each GIF, three stills (start, middle, end) are checked at 560 px.
- [x] Rerunning the script reproduces the files from the same footage.

## Comments

### 2026-11-22: rendered

- `tools/render_workshop_gifs.py` writes the four GIFs to `docs/images/workshop/` from run `20261121-223036-lootscavenging-workshop-clips-s1` (override with `LOOT_CLIPS_RUN`). `python tools/render_workshop_gifs.py <name>` renders one. Each GIF is two segments cut from marks (offsets in seconds from the mark) and joined with a hard cut; nothing is sped up. 560 px wide, 12 fps, 96-colour palette, looping. FFmpeg comes from PATH, `work/ffmpeg`, or the shared harness (`RIMWORLD_DEV_TESTS_ROOT`, then `E:/Coding/Templates/tools/rimworld-dev-tests`).
- Results:

  | GIF | Size | Length | Segments |
  | --- | --- | --- | --- |
  | `loot-scavenging-raid.gif` | 0.72 MB | 5.25 s | pirate falls and his scrap lands; warrior falls and the club drops (560x316) |
  | `loot-scavenging-recycle.gif` | 1.07 MB | 5.50 s | scrap carried to the spot and recycled; materials on the ground (560x314) |
  | `loot-scavenging-study.gif` | 0.40 MB | 5.66 s | mid-job at the bench; the points message (560x382) |
  | `loot-scavenging-on-body.gif` | 0.53 MB | 4.83 s | pirate dies, nothing drops; strip spills the loot (560x316) |

- Marks sit a little late for some beats (the pirate falls about 0.6 s after `raid-pirate-down`), so segments start before the mark. In the study shot the camera is still settling and the recycle shot's "Bill complete" message lingers until about 44.5 s, so the study GIF starts mid-job rather than at `study-work`. The study crop is wide because the points message is top-left, 700 px from the bench; it keeps one stray scrap item from the recycle shot at the left edge.
- Stills (start, middle, end) were checked at 560 px in `work/gifstills/`. A second render gave byte-identical files.
