[CmdletBinding()]
param(
    [string]$ServerInstance = '(localdb)\KrosAnalysis',
    [string]$InputPath,
    [string]$WorkingPath,
    [string]$OutputPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if (-not $InputPath) {
    $InputPath = Join-Path $PSScriptRoot '..\data\input'
}
if (-not $WorkingPath) {
    $WorkingPath = Join-Path $PSScriptRoot '..\data\working'
}
if (-not $OutputPath) {
    $OutputPath = Join-Path $PSScriptRoot '..\data\output'
}

function ConvertTo-SqlLiteral {
    param([Parameter(Mandatory)][string]$Value)
    return $Value.Replace("'", "''")
}

function Get-SourceToken {
    param([Parameter(Mandatory)][System.IO.FileInfo]$File)

    $identity = '{0}|{1}|{2}' -f $File.FullName, $File.Length, $File.LastWriteTimeUtc.Ticks
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        $bytes = [System.Text.Encoding]::UTF8.GetBytes($identity)
        $hash = $sha.ComputeHash($bytes)
        return ([System.BitConverter]::ToString($hash).Replace('-', '').Substring(0, 10))
    }
    finally {
        $sha.Dispose()
    }
}

function Invoke-DataTable {
    param(
        [Parameter(Mandatory)][System.Data.SqlClient.SqlConnection]$Connection,
        [Parameter(Mandatory)][string]$Sql
    )

    $command = $Connection.CreateCommand()
    $command.CommandTimeout = 300
    $command.CommandText = $Sql
    $adapter = New-Object System.Data.SqlClient.SqlDataAdapter $command
    $table = New-Object System.Data.DataTable
    [void]$adapter.Fill($table)
    return ,$table
}

function Restore-BackupForAnalysis {
    param(
        [Parameter(Mandatory)][System.Data.SqlClient.SqlConnection]$Connection,
        [Parameter(Mandatory)][System.IO.FileInfo]$Backup,
        [Parameter(Mandatory)][ValidateSet('omega', 'olymp')][string]$Product,
        [Parameter(Mandatory)][string]$Token,
        [Parameter(Mandatory)][string]$RestoreFolder
    )

    $dbName = 'Kros_Analysis_{0}_{1}' -f $Product, $Token
    $check = Invoke-DataTable -Connection $Connection -Sql (
        "SELECT COUNT(*) AS Cnt FROM sys.databases WHERE name = N'{0}'" -f (ConvertTo-SqlLiteral $dbName)
    )
    if ([int]$check.Rows[0].Cnt -eq 0) {
        $fileList = Invoke-DataTable -Connection $Connection -Sql (
            "RESTORE FILELISTONLY FROM DISK = N'{0}'" -f (ConvertTo-SqlLiteral $Backup.FullName)
        )
        $dataFile = $fileList | Where-Object Type -eq 'D' | Select-Object -First 1
        $logFile = $fileList | Where-Object Type -eq 'L' | Select-Object -First 1
        if (-not $dataFile -or -not $logFile) {
            throw "Backup does not contain one data and one log file: $($Backup.FullName)"
        }

        $mdf = Join-Path $RestoreFolder ('{0}_{1}.mdf' -f $Product, $Token)
        $ldf = Join-Path $RestoreFolder ('{0}_{1}_log.ldf' -f $Product, $Token)
        $restoreSql = @"
RESTORE DATABASE [$dbName]
FROM DISK = N'$(ConvertTo-SqlLiteral $Backup.FullName)'
WITH MOVE N'$(ConvertTo-SqlLiteral $dataFile.LogicalName)' TO N'$(ConvertTo-SqlLiteral $mdf)',
     MOVE N'$(ConvertTo-SqlLiteral $logFile.LogicalName)' TO N'$(ConvertTo-SqlLiteral $ldf)',
RECOVERY, STATS = 10;
ALTER DATABASE [$dbName] SET AUTO_CLOSE OFF;
"@
        [void](Invoke-DataTable -Connection $Connection -Sql $restoreSql)
    }

    $header = Invoke-DataTable -Connection $Connection -Sql (
        "RESTORE HEADERONLY FROM DISK = N'{0}'" -f (ConvertTo-SqlLiteral $Backup.FullName)
    )
    return [PSCustomObject]@{
        Product      = $Product.ToUpperInvariant()
        SourceId     = ('{0}-{1}' -f $Product.ToUpperInvariant(), $Token)
        DatabaseName = $dbName
        SnapshotDate = ([datetime]$header.Rows[0].BackupFinishDate).Date
    }
}

function Get-OmegaMetrics {
    param(
        [Parameter(Mandatory)][System.Data.SqlClient.SqlConnection]$Connection,
        [Parameter(Mandatory)]$Source
    )

    $snapshot = $Source.SnapshotDate.ToString('yyyy-MM-dd')
    $sql = @"
DECLARE @Snapshot date = '$snapshot';
WITH activity AS (
    SELECT 'EUD_' + C023_CisloInterneKodOkruh AS Metric,
           TRY_CONVERT(date, CONCAT(C116_RokZaevidovania, '-', C115_MesZaevidovania, '-', C114_DenZaevidovania)) AS ActivityDate
    FROM [$($Source.DatabaseName)].dbo.T040_EUD
    WHERE ISNULL(C001_Vymazany, 0) = 0
      AND C023_CisloInterneKodOkruh IN ('DF', 'OF', 'DPF', 'OPF', 'zDF', 'zOF')
    UNION ALL
    SELECT 'FAKTURACIA_T228_' + C022_KodEvidencia AS Metric,
           TRY_CONVERT(date, CONCAT(C062_RokVyst, '-', C061_MesVyst, '-', C060_DenVyst)) AS ActivityDate
    FROM [$($Source.DatabaseName)].dbo.T228_Faktury
    WHERE C022_KodEvidencia IN ('OF', 'OPF')
)
SELECT Metric,
       COUNT_BIG(*) AS TotalCount,
       MIN(ActivityDate) AS FirstActivity,
       MAX(ActivityDate) AS LastActivity,
       SUM(CASE WHEN ActivityDate = @Snapshot THEN 1 ELSE 0 END) AS TodayCount,
       SUM(CASE WHEN ActivityDate >= DATEADD(day, -6, @Snapshot) AND ActivityDate <= @Snapshot THEN 1 ELSE 0 END) AS Last7DaysCount,
       SUM(CASE WHEN ActivityDate >= DATEADD(day, -29, @Snapshot) AND ActivityDate <= @Snapshot THEN 1 ELSE 0 END) AS Last30DaysCount
FROM activity
GROUP BY Metric
ORDER BY Metric;
"@
    return Invoke-DataTable -Connection $Connection -Sql $sql
}

function Get-OlympMetrics {
    param(
        [Parameter(Mandatory)][System.Data.SqlClient.SqlConnection]$Connection,
        [Parameter(Mandatory)]$Source
    )

    $snapshot = $Source.SnapshotDate.ToString('yyyy-MM-dd')
    $sql = @"
DECLARE @Snapshot date = '$snapshot';
WITH activity AS (
    SELECT 'ACTIVITY_LOG_SPOLU' AS Metric, CONVERT(date, StartTime) AS ActivityDate
    FROM [$($Source.DatabaseName)].dbo.activityLog
    UNION ALL
    SELECT CONCAT('ACTIVITY_LOG_TYP_', ActivityType) AS Metric, CONVERT(date, StartTime) AS ActivityDate
    FROM [$($Source.DatabaseName)].dbo.activityLog
)
SELECT Metric,
       COUNT_BIG(*) AS TotalCount,
       MIN(ActivityDate) AS FirstActivity,
       MAX(ActivityDate) AS LastActivity,
       SUM(CASE WHEN ActivityDate = @Snapshot THEN 1 ELSE 0 END) AS TodayCount,
       SUM(CASE WHEN ActivityDate >= DATEADD(day, -6, @Snapshot) AND ActivityDate <= @Snapshot THEN 1 ELSE 0 END) AS Last7DaysCount,
       SUM(CASE WHEN ActivityDate >= DATEADD(day, -29, @Snapshot) AND ActivityDate <= @Snapshot THEN 1 ELSE 0 END) AS Last30DaysCount
FROM activity
GROUP BY Metric
ORDER BY Metric;
"@
    return Invoke-DataTable -Connection $Connection -Sql $sql
}

$resolvedInput = [System.IO.Path]::GetFullPath($InputPath)
$resolvedWorking = [System.IO.Path]::GetFullPath($WorkingPath)
$resolvedOutput = [System.IO.Path]::GetFullPath($OutputPath)
[void](New-Item -ItemType Directory -Force -Path $resolvedWorking, $resolvedOutput)

$connectionString = "Server=$ServerInstance;Integrated Security=true;Encrypt=False;TrustServerCertificate=True;Initial Catalog=master;Connection Timeout=10"
$connection = New-Object System.Data.SqlClient.SqlConnection $connectionString
$output = New-Object System.Collections.Generic.List[object]

try {
    $connection.Open()
    $backups = Get-ChildItem -Path $resolvedInput -Recurse -File -Filter '*.bak'
    foreach ($backup in $backups) {
        $product = $backup.Directory.Name.ToLowerInvariant()
        if ($product -notin @('omega', 'olymp')) {
            Write-Warning "Skipping backup outside omega/olymp folders: $($backup.FullName)"
            continue
        }

        $token = Get-SourceToken -File $backup
        $source = Restore-BackupForAnalysis -Connection $connection -Backup $backup -Product $product -Token $token -RestoreFolder $resolvedWorking
        $metrics = if ($product -eq 'omega') {
            Get-OmegaMetrics -Connection $connection -Source $source
        }
        else {
            Get-OlympMetrics -Connection $connection -Source $source
        }

        foreach ($metric in $metrics.Rows) {
            $output.Add([PSCustomObject]@{
                SourceId        = $source.SourceId
                Product         = $source.Product
                SnapshotDate    = $source.SnapshotDate.ToString('yyyy-MM-dd')
                Metric          = $metric.Metric
                TotalCount      = $metric.TotalCount
                FirstActivity   = if ($metric.FirstActivity -is [DBNull]) { $null } else { ([datetime]$metric.FirstActivity).ToString('yyyy-MM-dd') }
                LastActivity    = if ($metric.LastActivity -is [DBNull]) { $null } else { ([datetime]$metric.LastActivity).ToString('yyyy-MM-dd') }
                TodayCount      = $metric.TodayCount
                Last7DaysCount  = $metric.Last7DaysCount
                Last30DaysCount = $metric.Last30DaysCount
            })
        }
    }
}
finally {
    $connection.Dispose()
}

$reportPath = Join-Path $resolvedOutput 'activity_summary.csv'
$output | Sort-Object Product, SourceId, Metric | Export-Csv -Path $reportPath -NoTypeInformation -Encoding UTF8
$output | Sort-Object Product, SourceId, Metric | Format-Table -AutoSize
Write-Output "Report written to: $reportPath"
