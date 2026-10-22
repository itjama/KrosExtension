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

.PARAMETER HostName
    DNS name users will open in the browser. Default: computer name.

.PARAMETER CertificatePath
    Path to a PFX file used by the Web HTTPS endpoint (with -CertificatePassword).
    When web\appsettings.Production.json already exists, passing any certificate
    parameter updates only the certificate in it.

.PARAMETER CertificatePassword
    Password of the PFX file (stored in web\appsettings.Production.json).

.PARAMETER CertificateSubject
    Subject of a certificate in LocalMachine\My used by the Web HTTPS endpoint
    (alternative to -CertificatePath).

.PARAMETER BootstrapAdminPassword
    Initial password of the 'admin' account, written to Bootstrap:AdminPassword in
    api\appsettings.Production.json. When the file already exists, only that key is
    updated, and only when this parameter is passed.

.EXAMPLE
    .\Install-Service.ps1
    .\Install-Service.ps1 -SqlServer "MYSERVER" -InstallDir "D:\Services\KrosExtension"
#>

param(
    [string]$InstallDir = 'C:\KrosExtension',
    [string]$SqlServer  = '.\SQLEXPRESS',
    [string]$Database   = 'KrosExtension',
    [string]$SourceDir  = '',
    [string]$HostName   = $env:COMPUTERNAME,
    [string]$CertificatePath = '',
    [string]$CertificatePassword = '',
    [string]$CertificateSubject = '',
    [string]$BootstrapAdminPassword = ''
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
# Copy the CONTENTS into an existing destination (copying the folder itself would nest api\api on re-install).
New-Item -ItemType Directory -Path $apiDest -Force | Out-Null
New-Item -ItemType Directory -Path $webDest -Force | Out-Null
Copy-Item -Path (Join-Path $apiSrc '*') -Destination $apiDest -Recurse -Force
Copy-Item -Path (Join-Path $webSrc '*') -Destination $webDest -Recurse -Force

# ── Production config ─────────────────────────────────────────────────────────
# Created when missing. An existing file is kept as is, except that the keys
# for explicitly passed parameters are updated in place (certificate in the
# Web file, Bootstrap:AdminPassword in the Api file). Secrets are never printed.
function Set-JsonProperty {
    param($Object, [string]$Name, $Value)
    if ($Object.PSObject.Properties[$Name]) {
        $Object.$Name = $Value
    } else {
        $Object | Add-Member -NotePropertyName $Name -NotePropertyValue $Value
    }
}

function Get-JsonChild {
    # Returns the child object $Name, creating it (or replacing a non-object value) when needed.
    param($Object, [string]$Name)
    $property = $Object.PSObject.Properties[$Name]
    if (-not $property -or $property.Value -isnot [System.Management.Automation.PSCustomObject]) {
        Set-JsonProperty $Object $Name ([pscustomobject]@{})
    }
    return $Object.$Name
}

function Read-JsonConfig {
    param([string]$Path)
    $config = $null
    if (Test-Path $Path) {
        $config = Get-Content $Path -Raw | ConvertFrom-Json
    }
    if ($null -eq $config) {
        $config = [pscustomobject]@{}
    }
    return $config
}

function Set-WebHttpsConfig {
    # Ensures the HTTPS endpoint exists and applies the certificate parameters that were passed.
    # Returns $true when a certificate is configured afterwards.
    param($Config, $Bound)
    $endpoints = Get-JsonChild (Get-JsonChild $Config 'Kestrel') 'Endpoints'
    $https = Get-JsonChild $endpoints 'Https'
    if (-not $https.PSObject.Properties['Url']) {
        Set-JsonProperty $https 'Url' 'https://0.0.0.0:5284'
    }

    if ($Bound.ContainsKey('CertificatePath') -and $Bound['CertificatePath']) {
        $password = ''
        if ($Bound.ContainsKey('CertificatePassword')) { $password = $Bound['CertificatePassword'] }
        Set-JsonProperty $https 'Certificate' ([pscustomobject][ordered]@{ Path = $Bound['CertificatePath']; Password = $password })
    } elseif ($Bound.ContainsKey('CertificateSubject') -and $Bound['CertificateSubject']) {
        Set-JsonProperty $https 'Certificate' ([pscustomobject][ordered]@{ Subject = $Bound['CertificateSubject']; Store = 'My'; Location = 'LocalMachine' })
    } elseif ($Bound.ContainsKey('CertificatePassword') -and $https.PSObject.Properties['Certificate'] -and
              $https.Certificate -is [System.Management.Automation.PSCustomObject] -and $https.Certificate.PSObject.Properties['Path']) {
        # Only the PFX password changed.
        Set-JsonProperty $https.Certificate 'Password' $Bound['CertificatePassword']
    }

    return [bool]($https.PSObject.Properties['Certificate'] -and $https.Certificate)
}

function Set-ApiBootstrapConfig {
    param($Config, $Bound)
    $bootstrap = Get-JsonChild $Config 'Bootstrap'
    if (-not $bootstrap.PSObject.Properties['AdminUserName'] -or -not $bootstrap.AdminUserName) {
        Set-JsonProperty $bootstrap 'AdminUserName' 'admin'
    }
    if ($Bound.ContainsKey('BootstrapAdminPassword') -or -not $bootstrap.PSObject.Properties['AdminPassword']) {
        $password = ''
        if ($Bound.ContainsKey('BootstrapAdminPassword')) { $password = $Bound['BootstrapAdminPassword'] }
        Set-JsonProperty $bootstrap 'AdminPassword' $password
    }
    return [bool]$bootstrap.AdminPassword
}

function Write-JsonConfig {
    param($Config, [string]$Path)
    $Config | ConvertTo-Json -Depth 10 | Set-Content $Path -Encoding utf8
}

function Protect-AdminOnly {
    # Restricts a file/folder holding secrets to Administrators and SYSTEM (SIDs, so it works on localized Windows).
    param([string]$Path, [switch]$Directory)
    $grant = if ($Directory) { '(OI)(CI)F' } else { 'F' }
    & icacls.exe $Path /inheritance:r /grant:r "*S-1-5-32-544:$grant" "*S-1-5-18:$grant" | Out-Null
    if ($LASTEXITCODE -ne 0) {
        Write-Warning "Could not restrict access to $Path (icacls exit code $LASTEXITCODE)."
    }
}

$certBound = $PSBoundParameters.ContainsKey('CertificatePath') -or
             $PSBoundParameters.ContainsKey('CertificatePassword') -or
             $PSBoundParameters.ContainsKey('CertificateSubject')

$webProd = Join-Path $webDest 'appsettings.Production.json'
if (-not (Test-Path $webProd) -or $certBound) {
    $existed = Test-Path $webProd
    $webCfg = Read-JsonConfig $webProd
    $hasCert = Set-WebHttpsConfig $webCfg $PSBoundParameters
    Write-JsonConfig $webCfg $webProd
    if ($existed) { Write-Host "    Updated certificate settings in $webProd" } else { Write-Host "    Created $webProd" }
    if (-not $hasCert) {
        Write-Warning "No certificate configured (-CertificatePath / -CertificateSubject). HTTPS will not start until $webProd is completed."
    }
} else {
    Write-Host "    Keeping existing $webProd"
}
Protect-AdminOnly $webProd

$apiProd = Join-Path $apiDest 'appsettings.Production.json'
if (-not (Test-Path $apiProd) -or $PSBoundParameters.ContainsKey('BootstrapAdminPassword')) {
    $existed = Test-Path $apiProd
    $apiCfg = Read-JsonConfig $apiProd
    $hasPassword = Set-ApiBootstrapConfig $apiCfg $PSBoundParameters
    Write-JsonConfig $apiCfg $apiProd
    if ($existed) { Write-Host "    Updated Bootstrap:AdminPassword in $apiProd" } else { Write-Host "    Created $apiProd" }
    if (-not $hasPassword) {
        Write-Warning "Bootstrap:AdminPassword is empty. If no active admin exists yet, set it in $apiProd before the first login."
    }
} else {
    Write-Host "    Keeping existing $apiProd"
}
Protect-AdminOnly $apiProd

# Data protection key ring (auth cookies, encrypted SQL passwords). The Api creates it on first start;
# create it now so it can be restricted to Administrators and SYSTEM from the beginning.
$keysDir = Join-Path $env:ProgramData 'KrosExtension\keys'
New-Item -ItemType Directory -Path $keysDir -Force | Out-Null
Protect-AdminOnly $keysDir -Directory

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
Write-Host "Web UI: https://${HostName}:5284" -ForegroundColor Cyan
Write-Host ""
if ($apiStatus -ne 'Running' -or $webStatus -ne 'Running') {
    Write-Warning "One or more services did not start. Check Event Viewer > Windows Logs > Application."
}
