#!/usr/bin/env python3
"""Tests for tools/character.py, the compiler from character art and rigs to data.

    python3 tools/tests/test_character.py

Small SVGs are written inline; the complete skins the CLI tests need are built in temporary folders, and the
repo's own fixture skins (`Tests/Fixtures/characters/`) are compiled once each.
"""

from __future__ import annotations

import io
import json
import math
import struct
import sys
import tempfile
import unittest
import zlib
from contextlib import redirect_stderr, redirect_stdout
from pathlib import Path

TOOLS = Path(__file__).resolve().parents[1]
FIXTURES = TOOLS.parent / "Tests" / "Fixtures" / "characters"
sys.path.insert(0, str(TOOLS))
sys.path.insert(0, str(TOOLS / "tests"))
import character  # noqa: E402
from support import fixture_app  # noqa: E402

PALETTE = {
    "#FFD25A": "petal",
    "#EBAA2E": "petalEdge",
    "#FFDD55": "petal",
    "#8B5A2B": "seed",
    "#2B1A0E": "eye",
    "#7A6A55": "inkSoft",
    "#000000": "ink",
}
MOODS = character.MOODS
GLYPH = '<g id="idle"><circle cx="9" cy="7" r="4" fill="none" stroke="#000000" stroke-width="1.25"/></g>'


def svg(body: str, view_box: str = "0 0 120 132", extra: str = "") -> str:
    return f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="{view_box}" {extra}>{body}</svg>'


def art(text: str, filename: str = "body.svg"):
    return character.parse_svg(text, filename, PALETTE, mode="art")


def points(shape: dict) -> list[tuple[float, float]]:
    """The on-curve points of a shape's path."""
    out = []
    for command in shape["d"]:
        if command[0] in ("M", "L"):
            out.append((command[1], command[2]))
        elif command[0] == "C":
            out.append((command[5], command[6]))
    return out


def rig(**changes) -> dict:
    moods = {mood: {"eyes": "eyes-open", "mouth": ["mouth-smile"], "droop": 0, "sink": 0, "sway": 0, "blink": False} for mood in MOODS}
    data = {
        "period": 4.0,
        "stillTime": 1.0,
        "always": ["pot", "petals", "face"],
        "head": {"pivot": "neck", "parts": ["petals", "face"]},
        "stem": {"pivot": "rim", "parts": ["pot"], "follow": 0.25},
        "pulse": {"pivot": "neck", "parts": ["petals"]},
        "blink": {"duration": 0.12, "intervals": [4.2], "closedEyes": "eyes-open"},
        "moods": moods,
        "glyph": {mood: ["idle"] for mood in MOODS},
        "bubble": {"pivot": "neck", "tuck": 0.12},
    }
    data.update(changes)
    return data


def png(width: int, height: int) -> bytes:
    """A solid RGBA PNG of the given size."""

    def chunk(kind, data):
        return struct.pack(">I", len(data)) + kind + data + struct.pack(">I", zlib.crc32(kind + data) & 0xFFFFFFFF)

    rows = b"".join(b"\x00" + b"\xff\xd2\x5a\xff" * width for _ in range(height))
    return (
        character.PNG_SIGNATURE
        + chunk(b"IHDR", struct.pack(">IIBBBBB", width, height, 8, 6, 0, 0, 0))
        + chunk(b"IDAT", zlib.compress(rows))
        + chunk(b"IEND", b"")
    )


def write_skin(folder: Path, name: str = "tiny") -> Path:
    skin = folder / name
    skin.mkdir(parents=True)
    (skin / "body.svg").write_text(
        svg(
            '<g id="pot"><rect x="40" y="100" width="40" height="20" fill="#8B5A2B"/></g>'
            '<g id="petals"><circle cx="60" cy="44" r="20" fill="#FFD25A" stroke="#EBAA2E" stroke-width="1.5"/></g>'
            '<g id="face"><circle cx="60" cy="44" r="10" fill="#8B5A2B"/></g>'
            '<g id="cheeks"><circle cx="50" cy="50" r="3" fill="#FFD25A"/></g>'
            '<g id="zz"><circle cx="90" cy="10" r="3" fill="#7A6A55"/></g>'
            '<circle id="pivot-neck" cx="60" cy="68" r="0.5"/><circle id="pivot-rim" cx="60" cy="100" r="0.5"/>'
        )
    )
    (skin / "eyes-open.svg").write_text(svg('<g id="eyes-open"><circle cx="55" cy="42" r="2" fill="#2B1A0E"/></g>'))
    (skin / "mouth-smile.svg").write_text(svg('<g id="mouth-smile"><path d="M56 48Q60 52 64 48" fill="none" stroke="#2B1A0E"/></g>'))
    (skin / "glyph.svg").write_text(svg(GLYPH, "0 0 18 18"))
    (skin / f"{name}.rig.json").write_text(json.dumps(rig()))
    (folder / "palette.json").write_text(json.dumps(PALETTE))
    return skin


def write_raster_skin(folder: Path, name: str = "pixel") -> Path:
    skin = folder / name
    (skin / "layers").mkdir(parents=True)
    layers = {"pot": [40, 100, 40, 20], "petals": [40, 24, 40, 40], "face": [50, 34, 20, 20], "eyes-open": [52, 40, 16, 4], "mouth-smile": [56, 47, 8, 2]}
    for part, frame in layers.items():
        (skin / "layers" / f"{part}@2x.png").write_bytes(png(frame[2] * 2, frame[3] * 2))
    (skin / "layers" / "face@3x.png").write_bytes(png(60, 60))
    (skin / "glyph.svg").write_text(svg(GLYPH, "0 0 18 18"))
    data = rig(
        viewBox=[120, 132],
        pivots={"neck": [60, 68], "rim": [60, 100]},
        layers={part: {"frame": frame} for part, frame in layers.items()},
    )
    (skin / f"{name}.rig.json").write_text(json.dumps(data))
    return skin


def run(*argv) -> tuple[int, str]:
    stderr = io.StringIO()
    with redirect_stdout(io.StringIO()), redirect_stderr(stderr):
        code = character.main([str(a) for a in argv])
    return code, stderr.getvalue()


class ShapesTests(unittest.TestCase):
    def test_circle_becomes_four_cubics(self):
        result = art(svg('<g id="dot"><circle cx="5" cy="7" r="10" fill="#FFD25A"/></g>'))
        shape = result.parts["dot"][0]
        self.assertEqual([c[0] for c in shape["d"]], ["M", "C", "C", "C", "C", "Z"])
        for x, y in points(shape):
            self.assertAlmostEqual(math.hypot(x - 5, y - 7), 10, delta=1e-6)
        self.assertEqual(shape["fill"], "petal")

    def test_nested_transforms_flatten(self):
        result = art(
            svg('<g id="box" transform="translate(10 0)"><g transform="rotate(90 0 0)"><rect width="2" height="1" fill="#FFD25A"/></g></g>')
        )
        corners = points(result.parts["box"][0])
        expected = [(10, 0), (10, 2), (9, 2), (9, 0)]
        self.assertEqual(len(corners), 4)
        for (x, y), (ex, ey) in zip(corners, expected):
            self.assertAlmostEqual(x, ex, delta=1e-9)
            self.assertAlmostEqual(y, ey, delta=1e-9)

    def test_arc_command_becomes_cubics(self):
        result = art(svg('<g id="arc"><path d="M10 20 A 15 15 0 0 1 40 20 Z" fill="#FFD25A"/></g>'))
        shape = result.parts["arc"][0]
        self.assertTrue(set(c[0] for c in shape["d"]) <= {"M", "L", "C", "Z"})
        last_curve = [c for c in shape["d"] if c[0] == "C"][-1]
        self.assertAlmostEqual(last_curve[5], 40, delta=1e-9)
        self.assertAlmostEqual(last_curve[6], 20, delta=1e-9)

    def test_style_attribute_short_and_lowercase_hex(self):
        result = art(svg('<g id="p"><circle cx="0" cy="0" r="3" style="fill:#fd5;stroke:#ebaa2e;stroke-width:2"/></g>'))
        shape = result.parts["p"][0]
        self.assertEqual((shape["fill"], shape["stroke"], shape["strokeWidth"]), ("petal", "petalEdge", 2))

    def test_editor_noise_ignored(self):
        text = svg(
            '<metadata><rdf/></metadata><defs/>'
            '<sodipodi:namedview id="base"/>'
            '<g id="p" inkscape:label="Petals" inkscape:groupmode="layer"><circle cx="0" cy="0" r="3" fill="#FFD25A"/></g>',
            extra='xmlns:sodipodi="http://sodipodi.sourceforge.net/DTD/sodipodi-0.dtd" '
            'xmlns:inkscape="http://www.inkscape.org/namespaces/inkscape"',
        )
        self.assertEqual(len(art(text).parts["p"]), 1)

    def test_unsupported_elements_fail(self):
        cases = {
            "text": '<g id="p"><text x="0" y="0">hi</text></g>',
            "filter": '<defs><filter id="f"/></defs><g id="p"><circle r="1" fill="#FFD25A"/></g>',
            "mask": '<g id="p"><mask id="m"/></g>',
            "linearGradient": '<defs><linearGradient id="g"/></defs><g id="p"><circle r="1" fill="url(#g)"/></g>',
        }
        for element, body in cases.items():
            with self.subTest(element=element):
                with self.assertRaises(character.CompileError) as raised:
                    art(svg(body), filename="parts.svg")
                self.assertIn("parts.svg", str(raised.exception))
                self.assertIn(element if element != "linearGradient" else "gradient", str(raised.exception))

    def test_off_palette_color_fails(self):
        with self.assertRaises(character.CompileError) as raised:
            art(svg('<g id="p"><circle r="1" fill="#123456"/></g>'))
        self.assertIn("#123456", str(raised.exception))

    def test_glyph_must_be_black(self):
        with self.assertRaises(character.CompileError):
            character.parse_svg(svg('<g id="idle"><circle r="4" fill="#FFD25A"/></g>', "0 0 18 18"), "glyph.svg", PALETTE, mode="glyph")
        result = character.parse_svg(
            svg('<g id="idle"><path d="M0 0H18V18H0Z M5 5H13V13H5Z" fill="#000000" fill-rule="evenodd"/></g>', "0 0 18 18"),
            "glyph.svg",
            PALETTE,
            mode="glyph",
        )
        shape = result.parts["idle"][0]
        self.assertEqual((shape["fill"], shape["evenOdd"]), ("ink", True))

    def test_pivots_are_points_not_shapes(self):
        result = art(svg('<g id="p" transform="translate(1 2)"><circle id="pivot-neck" cx="60" cy="68" r="0.5"/></g>'))
        self.assertEqual(result.pivots["neck"], (61, 70))
        self.assertEqual(result.parts.get("p", []), [])


class RigTests(unittest.TestCase):
    parts = {"pot", "petals", "face", "eyes-open", "mouth-smile", "cheeks", "zz"}
    pivots = {"neck", "rim"}
    frames = {"idle"}

    def check(self, data):
        return character.validate_rig(data, self.parts, self.pivots, self.frames, "tiny.rig.json")

    def test_valid_rig_passes_and_fills_defaults(self):
        out = self.check(rig())
        awake = out["moods"]["awake"]
        self.assertEqual((awake["headExtras"], awake["overlays"], awake["swayPeriod"]), ([], [], 4))
        self.assertEqual(out["bubble"], {"pivot": "neck", "tuck": 0.12})

    def test_cheeks_and_zz_are_sugar(self):
        sugar, spelled = rig(), rig()
        sugar["moods"]["asleep"].update(cheeks=True, zz=True)
        spelled["moods"]["asleep"].update(headExtras=["cheeks"], overlays=["zz"])
        self.assertEqual(self.check(sugar), self.check(spelled))
        self.assertEqual(self.check(sugar)["moods"]["asleep"]["headExtras"], ["cheeks"])
        off = rig()
        off["moods"]["awake"].update(cheeks=False, zz=False)
        self.assertEqual(self.check(off)["moods"]["awake"]["overlays"], [])

    def test_rig_validation(self):
        missing_mood = rig()
        del missing_mood["moods"]["asleep"]
        no_glyph = rig()
        no_glyph["glyph"]["talking"] = []
        bad_part = rig()
        bad_part["moods"]["awake"]["eyes"] = "eyes-gone"
        both = rig()
        both["moods"]["awake"].update(cheeks=True, headExtras=["cheeks"])
        bad_overlay = rig()
        bad_overlay["moods"]["asleep"]["overlays"] = ["snore"]
        no_bubble = rig()
        del no_bubble["bubble"]
        no_rate = rig()
        no_rate["moods"]["talking"]["mouth"] = ["mouth-smile", "mouth-smile"]
        rest_gone = rig()
        rest_gone["moods"]["talking"]["restMouth"] = "mouth-gone"
        cases = {
            "missing part": (rig(always=["pot", "leaves"]), "leaves"),
            "missing pivot": (rig(head={"pivot": "hip", "parts": ["face"]}), "hip"),
            "missing mood": (missing_mood, "asleep"),
            "mood without glyph frames": (no_glyph, "talking"),
            "mood part": (bad_part, "eyes-gone"),
            "sugar and spelling": (both, "both"),
            "overlay part": (bad_overlay, "snore"),
            "no bubble": (no_bubble, "bubble"),
            "bubble pivot": (rig(bubble={"pivot": "hat", "tuck": 0.1}), "hat"),
            "bubble tuck": (rig(bubble={"pivot": "neck", "tuck": 1.5}), "tuck"),
            "mouths without a rate": (no_rate, "mouthRate"),
            "rest mouth part": (rest_gone, "mouth-gone"),
        }
        for name, (data, needle) in cases.items():
            with self.subTest(name):
                with self.assertRaises(character.CompileError) as raised:
                    self.check(data)
                self.assertIn(needle, str(raised.exception))

    def test_rest_mouth_is_kept_only_when_given(self):
        data = rig()
        data["moods"]["talking"]["restMouth"] = "mouth-smile"
        out = self.check(data)
        self.assertEqual(out["moods"]["talking"]["restMouth"], "mouth-smile")
        self.assertNotIn("restMouth", out["moods"]["awake"])

    def test_custom_moods(self):
        data = rig()
        data["moods"] = {"idle": data["moods"]["awake"]}
        data["glyph"] = {"idle": ["idle"]}
        out = character.validate_rig(data, self.parts, self.pivots, self.frames, "tiny.rig.json", ["idle"])
        self.assertEqual(list(out["moods"]), ["idle"])


class VectorCommandLineTests(unittest.TestCase):
    def test_compiles_and_check_detects_stale_output(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            write_skin(root / "design")
            out, swift = root / "out" / "tiny.json", root / "swift" / "TinyArt.swift"
            args = ["--design", root / "design", "--skin", "tiny", "--out", out, "--swift-out", swift]
            self.assertEqual(run(*args)[0], 0)
            data = json.loads(out.read_text())
            self.assertEqual(data["viewBox"], [120, 132])
            self.assertEqual(data["glyph"]["size"], 18)
            self.assertEqual(data["pivots"]["neck"], [60, 68])
            self.assertEqual(data["rig"]["bubble"], {"pivot": "neck", "tuck": 0.12})
            self.assertNotIn("cheeks", data["rig"]["moods"]["awake"])
            self.assertEqual(run(*args, "--check")[0], 0)
            self.assertTrue(swift.exists())

            out.write_text(out.read_text().replace('"stillTime": 1', '"stillTime": 2'))
            code, stderr = run(*args, "--check")
            self.assertEqual(code, 1)
            self.assertIn("tiny.json", stderr)

    def test_assets_need_raster_mode(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            write_skin(root / "design")
            with self.assertRaises(SystemExit):
                run("--design", root / "design", "--skin", "tiny", "--out", root / "x.json", "--assets", root / "a")

    def test_raster_keys_in_a_vector_rig_fail(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            skin = write_skin(root / "design")
            (skin / "tiny.rig.json").write_text(json.dumps(rig(layers={})))
            code, stderr = run("--design", root / "design", "--skin", "tiny", "--out", root / "x.json")
            self.assertEqual(code, 2)
            self.assertIn("--mode raster", stderr)

    def test_dump_rounds_and_keeps_paths_on_one_line(self):
        text = character.dump({"b": 1.23456, "a": {"d": [["M", 0.0004, -0.0001], ["Z"]]}})
        self.assertEqual(text, '{\n  "a": {\n    "d": [["M", 0, 0], ["Z"]]\n  },\n  "b": 1.235\n}\n')


class RasterCommandLineTests(unittest.TestCase):
    def compile(self, root: Path, *extra):
        return run("--design", root / "design", "--skin", "pixel", "--mode", "raster", "--out", root / "pixel.json", *extra)

    def test_layers_become_image_parts_and_assets(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            write_raster_skin(root / "design")
            assets, windows = root / "Assets.xcassets", root / "Windows"
            extra = ("--swift-out", root / "PixelArt.swift", "--assets", assets, "--windows-assets", windows)
            self.assertEqual(self.compile(root, *extra), (0, ""))
            data = json.loads((root / "pixel.json").read_text())
            self.assertEqual(data["parts"]["face"], {"image": "pixel.face", "frame": [50, 34, 20, 20]})
            self.assertEqual(data["viewBox"], [120, 132])
            self.assertEqual(data["pivots"]["rim"], [60, 100])
            self.assertNotIn("layers", data["rig"])
            contents = json.loads((assets / "pixel.face.imageset" / "Contents.json").read_text())
            self.assertEqual([i["scale"] for i in contents["images"]], ["2x", "3x"])
            self.assertTrue((assets / "pixel.face.imageset" / "pixel.face@3x.png").exists())
            self.assertFalse((assets / "pixel.pot.imageset" / "pixel.pot@3x.png").exists())
            self.assertTrue((windows / "pixel.face.scale-300.png").exists())
            self.assertEqual((windows / "pixel.pot.scale-200.png").read_bytes(), (root / "design/pixel/layers/pot@2x.png").read_bytes())
            swift = (root / "PixelArt.swift").read_text()
            self.assertIn('"face": .image(asset: "pixel.face", frame: Rect(50, 34, 20, 20)),', swift)
            self.assertNotIn("pixelPart", swift)
            self.assertEqual(self.compile(root, *extra, "--check")[0], 0)

            # --check hashes the PNG copies.
            (root / "design/pixel/layers/face@2x.png").write_bytes(png(40, 40)[:-1] + b"\x00")
            code, stderr = self.compile(root, *extra, "--check")
            self.assertEqual(code, 1)
            self.assertIn("pixel.face@2x.png", stderr)
            self.assertIn("pixel.face.scale-200.png", stderr)

    def test_pixel_size_must_match_the_frame(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            skin = write_raster_skin(root / "design")
            (skin / "layers" / "face@3x.png").write_bytes(png(40, 40))
            code, stderr = self.compile(root)
            self.assertEqual(code, 2)
            self.assertIn("face@3x.png", stderr)
            self.assertIn("60×60", stderr)

    def test_raster_failures(self):
        cases = {
            "missing @2x": (lambda skin: (skin / "layers" / "pot@2x.png").unlink(), "pot@2x.png"),
            "stray layer": (lambda skin: (skin / "layers" / "hat@2x.png").write_bytes(png(2, 2)), "hat@2x.png"),
            "not a png": (lambda skin: (skin / "layers" / "pot@2x.png").write_bytes(b"GIF89a" + bytes(30)), "not a PNG"),
            "an art svg": (lambda skin: (skin / "body.svg").write_text(svg("")), "layers/"),
            "no viewBox": (lambda skin: self.edit_rig(skin, lambda r: r.pop("viewBox")), "viewBox"),
        }
        for name, (break_it, needle) in cases.items():
            with self.subTest(name), tempfile.TemporaryDirectory() as tmp:
                root = Path(tmp)
                break_it(write_raster_skin(root / "design"))
                code, stderr = self.compile(root)
                self.assertEqual(code, 2)
                self.assertIn(needle, stderr)

    @staticmethod
    def edit_rig(skin: Path, change):
        path = skin / f"{skin.name}.rig.json"
        data = json.loads(path.read_text())
        change(data)
        path.write_text(json.dumps(data))


class AppConfigTests(unittest.TestCase):
    """Without --design, the skins come from the app's companion.json."""

    def configure(self, root: Path, **characters):
        path = root / "companion.json"
        data = json.loads(path.read_text())
        data["characters"] = {"design": "design/characters", **characters}
        path.write_text(json.dumps(data))

    def test_every_skin_goes_where_companion_json_says(self):
        with fixture_app() as root:
            write_skin(root / "design" / "characters", "tiny")
            write_skin(root / "design" / "characters", "big-cat")
            self.configure(root, swiftOut="macos/FixtureCore/Sources/FixtureCore/Art", swiftModule="FixtureCore")
            self.assertEqual(run()[0], 0)
            vectors = root / "conformance" / "characters"
            self.assertTrue((vectors / "tiny.json").is_file())
            self.assertTrue((vectors / "big-cat.json").is_file())
            swift = root / "macos/FixtureCore/Sources/FixtureCore/Art/BigCatArt.swift"
            self.assertIn("import DesklingCompanion\n", swift.read_text())
            self.assertIn("public static let bigCat = CharacterArt(", swift.read_text())
            self.assertEqual(run("--check")[0], 0)
            (vectors / "tiny.json").write_text("{}")
            code, stderr = run("--check", "--skin", "tiny")
            self.assertEqual(code, 1)
            self.assertIn("tiny.json is stale", stderr)

    def test_raster_skins_get_their_assets(self):
        with fixture_app() as root:
            write_raster_skin(root / "design" / "characters", "pixel")
            self.configure(root, mode="raster", assets="macos/App/Characters", windowsAssets="windows/App/Characters")
            self.assertEqual(run("--skin", "pixel")[0], 0)
            self.assertTrue((root / "macos/App/Characters/pixel.face.imageset/Contents.json").is_file())
            self.assertTrue((root / "windows/App/Characters/pixel.face.scale-300.png").is_file())

    def test_an_unknown_skin_is_an_error(self):
        with fixture_app() as root:
            write_skin(root / "design" / "characters", "tiny")
            self.configure(root)
            code, stderr = run("--skin", "nope")
            self.assertEqual(code, 2)
            self.assertIn("no skin 'nope'", stderr)


class SwiftOutputTests(unittest.TestCase):
    def test_swift_output_is_stable(self):
        with tempfile.TemporaryDirectory() as tmp:
            data = character.compile_skin(write_skin(Path(tmp)), PALETTE).data
        text = character.swift_source(data, "DesklingCompanion")
        self.assertEqual(text, character.swift_source(data, "DesklingCompanion"))
        self.assertTrue(text.startswith("// Generated by tools/character.py from the tiny art. Do not edit.\n"))
        self.assertIn("// swift-format-ignore-file", text)
        self.assertIn("\nimport DesklingCompanion\n", text)
        self.assertIn("extension CharacterArt {", text)
        self.assertIn("public static let tiny = CharacterArt(", text)
        self.assertIn('skin: "tiny",', text)
        self.assertIn('.awake: MoodRig(eyes: "eyes-open", mouth: ["mouth-smile"], mouthRate: 0, headExtras: [], overlays: []', text)
        self.assertIn('fill: "petal", stroke: "petalEdge", strokeWidth: 1.5)', text)
        self.assertIn('bubble: BubbleAnchor(pivot: "neck", tuck: 0.12)),', text)
        self.assertIn('"face": .shapes(tinyPart', text)
        self.assertIn(".cubic(Point(", text)
        self.assertNotIn("import", character.swift_source(data, ""))

    def test_swift_names(self):
        self.assertEqual(character.swift_name("fixture-vector"), "fixtureVector")
        self.assertEqual(character.swift_name("sunflower"), "sunflower")
        self.assertEqual(character.swift_name("big_cat-2"), "bigCat2")
        with self.assertRaises(character.CompileError):
            character.swift_name("9lives")


class PreviewTests(unittest.TestCase):
    def test_preview_has_every_mood_and_background(self):
        with tempfile.TemporaryDirectory() as tmp:
            compiled = character.compile_skin(write_skin(Path(tmp)), PALETTE)
        compiled.data["rig"]["moods"]["talking"]["mouth"] = ["mouth-smile", "mouth-smile"]
        sheet = character.preview_svg(compiled, PALETTE)
        columns = ["awake", "talking-1", "talking-2", "shushed", "hushedForCall", "hushedFullScreen", "asleep", "offHours"]
        for column in columns:
            for background in ("paper", "night", "busy"):
                self.assertIn(f'id="pose-{column}-{background}"', sheet)
        self.assertIn('id="glyph-idle"', sheet)

    def test_busy_tiles_are_clipped_and_glyphs_follow_mood_order(self):
        with tempfile.TemporaryDirectory() as tmp:
            compiled = character.compile_skin(write_skin(Path(tmp)), PALETTE)
        compiled.data["glyph"]["frames"]["zzz"] = compiled.data["glyph"]["frames"]["idle"]
        compiled.data["rig"]["glyph"]["asleep"] = ["zzz"]
        sheet = character.preview_svg(compiled, PALETTE)
        self.assertIn('clip-path="url(#busy-0)"', sheet)
        self.assertLess(sheet.index('id="glyph-idle"'), sheet.index('id="glyph-zzz"'))

    def test_raster_preview_embeds_the_layers(self):
        with tempfile.TemporaryDirectory() as tmp:
            compiled = character.compile_skin(write_raster_skin(Path(tmp)), {}, mode="raster")
            sheet = character.preview_svg(compiled, {})
        self.assertIn('<image href="data:image/png;base64,', sheet)

    def test_preview_command_writes_the_sheet(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            write_skin(root / "design")
            out = root / "sheet.svg"
            with redirect_stdout(io.StringIO()):
                self.assertEqual(character.main(["preview", str(out), "--design", str(root / "design"), "--skin", "tiny"]), 0)
            self.assertTrue(out.read_text().startswith("<svg"))


class FixtureSkinTests(unittest.TestCase):
    """The repo's fixture skins compile: every mood, sugar and spelled-out extras, and both modes."""

    def test_fixture_vector(self):
        palette = character.load_palette(FIXTURES / "palette.json", required=True)
        data = character.compile_skin(FIXTURES / "fixture-vector", palette).data
        self.assertEqual(data["rig"]["moods"]["asleep"]["overlays"], ["zz"])
        self.assertEqual(data["rig"]["moods"]["awake"]["headExtras"], ["cheeks"])
        self.assertTrue(all(isinstance(part, list) for part in data["parts"].values()))

    def test_fixture_raster(self):
        compiled = character.compile_skin(FIXTURES / "fixture-raster", {}, mode="raster")
        self.assertTrue(all(isinstance(part, dict) for part in compiled.data["parts"].values()))
        self.assertEqual(sorted(compiled.layers["head"]), [2, 3])
        self.assertEqual(sorted(compiled.layers["zz"]), [2])


if __name__ == "__main__":
    unittest.main()
