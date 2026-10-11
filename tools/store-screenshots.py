#!/usr/bin/env python3
"""Composes Mac App Store screenshots from the window captures (`screenshots.store` in companion.json).

    python3 deskling/tools/store-screenshots.py [captures-dir] [output-dir] [language]

Defaults: screenshots.out -> <store.out>/<source language>, the source language. Needs Pillow (pip3 install pillow).
deskling/tools/store-screenshots.sh [language|all] captures and composes other languages.

Each image is one window capture on the paper of its appearance with a headline (`store.font`) and a short note
(`store.noteFont`, else the same face) in the app's colors (`store.palette`: light/dark → paper, ink, accent,
accentText). The layout is drawn for 2880 × 1800 and scales with `store.size`. Captions live in `store.captions`, one
list per language in upload order: `[{"shot": capture, "appearance": "light"|"dark", "headline": [lines], "note":
text}, …]`; keep them to what ships and free of health or productivity claims.
"""

import json
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter, ImageFont

sys.path.insert(0, str(Path(__file__).resolve().parent))
import companion  # noqa: E402

CONFIG = companion.load()
ROOT = CONFIG.root
STORE = CONFIG.require("screenshots.store")
SOURCE = CONFIG.source_language
SRC = Path(sys.argv[1]) if len(sys.argv) > 1 else CONFIG.require_path("screenshots.out")
LANGUAGE = sys.argv[3] if len(sys.argv) > 3 else SOURCE
OUT = Path(sys.argv[2]) if len(sys.argv) > 2 else CONFIG.require_path("screenshots.store.out") / LANGUAGE

W, H = (int(v) for v in STORE.get("size", [2880, 1800]))
UNIT = W / 2880  # the layout's numbers are for a 2880-wide canvas
DEFAULT_PALETTE = {
    "light": {"paper": "#FFFFFF", "ink": "#1D1D1F", "accent": "#0A84FF", "accentText": "#0060DF"},
    "dark": {"paper": "#1C1C1E", "ink": "#F5F5F7", "accent": "#0A84FF", "accentText": "#64B5FF"},
}
PALETTE = {mode: {**DEFAULT_PALETTE[mode], **STORE.get("palette", {}).get(mode, {})} for mode in DEFAULT_PALETTE}
HEADLINE_FONT = CONFIG.require_path("screenshots.store.font")
NOTE_FONT = CONFIG.path("screenshots.store.noteFont") or HEADLINE_FONT
# Captures are at 2x; enlarging past this gets soft.
MAX_SCALE = 1.8

CAPTIONS = json.loads(CONFIG.require_path("screenshots.store.captions").read_text(encoding="utf-8"))


def px(value: float) -> int:
    return round(value * UNIT)


def wrap(draw, text, font, width):
    """Greedy wrap, then balanced so the last line isn't a lone word."""

    def greedy(limit):
        lines, line = [], ""
        for word in text.split():
            trial = f"{line} {word}".strip()
            if draw.textlength(trial, font=font) <= limit or not line:
                line = trial
            else:
                lines.append(line)
                line = word
        return lines + [line]

    count = len(greedy(width))
    lo, hi = 0, width
    while hi - lo > 4:
        mid = (lo + hi) / 2
        lo, hi = (lo, mid) if len(greedy(mid)) == count else (mid, hi)
    return greedy(hi)


def rounded(image, radius):
    mask = Image.new("L", image.size, 0)
    ImageDraw.Draw(mask).rounded_rectangle((0, 0, image.width - 1, image.height - 1), radius, fill=255)
    return mask


def compose(caption):
    colors = PALETTE[caption.get("appearance", "light")]
    canvas = Image.new("RGB", (W, H), colors["paper"])
    draw = ImageDraw.Draw(canvas)

    # The window on the right, with rounded corners and a soft shadow like a macOS window.
    shot = Image.open(SRC / f"{caption['shot']}-{caption.get('appearance', 'light')}.png").convert("RGB")
    scale = min(px(1440) / shot.height, px(1360) / shot.width, MAX_SCALE)
    shot = shot.resize((round(shot.width * scale), round(shot.height * scale)), Image.LANCZOS)
    x, y = W - shot.width - px(200), (H - shot.height) // 2
    radius = px(36)
    shadow = Image.new("L", (W, H), 0)
    ImageDraw.Draw(shadow).rounded_rectangle(
        (x, y + px(24), x + shot.width, y + shot.height + px(24)), radius,
        fill=90 if caption.get("appearance") == "dark" else 60,
    )
    shadow = shadow.filter(ImageFilter.GaussianBlur(px(40)))
    canvas.paste(Image.new("RGB", (W, H), "#000000"), (0, 0), shadow)
    canvas.paste(shot, (x, y), rounded(shot, radius))
    draw.rounded_rectangle((x, y, x + shot.width - 1, y + shot.height - 1), radius, outline=colors["ink"] + "33", width=2)

    # Headline and note in the left column, centered on the window.
    left, column = px(200), x - px(200) - px(140)
    size = px(168)
    while True:
        display = ImageFont.truetype(str(HEADLINE_FONT), size)
        if max(draw.textlength(line, font=display) for line in caption["headline"]) <= column:
            break
        size -= 4
    note_font = ImageFont.truetype(str(NOTE_FONT), px(76))
    leading, note_leading = round(size * 1.12), px(100)
    note_lines = wrap(draw, caption["note"], note_font, column)
    gap = round(size * 0.2)  # clears the descenders of the last headline line
    block = len(caption["headline"]) * leading + gap + px(56) + len(note_lines) * note_leading
    ty = (H - block) // 2
    for line in caption["headline"]:
        draw.text((left - display.getbbox(line)[0], ty), line, font=display, fill=colors["ink"])
        ty += leading
    # A short accent rule between headline and note.
    ty += gap
    draw.rounded_rectangle((left, ty + px(8), left + px(160), ty + px(26)), px(9), fill=colors["accent"])
    ty += px(56)
    for line in note_lines:
        draw.text((left - note_font.getbbox(line)[0], ty), line, font=note_font, fill=colors["accentText"])
        ty += note_leading
    return canvas


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    for i, caption in enumerate(CAPTIONS[LANGUAGE], 1):
        path = OUT / f"{i}-{caption['shot']}-{caption.get('appearance', 'light')}.png"
        compose(caption).save(path, optimize=True)
        print(path.relative_to(ROOT) if path.is_relative_to(ROOT) else path)


if __name__ == "__main__":
    main()
