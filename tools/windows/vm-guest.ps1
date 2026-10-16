<#
The VM's half of vm.sh (deskling/tools/windows/vm.py): every command vm.sh runs in the Windows VM dot-sources this file
from the Mac's shared folder, then calls these functions. Windows PowerShell 5.1 or PowerShell 7. Not for running by
hand.

Sync-Tree unpacks the Mac's tar (when there is one) into the copy, deletes the copy's files that aren't on the Mac's
list (files.tsv: path, size, mtime in whole seconds) outside build output, and writes need.txt: the listed files the
copy lacks or holds in another version, for the Mac to send.
#>

# Build output the copy keeps between syncs, so builds stay incremental.
$KeepFolders = @('bin', 'obj', '.vs', 'AppPackages', 'TestResults')
$global:VmFailures = @()

function Resolve-GuestFolder([string]$Path) {
    $full = [Environment]::ExpandEnvironmentVariables($Path)
    New-Item -ItemType Directory -Force -Path $full | Out-Null
    return (Resolve-Path -LiteralPath $full).ProviderPath.TrimEnd('\', '/')
}

function Reset-GuestFolder([string]$Path) {
    $full = [Environment]::ExpandEnvironmentVariables($Path)
    if (Test-Path -LiteralPath $full) { Remove-Item -LiteralPath $full -Recurse -Force }
    return Resolve-GuestFolder $full
}

function Sync-Tree([string]$Root, [string]$Stage) {
    $archive = Join-Path $Stage 'sync.tar'
    if (Test-Path -LiteralPath $archive) {
        # A local copy first: bsdtar reads a share path slowly, and the tar is gone from the Mac once this returns.
        $local = Join-Path ([IO.Path]::GetTempPath()) "deskling-vm-$([guid]::NewGuid().ToString('N')).tar"
        Copy-Item -LiteralPath $archive -Destination $local
        try {
            & tar -xf $local -C $Root
            if ($LASTEXITCODE -ne 0) { throw "tar couldn't unpack the files from the Mac (exit code $LASTEXITCODE)" }
        }
        finally { Remove-Item -LiteralPath $local -Force -ErrorAction SilentlyContinue }
    }

    $epoch = New-Object DateTime 1970, 1, 1, 0, 0, 0, ([DateTimeKind]::Utc)
    $want = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
    $need = New-Object 'System.Collections.Generic.List[string]'
    foreach ($line in [IO.File]::ReadAllLines((Join-Path $Stage 'files.tsv'), [Text.Encoding]::UTF8)) {
        if (-not $line) { continue }
        $path, $size, $mtime = $line.Split("`t")
        [void]$want.Add($path)
        $file = New-Object IO.FileInfo ([IO.Path]::Combine($Root, $path))
        if (-not $file.Exists -or $file.Length -ne [long]$size -or
            [long][Math]::Floor(($file.LastWriteTimeUtc - $epoch).TotalSeconds) -ne [long]$mtime) {
            $need.Add($path)
        }
    }

    $removed = 0
    $folders = New-Object 'System.Collections.Generic.Stack[string]'
    $folders.Push($Root)
    while ($folders.Count -gt 0) {
        $folder = $folders.Pop()
        foreach ($sub in [IO.Directory]::GetDirectories($folder)) {
            if ($KeepFolders -notcontains [IO.Path]::GetFileName($sub)) { $folders.Push($sub) }
        }
        foreach ($file in [IO.Directory]::GetFiles($folder)) {
            $relative = $file.Substring($Root.Length).TrimStart('\', '/').Replace('\', '/')
            if (-not $want.Contains($relative)) {
                [IO.File]::SetAttributes($file, [IO.FileAttributes]::Normal)
                [IO.File]::Delete($file)
                $removed++
            }
        }
        if ($folder -ne $Root -and [IO.Directory]::GetFileSystemEntries($folder).Count -eq 0) { [IO.Directory]::Delete($folder) }
    }
    [IO.File]::WriteAllLines((Join-Path $Stage 'need.txt'), [string[]]$need.ToArray(), (New-Object Text.UTF8Encoding $false))
    [Console]::Out.WriteLine("synced`t$Root`t$removed")
}

# Runs one step, streaming its output, and prints ok or FAIL with the time it took; Exit-Steps exits 1 if any failed.
function Invoke-Step([string]$Label, [scriptblock]$Block) {
    [Console]::Out.WriteLine("== $Label")
    $watch = [Diagnostics.Stopwatch]::StartNew()
    $global:LASTEXITCODE = 0
    try {
        & $Block | ForEach-Object { [Console]::Out.WriteLine("$_") }
        $code = $global:LASTEXITCODE
    }
    catch {
        [Console]::Error.WriteLine($_.Exception.Message)
        $code = 1
    }
    $seconds = [int]$watch.Elapsed.TotalSeconds
    if ($code -eq 0) { [Console]::Out.WriteLine("ok    $Label ($seconds s)") }
    else {
        [Console]::Out.WriteLine("FAIL  $Label (exit code $code, $seconds s)")
        $global:VmFailures += $Label
    }
}

function Exit-Steps {
    if ($global:VmFailures.Count) { exit 1 }
    exit 0
}

function Get-GuestPlatform {
    if ([Runtime.InteropServices.RuntimeInformation]::OSArchitecture -eq 'Arm64') { return 'ARM64' }
    return 'x64'
}

# Native AOT links with the Visual Studio tools it finds through vswhere, which a plain shell doesn't have on its path.
function Add-VsWhere {
    $installer = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer'
    if (-not (Get-Command vswhere.exe -ErrorAction SilentlyContinue) -and (Test-Path (Join-Path $installer 'vswhere.exe'))) {
        $env:PATH = "$installer;$env:PATH"
    }
}

# The app's package in a publish's AppxPackageDir, not the Windows App Runtime packages beside it.
function Find-Package([string]$Folder) {
    $msix = Get-ChildItem -LiteralPath $Folder -Recurse -Filter *.msix |
        Where-Object { $_.FullName -notmatch '[\\/]Dependencies[\\/]' } | Select-Object -First 1
    if (-not $msix) { throw "The publish left no .msix in $Folder." }
    return $msix
}

# The shots and the run's problems.txt, into the Mac's folder through the share.
function Copy-Shots([string]$From, [string]$To, [string]$Label = '') {
    $files = @(Get-ChildItem -LiteralPath $From -File -ErrorAction SilentlyContinue |
            Where-Object { $_.Extension -eq '.png' -or $_.Name -eq 'problems.txt' })
    foreach ($file in $files) { Copy-Item -LiteralPath $file.FullName -Destination $To -Force }
    $count = @($files | Where-Object { $_.Extension -eq '.png' }).Count
    [Console]::Out.WriteLine("$count screenshots$(if ($Label) { " ($Label)" }) copied to the Mac")
}

# Copy-Shots for each folder in $From (check-layout's one per language), into a folder of the same name in $To.
function Copy-ShotFolders([string]$From, [string]$To) {
    foreach ($folder in @(Get-ChildItem -LiteralPath $From -Directory -ErrorAction SilentlyContinue)) {
        $target = Join-Path $To $folder.Name
        New-Item -ItemType Directory -Force $target | Out-Null
        Copy-Shots $folder.FullName $target $folder.Name
    }
}
