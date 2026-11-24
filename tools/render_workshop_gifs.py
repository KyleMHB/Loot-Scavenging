"""Cut the Workshop description GIFs from the filmed Workshop clips.

Usage:
  python tools/render_workshop_gifs.py                          # every GIF into docs/images/workshop/
  python tools/render_workshop_gifs.py loot-scavenging-raid     # one GIF by name

Footage comes from the `lootscavenging-workshop-clips` batch (2560x1440 `build.mp4` with `shots.json` and
`recording.json` beside it). Each GIF is one or two short segments around named marks, cropped tight to the subject
and joined with a hard cut. Footage runs at normal game speed and is never sped up: a bill job longer than a GIF is
cut to its start and its end, not accelerated. A GIF named in RUNS uses that run; the rest use RUN_NAME. Set
LOOT_CLIPS_RUN to a run folder to render every GIF from that run instead.
"""
import json
import os
import shutil
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "docs" / "images" / "workshop"
HARNESS_CANDIDATES = [
    Path(os.environ["RIMWORLD_DEV_TESTS_ROOT"]) if os.environ.get("RIMWORLD_DEV_TESTS_ROOT") else None,
    Path("E:/Coding/Templates/tools/rimworld-dev-tests"),
    Path("E:/Coding/Rimworld/RimWorldDevTests"),
]
RUN_NAME = "20261124-154152-lootscavenging-workshop-clips-s1"
# The study GIF comes from the 0.5.2 run (study points halved). The downed GIF was refilmed once the downed raider kept
# his weapon in hand. The raid and recycle GIFs keep their first run, and the on-body GIF its Gear-tab refilm, which
# still show what they showed.
RUNS = {
    "loot-scavenging-downed": "20261124-161503-lootscavenging-workshop-clips-s1",
    "loot-scavenging-raid": "20261121-223036-lootscavenging-workshop-clips-s1",
    "loot-scavenging-recycle": "20261121-223036-lootscavenging-workshop-clips-s1",
    "loot-scavenging-on-body": "20261123-000545-lootscavenging-workshop-clips-s1",
}
WIDTH, FPS = 560, 12

# name: [(mark, start offset, end offset, crop width, crop height, crop left, crop top), ...]
# Offsets are seconds from the mark; a segment runs from mark + start to mark + end of video time (the mark's recorder
# time minus the recording's origin). Crops are in 2560x1440 pixels and stay clear of the top-left messages (except the
# study message, which is the point), the right-hand HUD text and neighbouring scenes' items.
GIFS = {
    # Filmed under "Drop the weapon" (the pre-0.5.2 downed behaviour): the downed pirate's autopistol becomes scrap, then the
    # killed raider's club drops whole.
    "loot-scavenging-raid": [
        ("raid-pirate-down", -0.7, 1.7, 620, 349, 1730, 718),
        ("raid-warrior-down", -0.4, 2.4, 800, 450, 100, 400),
    ],
    # A recycle bill at the crafting spot: the job starts, then its materials land on the ground.
    "loot-scavenging-recycle": [
        ("recycle-work", -0.3, 2.2, 450, 253, 1040, 565),
        ("recycle-done", -0.6, 2.4, 450, 253, 1040, 565),
    ],
    # A study bill at the research bench: the scrap is being studied, then the game's message reports the points.
    # The camera is still settling and the recycle shot's message still shows at the job's start, so the first
    # segment starts mid-job, once that message has faded.
    # In the 0.5.2 run the job is 13 s, so one continuous segment runs from mid-job (the recycle shot's message has gone by
    # study-done - 2.8) to the message that reports the points.
    "loot-scavenging-study": [
        ("study-done", -2.8, 2.4, 1260, 860, 200, 0),
    ],
    # Default "Keep his weapon": a pirate stands, is downed and nothing falls; then his Gear tab shows the autopistol
    # under Equipment. One continuous segment; the crop takes the tab (left) and the raider, clear of the forbidden-marked
    # chunks to the right, and stops under the tab's button row.
    "loot-scavenging-downed": [
        ("downed-fall", -0.3, 4.7, 1240, 868, 0, 272),
    ],
    # Nothing lands at the pirate's death; his corpse's Gear tab shows the loot still on him; stripping the corpse spills it
    # and the colonist steps off the pile. The gear crop takes the tab (bottom left) and the corpse; the others are tighter on
    # the corpse, at the same aspect so the segments join without stretching.
    "loot-scavenging-on-body": [
        ("on-body-death", 0.1, 1.2, 900, 630, 540, 470),
        ("on-body-gear", 0.6, 2.4, 1180, 826, 0, 400),
        ("on-body-stripped", -0.5, 3.4, 900, 630, 680, 470),
    ],
}


def harness():
    for candidate in HARNESS_CANDIDATES:
        if candidate and (candidate / "work" / "ffmpeg" / "ffmpeg.exe").is_file():
            return candidate
    sys.exit("shared dev-test harness not found (set RIMWORLD_DEV_TESTS_ROOT)")


def ffmpeg():
    for candidate in [shutil.which("ffmpeg"), ROOT / "work/ffmpeg/ffmpeg.exe", harness() / "work/ffmpeg/ffmpeg.exe"]:
        if candidate and Path(candidate).is_file():
            return str(candidate)
    sys.exit("ffmpeg not found (PATH, work/ffmpeg or the shared harness's work/ffmpeg)")


def run_folder(name):
    override = os.environ.get("LOOT_CLIPS_RUN")
    if override:
        return Path(override)
    return harness() / "work/userdata/loot/LootScavengingTests" / RUNS.get(name, RUN_NAME)


def render(name, segments):
    run = run_folder(name)
    clip = run / "build.mp4"
    if not clip.is_file():
        print(f"{name}: no footage at {clip}; skipped")
        return
    marks = {m["name"]: m["t"] for m in json.loads((run / "shots.json").read_text(encoding="utf-8"))["marks"]}
    origin = json.loads((run / "recording.json").read_text(encoding="utf-8"))["videoOriginMonotonicSeconds"]
    # Every segment is scaled to the same width and height so they join; the first crop sets the aspect.
    height = round(WIDTH * segments[0][4] / segments[0][3] / 2) * 2
    parts, labels = [], []
    for i, (mark, begin, finish, w, h, x, y) in enumerate(segments):
        start = max(0.0, marks[mark] - origin + begin)
        end = marks[mark] - origin + finish
        parts.append(f"[0:v]trim=start={start:.3f}:end={end:.3f},setpts=PTS-STARTPTS,crop={w}:{h}:{x}:{y},"
                     f"fps={FPS},scale={WIDTH}:{height}:flags=lanczos,setsar=1[s{i}]")
        labels.append(f"[s{i}]")
    joined = f"{''.join(labels)}concat=n={len(labels)}:v=1:a=0[v]" if len(labels) > 1 else "[s0]null[v]"
    graph = (";".join(parts) + f";{joined};[v]split[a][b];[a]palettegen=max_colors=96:stats_mode=diff[p];"
             "[b][p]paletteuse=dither=none:diff_mode=rectangle")
    OUT.mkdir(parents=True, exist_ok=True)
    out = OUT / (name + ".gif")
    subprocess.run([ffmpeg(), "-v", "error", "-y", "-i", str(clip), "-filter_complex", graph, "-loop", "0", str(out)],
                   check=True)
    print(f"Wrote {out.relative_to(ROOT)} ({out.stat().st_size / 1e6:.2f} MB)")


if __name__ == "__main__":
    wanted = sys.argv[1:]
    unknown = [n for n in wanted if n not in GIFS]
    if unknown:
        sys.exit(f"unknown GIF {unknown}; choose from {', '.join(GIFS)}")
    for gif, segments in GIFS.items():
        if not wanted or gif in wanted:
            render(gif, segments)
