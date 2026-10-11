#!/usr/bin/env python3
"""Tests for tools/companion.py: finding the root, the schema, the lookups and the CLI."""

from __future__ import annotations

import io
import json
import os
import sys
import tempfile
import unittest
from contextlib import redirect_stdout
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import companion  # noqa: E402
from support import FIXTURE_APP, HERE, REPO, fixture_app  # noqa: E402

PAGLIACCI = REPO.parent / "pagliacci" / "companion.json"


class SchemaTests(unittest.TestCase):
    def check(self, path: Path) -> list[str]:
        data = json.loads(path.read_text("utf8"))
        return companion.validate(data) + companion.consistency(data)

    def test_the_fixture_app_and_sunnysays_draft_validate(self):
        self.assertEqual(self.check(FIXTURE_APP / "companion.json"), [])
        self.assertEqual(self.check(HERE / "sunnysays.companion.json"), [])

    @unittest.skipUnless(PAGLIACCI.exists(), "../pagliacci isn't checked out next to deskling")
    def test_pagliacci_validates(self):
        self.assertEqual(self.check(PAGLIACCI), [])

    def test_problems_are_named(self):
        data = json.loads((FIXTURE_APP / "companion.json").read_text("utf8"))
        data["mac"]["hotkeySignature"] = "TOOLONG"
        data["characters"] = {"design": "d", "mode": "pixels"}
        data["nonsense"] = 1
        del data["l10n"]
        data["voice"]["defaultVoice"] = "mime"
        problems = companion.validate(data) + companion.consistency(data)
        joined = "\n".join(problems)
        for needle in ("hotkeySignature", "'pixels' is not one of", "unknown key nonsense", "missing l10n", "defaultVoice"):
            self.assertIn(needle, joined)

    def test_nullable_sections(self):
        data = json.loads((FIXTURE_APP / "companion.json").read_text("utf8"))
        self.assertIsNone(data["fonts"])
        data["fonts"] = {"out": "x"}
        self.assertIn("fonts: missing family", "\n".join(companion.validate(data)))


class LookupTests(unittest.TestCase):
    def test_root_from_the_environment_and_from_below(self):
        with fixture_app() as root:
            self.assertEqual(companion.find_root(), root)
            os.environ.pop("COMPANION_ROOT")
            self.assertEqual(companion.find_root(root / "macos" / "Fixture"), root)
            os.environ["COMPANION_ROOT"] = str(root)

    def test_no_root(self):
        with tempfile.TemporaryDirectory() as temp:
            previous = os.environ.pop("COMPANION_ROOT", None)
            try:
                with self.assertRaises(companion.ConfigError):
                    companion.find_root(Path(temp))
            finally:
                if previous:
                    os.environ["COMPANION_ROOT"] = previous

    def test_get_and_path(self):
        with fixture_app() as root:
            config = companion.load()
            self.assertEqual(config.get("mac.scheme"), "Fixture")
            self.assertEqual(config.get("voice.voices"), ["clown"])
            self.assertIsNone(config.get("fonts"))
            self.assertIsNone(config.get("l10n.plist"))
            self.assertEqual(config.get("mac.nothing", "x"), "x")
            self.assertEqual(config.path("l10n.catalog"), root / "macos/Fixture/Resources/Localizable.xcstrings")
            self.assertEqual(config.path("app", base="mac.dir"), None)
            self.assertEqual(config.path("mac.app", base="mac.dir"), root / "macos" / "Fixture")
            self.assertEqual(config.phrases_file, root / "conformance" / "phrases.json")
            self.assertEqual(config.languages, ["en", "it", "de"])
            with self.assertRaises(companion.ConfigError):
                config.require("fonts.out")

    def test_cli(self):
        with fixture_app() as root:
            for argv, expected in (
                (["get", "mac.scheme"], "Fixture"),
                (["get", "languages.all"], '["en", "it", "de"]'),
                (["path", "voice.dir"], str(root / "voices")),
                (["root"], str(root)),
            ):
                out = io.StringIO()
                with redirect_stdout(out):
                    self.assertEqual(companion.main(argv), 0)
                self.assertEqual(out.getvalue().strip(), expected)
            with redirect_stdout(io.StringIO()):
                self.assertEqual(companion.main(["get", "fonts"]), 1)
                self.assertEqual(companion.main(["validate"]), 0)

    def test_an_invalid_file_stops_the_tools(self):
        with fixture_app() as root:
            data = json.loads((root / "companion.json").read_text("utf8"))
            del data["mac"]
            (root / "companion.json").write_text(json.dumps(data), "utf8")
            with self.assertRaises(SystemExit) as stop:
                companion.load()
            self.assertIn("missing mac", str(stop.exception.code))


if __name__ == "__main__":
    unittest.main()
