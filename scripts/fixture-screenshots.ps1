# Renders the fixture app's windows to PNGs, in light and dark (or in the contrast theme alone, while Windows is in one):
# the Windows shell's screenshot run (Deskling.Windows.Companion's ScreenshotCapture) over sample state, never the
# user's settings. Windows only.
# Usage: scripts/fixture-screenshots.ps1 [-Only name,name] [-Out folder]   (default: deskling-fixture-shots in the
#        temporary folder)
#   -Only   just the shots whose file names start with one of the names (home, settings, onboarding, support, panel,
#           desktop).
# Fails, after copying the shots, when the run reports problems (problems.txt): a window that didn't open or couldn't
# be drawn, a shot that wasn't written, text that doesn't fit (LayoutCheck).
param([string] $Only = "", [string] $Out = "")
$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot
$platform = if ($env:PROCESSOR_ARCHITECTURE -eq "ARM64") { "ARM64" } else { "x64" }
dotnet build "$root/dotnet/Fixture.App" -p:Platform=$platform -c Debug -v quiet -nologo | Out-Null
if ($LASTEXITCODE -ne 0) { throw "The fixture app didn't build." }
$exe = Get-ChildItem "$root/dotnet/Fixture.App/bin/$platform/Debug" -Recurse -Filter Fixture.App.exe | Select-Object -First 1
$work = Join-Path ([IO.Path]::GetTempPath()) "deskling-fixture-screenshots.run"
if (-not $Out) { $Out = Join-Path ([IO.Path]::GetTempPath()) "deskling-fixture-shots" }
$env:DESKLING_SCREENSHOT_MODE = if ($Only) { $Only } else { "all" }
$env:DESKLING_SCREENSHOT_DIR = $work
try {
    $run = Start-Process $exe.FullName -PassThru
    if (-not $run.WaitForExit(300000)) { $run.Kill(); throw "The screenshot run didn't finish in 5 minutes." }
}
finally {
    Remove-Item Env:DESKLING_SCREENSHOT_MODE, Env:DESKLING_SCREENSHOT_DIR -ErrorAction SilentlyContinue
}
$shots = @(Get-ChildItem $work -Filter *.png -ErrorAction SilentlyContinue)
if ($shots.Count -eq 0) { throw "No screenshots were rendered." }
New-Item -ItemType Directory -Force $Out | Out-Null
foreach ($shot in $shots) {
    Copy-Item $shot.FullName $Out -Force
    Join-Path $Out $shot.Name
}
"$($shots.Count) screenshots in $Out"
$problems = Join-Path $work "problems.txt"
if (Test-Path $problems) {
    [Console]::Error.WriteLine("Problems:")
    Get-Content $problems | ForEach-Object { [Console]::Error.WriteLine("  $_") }
    exit 1
}
