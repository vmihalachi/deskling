#!/usr/bin/env python3
"""Composes store screenshots from the captures (`screenshots.store` in companion.json): the Mac App Store's by default,
Google Play's with --canvas.

    python3 laching/tools/store-screenshots.py [--canvas mac|android-phone|android-tablet] [captures-dir] [output-dir] [language]

Defaults: screenshots.out -> <store.out>/<source language> (<store.out>/<canvas>/<language> for an Android canvas),
the source language. Needs Pillow (pip3 install pillow). laching/tools/store-screenshots.sh [language|all] captures
and composes the Mac images of other languages.

Each image is one capture on the paper of its appearance with a headline (`store.font`) and a short note
(`store.noteFont`, else the same face) in the app's colors (`store.palette`: light/dark → paper, ink, accent,
accentText). Captions live in `store.captions`, one list per language in upload order: `[{"shot": capture,
"appearance": "light"|"dark", "headline": [lines], "note": text}, …]`; keep them to what ships and free of health or
productivity claims.

Canvases: `mac` is landscape, the window on the right and the text beside it, drawn for 2880 × 1800 and scaled to
`store.size`; `android-phone` (1080 × 1920, 9:16) stacks the caption above the screen, as the iPhone images do, every
shot of a language reserving its tallest caption's height so the screens line up; `android-tablet` (2560 × 1600, a
10-inch tablet) is the landscape layout. `store.canvases` overrides an Android canvas's size; Google Play takes each
side between 320 and 3840 px and no side more than twice the other, which every Android image is checked against.
"""

import json
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter, ImageFont

sys.path.insert(0, str(Path(__file__).resolve().parent))
import companion  # noqa: E402

ANDROID_CANVASES = {"android-phone": (1080, 1920), "android-tablet": (2560, 1600)}
# Google Play's screenshot limits: each side, and the longer side over the shorter.
PLAY_SIDE = (320, 3840)
PLAY_RATIO = 2

CONFIG = companion.load()
ROOT = CONFIG.root
STORE = CONFIG.require("screenshots.store")
SOURCE = CONFIG.source_language
ARGS = sys.argv[1:]
CANVAS = "mac"
if ARGS[:1] == ["--canvas"]:
    if len(ARGS) < 2 or ARGS[1] not in ("mac", *ANDROID_CANVASES):
        sys.exit(f"store-screenshots: --canvas takes mac, {' or '.join(ANDROID_CANVASES)}")
    CANVAS, ARGS = ARGS[1], ARGS[2:]
SRC = Path(ARGS[0]) if len(ARGS) > 0 else CONFIG.require_path("screenshots.out")
LANGUAGE = ARGS[2] if len(ARGS) > 2 else SOURCE
if len(ARGS) > 1:
    OUT = Path(ARGS[1])
elif CANVAS == "mac":
    OUT = CONFIG.require_path("screenshots.store.out") / LANGUAGE
else:
    OUT = CONFIG.require_path("screenshots.store.out") / CANVAS / LANGUAGE

if CANVAS == "mac":
    W, H = (int(v) for v in STORE.get("size", [2880, 1800]))
else:
    W, H = (int(v) for v in STORE.get("canvases", {}).get(CANVAS, ANDROID_CANVASES[CANVAS]))
PORTRAIT = CANVAS == "android-phone"
UNIT = W / (1080 if PORTRAIT else 2880)  # the layouts' numbers are for a 2880-wide (landscape) or 1080-wide (portrait) canvas
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


def portrait_text(draw, caption):
    """The headline's font (shrunk until its widest line fits the column), the wrapped note and the block's height."""
    column = W - 2 * px(72)
    size = px(72)
    while True:
        display = ImageFont.truetype(str(HEADLINE_FONT), size)
        if max(draw.textlength(line, font=display) for line in caption["headline"]) <= column:
            break
        size -= 2
    note_font = ImageFont.truetype(str(NOTE_FONT), px(40))
    note_lines = wrap(draw, caption["note"], note_font, column)
    leading, note_leading = round(size * 1.12), px(52)
    gap = round(size * 0.2)
    height = len(caption["headline"]) * leading + gap + px(32) + len(note_lines) * note_leading
    return display, leading, note_font, note_lines, note_leading, gap, height


def compose_portrait(caption, text_height):
    """A phone canvas: the headline and note centered at the top, the screen below them, scaled to what is left."""
    colors = PALETTE[caption.get("appearance", "light")]
    canvas = Image.new("RGB", (W, H), colors["paper"])
    draw = ImageDraw.Draw(canvas)
    display, leading, note_font, note_lines, note_leading, gap, height = portrait_text(draw, caption)

    def centered_x(text, font):
        left, _, right, _ = font.getbbox(text)
        return (W - (right - left)) // 2 - left

    top = px(96)
    ty = top + (text_height - height) // 2
    for line in caption["headline"]:
        draw.text((centered_x(line, display), ty), line, font=display, fill=colors["ink"])
        ty += leading
    ty += gap
    draw.rounded_rectangle(((W - px(72)) // 2, ty + px(4), (W + px(72)) // 2, ty + px(12)), px(4), fill=colors["accent"])
    ty += px(32)
    for line in note_lines:
        draw.text((centered_x(line, note_font), ty), line, font=note_font, fill=colors["accentText"])
        ty += note_leading

    # The screen below, with rounded corners and a soft shadow, as the Mac window.
    shot = Image.open(SRC / f"{caption['shot']}-{caption.get('appearance', 'light')}.png").convert("RGB")
    sy = top + text_height + px(48)
    scale = min((H - px(72) - sy) / shot.height, (W - 2 * px(72)) / shot.width, MAX_SCALE)
    shot = shot.resize((round(shot.width * scale), round(shot.height * scale)), Image.LANCZOS)
    x = (W - shot.width) // 2
    radius = px(24)
    shadow = Image.new("L", (W, H), 0)
    ImageDraw.Draw(shadow).rounded_rectangle(
        (x, sy + px(12), x + shot.width, sy + shot.height + px(12)), radius,
        fill=90 if caption.get("appearance") == "dark" else 60,
    )
    shadow = shadow.filter(ImageFilter.GaussianBlur(px(24)))
    canvas.paste(Image.new("RGB", (W, H), "#000000"), (0, 0), shadow)
    canvas.paste(shot, (x, sy), rounded(shot, radius))
    draw.rounded_rectangle((x, sy, x + shot.width - 1, sy + shot.height - 1), radius, outline=colors["ink"] + "33", width=2)
    return canvas


def play_problem(image):
    """What Google Play would refuse about an image's size, or None."""
    short, long = sorted(image.size)
    if not PLAY_SIDE[0] <= short and long <= PLAY_SIDE[1]:
        return f"{image.width}×{image.height}: Google Play takes each side between {PLAY_SIDE[0]} and {PLAY_SIDE[1]} px"
    if long > short * PLAY_RATIO:
        return f"{image.width}×{image.height}: Google Play takes no side more than {PLAY_RATIO}× the other"
    return None


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    if PORTRAIT:
        measure = ImageDraw.Draw(Image.new("RGB", (1, 1)))
        text_height = max(portrait_text(measure, caption)[-1] for caption in CAPTIONS[LANGUAGE])
    for i, caption in enumerate(CAPTIONS[LANGUAGE], 1):
        path = OUT / f"{i}-{caption['shot']}-{caption.get('appearance', 'light')}.png"
        image = compose_portrait(caption, text_height) if PORTRAIT else compose(caption)
        if CANVAS != "mac" and (problem := play_problem(image)):
            sys.exit(f"store-screenshots: {path.name} {problem} (store.canvases)")
        image.save(path, optimize=True)
        print(path.relative_to(ROOT) if path.is_relative_to(ROOT) else path)


if __name__ == "__main__":
    main()
