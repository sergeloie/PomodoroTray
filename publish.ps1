# Builds both distribution variants of PomodoroTray into dist/:
#   PomodoroTray-self-contained.exe       — full .NET inside, no install needed (~150 MB)
#   PomodoroTray-framework-dependent.exe  — tiny, requires .NET 8 Desktop Runtime (~0.2 MB)
#
# Usage:  .\publish.ps1
# Output: dist\

$ErrorActionPreference = 'Stop'

$root = $PSScriptRoot
$dist = Join-Path $root 'dist'

if (Test-Path $dist) {
    Remove-Item $dist -Recurse -Force
}
New-Item -ItemType Directory -Path $dist | Out-Null

function Publish-Variant {
    param(
        [string]$Name,
        [switch]$SelfContained
    )

    $out = Join-Path $dist $Name
    New-Item -ItemType Directory -Path $out | Out-Null

    # Windows PowerShell 5.1: no ternary operator — branch explicitly.
    $scFlag = 'false'
    if ($SelfContained) { $scFlag = 'true' }

    $args = @(
        'publish', (Join-Path $root 'PomodoroTray.csproj'),
        '-c', 'Release',
        '-r', 'win-x64',
        '--self-contained', $scFlag,
        '-p:PublishSingleFile=true',
        '-o', $out
    )
    if ($SelfContained) {
        # Bundle native libraries inside the exe (extracted to %TEMP% at first run).
        $args += '-p:IncludeNativeLibrariesForSelfExtract=true'
    }

    Write-Host "==> Publishing $Name (self-contained=$SelfContained)..."
    & dotnet @args
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed for $Name (exit $LASTEXITCODE)"
    }

    $exe = Join-Path $out 'PomodoroTray.exe'
    if (-not (Test-Path $exe)) {
        throw "Expected exe not found: $exe"
    }

    $finalName = "PomodoroTray-$Name.exe"
    Move-Item $exe (Join-Path $dist $finalName) -Force

    # Drop everything else (pdb etc.) — only the exe goes to dist root.
    Remove-Item $out -Recurse -Force

    $size = [math]::Round((Get-Item (Join-Path $dist $finalName)).Length / 1MB, 1)
    Write-Host "    OK: dist\$finalName ($size MB)"
}

Publish-Variant -Name 'self-contained' -SelfContained
Publish-Variant -Name 'framework-dependent'

Write-Host ''
Write-Host "Done. Distributable files:"
Get-ChildItem $dist -File | ForEach-Object {
    $mb = [math]::Round($_.Length / 1MB, 2)
    Write-Host ("  {0}  ({1} MB)" -f $_.Name, $mb)
}
