#!/usr/bin/env python3
"""Tests for scripts/release.sh (`plan`, `nuget`, `landing`, `bump`) and release.yml's plan step itself, over a copy of
this repository's version files and .NET projects tagged v0.13.0 in a temporary git repository, with `gh` and `npm`
stood in for."""

from __future__ import annotations

import os
import re
import shutil
import subprocess
import tempfile
import unittest
from pathlib import Path

from support import NO_SHEBANG, REPO, WINDOWS

COPIED = ("scripts/release.sh", "scripts/verify.sh", "Sources/LachingCore/Laching.swift", "kotlin/gradle.properties",
          "README.md", "site/index.html", "dotnet/Directory.Build.props", "tools/landing/package.json")
# gh api repos/…/commits/<sha>/pulls --jq …: the lines the --jq makes, from $FAKE_PULLS/<sha> ("number<tab>labels");
# "Bad credentials" without that folder. `gh workflow run` is logged.
GH = ('[ "$1" = workflow ] && { echo "$*" >> "$FAKE_PULLS.log"; exit 0; }\n'
      '[ -d "$FAKE_PULLS" ] || { echo "HTTP 401: Bad credentials" >&2; exit 1; }\n'
      'sha=$(echo "$2" | cut -d/ -f5); [ ! -f "$FAKE_PULLS/$sha" ] || cat "$FAKE_PULLS/$sha"\n')


@unittest.skipIf(WINDOWS, NO_SHEBANG)
class ReleaseTests(unittest.TestCase):
    def setUp(self):
        temporary = tempfile.TemporaryDirectory()
        self.addCleanup(temporary.cleanup)
        self.tmp = Path(temporary.name)
        self.root = self.tmp / "laching"
        # The version files, the projects and the tools' shell scripts (which verify.sh may source).
        for file in [*(REPO / name for name in COPIED), *(REPO / "dotnet").glob("*/*.csproj"), *(REPO / "tools").glob("*.sh")]:
            (self.root / file.relative_to(REPO)).parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(file, self.root / file.relative_to(REPO))
        # Whatever this repository's release, the copy is at 0.13.0, NuGet with it.
        version = re.search(r'version = "([^"]+)"', self.read("Sources/LachingCore/Laching.swift")).group(1)
        for name in ("Sources/LachingCore/Laching.swift", "kotlin/gradle.properties", "README.md", "site/index.html"):
            self.write(name, self.read(name).replace(version, "0.13.0"))
        self.write("dotnet/Directory.Build.props", re.sub(r"(<LachingVersion[^>]*>)[^<]+", r"\g<1>0.13.0",
                                                          self.read("dotnet/Directory.Build.props")))
        self.bin = self.tmp / "bin"
        self.bin.mkdir()
        for name, body in (("gh", GH), ("npm", '[ "$1 $2 $3" = "view @laching/landing version" ] && echo 0.13.0\n')):
            (self.bin / name).write_text("#!/bin/sh\n" + body, "utf8")
            (self.bin / name).chmod(0o755)
        self.pulls: dict[str, list[dict]] = {}
        self.git("init", "-q")
        self.git("remote", "add", "origin", "https://github.com/vmihalachi/laching.git")
        self.commit("0.13.0: release")
        self.git("tag", "-a", "v0.13.0", "-m", "Laching 0.13.0")

    def git(self, *args: str) -> str:
        env = {key: value for key, value in os.environ.items() if not key.startswith("GIT_")}
        command = ["git", "-c", "user.name=Fixture", "-c", "user.email=fixture@example.com", "-c", "init.defaultBranch=master",
                   "-c", "tag.gpgSign=false", "-c", "commit.gpgSign=false", *args]
        return subprocess.run(command, cwd=self.root, env=env, check=True, capture_output=True, text=True).stdout.strip()

    def read(self, name: str) -> str:
        return (self.root / name).read_text("utf8")

    def write(self, name: str, text: str) -> None:
        (self.root / name).parent.mkdir(parents=True, exist_ok=True)
        (self.root / name).write_text(text, "utf8")

    def commit(self, message: str, pr: int | None = None, labels: tuple[str, ...] = ()) -> None:
        self.git("add", "-A")
        self.git("commit", "-q", "--allow-empty", "-m", message)
        if pr is not None:
            self.pulls[self.git("rev-parse", "HEAD")] = [{"number": pr, "labels": list(labels)}]

    def change(self, path: str, pr: int | None = None, labels: tuple[str, ...] = ()) -> None:
        self.write(path, f"// {path} {self.git('rev-list', '--count', 'HEAD')}\n")
        self.commit(f"change {path}", pr, labels)

    def env(self, pulls: bool = True, **extra: str) -> dict[str, str]:
        """The environment with the fakes; `pulls=False` makes gh fail, as it does without a token."""
        pulls_file = self.tmp / "pulls"
        shutil.rmtree(pulls_file, ignore_errors=True)
        if pulls:
            pulls_file.mkdir()
            for sha, prs in self.pulls.items():
                (pulls_file / sha).write_text("".join(f"{pr['number']}\t{','.join(pr['labels'])}\n" for pr in prs), "utf8")
        env = {key: value for key, value in os.environ.items()
               if not key.startswith("GIT_") and key not in ("GITHUB_REPOSITORY", "GH_TOKEN")}
        return {**env, "PATH": f"{self.bin}{os.pathsep}{env.get('PATH', '')}", "FAKE_PULLS": str(pulls_file), **extra}

    def run_release(self, *args: str, pulls: bool = True) -> subprocess.CompletedProcess:
        return subprocess.run(["sh", str(self.root / "scripts/release.sh"), *args], cwd=self.root, env=self.env(pulls),
                              capture_output=True, text=True)

    def out(self, *args: str) -> str:
        result = self.run_release(*args)
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        return result.stdout.strip()

    def test_the_plan_numbers_the_release_from_the_breaking_label(self):
        self.assertEqual(self.out("plan"), "none (no commits since v0.13.0)")
        self.change("docs/a.md", pr=101)
        self.change("docs/b.md", pr=101)
        self.change("docs/c.md")  # pushed to master, no pull request
        self.assertEqual(self.out("plan"), "release 0.13.1 (patch: none of the 1 pull request merged is labelled "
                                           "breaking; 3 commits since v0.13.0)")
        self.change("docs/d.md", pr=102, labels=("enhancement", "breaking"))
        self.change("docs/e.md", pr=103, labels=("breaking",))
        self.assertEqual(self.out("plan"), "release 0.14.0 (minor: #102, #103 labelled breaking (a minor while 0.x); "
                                           "5 commits since v0.13.0)")
        self.assertEqual(self.out("plan", "--bump", "major"), "release 1.0.0 (major: asked for; 5 commits since v0.13.0)")
        self.assertEqual(self.run_release("plan", "--bump", "huge").returncode, 1)

        result = self.run_release("plan", pulls=False)
        self.assertEqual(result.returncode, 1)
        self.assertIn("Bad credentials", result.stderr)
        self.assertEqual(self.run_release("plan", "--bump", "patch", pulls=False).returncode, 0, "reads no labels")
        self.out("bump", "1.0.0")
        self.commit("1.0.0: release")
        self.git("tag", "-a", "v1.0.0", "-m", "Laching 1.0.0")
        self.change("docs/f.md", pr=104, labels=("breaking",))
        self.assertTrue(self.out("plan").startswith("release 2.0.0 (major: #104 labelled breaking;"))
        self.write("Sources/LachingCore/Laching.swift", self.read("Sources/LachingCore/Laching.swift").replace(
            '"1.0.0"', '"1.0.5"'))
        self.commit("a bump by hand")
        result = self.run_release("plan", "--bump", "patch")
        self.assertEqual(result.returncode, 1)
        self.assertIn("says 1.0.5, but the last release tag is v1.0.0", result.stderr)

    def test_nuget_ships_when_a_packages_folder_or_the_props_changed(self):
        for path in ("README.md", "dotnet/Laching.Core.Tests/A.cs", "dotnet/Fixture.App/A.cs", "Sources/LachingCore/A.swift"):
            self.change(path)
        self.assertEqual(self.out("nuget", "0.13.1"), "skip (the packages are unchanged since 0.13.0, their version on NuGet)")
        self.change("dotnet/Laching.Windows/Tray.cs")
        self.assertEqual(self.out("nuget", "0.13.1"), "ship (the packages changed since 0.13.0, their version on NuGet)")

    def test_bump_writes_the_strings_and_lachingversion_only_when_nuget_ships(self):
        self.change("tools/a.py")
        self.assertEqual(self.out("bump", "0.13.1"), "Bumped 0.13.0 to 0.13.1; NuGet: skip (the packages are unchanged "
                                                     "since 0.13.0, their version on NuGet)")
        self.assertIn('exact: "0.13.1"', self.read("README.md"))
        self.assertIn("latest release is `v0.13.1`", self.read("README.md"))
        self.assertIn("lachingVersion=0.13.1", self.read("kotlin/gradle.properties"))
        self.assertIn(">v0.13.1</a>", self.read("site/index.html"))
        self.assertIn(">0.13.0</LachingVersion>", self.read("dotnet/Directory.Build.props"), "NuGet keeps 0.13.0")
        verify = subprocess.run(["bash", str(self.root / "scripts/verify.sh"), "--version", "0.13.1"], cwd=self.root,
                                capture_output=True, text=True)
        self.assertEqual(verify.returncode, 0, verify.stdout + verify.stderr)
        self.assertEqual(self.run_release("bump", "0.13.1").returncode, 1, "bump goes forward")
        self.commit("0.13.1: release")
        self.git("tag", "-a", "v0.13.1", "-m", "Laching 0.13.1")

        self.change("dotnet/Laching.Core/Clock.cs")
        self.assertIn("NuGet: ship", self.out("bump", "0.13.2"))
        self.assertIn(">0.13.2</LachingVersion>", self.read("dotnet/Directory.Build.props"))
        self.assertEqual(self.out("nuget", "0.13.2"), "ship (LachingVersion is 0.13.2 already)", "the same after")

    def test_landing_ships_when_it_changed_since_npms_version(self):
        self.assertEqual(self.out("landing"), "no (tools/landing is unchanged since 0.13.0, the version on npm)")
        self.change("tools/landing/src/index.ts")
        self.assertEqual(self.out("landing"), "yes (tools/landing changed since 0.13.0, the version on npm)")

    def plan_step(self, **env: str) -> tuple[subprocess.CompletedProcess, dict[str, str], str]:
        """release.yml's plan step, its script as the workflow has it, against the origin here: the result, the step's
        outputs and its summary."""
        lines = (REPO / ".github/workflows/release.yml").read_text("utf8").splitlines()
        start = lines.index("        name: Plan, then bump and tag (a dry run stops after the plan)")
        start = lines.index("        run: |", start) + 1
        end = next(n for n in range(start, len(lines)) if lines[n].strip() and not lines[n].startswith(" " * 10))
        output, summary = self.tmp / "output", self.tmp / "summary"
        output.write_text("", "utf8")
        summary.write_text("", "utf8")
        variables = self.env(GITHUB_REPOSITORY="vmihalachi/laching", GITHUB_REF="refs/heads/master", BUMP="auto",
                             DRY_RUN="false", GITHUB_OUTPUT=str(output), GITHUB_STEP_SUMMARY=str(summary))
        result = subprocess.run(["bash", "-e", "-c", "\n".join(line[10:] for line in lines[start:end])], cwd=self.root,
                                env={**variables, **env}, capture_output=True, text=True)
        return result, dict(line.split("=", 1) for line in output.read_text("utf8").splitlines()), summary.read_text("utf8")

    def origin(self) -> Path:
        origin = self.tmp / "origin.git"
        subprocess.run(["git", "init", "-q", "--bare", str(origin)], check=True)
        self.git("remote", "set-url", "origin", str(origin))
        self.git("push", "-q", "origin", "HEAD:refs/heads/master", "v0.13.0")
        return origin

    def remote(self, origin: Path, *args: str) -> str:
        return subprocess.run(["git", "-C", str(origin), *args], capture_output=True, text=True).stdout.strip()

    def test_the_workflows_plan_step_dry_runs_then_releases(self):
        origin = self.origin()
        self.change("dotnet/Laching.Windows/Tray.cs", pr=110)
        self.git("push", "-q", "origin", "HEAD:refs/heads/master")
        head = self.git("rev-parse", "HEAD")

        result, outputs, summary = self.plan_step(DRY_RUN="true")
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        self.assertEqual(outputs, {"release": "false"})
        for line in ("Plan: release 0.13.1 (patch:", "NuGet: ship (", "@laching/landing: no (", "Dry run: nothing"):
            self.assertIn(line, summary)
        self.assertEqual((self.git("rev-parse", "HEAD"), self.git("status", "--porcelain")), (head, ""))
        self.assertEqual(self.remote(origin, "tag", "--list", "v0.13.1"), "")

        refused, _, _ = self.plan_step(GITHUB_REF="refs/heads/feature")
        self.assertEqual(refused.returncode, 1)
        self.assertIn("A release runs on master", refused.stdout)

        result, outputs, _ = self.plan_step()
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        self.assertEqual({key: outputs[key] for key in ("release", "version", "previous", "nuget", "landing")},
                         {"release": "true", "version": "0.13.1", "previous": "0.13.0", "nuget": "true", "landing": "false"})
        self.assertEqual(self.remote(origin, "log", "-1", "--format=%s", "master"), "0.13.1: release")
        self.assertEqual(self.remote(origin, "rev-parse", "v0.13.1^{commit}"), outputs["ref"])
        self.assertEqual(self.remote(origin, "rev-parse", "master"), outputs["ref"])
        self.assertIn("workflow run pages.yml --ref master", (self.tmp / "pulls.log").read_text("utf8"))

        result, outputs, summary = self.plan_step()
        self.assertEqual(outputs, {"release": "false"})
        self.assertIn("Nothing to release: none (no commits since v0.13.1)", summary)

    def test_the_workflows_plan_step_plans_again_when_master_moved(self):
        origin = self.origin()
        self.change("docs/a.md", pr=110)
        self.git("push", "-q", "origin", "HEAD:refs/heads/master")
        self.change("dotnet/Laching.Core/Clock.cs", pr=111, labels=("breaking",))
        self.git("push", "-q", "origin", "HEAD:refs/heads/master")
        self.git("reset", "-q", "--hard", "HEAD~1")  # the run checked out master before #111 landed
        result, outputs, summary = self.plan_step()
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        self.assertIn("Plan: release 0.13.1", summary)
        self.assertIn("NuGet: skip", summary)
        self.assertEqual((outputs["version"], outputs["nuget"]), ("0.14.0", "true"))
        self.assertEqual(self.remote(origin, "rev-parse", "v0.14.0^{commit}"), self.remote(origin, "rev-parse", "master"))
        self.assertEqual(self.remote(origin, "tag", "--list", "v0.13.1"), "", "the rejected push took its tag back")

    def test_the_workflows_plan_step_never_moves_a_tag(self):
        origin = self.origin()
        self.change("docs/a.md", pr=111)
        self.git("push", "-q", "origin", "HEAD:refs/heads/master")
        self.git("tag", "v0.13.1", "HEAD~1")
        self.git("push", "-q", "origin", "v0.13.1")
        self.git("tag", "-d", "v0.13.1")
        result, outputs, _ = self.plan_step()
        self.assertEqual(result.returncode, 1)
        self.assertIn("v0.13.1 already exists on origin", result.stdout)
        self.assertEqual(self.remote(origin, "log", "-1", "--format=%s", "master"), "change docs/a.md")
        self.assertNotIn("release", outputs)


if __name__ == "__main__":
    unittest.main()
