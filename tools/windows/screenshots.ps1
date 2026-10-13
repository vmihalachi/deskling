<#
Renders a companion app's windows to PNGs, in light and dark (or in the contrast theme alone, while Windows is in one):
the app's screenshot run (Deskling.Windows.Companion's ScreenshotCapture) over sample state, never the user's
settings. Windows only. Reads companion.json (found from the current folder up, or $env:COMPANION_ROOT) for the app's
project (windows.dir + windows.app) and where the shots go (windows.screenshots).

Usage: deskling/tools/windows/screenshots.ps1 [-Only name,name] [-Out folder] [-Language tag] [-Platform x64|ARM64]
                                              [-Configuration Debug|Release] [-Project path\to\App.csproj]
  -Only           just the shots whose file names start with one of the names (home, settings, onboarding, …).
  -Language       the language to shoot in ("it"); the app's own choice otherwise.
  -Configuration  Debug (the default) builds and runs the unpackaged Debug app. Release publishes the Native AOT app
                  unpackaged with screenshot mode compiled in (-p:ScreenshotMode=true), so the shots come from the build
                  that ships: Native AOT breaks some things a JIT build hides.
  -Project        another WinUI project than companion.json's (Deskling's fixture app).
Fails, after copying the shots, when the run reports problems (problems.txt): a window that didn't open or couldn't be
drawn, a shot that wasn't written, text that doesn't fit (LayoutCheck).
#>
param(
    [string] $Only = "",
    [string] $Out = "",
    [string] $Language = "",
    [ValidateSet("x64", "ARM64")][string] $Platform = $(if ([Runtime.InteropServices.RuntimeInformation]::OSArchitecture -eq "Arm64") { "ARM64" } else { "x64" }),
    [ValidateSet("Debug", "Release")][string] $Configuration = "Debug",
    [string] $Project = ""
)
$ErrorActionPreference = "Stop"

function Find-Root {
    if ($env:COMPANION_ROOT) { return (Resolve-Path $env:COMPANION_ROOT).Path }
    $dir = (Get-Location).Path
    while ($dir) {
        if (Test-Path (Join-Path $dir "companion.json")) { return $dir }
        $parent = Split-Path $dir
        if ($parent -eq $dir) { break }
        $dir = $parent
    }
    throw "No companion.json here or above; set COMPANION_ROOT."
}

if (-not $Project) {
    $root = Find-Root
    $config = Get-Content (Join-Path $root "companion.json") -Raw | ConvertFrom-Json
    if (-not $config.windows -or -not $config.windows.app) { throw "companion.json has no windows.app." }
    $Project = Join-Path $root "$($config.windows.dir)/$($config.windows.app)/$($config.windows.app).csproj"
    if (-not $Out) {
        $folder = if ($config.windows.screenshots) { $config.windows.screenshots } else { "screenshots/windows" }
        $Out = Join-Path $root $folder
    }
}
$Project = (Resolve-Path $Project).Path
$name = [IO.Path]::GetFileNameWithoutExtension($Project)
if (-not $Out) { $Out = Join-Path ([IO.Path]::GetTempPath()) "$name-shots" }
$Out = [IO.Path]::GetFullPath($Out)

if ($Configuration -eq "Release") {
    # Native AOT links with the Visual Studio tools, which it finds through vswhere: put it on the path when the
    # installer's copy isn't there (a plain shell, a hosted runner).
    $installer = Join-Path ${env:ProgramFiles(x86)} "Microsoft Visual Studio\Installer"
    if (-not (Get-Command vswhere.exe -ErrorAction SilentlyContinue) -and (Test-Path (Join-Path $installer "vswhere.exe"))) {
        $env:PATH = "$installer;$env:PATH"
    }
    $publish = Join-Path ([IO.Path]::GetTempPath()) "$name-screenshots.publish"
    dotnet publish $Project -c Release -p:Platform=$Platform -p:WindowsPackageType=None -p:ScreenshotMode=true -o $publish -v quiet -nologo | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "The app didn't publish." }
    $folder = $publish
}
else {
    dotnet build $Project -p:Platform=$Platform -c Debug -v quiet -nologo | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "The app didn't build." }
    $folder = (dotnet msbuild $Project -p:Platform=$Platform -p:Configuration=Debug -getProperty:TargetDir).Trim()
}
$assembly = (dotnet msbuild $Project -getProperty:AssemblyName).Trim()
$exe = Join-Path $folder "$assembly.exe"
if (-not (Test-Path $exe)) { throw "No $exe after the build." }

$work = Join-Path ([IO.Path]::GetTempPath()) "$name-screenshots.run"
$env:DESKLING_SCREENSHOT_MODE = if ($Only) { $Only } else { "all" }
$env:DESKLING_SCREENSHOT_DIR = $work
$env:DESKLING_SCREENSHOT_LANGUAGE = $Language
try {
    $run = Start-Process $exe -PassThru
    if (-not $run.WaitForExit(300000)) { $run.Kill(); throw "The screenshot run didn't finish in 5 minutes." }
}
finally {
    Remove-Item Env:DESKLING_SCREENSHOT_MODE, Env:DESKLING_SCREENSHOT_DIR, Env:DESKLING_SCREENSHOT_LANGUAGE -ErrorAction SilentlyContinue
}
$shots = @(Get-ChildItem $work -Filter *.png -ErrorAction SilentlyContinue)
if ($shots.Count -eq 0) { throw "No screenshots were rendered (exit code $($run.ExitCode))." }
New-Item -ItemType Directory -Force $Out | Out-Null
foreach ($shot in $shots) {
    Copy-Item $shot.FullName $Out -Force
}
"$($shots.Count) screenshots in $Out"
$problems = Join-Path $work "problems.txt"
if (Test-Path $problems) {
    [Console]::Error.WriteLine("Problems:")
    Get-Content $problems | ForEach-Object { [Console]::Error.WriteLine("  $_") }
    exit 1
}
