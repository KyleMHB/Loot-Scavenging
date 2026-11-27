"""Builds a simple title-card preview: the mod's name between two large scrap stacks on a dark ground.

    python tools/preview_icon.py [--row] [out.png]

Writes work/preview/icon-corners.png (or icon-row.png with --row) by default. About/Preview.png is the --row version, copied
from icon-row.png. The Workshop shows the preview as a
small thumbnail, so it carries only the title and two pieces of the mod's own art: neolithic scrap on the left and
archotech scrap on the right, the two ends of the era range.
"""
from pathlib import Path
import sys

from PIL import Image, ImageFilter

from preview_story import font

ROOT = Path(__file__).resolve().parents[1]
SCRAP = ROOT / "Textures" / "Things" / "Item" / "LootScavenging" / "Scrap"
SIZE = (1280, 720)
BACKGROUND = (16, 21, 30, 255)
CREAM = (245, 236, 214, 255)
AMBER = (255, 196, 84, 255)
ICON = 330
MARGIN = 24


ERAS = ["Neolithic", "Medieval", "Industrial", "Spacer", "Ultratech", "Archotech"]


def sprite(era, variant="c", size=ICON):
    img = Image.open(SCRAP / era / f"Scrap_{era}_{variant}.png").convert("RGBA")
    return img.resize((size, size), Image.LANCZOS)


def fitted(text, width, largest):
    """The largest font size, up to `largest`, at which `text` fits in `width` pixels."""
    size = largest
    while font(True, size).getlength(text) > width:
        size -= 2
    return font(True, size)


def glow_text(base, xy, text, fnt, fill, glow=(255, 255, 255, 70), blur=14):
    """Text with a soft halo, like the Lumen Stones title."""
    layer = Image.new("RGBA", base.size, (0, 0, 0, 0))
    from PIL import ImageDraw
    ImageDraw.Draw(layer).text(xy, text, font=fnt, anchor="mm", fill=glow)
    base.alpha_composite(layer.filter(ImageFilter.GaussianBlur(blur)))
    shadow = Image.new("RGBA", base.size, (0, 0, 0, 0))
    ImageDraw.Draw(shadow).text((xy[0] + 4, xy[1] + 6), text, font=fnt, anchor="mm", fill=(0, 0, 0, 200))
    base.alpha_composite(shadow.filter(ImageFilter.GaussianBlur(4)))
    ImageDraw.Draw(base).text(xy, text, font=fnt, anchor="mm", fill=fill)


def main(out, layout="corners"):
    img = Image.new("RGBA", SIZE, BACKGROUND)
    if layout == "row":
        # One stack per era, neolithic to archotech, along the bottom.
        size = 200
        step = (SIZE[0] - 2 * MARGIN) / len(ERAS)
        for i, era in enumerate(ERAS):
            img.alpha_composite(sprite(era, size=size), (round(MARGIN + step * i + (step - size) / 2), SIZE[1] - size - 50))
    else:
        # The scrap sits in the lower corners and the title takes the full width above it.
        top = SIZE[1] - ICON - MARGIN
        img.alpha_composite(sprite("Neolithic"), (MARGIN, top))
        img.alpha_composite(sprite("Archotech"), (SIZE[0] - ICON - MARGIN, top))

    # The font draws a capital O as a star, so the words go in lowercase; it renders every letter as a capital anyway.
    glow_text(img, (SIZE[0] / 2, 150), "Loot", font(True, 190), CREAM)
    glow_text(img, (SIZE[0] / 2, 330), "Scavenging", fitted("Scavenging", SIZE[0] - 120, 190), AMBER, glow=(255, 190, 80, 70))

    out.parent.mkdir(parents=True, exist_ok=True)
    img.convert("RGB").save(out, optimize=True)
    print(f"{out} {out.stat().st_size:,} bytes")


if __name__ == "__main__":
    layout = "row" if "--row" in sys.argv else "corners"
    args = [a for a in sys.argv[1:] if not a.startswith("--")]
    main(Path(args[0]) if args else ROOT / "work" / "preview" / f"icon-{layout}.png", layout)
