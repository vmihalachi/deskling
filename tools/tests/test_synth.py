#!/usr/bin/env python3
"""Tests for tools/synth.py: the engine renders sunnysays' five recipes (copied into the fixture app) to exactly the
WAV files sunnysays ships, compared by hash (and byte for byte when ../sunnysays is checked out)."""

from __future__ import annotations

import hashlib
import io
import sys
import unittest
from contextlib import redirect_stdout
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import synth  # noqa: E402
from support import FIXTURE_APP, REPO, fixture_app  # noqa: E402

SUNNYSAYS_SOUNDS = REPO.parent / "sunnysays" / "macos" / "SunnySays" / "Resources" / "Sounds"
# sha256 of sunnysays' committed files (macos/SunnySays/Resources/Sounds), the engine's safety net.
SHIPPED = {
    "blip": "0daaa48f17426a10383ef2cc1411b2c32ce530dbe9389ddb08af39720c19916b",
    "hello": "5b81100e153e5032fb8e5cd18c075554b1e9062fe9dbf1319d4687ed4c3ed7e6",
    "shh": "2b36bf06948079399e0f140f680f9349c420f6a797ac57f88d442fc75a530cbd",
    "tune-1": "b8402f5f5247538db3420488818a46a73137c4ff80499f04f183bd0eabf283c4",
    "tune-2": "63f1261ba39497493f62457bcf4fb7e314fdfe012f74ea8547c4b769517dd5ee",
}


class SynthTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.rendered = synth.render(synth.load_recipes(FIXTURE_APP / "macos" / "scripts" / "sounds.py"))

    def test_sunnysays_recipes_render_its_shipped_files(self):
        self.assertEqual({name: hashlib.sha256(data).hexdigest() for name, data in self.rendered.items()}, SHIPPED)

    @unittest.skipUnless(SUNNYSAYS_SOUNDS.exists(), "../sunnysays isn't checked out next to laching")
    def test_byte_for_byte_with_the_checkout(self):
        for name, data in self.rendered.items():
            self.assertEqual(data, (SUNNYSAYS_SOUNDS / f"{name}.wav").read_bytes(), name)

    def test_write_then_check(self):
        with fixture_app() as root:
            out = io.StringIO()
            with redirect_stdout(out):
                self.assertEqual(synth.main([]), 0)
                self.assertEqual(synth.main(["--check"]), 0)
                (root / "macos/Fixture/Resources/Sounds/blip.wav").write_bytes(b"changed")
                self.assertEqual(synth.main(["--check"]), 1)
            self.assertIn("stale: macos/Fixture/Resources/Sounds/blip.wav", out.getvalue())


if __name__ == "__main__":
    unittest.main()
