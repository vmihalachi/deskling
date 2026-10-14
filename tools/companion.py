#!/usr/bin/env python3
"""Reads a companion app's `companion.json`, the one file every tool in `tools/` takes its names and paths from.

    python3 deskling/tools/companion.py get mac.scheme       # a value: strings as they are, the rest as JSON
    python3 deskling/tools/companion.py path voice.dir       # a path from the file, made absolute
    python3 deskling/tools/companion.py root                 # the app's root folder
    python3 deskling/tools/companion.py validate [FILE…]     # check against companion.schema.json (exit 1 on problems)
    python3 deskling/tools/companion.py env                  # paths and path patterns as shell assignments (verify.sh)
    python3 deskling/tools/companion.py pins                 # the Deskling version everywhere it's pinned (exit 1 on a mismatch)
    python3 deskling/tools/companion.py bump X.Y.Z           # every pin, the submodule and the landing page to vX.Y.Z
    python3 deskling/tools/companion.py link [DESKLING]      # build against a Deskling checkout (../deskling) until unlink
    python3 deskling/tools/companion.py unlink               # back to the pinned release, byte for byte

The root is the folder holding `companion.json`: `$COMPANION_ROOT` when set, else the nearest one walking up from the
current directory. Paths in the file are relative to it. Other tools import this module:

    import companion
    config = companion.load()                 # validated; exits with the problems otherwise
    config.get("voice.voices")                # None for an absent or null value
    config.path("l10n.catalog")               # an absolute Path, or None

Standard library only. The schema (`companion.schema.json`) is the source of truth for every section.
"""

from __future__ import annotations

import hashlib
import json
import os
import re
import shutil
import subprocess
import sys
from pathlib import Path
from typing import NamedTuple

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


# MARK: Pins: where the Deskling release is pinned, `bump`, `link` and `unlink`


class PinError(Exception):
    """Why `bump`, `link` or `unlink` refused, and what to do about it."""


class Pin(NamedTuple):
    """A place the Deskling release is pinned and the version it names. A version written in a file carries the file and
    where the version sits in its text, which is what `bump` rewrites."""

    label: str
    version: str
    file: Path | None = None
    span: tuple[int, int] | None = None


def read_text(path: Path) -> str:
    """A file's text with its line endings as they are (a `.csproj` may use CRLF), so a rewrite changes only its edit."""
    return path.read_bytes().decode("utf8")


def write_text(path: Path, text: str) -> None:
    path.write_bytes(text.encode("utf8"))


def _rel(config: Config, path: Path) -> str:
    return path.relative_to(config.root).as_posix()


def _git(folder: Path, *args: str) -> subprocess.CompletedProcess:
    """git in `folder`'s repository, whichever repository a hook exported (`git_env`)."""
    return subprocess.run(["git", "-C", str(folder), *args], capture_output=True, text=True, env=git_env())


def _tail(result: subprocess.CompletedProcess, lines: int = 15) -> str:
    return "\n".join((result.stdout + result.stderr).strip().splitlines()[-lines:])


# A Swift manifest or an XcodeGen spec is read in blocks, cut before each `.package(` line and each YAML key that opens
# a mapping, so the version comes from the block that names Deskling and not from another package's.
_BLOCK = re.compile(r"\n(?=[ \t]*(?:\.package|\w+:[ \t]*$))", re.M)
_PACKAGE_EXACT = r'exact:\s*"([^"]+)"'
_SPEC_EXACT = r"(?:exactVersion|exact):\s*\"?([0-9][^\s\"]*)"
_NUGET = re.compile(r'Include="(Deskling\.[A-Za-z.]+)"\s+Version="([^"]+)"')
_COMPANION_TAG = re.compile(r'"deskling"\s*:\s*\{[^{}]*?"tag"\s*:\s*"([^"]*)"')
_LANDING_PACKAGE = "@deskling/landing"
_LANDING_DEPENDENCY = re.compile(r'"@deskling/landing"\s*:\s*"[~^]?(\d+\.\d+\.\d+)"')
_LOCKFILES = ("package-lock.json", "pnpm-lock.yaml", "yarn.lock", "bun.lock")


def _deskling_block_match(text: str, pattern: str) -> re.Match | None:
    cuts = list(_BLOCK.finditer(text))
    compiled = re.compile(pattern)
    for start, end in zip([0, *(cut.end() for cut in cuts)], [*(cut.start() for cut in cuts), len(text)]):
        if re.search("deskling", text[start:end], re.I) and (match := compiled.search(text, start, end)):
            return match
    return None


def _swift_manifests(config: Config) -> list[Path]:
    """The core package's Package.swift and the XcodeGen spec, those the app has."""
    core, spec = config.path("mac.core", "mac.dir"), config.path("mac.projectSpec", "mac.dir")
    return [file for file in (core / "Package.swift" if core else None, spec) if file and file.is_file()]


def _project_resolved(config: Config) -> Path | None:
    project = config.get("mac.project")
    return config.mac_dir / project / "project.xcworkspace/xcshareddata/swiftpm/Package.resolved" if project else None


def _resolved_files(config: Config) -> list[Path]:
    """Where SwiftPM records the resolved Deskling commit: the core package's Package.resolved and the Xcode project's,
    present or not."""
    core = config.path("mac.core", "mac.dir")
    return [file for file in (core / "Package.resolved" if core else None, _project_resolved(config)) if file]


def _submodule(config: Config) -> Path | None:
    submodule = config.path("deskling.path")
    return submodule if submodule and (submodule / ".git").exists() else None


def _submodule_tag(submodule: Path) -> str:
    return _git(submodule, "describe", "--tags", "--exact-match").stdout.strip()


def pin_sites(config: Config) -> list[Pin]:
    """Every place the Deskling release is pinned: the submodule's tag, companion.json's `deskling.tag`, the `exact:` of
    the core package's Package.swift, the `exactVersion`/`exact` of the XcodeGen spec and every `Deskling.*` NuGet
    reference under the Windows folder. A place the app doesn't have is left out."""
    found: list[Pin] = []
    if submodule := _submodule(config):
        found.append(Pin(f"{config.get('deskling.path')} (submodule tag)", _submodule_tag(submodule) or "(not on a tag)"))
    manifest = config.root / FILE
    match = _COMPANION_TAG.search(read_text(manifest))
    found.append(Pin("companion.json deskling.tag", config.get("deskling.tag", ""), manifest, match.span(1) if match else None))
    for file in _swift_manifests(config):
        match = _deskling_block_match(read_text(file), _PACKAGE_EXACT if file.name == "Package.swift" else _SPEC_EXACT)
        version = match.group(1) if match else "(not pinned to a version)"
        found.append(Pin(_rel(config, file), version, file, match.span(1) if match else None))
    windows = config.path("windows.dir")
    if windows and windows.is_dir():
        for file in sorted([*windows.rglob("*.csproj"), *windows.rglob("Directory.Packages.props")]):
            for match in _NUGET.finditer(read_text(file)):
                found.append(Pin(f"{_rel(config, file)} {match.group(1)}", match.group(2), file, match.span(2)))
    return found


def pins(config: Config) -> list[tuple[str, str]]:
    """Where the Deskling release is pinned and the version each place names (`pin_sites`), plus, while the app is
    linked to a Deskling checkout, a line that never agrees, so nothing linked gets committed."""
    found = [(pin.label, pin.version) for pin in pin_sites(config)]
    if linked := linked_to(config):
        found.append((f"linked to {linked}: `companion.py unlink` before committing", "(linked)"))
    return found


def pins_agree(found: list[tuple[str, str]]) -> bool:
    versions = {value.removeprefix("v") for _, value in found}
    return len(versions) == 1 and not next(iter(versions)).startswith("(")


def landing_pin(config: Config) -> Pin | None:
    """The landing page's `@deskling/landing` version, None without one. Not one of the pins: the package ships only when
    it changed, so its versions skip releases, and `bump` moves it to npm's newest at or below the release."""
    folder = config.path("landing.dir")
    file = folder / "package.json" if folder else None
    if not file or not file.is_file() or not (match := _LANDING_DEPENDENCY.search(read_text(file))):
        return None
    return Pin(f"{_rel(config, file)} {_LANDING_PACKAGE}", match.group(1), file, match.span(1))


def print_pins(config: Config) -> int:
    """`pins` for people: one line per place, the landing package below them; 1 when they disagree."""
    found = pins(config)
    agree = pins_agree(found)
    if not agree:
        print("The Deskling version must be the same everywhere it's pinned:")
    for where, version in found:
        print(f"  {version:<24} {where}")
    if landing := landing_pin(config):
        print(f"  {landing.version:<24} {landing.label} (ships only when it changes: not compared)")
    return 0 if agree else 1


def _version_key(version: str) -> tuple[int, ...]:
    return tuple(int(part) for part in version.split("."))


def _remote_tag(submodule: Path, tag: str) -> str:
    """The commit `tag` names on the submodule's origin; a PinError when origin has no such tag."""
    result = _git(submodule, "ls-remote", "--tags", "origin", f"refs/tags/{tag}*")
    if result.returncode:
        raise PinError(f"couldn't list the tags on Deskling's origin:\n{_tail(result)}")
    refs = dict(reversed(line.split("\t", 1)) for line in result.stdout.splitlines() if "\t" in line)
    commit = refs.get(f"refs/tags/{tag}^{{}}") or refs.get(f"refs/tags/{tag}")  # an annotated tag's commit, peeled
    if not commit:
        raise PinError(f"{tag} isn't a tag on Deskling's origin: release it first")
    return commit


def _repin_resolved(text: str, version: str, commit: str) -> str:
    """A Package.resolved with its `deskling` pin at `version` and `commit`."""

    def state(match: re.Match) -> str:
        body = re.sub(r'("revision"\s*:\s*")[^"]*(")', lambda m: m.group(1) + commit + m.group(2), match.group(0))
        return re.sub(r'("version"\s*:\s*")[^"]*(")', lambda m: m.group(1) + version + m.group(2), body)

    return re.sub(r'"identity"\s*:\s*"deskling"[^{}]*"state"\s*:\s*\{[^{}]*\}', state, text)


def regenerate_project(config: Config) -> str:
    """`xcodegen generate` for the app's spec; a line for the summary."""
    spec = config.path("mac.projectSpec", "mac.dir")
    if not spec or not spec.is_file():
        return ""
    if not shutil.which("xcodegen"):
        return f"xcodegen isn't installed: run `xcodegen generate` in {_rel(config, spec.parent)}"
    result = subprocess.run(["xcodegen", "generate", "--quiet", "--spec", str(spec)], cwd=spec.parent,
                            capture_output=True, text=True)
    if result.returncode:
        raise PinError(f"xcodegen generate failed:\n{_tail(result)}")
    return f"regenerated {config.get('mac.project') or 'the Xcode project'}"


def xcode_resolves(config: Config) -> bool:
    """Whether `resolve_packages` can run: a Mac with Xcode, and an Xcode project with a Package.resolved."""
    resolved = _project_resolved(config)
    return bool(resolved and resolved.is_file() and sys.platform == "darwin" and shutil.which("xcodebuild"))


def resolve_packages(config: Config) -> str:
    """`xcodebuild -resolvePackageDependencies`, which writes the Xcode project's Package.resolved the way a build would.
    `bump` leaves that file to it: its originHash covers the checkout's path, which only SwiftPM computes, and SwiftPM
    rewrites the file only when its pin no longer fits the project's requirement."""
    command = ["xcodebuild", "-resolvePackageDependencies", "-project", config.require("mac.project"),
               "-derivedDataPath", str(config.derived_data)]
    if scheme := config.get("mac.scheme"):
        command += ["-scheme", scheme]
    result = subprocess.run(command, cwd=config.mac_dir, capture_output=True, text=True)
    if result.returncode:
        raise PinError(f"xcodebuild -resolvePackageDependencies failed:\n{_tail(result)}")
    return "resolved the Xcode project's packages"


def npm_versions(package: str) -> list[str]:
    """Every version of `package` on npm."""
    if not shutil.which("npm"):
        raise PinError("npm isn't installed")
    result = subprocess.run(["npm", "view", package, "versions", "--json"], capture_output=True, text=True, timeout=120)
    if result.returncode:
        raise PinError(f"npm didn't answer:\n{_tail(result, 5)}")
    versions = json.loads(result.stdout)
    return versions if isinstance(versions, list) else [versions]


def _bump_landing(config: Config, version: str) -> tuple[list[str], list[str]]:
    """Moves `@deskling/landing` to npm's newest version at or below `version`, and its lockfile with the landing tool's
    `install` when that's safe: the tool is installed and neither file has uncommitted changes to mix in. What changed,
    and notes."""
    pin = landing_pin(config)
    if pin is None or pin.file is None or pin.span is None:
        return [], []
    try:
        published = [v for v in npm_versions(_LANDING_PACKAGE) if re.fullmatch(r"\d+\.\d+\.\d+", v)]
    except PinError as error:
        return [], [f"{_LANDING_PACKAGE} stays at {pin.version}: {error}"]
    target = max((v for v in published if _version_key(v) <= _version_key(version)), key=_version_key, default=None)
    if target is None or _version_key(target) <= _version_key(pin.version):
        return [], [f"{_LANDING_PACKAGE} stays at {pin.version}: npm has nothing newer at or below {version} "
                    "(it ships only when it changes, and a staged version waits for approval)"]
    folder = pin.file.parent
    lockfile = next((folder / name for name in _LOCKFILES if (folder / name).is_file()), None)
    files = [pin.file, *([lockfile] if lockfile else [])]
    status = _git(folder, "status", "--porcelain", "--", *(file.name for file in files))
    dirty = status.returncode == 0 and status.stdout.strip()
    originals = {file: file.read_bytes() for file in files}
    text = read_text(pin.file)
    write_text(pin.file, text[: pin.span[0]] + target + text[pin.span[1]:])
    changed = [f"{pin.label}: {pin.version} → {target}"]
    tool = config.get("landing.tool")
    where = _rel(config, folder)
    if not tool or not shutil.which(tool):
        return changed, [f"update the lockfile: `{tool or 'npm'} install` in {where}"]
    if dirty:
        return changed, [f"{where} has uncommitted dependency changes, so the lockfile is yours: `{tool} install` there"]
    result = subprocess.run([tool, "install"], cwd=folder, capture_output=True, text=True)
    if result.returncode:
        for file, data in originals.items():
            file.write_bytes(data)
        return [], [f"`{tool} install` in {where} failed, so {_LANDING_PACKAGE} stays at {pin.version}:\n{_tail(result)}"]
    if lockfile and lockfile.read_bytes() != originals[lockfile]:
        changed.append(f"{_rel(config, lockfile)} (`{tool} install`)")
    return changed, []


def bump(config: Config, version: str) -> int:
    """Moves the app to Deskling `version`: every pin `pin_sites` finds, the Package.resolved files, the submodule (to the
    tag) and the landing page's package. Refuses while linked, or when origin has no such tag. Prints what changed and
    the pins; 1 when they still disagree."""
    version = version.removeprefix("v")
    if not re.fullmatch(r"\d+\.\d+\.\d+", version):
        raise PinError(f"{version!r} isn't a version (X.Y.Z)")
    if linked := linked_to(config):
        raise PinError(f"linked to {linked}: run `companion.py unlink` first")
    tag = f"v{version}"
    submodule = _submodule(config)
    if submodule is None:
        raise PinError(f"bump needs the Deskling submodule at {config.get('deskling.path') or 'deskling.path'}")
    commit = _remote_tag(submodule, tag)
    move_submodule = _submodule_tag(submodule) != tag
    if move_submodule:
        if _git(submodule, "status", "--porcelain").stdout.strip():
            raise PinError(f"{_rel(config, submodule)} has uncommitted changes, which would ride along to {tag}")
        fetch = _git(submodule, "fetch", "--quiet", "origin", "tag", tag)
        if fetch.returncode:
            raise PinError(f"couldn't fetch {tag} into {_rel(config, submodule)}:\n{_tail(fetch)}")

    edits: dict[Path, list[tuple[tuple[int, int], str]]] = {}
    for pin in pin_sites(config):
        if pin.file is None:
            continue
        if pin.span is None:
            raise PinError(f"{pin.label} names no version to move ({pin.version or 'empty'})")
        new = ("v" if pin.version.startswith("v") else "") + version
        if pin.version != new:
            edits.setdefault(pin.file, []).append((pin.span, new))
    changed: list[str] = []
    for file, replacements in edits.items():
        text = read_text(file)
        for (start, end), new in sorted(replacements, reverse=True):
            text = text[:start] + new + text[end:]
        write_text(file, text)
        changed.append(_rel(config, file))
    notes: list[str] = []
    core = config.path("mac.core", "mac.dir")
    swift_moved = any(file in edits for file in _swift_manifests(config))
    by_xcode = _project_resolved(config) if swift_moved and xcode_resolves(config) else None
    for file in _resolved_files(config):
        if not file.is_file() or file == by_xcode:
            continue
        text = read_text(file)
        repinned = _repin_resolved(text, version, commit)
        if file == _project_resolved(config) and swift_moved:
            notes.append(f"{_rel(config, file)}: run `xcodebuild -resolvePackageDependencies` on a Mac for its originHash")
        if core and file == core / "Package.resolved" and core / "Package.swift" in edits:
            # A package's originHash is the SHA-256 of its manifest. The Xcode project's also covers the checkout's
            # path, so that one is left to Xcode (resolve_packages).
            digest = hashlib.sha256((core / "Package.swift").read_bytes()).hexdigest()
            repinned = re.sub(r'("originHash"\s*:\s*")[0-9a-f]*(")', lambda m: m.group(1) + digest + m.group(2),
                              repinned, count=1)
        if repinned != text:
            write_text(file, repinned)
            changed.append(_rel(config, file))
    if move_submodule:
        checkout = _git(submodule, "checkout", "--quiet", tag)
        if checkout.returncode:
            raise PinError(f"couldn't check out {tag} in {_rel(config, submodule)}:\n{_tail(checkout)}")
        changed.append(f"{_rel(config, submodule)} → {tag}")

    spec = config.path("mac.projectSpec", "mac.dir")
    if spec in edits:
        notes.append(regenerate_project(config))
    if by_xcode:
        before = by_xcode.read_bytes()
        notes.append(resolve_packages(config))
        if by_xcode.read_bytes() != before:
            changed.append(_rel(config, by_xcode))
        if _repin_resolved(read_text(by_xcode), version, commit) != read_text(by_xcode):
            notes.append(f"{_rel(config, by_xcode)} doesn't name {tag} yet: build once in Xcode")
    landing_changed, landing_notes = _bump_landing(config, version)
    changed += landing_changed
    notes += landing_notes
    config = load(config.root)  # companion.json may have changed
    if changed:
        print(f"Deskling {tag}:")
        for line in changed:
            print(f"  changed  {line}")
    else:
        print(f"Deskling {tag}: every pin already names it, nothing changed")
    for note in filter(None, notes):
        print(f"  note     {note}")
    return print_pins(config)


# The link: the files `link` changed and what they held, kept in the repository's git folder (out of the work tree, one
# per worktree) so `unlink` can put them back.
_LINK_DIR = "deskling-link"
LINK_MARKER = "deskling link"


class _LinkEdit(NamedTuple):
    file: Path
    start: int
    end: int
    new: str


def _link_dir(config: Config) -> Path:
    result = _git(config.root, "rev-parse", "--git-path", _LINK_DIR)
    if result.returncode == 0 and result.stdout.strip():
        path = Path(result.stdout.strip())
        return path if path.is_absolute() else (config.root / path).resolve()
    return config.root / f".{_LINK_DIR}"


def linked_to(config: Config) -> str | None:
    """The Deskling checkout the app is linked to (`link`), None when it builds against the pinned release."""
    state = _link_dir(config) / "state.json"
    if state.is_file():
        return json.loads(state.read_text("utf8"))["path"]
    windows = config.path("windows.dir")
    targets = windows / "Directory.Build.targets" if windows else None
    if targets and targets.is_file() and LINK_MARKER in read_text(targets):
        return "a Deskling checkout (Directory.Build.targets)"
    return None


def _link_path(target: Path, base: Path) -> str:
    """`target` relative to `base` when they share a folder below the root (a sibling checkout: ../../deskling), else
    absolute."""
    if os.path.commonpath([target, base]) == os.path.dirname(os.path.commonpath([target, base])):
        return target.as_posix()
    return os.path.relpath(target, base).replace(os.sep, "/")


def _link_msbuild(deskling: Path, windows: Path) -> str:
    """The MSBuild that swaps each `Deskling.*` PackageReference for a ProjectReference to the checkout's project of the
    same name. A Directory.Build.targets is read after the project's own items, so the projects stay as they are."""
    dotnet = deskling / "dotnet"
    names = sorted(file.stem for file in dotnet.glob("*/*.csproj")
                   if file.stem == file.parent.name and "<IsPackable>true</IsPackable>" in read_text(file))
    if not names:
        raise PinError(f"{dotnet} has no packable Deskling project")
    path = _link_path(dotnet, windows)
    prefix = "" if os.path.isabs(path) else "$(MSBuildThisFileDirectory)"
    return (
        f"  <!-- {LINK_MARKER}: Deskling's packages are the projects in {path}, not NuGet's, until\n"
        "       `companion.py unlink` takes this out. The pin check fails while it's here. -->\n"
        "  <PropertyGroup>\n"
        f"    <DesklingLinkedDotnet>{prefix}{path}/</DesklingLinkedDotnet>\n"
        "  </PropertyGroup>\n"
        "  <ItemGroup>\n"
        f'    <_DesklingLinkable Include="{";".join(names)}" />\n'
        '    <_DesklingNotLinked Include="@(PackageReference)" Exclude="@(_DesklingLinkable)" />\n'
        '    <_DesklingLinked Include="@(PackageReference)" Exclude="@(_DesklingNotLinked)" />\n'
        '    <PackageReference Remove="@(_DesklingLinked)" />\n'
        "    <ProjectReference Include=\"@(_DesklingLinked->'$(DesklingLinkedDotnet)%(Identity)/%(Identity).csproj')\" />\n"
        "  </ItemGroup>\n"
    )


def link(config: Config, checkout: Path | None = None) -> int:
    """Switches the app from the pinned release to a Deskling checkout (`../deskling` next to the app by default): the
    XcodeGen spec's and the core package's Deskling become `path:` packages (the project regenerated), and the Windows
    projects build Deskling's projects instead of its NuGet packages. The pins fail until `unlink`."""
    deskling = (checkout or config.root.parent / "deskling").expanduser().resolve()
    manifest = deskling / "Package.swift"
    if not manifest.is_file() or 'name: "Deskling"' not in read_text(manifest):
        raise PinError(f"{deskling} isn't a Deskling checkout (no Package.swift naming Deskling)")
    if linked := linked_to(config):
        raise PinError(f"already linked to {linked}: `unlink` first")
    edits: list[_LinkEdit] = []
    spec = config.path("mac.projectSpec", "mac.dir")
    if spec and spec.is_file():
        match = re.search(r"^(?P<indent>[ \t]+)url:[ \t]*\S*(?i:deskling)\S*[ \t]*\n(?P=indent)"
                          r"(?:exactVersion|exact|version|from|minorVersion|majorVersion):[ \t]*\S+[ \t]*$",
                          read_text(spec), re.M)
        if not match:
            raise PinError(f"{_rel(config, spec)}: no Deskling package with a url and a version to switch")
        edits.append(_LinkEdit(spec, *match.span(), f"{match['indent']}path: {_link_path(deskling, spec.parent)}"))
    core = config.path("mac.core", "mac.dir")
    if core and (core / "Package.swift").is_file():
        match = re.search(r'\.package\(\s*url:\s*"[^"]*(?i:deskling)[^"]*"\s*,\s*exact:\s*"[^"]*"\s*\)',
                          read_text(core / "Package.swift"))
        if not match:
            raise PinError(f"{_rel(config, core / 'Package.swift')}: no Deskling `.package(url:…, exact:…)` to switch")
        edits.append(_LinkEdit(core / "Package.swift", *match.span(), f'.package(path: "{_link_path(deskling, core)}")'))
    windows = config.path("windows.dir")
    if windows and any(pin.file and pin.file.is_relative_to(windows) for pin in pin_sites(config)):
        targets = windows / "Directory.Build.targets"
        block = _link_msbuild(deskling, windows)
        if targets.is_file():
            text = read_text(targets)
            if (at := text.rfind("</Project>")) < 0:
                raise PinError(f"{_rel(config, targets)} has no </Project> to add the link before")
            edits.append(_LinkEdit(targets, at, at, block.replace("\n", "\r\n") if "\r\n" in text else block))
        else:
            edits.append(_LinkEdit(targets, 0, 0, f"<Project>\n{block}</Project>\n"))
    if not edits:
        raise PinError("nothing here pins Deskling to link")

    # What SwiftPM and XcodeGen derive from the edited files: put back as they were, whatever the linked builds wrote.
    project = config.get("mac.project")
    derived = [*_resolved_files(config), *([config.mac_dir / project / "project.pbxproj"] if project and spec else [])]
    folder = _link_dir(config)
    entries = []
    for file in [*(edit.file for edit in edits), *derived]:
        if file.is_file():
            saved = folder / "files" / _rel(config, file)
            saved.parent.mkdir(parents=True, exist_ok=True)
            saved.write_bytes(file.read_bytes())
    for edit in edits:
        text = read_text(edit.file) if edit.file.is_file() else ""
        linked = text[: edit.start] + edit.new + text[edit.end:]
        entries.append({"path": _rel(config, edit.file), "original": text[edit.start: edit.end], "linked": edit.new,
                        "linkedHash": hashlib.sha256(linked.encode("utf8")).hexdigest()})
        write_text(edit.file, linked)
    entries += [{"path": _rel(config, file), "derived": True} for file in derived]
    (folder / "state.json").write_text(json.dumps({"path": str(deskling), "files": entries}, indent=2) + "\n", "utf8")

    print(f"Linked to {deskling}:")
    for edit in edits:
        print(f"  changed  {_rel(config, edit.file)}")
    if spec and (note := regenerate_project(config)):
        print(f"  note     {note}")
    if deskling.name.lower() != "deskling":
        print(f"  note     SwiftPM names a local package after its folder, so the products' package \"deskling\" needs "
              f"the checkout's folder named deskling, not {deskling.name}")
    print("Build and test as usual; `companion.py unlink` before committing.")
    print_pins(config)
    return 0


def unlink(config: Config) -> int:
    """Undoes `link`: each file it changed gets its bytes back (only the link's own edit is undone in a file changed
    since), the files SwiftPM and XcodeGen derive are put back, and the project is regenerated."""
    folder = _link_dir(config)
    state_file = folder / "state.json"
    if not state_file.is_file():
        if linked := linked_to(config):
            raise PinError(f"linked to {linked}, but there's no record of the link to undo: put the files back by hand")
        print("Not linked: nothing to undo.")
        return 0
    state = json.loads(state_file.read_text("utf8"))
    problems: list[str] = []
    restored: list[str] = []
    for entry in state["files"]:
        file = config.root / entry["path"]
        saved = folder / "files" / entry["path"]
        original = saved.read_bytes() if saved.is_file() else None
        current = file.read_bytes() if file.is_file() else None
        if entry.get("derived") or (current is not None and hashlib.sha256(current).hexdigest() == entry["linkedHash"]):
            if current == original:
                continue
            if original is None:
                file.unlink()
                restored.append(f"{entry['path']} (removed)")
            else:
                file.parent.mkdir(parents=True, exist_ok=True)
                file.write_bytes(original)
                restored.append(entry["path"])
        elif current is not None and entry["linked"] and current.decode("utf8").count(entry["linked"]) == 1:
            # Changed since the link (another edit to the same file): take out only the link's edit.
            text = current.decode("utf8").replace(entry["linked"], entry["original"], 1)
            if original is None and not text.strip():
                file.unlink()
            else:
                write_text(file, text)
            restored.append(f"{entry['path']} (the link's edit only: the file changed since)")
        else:
            problems.append(f"{entry['path']}: the link's edit is gone, so put back by hand:\n{entry['original']}")
    shutil.rmtree(folder)
    print("Unlinked:" if restored else "Unlinked: every file was already back")
    for line in restored:
        print(f"  restored {line}")
    spec = config.path("mac.projectSpec", "mac.dir")
    if spec and any(entry["path"] == _rel(config, spec) for entry in state["files"]):
        try:
            note = regenerate_project(config)
        except PinError as error:
            problems.append(str(error))
        else:
            if note:
                print(f"  note     {note}")
    for problem in problems:
        print(f"  problem  {problem}")
    status = print_pins(config)
    return 1 if problems else status


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
        return print_pins(config)
    elif (command, len(rest)) in (("bump", 1), ("link", 0), ("link", 1), ("unlink", 0)):
        try:
            if command == "bump":
                return bump(config, rest[0])
            if command == "link":
                return link(config, Path(rest[0]) if rest else None)
            return unlink(config)
        except PinError as error:
            print(f"companion {command}: {error}", file=sys.stderr)
            return 1
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
