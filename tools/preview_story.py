"""Builds the three preview candidates from a lootscavenging-preview-story run.

    python tools/ls_test.py run lootscavenging-preview-story --seed 1 --record --frames 1 --quit-on-finish
    python tools/preview_story.py <run folder> [out folder]

Writes story.png, aftermath.png and panels.png (1280x720) to the out folder, work/preview by default. One frame is taken from
the middle of each shot's hold; layout.json gives each shot's camera and the cells the labels point at.
"""
from pathlib import Path
import json
import subprocess
import sys
import tempfile

from PIL import Image, ImageDraw, ImageFilter, ImageFont

ROOT = Path(__file__).resolve().parents[1]
FFMPEG = Path("E:/Coding/Rimworld/RimWorldDevTests/work/ffmpeg/ffmpeg.exe")
GAME = Path("E:/Steam/steamapps/common/RimWorld")
RIMWORLD_FONT = ROOT.parent / "Configurable Quality" / "artifacts" / "steam-page-mockup" / "RimWordFont.ttf"
if not RIMWORLD_FONT.is_file():  # in a git worktree ROOT.parent is not the workspace
    RIMWORLD_FONT = Path("E:/Coding/Rimworld/Configurable Quality/artifacts/steam-page-mockup/RimWordFont.ttf")
SIZE = (1280, 720)
FRAME = (2560, 1440)
CREAM = (235, 225, 200, 255)
WHITE = (245, 245, 245, 255)


def font(bold, size):
    """RimWordFont, the RimWorld-style lettering the other mods' Workshop art uses, for all text. It has one weight, so
    `bold` changes nothing. The file is local and not distributed."""
    return ImageFont.truetype(str(RIMWORLD_FONT), size)


def frames(run, layout, work):
    """The middle frame of each shot's hold, by shot name."""
    out = {}
    for i, shot in enumerate(layout["shots"]):
        path = work / f"{shot['name']}.png"
        t = layout["hold"] * (i + 0.65)
        subprocess.run([str(FFMPEG), "-v", "error", "-y", "-ss", f"{t:.2f}", "-i", str(run / "build.mp4"), "-frames:v", "1", str(path)], check=True)
        out[shot["name"]] = Image.open(path).convert("RGBA")
    return out


def to_screen(shot, x, z, scale):
    """A map position to pixels in the frame scaled by `scale`."""
    ppc = FRAME[1] / (2 * shot["zoom"])
    return ((FRAME[0] / 2 + (x - shot["x"]) * ppc) * scale, (FRAME[1] / 2 - (z - shot["z"]) * ppc) * scale)


def shadowed(base, xy, text, fnt, anchor="mm", fill=WHITE, blur=4, offset=2):
    # RimWordFont draws lowercase as capitals; its capital O is a decorative glyph, so text goes in lowercase.
    text = text.lower()
    shadow = Image.new("RGBA", base.size, (0, 0, 0, 0))
    ImageDraw.Draw(shadow).text((xy[0] + offset, xy[1] + offset), text, font=fnt, anchor=anchor, fill=(0, 0, 0, 220))
    base.alpha_composite(shadow.filter(ImageFilter.GaussianBlur(blur)))
    ImageDraw.Draw(base).text(xy, text, font=fnt, anchor=anchor, fill=fill)


def arrow(base, start, end, width=6, head=20, fill=CREAM):
    """A straight arrow with a soft shadow, stopping short of both ends."""
    (x0, y0), (x1, y1) = start, end
    length = ((x1 - x0) ** 2 + (y1 - y0) ** 2) ** 0.5
    ux, uy = (x1 - x0) / length, (y1 - y0) / length
    tip = (x1, y1)
    back = (x1 - ux * head, y1 - uy * head)
    left = (back[0] - uy * head * 0.6, back[1] + ux * head * 0.6)
    right = (back[0] + uy * head * 0.6, back[1] - ux * head * 0.6)
    layer = Image.new("RGBA", base.size, (0, 0, 0, 0))
    shadow = Image.new("RGBA", base.size, (0, 0, 0, 0))
    for d, img, colour in ((3, shadow, (0, 0, 0, 200)), (0, layer, fill)):
        draw = ImageDraw.Draw(img)
        draw.line([(x0 + d, y0 + d), (back[0] + d, back[1] + d)], fill=colour, width=width)
        draw.polygon([(p[0] + d, p[1] + d) for p in (tip, left, right)], fill=colour)
    base.alpha_composite(shadow.filter(ImageFilter.GaussianBlur(3)))
    base.alpha_composite(layer)


def top_shade(img, height=300, strength=150):
    """Darkens the top of the image so the title reads over bright ground."""
    shade = Image.new("RGBA", img.size, (0, 0, 0, 0))
    draw = ImageDraw.Draw(shade)
    for y in range(height):
        draw.line([(0, y), (img.size[0], y)], fill=(0, 0, 0, int(strength * (1 - y / height) ** 1.5)))
    img.alpha_composite(shade)


def title(img, y=110):
    shadowed(img, (SIZE[0] / 2, y), "Loot Scavenging", font(True, 76), blur=6, offset=4)


def story(layout, shots):
    shot = next(s for s in layout["shots"] if s["name"] == "story")
    img = shots["story"].resize(SIZE, Image.LANCZOS)
    pts = {p["name"]: to_screen(shot, p["x"], p["z"], SIZE[0] / FRAME[0]) for p in layout["points"]}
    top_shade(img)
    title(img)
    label = font(True, 28)
    bx, by = pts["bodies"]
    hx, hy = pts["hauler"]
    rx, ry = pts["recycle"]
    sx, sy = pts["study"]
    shadowed(img, (bx + 10, by + 150), "raiders, downed or dead", label)
    arrow(img, (bx + 70, by + 10), (hx - 50, hy - 5))
    shadowed(img, (hx, hy + 70), "their gear becomes scrap", label, fill=CREAM)
    arrow(img, (hx + 45, hy - 25), (rx - 95, ry + 5))
    arrow(img, (hx + 45, hy + 15), (sx - 95, sy + 5))
    shadowed(img, (rx + 10, ry - 60), "recycle into materials", label)
    shadowed(img, (sx + 10, sy + 115), "study for research", label)
    return img


def aftermath(shots):
    img = shots["aftermath"].resize(SIZE, Image.LANCZOS)
    top_shade(img, 320, 170)
    title(img)
    shade = Image.new("RGBA", img.size, (0, 0, 0, 0))
    draw = ImageDraw.Draw(shade)
    for y in range(160):
        draw.line([(0, SIZE[1] - 1 - y), (SIZE[0], SIZE[1] - 1 - y)], fill=(0, 0, 0, int(150 * (1 - y / 160) ** 1.5)))
    img.alpha_composite(shade)
    shadowed(img, (SIZE[0] / 2, SIZE[1] - 50), "Raiders' gear becomes scrap of its era, to recycle or study", font(True, 32))
    return img


def panels(layout, shots):
    img = Image.new("RGBA", SIZE, (38, 36, 33, 255))
    # A blurred, darkened backdrop from the story shot keeps the game's colours behind the panels.
    back = shots["story"].resize(SIZE, Image.LANCZOS).filter(ImageFilter.GaussianBlur(14))
    img.alpha_composite(back)
    img.alpha_composite(Image.new("RGBA", SIZE, (0, 0, 0, 120)))
    title(img, y=125)
    w, h, gap, top = 380, 430, 26, 250
    left = (SIZE[0] - 3 * w - 2 * gap) / 2
    captions = ["1  Raiders drop scrap", "2  Recycle it", "3  Study it"]
    pts = {p["name"]: p for p in layout["points"]}
    # Each panel is cropped around its subject: the bodies, the machining table, the research bench and its researcher.
    subjects = [("close-bodies", "bodies", 0.6, 6.4), ("close-recycle", "recycle", 1.3, 5.0), ("close-study", "study", 1.3, 5.4)]
    for i, ((name, point, dx, height), caption) in enumerate(zip(subjects, captions)):
        shot = next(s for s in layout["shots"] if s["name"] == name)
        p = pts[point]
        ch = height * FRAME[1] / (2 * shot["zoom"])
        cw = ch * w / h
        cx, cy = to_screen(shot, p["x"] + dx, p["z"] - (0.0 if point == "bodies" else 0.6), 1.0)
        box = [round(v) for v in (cx - cw / 2, cy - ch / 2, cx + cw / 2, cy + ch / 2)]
        crop = shots[name].crop(box).resize((w, h), Image.LANCZOS)
        x = round(left + i * (w + gap))
        shadow = Image.new("RGBA", SIZE, (0, 0, 0, 0))
        ImageDraw.Draw(shadow).rectangle([x + 6, top + 6, x + w + 6, top + h + 6], fill=(0, 0, 0, 180))
        img.alpha_composite(shadow.filter(ImageFilter.GaussianBlur(8)))
        img.alpha_composite(crop, (x, top))
        band = Image.new("RGBA", (w, 64), (0, 0, 0, 150))
        img.alpha_composite(band, (x, top + h - 64))
        ImageDraw.Draw(img).rectangle([x, top, x + w - 1, top + h - 1], outline=(235, 225, 200, 200), width=2)
        shadowed(img, (x + w / 2, top + h - 32), caption, font(True, 30), fill=CREAM)
    return img


def switch(on, height):
    """The on/off switch from the Lumen Stones settings slide ("Enable coolers"): a 152x66 pill, blue with a dark "ON" and
    the knob right when on; the slide's dark track colour with the knob left when off. Drawn at 4x for smooth edges."""
    k = 4
    h = height * k
    w = round(h * 152 / 66)
    icon = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    draw = ImageDraw.Draw(icon)
    draw.rounded_rectangle([0, 0, w - 1, h - 1], radius=h // 2, fill="#8ed5ff" if on else "#40545d")
    inset = round(h * 8 / 66)
    knob = h - 2 * inset
    kx = w - inset - knob if on else inset
    draw.ellipse([kx, inset, kx + knob, inset + knob], fill="#f5eedc")
    label = font(False, round(h * 0.42))
    lx = (inset * 2.5) if on else (w - inset * 2.5)
    draw.text((lx, h / 2), "on" if on else "off", font=label, anchor="lm" if on else "rm", fill="#10232f" if on else "#f5eedc")
    return icon.resize((w // k, height), Image.LANCZOS)


def setting_chip(img, xy, text, on, max_width, size=17):
    """A dark chip with a setting's label and its switch. The label shrinks until the chip fits `max_width`."""
    text = text.lower()
    icon = switch(on, size + 9)
    pad, gap = 10, 12
    while size > 11 and font(False, size).getlength(text) + icon.width + pad * 2 + gap > max_width:
        size -= 1
    fnt = font(False, size)
    w = round(pad + fnt.getlength(text) + gap + icon.width + pad)
    h = icon.height + 2 * 6
    chip = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    ImageDraw.Draw(chip).rounded_rectangle([0, 0, w - 1, h - 1], radius=8, fill=(20, 20, 20, 205), outline=(235, 225, 200, 120))
    ImageDraw.Draw(chip).text((pad, h / 2), text, font=fnt, anchor="lm", fill=WHITE)
    chip.alpha_composite(icon, (w - pad - icon.width, 6))
    img.alpha_composite(chip, xy)


def framed(img, shot, frame, centre, height, box, caption, band=52):
    """Crops `height` cells around `centre` (map x, z) from a shot to fill `box` (x, y, w, h), with a frame and caption band."""
    x, y, w, h = box
    ch = height * FRAME[1] / (2 * shot["zoom"])
    cw = ch * w / h
    cx, cy = to_screen(shot, centre[0], centre[1], 1.0)
    crop = frame.crop([round(v) for v in (cx - cw / 2, cy - ch / 2, cx + cw / 2, cy + ch / 2)]).resize((w, h), Image.LANCZOS)
    shadow = Image.new("RGBA", SIZE, (0, 0, 0, 0))
    ImageDraw.Draw(shadow).rectangle([x + 6, y + 6, x + w + 6, y + h + 6], fill=(0, 0, 0, 180))
    img.alpha_composite(shadow.filter(ImageFilter.GaussianBlur(8)))
    img.alpha_composite(crop, (x, y))
    img.alpha_composite(Image.new("RGBA", (w, band), (0, 0, 0, 150)), (x, y + h - band))
    ImageDraw.Draw(img).rectangle([x, y, x + w - 1, y + h - 1], outline=(235, 225, 200, 200), width=2)
    # RimWordFont is wide capitals; the caption shrinks until it fits the panel.
    size = 26
    while size > 14 and font(True, size).getlength(caption.lower()) > w - 32:
        size -= 1
    shadowed(img, (x + w / 2, y + h - band / 2), caption, font(True, size), fill=CREAM)


def split(layout, shots):
    """Left: the same downed raider with his gear, then with scrap in its place. Right: that scrap at the research bench and
    the machining table."""
    img = Image.new("RGBA", SIZE, (38, 36, 33, 255))
    img.alpha_composite(shots["story"].resize(SIZE, Image.LANCZOS).filter(ImageFilter.GaussianBlur(14)))
    img.alpha_composite(Image.new("RGBA", SIZE, (0, 0, 0, 120)))
    title(img, y=82)
    s = {sh["name"]: sh for sh in layout["shots"]}
    p = {pt["name"]: (pt["x"], pt["z"]) for pt in layout["points"]}
    top, bottom, margin, gap = 160, 690, 40, 24
    lw, lgap, band = 540, 14, 40
    lh = (bottom - top - 2 * lgap) // 3
    # The pirate and the row of gear east of him sit below the setting chip and above the caption band. At the filmed zoom a
    # panel this wide shows at most about 3.3 cells top to bottom, and can shift at most 1.7 cells sideways, before the crop
    # runs off the frame.
    drop = (p["drop"][0] + 0.5, p["drop"][1] + 0.1)
    # Each panel shows the setting behind it as a switch, as on the Lumen Stones settings slide.
    left = [
        ("drop-gear", "Scrap raider gear", False, "Raiders drop their gear"),
        ("drop-scrap", "Scrap raider gear", True, "They drop scrap"),
        ("drop-body", "Keep loot on the body", True, "Or it stays on the corpse"),
    ]
    for i, (name, setting, on, caption) in enumerate(left):
        y = top + i * (lh + lgap)
        framed(img, s[name], shots[name], drop, 3.3, (margin, y, lw, lh), caption, band)
        setting_chip(img, (margin + 10, y + 10), setting, on, lw - 20)
    rx = margin + lw + gap
    framed(img, s["workshop"], shots["workshop"], (s["workshop"]["x"], s["workshop"]["z"]), 9.2, (rx, top, SIZE[0] - margin - rx, bottom - top),
           "Study it or recycle it")
    return img


def main(run, out):
    layout = json.loads((run / "layout.json").read_text())
    out.mkdir(parents=True, exist_ok=True)
    with tempfile.TemporaryDirectory() as tmp:
        shots = frames(run, layout, Path(tmp))
        for name, img in (("story", story(layout, shots)), ("aftermath", aftermath(shots)), ("panels", panels(layout, shots)), ("split", split(layout, shots))):
            path = out / f"{name}.png"
            img.convert("RGB").save(path, optimize=True)
            print(f"{path} {path.stat().st_size:,} bytes")


if __name__ == "__main__":
    if len(sys.argv) not in (2, 3):
        sys.exit(__doc__)
    main(Path(sys.argv[1]), Path(sys.argv[2]) if len(sys.argv) == 3 else ROOT / "work" / "preview")
