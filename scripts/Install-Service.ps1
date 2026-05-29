#Requires -RunAsAdministrator
#Requires -Version 5.1
<#
.SYNOPSIS
    Installs Kros Extension Api and Web as Windows Services.

.DESCRIPTION
    Copies the published binaries to $InstallDir, patches the connection string
    in appsettings.json, then registers both services and starts them.

    Run this script on the target machine with the dist\ folder next to it
    (i.e. the whole dist\ tree must be present alongside the scripts\ folder),
    OR use -SourceDir to point at the dist folder explicitly.

.PARAMETER InstallDir
    Directory where the services will be installed. Default: C:\KrosExtension

.PARAMETER SqlServer
    SQL Server instance name. Default: .\SQLEXPRESS
    Examples: localhost, .\MSSQLSERVER, MYSERVER\SQLEXPRESS

.PARAMETER Database
    KrosExtension configuration database name. Default: KrosExtension
    The database will be created automatically on first run (EF migrations run at startup).

.PARAMETER SourceDir
    Path to the dist\ folder produced by Build-Dist.ps1.
    Default: ..\dist relative to this script.

.EXAMPLE
    .\Install-Service.ps1
    .\Install-Service.ps1 -SqlServer "MYSERVER" -InstallDir "D:\Services\KrosExtension"
#>

param(
    [string]$InstallDir = 'C:\KrosExtension',
    [string]$SqlServer  = '.\SQLEXPRESS',
    [string]$Database   = 'KrosExtension',
    [string]$SourceDir  = ''
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if (-not $SourceDir) {
    $SourceDir = Resolve-Path "$PSScriptRoot\..\dist" -ErrorAction SilentlyContinue
    if (-not $SourceDir) {
        $SourceDir = Join-Path $PSScriptRoot '..\dist'
    }
}

$apiName    = 'KrosExtensionApi'
$webName    = 'KrosExtensionWeb'
$apiDisplay = 'Kros Extension Backend'
$webDisplay = 'Kros Extension Web'
$apiSrc     = Join-Path $SourceDir 'api'
$webSrc     = Join-Path $SourceDir 'web'
$apiDest    = Join-Path $InstallDir 'api'
$webDest    = Join-Path $InstallDir 'web'
$apiExe     = Join-Path $apiDest 'KrosExtension.Api.exe'
$webExe     = Join-Path $webDest 'KrosExtension.Web.exe'

Write-Host "=== Kros Extension — Install Services ===" -ForegroundColor Cyan
Write-Host "Install dir : $InstallDir"
Write-Host "SQL Server  : $SqlServer"
Write-Host "Database    : $Database"
Write-Host ""

# ── Validate source ──────────────────────────────────────────────────────────
if (-not (Test-Path $apiSrc)) {
    throw "API source not found: $apiSrc`nRun Build-Dist.ps1 first."
}
if (-not (Test-Path $webSrc)) {
    throw "Web source not found: $webSrc`nRun Build-Dist.ps1 first."
}

# ── Stop and remove existing services ────────────────────────────────────────
foreach ($svc in @($webName, $apiName)) {
    $existing = Get-Service -Name $svc -ErrorAction SilentlyContinue
    if ($existing) {
        Write-Host "Stopping existing service: $svc" -ForegroundColor Yellow
        if ($existing.Status -eq 'Running') {
            Stop-Service -Name $svc -Force
        }
        sc.exe delete $svc | Out-Null
        Start-Sleep -Seconds 2
    }
}

# ── Copy files ────────────────────────────────────────────────────────────────
Write-Host "[1/4] Copying files to $InstallDir..." -ForegroundColor Yellow
if (-not (Test-Path $InstallDir)) { New-Item -ItemType Directory -Path $InstallDir -Force | Out-Null }
Copy-Item -Path $apiSrc -Destination $apiDest -Recurse -Force
Copy-Item -Path $webSrc -Destination $webDest -Recurse -Force

# ── Patch API appsettings.json ────────────────────────────────────────────────
Write-Host "[2/4] Configuring connection string..." -ForegroundColor Yellow
$apiSettings = Join-Path $apiDest 'appsettings.json'
$connString  = "Server=$SqlServer;Database=$Database;Trusted_Connection=True;TrustServerCertificate=True"

$json = Get-Content $apiSettings -Raw | ConvertFrom-Json
$json.ConnectionStrings.DefaultConnection = $connString
$json | ConvertTo-Json -Depth 10 | Set-Content $apiSettings -Encoding utf8

Write-Host "    Connection string set: $connString"

# ── Register services ─────────────────────────────────────────────────────────
Write-Host "[3/4] Registering Windows Services..." -ForegroundColor Yellow

New-Service -Name $apiName `
            -BinaryPathName "`"$apiExe`"" `
            -DisplayName $apiDisplay `
            -Description 'Kros Extension backend API — spravuje konfiguráciu a číta dáta z Olymp/Omega databáz.' `
            -StartupType Automatic | Out-Null

New-Service -Name $webName `
            -BinaryPathName "`"$webExe`"" `
            -DisplayName $webDisplay `
            -Description 'Kros Extension web rozhranie — proxy na backend a statický frontend.' `
            -StartupType Automatic | Out-Null

# Ensure Web starts after Api (best-effort dependency)
sc.exe config $webName depend= $apiName | Out-Null

Write-Host "    $apiName registered"
Write-Host "    $webName registered (depends on $apiName)"

# ── Start services ────────────────────────────────────────────────────────────
Write-Host "[4/4] Starting services..." -ForegroundColor Yellow
Start-Service -Name $apiName
Start-Sleep -Seconds 3
Start-Service -Name $webName

$apiStatus = (Get-Service $apiName).Status
$webStatus = (Get-Service $webName).Status

Write-Host ""
Write-Host "=== Installation complete ===" -ForegroundColor Green
Write-Host "$apiName : $apiStatus"
Write-Host "$webName : $webStatus"
Write-Host ""
Write-Host "Web UI: http://localhost:5284" -ForegroundColor Cyan
Write-Host ""
if ($apiStatus -ne 'Running' -or $webStatus -ne 'Running') {
    Write-Warning "One or more services did not start. Check Event Viewer > Windows Logs > Application."
}
