#!/usr/bin/env python3
"""Reads a companion app's `companion.json`, the one file every tool in `tools/` takes its names and paths from.

    python3 deskling/tools/companion.py get mac.scheme       # a value: strings as they are, the rest as JSON
    python3 deskling/tools/companion.py path voice.dir       # a path from the file, made absolute
    python3 deskling/tools/companion.py root                 # the app's root folder
    python3 deskling/tools/companion.py validate [FILE…]     # check against companion.schema.json (exit 1 on problems)
    python3 deskling/tools/companion.py env                  # paths and path patterns as shell assignments (verify.sh)
    python3 deskling/tools/companion.py pins                 # the Deskling version everywhere it's pinned (exit 1 on a mismatch)

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
import subprocess
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
    def phrases_file(self) -> Path | None:
        """`conformance.dir`/phrases.json, or None for an app without a conformance section (no phrase bank)."""
        folder = self.path("conformance.dir")
        return folder / "phrases.json" if folder else None

    def require_phrases_file(self) -> Path:
        """The phrases file, exiting with a clear message when the app has no conformance section."""
        path = self.phrases_file
        if path is None:
            sys.exit(f"companion: {self.root / FILE} has no conformance section, so there is no phrase bank to read")
        return path


# MARK: Derived names (verify.sh and the hook share these)


def ere(text: str) -> str:
    """`text` as a literal in a regular expression both Python and `grep -E` read the same way."""
    return re.sub(r"([.^$*+?()\[\]{}|\\])", r"\\\1", text)


def derived(config: Config) -> dict[str, str]:
    """Paths relative to the root (POSIX, "" when absent) and the path patterns of the checks, by name."""

    def rel(dotted: str, base: str | None = None) -> str:
        path = config.path(dotted, base)
        return path.relative_to(config.root).as_posix() if path else ""

    mac, core, app, tests = rel("mac.dir"), rel("mac.core", "mac.dir"), rel("mac.app", "mac.dir"), rel("mac.tests", "mac.dir")
    windows, conformance = rel("windows.dir"), rel("conformance.dir")
    win_core, win_core_tests = config.get("windows.core", ""), config.get("windows.coreTests", "")
    win_app_tests = config.get("windows.appTests", "")
    landing, landing_i18n = rel("landing.dir"), rel("l10n.landing")
    store = rel("store.dir")
    values = {
        "ROOT": str(config.root),
        "TOOLS": str(Path(__file__).resolve().parent),
        "NAME": config.require("name"),
        "MAC_DIR": mac,
        "MAC_PROJECT": config.get("mac.project", ""),
        "MAC_SCHEME": config.get("mac.scheme", ""),
        "MAC_SPEC": rel("mac.projectSpec", "mac.dir"),
        "MAC_APP": app,
        "MAC_TESTS": tests,
        "MAC_CORE": core,
        "DERIVED": config.derived_data.relative_to(config.root).as_posix(),
        "CATALOG": rel("l10n.catalog"),
        "LANDING_DIR": landing,
        "LANDING_I18N": landing_i18n,
        "LANDING_TOOL": config.get("landing.tool", ""),
        "STORE_DIR": store,
        # The source language's listing, which store/check.py reads every other language against.
        "STORE_LISTING": f"{store}/{config.get('languages.source', '')}.md" if store else "",
        "WINDOWS_DIR": windows,
        "WINDOWS_CORE": win_core,
        "WINDOWS_CORE_TESTS": win_core_tests,
        "WINDOWS_APP": config.get("windows.app", ""),
        "WINDOWS_APP_TESTS": win_app_tests,
        "WINDOWS_ASSETS": rel("windows.assets.out"),
        "WINDOWS_STRINGS": rel("windows.strings"),
        "CONFORMANCE_DIR": conformance,
        "VOICE_DIR": rel("voice.dir"),
        "DESKLING_PATH": rel("deskling.path"),
        "DESKLING_TAG": config.get("deskling.tag", ""),
        "GIT_LOCAL_ENV": " ".join(GIT_LOCAL_ENV),  # verify.sh unsets them for git in a submodule
    }
    never = "^$a"  # matches no path: a section the app doesn't have
    values["RE_NEVER"] = never  # verify.sh --all skips a check whose patterns are all this one
    swift_dirs = "|".join(ere(d) for d in (app, tests) if d)
    values["RE_SWIFT"] = (
        f"^({swift_dirs})/.*\\.swift$" + (f"|^{ere(values['MAC_SPEC'])}$" if values["MAC_SPEC"] else "")
        if swift_dirs else never
    )
    values["RE_COREPKG"] = f"^{ere(core)}/" if core else never
    values["RE_PURE"] = f"^{ere(core)}/Sources/|^{ere(core)}/Tests/[^/]+/Conformance/" if core else never
    values["RE_STRINGS"] = "|".join(
        part for part in (
            f"^{ere(app)}/.*\\.swift$" if app else "", "\\.xcstrings$", f"^{ere(landing_i18n)}/" if landing_i18n else ""
        ) if part
    )
    values["RE_CSHARP"] = f"^{ere(windows)}/.*\\.cs$" if windows else never
    values["RE_WINDOWS_CORE"] = (
        f"^{ere(windows)}/{ere(win_core)}(\\.Tests)?/|^{ere(conformance)}/" if windows and win_core else never
    )
    values["RE_WINDOWS_APP"] = f"^{ere(windows)}/{ere(values['WINDOWS_APP'])}/" if windows and values["WINDOWS_APP"] else never
    # The app's model tests compile the app's own files, so either side changing runs them.
    values["RE_WINDOWS_APP_TESTS"] = (
        f"^{ere(windows)}/({ere(values['WINDOWS_APP'])}|{ere(win_app_tests)})/" if windows and win_app_tests else never
    )
    asset_sources = [rel(key) for key in ("windows.assets.source", "windows.assets.devSource") if config.get(key)]
    values["RE_WINDOWS_ASSETS"] = (
        "|".join([f"^{ere(source)}$" for source in asset_sources] + [f"^{ere(values['WINDOWS_ASSETS'])}/"])
        if values["WINDOWS_ASSETS"] else never
    )
    values["RE_LANDING"] = (
        f"^{ere(landing)}/|^{ere(conformance)}/(phrases|settings)\\.json$|Localizable\\.xcstrings$" if landing else never
    )
    values["RE_STORE"] = f"^{ere(store)}/[^/]+\\.md$" if store else never
    values["RE_PHRASES"] = f"^{ere(conformance)}/phrases\\.json$|Localizable\\.xcstrings$"
    values["RE_VOICE"] = (
        f"Localizable\\.xcstrings$|^{ere(values['VOICE_DIR'])}($|/)|^{ere(conformance)}/phrases\\.json$"
        + (f"|^{ere(rel('voice.config'))}$" if config.get("voice.config") else "")
        if values["VOICE_DIR"] else never
    )
    return values


# What `git rev-parse --local-env-vars` lists, less the `-c` settings: the variables that tell git which repository to
# use. A git hook exports them for the app's repository and they win over `-C`.
GIT_LOCAL_ENV = (
    "GIT_ALTERNATE_OBJECT_DIRECTORIES", "GIT_CONFIG", "GIT_OBJECT_DIRECTORY", "GIT_DIR", "GIT_WORK_TREE",
    "GIT_IMPLICIT_WORK_TREE", "GIT_GRAFT_FILE", "GIT_INDEX_FILE", "GIT_NO_REPLACE_OBJECTS", "GIT_REPLACE_REF_BASE",
    "GIT_PREFIX", "GIT_SHALLOW_FILE", "GIT_COMMON_DIR",
)


def git_env() -> dict[str, str]:
    """The environment for git in another repository than the app's (a submodule: `deskling/`, the voices): the
    current one without `GIT_LOCAL_ENV`. Under the pre-commit hook `git -C deskling describe` would otherwise ask the
    app's repository, always so in a linked worktree, where the exported GIT_DIR is absolute."""
    return {key: value for key, value in os.environ.items() if key not in GIT_LOCAL_ENV}


def pins(config: Config) -> list[tuple[str, str]]:
    """Where the Deskling release is pinned and the version each place names: the submodule's tag, companion.json's
    `deskling.tag`, the `exact:` of the core package's Package.swift, the `exactVersion`/`exact` of the XcodeGen spec
    and every `Deskling.*` NuGet reference under the Windows folder. A place the app doesn't have is left out."""
    found: list[tuple[str, str]] = []
    submodule = config.path("deskling.path")
    if submodule and (submodule / ".git").exists():
        described = subprocess.run(["git", "-C", str(submodule), "describe", "--tags", "--exact-match"],
                                   capture_output=True, text=True, env=git_env())
        found.append((f"{config.get('deskling.path')} (submodule tag)", described.stdout.strip() or "(not on a tag)"))
    found.append(("companion.json deskling.tag", config.get("deskling.tag", "")))
    deskling_line = re.compile(r"deskling", re.I)
    for label, path, pattern in (
        ("Package.swift", config.path("mac.core", "mac.dir"), r'exact:\s*"([^"]+)"'),
        ("project.yml", config.path("mac.projectSpec", "mac.dir"), r"(?:exactVersion|exact):\s*\"?([0-9][^\s\"]*)"),
    ):
        if path is None:
            continue
        file = path / "Package.swift" if label == "Package.swift" else path
        if not file.is_file():
            continue
        text = file.read_text("utf8")
        version = None
        for block in re.split(r"\n(?=\s*(?:\.package|\w+:\s*$))", text):
            if deskling_line.search(block) and (match := re.search(pattern, block)):
                version = match.group(1)
                break
        found.append((file.relative_to(config.root).as_posix(), version or "(not pinned to a version)"))
    windows = config.path("windows.dir")
    if windows and windows.is_dir():
        for file in sorted([*windows.rglob("*.csproj"), *windows.rglob("Directory.Packages.props")]):
            for name, version in re.findall(r'Include="(Deskling\.[A-Za-z.]+)"\s+Version="([^"]+)"', file.read_text("utf8")):
                found.append((f"{file.relative_to(config.root).as_posix()} {name}", version))
    return found


def pins_agree(found: list[tuple[str, str]]) -> bool:
    versions = {value.removeprefix("v") for _, value in found}
    return len(versions) == 1 and not next(iter(versions)).startswith("(")


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
    elif command == "env":
        import shlex

        for key, value in derived(config).items():
            print(f"{key}={shlex.quote(value)}")
    elif command == "pins":
        found = pins(config)
        agree = pins_agree(found)
        if not agree:
            print("The Deskling version must be the same everywhere it's pinned:")
        for where, version in found:
            print(f"  {version:<24} {where}")
        return 0 if agree else 1
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
