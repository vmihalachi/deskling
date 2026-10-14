#!/usr/bin/env python3
"""Tests for companion.py's `bump`, `link` and `unlink`, over the fixture app and a Deskling origin made here (git
repositories in a temporary folder; xcodegen, xcodebuild and npm are stood in for)."""

from __future__ import annotations

import hashlib
import io
import json
import os
import subprocess
import sys
import unittest
from contextlib import ExitStack, redirect_stderr, redirect_stdout
from pathlib import Path
from unittest import mock

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
import companion  # noqa: E402
from support import TOOLS, fixture_app  # noqa: E402

RESOLVED = "macos/FixtureCore/Package.resolved"
PROJECT_RESOLVED = "macos/Fixture.xcodeproj/project.xcworkspace/xcshareddata/swiftpm/Package.resolved"
PBXPROJ = "macos/Fixture.xcodeproj/project.pbxproj"


def git(cwd: Path, *args: str) -> str:
    env = {key: value for key, value in os.environ.items() if not key.startswith("GIT_")}
    command = ["git", "-c", "user.name=Fixture", "-c", "user.email=fixture@example.com", "-c", "init.defaultBranch=main",
               "-c", "tag.gpgSign=false", "-c", "commit.gpgSign=false", *args]
    return subprocess.run(command, cwd=cwd, env=env, check=True, capture_output=True, text=True).stdout.strip()


def resolved(revision: str, version: str, origin_hash: str) -> str:
    """A Package.resolved as SwiftPM writes it, pinning Deskling."""
    return (
        "{\n"
        f'  "originHash" : "{origin_hash}",\n'
        '  "pins" : [\n    {\n      "identity" : "deskling",\n      "kind" : "remoteSourceControl",\n'
        '      "location" : "https://github.com/vmihalachi/deskling",\n      "state" : {\n'
        f'        "revision" : "{revision}",\n        "version" : "{version}"\n'
        "      }\n    }\n  ],\n  \"version\" : 3\n}\n"
    )


class PinsTestCase(unittest.TestCase):
    """The fixture app at `self.root` and a Deskling origin next to it, tagged v0.5.0 and v0.6.0. xcodegen and xcodebuild
    are stood in for (`self.calls` records them), and npm has the versions in `self.npm`."""

    def setUp(self):
        stack = ExitStack()
        self.addCleanup(stack.close)
        self.root = stack.enter_context(fixture_app())
        self.origin = self.root.parent / "deskling-origin"
        self.origin.mkdir()
        (self.origin / "Package.swift").write_text('let package = Package(name: "Deskling")\n', "utf8")
        for name in ("Deskling.Core", "Deskling.Companion", "Deskling.Core.Tests"):
            project = self.origin / "dotnet" / name / f"{name}.csproj"
            project.parent.mkdir(parents=True)
            packable = "" if name.endswith("Tests") else "<IsPackable>true</IsPackable>"
            project.write_text(f"<Project><PropertyGroup>{packable}</PropertyGroup></Project>\n", "utf8")
        git(self.origin, "init", "-q")
        self.commits = {}
        for version in ("0.5.0", "0.6.0"):
            (self.origin / "CHANGELOG.md").write_text(f"## {version}\n", "utf8")
            git(self.origin, "add", "-A")
            git(self.origin, "commit", "-q", "-m", version)
            git(self.origin, "tag", "-a", f"v{version}", "-m", f"Deskling {version}")
            self.commits[version] = git(self.origin, "rev-parse", "HEAD")
        self.calls: list[str] = []
        self.npm: list[str] = []
        for name, value in (("regenerate_project", "regenerated"), ("resolve_packages", "resolved")):
            stack.enter_context(mock.patch.object(companion, name, side_effect=lambda config, n=name, v=value: (
                self.calls.append(n), v)[1]))
        stack.enter_context(mock.patch.object(companion, "xcode_resolves", return_value=False))
        stack.enter_context(mock.patch.object(companion, "npm_versions", side_effect=lambda package: self.npm))

    def cli(self, *argv: str) -> tuple[int, str]:
        out = io.StringIO()
        with redirect_stdout(out), redirect_stderr(out):
            code = companion.main(list(argv))
        return code, out.getvalue()

    def files(self) -> dict[str, bytes]:
        """Every file of the app but git's own, with its bytes."""
        return {path.relative_to(self.root).as_posix(): path.read_bytes() for path in sorted(self.root.rglob("*"))
                if path.is_file() and ".git" not in path.relative_to(self.root).parts}


class BumpTests(PinsTestCase):
    def setUp(self):
        super().setUp()
        git(self.root.parent, "clone", "-q", str(self.origin), str(self.root / "deskling"))
        git(self.root / "deskling", "checkout", "-q", "v0.5.0")
        manifest = (self.root / "macos/FixtureCore/Package.swift").read_bytes()
        (self.root / RESOLVED).write_text(
            resolved(self.commits["0.5.0"], "0.5.0", hashlib.sha256(manifest).hexdigest()), "utf8")
        (self.root / PROJECT_RESOLVED).parent.mkdir(parents=True)
        (self.root / PROJECT_RESOLVED).write_text(resolved(self.commits["0.5.0"], "0.5.0", "f" * 64), "utf8")

    def test_bump_moves_every_pin(self):
        before = self.files()
        code, out = self.cli("bump", "0.6.0")
        self.assertEqual(code, 0, out)
        config = companion.load()
        self.assertTrue(companion.pins_agree(companion.pins(config)))
        self.assertEqual({version.removeprefix("v") for _, version in companion.pins(config)}, {"0.6.0"})
        self.assertEqual(git(self.root / "deskling", "describe", "--tags"), "v0.6.0")
        self.assertEqual(config.get("deskling.tag"), "v0.6.0")
        after = self.files()
        # Only the versions changed: each file differs from its old self by the version strings alone.
        for path in ("companion.json", "macos/project.yml", "macos/FixtureCore/Package.swift",
                     "windows/Fixture.Core/Fixture.Core.csproj"):
            self.assertEqual(after[path].replace(b"0.6.0", b"0.5.0"), before[path], path)
        core = json.loads(after[RESOLVED])
        self.assertEqual(core["pins"][0]["state"], {"revision": self.commits["0.6.0"], "version": "0.6.0"})
        self.assertEqual(core["originHash"], hashlib.sha256(after["macos/FixtureCore/Package.swift"]).hexdigest())
        project = json.loads(after[PROJECT_RESOLVED])
        self.assertEqual(project["pins"][0]["state"], {"revision": self.commits["0.6.0"], "version": "0.6.0"})
        self.assertEqual(project["originHash"], "f" * 64, "the project's hash covers its path: left to Xcode")
        self.assertIn("originHash", out)
        self.assertEqual(self.calls, ["regenerate_project"])

    def test_bump_to_the_current_version_changes_nothing(self):
        before = self.files()
        code, out = self.cli("bump", "v0.5.0")
        self.assertEqual(code, 0, out)
        self.assertIn("nothing changed", out)
        self.assertEqual(self.files(), before)
        self.assertEqual(self.calls, [])

    def test_xcode_rewrites_the_projects_package_resolved(self):
        def resolve(config):
            self.calls.append("resolve_packages")
            (self.root / PROJECT_RESOLVED).write_text(resolved(self.commits["0.6.0"], "0.6.0", "a" * 64), "utf8")
            return "resolved"

        with mock.patch.object(companion, "xcode_resolves", return_value=True), \
                mock.patch.object(companion, "resolve_packages", side_effect=resolve):
            code, out = self.cli("bump", "0.6.0")
        self.assertEqual(code, 0, out)
        self.assertEqual(self.calls, ["regenerate_project", "resolve_packages"])
        self.assertEqual(json.loads((self.root / PROJECT_RESOLVED).read_text("utf8"))["originHash"], "a" * 64)
        self.assertIn(f"changed  {PROJECT_RESOLVED}", out)

    def test_bump_refuses_a_tag_origin_doesnt_have(self):
        before = self.files()
        for version in ("0.7.0", "0.6"):
            code, out = self.cli("bump", version)
            self.assertEqual(code, 1)
            self.assertIn("release it first" if version == "0.7.0" else "isn't a version", out)
        self.assertEqual(self.files(), before)
        self.assertEqual(git(self.root / "deskling", "describe", "--tags"), "v0.5.0")

    def test_bump_refuses_a_submodule_with_changes(self):
        (self.root / "deskling" / "CHANGELOG.md").write_text("mine\n", "utf8")
        code, out = self.cli("bump", "0.6.0")
        self.assertEqual(code, 1)
        self.assertIn("uncommitted changes", out)
        self.assertEqual(companion.load().get("deskling.tag"), "v0.5.0")

    def test_bump_keeps_crlf_line_endings(self):
        csproj = self.root / "windows/Fixture.Core/Fixture.Core.csproj"
        csproj.write_bytes(csproj.read_bytes().replace(b"\r\n", b"\n").replace(b"\n", b"\r\n"))
        self.cli("bump", "0.6.0")
        text = csproj.read_bytes()
        self.assertIn(b'Version="0.6.0"', text)
        self.assertEqual(text.count(b"\n"), text.count(b"\r\n"))

    def landing(self, tool: str | None = None) -> Path:
        """A landing page on @deskling/landing 0.4.0, its lockfile and, with `tool`, a landing tool at that path."""
        landing = self.root / "landing"
        landing.mkdir()
        (landing / "package.json").write_text('{\n  "devDependencies": {\n    "@deskling/landing": "0.4.0"\n  }\n}\n')
        (landing / "package-lock.json").write_text('{ "lock": "0.4.0" }\n', "utf8")
        data = json.loads((self.root / "companion.json").read_text("utf8"))
        data["landing"] = {"dir": "landing", **({"tool": tool} if tool else {})}
        (self.root / "companion.json").write_text(json.dumps(data, indent=2) + "\n", "utf8")
        return landing

    def test_the_landing_page_takes_npms_newest_at_or_below_the_release(self):
        landing = self.landing()
        self.npm = ["0.0.0-stage", "0.4.0", "0.5.0", "0.5.1", "0.7.0"]
        code, out = self.cli("bump", "0.6.0")
        self.assertEqual(code, 0, out)
        self.assertIn('"@deskling/landing": "0.5.1"', (landing / "package.json").read_text("utf8"))
        self.assertIn("`npm install` in landing", out, "no landing tool: the lockfile is left to you")
        self.assertIn("0.5.1                    landing/package.json @deskling/landing", out)
        self.npm = ["0.4.0"]
        code, out = self.cli("bump", "0.6.0")
        self.assertIn("@deskling/landing stays at 0.5.1", out)

    def test_the_landing_tool_updates_the_lockfile_or_everything_goes_back(self):
        tool = self.root.parent / "fake-vp"
        landing = self.landing(str(tool))
        self.npm = ["0.4.0", "0.6.0"]
        (landing / "pnpm-workspace.yaml").write_text("minimumReleaseAgeExclude: []\n", "utf8")
        git(landing, "init", "-q")
        git(landing, "add", "-A")
        git(landing, "commit", "-q", "-m", "landing")
        tool.write_text('#!/bin/sh\n[ "$1" = install ] && echo \'{ "lock": "0.6.0" }\' > package-lock.json'
                        ' && echo "minimumReleaseAgeExclude: [0.6.0]" > pnpm-workspace.yaml\n', "utf8")
        tool.chmod(0o755)
        code, out = self.cli("bump", "0.6.0")
        self.assertEqual(code, 0, out)
        self.assertIn("0.6.0", (landing / "package-lock.json").read_text("utf8"))
        self.assertIn("changed  landing/package-lock.json (`", out)
        self.assertIn("changed  landing/pnpm-workspace.yaml (`", out)
        git(landing, "commit", "-q", "-am", "0.6.0")

        self.cli("bump", "0.5.0")  # npm has 0.4.0 at or below it, older than the page's: it stays
        self.assertIn('"0.6.0"', (landing / "package.json").read_text("utf8"))
        (landing / "package.json").write_text('{ "devDependencies": { "@deskling/landing": "0.4.0" } }\n', "utf8")
        (landing / "package-lock.json").write_text('{ "lock": "0.4.0" }\n', "utf8")
        tool.write_text("#!/bin/sh\necho 'no network' >&2\nexit 1\n", "utf8")
        code, out = self.cli("bump", "0.6.0")
        self.assertIn("landing has uncommitted dependency changes, so the lockfile is yours", out)
        (landing / "package.json").write_text('{ "devDependencies": { "@deskling/landing": "0.4.0" } }\n', "utf8")
        git(landing, "commit", "-q", "-am", "back to 0.4.0")
        code, out = self.cli("bump", "0.6.0")
        self.assertIn("failed, so @deskling/landing stays at 0.4.0", out)
        self.assertIn('"0.4.0"', (landing / "package.json").read_text("utf8"))
        self.assertIn('"0.4.0"', (landing / "package-lock.json").read_text("utf8"))

    def bump_apps(self, *args: str) -> subprocess.CompletedProcess:
        env = {key: value for key, value in os.environ.items() if key != "COMPANION_ROOT" and not key.startswith("GIT_")}
        return subprocess.run(["bash", str(TOOLS / "bump-apps.sh"), *args], capture_output=True, text=True, env=env)

    def test_bump_apps_bumps_then_verifies_each_app(self):
        failing = self.bump_apps("0.7.0", str(self.root))
        self.assertEqual(failing.returncode, 1)
        self.assertIn("v0.7.0 isn't a tag", failing.stderr)
        self.assertIn("bump failed    app", failing.stdout)
        passing = self.bump_apps("0.5.0", str(self.root))
        self.assertEqual(passing.returncode, 0, passing.stdout + passing.stderr)
        self.assertIn("ok    Deskling pins agree", passing.stdout)
        self.assertIn("ok             app", passing.stdout)
        self.assertEqual(self.bump_apps().returncode, 2)

    def test_bump_apps_finds_the_apps_next_to_its_checkout_but_not_worktrees(self):
        tools = self.root.parent / "checkout" / "tools"
        tools.mkdir(parents=True)
        for name in ("bump-apps.sh", "companion.py", "companion.schema.json"):
            (tools / name).write_bytes((TOOLS / name).read_bytes())
        worktree = self.root.parent / "app-worktree"
        worktree.mkdir()
        (worktree / "companion.json").write_text("{}", "utf8")
        (worktree / ".git").write_text("gitdir: elsewhere\n", "utf8")
        result = subprocess.run(["bash", str(tools / "bump-apps.sh"), "0.7.0"], capture_output=True, text=True,
                                env={key: value for key, value in os.environ.items() if key != "COMPANION_ROOT"})
        self.assertIn("Skipping app-worktree: a git worktree", result.stdout)
        self.assertIn("bump failed    app", result.stdout)
        self.assertNotIn("== app-worktree", result.stdout)


class LinkTests(PinsTestCase):
    """The app is a git repository (committed) and the Deskling checkout sits next to it, at ../deskling."""

    def setUp(self):
        super().setUp()
        self.checkout = self.root.parent / "deskling"
        git(self.root.parent, "clone", "-q", str(self.origin), str(self.checkout))
        for path, text in ((RESOLVED, resolved(self.commits["0.5.0"], "0.5.0", "c" * 64)),
                           (PBXPROJ, "// the project XcodeGen wrote\n")):
            (self.root / path).parent.mkdir(parents=True, exist_ok=True)
            (self.root / path).write_text(text, "utf8")
        git(self.root, "init", "-q")
        git(self.root, "add", "-A")
        git(self.root, "commit", "-q", "-m", "app")

    def status(self) -> str:
        return git(self.root, "status", "--porcelain", "--untracked-files=all")

    def test_link_switches_every_side_and_unlink_puts_back_every_byte(self):
        code, out = self.cli("link")
        self.assertEqual(code, 0, out)
        self.assertIn("path: ../../deskling", (self.root / "macos/project.yml").read_text("utf8"))
        self.assertNotIn("exactVersion", (self.root / "macos/project.yml").read_text("utf8"))
        self.assertIn('.package(path: "../../../deskling")',
                      (self.root / "macos/FixtureCore/Package.swift").read_text("utf8"))
        targets = (self.root / "windows/Directory.Build.targets").read_text("utf8")
        self.assertIn('<_DesklingLinkable Include="Deskling.Companion;Deskling.Core" />', targets)
        self.assertIn("$(MSBuildThisFileDirectory)../../deskling/dotnet/", targets)
        self.assertEqual(self.calls, ["regenerate_project"])

        code, out = self.cli("pins")
        self.assertEqual(code, 1, "a linked app can't be committed")
        self.assertIn(f"linked to {self.checkout}", out)
        code, out = self.cli("bump", "0.6.0")
        self.assertEqual(code, 1)
        self.assertIn("unlink", out)
        code, out = self.cli("link")
        self.assertEqual(code, 1)
        self.assertIn("already linked", out)

        # What the linked builds write: SwiftPM drops the remote pin, Xcode records a project Package.resolved.
        (self.root / RESOLVED).write_text('{ "pins" : [ ], "version" : 3 }\n', "utf8")
        (self.root / PROJECT_RESOLVED).parent.mkdir(parents=True)
        (self.root / PROJECT_RESOLVED).write_text('{ "pins" : [ ] }\n', "utf8")
        (self.root / PBXPROJ).write_text("// the linked project\n", "utf8")
        code, out = self.cli("unlink")
        self.assertEqual(code, 0, out)
        self.assertEqual(self.status(), "")
        self.assertFalse((self.root / ".git" / "deskling-link").exists())
        self.assertEqual(self.calls, ["regenerate_project", "regenerate_project"])
        self.assertEqual(self.cli("pins")[0], 0)
        self.assertIn("Not linked", self.cli("unlink")[1])

    def test_unlink_keeps_what_else_changed_meanwhile(self):
        self.cli("link")
        spec = self.root / "macos/project.yml"
        spec.write_text(spec.read_text("utf8") + "# added while linked\n", "utf8")
        code, out = self.cli("unlink")
        self.assertEqual(code, 0, out)
        self.assertIn("the link's edit only", out)
        self.assertEqual(self.status(), "M macos/project.yml")
        self.assertEqual(git(self.root, "diff", "--unified=0", "macos/project.yml").splitlines()[-1],
                         "+# added while linked")

    def test_an_existing_targets_file_gets_the_link_and_loses_it(self):
        targets = self.root / "windows/Directory.Build.targets"
        targets.write_bytes(b"<Project>\r\n  <PropertyGroup />\r\n</Project>\r\n")
        git(self.root, "add", "-A")
        git(self.root, "commit", "-q", "-m", "targets")
        self.cli("link")
        text = targets.read_bytes().decode("utf8")
        self.assertTrue(text.startswith("<Project>\r\n  <PropertyGroup />\r\n  <!-- deskling link"))
        self.assertTrue(text.endswith("</ItemGroup>\r\n</Project>\r\n"))
        self.assertEqual(text.count("\n"), text.count("\r\n"), "the file's own line endings")
        self.cli("unlink")
        self.assertEqual(self.status(), "")

    def test_link_takes_another_checkout_and_refuses_what_isnt_one(self):
        code, out = self.cli("link", str(self.root.parent / "deskling-origin"))
        self.assertEqual(code, 0, out)
        self.assertIn("path: ../../deskling-origin", (self.root / "macos/project.yml").read_text("utf8"))
        self.assertIn("named deskling, not deskling-origin", out)
        self.cli("unlink")
        code, out = self.cli("link", str(self.root / "macos"))
        self.assertEqual(code, 1)
        self.assertIn("isn't a Deskling checkout", out)
        self.assertEqual(self.status(), "")

    def test_a_link_left_in_a_committed_file_fails_the_pins(self):
        self.cli("link")
        targets = (self.root / "windows/Directory.Build.targets").read_text("utf8")
        self.cli("unlink")
        (self.root / "windows/Directory.Build.targets").write_text(targets, "utf8")
        code, out = self.cli("pins")
        self.assertEqual(code, 1)
        self.assertIn("linked to a Deskling checkout (Directory.Build.targets)", out)


if __name__ == "__main__":
    unittest.main()
