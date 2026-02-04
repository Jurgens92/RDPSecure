#Requires -Version 5.1
<#
.SYNOPSIS
    Builds and packages RDPSecure into a single-file Inno Setup installer.

.DESCRIPTION
    1. Runs "dotnet publish" for win-x64 in framework-dependent mode.
    2. Invokes the Inno Setup Compiler (iscc.exe) on RDPSecure.iss.
    3. Drops the finished installer into installer\Output\

.PREREQUISITES
    - .NET SDK 8.0 or later  (https://dotnet.microsoft.com/download)
    - Inno Setup 6.x         (https://jrsoftware.org/isdownload.php)
      Install Inno Setup to its default path (C:\Program Files (x86)\Inno Setup 6)
      OR set the environment variable INNO_SETUP_PATH to your install directory.

.USAGE
    powershell -ExecutionPolicy Bypass -File .\build.ps1
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# ---------------------------------------------------------------------------
# Paths
# ---------------------------------------------------------------------------
$ScriptDir   = $PSScriptRoot                                          # installer/
$RepoRoot    = (Resolve-Path "$ScriptDir\..").Path                   # repo root
$ProjectDir  = Join-Path $RepoRoot 'RDPSecure'                       # RDPSecure/
$PublishDir  = Join-Path $ScriptDir 'publish'                        # installer/publish/
$OutputDir   = Join-Path $ScriptDir 'Output'                         # installer/Output/
$IssFile     = Join-Path $ScriptDir 'RDPSecure.iss'

# ---------------------------------------------------------------------------
# Locate iscc.exe
# ---------------------------------------------------------------------------
function Resolve-Iscc {
    # 1) Environment variable
    if ($env:INNO_SETUP_PATH) {
        $candidate = Join-Path $env:INNO_SETUP_PATH 'iscc.exe'
        if (Test-Path $candidate) { return $candidate }
    }

    # 2) Default install location (32-bit Program Files)
    $defaultPath = 'C:\Program Files (x86)\Inno Setup 6\iscc.exe'
    if (Test-Path $defaultPath) { return $defaultPath }

    # 3) Search PATH
    $fromPath = Get-Command 'iscc.exe' -ErrorAction SilentlyContinue
    if ($fromPath) { return $fromPath.Source }

    return $null
}

# ---------------------------------------------------------------------------
# Main
# ---------------------------------------------------------------------------
Write-Host '=== RDPSecure Installer Build ===' -ForegroundColor Cyan

# -- Step 1: publish ----------------------------------------------------------
Write-Host '[1/2] Running dotnet publish (win-x64, framework-dependent)...' -ForegroundColor Yellow

# Clean previous publish output so nothing stale is bundled
if (Test-Path $PublishDir) { Remove-Item $PublishDir -Recurse -Force }

dotnet publish `
    "$ProjectDir\RDPSecure.csproj" `
    -c   Release `
    -r   win-x64 `
    --no-self-contained `
    -p:PublishSingleFile=false `
    -o   $PublishDir

if ($LASTEXITCODE -ne 0) {
    throw 'dotnet publish failed. Check the output above for details.'
}

Write-Host '    Publish succeeded.' -ForegroundColor Green

# -- Step 2: run Inno Setup ---------------------------------------------------
Write-Host '[2/2] Running Inno Setup Compiler...' -ForegroundColor Yellow

$Iscc = Resolve-Iscc
if (-not $Iscc) {
    Write-Host @"

    ERROR: Inno Setup Compiler (iscc.exe) not found.

    Download and install Inno Setup 6 from:
        https://jrsoftware.org/isdownload.php

    Then either:
      a) Install to the default path, OR
      b) Set the environment variable INNO_SETUP_PATH to your install folder
         and re-run this script.
"@ -ForegroundColor Red
    exit 1
}

Write-Host "    Using iscc.exe at: $Iscc" -ForegroundColor DarkGray

& $Iscc /O"$OutputDir" $IssFile

if ($LASTEXITCODE -ne 0) {
    throw 'Inno Setup compilation failed. Check the output above for details.'
}

# -- Done ---------------------------------------------------------------------
$Installer = Get-ChildItem -Path $OutputDir -Filter '*.exe' | Select-Object -Last 1
Write-Host ''
Write-Host '=== Build Complete ===' -ForegroundColor Cyan
Write-Host "    Installer: $($Installer.FullName)" -ForegroundColor Green
Write-Host "    Size:      $([math]::Round($Installer.Length / 1MB, 2)) MB" -ForegroundColor Green
