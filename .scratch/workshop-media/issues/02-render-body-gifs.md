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

### 2026-11-22: on-body GIF redone

- Kyle's notes on the first on-body GIF: the walk to the corpse took too long, and it cut too soon after the strip to show what came off. He also wanted the corpse's Gear tab open before the strip.
- `WorkshopClipsBatch`'s on-body beat now:
  - frames the corpse left of centre at zoom 5;
  - after the death, selects the corpse, opens its Gear tab (`InspectPaneUtility.OpenTab(typeof(ITab_Pawn_Gear))`), marks `on-body-gear` and holds 2.5 s;
  - spawns the stripper drafted, 2 cells east, after the death (no armed raider near him; drafted so he neither wanders nor hauls the loot);
  - after `on-body-stripped`, orders him to a cell 4 east and 1 north, and holds 3.5 s.
  The case also checks the tab opened.
- Refilmed in run `20261123-000545-lootscavenging-workshop-clips-s1` (passed). The script's `RUNS` table points only `loot-scavenging-on-body` at it; the other three GIFs still come from `20261121-223036`.
- The GIF is three segments, hard cut: the death (1.1 s), the Gear tab beside the corpse (1.8 s) and the strip with the loot left lying (3.9 s). 560x392, 6.83 s, 1.05 MB. The gear crop (1180x826 from the left edge) is wider than the others (900x630) to take in the tab, at the same aspect.
- The tab lists cloth tribalwear, industrial scrap x3, medicine x3 and silver x40. The autopistol and flak vest became scrap at death, so the tab has no weapon line.
- Marks here run about 0.55 s ahead of the picture, so the segments start after their marks.
- Stills (start, gear tab, after the strip, end) checked at 560 px in `work/gifstills/`. The tab text reads clearly; the ground items show as icons, without stack counts.
