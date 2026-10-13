# Renders the fixture app's windows to PNGs, in light and dark (or in the contrast theme alone, while Windows is in one),
# through tools/windows/screenshots.ps1, the runner every companion app uses. Windows only.
# Usage: scripts/fixture-screenshots.ps1 [-Only name,name] [-Out folder] [-Configuration Debug|Release]
#   -Only   just the shots whose file names start with one of the names (home, settings, onboarding, support, panel,
#           desktop).
#   -Out    where the PNGs go (default: Fixture.App-shots in the temporary folder).
# Fails, after copying the shots, when the run reports problems (problems.txt): a window that didn't open or couldn't
# be drawn, a shot that wasn't written, text that doesn't fit (LayoutCheck).
param([string] $Only = "", [string] $Out = "", [ValidateSet("Debug", "Release")][string] $Configuration = "Debug")
$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot
& (Join-Path $root "tools/windows/screenshots.ps1") -Project (Join-Path $root "dotnet/Fixture.App/Fixture.App.csproj") -Only $Only -Out $Out -Configuration $Configuration
exit $LASTEXITCODE
