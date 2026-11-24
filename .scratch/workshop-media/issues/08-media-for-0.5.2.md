# 08: Media for 0.5.2

Status: ready-for-human

**Recommended model:** `opus`: film direction and public copy.

**Blocked by:** 06 (done). Follows ticket 21, which halved study points and made "Keep the weapon in his pack" the downed default.

**What to build:** refilm the media that showed the old study values, keep the raid shot under an honest chip, and add a downed-raider shot and GIF.

## What was built

- `DevTests/WorkshopClipsBatch.cs`: a new `downed` beat, after the on-body beat. Under the default "Keep the weapon in his pack" a pirate (collar shirt, pants, flak vest, autopistol) is anesthetised. He cannot get up during the shot, so the other agent's re-arm work does not matter. The camera selects him and opens his Gear tab (marks `downed`, `downed-fall`, `downed-gear`). The case checks that nothing lies within 3 cells, the autopistol is in his pack, he stays down and the tab opened. The batch still sets "Drop the weapon" for the raid beat and restores it after the new one.
- Run `20261124-154152-lootscavenging-workshop-clips-s1`, passed. An earlier run, `20261124-153920-...`, put the raider too far left, so the Gear tab covered half of him; the camera moved 2.5 cells.
- `tools/render_workshop_gifs.py`: `loot-scavenging-study.gif` and the new `loot-scavenging-downed.gif` come from the new run. The raid, recycle and on-body GIFs keep their earlier runs and are unchanged.
- `tools/render_promo.py`: a `downed` section after the raid, captioned "a downed raider keeps his weapon in his pack", with no chip because it is the default. The study section uses the new run. The raid's `raid-pirate-down` segment has a chip, "When a non-colony pawn is downed: Drop the weapon" (the Keyed label and value), since it was filmed under that choice.
- `tools/workshop_gallery.py inspect` reads the new run (`INSPECT_RUN`), so `gallery-inspect.png` now reads 5 and 12.5 points per unit from the game's own inspect pane. `drop-modes` keeps its run.
- `tools/preview_story.py`: the font path falls back to the workspace's `Configurable Quality` folder, since in a worktree `ROOT.parent` is not the workspace.
- `steam-description.md`: a short `[h2]` "Downed raiders keep their weapon" with the new GIF and a paragraph, after "Loot on the body". 7,795 of 8,000 characters in the Workshop preview (`work/preview/workshop-page-preview.html`).

## Choices

- Raid footage reused: the old run was filmed under "Drop the weapon", which the batch still sets, so it plays as filmed. Only the promo segment gets a chip. The raid GIF has no captions, so it has no chip.
- The new description paragraph says the first GIF was filmed under "Drop the weapon". The paragraph under the raid GIF says a downed pawn keeps his weapon in his pack, and the GIF shows it scrapped at the downing, so the pairing needed a note. Kyle may prefer to refilm the raid under the default (his autopistol would then become scrap only at the strip or death).
- The study GIF is one continuous segment now: the job runs 13 s, not 25 s, so there is no need to join a mid-job cut to a result cut.
- The downed GIF is one 5 s segment: the pirate stands, goes down, and the Gear tab opens with the autopistol under "Inventory".

## Stone knife check

"A stone knife gives one unit and a charge rifle gives several" still holds. `ScrapAmounts.Units` is round(sqrt(value) / 4) per item with a minimum of 1: a knife at about 12 silver gives 1, a charge rifle at about 1,650 gives about 10. Left unchanged.

## Acceptance

- [x] Study GIF, promo study section and inspect gallery show the new values (+30 for six units; 5 and 12.5 per unit).
- [x] New downed shot in the promo and the GIF; description checked in the preview tool.
- [x] Every GIF and still opened and viewed; captions clear of the action, Gear tab legible.
- [ ] Kyle reviews the promo and the description. Nothing is pushed or uploaded.
