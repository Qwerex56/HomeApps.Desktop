# Builds HomeApps.Desktop for Windows x64 (self-contained) into dist\ as a zip.
# Usage: pwsh scripts/package-windows.ps1 -Version 1.2.3
param([Parameter(Mandatory = $true)][string]$Version)
$ErrorActionPreference = 'Stop'

$Version = $Version.TrimStart('v')
$Rid = 'win-x64'
$Root = Split-Path -Parent $PSScriptRoot
$Build = Join-Path $Root "dist/build-$Rid"
$Dir = Join-Path $Build 'HomeApps.Desktop'

if (Test-Path $Build) { Remove-Item -Recurse -Force $Build }
New-Item -ItemType Directory -Force -Path $Dir | Out-Null

dotnet publish (Join-Path $Root 'src/HomeApps.Desktop/HomeApps.Desktop.csproj') -c Release -r $Rid --self-contained true "-p:Version=$Version" -o $Dir
if ($LASTEXITCODE -ne 0) { throw 'dotnet publish failed' }

$Zip = Join-Path $Root "dist/HomeApps.Desktop-$Version-$Rid.zip"
if (Test-Path $Zip) { Remove-Item -Force $Zip }
Compress-Archive -Path $Dir -DestinationPath $Zip
Write-Output $Zip
