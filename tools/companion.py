#!/usr/bin/env python3
"""Reads a companion app's `companion.json`, the one file every tool in `tools/` takes its names and paths from.

    python3 deskling/tools/companion.py get mac.scheme       # a value: strings as they are, the rest as JSON
    python3 deskling/tools/companion.py path voice.dir       # a path from the file, made absolute
    python3 deskling/tools/companion.py root                 # the app's root folder
    python3 deskling/tools/companion.py validate [FILE…]     # check against companion.schema.json (exit 1 on problems)

The root is the folder holding `companion.json`: `$COMPANION_ROOT` when set, else the nearest one walking up from the
current directory. Paths in the file are relative to it. Other tools import this module:

    import companion
    config = companion.load()                 # validated; exits with the problems otherwise
    config.get("voice.voices")                # None for an absent or null value
    config.path("l10n.catalog")               # an absolute Path, or None

Standard library only. The schema (`companion.schema.json`) is the source of truth for every section.
"""

from __future__ import annotations

import json
import os
import re
import sys
from pathlib import Path

FILE = "companion.json"
SCHEMA = Path(__file__).resolve().with_name("companion.schema.json")


class ConfigError(Exception):
    pass


def find_root(start: Path | None = None) -> Path:
    """`$COMPANION_ROOT`, else the nearest folder with a companion.json from `start` (the current directory) up."""
    env = os.environ.get("COMPANION_ROOT")
    if env:
        root = Path(env).expanduser().resolve()
        if not (root / FILE).is_file():
            raise ConfigError(f"$COMPANION_ROOT is {root}, which has no {FILE}")
        return root
    folder = (start or Path.cwd()).resolve()
    for candidate in [folder, *folder.parents]:
        if (candidate / FILE).is_file():
            return candidate
    raise ConfigError(f"no {FILE} in {folder} or any folder above it (or set $COMPANION_ROOT)")


class Config:
    """A loaded companion.json: dotted lookups and paths resolved against the root."""

    def __init__(self, data: dict, root: Path):
        self.data = data
        self.root = root

    def get(self, dotted: str, default=None):
        node = self.data
        for part in dotted.split("."):
            if not isinstance(node, dict) or part not in node:
                return default
            node = node[part]
        return default if node is None else node

    def require(self, dotted: str):
        value = self.get(dotted)
        if value is None:
            raise ConfigError(f"{FILE} needs {dotted}")
        return value

    def path(self, dotted: str, base: str | None = None) -> Path | None:
        """The path at `dotted`, absolute; relative to the folder at `base` (another dotted key) instead of the root."""
        value = self.get(dotted)
        if value is None:
            return None
        folder = self.path(base) if base else self.root
        return (folder / value).resolve()

    def require_path(self, dotted: str, base: str | None = None) -> Path:
        self.require(dotted)
        return self.path(dotted, base)  # type: ignore[return-value]

    # Names several tools need.

    @property
    def source_language(self) -> str:
        return self.require("languages.source")

    @property
    def languages(self) -> list[str]:
        return list(self.require("languages.all"))

    @property
    def mac_dir(self) -> Path:
        return self.require_path("mac.dir")

    @property
    def derived_data(self) -> Path:
        return self.path("mac.derivedData") or self.mac_dir / "build" / "dd.noindex"

    @property
    def work_dir(self) -> Path:
        """Scratch output for the tools (work files, voice renders), ignored by git."""
        return self.mac_dir / "build"

    @property
    def phrases_file(self) -> Path:
        return self.require_path("conformance.dir") / "phrases.json"


# MARK: Validation (the subset of JSON Schema the schema uses)

_TYPES = {
    "object": dict,
    "array": list,
    "string": str,
    "number": (int, float),
    "integer": int,
    "boolean": bool,
    "null": type(None),
}


def _is(value, kind: str) -> bool:
    if kind in ("number", "integer") and isinstance(value, bool):
        return False
    return isinstance(value, _TYPES[kind])


def validate(data, schema: dict | None = None, where: str = "") -> list[str]:
    """Every way `data` breaks `schema` (companion.schema.json by default), one message each."""
    if schema is None:
        schema = json.loads(SCHEMA.read_text("utf8"))
    label = where or "(root)"
    problems: list[str] = []
    kinds = schema.get("type")
    if kinds is not None:
        kinds = [kinds] if isinstance(kinds, str) else kinds
        if not any(_is(data, kind) for kind in kinds):
            return [f"{label}: expected {' or '.join(kinds)}, got {type(data).__name__}"]
    if "enum" in schema and data not in schema["enum"]:
        problems.append(f"{label}: {data!r} is not one of {schema['enum']}")
    if isinstance(data, str) and "pattern" in schema and not re.search(schema["pattern"], data):
        problems.append(f"{label}: {data!r} doesn't match {schema['pattern']}")
    if isinstance(data, dict):
        for key in schema.get("required", []):
            if key not in data:
                problems.append(f"{label}: missing {key}")
        properties = schema.get("properties", {})
        extra = schema.get("additionalProperties", True)
        for key, value in data.items():
            path = f"{where}.{key}" if where else key
            if key in properties:
                problems += validate(value, properties[key], path)
            elif extra is False:
                problems.append(f"{label}: unknown key {key}")
            elif isinstance(extra, dict):
                problems += validate(value, extra, path)
    if isinstance(data, list):
        if len(data) < schema.get("minItems", 0):
            problems.append(f"{label}: needs at least {schema['minItems']} items")
        if "items" in schema:
            for index, item in enumerate(data):
                problems += validate(item, schema["items"], f"{where}[{index}]")
    return problems


def consistency(data: dict) -> list[str]:
    """Checks the schema can't express: the source and default voice are among the lists that name them."""
    problems = []
    languages = data.get("languages", {})
    if languages.get("source") not in languages.get("all", []):
        problems.append("languages.source must be in languages.all")
    voice = data.get("voice") or {}
    if voice and voice.get("defaultVoice") not in voice.get("voices", []):
        problems.append("voice.defaultVoice must be in voice.voices")
    for lang in (data.get("l10n") or {}).get("pluralCategories", {}):
        if lang not in languages.get("all", []):
            problems.append(f"l10n.pluralCategories names {lang}, which isn't in languages.all")
    return problems


def load(root: Path | None = None, check: bool = True) -> Config:
    """The companion.json at `root` (found with `find_root` when None). Exits with the problems when it's invalid."""
    try:
        root = root or find_root()
        data = json.loads((root / FILE).read_text("utf8"))
    except (ConfigError, json.JSONDecodeError) as error:
        sys.exit(f"companion: {error}")
    if check:
        problems = validate(data) + consistency(data)
        if problems:
            sys.exit("companion: " + str(root / FILE) + " is invalid:\n  " + "\n  ".join(problems))
    return Config(data, root)


def main(argv: list[str]) -> int:
    if not argv or argv[0] in ("-h", "--help"):
        print(__doc__)
        return 0 if argv else 2
    command, rest = argv[0], argv[1:]
    if command == "validate":
        files = [Path(f) for f in rest] or [find_root() / FILE]
        status = 0
        for file in files:
            data = json.loads(file.read_text("utf8"))
            problems = validate(data) + consistency(data)
            print(f"{file}: " + ("ok" if not problems else "\n  " + "\n  ".join(problems)))
            status |= bool(problems)
        return status
    config = load()
    if command == "root":
        print(config.root)
    elif command in ("get", "path") and len(rest) == 1:
        if command == "path":
            value = config.path(rest[0])
            if value is None:
                return 1
            print(value)
            return 0
        value = config.get(rest[0])
        if value is None:
            return 1
        print(value if isinstance(value, str) else json.dumps(value, ensure_ascii=False))
    else:
        print(__doc__)
        return 2
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
