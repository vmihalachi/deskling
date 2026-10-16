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
from support import BASH, HERE, NO_SHELL, TOOLS, WINDOWS, fixture_app, search_path, shell  # noqa: E402


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
        # LF on Windows too: sh reads a \r as part of its line.
        path.write_text("#!/bin/sh\n" + body, "utf8", newline="\n")
        path.chmod(0o755)

    def run_script(self, script: str, *args: str, env: dict | None = None, cwd: Path | None = None):
        if script.endswith(".sh") and BASH is None:
            self.skipTest(NO_SHELL)
        environment = {**os.environ, "PATH": search_path(self.bin), **(env or {})}
        return subprocess.run(
            [*shell(TOOLS / script), *args] if script.endswith(".sh") else [sys.executable, str(TOOLS / script), *args],
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
        # xcodebuild "builds" an app that writes its PNGs, one more than asked for, and prints the folder.
        self.fake("xcodebuild", f'''echo "$*" > "{self.log}"
while [ $# -gt 0 ]; do [ "$1" = -derivedDataPath ] && derived="$2"; shift; done
app="$derived/Build/Products/Debug/Fixture.app/Contents/MacOS"
mkdir -p "$app"
cat > "$app/Fixture" <<'APP'
#!/bin/sh
out=$(mktemp -d)
echo "$DESKLING_SCREENSHOT_MODE hidden=$DESKLING_SCREENSHOT_HIDDEN $*" >> "{self.root / 'runs.log'}"
printf png > "$out/panel-light.png"
printf png > "$out/demo-light.png"
echo "$out"
APP
chmod +x "$app/Fixture"
''')
        out = self.root / "shots"
        result = self.run_script("screenshots.sh", "--only", "panel", str(out), "it")
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertIn("-project Fixture.xcodeproj -scheme Fixture -configuration Debug", self.log.read_text())
        self.assertEqual(sorted(p.name for p in out.iterdir()), ["panel-light.png"], "only the shots asked for")
        self.assertIn("1 screenshots in", result.stdout)
        # --only runs hidden unless --front; a full run comes forward; --build-only doesn't run the app.
        self.run_script("screenshots.sh", "--only", "panel", "--front", str(out), "it")
        self.run_script("screenshots.sh", str(out), "it")
        self.run_script("screenshots.sh", "--build-only")
        runs = (self.root / "runs.log").read_text().splitlines()
        self.assertEqual([line.split(" -Apple")[0] for line in runs], ["panel hidden=1", "panel hidden=0", "all hidden=0"])

    def fake_app(self, problems: dict[str, str] | None = None) -> None:
        """xcodebuild "builds" an app that writes a shot and prints its folder; in the languages named in `problems`
        it also writes that problems.txt, as ScreenshotCapture.finish() does."""
        cases = "".join(f'  {lang}) printf "%s\\n" "{text}" > "$out/problems.txt" ;;\n' for lang, text in (problems or {}).items())
        self.fake("xcodebuild", f'''echo build >> "{self.log}"
while [ $# -gt 0 ]; do [ "$1" = -derivedDataPath ] && derived="$2"; shift; done
app="$derived/Build/Products/Debug/Fixture.app/Contents/MacOS"
mkdir -p "$app"
cat > "$app/Fixture" <<'APP'
#!/bin/sh
out=$(mktemp -d)
printf png > "$out/panel-light.png"
case "$4" in  # -AppleLanguages (xx) -AppleLocale xx
{cases}esac
echo "$out"
APP
chmod +x "$app/Fixture"
''')

    def test_screenshots_fail_on_the_problems_the_run_reports(self):
        self.fake_app({"it": "panel-light.png: the content needs 540 pt of height, the window has 520"})
        out = self.root / "shots"
        result = self.run_script("screenshots.sh", str(out), "it")
        self.assertEqual(result.returncode, 1, result.stdout)
        self.assertIn("Problems (it):", result.stderr)
        self.assertIn("the content needs 540 pt", result.stderr)
        self.assertEqual(sorted(p.name for p in out.iterdir()), ["panel-light.png", "problems.txt"],
                         "the shots are still copied, the problems beside them")
        self.assertIn("the content needs 540 pt", (out / "problems.txt").read_text())
        self.assertEqual(self.run_script("screenshots.sh", str(out), "en").returncode, 0)
        self.assertFalse((out / "problems.txt").exists(), "a clean run removes the last run's problems")

    def test_check_layout_runs_every_language_and_names_the_ones_that_fail(self):
        self.fake_app({"de": "panel-light.png: the content needs 540 pt of height, the window has 520"})
        result = self.run_script("check-layout.sh")
        self.assertEqual(result.returncode, 1, result.stdout + result.stderr)
        self.assertEqual([line for line in result.stdout.splitlines() if line.startswith("==")], ["== en", "== it", "== de"])
        self.assertIn("Layout problems in: de", result.stderr)
        self.assertTrue((self.root / "macos" / "build" / "layout-check" / "it" / "panel-light.png").exists())
        self.assertEqual(self.log.read_text().split(), ["build"], "one build for every language")
        self.assertIn("panel-light.png: the content needs 540 pt", result.stderr)
        passing = self.run_script("check-layout.sh", "--only", "panel", "en", "it")
        self.assertEqual(passing.returncode, 0, passing.stderr)
        self.assertIn("Every window fits in: en it", passing.stdout)

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

    def test_verify_all_skips_what_the_app_lacks(self):
        for tool in ("xcrun", "xcodegen", "dotnet"):
            self.fake(tool, "exit 0\n")
        store_line = lambda result: [line for line in result.stdout.splitlines() if "store listing" in line]
        self.assertEqual(store_line(self.run_script("verify.sh", "--all", "--fast")), ["ok    store listing (store/check.py)"])
        (self.root / "store-listing" / "en.md").unlink()
        self.assertEqual(
            store_line(self.run_script("verify.sh", "--all", "--fast")), ["skip  store listing: no store-listing/en.md yet"]
        )
        config = self.root / "companion.json"
        data = json.loads(config.read_text("utf8"))
        del data["store"]
        config.write_text(json.dumps(data), "utf8")
        result = self.run_script("verify.sh", "--all", "--fast")
        self.assertIn("ok    Deskling pins agree", result.stdout, result.stderr)
        self.assertEqual(store_line(result), [], "no store section: --all has no store check to run")

    def git(self, cwd: Path, *args: str) -> str:
        return subprocess.run(
            ["git", "-c", "user.name=t", "-c", "user.email=t@t", "-c", "commit.gpgsign=false", *args],
            cwd=cwd, env=companion.git_env(), capture_output=True, text=True, check=True,
        ).stdout.strip()

    def commit_app(self) -> None:
        """The fixture app as a repository, with deskling/ a checkout of its own on the pinned tag."""
        deskling = self.root / "deskling"
        deskling.mkdir()
        self.git(deskling, "init", "-q")
        self.git(deskling, "commit", "-q", "--allow-empty", "-m", "release")
        self.git(deskling, "tag", "v0.5.0")
        self.git(self.root, "init", "-q")
        (self.root / ".gitignore").write_text("deskling/\nfake-bin/\ncalls.log\n", "utf8")
        self.git(self.root, "add", "-A")
        self.git(self.root, "commit", "-q", "-m", "app")

    @unittest.skipUnless(WINDOWS, "Windows builds the WinUI app itself; elsewhere it's the VM's")
    def test_verify_builds_the_winui_app_itself_on_windows(self):
        self.commit_app()
        self.fake("dotnet", f'echo "$*" >> "{self.log}"\n')
        result = self.run_script("verify.sh", "--", "windows/Fixture.App/Views/HomeView.xaml")
        self.assertIn("ok    WinUI app builds (Fixture.App)", result.stdout, result.stderr)
        self.assertIn("build ", self.log.read_text())

    @unittest.skipIf(WINDOWS, "on Windows verify.sh builds the WinUI app itself and never asks for the Mac's VM")
    def test_verify_builds_the_winui_app_in_the_vm_when_there_is_one(self):
        self.commit_app()
        self.fake("dotnet", "exit 0\n")
        app = "windows/Fixture.App/Views/HomeView.xaml"
        self.fake("prlctl", 'echo "Failed to get VM config: The virtual machine could not be found."; exit 255\n')
        result = self.run_script("verify.sh", "--", app, env={"HOME": str(self.root)})
        self.assertIn("skip  WinUI app: builds on Windows or in the Parallels VM (Parallels has no VM named "
                      "'Windows 11'", result.stdout, result.stderr)
        # With a VM, vm.sh builds it (this fake one runs nothing, so the copy into it fails).
        self.fake("prlctl", f'''echo "$*" >> "{self.log}"
case "$1" in status) echo "VM $2 exist running" ;; exec) [ "$3" = --current-user ] && [ "$4" = cmd ] || exit 1 ;; esac
''')
        result = self.run_script("verify.sh", "--", app, env={"HOME": str(self.root)})
        self.assertIn("FAIL  WinUI app builds in the VM (Fixture.App, x64 + ARM64)", result.stdout, result.stderr)
        self.assertIn("the copy into the VM failed", result.stdout)
        self.assertIn("powershell.exe -NoProfile -NonInteractive -ExecutionPolicy Bypass -EncodedCommand",
                      self.log.read_text())

    def test_verify_reads_the_submodules_under_a_git_hook(self):
        self.commit_app()
        # What git exports to a pre-commit hook in a linked worktree: absolute paths into the app's repository.
        hook = {"GIT_DIR": str(self.root / ".git"), "GIT_INDEX_FILE": str(self.root / ".git" / "index")}
        result = self.run_script("verify.sh", "--fast", "--", "notes.md", env=hook)
        self.assertEqual(result.stdout.strip(), "ok    Deskling pins agree", result.stdout + result.stderr)

    @unittest.skipUnless(sys.platform == "darwin", "verify.sh runs xcodegen on macOS only")
    def test_verify_names_a_group_named_after_the_checkout(self):
        project = self.root / "macos" / "Fixture.xcodeproj" / "project.pbxproj"
        project.parent.mkdir()
        project.write_text("// generated\n", "utf8")
        self.commit_app()
        # XcodeGen's group for a source outside mac.dir, named after the folder the app is checked out in ("app").
        self.fake("xcodegen", f"printf '\\t\\tname = app;\\n' >> '{project}'\n")
        self.fake("xcrun", "exit 0\n")
        spec = self.root / "macos" / "project.yml"
        spec.write_text(spec.read_text("utf8") + "\n", "utf8")
        self.git(self.root, "add", "macos/project.yml")
        result = self.run_script("verify.sh", "--fast", "--staged")
        self.assertIn("FAIL  xcodegen (project.pbxproj in sync)", result.stdout, result.stderr)
        self.assertIn("named after this checkout's folder (app)", result.stdout)

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
        root = Path("/app").resolve()  # the Config resolves its paths: Q:\app on Windows
        plan = fonts.Plan(companion.Config(data, root))
        self.assertEqual(plan.out, root / "macos/SunnySays/Resources/Fonts")
        self.assertEqual(plan.path("Medium"), plan.out / "Fredoka-Medium.ttf")
        self.assertEqual(plan.weights, {"Medium": 500, "SemiBold": 600})
        self.assertEqual(plan.compose["Ș"], ["S", "comma", "below", "Ş"])
        self.assertEqual(plan.license_file, "OFL-Fredoka.txt")
        self.assertEqual(fonts.glyph_name("ț"), "uni021B")
        self.assertIn("for sunnysays", plan.description())
        self.assertIn("no kerning after the space", plan.description())

    def test_more_faces_share_the_license_and_the_checks(self):
        data = json.loads((HERE / "sunnysays.companion.json").read_text("utf8"))
        data["fonts"]["faces"] = [{"family": "FredokaUpright", "font": ["https://example.com/u.ttf", "0" * 64],
                                   "weights": {"Bold": 700}}]
        self.assertEqual(companion.validate(data), [])
        plan = fonts.Plan(companion.Config(data, Path("/app")))
        self.assertEqual([face.family for face in plan.faces], ["Fredoka", "FredokaUpright"])
        self.assertEqual(plan.faces[1].path("Bold"), plan.out / "FredokaUpright-Bold.ttf")
        self.assertEqual(plan.faces[1].font, ("https://example.com/u.ttf", "0" * 64))
        self.assertEqual(plan.weights, {"Medium": 500, "SemiBold": 600})
        self.assertIn("Modified version of FredokaUpright", plan.description("FredokaUpright"))
        data["fonts"]["faces"][0].pop("weights")
        self.assertIn("weights", "\n".join(companion.validate(data)))

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
            (catalog / "AppIcon.appiconset" / "Contents.json").write_text('{"images": []}')
            result = subprocess.run(
                ["swift", str(TOOLS / "dev-icon.swift"), str(catalog), "#123456", "#FFFFFF"], capture_output=True, text=True)
            self.assertEqual(result.returncode, 0, result.stderr)
            self.assertIn("Wrote 2 icons", result.stdout)
            for size in (32, 128):
                stamped = (catalog / "AppIcon-Dev.appiconset" / f"icon_{size}.png").read_bytes()
                self.assertNotEqual(stamped, (catalog / "AppIcon.appiconset" / f"icon_{size}.png").read_bytes())
            # A fresh catalog gets the Dev set made, starting from AppIcon's Contents.json.
            self.assertEqual((catalog / "AppIcon-Dev.appiconset" / "Contents.json").read_text(), '{"images": []}')


class SharedFilesTests(unittest.TestCase):
    """The tools and skills are shared by every app: none names one, and every skill is well formed."""

    def test_no_app_is_named(self):
        names = re.compile(r"Sunny|sunnysays|Pagliacci|pagliacci|sunflower")
        for path in TOOLS.rglob("*"):
            parts = path.relative_to(TOOLS).parts
            # The landing package's installed dependencies and build output aren't ours.
            if "node_modules" in parts or parts[:2] == ("landing", "dist"):
                continue
            if path.is_file() and "tests" not in parts and "test" not in parts and path.suffix != ".pyc":
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
