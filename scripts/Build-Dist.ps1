#Requires -Version 5.1
<#
.SYNOPSIS
    Builds the Kros Extension distribution package for client deployment.

.DESCRIPTION
    1. Runs npm install + npm run build for the frontend.
    2. Publishes both Api and Web as self-contained win-x64 executables.
    3. Output lands in dist\api\ and dist\web\ under the repo root.

.EXAMPLE
    .\Build-Dist.ps1
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot  = Resolve-Path "$PSScriptRoot\.."
$distApi   = Join-Path $repoRoot 'dist\api'
$distWeb   = Join-Path $repoRoot 'dist\web'
$clientApp = Join-Path $repoRoot 'src\KrosExtension.Web\ClientApp'
$apiProj   = Join-Path $repoRoot 'src\KrosExtension.Api\KrosExtension.Api.csproj'
$webProj   = Join-Path $repoRoot 'src\KrosExtension.Web\KrosExtension.Web.csproj'

Write-Host "=== Kros Extension — Build Distribution ===" -ForegroundColor Cyan

# 1. Frontend
Write-Host "`n[1/3] Frontend build..." -ForegroundColor Yellow
Push-Location $clientApp
try {
    npm install
    if ($LASTEXITCODE -ne 0) { throw "npm install failed" }
    npm run build
    if ($LASTEXITCODE -ne 0) { throw "npm run build failed" }
} finally {
    Pop-Location
}

# 2. Publish API
Write-Host "`n[2/3] Publishing API..." -ForegroundColor Yellow
if (Test-Path $distApi) { Remove-Item $distApi -Recurse -Force }
dotnet publish $apiProj -c Release -r win-x64 --self-contained -o $distApi
if ($LASTEXITCODE -ne 0) { throw "dotnet publish API failed" }

# 3. Publish Web
Write-Host "`n[3/3] Publishing Web..." -ForegroundColor Yellow
if (Test-Path $distWeb) { Remove-Item $distWeb -Recurse -Force }
dotnet publish $webProj -c Release -r win-x64 --self-contained -o $distWeb
if ($LASTEXITCODE -ne 0) { throw "dotnet publish Web failed" }

Write-Host "`n=== Build complete ===" -ForegroundColor Green
Write-Host "API : $distApi"
Write-Host "Web : $distWeb"
Write-Host ""
Write-Host "Next: run .\Install-Service.ps1 on the target machine (as Administrator)." -ForegroundColor Cyan
