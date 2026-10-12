#!/usr/bin/env python3
"""Tests for tools/l10n.py and tools/phrases.py over the fixture app (tools/tests/fixture-app): gaps, work files,
apply, the Windows (.resw) and Linux (.po) exports, manual keys, language-scoped phrases and the phrase sync."""

from __future__ import annotations

import io
import json
import re
import sys
import unittest
import xml.etree.ElementTree as ET
from contextlib import redirect_stdout
from pathlib import Path
from types import SimpleNamespace

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import companion  # noqa: E402
import l10n  # noqa: E402
import phrases  # noqa: E402
from support import fixture_app  # noqa: E402

APPLE_SPEC_RE = re.compile(r"%(?:\d+\$)?(?:ll|l|q|z)?[@diu]|%#@|%arg")
NET_ARG_RE = re.compile(r"(?<!\{)\{(\d+)\}(?!\})")


def quiet(function, *args):
    out = io.StringIO()
    with redirect_stdout(out):
        result = function(*args)
    return result, out.getvalue()


class L10nTestCase(unittest.TestCase):
    def setUp(self):
        self.app = fixture_app()
        self.root = self.app.__enter__()
        l10n.configure(companion.load())

    def tearDown(self):
        self.app.__exit__(None, None, None)

    def args(self, **values):
        defaults = {"since": None, "verbose": True, "strict": False, "lang": None, "out": None, "dry_run": False}
        return SimpleNamespace(**{**defaults, **values})


class ConfigurationTests(L10nTestCase):
    def test_everything_comes_from_companion_json(self):
        self.assertEqual(l10n.SOURCE, "en")
        self.assertEqual(l10n.app_languages(), ["it", "de"])
        self.assertEqual(list(l10n.CATALOGS), ["app"], "l10n.plist is null")
        self.assertEqual(l10n.WINDOWS_STRINGS, self.root / "windows/Fixture.App/Strings")
        self.assertIsNone(l10n.LINUX_PO)
        self.assertEqual(l10n.landing_languages(), [])
        self.assertEqual(l10n.GLOSSARY_KEYS, ["Say something"])
        self.assertIn("Fixy is a fixture", l10n.RULES, "the app's rules file")
        self.assertIn("Keep every format specifier", l10n.RULES, "the generic rules")
        self.assertTrue(l10n.RULES.endswith("Keep the names Fixy as they are."))

    def test_scoped_phrases(self):
        self.assertEqual(l10n.SCOPED, {"phrase.ciao": {"it"}})
        self.assertTrue(l10n.in_scope("phrase.ciao", "it"))
        self.assertTrue(l10n.in_scope("phrase.ciao", "it-CH"))
        self.assertTrue(l10n.in_scope("phrase.ciao", "en"), "the source language always has the key")
        self.assertFalse(l10n.in_scope("phrase.ciao", "de"))
        self.assertTrue(l10n.in_scope("phrase.hello", "de"))
        self.assertEqual(l10n.base_language("pt-BR"), "pt")


class GapTests(L10nTestCase):
    def test_status_skips_keys_out_of_scope(self):
        report = l10n.collect()
        self.assertEqual(report["it"]["app"], {})
        self.assertEqual(
            report["de"]["app"],
            {"Hello. This is how I sound.": ["missing"], "Shh": ["missing"], "phrase.joke.1": ["missing"]},
        )
        result, out = quiet(l10n.cmd_status, self.args())
        self.assertEqual(result, 0)
        self.assertIn("de: app 3", out)
        self.assertNotIn("phrase.ciao", out)
        self.assertEqual(quiet(l10n.cmd_check, self.args())[0], 1)

    def test_a_substitution_needs_every_plural_category(self):
        def plural(values):
            return {"variations": {"plural": {cat: {"stringUnit": {"state": "translated", "value": value}}
                                              for cat, value in values.items()}}}

        def localization(value, lines):
            return {"stringUnit": {"state": "translated", "value": value},
                    "substitutions": {"lines": {"argNum": 1, "formatSpecifier": "lld", **plural(lines)}}}

        entry = {"localizations": {
            "en": localization("%#@lines@ muted", {"one": "%arg line", "other": "%arg lines"}),
            "it": localization("%#@lines@ silenziate", {"one": "%arg battuta"}),
            "de": localization("%#@lines@ stumm", {"one": "%arg Satz", "other": "%arg Sätze"}),
        }}
        self.assertEqual(l10n.english_substitutions(entry)["lines"]["plural"], {"one": "%arg line", "other": "%arg lines"})
        self.assertEqual(l10n.catalog_issues("%lld lines muted", entry, "it"), ["substitution lines: missing other"])
        self.assertEqual(l10n.catalog_issues("%lld lines muted", entry, "de"), [])
        self.assertEqual(l10n.substitution_issues({"localizations": {"en": {}}}, "it"), [], "no substitutions, nothing to check")

    def test_export_and_apply(self):
        out = self.root / "work"
        quiet(l10n.cmd_export, self.args(out=str(out)))
        self.assertFalse((out / "it.json").exists(), "nothing to translate in Italian")
        work = json.loads((out / "de.json").read_text("utf8"))
        self.assertEqual(list(work["app"]), ["Hello. This is how I sound.", "Shh", "phrase.joke.1"])
        self.assertEqual(work["glossary"], {"Say something": "Sag etwas"})
        self.assertEqual(work["rules"], l10n.RULES)
        self.assertEqual(work["app"]["phrase.joke.1"]["comment"], "Fixy says this aloud. Part 1 of 2.")
        work["app"]["Shh"]["value"] = "Pst"
        work["app"]["phrase.joke.1"]["value"] = "Warum hat sich der Clown hingesetzt? %@"
        (out / "de.json").write_text(json.dumps(work, ensure_ascii=False), "utf8")
        result, text = quiet(l10n.cmd_apply, SimpleNamespace(files=[str(out / "de.json")]))
        self.assertEqual(result, 1, "a specifier the English lacks is rejected")
        self.assertIn("applied 1", text)
        self.assertEqual(
            l10n.collect()["de"]["app"], {"Hello. This is how I sound.": ["missing"], "phrase.joke.1": ["missing"]}
        )


class ExportTests(L10nTestCase):
    def setUp(self):
        super().setUp()
        self.resw = l10n.windows_files(self.root / "windows")
        self.po = l10n.linux_files(self.root / "linux")
        self.units = {lang: l10n.export_units(lang) for lang in l10n.export_languages()}

    def test_every_language_is_exported(self):
        self.assertEqual(l10n.export_languages(), ["en", "it", "de"])
        self.assertEqual(len(self.resw), 3)
        self.assertEqual(sorted(path.name for path in self.po), ["LINGUAS", "de.po", "en.po", "it.po"])

    def test_a_scoped_phrase_is_left_out_of_other_languages(self):
        keys = {lang: {unit["key"] for unit in units} for lang, units in self.units.items()}
        self.assertIn("phrase.ciao", keys["en"])
        self.assertIn("phrase.ciao", keys["it"])
        self.assertNotIn("phrase.ciao", keys["de"])
        self.assertIn("phrase.joke.1", keys["de"], "a missing translation falls back to English, still exported")
        de = (self.root / "windows" / "de" / "Resources.resw")
        self.assertNotIn(l10n.resw_name("phrase.ciao"), self.resw[de])

    def test_no_apple_specifiers_remain(self):
        for lang, units in self.units.items():
            for unit in units:
                for text in [unit.get("value", "")] + list(unit.get("plural", {}).values()):
                    self.assertIsNone(APPLE_SPEC_RE.search(text), f"{lang} {unit['key']}: {text}")

    def test_known_conversions(self):
        plural = {u["key"]: u for u in self.units["de"]}["%lld lines"]
        self.assertEqual(plural["plural"], {"one": "{0} Zeile", "other": "{0} Zeilen"})
        self.assertEqual(l10n.to_net_format("%2$@ and %1$@, 100%% {x}"), "{1} and {0}, 100% {{x}}")

    def test_resw_is_valid_and_names_are_unique(self):
        for path, text in self.resw.items():
            names = [data.get("name") for data in ET.fromstring(text).iter("data")]
            self.assertEqual(len(names), len(set(names)), path)
            self.assertIn("Localizable.xcstrings by deskling/tools/l10n.py", text)
        self.assertEqual(l10n.resw_name("A_b é"), "_0041_005Fb_0020_00E9")
        self.assertEqual(l10n.resw_name("💪"), "_D83D_DCAA")
        self.assertNotEqual(l10n.resw_name("To").lower(), l10n.resw_name("to").lower())
        long_key = "x" * 300
        self.assertLessEqual(len(l10n.resw_name(long_key)), l10n.RESW_NAME_MAX)

    def test_po_is_valid(self):
        for path, text in self.po.items():
            if path.name == "LINGUAS":
                self.assertEqual(text, "en\nit\nde\n")
                continue
            self.assertIn("Plural-Forms: nplurals=2; plural=(n != 1);", text)
            self.assertEqual(text.count("msgid_plural"), 1, path)

    def test_check_flags_stale_exports(self):
        l10n.WINDOWS_STRINGS = self.root / "port" / "Fixture.App" / "Strings"
        self.assertEqual(l10n.stale_exports(), [], "no port folder yet: nothing to check")
        (self.root / "port").mkdir()
        self.assertEqual(len(l10n.stale_exports()), 3, "the port exists: its exports must too")
        quiet(l10n.write_export, l10n.windows_files(), False)
        self.assertEqual(l10n.stale_exports(), [])
        stale = l10n.WINDOWS_STRINGS / "de" / "Resources.resw"
        stale.write_text("stale", "utf8")
        self.assertEqual(l10n.stale_exports(), [stale])

    def test_add_writes_manual_entries(self):
        new = self.root / "new.json"
        new.write_text(json.dumps({"Only on Windows": {"comment": "A tray tooltip", "en": "Only on Windows"}}), "utf8")
        self.assertEqual(quiet(l10n.cmd_add, SimpleNamespace(file=str(new)))[0], 0)
        entry = l10n.load_catalog(l10n.CATALOGS["app"])["strings"]["Only on Windows"]
        self.assertEqual(entry["extractionState"], "manual")
        self.assertEqual(l10n.collect()["de"]["app"]["Only on Windows"], ["missing"])


class PhrasesTests(L10nTestCase):
    def config(self) -> companion.Config:
        return companion.load()

    def rewrite_bank(self, change):
        path = self.root / "conformance" / "phrases.json"
        data = json.loads(path.read_text("utf8"))
        change(data["phrases"])
        path.write_text(json.dumps(data, ensure_ascii=False), "utf8")

    def test_the_fixture_is_in_step(self):
        self.assertEqual(quiet(phrases.cmd_check, self.config())[0], 0)

    def test_sync_adds_parts_updates_text_and_retires_lines(self):
        def change(bank):
            bank[:] = [p for p in bank if p["id"] != "hello"]
            joke = next(p for p in bank if p["id"] == "joke")
            joke["parts"][1] = "His feet were sore."
            bank.append({
                "id": "long-joke", "kind": "sequence", "tags": ["general"], "weight": 3, "skins": [], "flavor": "a",
                "keys": ["phrase.long-joke.1", "phrase.long-joke.2", "phrase.long-joke.3"],
                "parts": ["One.", "Two.", "Three."], "languages": ["it"],
            })
        self.rewrite_bank(change)
        self.assertEqual(quiet(phrases.cmd_check, self.config())[0], 1)
        result, out = quiet(phrases.cmd_sync, self.config())
        self.assertEqual(result, 0, out)
        strings = l10n.load_catalog(l10n.CATALOGS["app"])["strings"]
        self.assertNotIn("phrase.hello", strings)
        self.assertEqual(strings["phrase.joke.2"]["localizations"], {"en": {"stringUnit": {"state": "translated", "value": "His feet were sore."}}})
        self.assertIn("it", strings["phrase.joke.1"]["localizations"], "an unchanged part keeps its translations")
        self.assertEqual(strings["phrase.long-joke.3"]["extractionState"], "manual")
        self.assertEqual(
            strings["phrase.long-joke.2"]["comment"],
            "Fixy says this aloud (general). Flavor a: the first flavor. Part 2 of 3 of a joke told in beats. "
            "Exists only in it: don't translate it into other languages.",
        )
        self.assertEqual(
            strings["phrase.joke.1"]["comment"],
            "Fixy says this aloud (buttonPress, general). Flavor b: the second flavor. Part 1 of 2 of a joke told in beats.",
        )
        self.assertEqual(quiet(phrases.cmd_check, self.config())[0], 0)
        l10n.configure(companion.load())
        report = l10n.collect()
        self.assertNotIn("phrase.long-joke.1", report["de"]["app"])
        self.assertIn("phrase.long-joke.1", report["it"]["app"])
        self.assertIn("phrase.joke.2", report["it"]["app"], "a rewritten part needs translating again")


if __name__ == "__main__":
    unittest.main()
