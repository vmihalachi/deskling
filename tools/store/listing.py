"""A companion app's store listing, as `companion.json`'s `store` section describes it.

`store.dir` holds one `<lang>.md` per language in `languages.all`: each field is a `### Heading `field.id`` followed by
a ```text block (`parse`). `store.apple` names the app in App Store Connect (its Apple ID, platform, copyright, the
listing language → App Store Connect locale map, the in-app purchases), `store.microsoft` the Partner Center import.
Standard library only.
"""

from __future__ import annotations

import re
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import companion  # noqa: E402

HEADING = re.compile(r"^### .*`([a-z0-9.]+)`\s*$")

# field id -> (max characters per block, or per line for list fields); the in-app purchases' fields come from the
# config (`Listing.limits`).
APPLE_LIMITS = {
    "apple.name": 30,
    "apple.subtitle": 30,
    "apple.promo": 170,
    "apple.keywords": 100,  # bytes, see check.py
    "apple.description": 4000,
    "apple.whatsnew": 4000,
}
IAP_NAME_LIMIT = 30
IAP_DESCRIPTION_LIMIT = 45
MICROSOFT_LIMITS = {
    "ms.name": 256,
    "ms.short": 1000,
    "ms.description": 10000,
    "ms.features": 200,
    "ms.search": 30,
    "ms.captions": 200,
    "ms.copyright": 200,
}
# Fields a file may leave out: What's New only exists from the second version on.
OPTIONAL = {"apple.whatsnew"}
# list fields: one entry per line, at most this many lines
MAX_LINES = {"ms.features": 20, "ms.search": 7, "ms.captions": 10}
MULTI_LINE = {"apple.description", "apple.whatsnew", "ms.description"}


def parse(path: Path) -> dict[str, str]:
    """Maps each field id to the first ```text block after its heading."""
    fields, key, block = {}, None, None
    for line in path.read_text(encoding="utf-8").split("\n"):
        if block is not None:
            if line == "```":
                fields[key], key, block = "\n".join(block), None, None
            else:
                block.append(line)
        elif match := HEADING.match(line):
            key = match.group(1)
        elif line == "```text" and key:
            block = []
    return fields


class Listing:
    """The `store` section of a loaded companion.json, with its paths resolved."""

    def __init__(self, config: companion.Config):
        self.config = config
        self.dir = config.require_path("store.dir")
        self.langs = config.languages
        self.source = config.source_language
        self.site_url = (config.get("store.siteUrl") or f"https://{config.require('domain')}").rstrip("/")
        apple = config.get("store.apple") or {}
        self.apple = apple
        self.app_id = apple.get("appId")
        self.platform = apple.get("platform", "MAC_OS")
        self.copyright = apple.get("copyright")
        self.locale = dict(apple.get("locales", {}))
        self.extra_locales = dict(apple.get("extraLocales", {}))
        self.base_territory = apple.get("baseTerritory", "USA")
        self.screenshots = self.dir / apple.get("screenshots", "apple/screenshots")
        self.previews = self.dir / apple.get("previews", "apple/previews")
        self.screenshot_type = apple.get("screenshotType", "APP_DESKTOP")
        self.preview_type = apple.get("previewType", "DESKTOP")
        review = apple.get("review", {})
        self.review_notes = self.dir / review.get("notes", "app-review.md")
        self.review_shots = config.root / review["shots"] if review.get("shots") else None
        self.review_size = tuple(review.get("size", (1440, 900)))
        self.review_background = review.get("background", "FFFFFF")
        self.iaps = {
            product: {**spec, "screenshot": self.review_shots / spec["reviewShot"]
                      if self.review_shots and spec.get("reviewShot") else None}
            for product, spec in apple.get("iaps", {}).items()
        }
        self.microsoft = config.get("store.microsoft")

    def site(self, lang: str) -> str:
        """The landing page for a language: the site's root for the source language, `/<lang>/` for the others."""
        return f"{self.site_url}/" + ("" if lang == self.source else lang.lower() + "/")

    def fields(self, lang: str) -> dict[str, str]:
        return parse(self.dir / f"{lang}.md")

    @property
    def limits(self) -> dict[str, int]:
        limits = dict(APPLE_LIMITS)
        for spec in self.iaps.values():
            limits[spec["name"]] = IAP_NAME_LIMIT
            limits[spec["description"]] = IAP_DESCRIPTION_LIMIT
        if self.microsoft is not None:
            limits.update(MICROSOFT_LIMITS)
        return limits

    @property
    def single_line(self) -> set[str]:
        return {key for key in self.limits if key not in MAX_LINES and key not in MULTI_LINE}


def load(root: Path | None = None) -> Listing:
    """The app's listing (companion.json found as `companion.load` finds it); exits when it has no `store` section."""
    config = companion.load(root)
    if config.get("store") is None:
        sys.exit(f"companion: {config.root / companion.FILE} has no store section")
    return Listing(config)
