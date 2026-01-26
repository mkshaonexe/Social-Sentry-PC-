<#
.SYNOPSIS
    Automated Build Script for Social Sentry PC Installer (WiX)
.DESCRIPTION
    1. Installs WiX Toolset globally if not present.
    2. Publishes the WPF App and Watchdog to a temporary directory.
    3. Harvests files into WiX Components.
    4. Builds the MSI Installer.
#>

$ErrorActionPreference = "Stop"

# --- Configuration ---
$projectRoot = $PSScriptRoot
$sourceDir = Join-Path $projectRoot "Installer\source"
$installerDir = Join-Path $projectRoot "Installer"
$mainProject = Join-Path $projectRoot "Social Sentry\Social Sentry.csproj"
$watchdogProject = Join-Path $projectRoot "SocialSentry.Watchdog\SocialSentry.Watchdog.csproj"
$msiOutput = "SocialSentry.msi"

Write-Host "Starting Build Process for Social Sentry Installer..." -ForegroundColor Cyan

# 1. Check & Install WiX Toolset
Write-Host "Checking WiX Toolset..." -ForegroundColor Yellow
try {
    wix --version | Out-Null
    Write-Host "   WiX is installed." -ForegroundColor Green
}
catch {
    Write-Host "   WiX not found. Installing globally..." -ForegroundColor Yellow
    dotnet tool install --global wix
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to install WiX. Please run 'dotnet tool install --global wix' manually."
    }
}

# Install WiX UI Extension (Required for UI)
Write-Host "Installing WiX UI Extension..." -ForegroundColor Yellow
wix extension add -g WixToolset.UI.wixext

# 2. Clean & Prepare Source Directory
Write-Host "Cleaning build directories..." -ForegroundColor Yellow
if (Test-Path $sourceDir) {
    Remove-Item $sourceDir -Recurse -Force
}
New-Item -Path $sourceDir -ItemType Directory -Force | Out-Null

# 3. Publish Applications
Write-Host "Publishing Main Application..." -ForegroundColor Cyan
dotnet publish $mainProject -c Release -r win-x64 --self-contained true -o $sourceDir /p:DebugType=None /p:DebugSymbols=false
if ($LASTEXITCODE -ne 0) { Write-Error "Main App Publish Failed" }

Write-Host "Publishing Watchdog..." -ForegroundColor Cyan
dotnet publish $watchdogProject -c Release -r win-x64 --self-contained true -o $sourceDir /p:DebugType=None /p:DebugSymbols=false
if ($LASTEXITCODE -ne 0) { Write-Error "Watchdog Publish Failed" }

# 4. Copy Extension Files (If needed, assume they are in specific folder)
$extSrc = Join-Path $projectRoot "Social Sentry\extension"
$extDest = Join-Path $sourceDir "extension"
if (Test-Path $extSrc) {
    Write-Host "Copying Browser Extension..." -ForegroundColor Cyan
    Copy-Item $extSrc -Destination $extDest -Recurse -Force
}

# 5. Generate WiX Components (Auto-Harvesting)
Write-Host "Harvesting files..." -ForegroundColor Yellow
# We run the existing script, but make sure to pass correct paths if needed
# The existing script uses relative paths from its own location
Push-Location $projectRoot
& "./Installer/GenerateComponents.ps1"
Pop-Location

# 6. Build MSI
Write-Host "Building MSI..." -ForegroundColor Cyan
Set-Location $installerDir

# wix build command
# -ext WixToolset.UI.wixext : Include UI extension
# -o : Output file
wix build -ext WixToolset.UI.wixext Package.wxs Components.wxs -o $msiOutput

if ($LASTEXITCODE -eq 0) {
    Write-Host "SUCCESS! Installer created at:" -ForegroundColor Green
    Write-Host "   $(Join-Path $installerDir $msiOutput)" -ForegroundColor White
    
    # Open folder
    Invoke-Item $installerDir
}
else {
    Write-Error "Build Failed."
}
