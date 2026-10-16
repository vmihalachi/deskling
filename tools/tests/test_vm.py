#!/usr/bin/env python3
"""Tests for tools/windows/vm.sh (vm.py), the Parallels VM runner, without a VM: the files it mirrors, the tar it
sends, the paths the VM sees, the PowerShell it runs for each command, and the one line it prints when there's no VM
(no prlctl, an unknown, stopped or suspended VM) against a fake `prlctl`. Where PowerShell is installed, vm-guest.ps1's
Sync-Tree runs over a folder: what it unpacks, deletes, keeps and asks for."""

from __future__ import annotations

import base64
import os
import shutil
import subprocess
import sys
import tarfile
import tempfile
import unittest
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "windows"))
from support import TOOLS, WINDOWS, fixture_app  # noqa: E402

if WINDOWS:
    raise unittest.SkipTest("vm.py runs on a Mac, driving its Parallels VM (it locks with fcntl, which Windows lacks)")
import vm  # noqa: E402

VM_SH = TOOLS / "windows" / "vm.sh"
POWERSHELL = shutil.which("pwsh") or shutil.which("powershell")


def git(folder: Path, *args: str) -> str:
    return subprocess.run(["git", "-C", str(folder), "-c", "user.name=t", "-c", "user.email=t@t", *args],
                          capture_output=True, text=True, check=True).stdout


class FilesTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.root = Path(self.temp.name).resolve() / "app"
        self.root.mkdir()

    def tearDown(self):
        self.temp.cleanup()

    def write(self, name: str, text: str = "x", folder: Path | None = None) -> Path:
        path = (folder or self.root) / name
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(text, "utf8")
        return path

    def test_lists_what_git_sees_with_submodules(self):
        git(self.root, "init", "-q")
        self.write(".gitignore", "bin/\n*.log\n")
        self.write("windows/App/App.cs", "class A {}")
        self.write("windows/App/Gone.cs")
        self.write("macos/Model.swift")
        git(self.root, "add", ".")
        git(self.root, "commit", "-qm", "one")
        self.write("windows/App/App.cs", "class A { int b; }")  # modified, uncommitted
        self.write("windows/App/New.cs")  # untracked
        self.write("windows/App/bin/Debug/App.dll")  # ignored
        self.write("build.log")  # ignored
        (self.root / "windows/App/Gone.cs").unlink()  # deleted, uncommitted
        (self.root / "link.cs").symlink_to("windows/App/App.cs")
        self.write("odd\tname.cs")
        # A checked-out submodule: a repository of its own, recorded as a gitlink.
        sub = self.root / "deskling"
        sub.mkdir()
        git(sub, "init", "-q")
        self.write("tools/windows/vm.py", folder=sub)
        git(sub, "add", ".")
        git(sub, "commit", "-qm", "tools")
        self.write("tools/windows/draft.py", folder=sub)  # untracked in the submodule
        head = git(sub, "rev-parse", "HEAD").strip()
        git(self.root, "update-index", "--add", "--cacheinfo", f"160000,{head},deskling")

        files = vm.list_files(self.root)
        names = [name for name, _, _ in files]
        self.assertEqual(names, sorted([
            ".gitignore", "deskling/tools/windows/draft.py", "deskling/tools/windows/vm.py", "macos/Model.swift",
            "windows/App/App.cs", "windows/App/New.cs"]))
        app = next(entry for entry in files if entry[0] == "windows/App/App.cs")
        stat = (self.root / "windows/App/App.cs").stat()
        self.assertEqual(app[1:], (stat.st_size, int(stat.st_mtime)))

    def test_the_tar_holds_whole_second_mtimes(self):
        path = self.write("windows/App/App.cs", "class A {}")
        os.utime(path, (1_700_000_000.75, 1_700_000_000.75))
        archive = self.root.parent / "sync.tar"
        vm.write_tar(self.root, ["windows/App/App.cs", "missing.cs"], archive)
        with tarfile.open(archive) as tar:
            members = tar.getmembers()
            self.assertEqual([m.name for m in members], ["windows/App/App.cs"])
            self.assertEqual(members[0].mtime, 1_700_000_000)
            self.assertEqual(tar.extractfile(members[0]).read(), b"class A {}")

    def test_the_list_is_tab_separated(self):
        vm.write_list([("a/b.cs", 3, 17), ("c.txt", 0, 18)], self.root / "files.tsv")
        self.assertEqual((self.root / "files.tsv").read_text("utf8"), "a/b.cs\t3\t17\nc.txt\t0\t18\n")


class PathTests(unittest.TestCase):
    def test_the_vm_sees_mac_paths_through_the_share(self):
        with tempfile.TemporaryDirectory() as temp:
            real = os.path.realpath(temp)
            self.assertEqual(vm.guest_path(temp, r"\\Mac\AllFiles"), "\\\\Mac\\AllFiles" + real.replace("/", "\\"))
            os.environ["DESKLING_VM_SHARE"] = "Z:\\"
            try:
                self.assertEqual(vm.guest_path(Path(temp) / "a b"), "Z:" + real.replace("/", "\\") + "\\a b")
            finally:
                del os.environ["DESKLING_VM_SHARE"]

    def test_each_checkout_gets_its_own_folder(self):
        with tempfile.TemporaryDirectory() as temp:
            one, two = Path(temp) / "my app", Path(temp) / "wt" / "my app"
            self.assertRegex(vm.checkout_key(one), r"^my-app-[0-9a-f]{6}$")
            self.assertEqual(vm.checkout_key(one), vm.checkout_key(one))
            self.assertNotEqual(vm.checkout_key(one), vm.checkout_key(two))

    def test_powershell_quoting_and_encoding(self):
        self.assertEqual(vm.ps("it's"), "'it''s'")
        script = "Write-Output 'ciao'"
        self.assertEqual(base64.b64decode(vm.encode(script)).decode("utf-16-le"), script)

    def test_platforms(self):
        self.assertEqual(vm.parse_platforms([]), [])
        self.assertEqual(vm.parse_platforms(["arm64", "ARM64", "X64"]), ["ARM64", "x64"])
        with self.assertRaises(vm.Usage):
            vm.parse_platforms(["x86"])

    def test_extra_arguments_follow_the_dashes(self):
        self.assertEqual(vm.split_extra(["x64", "--", "-c", "Release"]), (["x64"], "-c Release"))
        self.assertEqual(vm.split_extra(["ARM64"]), (["ARM64"], ""))


class BodyTests(unittest.TestCase):
    """The PowerShell each command runs in the VM, over the fixture app's companion.json."""

    def setUp(self):
        self.app = fixture_app()
        self.root = self.app.__enter__()
        import companion

        config = companion.load(self.root)
        self.ctx = vm.Context(self.root, config, "Windows 11", vm.checkout_key(self.root), self.root / "stage")

    def tearDown(self):
        self.app.__exit__(None, None, None)

    def test_build_both_platforms_with_extra_arguments(self):
        body = vm.build_body(self.ctx, ["x64", "ARM64"], "-c Release")
        project = r"'windows\Fixture.App\Fixture.App.csproj'"
        self.assertIn(f"dotnet build {project} -p:Platform=x64 -v q -nologo '-clp:NoSummary' -c Release", body)
        self.assertIn(f"dotnet build {project} -p:Platform=ARM64 -v q -nologo '-clp:NoSummary' -c Release", body)
        self.assertTrue(body.endswith("Exit-Steps"))

    def test_test_runs_the_test_projects(self):
        body = vm.test_body(self.ctx, "")
        self.assertIn(r"dotnet test 'windows\Fixture.App.Tests\Fixture.App.Tests.csproj'", body)
        self.assertNotIn("Core.Tests", body, "the fixture names no windows.coreTests")

    def test_screenshots_take_the_apps_wrapper_when_it_has_one(self):
        shots = Path("/tmp/shots")
        body = vm.screenshots_body(self.ctx, shots, "-Language it")
        self.assertIn(vm.guest_path(vm.HERE / "screenshots.ps1"), body, "Deskling's, through the share")
        self.assertIn("-Out $shots -Language it", body)
        self.assertIn(f"Copy-Shots $shots {vm.ps(vm.guest_path(shots))}", body)
        wrapper = self.root / "windows" / "scripts" / "screenshots.ps1"
        wrapper.parent.mkdir(parents=True, exist_ok=True)
        wrapper.write_text("& \"$PSScriptRoot/../../deskling/tools/windows/screenshots.ps1\" @args\n", "utf8")
        self.assertIn(r"& 'windows\scripts\screenshots.ps1' -Out $shots", vm.screenshots_body(self.ctx, shots, ""))

    def test_smoke_publishes_then_runs_the_smoke_script(self):
        body = vm.smoke_body(self.ctx, None, "-SideBySide")
        self.assertIn("$platform = Get-GuestPlatform", body)
        self.assertIn("dotnet publish 'windows\\Fixture.App\\Fixture.App.csproj' -c Release -p:Platform=$platform", body)
        self.assertIn("-Package $msix.FullName -SideBySide", body)
        self.assertIn("$platform = 'x64'", vm.smoke_body(self.ctx, "x64", ""))

    def test_the_script_loads_the_guest_functions_from_the_share(self):
        script = vm.guest_script(self.ctx, "exit 0")
        self.assertIn(f". {vm.ps(vm.guest_path(vm.GUEST_SCRIPT))}", script)
        self.assertIn(f"Resolve-GuestFolder '%USERPROFILE%\\deskling-vm\\{self.ctx.key}'", script)
        self.assertIn("catch", script)


class UnavailableTests(unittest.TestCase):
    """vm.sh with a fake prlctl, or none: one line and exit 3 when there's no VM to use."""

    def setUp(self):
        self.app = fixture_app()
        self.root = self.app.__enter__()
        self.bin = self.root / "fake-bin"
        self.bin.mkdir()

    def tearDown(self):
        self.app.__exit__(None, None, None)

    def prlctl(self, status: str, exec_code: int = 0) -> None:
        path = self.bin / "prlctl"
        path.write_text(f'#!/bin/sh\ncase "$1" in\n  status) {status} ;;\n  exec) exit {exec_code} ;;\n'
                        '  *) exit 9 ;;\nesac\n', "utf8")
        path.chmod(0o755)

    def run_vm(self, *args: str, path: str | None = None) -> subprocess.CompletedProcess:
        env = {**os.environ, "PATH": path or f"{self.bin}:{os.environ['PATH']}", "HOME": str(self.root)}
        return subprocess.run([str(VM_SH), *args], cwd=self.root, env=env, capture_output=True, text=True)

    def test_without_prlctl(self):
        python = Path(sys.executable).parent
        for command in ("status", "build", "screenshots"):
            done = self.run_vm(command, *(["shots"] if command == "screenshots" else []),
                               path=f"{self.bin}:{python}:/usr/bin:/bin")
            if shutil.which("prlctl", path=f"{python}:/usr/bin:/bin"):
                self.skipTest("prlctl is in /usr/bin")
            self.assertEqual(done.returncode, vm.UNAVAILABLE, done.stderr)
            self.assertEqual(done.stderr.count("\n"), 1, done.stderr)
            self.assertIn("no prlctl here", done.stderr)

    def test_unknown_stopped_and_suspended_vms(self):
        self.prlctl('echo "Failed to get VM config: The virtual machine could not be found."; exit 255')
        done = self.run_vm("build")
        self.assertEqual(done.returncode, vm.UNAVAILABLE)
        self.assertIn("no VM named 'Windows 11'", done.stderr)

        self.prlctl('echo "VM $2 exist stopped"')
        done = self.run_vm("status", path=f"{self.bin}:{os.environ['PATH']}")
        self.assertEqual(done.returncode, vm.UNAVAILABLE)
        self.assertIn("'Windows 11' is stopped", done.stderr)

        self.prlctl('echo "VM $2 exist suspended"')
        done = self.run_vm("status")
        self.assertEqual(done.returncode, 0, done.stderr)
        self.assertIn("suspended; vm.sh resumes it", done.stdout)

    def test_running_needs_someone_signed_in(self):
        self.prlctl('echo "VM $2 exist running"', exec_code=1)
        done = self.run_vm("status")
        self.assertEqual(done.returncode, vm.UNAVAILABLE)
        self.assertIn("no one is signed in", done.stderr)
        self.prlctl('echo "VM $2 exist running"')
        done = self.run_vm("status")
        self.assertEqual((done.returncode, done.stdout.strip()), (0, "vm.sh: the VM 'Windows 11' is ready"))
        done = subprocess.run([str(VM_SH), "status"], cwd=self.root, capture_output=True, text=True,
                              env={**os.environ, "PATH": f"{self.bin}:{os.environ['PATH']}", "DESKLING_VM": "Dev Box"})
        self.assertIn("'Dev Box' is ready", done.stdout)

    def test_a_lost_job_is_tried_again(self):
        count = self.root / "count"
        path = self.bin / "prlctl"
        path.write_text(f'''#!/bin/sh
n=$(cat "{count}" 2>/dev/null || echo 0); n=$((n + 1)); echo $n > "{count}"
if [ $n -lt 3 ]; then echo "PrlJob_GetResult: Invalid argument. An invalid argument was passed." >&2; exit 255; fi
echo "ran $*"
''', "utf8")
        path.chmod(0o755)
        previous = os.environ["PATH"]
        os.environ["PATH"] = f"{self.bin}:{previous}"
        try:
            done = vm.prlctl_exec(["Windows 11", "cmd", "/c", "exit 0"], capture=True)
            self.assertEqual((done.returncode, done.stdout.strip()), (0, "ran exec Windows 11 cmd /c exit 0"))
            self.assertEqual(count.read_text().strip(), "3")
            count.unlink()
            done = vm.prlctl_exec(["Windows 11", "cmd"], capture=True, attempts=2)
            self.assertEqual(done.returncode, 255, "gives up after the attempts")
            self.assertIn("PrlJob_GetResult", done.stderr)
        finally:
            os.environ["PATH"] = previous

    def test_usage_mistakes(self):
        self.prlctl('echo "VM $2 exist running"')
        for args, words in ((["bogus"], "unknown command"), (["screenshots"], "one folder"),
                            (["run"], "after --"), (["build", "x86"], "unknown platform"),
                            (["test", "fast"], "ARGS go after --"),
                            (["smoke", "x64", "ARM64"], "one platform")):
            with self.subTest(args=args):
                done = self.run_vm(*args)
                self.assertEqual(done.returncode, 2, done.stderr)
                self.assertIn(words, done.stderr)


@unittest.skipUnless(POWERSHELL, "needs PowerShell")
class SyncTreeTests(unittest.TestCase):
    """vm-guest.ps1's Sync-Tree over a folder: unpack, delete what the Mac no longer has, keep bin/obj, list the rest."""

    def test_sync_tree(self):
        with tempfile.TemporaryDirectory() as temp:
            temp = Path(temp).resolve()
            mac, root, stage = temp / "mac", temp / "vm", temp / "stage"
            for folder in (mac, root, stage):
                folder.mkdir()
            for name, text in (("a/App.cs", "new"), ("b.txt", "same"), ("c.txt", "later")):
                (mac / name).parent.mkdir(parents=True, exist_ok=True)
                (mac / name).write_text(text, "utf8")
                os.utime(mac / name, (1_700_000_000, 1_700_000_000))
            vm.write_tar(mac, ["a/App.cs", "b.txt"], stage / "sync.tar")
            for name in ("a/App.cs", "stale/Old.cs", "obj/project.assets.json", "a/bin/App.dll"):
                (root / name).parent.mkdir(parents=True, exist_ok=True)
                (root / name).write_text("old", "utf8")
            vm.write_list([(n, (mac / n).stat().st_size, 1_700_000_000) for n in ("a/App.cs", "b.txt", "c.txt")],
                          stage / "files.tsv")
            done = subprocess.run([POWERSHELL, "-NoProfile", "-NonInteractive", "-Command",
                                   f". {vm.ps(str(vm.GUEST_SCRIPT))}; Sync-Tree {vm.ps(str(root))} {vm.ps(str(stage))}"],
                                  capture_output=True, text=True)
            self.assertEqual(done.returncode, 0, done.stdout + done.stderr)
            self.assertIn(f"synced\t{root}\t1", done.stdout)
            self.assertEqual((root / "a/App.cs").read_text("utf8"), "new")
            self.assertEqual((root / "b.txt").read_text("utf8"), "same")
            self.assertFalse((root / "stale").exists(), "a folder left empty goes too")
            self.assertTrue((root / "obj/project.assets.json").exists())
            self.assertTrue((root / "a/bin/App.dll").exists())
            self.assertEqual((stage / "need.txt").read_text("utf8").split(), ["c.txt"])


if __name__ == "__main__":
    unittest.main()
