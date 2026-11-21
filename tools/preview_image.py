"""Builds About/Preview.png from a frame of the lootscavenging-preview showcase.

    python tools/ls_test.py run lootscavenging-preview --seed 1 --record --frames 1 --quit-on-finish
    ffmpeg -ss 3.5 -i <run>/build.mp4 -frames:v 1 frame.png
    python tools/preview_image.py frame.png

The frame is the 2560x1440 recording at camera size 7.5. The crop and the pile positions below match that layout;
change them together with DevTests/PreviewBatch.cs.
"""
from pathlib import Path
import sys

from PIL import Image, ImageDraw, ImageFilter, ImageFont

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "About" / "Preview.png"
SIZE = (1280, 720)
# Crop off the grass at the top and right edges of the paved area, keeping 16:9.
CROP = (0, 45, 2480, 1440)
# Pile centres in the full frame, neolithic to archotech.
PILES = [(320 + i * 384, 1100) for i in range(6)]
ERAS = ["neolithic", "medieval", "industrial", "spacer", "ultratech", "archotech"]
FONTS = Path("C:/Windows/Fonts")


def to_out(x, y):
    sx = SIZE[0] / (CROP[2] - CROP[0])
    return (x - CROP[0]) * sx, (y - CROP[1]) * sx


def shadowed(base, xy, text, font, anchor, fill=(255, 255, 255, 255), blur=4, offset=2):
    shadow = Image.new("RGBA", base.size, (0, 0, 0, 0))
    ImageDraw.Draw(shadow).text((xy[0] + offset, xy[1] + offset), text, font=font, anchor=anchor, fill=(0, 0, 0, 200))
    base.alpha_composite(shadow.filter(ImageFilter.GaussianBlur(blur)))
    ImageDraw.Draw(base).text(xy, text, font=font, anchor=anchor, fill=fill)


def main(frame):
    img = Image.open(frame).convert("RGBA").crop(CROP).resize(SIZE, Image.LANCZOS)

    title = ImageFont.truetype(str(FONTS / "segoeuib.ttf"), 104)
    tagline = ImageFont.truetype(str(FONTS / "segoeui.ttf"), 44)
    label = ImageFont.truetype(str(FONTS / "segoeuib.ttf"), 26)

    shadowed(img, (SIZE[0] / 2, 120), "Loot Scavenging", title, "mm", blur=6, offset=4)
    shadowed(img, (SIZE[0] / 2, 205), "scrap  \u2022  recycle  \u2022  study", tagline, "mm", fill=(235, 225, 200, 255))
    for (x, y), era in zip(PILES, ERAS):
        ox, oy = to_out(x, y)
        shadowed(img, (ox, oy + 48), era, label, "mm", fill=(240, 240, 240, 255), blur=3)

    img.convert("RGB").save(OUT, optimize=True)
    print(f"{OUT} {OUT.stat().st_size:,} bytes")


if __name__ == "__main__":
    if len(sys.argv) != 2:
        sys.exit(__doc__)
    main(sys.argv[1])
