# 01: Film the Workshop clips and stills

Status: done

**Recommended model:** `opus`: staged scenes, camera framing and timed marks in a new showcase batch.

**Blocked by:** None (can start immediately)

**What to build:** A showcase batch, `lootscavenging-workshop-clips`, films the four body-GIF beats and the stills for gallery images 3 and 5. The spec's "Body GIFs" and "Gallery" sections describe them. The batch writes a `shots.json` with named marks on the recorder's clock, as `MilestoneBatch` does, so ticket 02 can cut each GIF from a mark.

## Scope

- `DevTests/WorkshopClipsBatch.cs`, registered in `ModHarness.CreateBatches`. It is not part of run-all.
- Shots, each on its own clean staged area, under default Progression settings unless noted:
  - `raid`: a tribal colony. A pirate with an autopistol and flak vest is downed, and a tribal raider with a club is killed. Marks at each fall.
  - `recycle`: a recycle bill on neolithic scrap at the crafting spot, at normal speed. Marks at the job's start and at its product.
  - `study`: a study bill at a simple research bench with a current project set and the HUD on. Marks at the start and when the points land.
  - `on-body`: "Keep loot on the body" on. A raider dies and a colonist strips the corpse. Marks at the death and at the strip.
  - `drop-modes` stills: the same downed pirate under each of the four drop modes. Keep-original shows the item selected with its inspect pane.
  - `inspect` stills: one scrap stack that can be studied now and one that needs research, each selected.
- Settings changed for a shot go through `ModHarness.SaveSettings` / `RestoreSettings`.
- Reuse `PreviewStoryBatch` and `MilestoneBatch` helpers and setup where they fit. Before building, check whether the milestone run's footage still exists; a beat it covers at 1x under the right settings need not be refilmed.
- `TESTING.md`: an entry for the batch with its run command and steps.

## Acceptance

- [x] One `--record --show-ui` run produces a clip with every mark in `shots.json`, and the run passes. Its cases check that each beat happened on film, for example that scrap exists where the pirate fell and nothing lies on the ground after the on-body death.
- [x] Stills from `--frames` or `frames --at` show each shot uncluttered: no red forbidden marks, no naked corpses, and the subject centred.
- [x] No settings changes survive the run.
- [x] Both builds compile; `deploy.ps1` was run before recording.

## Comments

### 2026-11-21: filmed

- Batch `DevTests/WorkshopClipsBatch.cs` (`lootscavenging-workshop-clips`), registered in `ModHarness`. Run `20261121-223036-lootscavenging-workshop-clips-s1` (seed 1, `--mods default --record --show-ui --frames 12 --quit-on-finish`) passed. The clip is `build.mp4` in `E:/Coding/Templates/tools/rimworld-dev-tests/work/userdata/loot/LootScavengingTests/<run>/`, beside `shots.json` and `layout.json`.
- Mark times in the video, in seconds; video time is the mark minus `recording.json`'s `videoOriginMonotonicSeconds`:

  | Beat | Marks |
  | --- | --- |
  | Raid | attack 1.2, pirate down 6.3, warrior down 8.4, over 11.4 |
  | Recycle | work 14.7, done 31.0 (16 s at 1x) |
  | Study | work 34.5, done 59.7 (25 s at 1x) |
  | On-body | death 63.2, strip ordered 65.2, stripped 71.3 |
  | Drop-mode stills | 74.3, 77.4, 80.4, 83.4 |
  | Inspect stills | 86.4, 89.4 |

- What happened in each beat:
  - **Raid:** the archers, pirate and warrior act on direct orders. A lone raid lord wandered off the map, and the first run's raiders went for colonists in the other scenes.
  - **Raid falls:** bow fire did not drop anyone within 15 s, so the pirate is downed by developer at 5 s and the warrior killed at 7 s, both after a real exchange of fire and melee. His autopistol becomes 1 industrial scrap beside him; the club drops whole.
  - **Colonists:** each scene's colonists are spawned for their shot and removed after it, so no idle colonist wanders into another shot.
  - **On-body:** nothing landed on the ground at death. The strip dropped tribalwear, 3 industrial scrap, 3 industrial medicine and 40 silver.
  - **Drop modes, as logged:**
    - out-of-reach: club, 1 industrial scrap;
    - everything: 2 neolithic and 1 industrial scrap (the corpse was redressed);
    - keep original: club and autopistol, with "(unfamiliar)" in its inspect pane;
    - off: club and autopistol.
  - **Inspect:**
    - neolithic: "Can be studied now for Recurve bow, with a study bill at a research bench." (10 points per unit);
    - spacer: "Cannot be studied yet: The colony has not reached the spacer era." (50 per unit).
- For ticket 02: the recycle and study jobs are longer than a GIF, so cut each job's start and its end (product, message) rather than speeding it up.
- Crop notes:
  - The scenes sit near each other, so wide crops can catch a neighbouring scene. The raid's downed pirate shows at the top right of the on-body shot, and the on-body loot at the right of the spacer inspect still.
  - Crop tight to the subject.
  - Top-left messages ("work speed penalty: Outdoors", "Bill complete") stay out of crops.
