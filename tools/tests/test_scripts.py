#!/usr/bin/env python3
"""Tests for the shell scripts and the small tools of tools/: conformance.sh, screenshots.sh and install-dev.sh run
against fake `swift` / `xcodebuild` commands over the fixture app; fonts.py's plan comes from companion.json;
store-screenshots.py and dev-icon.swift run when Pillow and a Mac with Swift are around."""

from __future__ import annotations

import json
import os
import re
import shutil
import struct
import subprocess
import sys
import tempfile
import unittest
import zlib
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import companion  # noqa: E402
import fonts  # noqa: E402
from support import HERE, TOOLS, fixture_app  # noqa: E402


def png(path: Path, size: int, rgba=(255, 200, 0, 255)) -> None:
    raw = b"".join(b"\x00" + bytes(rgba) * size for _ in range(size))

    def chunk(kind: bytes, data: bytes) -> bytes:
        return struct.pack(">I", len(data)) + kind + data + struct.pack(">I", zlib.crc32(kind + data) & 0xFFFFFFFF)

    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_bytes(
        b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB", size, size, 8, 6, 0, 0, 0))
        + chunk(b"IDAT", zlib.compress(raw)) + chunk(b"IEND", b""))


class ScriptTests(unittest.TestCase):
    def setUp(self):
        self.app = fixture_app()
        self.root = self.app.__enter__()
        self.bin = self.root / "fake-bin"
        self.bin.mkdir()
        self.log = self.root / "calls.log"

    def tearDown(self):
        self.app.__exit__(None, None, None)

    def fake(self, name: str, body: str) -> None:
        path = self.bin / name
        path.write_text("#!/bin/sh\n" + body, "utf8")
        path.chmod(0o755)

    def run_script(self, script: str, *args: str, env: dict | None = None, cwd: Path | None = None):
        environment = {**os.environ, "PATH": f"{self.bin}:{os.environ['PATH']}", **(env or {})}
        return subprocess.run(
            [str(TOOLS / script), *args] if script.endswith(".sh") else [sys.executable, str(TOOLS / script), *args],
            cwd=cwd or self.root,
            env=environment, capture_output=True, text=True,
        )

    def test_every_shell_script_parses(self):
        for script in TOOLS.glob("*.sh"):
            result = subprocess.run(["bash", "-n", str(script)], capture_output=True, text=True)
            self.assertEqual(result.returncode, 0, f"{script.name}: {result.stderr}")
            self.assertTrue(os.access(script, os.X_OK), f"{script.name} isn't executable")

    def test_conformance_runs_the_apps_generator(self):
        self.fake("swift", f'echo "$FIXTURE_WRITE_CONFORMANCE $*" > "{self.log}"\n')
        result = self.run_script("conformance.sh", cwd=self.root / "macos")
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(
            self.log.read_text().strip(),
            "1 test --package-path macos/FixtureCore --filter ConformanceTests/testWriteVectors",
        )
        self.assertIn("Updated conformance/ from macos/FixtureCore", result.stdout)

    def test_screenshots_build_run_and_copy(self):
        # xcodebuild "builds" an app that writes one PNG per shot it's asked for and prints the folder.
        self.fake("xcodebuild", f'''echo "$*" > "{self.log}"
while [ $# -gt 0 ]; do [ "$1" = -derivedDataPath ] && derived="$2"; shift; done
app="$derived/Build/Products/Debug/Fixture.app/Contents/MacOS"
mkdir -p "$app"
cat > "$app/Fixture" <<'APP'
#!/bin/sh
out=$(mktemp -d)
echo "$DESKLING_SCREENSHOT_MODE $*" > "$out/args.txt"
printf png > "$out/panel-light.png"
echo "$out"
APP
chmod +x "$app/Fixture"
''')
        out = self.root / "shots"
        result = self.run_script("screenshots.sh", "--only", "panel", str(out), "it")
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn("-project Fixture.xcodeproj -scheme Fixture -configuration Debug", self.log.read_text())
        self.assertEqual(sorted(p.name for p in out.iterdir()), ["panel-light.png"])
        self.assertIn("1 screenshots in", result.stdout)

    def test_verify_runs_what_the_paths_need(self):
        result = self.run_script("verify.sh", "--fast", "--", "conformance/phrases.json")
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        self.assertEqual(
            [line.split(None, 1)[1] for line in result.stdout.splitlines() if line.startswith("ok")],
            ["Deskling pins agree", "phrases.py check", "voice.py check"],
        )
        strings = self.run_script("verify.sh", "--fast", "--", "macos/Fixture/Resources/Localizable.xcstrings")
        self.assertEqual(strings.returncode, 1, "the fixture's German has gaps")
        self.assertIn("FAIL  l10n.py check", strings.stdout)
        self.assertIn("de: app 3", strings.stdout)
        quiet = self.run_script("verify.sh", "--", "notes.md")
        self.assertEqual(quiet.returncode, 0, quiet.stdout)
        self.assertEqual(quiet.stdout.strip(), "ok    Deskling pins agree")

    @unittest.skipUnless(sys.platform == "darwin", "ditto and lsregister are macOS tools")
    def test_install_dev_copies_the_debug_build(self):
        built = self.root / "products"
        (built / "Fixture.app" / "Contents").mkdir(parents=True)
        home = self.root / "home"
        env = {"CONFIGURATION": "Debug", "BUILT_PRODUCTS_DIR": str(built), "FULL_PRODUCT_NAME": "Fixture.app",
               "PROJECT_DIR": str(self.root / "macos"), "HOME": str(home), "CI": ""}
        result = self.run_script("install-dev.sh", env=env)
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertTrue((home / "Applications" / "Fixture Dev.app" / "Contents").is_dir())
        release = self.run_script("install-dev.sh", env={**env, "CONFIGURATION": "Release", "HOME": str(self.root / "h2")})
        self.assertEqual(release.returncode, 0)
        self.assertFalse((self.root / "h2").exists(), "Release builds are skipped")


class FontsPlanTests(unittest.TestCase):
    def test_the_plan_comes_from_companion_json(self):
        data = json.loads((HERE / "sunnysays.companion.json").read_text("utf8"))
        plan = fonts.Plan(companion.Config(data, Path("/app")))
        self.assertEqual(plan.out, Path("/app/macos/SunnySays/Resources/Fonts"))
        self.assertEqual(plan.path("Medium"), plan.out / "Fredoka-Medium.ttf")
        self.assertEqual(plan.weights, {"Medium": 500, "SemiBold": 600})
        self.assertEqual(plan.compose["Ș"], ["S", "comma", "below", "Ş"])
        self.assertEqual(plan.license_file, "OFL-Fredoka.txt")
        self.assertEqual(fonts.glyph_name("ț"), "uni021B")
        self.assertIn("for sunnysays", plan.description())
        self.assertIn("no kerning after the space", plan.description())

    def test_no_fonts_section(self):
        with fixture_app():
            with self.assertRaises(SystemExit):
                fonts.Plan(companion.load())


ARIAL = Path("/System/Library/Fonts/Supplemental/Arial.ttf")


class StoreScreenshotTests(unittest.TestCase):
    @unittest.skipUnless(shutil.which("python3") and ARIAL.exists(), "needs a Mac font")
    def test_composes_a_store_image(self):
        try:
            import PIL  # noqa: F401
        except ImportError:
            self.skipTest("needs Pillow (pip3 install pillow)")
        with fixture_app() as root:
            data = json.loads((root / "companion.json").read_text("utf8"))
            data["screenshots"]["store"] = {"captions": "captions.json", "out": "store", "size": [1440, 900], "font": str(ARIAL)}
            (root / "companion.json").write_text(json.dumps(data), "utf8")
            (root / "captions.json").write_text(json.dumps(
                {"en": [{"shot": "panel", "appearance": "dark", "headline": ["Fixy says hi"], "note": "A fixture."}]}), "utf8")
            png(root / "screenshots" / "macos" / "panel-dark.png", 200)
            result = subprocess.run([sys.executable, str(TOOLS / "store-screenshots.py")], cwd=root, capture_output=True, text=True)
            self.assertEqual(result.returncode, 0, result.stderr)
            self.assertTrue((root / "store" / "en" / "1-panel-dark.png").exists())


class DevIconTests(unittest.TestCase):
    @unittest.skipUnless(sys.platform == "darwin" and shutil.which("swift"), "AppKit: macOS only")
    def test_stamps_every_icon(self):
        with tempfile.TemporaryDirectory() as temp:
            catalog = Path(temp) / "Assets.xcassets"
            for size in (32, 128):
                png(catalog / "AppIcon.appiconset" / f"icon_{size}.png", size)
            (catalog / "AppIcon-Dev.appiconset").mkdir()
            result = subprocess.run(
                ["swift", str(TOOLS / "dev-icon.swift"), str(catalog), "#123456", "#FFFFFF"], capture_output=True, text=True)
            self.assertEqual(result.returncode, 0, result.stderr)
            self.assertIn("Wrote 2 icons", result.stdout)
            for size in (32, 128):
                stamped = (catalog / "AppIcon-Dev.appiconset" / f"icon_{size}.png").read_bytes()
                self.assertNotEqual(stamped, (catalog / "AppIcon.appiconset" / f"icon_{size}.png").read_bytes())


class SharedFilesTests(unittest.TestCase):
    """The tools and skills are shared by every app: none names one, and every skill is well formed."""

    def test_no_app_is_named(self):
        names = re.compile(r"Sunny|sunnysays|Pagliacci|pagliacci|sunflower")
        for path in TOOLS.rglob("*"):
            if path.is_file() and "tests" not in path.relative_to(TOOLS).parts and path.suffix != ".pyc":
                self.assertIsNone(names.search(path.read_text("utf8", errors="replace")), path)

    def test_every_skill_has_frontmatter(self):
        skills = sorted(p.parent.name for p in (TOOLS / "skills").glob("*/SKILL.md"))
        self.assertEqual(skills, ["add-phrase", "add-setting", "conformance", "localization", "release-mac",
                                  "review-invariants", "voice", "windows-port"])
        for name in skills:
            text = (TOOLS / "skills" / name / "SKILL.md").read_text("utf8")
            header = text.split("---", 2)[1]
            self.assertIn(f"name: {name}\n", header)
            self.assertRegex(header, r"description: .{80,}")
        self.assertTrue((TOOLS / "skills" / "review-invariants" / "checklist.md").exists())


if __name__ == "__main__":
    unittest.main()
