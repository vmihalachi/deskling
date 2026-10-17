<#
Installs a companion app's built .msix the way a customer gets it (fresh, no app data) and checks it starts, so a
package that crashes at launch never reaches the Store (mybackhurts' 1.0.1-1.0.3 did: every Native AOT build crashed on
first launch while Debug runs were fine). Run it on every package before uploading; the app's windows.yml runs it too.
Everything app-specific comes from the package's own manifest (its identity, its executable), so it works for any app.

Usage: laching/tools/windows/smoke-package.ps1 -Package <path to .msix> [-Seconds 20] [-SideBySide] [-Keep]
       laching/tools/windows/smoke-package.ps1 -Package <path to .msix> -CheckOnly

-CheckOnly installs nothing: it fails unless the package holds its executable and no runtimeconfig.json (a Native AOT
package needs no .NET runtime; a runtimeconfig.json means the packaging took the JIT build's files).
Plain mode launches the app from Start, waits, and fails unless it is still running with a window open (onboarding, on
a fresh install) and Windows logged no crash. Needs Developer Mode (the package is unsigned). Installs the Windows App
Runtime packages found next to the .msix (Dependencies\<arch>) when they're missing. Removes the test install afterwards
unless -Keep (for the hand checks a package needs: the startup task, a notification click after the app quit). Refuses
to touch a copy installed from the Store, so it never deletes real data; -SideBySide tests next to that copy instead, the
test install getting its own identity (the package name plus .SmokeTest) and its own toast activator class.
#>
param(
    [Parameter(Mandatory)][string]$Package,
    [int]$Seconds = 20,
    [switch]$SideBySide,
    [switch]$Keep,
    [switch]$CheckOnly
)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem
$Package = (Resolve-Path $Package).Path

function Read-Manifest([string]$path) {
    $zip = [IO.Compression.ZipFile]::OpenRead($path)
    try {
        $reader = New-Object IO.StreamReader($zip.GetEntry('AppxManifest.xml').Open())
        try { return [xml]$reader.ReadToEnd() } finally { $reader.Dispose() }
    } finally { $zip.Dispose() }
}

if ($CheckOnly) {
    $manifest = Read-Manifest $Package
    $exeName = ($manifest.Package.Applications.Application | Select-Object -First 1).Executable
    $zip = [IO.Compression.ZipFile]::OpenRead($Package)
    try { $names = @($zip.Entries | ForEach-Object { [Uri]::UnescapeDataString($_.FullName) }) } finally { $zip.Dispose() }
    if ($names -like '*.runtimeconfig.json') { throw "$(Split-Path $Package -Leaf) expects an installed .NET runtime (it holds a runtimeconfig.json)." }
    if ($names -notcontains $exeName) { throw "$(Split-Path $Package -Leaf) has no $exeName." }
    "$(Split-Path $Package -Leaf): native, $($names.Count) files, $([math]::Round((Get-Item $Package).Length / 1MB, 1)) MB"
    return
}

# A loose layout of the package: everything but the signature files, which a registered folder can't have.
$layout = Join-Path ([IO.Path]::GetTempPath()) "companion-smoke-$([guid]::NewGuid().ToString('N').Substring(0, 8))"
$zip = [IO.Compression.ZipFile]::OpenRead($Package)
try {
    foreach ($entry in $zip.Entries) {
        $name = [Uri]::UnescapeDataString($entry.FullName)
        if ($name -in 'AppxBlockMap.xml', 'AppxSignature.p7x', '[Content_Types].xml' -or $name.StartsWith('AppxMetadata/')) { continue }
        $target = Join-Path $layout $name
        New-Item -ItemType Directory -Force (Split-Path $target) | Out-Null
        [IO.Compression.ZipFileExtensions]::ExtractToFile($entry, $target, $true)
    }
} finally { $zip.Dispose() }
$manifestPath = Join-Path $layout 'AppxManifest.xml'
if ($SideBySide) {
    # Two packages can't share a name or the toast activator's COM class, so the copy gets its own of both.
    $text = [IO.File]::ReadAllText($manifestPath)
    $original = ([xml]$text).Package.Identity.Name
    $text = $text.Replace("Name=`"$original`"", "Name=`"$original.SmokeTest`"")
    $clsid = [regex]::Match($text, 'ToastActivatorCLSID="([0-9A-Fa-f-]{36})"').Groups[1].Value
    if ($clsid) { $text = $text.Replace($clsid, [guid]::NewGuid().ToString().ToUpperInvariant()) }
    [IO.File]::WriteAllText($manifestPath, $text, (New-Object Text.UTF8Encoding($false)))
}
$manifest = [xml](Get-Content $manifestPath)
$identity = $manifest.Package.Identity.Name
$arch = $manifest.Package.Identity.ProcessorArchitecture
$exeName = ($manifest.Package.Applications.Application | Select-Object -First 1).Executable
$processName = [IO.Path]::GetFileNameWithoutExtension($exeName)
if ($SideBySide -and $identity -ne "$original.SmokeTest") { throw "The package identity wasn't renamed ($identity)." }

$existing = Get-AppxPackage -Name $identity
if ($existing | Where-Object { -not $_.IsDevelopmentMode }) {
    throw "$identity is installed from the Store here; uninstall it first (this would delete its data), or pass -SideBySide."
}
if ($existing) { $existing | Remove-AppxPackage }

# The Windows App Runtime the package depends on, from the publish output next to it.
$dependencies = Get-ChildItem (Split-Path $Package) -Recurse -Filter *.msix -ErrorAction SilentlyContinue |
    Where-Object { $_.Directory.Name -eq $arch -and $_.Directory.Parent.Name -eq 'Dependencies' }
foreach ($dependency in $dependencies) {
    $needed = (Read-Manifest $dependency.FullName).Package.Identity
    $have = Get-AppxPackage -Name $needed.Name | Where-Object {
        "$($_.Architecture)" -eq $needed.ProcessorArchitecture -and [version]$_.Version -ge [version]$needed.Version }
    if (-not $have) { Add-AppxPackage $dependency.FullName }
}

$failure = $null
$started = Get-Date
$exe = $null
try {
    Add-AppxPackage -Register $manifestPath
    $installed = Get-AppxPackage -Name $identity
    $exe = Join-Path $installed.InstallLocation $exeName
    Start-Process "shell:AppsFolder\$($installed.PackageFamilyName)!App"
    $deadline = (Get-Date).AddSeconds(15)
    while (-not ($process = Get-Process $processName -ErrorAction SilentlyContinue | Where-Object { $_.Path -eq $exe }) -and
        (Get-Date) -lt $deadline) { Start-Sleep -Milliseconds 250 }
    if (-not $process) { throw 'The app never started.' }
    $process = $process | Select-Object -First 1
    Start-Sleep -Seconds $Seconds
    $process.Refresh()
    if ($process.HasExited) { $failure = "The app quit within $Seconds seconds." }
    elseif ($process.MainWindowHandle -eq 0) { $failure = "The app is running but shows no window after $Seconds seconds." }
    else { "Running after $Seconds seconds, window: $($process.MainWindowTitle)" }

    # Windows logs a crash in the Application log even when the process is already gone.
    $crashes = Get-WinEvent -FilterHashtable @{ LogName = 'Application'; ProviderName = 'Application Error'; StartTime = $started } -ErrorAction SilentlyContinue |
        Where-Object { $_.Message -match [regex]::Escape($exeName) }
    if ($crashes) {
        $failure = (@($failure) + "Windows logged a crash:`n$($crashes[0].Message)" | Where-Object { $_ }) -join "`n"
    }
} finally {
    if (-not $Keep -or $failure) {
        Get-Process $processName -ErrorAction SilentlyContinue | Where-Object { $_.Path -like "$layout*" -or ($exe -and $_.Path -eq $exe) } | Stop-Process -Force
        Start-Sleep -Seconds 1
        Get-AppxPackage -Name $identity | Where-Object { $_.IsDevelopmentMode } | Remove-AppxPackage
        Remove-Item -Recurse -Force $layout -ErrorAction SilentlyContinue
    }
    else {
        "Kept installed from $layout (Settings › Apps to remove it, or run again without -Keep)."
    }
}
if ($failure) { throw $failure }
"$(Split-Path $Package -Leaf): passed."
