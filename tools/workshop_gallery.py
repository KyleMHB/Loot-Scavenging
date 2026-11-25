"""Renders the Workshop gallery images (1920x1080) into docs/images/workshop/.

    python tools/workshop_gallery.py scrap-tiers
    python tools/workshop_gallery.py loot-styles [run folder]
    python tools/workshop_gallery.py settings [frame.png]
    python tools/workshop_gallery.py inspect [run folder]

inspect cuts stills from a lootscavenging-workshop-clips run (shots.json marks, layout.json cameras); loot-styles cuts them from
a lootscavenging-promo-clips run (PROMO_RUN by default). settings uses work/gallery/settings-frame.png, a frame from a `lootscavenging-ui --record
--show-ui` run (see TESTING.md). Numbers come from Defs/ (study points from SettingsDefaults.xml), bench labels from the game's Data, mode labels from the English Keyed
file, and the inspect text is the game's own, cropped from the stills.
"""
from pathlib import Path
import json
import re
import subprocess
import sys
import xml.etree.ElementTree as ET

from PIL import Image, ImageDraw, ImageFilter

import preview_story as ps
from preview_story import font, setting_chip, shadowed, title

ROOT = ps.ROOT
OUT = ROOT / "docs" / "images" / "workshop"
WORK = ROOT / "work" / "gallery"
GAME_DATA = ps.GAME / "Data"
ERA_LOOKS = ps.GAME / "Mods" / "UI-Rethemed-Era-Looks"
CLIPS_RUN = Path("E:/Coding/Templates/tools/rimworld-dev-tests/work/userdata/loot/LootScavengingTests/20261121-223036-lootscavenging-workshop-clips-s1")
# The inspect stills were refilmed in 0.5.2, when study points per unit were halved (5 and 12.5, not 10 and 25 and 50).
INSPECT_RUN = Path("E:/Coding/Templates/tools/rimworld-dev-tests/work/userdata/loot/LootScavengingTests/20261124-154152-lootscavenging-workshop-clips-s1")
# The loot-styles stills: the promo video's drop-mode deaths (no layout.json, so crops are in frame pixels).
PROMO_RUN = Path("E:/Coding/Templates/tools/rimworld-dev-tests/work/userdata/loot/LootScavengingTests/20261123-004719-lootscavenging-promo-clips-s1")
CANVAS = (1920, 1080)
ps.SIZE = CANVAS  # framed() and title() size their layers from preview_story.SIZE
BG = (38, 36, 33, 255)
MUTED = (190, 184, 168, 255)


# ---------- data ----------

def keyed():
    root = ET.parse(ROOT / "Languages" / "English" / "Keyed" / "LootScavenging.xml").getroot()
    return {e.tag: (e.text or "") for e in root}


def thing_defs():
    """defName -> (label, sprite folder) for the mod's scrap items."""
    out = {}
    for d in ET.parse(ROOT / "Defs" / "ThingDefs" / "Scrap.xml").getroot().iter("ThingDef"):
        name = d.findtext("defName")
        if name and name.startswith("LS_Scrap_"):
            out[name] = (d.findtext("label"), d.findtext("graphicData/texPath"))
    return out


def default_study_points(tier):
    """The default research points per unit for a tier, from studyPoints<Tier> in the settings defaults."""
    root = ET.parse(ROOT / "Defs" / "SettingsDefaults.xml").getroot()
    return float(next(root.iter("studyPoints" + tier)).text)


def bench_labels():
    """Building defName -> the game's label, read from the installed Data."""
    labels = {}
    for path in (GAME_DATA / "Core" / "Defs" / "ThingDefs_Buildings").glob("*.xml"):
        for d in ET.parse(path).getroot().iter("ThingDef"):
            name = d.findtext("defName")
            if name and d.findtext("label"):
                labels[name] = d.findtext("label")
    return labels


def tiers():
    """The six tiers in Defs order: era name (the scrap's label less 'scrap'), bench labels, points per unit, sprite folder."""
    things, labels = thing_defs(), bench_labels()
    out = []
    for t in ET.parse(ROOT / "Defs" / "ScrapTiers.xml").getroot().iter("LootScavenging.ScrapTierDef"):
        label, tex = things[t.findtext("scrap")]
        pts = default_study_points(t.findtext("tier"))
        benches = [labels[li.text] for li in t.findall("benches/li")]
        out.append({"era": label.replace(" scrap", ""), "tier": t.findtext("tier"), "benches": benches, "points": pts, "tex": tex})
    return out


# ---------- era tiles (UI Rethemed - Era Looks) ----------

# Each tier's palette and pattern defNames in Era Looks' own era comments: Neolithic rock art + fieldstone, medieval tapestry +
# cut stone, industrial engraving + tread plate, spacer, ultratech and archotech with their own patterns.
ERA_LOOK = {
    "Neolithic": ("EraLooks_RockArt", "EraLooks_Scree"),
    "Medieval": ("EraLooks_Tapestry", "EraLooks_CutStone"),
    "Industrial": ("EraLooks_Engraving", "EraLooks_TreadPlate"),
    "Spacer": ("EraLooks_Spacer", "EraLooks_HullPanels"),
    "Ultra": ("EraLooks_Ultratech", "EraLooks_Honeycomb"),
    "Archotech": ("EraLooks_Archotech", "EraLooks_EtchedCircuits"),
}


def era_look(tier):
    palette_name, pattern_name = ERA_LOOK[tier]
    palettes, patterns = {}, {}
    for d in ET.parse(ERA_LOOKS / "Defs" / "EraLooks.xml").getroot():
        if d.tag.endswith("ThemePaletteDef") and d.findtext("defName") == palette_name:
            palettes[palette_name] = {k: d.findtext(k) for k in ("window", "panel", "card", "green", "titleAccent")}
        if d.tag.endswith("PatternDef") and d.findtext("defName") == pattern_name:
            patterns[pattern_name] = (d.findtext("texPath"), float(d.findtext("scale") or 0.5))
    return palettes[palette_name], patterns[pattern_name]


def rgb(hexstr, alpha=255):
    return tuple(int(hexstr[i:i + 2], 16) for i in (0, 2, 4)) + (alpha,)


def era_tile(size, tier):
    """A tile in the era's Era Looks palette: the panel colour with the era's pattern tiled over it in the accent colour."""
    palette, (tex, scale) = era_look(tier)
    w, h = size
    tile = Image.new("RGBA", size, rgb(palette["panel"]))
    pattern = Image.open(ERA_LOOKS / "Textures" / f"{tex}.png").convert("RGBA")
    pw = max(32, round(pattern.width * scale * 0.9))
    pattern = pattern.resize((pw, round(pattern.height * pw / pattern.width)), Image.LANCZOS)
    # The pattern PNGs are white with an opacity mask; Core tints them with the palette's accent.
    mask = pattern.getchannel("A").point(lambda a: int(a * 0.22))
    tinted = Image.new("RGBA", pattern.size, rgb(palette["green"]))
    tinted.putalpha(mask)
    layer = Image.new("RGBA", size, (0, 0, 0, 0))
    for y in range(0, h, tinted.height):
        for x in range(0, w, tinted.width):
            layer.alpha_composite(tinted, (x, y))
    tile.alpha_composite(layer)
    # A darker wash down the left, where the sprite sits, keeps its edges readable.
    wash = Image.new("RGBA", size, (0, 0, 0, 0))
    d = ImageDraw.Draw(wash)
    for x in range(w):
        d.line([(x, 0), (x, h)], fill=(0, 0, 0, int(90 * max(0, 1 - x / (w * 0.7)))))
    tile.alpha_composite(wash)
    return tile, palette


def fitted(size, text, width):
    """RimWordFont at `size`, shrunk until `text` fits `width` (the face is wide)."""
    while size > 12 and font(False, size).getlength(text.lower()) > width:
        size -= 1
    return font(False, size)


def backdrop(frame=None):
    img = Image.new("RGBA", CANVAS, BG)
    if frame is not None:
        back = frame.convert("RGBA").resize(CANVAS, Image.LANCZOS).filter(ImageFilter.GaussianBlur(16))
        img.alpha_composite(back)
        img.alpha_composite(Image.new("RGBA", CANVAS, (0, 0, 0, 150)))
    return img


def header(img, sub):
    title(img, y=62)
    shadowed(img, (CANVAS[0] / 2, 132), sub, font(True, 40), fill=ps.CREAM)


def save(img, name):
    OUT.mkdir(parents=True, exist_ok=True)
    path = OUT / name
    img.convert("RGB").save(path, optimize=True)
    print(f"{path} {path.stat().st_size:,} bytes")


# ---------- scrap-tiers ----------

def scrap_tiers():
    img = backdrop()
    header(img, "Six scrap tiers, one for each era of gear")
    cols, rows, gap, margin = 3, 2, 30, 60
    tw = (CANVAS[0] - 2 * margin - (cols - 1) * gap) // cols
    top, caption_y = 180, 960
    th = (caption_y - 40 - top - (rows - 1) * gap) // rows
    for i, t in enumerate(tiers()):
        x = margin + (i % cols) * (tw + gap)
        y = top + (i // cols) * (th + gap)
        tile, palette = era_tile((tw, th), t["tier"])
        accent = rgb(palette["green"])
        shadow = Image.new("RGBA", CANVAS, (0, 0, 0, 0))
        ImageDraw.Draw(shadow).rectangle([x + 6, y + 6, x + tw + 6, y + th + 6], fill=(0, 0, 0, 180))
        img.alpha_composite(shadow.filter(ImageFilter.GaussianBlur(8)))
        img.alpha_composite(tile, (x, y))
        ImageDraw.Draw(img).rectangle([x, y, x + tw - 1, y + th - 1], outline=accent[:3] + (200,), width=2)

        shadowed(img, (x + 24, y + 22), t["era"], font(True, 46), anchor="la", fill=accent)
        sprite = Image.open(ROOT / "Textures" / (t["tex"] + "/Scrap_" + t["tier"].replace("Ultra", "Ultratech") + "_c.png")).convert("RGBA")
        sprite = sprite.resize((220, 220), Image.LANCZOS)
        img.alpha_composite(sprite, (x + 14, y + 100))

        cx = x + 245
        room = tw - 245 - 16
        shadowed(img, (cx, y + 100), "recycled at", fitted(22, "recycled at", room), anchor="la", fill=MUTED, blur=2, offset=1)
        for j, bench in enumerate(t["benches"]):
            shadowed(img, (cx, y + 132 + j * 36), bench, fitted(29, bench, room), anchor="la", blur=2, offset=1)
        points = f"{t['points']:g}"
        shadowed(img, (cx, y + th - 138), points, font(True, 62), anchor="la", fill=accent)
        label = "default study points per unit"
        shadowed(img, (cx, y + th - 34), label, fitted(22, label, room), anchor="ls", fill=MUTED, blur=2, offset=1)

    shadowed(img, (CANVAS[0] / 2, caption_y), "A bench recycles its own tier and every tier below it", font(True, 36), fill=ps.CREAM)
    shadowed(img, (CANVAS[0] / 2, caption_y + 52), "Animal-era gear becomes neolithic scrap", font(False, 26), fill=MUTED, blur=2, offset=1)
    save(img, "gallery-scrap-tiers.png")


# ---------- stills from the clips run ----------

def still(run, name, after=1.6):
    """A full-size frame from the clips video, `after` seconds past the shot's mark."""
    WORK.mkdir(parents=True, exist_ok=True)
    origin = json.loads((run / "recording.json").read_text())["videoOriginMonotonicSeconds"]
    marks = {m["name"]: m["t"] for m in json.loads((run / "shots.json").read_text())["marks"]}
    path = WORK / f"{name}.png"
    subprocess.run([str(ps.FFMPEG), "-v", "error", "-y", "-ss", f"{marks[name] - origin + after:.2f}", "-i", str(run / "build.mp4"),
                    "-frames:v", "1", str(path)], check=True)
    return Image.open(path).convert("RGBA")


def shot_of(run, name):
    return next(s for s in json.loads((run / "layout.json").read_text())["shots"] if s["name"] == name)


def outlined(img, box, colour=(235, 225, 200, 200)):
    ImageDraw.Draw(img).rectangle([box[0], box[1], box[0] + box[2] - 1, box[1] + box[3] - 1], outline=colour, width=2)


# The inspect pane sits bottom-left of a 2560x1440 frame. The Architect tooltip below it hides the pane's lower lines, so the
# crop stops above it.
PANE_BOX = (0, 1140, 646, 1302)


def pane(frame):
    return frame.crop(PANE_BOX)


# ---------- loot-styles ----------

def cropped(img, frame, centre, src_h, box, caption, band=52):
    """framed() for a run without layout.json: crops `src_h` px of the 2560x1440 frame around `centre` (px) to fill `box`."""
    x, y, w, h = box
    sw = src_h * w / h
    cx, cy = centre
    crop = frame.crop([round(v) for v in (cx - sw / 2, cy - src_h / 2, cx + sw / 2, cy + src_h / 2)]).resize((w, h), Image.LANCZOS)
    shadow = Image.new("RGBA", CANVAS, (0, 0, 0, 0))
    ImageDraw.Draw(shadow).rectangle([x + 6, y + 6, x + w + 6, y + h + 6], fill=(0, 0, 0, 180))
    img.alpha_composite(shadow.filter(ImageFilter.GaussianBlur(8)))
    img.alpha_composite(crop, (x, y))
    img.alpha_composite(Image.new("RGBA", (w, band), (0, 0, 0, 150)), (x, y + h - band))
    outlined(img, box)
    size = 26
    while size > 14 and font(True, size).getlength(caption.lower()) > w - 32:
        size -= 1
    shadowed(img, (x + w / 2, y + h - band / 2), caption, font(True, size), fill=ps.CREAM)


# The same kind of tribal warrior (club, an autopistol in his pack) killed under four sets of rules, from the promo run's
# 0.5.2 drop-mode beats: each still is taken `after` seconds past the death or selection mark, past the picture's 0.55 s lag. The
# chips name the 0.6.0 setting that gives the same outcome, as ModHarness.ApplyStyle sets it. Each centre (px in the
# 2560x1440 frame) puts the body and what he dropped right of the inset and between the chip and the caption band; the
# off scene's crop stays west of the map's edge at x 2290.
STYLES = [
    ("drop-scrapoutofreach-dead", 1.8, (1110, 650), "The autopistol is scrap; the club drops whole"),
    ("drop-scrapeverything-dead", 1.8, (1260, 530), "Everything he carried is scrap"),
    ("drop-keeporiginal-selected", 1.8, (1180, 610), "Both drop whole; the autopistol reads unfamiliar"),
    ("drop-off-dead", 1.8, (1340, 640), "Nothing is sorted: both drop whole"),
]


def loot_styles(run):
    text = keyed()
    reach = text["LootScavenging_OnlyBeyondReach"]
    gear = f"{text['LootScavenging_CategoryWeapons']}, {text['LootScavenging_CategoryApparel']}"
    chips = [
        (reach, True),
        (reach, False),
        (f"{text['LootScavenging_WhoHostile']}: {text['LootScavenging_WhoMark']}", True),
        (f"{gear}: {text['LootScavenging_OutcomeKeep']}", False),
    ]
    frames = [still(run, name, after) for name, after, _, _ in STYLES]
    img = backdrop(frames[0])
    header(img, "What a fallen raider drops under four settings")
    # Every still shows gear landing at death, which 0.6.0 does only with the Hostiles box off; the chip says so.
    label = text["LootScavenging_HostilesKeepGear"]
    probe = Image.new("RGBA", CANVAS, (0, 0, 0, 0))
    setting_chip(probe, (0, 0), label, False, 1400, size=22)
    chip_w = probe.getbbox()[2]
    setting_chip(img, ((CANVAS[0] - chip_w) // 2, 168), label, False, 1400, size=22)
    w, h, gap, top = 925, 385, 24, 232
    left = (CANVAS[0] - 2 * w - gap) // 2
    for i, ((name, _, centre, caption), frame, (chip, on)) in enumerate(zip(STYLES, frames, chips)):
        x = left + (i % 2) * (w + gap)
        y = top + (i // 2) * (h + gap)
        cropped(img, frame, centre, 470, (x, y, w, h), caption)
        setting_chip(img, (x + 10, y + 10), chip, on, w - 20)
        if name == "drop-keeporiginal-selected":
            inset = pane(frame)
            inset = inset.resize((round(inset.width * 0.8), round(inset.height * 0.8)), Image.LANCZOS)
            img.alpha_composite(inset, (x + 10, y + h - 52 - inset.height - 8))
            outlined(img, (x + 10, y + h - 52 - inset.height - 8, inset.width, inset.height))
    save(img, "gallery-loot-styles.png")


# ---------- settings ----------

def settings(frame_path):
    frame = Image.open(frame_path).convert("RGBA")
    img = backdrop(frame)
    header(img, "The settings page on the Progression preset")
    # One frame of the page, cut in two: the preset line with its sentence, and the first item rules with theirs. The gap
    # between the bands carries no mark.
    x0, x1 = 666, 1846
    bands = [(135, 342), (862, 1132)]
    scale = (CANVAS[0] - 120) / (x1 - x0)
    y = 190
    for y0, y1 in bands:
        crop = frame.crop((x0, y0, x1, y1))
        crop = crop.resize((round(crop.width * scale), round(crop.height * scale)), Image.LANCZOS)
        x = (CANVAS[0] - crop.width) // 2
        img.alpha_composite(crop, (x, y))
        outlined(img, (x, y, crop.width, crop.height))
        y += crop.height + 22
    save(img, "gallery-settings.png")


# ---------- inspect ----------

INSPECT = [
    ("inspect-neolithic", "Can be studied now"),
    ("inspect-spacer", "Not yet: the colony needs the spacer era"),
]


def inspect(run):
    stills = [still(run, n) for n, _ in INSPECT]
    img = backdrop(stills[0])
    header(img, "Select a scrap stack to see if it can be studied")
    w, h, gap, top = 920, 520, 40, 190
    left = (CANVAS[0] - 2 * w - gap) // 2
    for i, ((name, caption), frame) in enumerate(zip(INSPECT, stills)):
        shot = shot_of(run, name)
        x = left + i * (w + gap)
        ps.framed(img, shot, frame, (shot["x"], shot["z"]), h / 180, (x, top, w, h), caption, 56)
        p = pane(frame)
        p = p.resize((w, round(p.height * w / p.width)), Image.LANCZOS)
        img.alpha_composite(p, (x, top + h + 22))
        outlined(img, (x, top + h + 22, p.width, p.height))
    save(img, "gallery-inspect.png")


def main(argv):
    if not argv or argv[0] not in ("scrap-tiers", "loot-styles", "settings", "inspect"):
        sys.exit(__doc__)
    cmd, rest = argv[0], argv[1:]
    run = Path(rest[0]) if rest else (INSPECT_RUN if cmd == "inspect" else PROMO_RUN if cmd == "loot-styles" else CLIPS_RUN)
    if cmd == "scrap-tiers":
        scrap_tiers()
    elif cmd == "loot-styles":
        loot_styles(run)
    elif cmd == "settings":
        settings(Path(rest[0]) if rest else WORK / "settings-frame.png")
    else:
        inspect(run)


if __name__ == "__main__":
    main(sys.argv[1:])
