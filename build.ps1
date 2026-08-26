<#
.SYNOPSIS
    Build and deployment script for SPT Optimizer plugin.
.DESCRIPTION
    Compiles the project in Release (or Debug) configuration, 
    deploys to the SPT installation folder, and creates a distributable zip in /dist.
.PARAMETER Configuration
    Build configuration: 'Release' (default) or 'Debug'.
.PARAMETER Clean
    Performs a clean build (removes bin and obj folders first).
.PARAMETER NoDeploy
    Skips copying to the SPT game plugins directory.
#>

[CmdletBinding()]
param (
    [ValidateSet("Release", "Debug")]
    [string]$Configuration = "Release",

    [switch]$Clean,
    [switch]$NoDeploy
)

$ErrorActionPreference = "Stop"
$ProjectDir = $PSScriptRoot
$CsprojFile = Join-Path $ProjectDir "SPTOptimizer.csproj"
$DistDir = Join-Path $ProjectDir "dist"
$DefaultSptPath = "C:\Battlestate Games\SPP-Tarkov"

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "       SPT Optimizer Build Script         " -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "Configuration : $Configuration" -ForegroundColor Gray
Write-Host "Project       : $CsprojFile" -ForegroundColor Gray

# 1. Clean if requested
if ($Clean) {
    Write-Host "`n[1/4] Cleaning project artifacts..." -ForegroundColor Yellow
    dotnet clean $CsprojFile -c $Configuration
    
    $binDir = Join-Path $ProjectDir "bin"
    $objDir = Join-Path $ProjectDir "obj"
    if (Test-Path $binDir) { Remove-Item -Path $binDir -Recurse -Force }
    if (Test-Path $objDir) { Remove-Item -Path $objDir -Recurse -Force }
    Write-Host "Clean completed." -ForegroundColor Green
} else {
    Write-Host "`n[1/4] Skipping clean (use -Clean to force)." -ForegroundColor DarkGray
}

# 2. Build
Write-Host "`n[2/4] Building SPT Optimizer ($Configuration)..." -ForegroundColor Yellow
if ($Clean) {
    dotnet build $CsprojFile -c $Configuration --no-incremental
} else {
    dotnet build $CsprojFile -c $Configuration
}

if ($LASTEXITCODE -ne 0) {
    Write-Host "`n[ERROR] Build failed! Check output above." -ForegroundColor Red
    exit 1
}
Write-Host "Build succeeded!" -ForegroundColor Green

# Output DLL path
$dllPath = Join-Path $ProjectDir "bin\$Configuration\netstandard2.1\SPTOptimizer.dll"
if (-not (Test-Path $dllPath)) {
    Write-Host "[ERROR] Compiled DLL not found at: $dllPath" -ForegroundColor Red
    exit 1
}

$fileVersion = (Get-Item $dllPath).VersionInfo.FileVersion
if ([string]::IsNullOrWhiteSpace($fileVersion)) {
    $fileVersion = "1.1.0"
}
Write-Host "Compiled binary: SPTOptimizer.dll (v$fileVersion)" -ForegroundColor Gray

# 3. Direct Deploy to SPT folder
if (-not $NoDeploy) {
    Write-Host "`n[3/4] Deploying to SPT game folder..." -ForegroundColor Yellow
    $pluginTargetDir = Join-Path $DefaultSptPath "BepInEx\plugins\SPTOptimizer"

    if (Test-Path (Join-Path $DefaultSptPath "BepInEx")) {
        if (-not (Test-Path $pluginTargetDir)) {
            New-Item -ItemType Directory -Path $pluginTargetDir -Force | Out-Null
        }
        Copy-Item -Path $dllPath -Destination (Join-Path $pluginTargetDir "SPTOptimizer.dll") -Force
        Write-Host "Successfully deployed to: $pluginTargetDir\SPTOptimizer.dll" -ForegroundColor Green
    } else {
        Write-Host "[WARN] SPT install path not found at '$DefaultSptPath'. Skipped copy." -ForegroundColor Yellow
    }
} else {
    Write-Host "`n[3/4] Skipping deployment (-NoDeploy specified)." -ForegroundColor DarkGray
}

# 4. Packaging / Distribution Zip
Write-Host "`n[4/4] Creating distribution package in /dist..." -ForegroundColor Yellow
if (-not (Test-Path $DistDir)) {
    New-Item -ItemType Directory -Path $DistDir -Force | Out-Null
}

$stagingRoot = Join-Path $DistDir "staging"
$packageStaging = Join-Path $stagingRoot "BepInEx\plugins\SPTOptimizer"
if (Test-Path $stagingRoot) {
    Remove-Item -Path $stagingRoot -Recurse -Force
}
New-Item -ItemType Directory -Path $packageStaging -Force | Out-Null

Copy-Item -Path $dllPath -Destination $packageStaging -Force

$zipFileName = "SPTOptimizer-v$($fileVersion).zip"
$zipFilePath = Join-Path $DistDir $zipFileName

if (Test-Path $zipFilePath) {
    Remove-Item -Path $zipFilePath -Force
}

Compress-Archive -Path "$stagingRoot\*" -DestinationPath $zipFilePath -Force
Remove-Item -Path $stagingRoot -Recurse -Force

Write-Host "Package created: dist\$zipFileName" -ForegroundColor Green

Write-Host "`n==========================================" -ForegroundColor Cyan
Write-Host "              BUILD COMPLETE!             " -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
