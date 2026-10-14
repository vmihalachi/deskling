#!/usr/bin/env python3
"""Tests for tools/windows/: check-winui.py's rules, assets.py's images from the fixture app's icon, smoke-package.ps1's
package check and the PowerShell scripts parsing (where PowerShell is installed), companion.targets as MSBuild XML."""

from __future__ import annotations

import io
import json
import shutil
import struct
import subprocess
import sys
import tempfile
import unittest
import xml.etree.ElementTree as ElementTree
import zipfile
import zlib
from contextlib import redirect_stderr, redirect_stdout
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "windows"))
import assets  # noqa: E402
from support import TOOLS, fixture_app  # noqa: E402

WINDOWS = TOOLS / "windows"
CHECK_WINUI = WINDOWS / "check-winui.py"
POWERSHELL = shutil.which("pwsh") or shutil.which("powershell")


def icon_png(path: Path, size: int, margin: int, rgba=(200, 30, 40, 255)) -> None:
    """A square icon: an opaque block with a transparent `margin` around it, as a Mac app icon leaves."""
    inside = range(margin, size - margin)
    raw = b"".join(
        b"\x00" + b"".join(bytes(rgba) if x in inside and y in inside else b"\0\0\0\0" for x in range(size))
        for y in range(size))

    def chunk(kind: bytes, data: bytes) -> bytes:
        return struct.pack(">I", len(data)) + kind + data + struct.pack(">I", zlib.crc32(kind + data) & 0xFFFFFFFF)

    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_bytes(b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB", size, size, 8, 6, 0, 0, 0))
                     + chunk(b"IDAT", zlib.compress(raw)) + chunk(b"IEND", b""))


class CheckWinUITests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.root = Path(self.temp.name)

    def tearDown(self):
        self.temp.cleanup()

    def lint(self, *paths: Path) -> subprocess.CompletedProcess:
        return subprocess.run([sys.executable, str(CHECK_WINUI), *map(str, paths)], capture_output=True, text=True)

    def write(self, name: str, text: str) -> Path:
        path = self.root / name
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(text, "utf8")
        return path

    def test_untyped_xaml_casts(self):
        for line in (
            "var s = (Style)Application.Current.Resources[\"Card\"];",
            "var p = GetTemplateChild(\"Part\") as Border;",
            "if (VisualTreeHelper.GetParent(e) is Grid g) {}",
            "if (window.AppWindow.Presenter is OverlappedPresenter p) {}",
            "var b = (Brush)e.NewValue;",
            "var f = (FontFamily?)GetValue(FontProperty);",
        ):
            with self.subTest(line=line):
                done = self.lint(self.write("A.cs", line + "\n"))
                self.assertEqual(done.returncode, 1, done.stdout)
                self.assertIn("XamlCast", done.stdout)

    def test_fine_csharp(self):
        path = self.write("B.cs", "\n".join((
            "var s = XamlCast.As<Style>(Application.Current.Resources[\"Card\"]);",
            "if (GetTemplateChild(\"Part\") is null) return;",
            "var n = (double)e.NewValue;",
            "var b = (Brush)e.NewValue; // winui-check: ok, set in code to a SolidColorBrush",
        )))
        done = self.lint(path)
        self.assertEqual(done.returncode, 0, done.stdout)
        self.assertIn("ok: 1 WinUI files", done.stdout)

    def test_xaml_rules(self):
        for line, words in (
            ('<Setter Property="Data" Value="M0,0 L1,1" />', "set it on each element"),
            ('<Setter Property="Shapes.StrokeDashArray" Value="2 2" />', "set it on each element"),
            ('<Grid x:Name="Language" />', "hides FrameworkElement.Language"),
            ('<Border Margin="{x:Bind local:Inset.Of(Model, 12)}" />', "WMC9999"),
            ('<Border Margin="{x:Bind Inset(-0.5)}" />', "WMC9999"),
        ):
            with self.subTest(line=line):
                done = self.lint(self.write("Proj/Views/A.xaml", line + "\n"))
                self.assertEqual(done.returncode, 1, done.stdout)
                self.assertIn(words, done.stdout)
        fine = self.write("Proj/Views/B.xaml", '<Border Margin="{x:Bind local:Inset.Card}" Tag="{x:Bind Name(Model)}" />\n')
        self.assertEqual(self.lint(fine).returncode, 0)

    def test_the_app_xaml_sits_next_to_the_project(self):
        app = '<shell:CompanionApp x:Class="Fixture.App" xmlns:shell="using:Deskling.Windows.Companion">\n</shell:CompanionApp>\n'
        self.write("Proj/Proj.csproj", "<Project />\n")
        nested = self.write("Proj/App/App.xaml", app)
        done = self.lint(nested)
        self.assertEqual(done.returncode, 1, done.stdout)
        self.assertIn("must sit next to the project file", done.stdout)
        self.assertEqual(self.lint(self.write("Proj/App.xaml", app)).returncode, 0)

    def test_a_folder_skips_build_output_and_no_arguments_read_companion_json(self):
        self.write("Proj/Views/A.cs", "var x = 1;\n")
        self.write("Proj/obj/Generated.cs", "var s = (Style)Resources[\"x\"];\n")
        done = self.lint(self.root / "Proj")
        self.assertEqual(done.returncode, 0, done.stdout)
        self.assertIn("ok: 1 WinUI files", done.stdout)
        with fixture_app() as app:
            bad = app / "windows/Fixture.App/Views/Home.cs"
            bad.parent.mkdir(parents=True)
            bad.write_text("var s = (Style)Resources[\"x\"];\n", "utf8")
            done = subprocess.run([sys.executable, str(CHECK_WINUI)], capture_output=True, text=True, cwd=app)
            self.assertEqual(done.returncode, 1, done.stdout + done.stderr)
            self.assertIn("Home.cs:1", done.stdout)


class AssetsTests(unittest.TestCase):
    def run_assets(self, *args: str) -> tuple[int, str]:
        out, err = io.StringIO(), io.StringIO()
        with redirect_stdout(out), redirect_stderr(err):
            code = assets.main(list(args))
        return code, out.getvalue() + err.getvalue()

    def test_the_set_from_the_icon_and_the_check(self):
        with fixture_app() as root:
            icon_png(root / "macos/Fixture/Resources/AppIcon.png", 96, margin=16)
            icon_png(root / "macos/Fixture/Resources/AppIcon-Dev.png", 96, margin=16, rgba=(30, 30, 200, 255))
            code, said = self.run_assets("--check")
            self.assertEqual(code, 1, "nothing written yet")
            self.assertIn("StoreLogo.scale-100.png", said)

            self.assertEqual(self.run_assets()[0], 0)
            out = root / "windows/Fixture.App/Assets"
            names = sorted(p.relative_to(out).as_posix() for p in out.rglob("*") if p.is_file())
            self.assertIn("Square44x44Logo.targetsize-24_altform-unplated.png", names)
            self.assertIn("Wide310x150Logo.scale-200.png", names)
            self.assertIn("Dev/AppIcon.ico", names)
            self.assertEqual(len(names), 22)

            # The trimmed icon fills a plain logo edge to edge; the wide one is centred with clear sides.
            small = assets.read_png(out / "Square44x44Logo.targetsize-16.png")
            self.assertEqual((small.width, small.height), (16, 16))
            self.assertGreater(small.pixels[3], 250, "the corner is drawn: the margin was trimmed")
            wide = assets.read_png(out / "Wide310x150Logo.scale-100.png")
            self.assertEqual((wide.width, wide.height), (310, 150))
            self.assertEqual(wide.pixels[3], 0)
            middle = 4 * (75 * 310 + 155)
            self.assertGreater(wide.pixels[middle + 3], 250)

            ico = (out / "AppIcon.ico").read_bytes()
            _, kind, count = struct.unpack("<HHH", ico[:6])
            self.assertEqual((kind, count), (1, len(assets.ICO_SIZES)))
            sides = [ico[6 + 16 * i] or 256 for i in range(count)]
            self.assertEqual(sides, assets.ICO_SIZES)
            self.assertNotEqual(ico, (out / "Dev/AppIcon.ico").read_bytes())

            self.assertEqual(self.run_assets("--check")[0], 0)
            (out / "StoreLogo.scale-200.png").write_bytes(b"changed")
            code, said = self.run_assets("--check")
            self.assertEqual(code, 1)
            self.assertIn("StoreLogo.scale-200.png", said)
            self.assertNotIn("StoreLogo.scale-100.png", said)

    def test_needs_the_section(self):
        with fixture_app() as root:
            config = root / "companion.json"
            data = json.loads(config.read_text("utf8"))
            del data["windows"]["assets"]
            config.write_text(json.dumps(data), "utf8")
            code, said = self.run_assets()
            self.assertEqual(code, 1)
            self.assertIn("no windows.assets", said)


class TargetsTests(unittest.TestCase):
    def test_companion_targets_is_msbuild(self):
        project = ElementTree.parse(WINDOWS / "companion.targets").getroot()
        self.assertEqual(project.tag, "Project")
        targets = {t.get("Name") for t in project.iter("Target")}
        self.assertIn("CompanionRemoveRuntimeFilesFromNativePackage", targets)
        self.assertIn("CompanionKeepVoiceOutOfPri", targets)


@unittest.skipUnless(POWERSHELL, "needs PowerShell")
class PowerShellTests(unittest.TestCase):
    def powershell(self, *args: str) -> subprocess.CompletedProcess:
        return subprocess.run([POWERSHELL, "-NoProfile", "-NonInteractive", *args], capture_output=True, text=True)

    def test_the_scripts_parse(self):
        for script in sorted(WINDOWS.glob("*.ps1")):
            with self.subTest(script=script.name):
                done = self.powershell("-Command", (
                    "$errors = $null; [void][System.Management.Automation.Language.Parser]::ParseFile("
                    f"'{script}', [ref]$null, [ref]$errors); $errors | ForEach-Object {{ $_.ToString() }}"))
                self.assertEqual(done.returncode, 0, done.stderr)
                self.assertEqual(done.stdout.strip(), "")

    def package(self, folder: Path, *names: str) -> Path:
        manifest = ('<Package xmlns="http://schemas.microsoft.com/appx/manifest/foundation/windows10">'
                    '<Identity Name="Fixture" Publisher="CN=Fixture" Version="1.0.0.0" ProcessorArchitecture="x64" />'
                    '<Applications><Application Id="App" Executable="Fixture.exe" /></Applications></Package>')
        path = folder / "Fixture.msix"
        with zipfile.ZipFile(path, "w") as package:
            package.writestr("AppxManifest.xml", manifest)
            for name in names:
                package.writestr(name, b"x")
        return path

    def test_smoke_package_check_only(self):
        with tempfile.TemporaryDirectory() as temp:
            folder = Path(temp)

            def check(*names: str) -> subprocess.CompletedProcess:
                package = self.package(folder, *names)
                return self.powershell("-File", str(WINDOWS / "smoke-package.ps1"), "-Package", str(package), "-CheckOnly")

            done = check("Fixture.exe", "Microsoft.WindowsAppRuntime.Bootstrap.dll")
            self.assertEqual(done.returncode, 0, done.stdout + done.stderr)
            self.assertIn("native, 3 files", done.stdout)
            done = check("Fixture.exe", "Fixture.runtimeconfig.json")
            self.assertNotEqual(done.returncode, 0)
            self.assertIn("runtimeconfig.json", done.stdout + done.stderr)
            done = check("Other.exe")
            self.assertNotEqual(done.returncode, 0)
            self.assertIn("has no Fixture.exe", done.stdout + done.stderr)


if __name__ == "__main__":
    unittest.main()
