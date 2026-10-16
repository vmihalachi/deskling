"""tools/store over the fixture app: check.py's limits, push.py against an in-memory App Store Connect (from
mybackhurts' test_push.py), import.py's Partner Center folder."""

from __future__ import annotations

import base64
import csv
import json
import os
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

from support import TOOLS, fixture_app

sys.path.insert(0, str(TOOLS / "store"))
import check  # noqa: E402
import listing as listing_module  # noqa: E402
import push  # noqa: E402

PNG = b"\x89PNG\r\n\x1a\n" + b"\0" * 16


class FakeApi:
    """An in-memory App Store Connect holding the objects push.py reads and writes."""

    def __init__(self, app_id):
        self.app_id = app_id
        self.objects = {}  # (kind, id) -> {"id", "type", "attributes", "parent"}
        self.calls = []
        self.next_id = 1

    def add(self, kind, attributes, parent=None):
        obj = {"id": str(self.next_id), "type": kind, "attributes": dict(attributes), "parent": parent}
        self.next_id += 1
        self.objects[(kind, obj["id"])] = obj
        return obj

    def children(self, kind, parent_id):
        return [o for (k, _), o in self.objects.items() if k == kind and o["parent"] == parent_id]

    def get_all(self, path):
        path, _, query = path.partition("?")
        filters = dict(x.split("=", 1) for x in query.split("&") if x.startswith("filter["))

        def matching(objects, names):
            return [o for o in objects if all(o["attributes"].get(attr, "MAC_OS" if attr == "platform" else None)
                                              == filters[key] for key, attr in names.items() if key in filters)]
        if path.endswith("/appStoreVersions"):
            return matching(self.children("appStoreVersions", self.app_id),
                            {"filter[platform]": "platform", "filter[versionString]": "versionString"})
        if path.endswith("/appInfos"):
            return self.children("appInfos", self.app_id)
        if path.endswith("/inAppPurchasesV2"):
            return self.children("inAppPurchases", self.app_id)
        if path == "/v1/builds":
            return matching(self.children("builds", self.app_id),
                            {"filter[version]": "version", "filter[preReleaseVersion.platform]": "platform"})
        kind = path.rsplit("/", 1)[1]
        parent_id = path.rsplit("/", 2)[1]
        return self.children(kind, parent_id)

    def request(self, method, path, body=None):
        self.calls.append((method, path))
        if method == "GET" and path.endswith("/build"):
            return {"data": None}
        return None

    def create(self, kind, attributes, relationships):
        self.calls.append(("POST", kind, attributes.get("locale") or attributes.get("fileName")))
        parent = next(iter(relationships.values()))[1]
        if "locale" in attributes and any(o["attributes"].get("locale") == attributes["locale"]
                                          for o in self.children(kind, parent)):
            raise push.ApiError(f"409 Entity with locale: {attributes['locale']} already exists.")
        obj = self.add(kind, attributes, parent)
        if kind == "appInfoLocalizations":  # like App Store Connect: the editable version gets the language too
            for version in self.children("appStoreVersions", self.app_id):
                self.add("appStoreVersionLocalizations", {"locale": attributes["locale"]}, version["id"])
        if kind in ("appScreenshots", "appPreviews"):
            obj["attributes"]["uploadOperations"] = []
        return obj

    def update(self, kind, id, attributes):
        self.calls.append(("PATCH", kind, tuple(sorted(attributes))))
        self.objects[(kind, id)]["attributes"].update(attributes)

    def delete(self, kind, id):
        self.calls.append(("DELETE", kind, id))
        del self.objects[(kind, id)]

    def upload(self, operations, data):
        pass


def quiet(_):
    pass


class StoreTests(unittest.TestCase):
    def setUp(self):
        self._app = fixture_app()
        self.root = self._app.__enter__()
        self.listing = listing_module.load(self.root)

    def tearDown(self):
        self._app.__exit__(None, None, None)

    # MARK: The listing and check.py

    def test_every_language_has_a_locale_and_all_fields(self):
        self.assertEqual(set(self.listing.locale), set(self.listing.langs))
        for lang in self.listing.langs:
            fields = self.listing.fields(lang)
            info = push.info_fields(self.listing, lang, fields)
            for value in {**info, **push.version_fields(self.listing, lang, fields)}.values():
                self.assertTrue(value, lang)
            for spec in self.listing.iaps.values():
                self.assertTrue(fields[spec["name"]] and fields[spec["description"]], lang)

    def test_the_source_language_is_the_site_root(self):
        self.assertEqual(self.listing.site("en"), "https://fixture.example/")
        self.assertEqual(self.listing.site("de"), "https://fixture.example/de/")
        self.assertEqual(push.info_fields(self.listing, "it", {"apple.name": "", "apple.subtitle": ""})["privacyPolicyUrl"],
                         "https://fixture.example/it/privacy/")

    def test_the_limits_follow_the_config(self):
        limits = self.listing.limits
        self.assertEqual(limits["apple.iap.supporter.name"], 30)
        self.assertEqual(limits["apple.iap.supporter.description"], 45)
        self.assertIn("ms.captions", limits, "the app has store.microsoft")
        self.assertIn("apple.description", limits)
        self.assertNotIn("apple.description", self.listing.single_line)

    def test_check_passes_the_fixture_and_names_each_problem(self):
        self.assertTrue(check.run(self.listing, self.listing.langs))
        de = self.root / "store-listing" / "de.md"
        text = de.read_text("utf8").replace("Ein Freund am Schreibtisch", "Ein viel zu langer Untertitel hier")
        de.write_text(text.replace("begleiter,haustier", "begleiter, haustier"), "utf8")
        problems = check.check(self.listing, self.listing.fields("de"), self.listing.fields("en"))
        self.assertIn("apple.subtitle: 34 > 30 characters", problems)
        self.assertIn("apple.keywords: no spaces around commas (they count toward the limit)", problems)
        result = subprocess.run([sys.executable, str(TOOLS / "store" / "check.py"), "de"], capture_output=True, text=True,
                                env={**os.environ, "COMPANION_ROOT": str(self.root)})
        self.assertEqual(result.returncode, 1)
        self.assertIn("de: 2 problem(s)", result.stdout)

    def enable_ios(self):
        path = self.root / "companion.json"
        config = json.loads(path.read_text("utf8"))
        config["store"]["apple"]["ios"] = {}
        path.write_text(json.dumps(config), "utf8")
        self.listing = listing_module.load(self.root)

    def write_ios_page(self, keywords="companion,friend,pocket", whatsnew=None):
        en = self.root / "store-listing" / "en.md"
        en.write_text(en.read_text("utf8") + "\n".join([
            "### iOS promotional text `ios.promo`", "", "```text", "It says things, on the phone too.", "```", "",
            "### iOS keywords `ios.keywords`", "", "```text", keywords, "```", "",
            "### iOS description `ios.description`", "", "```text", "Fixture says a line now and then.", "",
            "Nothing leaves your iPhone.", "```", ""]
            + (["### iOS What's New `ios.whatsnew`", "", "```text", whatsnew, "```", ""] if whatsnew else [])), "utf8")

    def test_the_ios_page_is_checked_only_when_configured(self):
        self.assertNotIn("ios.promo", self.listing.limits)
        self.enable_ios()
        problems = check.check(self.listing, self.listing.fields("en"), self.listing.fields("en"))
        self.assertEqual(problems, ["missing field ios.promo", "missing field ios.keywords", "missing field ios.description"],
                         "What's New stays optional")
        self.write_ios_page()
        problems = check.check(self.listing, self.listing.fields("en"), self.listing.fields("en"))
        self.assertEqual(problems, ["ios.keywords: 'friend' is already in the name or subtitle"])
        self.assertNotIn("ios.description", self.listing.single_line)

    # MARK: push.py

    def test_token_is_a_valid_es256_jwt(self):
        with tempfile.TemporaryDirectory() as tmp:
            key, pub = Path(tmp) / "AuthKey_TEST.p8", Path(tmp) / "pub.pem"
            pem = subprocess.run("openssl ecparam -name prime256v1 -genkey -noout | openssl pkcs8 -topk8 -nocrypt",
                                 shell=True, capture_output=True, check=True).stdout
            key.write_bytes(pem)
            subprocess.run(["openssl", "ec", "-in", key, "-pubout", "-out", pub], capture_output=True, check=True)
            token = push.make_token("TEST", "issuer", key, now=1000)
            header, claims, signature = token.split(".")
            pad = lambda s: base64.urlsafe_b64decode(s + "=" * (-len(s) % 4))  # noqa: E731
            self.assertEqual(json.loads(pad(header)), {"alg": "ES256", "kid": "TEST", "typ": "JWT"})
            self.assertEqual(json.loads(pad(claims))["aud"], "appstoreconnect-v1")
            raw = pad(signature)
            self.assertEqual(len(raw), 64)

            def integer(b):
                b = b.lstrip(b"\0")
                b = b"\0" + b if b[0] & 0x80 else b
                return bytes([2, len(b)]) + b
            body = integer(raw[:32]) + integer(raw[32:])
            sig = Path(tmp) / "sig.der"
            sig.write_bytes(bytes([0x30, len(body)]) + body)
            verify = subprocess.run(["openssl", "dgst", "-sha256", "-verify", pub, "-signature", sig],
                                    input=f"{header}.{claims}".encode(), capture_output=True)
            self.assertEqual(verify.returncode, 0, verify.stdout + verify.stderr)

    def test_der_to_raw_pads_and_strips(self):
        r, s = b"\x00\x80" + b"\x01" * 31, b"\x05"
        der = bytes([0x30, 4 + len(r) + len(s), 2, len(r)]) + r + bytes([2, len(s)]) + s
        self.assertEqual(push.der_to_raw(der), b"\x80" + b"\x01" * 31 + b"\0" * 31 + b"\x05")

    def setup_app(self, earlier=True):
        api = FakeApi(self.listing.app_id)
        if earlier:
            api.add("appStoreVersions", {"platform": "MAC_OS", "versionString": "1.0.1", "appVersionState": "READY_FOR_SALE"},
                    self.listing.app_id)
        version = api.add("appStoreVersions", {"platform": "MAC_OS", "versionString": "1.0.2",
                                               "appVersionState": "PREPARE_FOR_SUBMISSION",
                                               "copyright": self.listing.copyright, "releaseType": "MANUAL"},
                          self.listing.app_id)
        info = api.add("appInfos", {"state": "PREPARE_FOR_SUBMISSION"}, self.listing.app_id)
        en = self.listing.fields("en")
        api.add("appInfoLocalizations", {"locale": "en-US", **push.info_fields(self.listing, "en", en)}, info["id"])
        stale = {**push.version_fields(self.listing, "en", en), "whatsNew": "old"}
        api.add("appStoreVersionLocalizations", {"locale": "en-US", **stale}, version["id"])
        return api, version

    def test_text_patches_only_what_changed_and_adds_missing_languages(self):
        api, version = self.setup_app()
        sync = push.Sync(self.listing, api, apply=True, log=quiet)
        result = sync.text(["en", "de"], sync.version("1.0.2", False, None), extra=False, write=True)
        self.assertEqual(sync.problems, [])
        self.assertIn(("PATCH", "appStoreVersionLocalizations", ("whatsNew",)), api.calls)
        self.assertIn(("POST", "appInfoLocalizations", "de-DE"), api.calls)
        # App Store Connect added de-DE to the version itself; push.py fills it in instead of adding it again.
        self.assertNotIn(("POST", "appStoreVersionLocalizations", "de-DE"), api.calls)
        self.assertEqual(api.children("appStoreVersionLocalizations", version["id"])[1]["attributes"]["whatsNew"],
                         self.listing.fields("de")["apple.whatsnew"])
        self.assertNotIn("PATCH", [c[0] for c in api.calls if c[1] == "appInfoLocalizations"])
        self.assertEqual(set(result), {"en-US", "de-DE"})

        api.calls.clear()
        sync.text(["en", "de"], version, extra=False, write=True)
        self.assertEqual([c for c in api.calls if c[0] != "GET"], [], "a second run changes nothing")

    def test_whats_new_is_left_out_on_a_first_version(self):
        api, version = self.setup_app(earlier=False)
        sync = push.Sync(self.listing, api, apply=True, log=quiet)
        sync.text(["de"], version, extra=False, write=True)
        de = next(x for x in api.children("appStoreVersionLocalizations", version["id"]) if x["attributes"]["locale"] == "de-DE")
        self.assertNotIn("whatsNew", de["attributes"])
        self.assertEqual(de["attributes"]["description"], self.listing.fields("de")["apple.description"])

    def test_the_ios_page_is_found_or_created_filled_and_given_its_screenshots(self):
        self.enable_ios()
        self.write_ios_page(whatsnew="Now on iPhone.")
        for folder, shots in ((self.listing.iphone_screenshots, ("1-phone.png",)),
                              (self.listing.ipad_screenshots, ("1-pad.png", "2-pad.png"))):
            (folder / "en").mkdir(parents=True)
            for name in shots:
                (folder / "en" / name).write_bytes(PNG + name.encode())
        api, mac = self.setup_app()
        api.add("builds", {"version": "4", "platform": "MAC_OS", "processingState": "VALID"}, self.listing.app_id)
        api.add("builds", {"version": "4", "platform": "IOS", "processingState": "VALID"}, self.listing.app_id)
        sync = push.Sync(self.listing, api, apply=True, log=quiet)
        with self.assertRaises(SystemExit, msg="no iOS version without --create"):
            sync.version("1.0.2", False, None, sync.ios)
        version = sync.version("1.0.2", True, "4", sync.ios)
        self.assertEqual(version["attributes"]["platform"], "IOS")
        self.assertNotEqual(version["id"], mac["id"])
        self.assertIn(("PATCH", f"/v1/appStoreVersions/{version['id']}/relationships/build"), api.calls)
        self.assertNotIn(("PATCH", f"/v1/appStoreVersions/{mac['id']}/relationships/build"), api.calls)

        localizations = sync.text(["en"], version, extra=False, write=True, page=sync.ios)
        sync.screenshots(localizations, sync.ios)
        self.assertEqual(sync.problems, [])
        en = localizations["en-US"][1]["attributes"]
        fields = self.listing.fields("en")
        self.assertEqual(en["description"], fields["ios.description"])
        self.assertEqual(en["keywords"], fields["ios.keywords"])
        self.assertEqual(en["promotionalText"], fields["ios.promo"])
        self.assertNotIn("whatsNew", en, "the platform's first version")
        sets = {s["attributes"]["screenshotDisplayType"]: s for s in api.children("appScreenshotSets", localizations["en-US"][1]["id"])}
        self.assertEqual(set(sets), {"APP_IPHONE_67", "APP_IPAD_PRO_3GEN_129"})
        self.assertEqual([x["attributes"]["fileName"] for x in api.children("appScreenshots", sets["APP_IPAD_PRO_3GEN_129"]["id"])],
                         ["1-pad.png", "2-pad.png"])
        self.assertEqual(api.children("appStoreVersionLocalizations", mac["id"])[0]["attributes"]["description"],
                         fields["apple.description"], "the Mac's page keeps its own text")

        api.calls.clear()
        sync.version("1.0.2", True, None, sync.ios)
        sync.screenshots(sync.text(["en"], version, extra=False, write=True, page=sync.ios), sync.ios)
        self.assertEqual([c for c in api.calls if c[0] != "GET"], [], "a second run changes nothing")

    def test_the_ios_parts_need_the_ios_config(self):
        result = subprocess.run([sys.executable, str(TOOLS / "store" / "push.py"), "--version", "1.0", "--only", "ios-text"],
                                capture_output=True, text=True, env={**os.environ, "COMPANION_ROOT": str(self.root)})
        self.assertEqual(result.returncode, 2)
        self.assertIn("need store.apple.ios", result.stderr)

    def test_dry_run_writes_nothing(self):
        api, _ = self.setup_app()
        sync = push.Sync(self.listing, api, apply=False, log=quiet)
        localizations = sync.text(["en", "it"], sync.version("1.0.2", False, None), extra=False, write=True)
        sync.screenshots(localizations)
        sync.iaps(["en"], extra=False)
        self.assertEqual(api.calls, [])

    def test_screenshots_replace_a_stale_set_and_then_stay(self):
        folder = self.listing.screenshots / "en"
        folder.mkdir(parents=True)
        for name in ("1-home.png", "2-settings.png"):
            (folder / name).write_bytes(PNG + name.encode())
        api, version = self.setup_app()
        sync = push.Sync(self.listing, api, apply=True, log=quiet)
        localizations = sync.text(["en"], version, extra=False, write=False)
        locale = localizations["en-US"][1]
        shot_set = api.add("appScreenshotSets", {"screenshotDisplayType": self.listing.screenshot_type}, locale["id"])
        api.add("appScreenshots", {"fileName": "old.png", "sourceFileChecksum": "x"}, shot_set["id"])
        sync.screenshots(localizations)
        self.assertEqual(sync.problems, [])
        self.assertEqual(sum(c[0] == "DELETE" for c in api.calls), 1)
        self.assertEqual([c[2] for c in api.calls if c[:2] == ("POST", "appScreenshots")], ["1-home.png", "2-settings.png"])

        api.calls.clear()
        sync.screenshots(localizations)
        self.assertEqual(api.calls, [], "matching checksums leave the set alone")

    def test_extra_locales_only_when_present_or_asked(self):
        api, version = self.setup_app()
        sync = push.Sync(self.listing, api, apply=False, log=quiet)
        self.assertEqual(set(sync.text(["en"], version, extra=False, write=False)), {"en-US"})
        api.add("appStoreVersionLocalizations", {"locale": "en-GB"}, version["id"])
        self.assertEqual(set(sync.text(["en"], version, extra=False, write=False)), {"en-US", "en-GB"})
        self.assertEqual(set(sync.text(["en"], version, extra=True, write=False)), {"en-US", "en-GB", "en-CA", "en-AU"})

    def test_missing_version_needs_create(self):
        api = FakeApi(self.listing.app_id)
        with self.assertRaises(SystemExit):
            push.Sync(self.listing, api, apply=False, log=quiet).version("9.9", False, None)
        version = push.Sync(self.listing, api, apply=False, log=quiet).version("9.9", True, None)
        self.assertIsNone(version["id"])
        self.assertEqual(api.calls, [])

    def test_the_review_notes_come_from_their_block(self):
        self.assertEqual(push.review_notes(self.listing), "Fixture lives in the menu bar. Click it for a line.")

    def test_without_review_notes_or_an_iap_note_and_shot_app_store_connect_keeps_its_own(self):
        self.listing.review_notes.unlink()
        for spec in self.listing.iaps.values():
            del spec["reviewNote"], spec["reviewShot"]
            spec["screenshot"] = None
        api, version = self.setup_app()
        for product_id in self.listing.iaps:
            api.add("inAppPurchases", {"productId": product_id, "reviewNote": "theirs"}, self.listing.app_id)
        sync = push.Sync(self.listing, api, apply=True, log=quiet)
        sync.review(version)
        sync.iaps(["en"], extra=False)
        self.assertEqual(sync.problems, [])
        self.assertNotIn("PATCH", [c[0] for c in api.calls], "no review note set")
        self.assertFalse([c for c in api.calls if "appStoreReviewScreenshot" in str(c) or "ReviewDetail" in str(c)])

    # MARK: import.py

    def test_import_builds_a_partner_center_folder(self):
        store = self.root / "store-listing"
        for lang in self.listing.langs:
            folder = store / "microsoft" / "screenshots" / lang
            folder.mkdir(parents=True)
            for name in ("1.png", "2.png"):
                (folder / name).write_bytes(PNG)
        art = store / "microsoft" / "art"
        art.mkdir(parents=True)
        for name in ("poster-art.png", "box-art.png", "super-hero-art.png"):
            (art / name).write_bytes(PNG)
        out = self.root / "build" / "import"
        result = subprocess.run([sys.executable, str(TOOLS / "store" / "import.py"), "--out", str(out)],
                                capture_output=True, text=True, env={**os.environ, "COMPANION_ROOT": str(self.root)})
        self.assertEqual(result.returncode, 0, result.stderr)
        with open(out / "listings.csv", encoding="utf-8-sig", newline="") as f:
            rows = list(csv.reader(f))
        self.assertEqual(rows[0], ["Field", "ID", "Type (Type)", "default", "en", "it", "de"])
        values = {row[0]: row[4:] for row in rows[1:]}
        self.assertEqual(values["Title"], ["Fixture", "Fixture", "Fixture"])
        self.assertEqual(values["DevStudio"], ["Fixture Studio"] * 3)
        self.assertEqual(values["DesktopScreenshot1"][1], "import/it/1.png")
        self.assertEqual(values["DesktopScreenshotCaption2"][2], "Einstellungen")
        self.assertTrue((out / "art" / "box-art.png").exists())

    def test_import_refuses_a_language_without_its_screenshots(self):
        result = subprocess.run([sys.executable, str(TOOLS / "store" / "import.py"), "--out", str(self.root / "out")],
                                capture_output=True, text=True, env={**os.environ, "COMPANION_ROOT": str(self.root)})
        self.assertEqual(result.returncode, 1)
        self.assertIn("en: 0 screenshots", result.stderr)


if __name__ == "__main__":
    unittest.main()
