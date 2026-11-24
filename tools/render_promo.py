"""Stitch the Workshop promo video from filmed footage; every section moves, with no still images.

Usage:
  python tools/render_promo.py            # renders work/promo/loot-scavenging-promo.mp4
  python tools/render_promo.py --stills   # also writes one still per segment into work/promo/stills/

Footage comes from `lootscavenging-workshop-clips` and `lootscavenging-promo-clips` runs (2560x1440 `build.mp4` with
`shots.json` and `recording.json`). Each live segment is cut around a named mark, cropped to 16:9, scaled to 1920x1080 and
played at normal game speed: a bill job too long for the video is cut to its start and its product, never sped up. The
title and end cards set their text over blurred, dimmed footage that keeps playing. Sections fade to and from black;
segments inside a section join with a hard cut. The video is silent.

The on-body beat and the promo-clips beats each have their own run id and segment table, so a refilmed run can replace
its beats alone.
"""
import json
import os
import shutil
import subprocess
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

from PIL import Image

import preview_story as ps
from preview_story import font, setting_chip, shadowed

ROOT = Path(__file__).resolve().parents[1]
OUT_DIR = ROOT / "work" / "promo"
OUT = OUT_DIR / "loot-scavenging-promo.mp4"
PARTS = OUT_DIR / "parts"
STILLS = OUT_DIR / "stills"
HARNESS_CANDIDATES = [
    Path(os.environ["RIMWORLD_DEV_TESTS_ROOT"]) if os.environ.get("RIMWORLD_DEV_TESTS_ROOT") else None,
    Path("E:/Coding/Templates/tools/rimworld-dev-tests"),
    Path("E:/Coding/Rimworld/RimWorldDevTests"),
]
MAIN_RUN = "20261121-223036-lootscavenging-workshop-clips-s1"
# The study beat (points halved in 0.5.2) and the downed raider beat come from this run of the same batch; the raid and recycle
# beats keep MAIN_RUN, which was filmed under "Drop the weapon" and still plays as it did.
NEW_RUN = "20261124-154152-lootscavenging-workshop-clips-s1"
# The downed raider beat, refilmed with the weapon kept in hand.
DOWNED_RUN = "20261124-161503-lootscavenging-workshop-clips-s1"
# The on-body beat was refilmed with the corpse's Gear tab open. Its crops and offsets in ON_BODY belong to this run.
ON_BODY_RUN = "20261123-000545-lootscavenging-workshop-clips-s1"
# The drop-mode deaths and the settings page, filmed live for the promo in place of the gallery images.
PROMO_RUN = "20261123-004719-lootscavenging-promo-clips-s1"
W, H, FPS = 1920, 1080, 30
FADE = 0.35

# A live segment: (mark, start offset, end offset, crop (w, h, left, top) in 2560x1440 px, caption, freeze seconds[, chip]).
# A chip (Keyed label, Keyed value, switch on) names the segment's non-default setting; a section's chip covers the rest.
# Offsets are seconds from the mark's video time. Crops are 16:9 and keep out the top-left messages (except the study
# points message, which is the point), the right-hand HUD and alerts, and neighbouring scenes.
RAID = [
    # The pirate fires on the archers; the tribal raider runs at the wall.
    ("raid-attack", 1.3, 4.3, (2048, 1152, 200, 180), "raiders attack a tribal colony", 0),
    # The pirate falls about 0.6 s after his mark; his autopistol becomes scrap where he lies. Alerts slide in beside
    # him about 2.5 s after the mark, so the segment ends before them.
    # Filmed under "Drop the weapon", which is no longer the default, so this segment names it with a chip.
    ("raid-pirate-down", -1.0, 2.25, (1120, 630, 1220, 590), "gear beyond your research becomes scrap", 0,
     ("LootScavenging_Downed", "LootScavenging_DownedDropWeapon", True)),
    # The raider dies at the wall and his club drops whole.
    ("raid-warrior-down", -0.4, 2.6, (1280, 720, 0, 200), "gear you could make drops whole", 0),
]
RECYCLE = [
    # The colonist carries neolithic scrap to the crafting spot and starts the bill.
    ("recycle-work", -1.4, 1.9, (1280, 720, 690, 340), "recycle scrap at benches you already have", 0),
    # The product lands on the ground.
    ("recycle-done", -1.0, 2.4, (1280, 720, 690, 340), "it becomes materials of its era", 0),
]
# Default "Keep his weapon", so no chip: a pirate is downed, nothing falls, and his Gear tab shows the autopistol
# under Equipment. Refilmed in DOWNED_RUN once the downed raider kept his weapon in hand. The tab covers the left of the frame; the crop ends inside its empty lower part, above the caption band, and clear of the chunks to the
# right, and the caption band covers only the tab's empty lower part.
DOWNED = [
    ("downed-fall", -0.3, 4.2, (1440, 810, 0, 230), "a downed raider keeps his weapon", 0),
]
STUDY = [
    # Mid-job at the bench; the recycle shot's "Bill complete" message has faded by now (0.5.2 run: the job is 13 s).
    ("study-done", -2.8, -0.6, (1280, 720, 580, 320), "or study it at a research bench", 0),
    # The game's message reports the points (+30 for six units of neolithic scrap); wide enough to hold it and the bench.
    # The camera leaves about 3 s after the mark, so the last frame is held a moment for reading.
    ("study-done", -0.6, 2.4, (1760, 990, 150, 0), "for points toward your current research", 1.0),
]
ON_BODY = [
    # The picture lags this run's marks by about 0.55 s.
    # Everything stays on the body: the pirate dies and nothing lands on the ground.
    ("on-body-death", -0.6, 2.1, (1280, 720, 400, 380), "nothing drops at death", 0),
    # The corpse is selected with its Gear tab open: the scrap, medicine and silver are still on him. The tab closes
    # about 2.9 s after the mark, so the last frame is held a moment for reading.
    ("on-body-gear", 0.55, 2.85, (1440, 810, 0, 280), "his gear stays on the body", 0.4),
    # A colonist strips the corpse, the loot spills out and he steps away.
    ("on-body-stripped", -1.18, 3.0, (1280, 720, 400, 380), "strip the corpse and it all spills out", 0),
]
# The shot's non-default setting, named by a chip (Keyed label: Keyed value), as the spec's settings rule asks.
ON_BODY_CHIP = ("LootScavenging_DeathDrop", "LootScavenging_DeathDropAllOnBody", True)
# The picture lags this run's marks by about 0.55 s, so each segment starts 0.75 s after its mark, past the camera's cut.
DROP_MODES = [
    # The warrior stands a beat and dies; his autopistol becomes industrial scrap, the club drops whole.
    ("drop-scrapoutofreach", 0.75, 3.6, (1280, 720, 480, 320), "the autopistol becomes scrap; the club drops whole", 0,
     ("LootScavenging_DropMode", "LootScavenging_DropModeScrapOutOfReach", True)),
    # The club and tribalwear become neolithic scrap too; the body is redressed in the same tick so it never shows naked.
    ("drop-scrapeverything", 0.75, 3.6, (1280, 720, 620, 200), "everything he carried becomes scrap", 0,
     ("LootScavenging_DropMode", "LootScavenging_DropModeScrapEverything", True)),
    # Both drop whole; the autopistol is selected about 2 s after the mark and its inspect pane opens at the bottom left.
    # The crop reaches the screen's bottom, so the pane's title, which carries "(unfamiliar)", and its first lines sit above
    # the caption band; the band covers the Architect tooltip under the pane.
    ("drop-keeporiginal", 0.75, 4.9, (2048, 1152, 0, 288), "both drop whole; the autopistol reads unfamiliar", 0,
     ("LootScavenging_DropMode", "LootScavenging_DropModeKeepOriginal", True)),
    # The off scene sits near the map's east edge; the crop stays west of it.
    ("drop-off", 0.75, 3.6, (1280, 720, 760, 340), "no era handling: both drop whole", 0,
     ("LootScavenging_DropMode", "LootScavenging_DropModeOff", False)),
]
# The settings window, centred at UI scale 1.5; the crop holds its presets, who-and-where and the first item rules.
SETTINGS = [
    ("settings-progression", 0.75, 3.0, (1440, 810, 560, 30), "progression: gear beyond your research becomes scrap", 0),
    ("settings-cleanup", 0.75, 3.0, (1440, 810, 560, 30), "cleanup: poor gear and hostile corpses are cleared away", 0),
    ("settings-vanilla", 0.75, 3.0, (1440, 810, 560, 30), "vanilla: loot as in the base game", 0),
]

# An overlay card: text over blurred, dimmed footage that keeps playing. (run id, mark, start offset, crop)
TITLE_FOOTAGE = (MAIN_RUN, "raid-attack", -1.2, (2048, 1152, 200, 180))
# The stripped corpse's spilled loot, before the run's camera moves on about 4 s after the mark.
END_FOOTAGE = (ON_BODY_RUN, "on-body-stripped", -0.5, (1280, 720, 400, 380))

# (name, kind, content, seconds for cards); live content is (run id, segments, chip or None)
SECTIONS = [
    ("title", "card", "title", 4.5),
    ("raid", "live", (MAIN_RUN, RAID, None), None),
    ("downed", "live", (DOWNED_RUN, DOWNED, None), None),
    ("recycle", "live", (MAIN_RUN, RECYCLE, None), None),
    ("study", "live", (NEW_RUN, STUDY, None), None),
    ("on-body", "live", (ON_BODY_RUN, ON_BODY, ON_BODY_CHIP), None),
    ("drop-modes", "live", (PROMO_RUN, DROP_MODES, None), None),
    ("settings", "live", (PROMO_RUN, SETTINGS, None), None),
    ("end", "card", "end", 4.5),
]


def harness():
    for candidate in HARNESS_CANDIDATES:
        if candidate and (candidate / "work" / "ffmpeg" / "ffmpeg.exe").is_file():
            return candidate
    sys.exit("shared dev-test harness not found (set RIMWORLD_DEV_TESTS_ROOT)")


def tool(name):
    for candidate in [shutil.which(name), ROOT / f"work/ffmpeg/{name}.exe", harness() / f"work/ffmpeg/{name}.exe"]:
        if candidate and Path(candidate).is_file():
            return str(candidate)
    sys.exit(f"{name} not found (PATH, work/ffmpeg or the shared harness's work/ffmpeg)")


def run_folder(run_id):
    return harness() / "work/userdata/loot/LootScavengingTests" / run_id


def mark_times(run):
    marks = {m["name"]: m["t"] for m in json.loads((run / "shots.json").read_text(encoding="utf-8"))["marks"]}
    origin = json.loads((run / "recording.json").read_text(encoding="utf-8"))["videoOriginMonotonicSeconds"]
    return {name: t - origin for name, t in marks.items()}


def duration(path):
    out = subprocess.run([tool("ffprobe"), "-v", "error", "-show_entries", "format=duration", "-of", "csv=p=0", str(path)],
                         check=True, capture_output=True, text=True).stdout
    return float(out.strip())


# ---------- overlays and cards (PIL) ----------

def keyed():
    root = ET.parse(ROOT / "Languages" / "English" / "Keyed" / "LootScavenging.xml").getroot()
    return {e.tag: (e.text or "") for e in root}


def caption_png(text, path, chip=None):
    """A full-frame transparent layer with a dark band and the caption at the bottom, and a settings chip at the top left
    when the shot uses a non-default setting."""
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    band = 104
    img.alpha_composite(Image.new("RGBA", (W, band), (0, 0, 0, 160)), (0, H - band))
    size = 50
    while font(False, size).getlength(text.lower()) > W - 120:
        size -= 1
    shadowed(img, (W / 2, H - band / 2), text.lower(), font(True, size), fill=ps.CREAM)
    if chip:
        text_of = keyed()
        setting_chip(img, (28, 28), f"{text_of[chip[0]]}: {text_of[chip[1]]}", chip[2], W - 56, size=30)
    img.save(path)


def supported_versions():
    root = ET.parse(ROOT / "About" / "About.xml").getroot()
    return [li.text for li in root.find("supportedVersions")]


def dimmed():
    """A full-frame layer that darkens the blurred footage under a card's text."""
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    img.alpha_composite(Image.new("RGBA", (W, H), (0, 0, 0, 170)))
    return img


def title_overlay(path):
    """The mod's name, in About/Preview.png's cream and gold, and what it does."""
    img = dimmed()
    shadowed(img, (W / 2, 330), "loot", font(True, 210), fill=ps.CREAM, blur=8, offset=5)
    shadowed(img, (W / 2, 560), "scavenging", font(True, 210), fill=(255, 193, 82, 255), blur=8, offset=5)
    line = "raiders' gear becomes scrap of its era, to recycle or study"
    size = 50
    while font(True, size).getlength(line) > W - 160:
        size -= 1
    shadowed(img, (W / 2, 790), line, font(True, size), fill=ps.CREAM)
    img.save(path)


def end_overlay(path):
    """The mod name and where to find it."""
    img = dimmed()
    shadowed(img, (W / 2, 450), "loot scavenging", font(True, 150), blur=8, offset=5)
    shadowed(img, (W / 2, 610), "on the steam workshop", font(True, 64), fill=ps.CREAM)
    versions = supported_versions()
    listed = " and ".join([", ".join(versions[:-1]), versions[-1]] if len(versions) > 1 else versions)
    shadowed(img, (W / 2, 700), f"for rimworld {listed}", font(True, 40), fill=(190, 184, 168, 255))
    img.save(path)


# ---------- rendering ----------

ENCODE = ["-c:v", "libx264", "-preset", "slow", "-crf", "16", "-pix_fmt", "yuv420p", "-r", str(FPS), "-an"]


def render_card(name, source, seconds):
    """A card's text over its footage, blurred and dimmed, which keeps playing underneath."""
    overlay = PARTS / f"{name}-overlay.png"
    (title_overlay if source == "title" else end_overlay)(overlay)
    run_id, mark, offset, (cw, ch, cx, cy) = TITLE_FOOTAGE if source == "title" else END_FOOTAGE
    run = run_folder(run_id)
    start = max(0.0, mark_times(run)[mark] + offset)
    out = PARTS / f"{name}.mp4"
    # The video's first frame is its preview on Steam, Discord and YouTube, so the title opens on its text, not black.
    fade_in = "" if source == "title" else f"fade=t=in:st=0:d={FADE},"
    chain = (f"[0:v]crop={cw}:{ch}:{cx}:{cy},scale={W}:{H}:flags=lanczos,setsar=1,fps={FPS},gblur=sigma=14[v];"
             f"[v][1:v]overlay=0:0:format=auto,format=yuv420p,"
             f"{fade_in}fade=t=out:st={seconds - FADE:.3f}:d={FADE}[out]")
    subprocess.run([tool("ffmpeg"), "-v", "error", "-y", "-ss", f"{start:.3f}", "-t", f"{seconds:.3f}",
                    "-i", str(run / "build.mp4"), "-i", str(overlay), "-filter_complex", chain, "-map", "[out]",
                    "-t", f"{seconds:.3f}", *ENCODE, str(out)], check=True)
    return out, [(name, seconds)]


def render_live(name, run_id, segments, chip=None):
    """Each segment is cut, cropped, captioned and encoded on its own, then the section's segments are joined with
    hard cuts. The first segment fades in from black and the last fades out."""
    run = run_folder(run_id)
    clip = run / "build.mp4"
    if not clip.is_file():
        sys.exit(f"{name}: no footage at {clip}")
    marks = mark_times(run)
    pieces, report = [], []
    for i, (mark, begin, finish, (cw, ch, cx, cy), text, freeze, *own_chip) in enumerate(segments):
        assert abs(cw / ch - 16 / 9) < 0.01, f"{name} segment {i} crop is not 16:9"
        start = max(0.0, marks[mark] + begin)
        span = marks[mark] + finish - start
        seconds = span + freeze
        cap = PARTS / f"{name}-{i}-caption.png"
        caption_png(text, cap, own_chip[0] if own_chip else chip)
        chain = f"[0:v]crop={cw}:{ch}:{cx}:{cy},scale={W}:{H}:flags=lanczos,setsar=1,fps={FPS}"
        if freeze:
            chain += f",tpad=stop_mode=clone:stop_duration={freeze}"
        chain += "[v];[v][1:v]overlay=0:0:format=auto,format=yuv420p"
        if i == 0:
            chain += f",fade=t=in:st=0:d={FADE}"
        if i == len(segments) - 1:
            chain += f",fade=t=out:st={seconds - FADE:.3f}:d={FADE}"
        out = PARTS / f"{name}-{i}.mp4"
        subprocess.run([tool("ffmpeg"), "-v", "error", "-y", "-ss", f"{start:.3f}", "-t", f"{span:.3f}", "-i", str(clip),
                        "-i", str(cap), "-filter_complex", chain + "[out]", "-map", "[out]", "-t", f"{seconds:.3f}",
                        *ENCODE, str(out)], check=True)
        pieces.append(out)
        report.append((f"{name}: {text}", duration(out), f"source {start:.2f}-{start + span:.2f} s of {run_id}"
                       + (f", last frame held {freeze:.1f} s" if freeze else "")))
    return pieces, report


def concat(pieces, out):
    listing = PARTS / "concat.txt"
    listing.write_text("".join(f"file '{p.as_posix()}'\n" for p in pieces), encoding="utf-8")
    subprocess.run([tool("ffmpeg"), "-v", "error", "-y", "-f", "concat", "-safe", "0", "-i", str(listing),
                    "-c", "copy", "-movflags", "+faststart", str(out)], check=True)


def main(argv):
    PARTS.mkdir(parents=True, exist_ok=True)
    pieces, timeline = [], []
    for name, kind, content, seconds in SECTIONS:
        if kind == "card":
            out, _ = render_card(name, content, seconds)
            pieces.append(out)
            timeline.append((name, duration(out), "card"))
        else:
            run_id, segments, chip = content
            parts, report = render_live(name, run_id, segments, chip)
            pieces += parts
            timeline += report
    concat(pieces, OUT)
    total = duration(OUT)
    print(f"Wrote {OUT.relative_to(ROOT)} ({OUT.stat().st_size / 1e6:.2f} MB, {total:.2f} s)")
    t = 0.0
    if "--stills" in argv:
        STILLS.mkdir(parents=True, exist_ok=True)
    for i, (label, seconds, note) in enumerate(timeline):
        print(f"  {t:6.2f}-{t + seconds:6.2f}  {label}  ({note})")
        if "--stills" in argv:
            # One still near the middle of each segment, and one late in it to catch what the segment ends on.
            for tag, at in (("a", t + seconds * 0.45), ("b", t + seconds - FADE - 0.15)):
                path = STILLS / f"{i:02d}{tag}-{label.split(':')[0]}.png"
                subprocess.run([tool("ffmpeg"), "-v", "error", "-y", "-ss", f"{at:.3f}", "-i", str(OUT), "-frames:v", "1",
                                str(path)], check=True)
        t += seconds


if __name__ == "__main__":
    main(sys.argv[1:])
