#!/usr/bin/env python3
"""Tests for tools/screens.py: the manifest, the canonical captures, the sheets and the perceptual diff."""

from __future__ import annotations

import io
import json
import shutil
import subprocess
import sys
import unittest
from contextlib import redirect_stderr, redirect_stdout
from pathlib import Path
from unittest import mock

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import companion  # noqa: E402
import screens  # noqa: E402
from support import fixture_app  # noqa: E402

try:
    from PIL import Image, ImageDraw
except ImportError:
    Image = None
NO_PILLOW = "needs Pillow (the macOS /usr/bin/python3 has it)"


def manifest(root: Path) -> screens.Screens:
    return screens.Screens(companion.load(root))


class ManifestTests(unittest.TestCase):
    def setUp(self):
        self.context = fixture_app()
        self.root = self.context.__enter__()
        self.addCleanup(self.context.__exit__, None, None, None)
        self.screens = manifest(self.root)
        self.mac, self.windows, self.phone = self.screens.platforms

    def test_each_platform_gets_its_shot_by_name_or_by_capture_kind(self):
        self.assertEqual(self.screens.shot("home", self.phone), screens.Shot("today"))
        self.assertEqual(self.screens.shot("settings", self.phone), screens.Shot("settings", (("FIXTURE_TWO", "1"),)))
        self.assertIsNone(self.screens.shot("settings", self.windows), "null: not on that platform")
        self.assertIsNone(self.screens.shot("panel", self.phone), "absent: not on that platform")
        self.assertEqual(self.screens.out, self.root / "build" / "screens")
        self.assertEqual(self.windows.label, "Windows")
        self.assertTrue(self.mac.landscape)
        self.assertFalse(self.phone.landscape, "iOS goes in the portrait row")

    def test_picking_platforms_screens_and_languages(self):
        self.assertEqual(self.screens.pick_platforms(["ios"]), [self.phone])
        self.assertEqual(self.screens.pick_platforms(["windows", "mac"]), [self.mac, self.windows], "manifest order")
        self.assertEqual(self.screens.pick_screens(["settings", "panel"]), ["panel", "settings"])
        self.assertEqual(self.screens.pick_languages(["all"], ["en"]), ["en", "it", "de"])
        self.assertEqual(self.screens.pick_languages(None, ["en"]), ["en"])
        for pick, argument in ((self.screens.pick_platforms, ["linux"]), (self.screens.pick_screens, ["nope"]),
                               (self.screens.pick_languages, ["xx"])):
            with self.subTest(argument=argument), self.assertRaises(screens.Usage):
                pick(argument) if pick != self.screens.pick_languages else pick(argument, [])

    def test_a_simulator_region_for_each_language(self):
        self.assertEqual(screens.region("en"), "en_US")
        self.assertEqual(screens.region("it"), "it_IT")
        self.assertEqual(screens.region("pt-BR"), "pt_BR")
        self.assertEqual(screens.region("zh-Hant"), "zh_TW")
        self.assertEqual(screens.words("en, fr,de"), ["en", "fr", "de"])

    def test_a_simulator_is_made_on_the_newest_runtime_that_runs_the_device(self):
        runtimes = {"runtimes": [
            {"platform": "iOS", "isAvailable": True, "version": "27.0", "identifier": "ios-27-0",
             "supportedDeviceTypes": [{"name": "iPhone 17 Pro", "identifier": "type.iPhone-17-Pro"}]},
            {"platform": "iOS", "isAvailable": True, "version": "27.1", "identifier": "ios-27-1",
             "supportedDeviceTypes": [{"name": "iPhone Duo", "identifier": "type.iPhone-Duo"}]},
        ]}
        devices = {"devices": {"ios-27-1": [{"name": "app screens duo", "udid": "DUO", "isAvailable": True}]}}
        calls = []

        def run(command, **_):
            calls.append(command)
            listing = {"devices": devices, "runtimes": runtimes}.get(command[3])
            return subprocess.CompletedProcess(command, 0, json.dumps(listing) if listing else "NEW\n", "")

        with mock.patch.object(screens.subprocess, "run", run):
            self.assertEqual(screens.simulator("app screens duo", "iPhone Duo"), "DUO", "already made")
            self.assertEqual(screens.simulator("app screens iphone", "iPhone 17 Pro"), "NEW")
            self.assertEqual(calls[-1], ["xcrun", "simctl", "create", "app screens iphone", "iPhone 17 Pro", "ios-27-0"])
            with self.assertRaisesRegex(RuntimeError, "supports iPad Pro"):
                screens.simulator("app screens ipad", "iPad Pro")

    def test_ios_takes_each_language_in_one_launch_and_a_shot_with_its_own_env_in_another(self):
        capture = screens.Capture(self.screens, [self.phone], ["en"], ["home", "settings"], ["light"], 1, False)
        drawn = self.root / "container"
        launches = []

        def run(command, env=None, **_):
            mode = env["SIMCTL_CHILD_DESKLING_SCREENSHOT_MODE"]
            launches.append((mode, env.get("SIMCTL_CHILD_FIXTURE_TWO"), command[-4:]))
            folder = drawn / str(len(launches))
            folder.mkdir(parents=True)
            for theme in ("light", "dark"):
                (folder / f"{mode}-{theme}.png").write_bytes(b"png")
            if mode == "settings":
                (folder / "problems.txt").write_text("settings-light.png: a label ends in …\n", "utf8")
            return subprocess.CompletedProcess(command, 0, f"app.fixture: 42\r\nReady\r\n{folder}\r\n", "")

        with mock.patch.object(screens, "simulator", return_value="UDID"), \
                mock.patch.object(capture, "sh", return_value=0), \
                mock.patch.object(screens.subprocess, "run", run), redirect_stdout(io.StringIO()):
            capture.device(self.phone, self.root / "Fixture.app", "app.fixture")
        self.assertEqual(launches, [("today", None, ["-AppleLanguages", "(en)", "-AppleLocale", "en_US"]),
                                    ("settings", "1", ["-AppleLanguages", "(en)", "-AppleLocale", "en_US"])])
        folder = self.screens.out / "phone" / "en"
        self.assertEqual(sorted(p.name for p in folder.iterdir()), ["home-light.png", "settings-light.png"])
        self.assertEqual(capture.problems, [(2, "en", "settings-light.png: a label ends in …")])
        self.assertEqual(capture.ran, {"phone"})
        self.assertFalse((drawn / "1").exists(), "moved out of the app's container")

    def test_a_platform_that_didnt_run_reports_its_problems_in_one_list(self):
        capture = screens.Capture(self.screens, [self.mac], ["en"], ["panel", "home"], ["light"], 1, False)
        raw = self.root / "raw"
        raw.mkdir()
        (raw / "problems.txt").write_text("home: the text doesn't fit\n", "utf8")
        log = self.root / "run.log"
        log.write_text("Problems (en):\n  ignored, problems.txt wins\n", "utf8")
        capture.problems_from(self.mac, "en", raw, 1, log)
        (raw / "problems.txt").unlink()
        capture.problems_from(self.mac, "it", raw, 1, log)
        log.write_text("crashed\n", "utf8")
        capture.problems_from(self.mac, "de", raw, 2, log)
        capture.collect(self.mac, "en", raw)
        with redirect_stdout(io.StringIO()):
            capture.write_problems(60)
        report = (self.screens.out / "problems.txt").read_text("utf8").splitlines()
        self.assertTrue(report[0].startswith("# screens.py capture: mac in en, 2 screens, light"))
        self.assertEqual(report[1:], [
            "mac de: the run failed (exit 2); see " + str(log),
            "mac en: home: the text doesn't fit",
            "mac en: no panel-light.png for panel",
            "mac en: no home-light.png for home",
            "mac it: ignored, problems.txt wins",
        ])
        # A later run of another platform keeps these; one of the same platform and language replaces them.
        windows = screens.Capture(self.screens, [self.windows, self.mac], ["it"], ["panel"], ["light"], 1, False)
        windows.problem(self.windows, "it", "panel: cut off")
        with redirect_stdout(io.StringIO()):
            windows.write_problems(60)
        report = (self.screens.out / "problems.txt").read_text("utf8").splitlines()
        self.assertEqual([line for line in report if not line.startswith("#")], [
            "mac de: the run failed (exit 2); see " + str(log),
            "mac en: home: the text doesn't fit",
            "mac en: no panel-light.png for panel",
            "mac en: no home-light.png for home",
            "windows it: panel: cut off",
        ])

    def test_usage_mistakes_exit_2(self):
        for args, words in ((["capture", "--platforms", "linux"], "unknown platform"),
                            (["compare", "--themes", "sepia"], "--themes"),
                            (["accept", "../x"], "baseline name")):
            with self.subTest(args=args):
                error = io.StringIO()
                with redirect_stderr(error), redirect_stdout(io.StringIO()):
                    self.assertEqual(screens.main(args), 2)
                self.assertIn(words, error.getvalue())


@unittest.skipIf(Image is None, NO_PILLOW)
class ImageTests(unittest.TestCase):
    def setUp(self):
        self.context = fixture_app()
        self.root = self.context.__enter__()
        self.addCleanup(self.context.__exit__, None, None, None)
        self.screens = manifest(self.root)
        self.mac, self.windows, self.phone = self.screens.platforms

    def shot(self, path: Path, size: tuple[int, int], color, mark=None) -> Path:
        image = Image.new("RGB", size, color)
        if mark:
            ImageDraw.Draw(image).rectangle(mark, fill=(250, 250, 250))
        path.parent.mkdir(parents=True, exist_ok=True)
        image.save(path)
        return path

    def test_windows_shots_are_cropped_to_the_window(self):
        image = Image.new("RGBA", (300, 200), (0, 0, 0, 0))
        ImageDraw.Draw(image).rectangle((20, 10, 219, 159), fill=(200, 30, 30, 255))
        self.assertEqual(screens.crop_to_window(image).size, (200, 150))
        flat = Image.new("RGB", (300, 200), (12, 12, 12))
        ImageDraw.Draw(flat).rectangle((50, 40, 149, 139), fill=(240, 240, 240))
        self.assertEqual(screens.crop_to_window(flat).size, (100, 100))
        whole = Image.new("RGB", (30, 20), (1, 2, 3))
        self.assertIs(screens.crop_to_window(whole), whole, "nothing to crop")

    def test_collect_names_the_shots_canonically(self):
        raw = self.root / "raw"
        self.shot(raw / "today-dark.png", (40, 80), (10, 10, 10))
        capture = screens.Capture(self.screens, [self.phone], ["en"], ["home"], ["dark"], 1, False)
        self.assertEqual(capture.collect(self.phone, "en", raw), 1)
        self.assertTrue((self.screens.out / "phone" / "en" / "home-dark.png").is_file())
        self.assertEqual(capture.problems, [])

    def test_a_sheet_puts_the_desktops_above_the_phones(self):
        out = self.screens.out
        self.shot(out / "mac" / "en" / "home-light.png", (400, 300), (200, 200, 255))
        self.shot(out / "windows" / "en" / "home-light.png", (200, 100), (255, 200, 200))
        self.shot(out / "phone" / "en" / "home-light.png", (100, 200), (200, 255, 200))
        sheet = screens.sheet(self.screens, "home", "en", "light")
        self.assertGreater(sheet.height, screens.TOP_HEIGHT + screens.BOTTOM_HEIGHT)
        # A row is one height: the Mac's 4:3 and Windows' 2:1 side by side at TOP_HEIGHT.
        self.assertGreaterEqual(sheet.width, round(screens.TOP_HEIGHT * 4 / 3) + screens.TOP_HEIGHT * 2)
        self.assertIsNone(screens.sheet(self.screens, "home", "it", "light"), "nothing captured in Italian")
        with redirect_stdout(io.StringIO()):
            self.assertEqual(screens.compare(self.screens, ["en"], ["panel", "home"], ["light"]), 0)
        self.assertTrue((out / "sheets" / "en" / "home-light.jpg").is_file())
        self.assertFalse((out / "sheets" / "en" / "panel-light.jpg").exists(), "no capture of panel")
        page = (out / "index.html").read_text("utf8")
        self.assertIn('"sheets/en/home-light.jpg"', page)

    def test_diff_shows_what_changed_since_the_accepted_run(self):
        out = self.screens.out
        home = self.shot(out / "mac" / "en" / "home-light.png", (160, 120), (30, 30, 30), (10, 10, 60, 40))
        panel = self.shot(out / "mac" / "en" / "panel-light.png", (160, 120), (30, 30, 30), (90, 60, 150, 110))
        with redirect_stdout(io.StringIO()):
            self.assertEqual(screens.accept(self.screens, "accepted"), 0)
            self.shot(home, (160, 120), (30, 30, 30), (10, 10, 61, 40))  # a pixel's difference: the same
            self.shot(panel, (160, 120), (30, 30, 30), (10, 60, 70, 110))  # moved: changed
            self.shot(out / "phone" / "en" / "home-light.png", (60, 120), (0, 0, 0))  # new
            self.assertEqual(screens.diff(self.screens, "accepted", 4), 0)
        changes = json.loads((out / "changes.json").read_text("utf8"))
        self.assertEqual(changes["changed"], ["mac/en/panel-light.png"])
        self.assertEqual(changes["new"], ["phone/en/home-light.png"])
        self.assertEqual(screens.changed_sheets(changes), {("en", "panel"), ("en", "home")})
        shutil.rmtree(out / "phone")
        panel.unlink()
        with redirect_stdout(io.StringIO()):
            screens.diff(self.screens, "accepted", 4)
        self.assertEqual(json.loads((out / "changes.json").read_text("utf8"))["gone"], ["mac/en/panel-light.png"])

    def test_the_hash_ignores_scale_but_not_layout(self):
        a = Image.new("RGB", (320, 240), (0, 0, 0))
        ImageDraw.Draw(a).rectangle((20, 20, 120, 100), fill=(255, 255, 255))
        self.assertLessEqual(screens.distance(screens.dhash(a), screens.dhash(a.resize((160, 120)))), 4)
        b = Image.new("RGB", (320, 240), (0, 0, 0))
        ImageDraw.Draw(b).rectangle((180, 120, 300, 220), fill=(255, 255, 255))
        self.assertGreater(screens.distance(screens.dhash(a), screens.dhash(b)), 20)


if __name__ == "__main__":
    unittest.main()
