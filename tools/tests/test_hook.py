#!/usr/bin/env python3
"""Tests for tools/hooks/hook.py, which runs before and after every tool call in a companion app: a bad pattern there
quietly blocks or lets through things in every session. Runs the hook over the fixture app."""

from __future__ import annotations

import importlib.util
import json
import os
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

from support import TOOLS, fixture_app

HOOK = TOOLS / "hooks" / "hook.py"


def load_hook():
    spec = importlib.util.spec_from_file_location("hook", HOOK)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


class HookTestCase(unittest.TestCase):
    def setUp(self):
        self.app = fixture_app()
        self.root = self.app.__enter__()
        self.tmp = tempfile.mkdtemp()

    def tearDown(self):
        self.app.__exit__(None, None, None)

    def run_hook(self, mode, event, payload=None):
        env = {**os.environ, "CLAUDE_PROJECT_DIR": str(self.root), "TMPDIR": self.tmp}
        return subprocess.run([sys.executable, str(HOOK), mode], input=payload if payload is not None else json.dumps(event),
                              text=True, capture_output=True, env=env, timeout=60)

    def configure(self, **hooks):
        """Adds `hooks` entries to the fixture app's companion.json."""
        path = self.root / "companion.json"
        data = json.loads(path.read_text("utf8"))
        data["hooks"].update(hooks)
        path.write_text(json.dumps(data), "utf8")

    def blocked(self, tool, **tool_input):
        done = self.run_hook("pre", {"session_id": "t", "tool_name": tool, "tool_input": tool_input})
        self.assertIn(done.returncode, (0, 2), done.stderr)
        return done.returncode == 2


class PreToolUse(HookTestCase):
    def test_reads(self):
        root = self.root
        self.assertTrue(self.blocked("Read", file_path=str(root / "macos/Fixture/Resources/Localizable.xcstrings")))
        self.assertTrue(self.blocked("Read", file_path=str(root / "macos/Fixture.xcodeproj/project.pbxproj")))
        self.assertFalse(self.blocked("Read", file_path=str(root / "conformance/phrases.json")))
        self.assertFalse(self.blocked("Read", file_path=str(root / "companion.json")))
        self.assertTrue(self.blocked("Read", file_path=str(root / "voices/Voice/manifest.json")))
        self.assertFalse(self.blocked("Read", file_path=str(root / "macos/scripts/voice-config.json")))
        self.assertFalse(self.blocked("Read", file_path=str(root / "design/locked.txt")), "guardedExtra blocks edits only")

    def test_edits(self):
        root = self.root
        self.assertTrue(self.blocked("Edit", file_path=str(root / "conformance/phrases.json")))
        self.assertTrue(self.blocked("Write", file_path=str(root / "windows/Fixture.App/Strings/en/Resources.resw")))
        self.assertFalse(self.blocked("Edit", file_path=str(root / "conformance/README.md")))
        self.assertFalse(self.blocked("Edit", file_path=str(root / "macos/project.yml")))
        self.assertTrue(self.blocked("Write", file_path=str(root / "voices/Voice/it/clown/hello.m4a")))
        self.assertTrue(self.blocked("Edit", file_path=str(root / "voices/Voice/manifest.json")))
        self.assertTrue(self.blocked("Edit", file_path=str(root / "voices/review/it.html")))
        self.assertTrue(self.blocked("Write", file_path=str(root / "voices/LICENSE")))
        self.assertFalse(self.blocked("Edit", file_path=str(root / "macos/scripts/voice-direction.json")))
        self.assertTrue(self.blocked("Edit", file_path=str(root / "design/locked.txt")), "hooks.guardedExtra")

    def test_bash(self):
        xcstrings = "macos/Fixture/Resources/Localizable.xcstrings"
        self.assertTrue(self.blocked("Bash", command=f"cat {xcstrings}"))
        self.assertTrue(self.blocked("Bash", command=f"grep -C3 '\"Skip\"' {xcstrings}"))
        self.assertTrue(self.blocked("Bash", command=f"git diff {xcstrings}"))
        self.assertTrue(self.blocked("Bash", command=f"ls && head -50 {xcstrings}"))
        self.assertFalse(self.blocked("Bash", command=f"grep -n '\"Skip\"' {xcstrings}"))
        self.assertFalse(self.blocked("Bash", command=f"git diff --stat {xcstrings}"))
        self.assertFalse(self.blocked("Bash", command=f"git add {xcstrings} && git status"))
        self.assertFalse(self.blocked("Bash", command="python3 deskling/tools/l10n.py status -v"))
        self.assertFalse(self.blocked("Bash", command="cat > notes.md <<'EOF'\nsee Localizable.xcstrings\nEOF"))
        self.assertFalse(self.blocked("Bash", command="cat companion.json"))

    def test_big_files_are_read_by_range(self):
        (self.root / "macos/Fixture/Views").mkdir(parents=True, exist_ok=True)
        big = self.root / "macos/Fixture/Views/Big.swift"
        big.write_text("".join(f"func f{i}() {{}}\n" for i in range(700)), "utf8")
        small = self.root / "macos/Fixture/Views/Small.swift"
        small.write_text("func f() {}\n" * 100, "utf8")
        done = self.run_hook("pre", {"session_id": "t", "tool_name": "Read", "tool_input": {"file_path": str(big)}})
        self.assertEqual(done.returncode, 2)
        self.assertIn("macos/Fixture/Views/Big.swift is 700 lines: read its outline first", done.stderr)
        self.assertFalse(self.blocked("Read", file_path=str(big), offset=100, limit=80))
        self.assertFalse(self.blocked("Read", file_path=str(big), limit=50))
        self.assertFalse(self.blocked("Read", file_path=str(small)))
        self.assertFalse(self.blocked("Read", file_path=str(self.root / "macos/Fixture/Views/Missing.swift")))
        self.assertTrue(self.blocked("Bash", command="cat macos/Fixture/Views/Big.swift"))
        self.assertTrue(self.blocked("Bash", command=f"cat {big} | grep func"))
        self.assertTrue(self.blocked("Bash", command="sed -n p macos/Fixture/Views/Big.swift"))
        self.assertTrue(self.blocked("Bash", command="sed 's/a/b/' macos/Fixture/Views/Big.swift"))
        self.assertFalse(self.blocked("Bash", command="sed -n '100,180p' macos/Fixture/Views/Big.swift"))
        self.assertFalse(self.blocked("Bash", command="sed -n '/^func f1/,/^}/p' macos/Fixture/Views/Big.swift"))
        self.assertFalse(self.blocked("Bash", command="sed -i '' 's/a/b/' macos/Fixture/Views/Big.swift"))
        self.assertFalse(self.blocked("Bash", command="head -40 macos/Fixture/Views/Big.swift"))
        self.assertFalse(self.blocked("Bash", command="grep -n '^func' macos/Fixture/Views/Big.swift"))
        self.assertFalse(self.blocked("Bash", command="wc -l macos/Fixture/Views/Big.swift"))
        self.assertFalse(self.blocked("Bash", command="cat > macos/Fixture/Views/Big.swift"), "a redirect target")
        self.assertFalse(self.blocked("Bash", command="cat macos/Fixture/Views/Small.swift"))
        self.configure(bigFileLines=0)
        self.assertFalse(self.blocked("Read", file_path=str(big)), "hooks.bigFileLines 0 disables")
        self.configure(bigFileLines=50)
        self.assertTrue(self.blocked("Read", file_path=str(small)))

    def test_grep(self):
        path = "macos/Fixture/Resources/Localizable.xcstrings"
        self.assertTrue(self.blocked("Grep", pattern="Skip", path=path, **{"-C": 3}))
        self.assertFalse(self.blocked("Grep", pattern="Skip", path=path))
        self.assertFalse(self.blocked("Grep", pattern="Skip", path="macos/Fixture", **{"-C": 3}))

    def test_codex_apply_patch(self):
        def patch(path):
            return f"*** Begin Patch\n*** Update File: {path}\n@@\n-a\n+b\n*** End Patch\n"

        self.assertTrue(self.blocked("apply_patch", command=patch("conformance/phrases.json")))
        self.assertTrue(self.blocked("apply_patch", command="*** Begin Patch\n*** Add File: notes.md\n+x\n"
                                     "*** Update File: macos/Fixture.xcodeproj/project.pbxproj\n*** End Patch\n"))
        self.assertFalse(self.blocked("apply_patch", command=patch("macos/project.yml")))
        below_root = {"session_id": "t", "cwd": str(self.root / "macos"), "tool_name": "apply_patch",
                      "tool_input": {"command": patch("../conformance/phrases.json")}}
        self.assertEqual(self.run_hook("pre", below_root).returncode, 2, "relative to the session's cwd")

    def test_copilot_shapes(self):
        xcstrings = str(self.root / "macos/Fixture/Resources/Localizable.xcstrings")
        self.assertTrue(self.blocked("Read", path=xcstrings))
        self.assertTrue(self.blocked("Edit", path=str(self.root / "conformance/phrases.json")))
        done = self.run_hook("pre", {"session_id": "t", "tool_name": "Bash",
                                     "tool_input": json.dumps({"command": f"cat {xcstrings}"})})
        self.assertEqual(done.returncode, 2)
        self.assertEqual(json.loads(done.stdout)["permissionDecision"], "deny")

    def test_app_guards_name_their_reason_and_may_block_reads(self):
        self.configure(guards=[
            {"pattern": "^data/big-[^/]*\\.json$", "why": "generated: read data/index.json.", "reads": True},
            {"pattern": "^data/(?!index\\.json$)", "why": "generated by make-data."},
        ])
        big = str(self.root / "data/big-1.json")
        done = self.run_hook("pre", {"session_id": "t", "tool_name": "Read", "tool_input": {"file_path": big}})
        self.assertEqual(done.returncode, 2)
        self.assertIn("data/big-1.json: generated: read data/index.json.", done.stderr)
        self.assertTrue(self.blocked("Bash", command="head data/big-1.json"))
        self.assertFalse(self.blocked("Read", file_path=str(self.root / "data/small.json")), "edits only")
        self.assertTrue(self.blocked("Edit", file_path=str(self.root / "data/small.json")))
        self.assertFalse(self.blocked("Edit", file_path=str(self.root / "data/index.json")))

    def test_codex_argv_commands(self):
        xcstrings = "macos/Fixture/Resources/Localizable.xcstrings"
        self.assertTrue(self.blocked("Bash", command=["cat", xcstrings]))
        self.assertFalse(self.blocked("Bash", command=["git", "status"]))

    def test_bad_input_and_a_bad_companion_json_fail_open(self):
        self.assertEqual(self.run_hook("pre", None, payload="not json").returncode, 0)
        for odd in ([1, 2], {"tool_name": "Read", "tool_input": 7}, {"tool_name": "apply_patch", "tool_input": None}):
            done = self.run_hook("pre", odd)
            self.assertEqual(done.returncode, 0, done.stderr)
        (self.root / "companion.json").write_text("{}", "utf8")
        event = {"session_id": "t", "tool_name": "Edit", "tool_input": {"file_path": str(self.root / "conformance/phrases.json")}}
        self.assertEqual(self.run_hook("pre", event).returncode, 0)


class StopAndParity(HookTestCase):
    def setUp(self):
        super().setUp()
        os.environ["TMPDIR"] = self.tmp
        self.hook = load_hook()
        self.hook.setup(self.root)
        self.event = {"session_id": "parity"}

    def remind(self, last_edit):
        path = self.hook.state_file(self.event)
        lines = path.read_text().splitlines() if path.exists() else []
        last_ran = {v: float(t) for t, k, v in (line.split("\t", 2) for line in lines) if k == "ran"}
        return self.hook.parity_reminder(self.event, last_edit, last_ran)

    def test_mac_ui_edit_reminds_once(self):
        edits = {"macos/Fixture/Views/SettingsView.swift": 100.0}
        reminder = self.remind(edits) or ""
        self.assertIn("SettingsView.swift", reminder)
        self.assertIn("nothing under windows/Fixture.App", reminder)
        self.assertIsNone(self.remind(edits))

    def test_windows_edit_after_mac_is_enough(self):
        edits = {"macos/Fixture/Views/HomeView.swift": 100.0, "windows/Fixture.App/Views/HomeView.xaml": 120.0}
        self.assertIsNone(self.remind(edits))

    def test_not_ui(self):
        for path in ("macos/FixtureCore/Sources/FixtureCore/Phrases.swift", "macos/Fixture/App/AppModel.swift"):
            self.assertIsNone(self.remind({path: 100.0}), path)

    def test_slow_checks_come_from_companion_json(self):
        kinds = {kind: pattern for kind, pattern, _ in self.hook.SLOW}
        self.assertEqual(set(kinds), {"xcodebuild-test", "conformance", "swift-test", "dotnet-test", "dotnet-app-test",
                                      "winui-build"}, "no landing")
        self.assertRegex("windows/Fixture.App/Views/HomeView.xaml", kinds["winui-build"])
        self.assertNotRegex("windows/Fixture.Core/A.cs", kinds["winui-build"])
        self.assertRegex("macos/FixtureCore/Sources/FixtureCore/A.swift", kinds["conformance"])
        self.assertRegex("windows/Fixture.Core/A.cs", kinds["dotnet-test"])
        self.assertNotRegex("windows/Fixture.App/A.cs", kinds["dotnet-test"])
        self.assertRegex("windows/Fixture.App/App/AppModel.cs", kinds["dotnet-app-test"])
        self.assertRegex("windows/Fixture.App.Tests/AppModelTests.cs", kinds["dotnet-app-test"])
        self.assertNotRegex("windows/Fixture.Core/A.cs", kinds["dotnet-app-test"])

    def test_app_model_tests_count_when_their_project_or_the_solution_ran(self):
        edit = {"session_id": "app", "tool_name": "Edit",
                "tool_input": {"file_path": str(self.root / "windows/Fixture.App/App/AppModel.cs")}}

        def reminded_after(command):
            self.run_hook("post", edit)
            self.run_hook("post", {"session_id": "app", "tool_name": "Bash", "tool_input": {"command": command}})
            return "Windows app model tests" in self.run_hook("stop", {"session_id": "app"}).stderr

        self.assertTrue(reminded_after("dotnet test windows/Fixture.Core.Tests"), "another test project")
        self.assertFalse(reminded_after("dotnet test windows/Fixture.App.Tests"))
        self.assertFalse(reminded_after("cd windows && dotnet test"), "the solution")

    def test_the_winui_build_counts_from_the_vm_or_dotnet(self):
        sessions = iter(range(100))

        def reminded_after(command):
            session = f"winui{next(sessions)}"  # the log has whole seconds: one session per command
            self.run_hook("post", {"session_id": session, "tool_name": "Edit",
                                   "tool_input": {"file_path": str(self.root / "windows/Fixture.App/Views/HomeView.xaml")}})
            if command:
                self.run_hook("post", {"session_id": session, "tool_name": "Bash", "tool_input": {"command": command}})
            return "WinUI app build" in self.run_hook("stop", {"session_id": session}).stderr

        self.assertTrue(reminded_after(None))
        self.assertIn("vm.sh build", self.hook.SLOW[-1][2])
        for command in ("deskling/tools/windows/vm.sh build", "deskling/tools/windows/vm.sh build ARM64 -- -c Release",
                        "deskling/tools/windows/vm.sh screenshots /tmp/shots -- -Language fr",
                        "deskling/tools/windows/vm.sh smoke", "dotnet build windows/Fixture.App -p:Platform=x64",
                        "dotnet publish windows/Fixture.App/Fixture.App.csproj -c Release"):
            self.assertFalse(reminded_after(command), command)
        for command in ("deskling/tools/windows/vm.sh status", "deskling/tools/windows/vm.sh sync",
                        "dotnet build windows/Fixture.Core"):
            self.assertTrue(reminded_after(command), command)

    def test_vm_tests_count_as_the_dotnet_tests(self):
        edit = {"session_id": "vmtest", "tool_name": "Edit",
                "tool_input": {"file_path": str(self.root / "windows/Fixture.Core/A.cs")}}
        self.run_hook("post", edit)
        self.assertIn("Windows Core tests", self.run_hook("stop", {"session_id": "vmtest"}).stderr)
        self.run_hook("post", {"session_id": "vmtest", "tool_name": "Bash",
                               "tool_input": {"command": "deskling/tools/windows/vm.sh test"}})
        self.assertNotIn("Windows Core tests", self.run_hook("stop", {"session_id": "vmtest"}).stderr)

    def test_app_slow_checks_replace_and_add(self):
        self.configure(slow=[
            {"name": "conformance", "paths": "^macos/Fixture/Model/", "label": "the app's vectors"},
            {"name": "app-build", "paths": "^windows/Fixture\\.App/", "label": "the tray app build",
             "ran": "\\bdotnet\\s+build\\b.*Fixture\\.App"},
        ])
        self.hook.setup(self.root)
        checks = {kind: (pattern, label) for kind, pattern, label in self.hook.SLOW}
        self.assertEqual(checks["conformance"], ("^macos/Fixture/Model/", "the app's vectors"))
        self.assertEqual(checks["app-build"][1], "the tray app build")
        self.assertIn("swift-test", checks, "the other built-in checks stay")
        edit = {"session_id": "slow", "tool_name": "Edit",
                "tool_input": {"file_path": str(self.root / "windows/Fixture.App/Tray.cs")}}
        self.run_hook("post", edit)
        done = self.run_hook("stop", {"session_id": "slow"})
        self.assertIn("the tray app build", done.stderr)
        self.run_hook("post", {"session_id": "slow", "tool_name": "Bash",
                               "tool_input": {"command": "dotnet build windows/Fixture.App/Fixture.App.csproj"}})
        self.assertNotIn("the tray app build", self.run_hook("stop", {"session_id": "slow"}).stderr)

    def test_cloud_stop_does_nothing_outside_github_actions(self):
        env_actions = os.environ.pop("GITHUB_ACTIONS", None)
        try:
            done = self.run_hook("cloud-stop", {"session_id": "cloud"})
        finally:
            if env_actions is not None:
                os.environ["GITHUB_ACTIONS"] = env_actions
        self.assertEqual((done.returncode, done.stdout.strip()), (0, "{}"))

    def test_stop_runs_the_fast_checks_and_names_the_slow_ones(self):
        event = {"session_id": "stop", "tool_name": "Edit",
                 "tool_input": {"file_path": str(self.root / "macos/FixtureCore/Sources/FixtureCore/A.swift")}}
        self.assertEqual(self.run_hook("post", event).returncode, 0)
        done = self.run_hook("stop", {"session_id": "stop"})
        self.assertEqual(done.returncode, 2, done.stderr)
        self.assertIn("Not run since your last edit: conformance vectors", done.stderr)
        # Deskling's own (the fixture has no scripts/ wrapper), wherever this checkout of it is.
        self.assertIn("tools/verify.sh -- macos/FixtureCore/", done.stderr)
        self.run_hook("post", {"session_id": "stop", "tool_name": "Bash", "tool_input": {"command": "scripts/verify.sh"}})
        self.assertEqual(self.run_hook("stop", {"session_id": "stop"}).returncode, 0)


if __name__ == "__main__":
    unittest.main()
