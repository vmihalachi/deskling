<#
Renders a companion app's windows to PNGs, in light and dark (or in the contrast theme alone, while Windows is in one):
the app's screenshot run (Deskling.Windows.Companion's ScreenshotCapture) over sample state, never the user's
settings. Windows only. Reads companion.json (found from the current folder up, or $env:COMPANION_ROOT) for the app's
project (windows.dir + windows.app) and where the shots go (windows.screenshots).

Usage: deskling/tools/windows/screenshots.ps1 [-Only name,name] [-Hidden|-Front] [-Out folder] [-Language tag]
                                              [-Platform x64|ARM64] [-Configuration Debug|Release] [-NoBuild|-BuildOnly]
                                              [-Project path\to\App.csproj]
  -Only           just the shots whose file names start with one of the names (home, settings, onboarding, …), for
                  checking a change. Pair it with a scratch -Out so the committed screenshots stay as they are. Implies
                  -Hidden.
  -Hidden         the windows open cloaked (drawn and captured, never on screen) and never take the focus, so you can
                  keep working, and each is shot once it has drawn rather than after a fixed wait; the shots show them
                  inactive (grey caption buttons). For checks, never the committed shots. An app on a Deskling.Windows.Companion
                  older than 0.10.5 opens its windows in front, as before.
  -Front          the windows come forward and are shot active, as in use (the default without -Only); leave the PC alone.
  -Language       the language to shoot in ("it"); the app's own choice otherwise.
  -Configuration  Debug (the default) builds and runs the unpackaged Debug app. Release publishes the Native AOT app
                  unpackaged with screenshot mode compiled in (-p:ScreenshotMode=true), so the shots come from the build
                  that ships: Native AOT breaks some things a JIT build hides. It builds from scratch in
                  <project>\obj\screenshots\<platform>, emptied first, never in the obj\ and bin\ a packaged build uses.
  -NoBuild        run the last build (check-layout.ps1 builds once, then runs the languages side by side).
  -BuildOnly      just build.
  -Project        another WinUI project than companion.json's (Deskling's fixture app).
Fails, after copying the shots, when the run reports problems (problems.txt, left next to the shots): a window that
didn't open or couldn't be drawn, a shot that wasn't written, text that doesn't fit (LayoutCheck); or when the app
didn't finish the run (it crashed or was closed partway, so some windows have no shot). check-layout.ps1 runs it per
language.
#>
param(
    [string[]] $Only = @(),
    [string] $Out = "",
    [string] $Language = "",
    [ValidateSet("x64", "ARM64")][string] $Platform = $(if ([Runtime.InteropServices.RuntimeInformation]::OSArchitecture -eq "Arm64") { "ARM64" } else { "x64" }),
    [ValidateSet("Debug", "Release")][string] $Configuration = "Debug",
    [string] $Project = "",
    [switch] $Hidden,
    [switch] $Front,
    [switch] $NoBuild,
    [switch] $BuildOnly
)
$ErrorActionPreference = "Stop"
# -Only home,settings arrives as two strings.
$Only = @($Only | Where-Object { $_ }) -join ","

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
$hide = -not $Front -and ($Hidden -or $Only)

if ($Configuration -eq "Release") {
    # Native AOT links with the Visual Studio tools, which it finds through vswhere: put it on the path when the
    # installer's copy isn't there (a plain shell, a hosted runner).
    $installer = Join-Path ${env:ProgramFiles(x86)} "Microsoft Visual Studio\Installer"
    if (-not (Get-Command vswhere.exe -ErrorAction SilentlyContinue) -and (Test-Path (Join-Path $installer "vswhere.exe"))) {
        $env:PATH = "$installer;$env:PATH"
    }
    # Everything this build writes (restore, intermediate, output, publish) goes to a folder of its own per platform under
    # the project's obj\, emptied first. In the project's own obj\ and bin\, after a packaged publish
    # (GenerateAppxPackageOnBuild), the incremental build kept what it had made for the package and the app died at
    # start (a XAML parse failure, 0xC000027B); a publish folder kept between runs mixed in the other platform's files.
    # Right after a run, Windows can hold one of the app's DLLs for a few seconds (the first time an x64 build runs from
    # that folder on ARM64; nothing has it loaded), so the removal tries again for up to a minute.
    $artifacts = Join-Path (Split-Path $Project) "obj\screenshots\$Platform"
    $publish = Join-Path $artifacts "app"
    if (-not $NoBuild) {
        for ($try = 1; Test-Path $artifacts; $try++) {
            try { Remove-Item -LiteralPath $artifacts -Recurse -Force -ErrorAction Stop }
            catch { if ($try -ge 60) { throw }; Start-Sleep -Seconds 1 }
        }
        dotnet publish $Project -c Release -p:Platform=$Platform -p:WindowsPackageType=None -p:ScreenshotMode=true --artifacts-path $artifacts -o $publish -v quiet -nologo | Out-Null
        if ($LASTEXITCODE -ne 0) { throw "The app didn't publish." }
    }
    $folder = $publish
    $assembly = (dotnet msbuild $Project -getProperty:AssemblyName).Trim()
}
else {
    if (-not $NoBuild) {
        dotnet build $Project -p:Platform=$Platform -c Debug -v quiet -nologo | Out-Null
        if ($LASTEXITCODE -ne 0) { throw "The app didn't build." }
    }
    # One evaluation for both (each costs about a second).
    $properties = (dotnet msbuild $Project -p:Platform=$Platform -p:Configuration=Debug -getProperty:TargetDir -getProperty:AssemblyName | Out-String | ConvertFrom-Json).Properties
    $folder = $properties.TargetDir
    $assembly = $properties.AssemblyName
}
$exe = Join-Path $folder "$assembly.exe"
if (-not (Test-Path $exe)) { throw "No $exe$(if ($NoBuild) { '; build it first (without -NoBuild)' } else { ' after the build' })." }
if ($BuildOnly) { exit 0 }

# A folder per run, so runs in several languages at once (check-layout.ps1) don't share one; removed once copied.
$work = Join-Path ([IO.Path]::GetTempPath()) "$name-screenshots-$PID.run"
$env:DESKLING_SCREENSHOT_MODE = if ($Only) { $Only } else { "all" }
$env:DESKLING_SCREENSHOT_DIR = $work
$env:DESKLING_SCREENSHOT_LANGUAGE = $Language
$env:DESKLING_SCREENSHOT_HIDDEN = if ($hide) { "1" } else { "0" }
try {
    $run = Start-Process $exe -PassThru
    if (-not $run.WaitForExit(300000)) { $run.Kill(); throw "The screenshot run didn't finish in 5 minutes." }
}
finally {
    Remove-Item Env:DESKLING_SCREENSHOT_MODE, Env:DESKLING_SCREENSHOT_DIR, Env:DESKLING_SCREENSHOT_LANGUAGE, Env:DESKLING_SCREENSHOT_HIDDEN -ErrorAction SilentlyContinue
}
$shots = @(Get-ChildItem $work -Filter *.png -ErrorAction SilentlyContinue)
if ($shots.Count -eq 0) {
    Remove-Item -LiteralPath $work -Recurse -Force -ErrorAction SilentlyContinue
    throw "No screenshots were rendered (exit code $($run.ExitCode))."
}
New-Item -ItemType Directory -Force $Out | Out-Null
Remove-Item (Join-Path $Out "problems.txt") -ErrorAction SilentlyContinue
foreach ($shot in $shots) {
    Copy-Item $shot.FullName $Out -Force
}
"$($shots.Count) screenshots in $Out"
$problems = Join-Path $work "problems.txt"
$found = @(if (Test-Path $problems) { Get-Content $problems -Encoding UTF8 })
Remove-Item -LiteralPath $work -Recurse -Force -ErrorAction SilentlyContinue
if ($found.Count -gt 0) {
    $found | Set-Content (Join-Path $Out "problems.txt") -Encoding UTF8
    [Console]::Error.WriteLine("Problems$(if ($Language) { " ($Language)" }):")
    $found | ForEach-Object { [Console]::Error.WriteLine("  $_") }
    exit 1
}
if ($run.ExitCode -ne 0) {
    [Console]::Error.WriteLine("The app stopped before the end of the run (exit code $($run.ExitCode)), so some windows have no shot.")
    exit 1
}
