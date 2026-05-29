#Requires -RunAsAdministrator
#Requires -Version 5.1
<#
.SYNOPSIS
    Stops and removes Kros Extension Windows Services.

.PARAMETER RemoveFiles
    If specified, also deletes the installation directory.

.PARAMETER InstallDir
    Installation directory to remove (only used with -RemoveFiles). Default: C:\KrosExtension

.EXAMPLE
    .\Uninstall-Service.ps1
    .\Uninstall-Service.ps1 -RemoveFiles
    .\Uninstall-Service.ps1 -RemoveFiles -InstallDir "D:\Services\KrosExtension"
#>

param(
    [switch]$RemoveFiles,
    [string]$InstallDir = 'C:\KrosExtension'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$services = @('KrosExtensionWeb', 'KrosExtensionApi')

Write-Host "=== Kros Extension — Uninstall Services ===" -ForegroundColor Cyan

foreach ($svc in $services) {
    $existing = Get-Service -Name $svc -ErrorAction SilentlyContinue
    if ($existing) {
        if ($existing.Status -eq 'Running') {
            Write-Host "Stopping $svc..." -ForegroundColor Yellow
            Stop-Service -Name $svc -Force
        }
        Write-Host "Removing $svc..." -ForegroundColor Yellow
        sc.exe delete $svc | Out-Null
    } else {
        Write-Host "$svc not found, skipping." -ForegroundColor Gray
    }
}

if ($RemoveFiles) {
    if (Test-Path $InstallDir) {
        Write-Host "Removing files: $InstallDir..." -ForegroundColor Yellow
        Remove-Item -Path $InstallDir -Recurse -Force
        Write-Host "Files removed."
    } else {
        Write-Host "Install directory not found: $InstallDir" -ForegroundColor Gray
    }
}

Write-Host ""
Write-Host "=== Uninstall complete ===" -ForegroundColor Green
