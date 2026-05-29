# Omega Monitoring Dashboard — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Extend the existing Omega dashboard with granular warning flags, a live company-detail drill-down page (workflow funnel, aging buckets, monthly heatmap), and polished global dashboard UX.

**Architecture:** The global snapshot table already covers all required columns and is computed by UpdateData. This plan adds three things: (1) split the single `DATA_QUALITY` warning into `ORPHAN_LINES` / `ORPHAN_PAYMENTS` / `UNKNOWN_CIRCUIT` / `NULL_DATE`, (2) a live on-demand company-detail endpoint with workflow funnel, backlog buckets, and monthly heatmap, and (3) frontend navigation to the detail page. No schema migrations are needed — `WarningFlags` stores comma-separated strings, new codes just extend the vocabulary.

**Tech Stack:** .NET 10 / C# / EF Core / Minimal API / SQL Server (backend); React 19 / TypeScript / Vite, single-file `main.tsx` (frontend)

---

## Reference: What Already Exists (do not re-implement)

| Feature | Location | Status |
|---------|----------|--------|
| Global snapshot table with all required columns | `OmegaDashboardPage` in `main.tsx` | ✅ Done |
| Health score — 5 components, weights, clamping | `OmegaDashboardSnapshotCalculator.CalculateScores` | ✅ Done |
| Warnings: OLD_UNPOSTED_*, ZERO_POSTING, ACTIVITY_DROP, IMPORT_ONLY, BROKEN_MONTH, DUPLICATES, DATA_QUALITY | `OmegaDashboardSnapshotCalculator.BuildWarnings` | ✅ Done |
| UpdateData snapshot pipeline | `CompanyDataRefreshService` → `OmegaDashboardSnapshotCalculator` | ✅ Done |
| `GET /api/omega/dashboard/companies` | `ProductCompanyEndpoints` | ✅ Done |

## Reference: Health Score Algorithm (no changes planned)

| Component | Weight | Scoring rule |
|-----------|--------|-------------|
| ActivityScore | 20 % | 100 if any 30d activity; 55 if idle + no backlog; 15 if idle + backlog; −25 if 7d < 50 % of prev 7d |
| CompletionScore | 30 % | Posting rate % capped 0–100; forced 0 if registered > 0 and posted = 0 |
| BacklogAgingScore | 25 % | Start 100; −3 per doc > 7d old (max −45); −10 per doc > 30d old (max −55); bonus −up to 30 for oldest open > 30d |
| MonthClosingScore | 20 % | Start 100; −35 if prev month rate < 95 %; −45 if historical rate < 98 % |
| DataQualityScore | 5 % | 100 if 0 issues; −10 per issue, min 0 |

`HealthScore = round(Activity×0.20 + Completion×0.30 + BacklogAging×0.25 + MonthClosing×0.20 + DataQuality×0.05)`

**Colour thresholds:** ≥ 85 → green `#10b981`; 70–84 → yellow `#f59e0b`; 50–69 → orange `#f97316`; < 50 → red `#ef4444`

## Reference: Warning System After This Plan

| Flag | Severity | Trigger | Change |
|------|----------|---------|--------|
| OLD_UNPOSTED_30D | critical | Backlog30DaysPlus > 0 | existing |
| OLD_UNPOSTED_7D | bad | Backlog7DaysPlus > 0 | existing |
| ZERO_POSTING | critical | registered > 0 and posted = 0 | existing |
| ACTIVITY_DROP | bad | 7d activity < 50 % of prev 7d | existing |
| IMPORT_ONLY | bad | platform+imported > 0 and posted = 0 | existing |
| BROKEN_MONTH | critical | prev month < 95 % or historical < 98 % | existing |
| DUPLICATES | bad | duplicate document links > 0 | existing |
| **NULL_DATE** | **bad** | docs with NULL RegisteredDate | **new** |
| **ORPHAN_LINES** | **bad** | orphan T041_EUD_Polozky rows | **new** |
| **ORPHAN_PAYMENTS** | **bad** | orphan T047_EUD_UhradyOdpocty rows | **new** |
| **UNKNOWN_CIRCUIT** | **critical** | docs referencing a non-existent circuit code | **new** |

## File Map

| File | Action | Purpose |
|------|--------|---------|
| `src/KrosExtension.Infrastructure/Companies/OmegaDashboardSnapshotCalculator.cs` | Modify | Split data_quality CTE into 4 columns; update metrics record and BuildWarnings |
| `src/KrosExtension.Application/Companies/OmegaMonthlyBreakdown.cs` | **Create** | Monthly heatmap DTO |
| `src/KrosExtension.Application/Companies/OmegaCompanyDetail.cs` | **Create** | Company detail composite DTO |
| `src/KrosExtension.Application/Companies/IOmegaCompanyDetailReader.cs` | **Create** | Reader interface |
| `src/KrosExtension.Infrastructure/Companies/OmegaCompanyDetailReader.cs` | **Create** | Two SQL queries: funnel+aging and monthly breakdown |
| `src/KrosExtension.Infrastructure/DependencyInjection.cs` | Modify | Register `IOmegaCompanyDetailReader` |
| `src/KrosExtension.Api/Endpoints/ProductCompanyEndpoints.cs` | Modify | Add `GET /api/omega/dashboard/companies/{serverId}/{db}` |
| `src/KrosExtension.Web/ClientApp/src/main.tsx` | Modify | Detail page component, navigation, types, snapshot age |
| `src/KrosExtension.Web/ClientApp/src/styles.css` | Modify | CSS for funnel, heatmap, score bars, aging buckets |

---

### Task 1: Split DATA_QUALITY into granular warning flags

**Files:**
- Modify: `src/KrosExtension.Infrastructure/Companies/OmegaDashboardSnapshotCalculator.cs`

The current `data_quality` CTE adds four issue counts into one integer. Replace it with four named columns. Update `OmegaDashboardMetrics` to carry them separately and compute the total as a property. Update `BuildWarnings` to emit specific flag codes. `DataQualityScore` continues to use the total — no snapshot entity change needed.

- [ ] **Step 1.1: Replace the `data_quality` CTE in `SnapshotSql`**

In `OmegaDashboardSnapshotCalculator.cs`, locate the `data_quality` CTE (the one that adds four `COUNT_BIG` values into a single `IssueCount`). Replace the entire CTE:

```sql
        data_quality AS (
            SELECT
                (
                    SELECT COUNT_BIG(*)
                    FROM documents
                    WHERE RegisteredDate IS NULL
                ) AS NullDateCount,
                (
                    SELECT COUNT_BIG(*)
                    FROM dbo.T041_EUD_Polozky AS p
                    LEFT JOIN dbo.T040_EUD AS h ON h.C000_ID = p.C010_IDEUD
                    WHERE h.C000_ID IS NULL
                ) AS OrphanLineCount,
                (
                    SELECT COUNT_BIG(*)
                    FROM dbo.T047_EUD_UhradyOdpocty AS p
                    LEFT JOIN dbo.T040_EUD AS h ON h.C000_ID = p.C010_IDEUD
                    WHERE h.C000_ID IS NULL
                ) AS OrphanPaymentCount,
                (
                    SELECT COUNT_BIG(*)
                    FROM dbo.T040_EUD AS e
                    LEFT JOIN dbo.T025_OkruhyUctovnychDokladov AS k
                        ON k.C021_Kod = e.C023_CisloInterneKodOkruh
                    WHERE ISNULL(e.C001_Vymazany, 0) = 0
                      AND e.C023_CisloInterneKodOkruh IN ('DF', 'OF', 'DPF', 'OPF', 'zDF', 'zOF')
                      AND k.C021_Kod IS NULL
                ) AS UnknownCircuitCount
        )
```

In the final SELECT, replace `data_quality.IssueCount AS DataQualityIssues` with:

```sql
               data_quality.NullDateCount,
               data_quality.OrphanLineCount,
               data_quality.OrphanPaymentCount,
               data_quality.UnknownCircuitCount
```

- [ ] **Step 1.2: Update `OmegaDashboardMetrics` record**

Replace the existing `OmegaDashboardMetrics` private record at the bottom of the file:

```csharp
    private sealed record OmegaDashboardMetrics(
        long Activity7Days,
        long Activity30Days,
        long Previous7DaysActivity,
        long Registered30Days,
        long Posted30Days,
        long BacklogCount,
        long Backlog7DaysPlus,
        long Backlog30DaysPlus,
        int? OldestOpenAgeDays,
        DateTime? LastActivityDate,
        decimal? PreviousMonthPostingRate,
        decimal? HistoricalPostingRate,
        long Imported30Days,
        long Platform30Days,
        long Manual30Days,
        long DuplicateDocumentLinks,
        long NullDateCount,
        long OrphanLineCount,
        long OrphanPaymentCount,
        long UnknownCircuitCount)
    {
        public long DataQualityIssues =>
            NullDateCount + OrphanLineCount + OrphanPaymentCount + UnknownCircuitCount;
    }
```

- [ ] **Step 1.3: Update `ReadSnapshotAsync` — metrics construction**

In `ReadSnapshotAsync`, find `new OmegaDashboardMetrics(...)`. Replace the last argument (`GetInt64(reader, "DataQualityIssues")`) with four arguments:

```csharp
            GetInt64(reader, "DuplicateDocumentLinks"),
            GetInt64(reader, "NullDateCount"),
            GetInt64(reader, "OrphanLineCount"),
            GetInt64(reader, "OrphanPaymentCount"),
            GetInt64(reader, "UnknownCircuitCount"));
```

The `DataQualityIssues` call in `CalculateScores` and the snapshot constructor already use `metrics.DataQualityIssues` which now comes from the computed property — no other changes needed there.

- [ ] **Step 1.4: Replace `BuildWarnings` method**

```csharp
    private static IReadOnlyList<string> BuildWarnings(OmegaDashboardMetrics metrics)
    {
        var warnings = new List<string>();

        if (metrics.Backlog30DaysPlus > 0)
            warnings.Add("OLD_UNPOSTED_30D");

        if (metrics.Backlog7DaysPlus > 0)
            warnings.Add("OLD_UNPOSTED_7D");

        if (metrics.Registered30Days > 0 && metrics.Posted30Days == 0)
            warnings.Add("ZERO_POSTING");

        if (metrics.Previous7DaysActivity > 0 && metrics.Activity7Days < metrics.Previous7DaysActivity / 2m)
            warnings.Add("ACTIVITY_DROP");

        if (metrics.Imported30Days + metrics.Platform30Days > 0 && metrics.Posted30Days == 0)
            warnings.Add("IMPORT_ONLY");

        if (metrics.PreviousMonthPostingRate is < 95m || metrics.HistoricalPostingRate is < 98m)
            warnings.Add("BROKEN_MONTH");

        if (metrics.DuplicateDocumentLinks > 0)
            warnings.Add("DUPLICATES");

        if (metrics.NullDateCount > 0)
            warnings.Add("NULL_DATE");

        if (metrics.OrphanLineCount > 0)
            warnings.Add("ORPHAN_LINES");

        if (metrics.OrphanPaymentCount > 0)
            warnings.Add("ORPHAN_PAYMENTS");

        if (metrics.UnknownCircuitCount > 0)
            warnings.Add("UNKNOWN_CIRCUIT");

        return warnings;
    }
```

- [ ] **Step 1.5: Update frontend warning tables in `main.tsx`**

Replace both helper functions:

```typescript
function warningSeverity(warning: string) {
  if (['OLD_UNPOSTED_30D', 'ZERO_POSTING', 'BROKEN_MONTH', 'UNKNOWN_CIRCUIT'].includes(warning)) {
    return 'critical';
  }

  if (['OLD_UNPOSTED_7D', 'ACTIVITY_DROP', 'IMPORT_ONLY', 'DUPLICATES',
       'ORPHAN_LINES', 'ORPHAN_PAYMENTS', 'NULL_DATE'].includes(warning)) {
    return 'bad';
  }

  return 'watch';
}

function warningLabel(warning: string) {
  const labels: Record<string, string> = {
    OLD_UNPOSTED_30D: '30d backlog',
    OLD_UNPOSTED_7D: '7d backlog',
    ZERO_POSTING: 'no posting',
    ACTIVITY_DROP: 'drop',
    IMPORT_ONLY: 'import only',
    BROKEN_MONTH: 'broken month',
    DUPLICATES: 'duplicates',
    NULL_DATE: 'null date',
    ORPHAN_LINES: 'orphan lines',
    ORPHAN_PAYMENTS: 'orphan payments',
    UNKNOWN_CIRCUIT: 'unknown circuit'
  };

  return labels[warning] ?? warning;
}
```

- [ ] **Step 1.6: Build Infrastructure**

```powershell
dotnet build .\src\KrosExtension.Infrastructure\KrosExtension.Infrastructure.csproj
```

Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 1.7: Smoke test — UpdateData then check warning flags**

```powershell
dotnet run --project .\src\KrosExtension.Api\KrosExtension.Api.csproj
```

```powershell
Invoke-RestMethod -Uri 'http://localhost:5000/api/omega/companies/update-data' -Method POST -ContentType 'application/json' -Body '{"databaseName": null}'
(Invoke-RestMethod 'http://localhost:5000/api/omega/dashboard/companies')[0].warningFlags
```

Expected: array of strings; no `DATA_QUALITY` value present; any of the new codes appear if data issues exist.

- [ ] **Step 1.8: Commit**

```bash
git add src/KrosExtension.Infrastructure/Companies/OmegaDashboardSnapshotCalculator.cs
git add src/KrosExtension.Web/ClientApp/src/main.tsx
git commit -m "feat: split DATA_QUALITY into ORPHAN_LINES, ORPHAN_PAYMENTS, UNKNOWN_CIRCUIT, NULL_DATE warnings"
```

---

### Task 2: Create Application-layer DTOs for company detail

**Files:**
- Create: `src/KrosExtension.Application/Companies/OmegaMonthlyBreakdown.cs`
- Create: `src/KrosExtension.Application/Companies/OmegaCompanyDetail.cs`
- Create: `src/KrosExtension.Application/Companies/IOmegaCompanyDetailReader.cs`

- [ ] **Step 2.1: Create `OmegaMonthlyBreakdown.cs`**

```csharp
namespace KrosExtension.Application.Companies;

public sealed record OmegaMonthlyBreakdown(
    int Year,
    int Month,
    long Registered,
    long Posted,
    decimal? PostingRate);
```

- [ ] **Step 2.2: Create `OmegaCompanyDetail.cs`**

```csharp
namespace KrosExtension.Application.Companies;

public sealed record OmegaCompanyDetail(
    Guid DataReadServerId,
    string ServerDisplayName,
    string DatabaseName,
    string CompanyName,
    int? AccountingYear,
    DateTimeOffset SnapshotAt,
    int HealthScore,
    int ActivityScore,
    int CompletionScore,
    int BacklogAgingScore,
    int MonthClosingScore,
    int DataQualityScore,
    long Registered30Days,
    long PreAccounted30Days,
    long Posted30Days,
    long Platform30Days,
    long Imported30Days,
    long Manual30Days,
    long BacklogTotal,
    long Backlog0to3Days,
    long Backlog4to7Days,
    long Backlog8to30Days,
    long Backlog30DaysPlus,
    IReadOnlyList<OmegaMonthlyBreakdown> MonthlyBreakdown,
    IReadOnlyList<string> WarningFlags);
```

- [ ] **Step 2.3: Create `IOmegaCompanyDetailReader.cs`**

```csharp
namespace KrosExtension.Application.Companies;

public interface IOmegaCompanyDetailReader
{
    Task<OmegaCompanyDetail?> GetCompanyDetailAsync(
        Guid dataReadServerId,
        string databaseName,
        CancellationToken cancellationToken = default);
}
```

- [ ] **Step 2.4: Build Application**

```powershell
dotnet build .\src\KrosExtension.Application\KrosExtension.Application.csproj
```

Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 2.5: Commit**

```bash
git add src/KrosExtension.Application/Companies/OmegaMonthlyBreakdown.cs
git add src/KrosExtension.Application/Companies/OmegaCompanyDetail.cs
git add src/KrosExtension.Application/Companies/IOmegaCompanyDetailReader.cs
git commit -m "feat: add OmegaCompanyDetail DTOs and IOmegaCompanyDetailReader interface"
```

---

### Task 3: Implement OmegaCompanyDetailReader

**Files:**
- Create: `src/KrosExtension.Infrastructure/Companies/OmegaCompanyDetailReader.cs`
- Modify: `src/KrosExtension.Infrastructure/DependencyInjection.cs`

The reader: (1) fetches the latest snapshot from `KrosExtension` DB for health scores and warning flags, (2) connects live to the Omega company DB and runs two queries — `FunnelSql` (workflow funnel + backlog buckets) and `MonthlySql` (last 12 calendar months). The snapshot gives us computed scores without re-deriving them; the live queries give granular funnel and heatmap data not stored in the snapshot.

- [ ] **Step 3.1: Create `OmegaCompanyDetailReader.cs`**

```csharp
using KrosExtension.Application.Companies;
using KrosExtension.Domain.Configuration;
using KrosExtension.Infrastructure.Configuration;
using KrosExtension.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace KrosExtension.Infrastructure.Companies;

public sealed class OmegaCompanyDetailReader(ApplicationDbContext dbContext) : IOmegaCompanyDetailReader
{
    public async Task<OmegaCompanyDetail?> GetCompanyDetailAsync(
        Guid dataReadServerId,
        string databaseName,
        CancellationToken cancellationToken = default)
    {
        var server = await dbContext.DataReadServers
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == dataReadServerId && s.IsActive, cancellationToken);

        if (server is null)
        {
            return null;
        }

        var snapshot = await dbContext.OmegaCompanyDashboardSnapshots
            .AsNoTracking()
            .Where(s => s.DataReadServerId == dataReadServerId && s.DatabaseName == databaseName)
            .OrderByDescending(s => s.SnapshotAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (snapshot is null)
        {
            return null;
        }

        var connectionString = DataReadServerConnectionStringFactory.CreateDatabaseConnectionString(
            server, ProductType.Omega, databaseName);

        if (connectionString is null)
        {
            return null;
        }

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var funnel = await ReadFunnelAsync(connection, DateTime.Today, cancellationToken);
        var monthly = await ReadMonthlyAsync(connection, DateTime.Today, cancellationToken);

        var warnings = string.IsNullOrWhiteSpace(snapshot.WarningFlags)
            ? (IReadOnlyList<string>)[]
            : snapshot.WarningFlags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return new OmegaCompanyDetail(
            snapshot.DataReadServerId,
            snapshot.ServerDisplayName,
            snapshot.DatabaseName,
            snapshot.CompanyName,
            snapshot.AccountingYear,
            snapshot.SnapshotAt,
            snapshot.HealthScore,
            snapshot.ActivityScore,
            snapshot.CompletionScore,
            snapshot.BacklogAgingScore,
            snapshot.MonthClosingScore,
            snapshot.DataQualityScore,
            funnel.Registered30d,
            funnel.PreAccounted30d,
            funnel.Posted30d,
            funnel.Platform30d,
            funnel.Imported30d,
            funnel.Manual30d,
            funnel.Bucket0to3d + funnel.Bucket4to7d + funnel.Bucket8to30d + funnel.Bucket30dPlus,
            funnel.Bucket0to3d,
            funnel.Bucket4to7d,
            funnel.Bucket8to30d,
            funnel.Bucket30dPlus,
            monthly,
            warnings);
    }

    private static async Task<FunnelMetrics> ReadFunnelAsync(
        SqlConnection connection,
        DateTime today,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = FunnelSql;
        command.Parameters.Add(new SqlParameter("@Today", SqlDbType.Date) { Value = today.Date });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return new FunnelMetrics(0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
        }

        return new FunnelMetrics(
            GetInt64(reader, "Registered30d"),
            GetInt64(reader, "PreAccounted30d"),
            GetInt64(reader, "Posted30d"),
            GetInt64(reader, "Platform30d"),
            GetInt64(reader, "Imported30d"),
            GetInt64(reader, "Manual30d"),
            GetInt64(reader, "Bucket0to3d"),
            GetInt64(reader, "Bucket4to7d"),
            GetInt64(reader, "Bucket8to30d"),
            GetInt64(reader, "Bucket30dPlus"));
    }

    private static async Task<IReadOnlyList<OmegaMonthlyBreakdown>> ReadMonthlyAsync(
        SqlConnection connection,
        DateTime today,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = MonthlySql;
        command.Parameters.Add(new SqlParameter("@Today", SqlDbType.Date) { Value = today.Date });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var list = new List<OmegaMonthlyBreakdown>();

        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(new OmegaMonthlyBreakdown(
                reader.GetInt32(reader.GetOrdinal("Year")),
                reader.GetInt32(reader.GetOrdinal("Month")),
                GetInt64(reader, "Registered"),
                GetInt64(reader, "Posted"),
                GetNullableDecimal(reader, "PostingRate")));
        }

        return list;
    }

    private static long GetInt64(SqlDataReader reader, string name)
    {
        var value = reader[name];
        return value is null or DBNull ? 0 : Convert.ToInt64(value);
    }

    private static decimal? GetNullableDecimal(SqlDataReader reader, string name)
    {
        var value = reader[name];
        return value is null or DBNull ? null : Convert.ToDecimal(value);
    }

    private sealed record FunnelMetrics(
        long Registered30d,
        long PreAccounted30d,
        long Posted30d,
        long Platform30d,
        long Imported30d,
        long Manual30d,
        long Bucket0to3d,
        long Bucket4to7d,
        long Bucket8to30d,
        long Bucket30dPlus);

    private const string FunnelSql = """
        DECLARE @From30 date = DATEADD(day, -29, @Today);

        WITH documents AS (
            SELECT
                TRY_CONVERT(date, CONCAT(
                    e.C116_RokZaevidovania, '-',
                    e.C115_MesZaevidovania, '-',
                    e.C114_DenZaevidovania
                )) AS RegisteredDate,
                CASE WHEN NULLIF(LTRIM(RTRIM(e.C027_KodZauctoval)), '') IS NULL
                     THEN CAST(0 AS bit) ELSE CAST(1 AS bit) END AS IsPosted,
                CASE WHEN e.C108_PREdkontovany <> 0
                     THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS IsPreAccounted,
                CASE WHEN NULLIF(LTRIM(RTRIM(e.C187_PlatformaId)), '') IS NULL
                     THEN CAST(0 AS bit) ELSE CAST(1 AS bit) END AS HasPlatformId,
                e.C138_PovodDokladu AS OriginCode
            FROM dbo.T040_EUD AS e
            WHERE ISNULL(e.C001_Vymazany, 0) = 0
              AND e.C023_CisloInterneKodOkruh IN ('DF', 'OF', 'DPF', 'OPF', 'zDF', 'zOF')
        ),
        funnel AS (
            SELECT
                COUNT(*) AS Registered30d,
                SUM(CASE WHEN IsPreAccounted = 1 THEN 1 ELSE 0 END) AS PreAccounted30d,
                SUM(CASE WHEN IsPosted = 1 THEN 1 ELSE 0 END) AS Posted30d,
                SUM(CASE WHEN HasPlatformId = 1 THEN 1 ELSE 0 END) AS Platform30d,
                SUM(CASE WHEN HasPlatformId = 0 AND ISNULL(OriginCode, 0) <> 0 THEN 1 ELSE 0 END) AS Imported30d,
                SUM(CASE WHEN HasPlatformId = 0 AND ISNULL(OriginCode, 0) = 0 THEN 1 ELSE 0 END) AS Manual30d
            FROM documents
            WHERE RegisteredDate BETWEEN @From30 AND @Today
        ),
        backlog AS (
            SELECT
                SUM(CASE WHEN DATEDIFF(day, RegisteredDate, @Today) <= 3 THEN 1 ELSE 0 END) AS Bucket0to3d,
                SUM(CASE WHEN DATEDIFF(day, RegisteredDate, @Today) BETWEEN 4 AND 7 THEN 1 ELSE 0 END) AS Bucket4to7d,
                SUM(CASE WHEN DATEDIFF(day, RegisteredDate, @Today) BETWEEN 8 AND 30 THEN 1 ELSE 0 END) AS Bucket8to30d,
                SUM(CASE WHEN DATEDIFF(day, RegisteredDate, @Today) > 30 THEN 1 ELSE 0 END) AS Bucket30dPlus
            FROM documents
            WHERE IsPosted = 0
              AND RegisteredDate IS NOT NULL
        )
        SELECT
            f.Registered30d, f.PreAccounted30d, f.Posted30d,
            f.Platform30d, f.Imported30d, f.Manual30d,
            b.Bucket0to3d, b.Bucket4to7d, b.Bucket8to30d, b.Bucket30dPlus
        FROM funnel AS f
        CROSS JOIN backlog AS b
        """;

    private const string MonthlySql = """
        WITH documents AS (
            SELECT
                TRY_CONVERT(date, CONCAT(
                    e.C116_RokZaevidovania, '-',
                    e.C115_MesZaevidovania, '-',
                    e.C114_DenZaevidovania
                )) AS RegisteredDate,
                CASE WHEN NULLIF(LTRIM(RTRIM(e.C027_KodZauctoval)), '') IS NULL
                     THEN CAST(0 AS bit) ELSE CAST(1 AS bit) END AS IsPosted
            FROM dbo.T040_EUD AS e
            WHERE ISNULL(e.C001_Vymazany, 0) = 0
              AND e.C023_CisloInterneKodOkruh IN ('DF', 'OF', 'DPF', 'OPF', 'zDF', 'zOF')
        )
        SELECT
            YEAR(RegisteredDate)  AS Year,
            MONTH(RegisteredDate) AS Month,
            COUNT(*) AS Registered,
            SUM(CASE WHEN IsPosted = 1 THEN 1 ELSE 0 END) AS Posted,
            CASE WHEN COUNT(*) = 0 THEN NULL
                 ELSE CONVERT(decimal(5, 2),
                      SUM(CASE WHEN IsPosted = 1 THEN 1 ELSE 0 END) * 100.0 / COUNT(*))
            END AS PostingRate
        FROM documents
        WHERE RegisteredDate IS NOT NULL
          AND RegisteredDate >= DATEADD(month, -11, DATEFROMPARTS(YEAR(@Today), MONTH(@Today), 1))
          AND RegisteredDate <  DATEADD(month,   1, DATEFROMPARTS(YEAR(@Today), MONTH(@Today), 1))
        GROUP BY YEAR(RegisteredDate), MONTH(RegisteredDate)
        ORDER BY Year, Month
        """;
}
```

- [ ] **Step 3.2: Register the new reader in DI**

In `src/KrosExtension.Infrastructure/DependencyInjection.cs`, after the `IOmegaDashboardReader` line, add:

```csharp
        services.AddScoped<IOmegaCompanyDetailReader, OmegaCompanyDetailReader>();
```

- [ ] **Step 3.3: Build Infrastructure**

```powershell
dotnet build .\src\KrosExtension.Infrastructure\KrosExtension.Infrastructure.csproj
```

Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 3.4: Commit**

```bash
git add src/KrosExtension.Infrastructure/Companies/OmegaCompanyDetailReader.cs
git add src/KrosExtension.Infrastructure/DependencyInjection.cs
git commit -m "feat: implement OmegaCompanyDetailReader with funnel and monthly breakdown SQL"
```

---

### Task 4: Add company detail API endpoint

**Files:**
- Modify: `src/KrosExtension.Api/Endpoints/ProductCompanyEndpoints.cs`

- [ ] **Step 4.1: Add the endpoint inside the Omega-only block**

In `ProductCompanyEndpoints.cs`, find the `if (productType == ProductType.Omega)` block that registers `GetOmegaDashboardCompanies`. Add a second `MapGet` immediately after it:

```csharp
        if (productType == ProductType.Omega)
        {
            productGroup.MapGet("/dashboard/companies", async (
                IOmegaDashboardReader dashboardReader,
                CancellationToken cancellationToken) =>
            {
                var companies = await dashboardReader.GetCompaniesAsync(cancellationToken);
                return Results.Ok(companies);
            }).WithName("GetOmegaDashboardCompanies");

            productGroup.MapGet("/dashboard/companies/{dataReadServerId:guid}/{databaseName}", async (
                Guid dataReadServerId,
                string databaseName,
                IOmegaCompanyDetailReader detailReader,
                CancellationToken cancellationToken) =>
            {
                var detail = await detailReader.GetCompanyDetailAsync(
                    dataReadServerId, databaseName, cancellationToken);

                return detail is null ? Results.NotFound() : Results.Ok(detail);
            }).WithName("GetOmegaCompanyDetail");
        }
```

- [ ] **Step 4.2: Build the full solution**

```powershell
dotnet build .\KrosExtension.sln
```

Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 4.3: Smoke test the detail endpoint**

```powershell
dotnet run --project .\src\KrosExtension.Api\KrosExtension.Api.csproj
```

```powershell
$companies = Invoke-RestMethod 'http://localhost:5000/api/omega/dashboard/companies'
$c = $companies[0]
Invoke-RestMethod "http://localhost:5000/api/omega/dashboard/companies/$($c.dataReadServerId)/$($c.databaseName)"
```

Expected: JSON object with `registered30Days`, `preAccounted30Days`, `posted30Days`, `backlog*`, `monthlyBreakdown` (array), `warningFlags`.

- [ ] **Step 4.4: Commit**

```bash
git add src/KrosExtension.Api/Endpoints/ProductCompanyEndpoints.cs
git commit -m "feat: add GET /api/omega/dashboard/companies/{serverId}/{db} detail endpoint"
```

---

### Task 5: Frontend — global dashboard UX improvements

**Files:**
- Modify: `src/KrosExtension.Web/ClientApp/src/main.tsx`

Three changes: (1) add the `OmegaCompanyDetail` and `OmegaMonthlyBreakdown` TypeScript types, (2) add detail navigation state to `App` and wire it into the workspace renderer, (3) update `OmegaDashboardPage` to accept an `onOpenDetail` prop and make company names clickable, plus add a snapshot age badge.

- [ ] **Step 5.1: Add TypeScript types after existing `OmegaDashboardCompany` type**

```typescript
type OmegaMonthlyBreakdown = {
  year: number;
  month: number;
  registered: number;
  posted: number;
  postingRate: number | null;
};

type OmegaCompanyDetail = {
  dataReadServerId: string;
  serverDisplayName: string;
  databaseName: string;
  companyName: string;
  accountingYear: number | null;
  snapshotAt: string;
  healthScore: number;
  activityScore: number;
  completionScore: number;
  backlogAgingScore: number;
  monthClosingScore: number;
  dataQualityScore: number;
  registered30Days: number;
  preAccounted30Days: number;
  posted30Days: number;
  platform30Days: number;
  imported30Days: number;
  manual30Days: number;
  backlogTotal: number;
  backlog0to3Days: number;
  backlog4to7Days: number;
  backlog8to30Days: number;
  backlog30DaysPlus: number;
  monthlyBreakdown: OmegaMonthlyBreakdown[];
  warningFlags: string[];
};
```

- [ ] **Step 5.2: Add detail navigation state to `App` and update workspace renderer**

Add one new `useState` in `App` (after the existing two):

```typescript
  const [omegaDetail, setOmegaDetail] = useState<{ dataReadServerId: string; databaseName: string } | null>(null);
```

Replace the workspace section's Omega stats line. Old:

```typescript
        {section === 'stats-omega' && <OmegaDashboardPage />}
```

New:

```typescript
        {section === 'stats-omega' && (omegaDetail
          ? <OmegaCompanyDetailPage
              dataReadServerId={omegaDetail.dataReadServerId}
              databaseName={omegaDetail.databaseName}
              onBack={() => setOmegaDetail(null)}
            />
          : <OmegaDashboardPage
              onOpenDetail={(serverId, db) => setOmegaDetail({ dataReadServerId: serverId, databaseName: db })}
            />
        )}
```

Also reset detail when the user navigates away from stats-omega. In the sidebar `onClick` for the Omega button, replace:

```typescript
onClick={() => setSection('stats-omega')}
```

with:

```typescript
onClick={() => { setSection('stats-omega'); setOmegaDetail(null); }}
```

- [ ] **Step 5.3: Update `OmegaDashboardPage` signature**

Change the function signature from:

```typescript
function OmegaDashboardPage() {
```

to:

```typescript
function OmegaDashboardPage({ onOpenDetail }: { onOpenDetail: (dataReadServerId: string, databaseName: string) => void }) {
```

- [ ] **Step 5.4: Make company cell clickable**

In `OmegaDashboardPage`, replace the company `<td>`:

Old:
```typescript
              <td>
                <strong>{company.companyName}</strong>
                <span className="muted-line">{company.databaseName}</span>
              </td>
```

New:
```typescript
              <td>
                <button
                  className="company-link"
                  onClick={() => onOpenDetail(company.dataReadServerId, company.databaseName)}
                >
                  <strong>{company.companyName}</strong>
                  <span className="muted-line">{company.databaseName}</span>
                </button>
              </td>
```

- [ ] **Step 5.5: Add snapshot age display and helper function**

Add helper after `formatTrend`:

```typescript
function formatSnapshotAge(isoDate: string | null) {
  if (!isoDate) return null;
  const diffHours = Math.floor((Date.now() - new Date(isoDate).getTime()) / 3_600_000);
  if (diffHours < 1) return '< 1 hod';
  if (diffHours < 24) return `${diffHours} hod`;
  return `${Math.floor(diffHours / 24)} dní`;
}
```

In `OmegaDashboardPage` JSX, after `{error && ...}` add:

```typescript
      {companies.length > 0 && (
        <div className="snapshot-age">
          Snapshot: {formatSnapshotAge(companies[0].snapshotAt)} starý
        </div>
      )}
```

- [ ] **Step 5.6: Commit**

```bash
git add src/KrosExtension.Web/ClientApp/src/main.tsx
git commit -m "feat: add clickable company rows, snapshot age, and detail navigation to Omega dashboard"
```

---

### Task 6: Frontend — OmegaCompanyDetailPage component and CSS

**Files:**
- Modify: `src/KrosExtension.Web/ClientApp/src/main.tsx`
- Modify: `src/KrosExtension.Web/ClientApp/src/styles.css`

- [ ] **Step 6.1: Add `OmegaCompanyDetailPage` component**

Add this component before the `scoreClass` function:

```typescript
function OmegaCompanyDetailPage({
  dataReadServerId,
  databaseName,
  onBack
}: {
  dataReadServerId: string;
  databaseName: string;
  onBack: () => void;
}) {
  const [detail, setDetail] = useState<OmegaCompanyDetail | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState('');

  async function load() {
    setIsLoading(true);
    setError('');

    try {
      const response = await fetch(
        `/api/omega/dashboard/companies/${dataReadServerId}/${encodeURIComponent(databaseName)}`
      );

      if (!response.ok) {
        setError(await response.text());
        return;
      }

      setDetail(await response.json());
    } catch (exception) {
      setError(exception instanceof Error ? exception.message : 'Načítanie detailu zlyhalo.');
    } finally {
      setIsLoading(false);
    }
  }

  useEffect(() => {
    void load();
  }, [dataReadServerId, databaseName]);

  return (
    <div className="page">
      <header>
        <div className="back-header">
          <button className="back-btn" onClick={onBack}>← Späť</button>
          {detail && (
            <h1>
              {detail.companyName}
              <span className="muted-label"> ({detail.databaseName})</span>
            </h1>
          )}
        </div>
        <button onClick={load} disabled={isLoading}>
          <RefreshCw size={16} /> {isLoading ? 'Načítavam' : 'Obnoviť'}
        </button>
      </header>

      {error && <div className="status error">{error}</div>}
      {isLoading && <div className="status">Načítavam detail...</div>}

      {detail && (
        <div className="detail-layout">

          <section className="detail-card">
            <h2>Health score</h2>
            <div className="health-score-hero">
              <span className={`score-pill large ${scoreClass(detail.healthScore)}`}>
                {detail.healthScore}
              </span>
            </div>
            <div className="score-breakdown">
              {([
                { label: 'Aktivita', value: detail.activityScore, weight: '20 %' },
                { label: 'Completion', value: detail.completionScore, weight: '30 %' },
                { label: 'Backlog aging', value: detail.backlogAgingScore, weight: '25 %' },
                { label: 'Month closing', value: detail.monthClosingScore, weight: '20 %' },
                { label: 'Data quality', value: detail.dataQualityScore, weight: '5 %' }
              ] as const).map(({ label, value, weight }) => (
                <div key={label} className="score-row">
                  <span className="score-label">{label} <span className="muted-label">{weight}</span></span>
                  <div className="score-bar-track">
                    <div className={`score-bar-fill ${scoreClass(value)}`} style={{ width: `${value}%` }} />
                  </div>
                  <span className="score-value">{value}</span>
                </div>
              ))}
            </div>
          </section>

          {detail.warningFlags.length > 0 && (
            <section className="detail-card">
              <h2>Varovania</h2>
              <div className="warning-list large">
                {detail.warningFlags.map((flag) => (
                  <span key={flag} className={`warning-pill ${warningSeverity(flag)}`}>
                    {warningLabel(flag)}
                  </span>
                ))}
              </div>
            </section>
          )}

          <section className="detail-card">
            <h2>Workflow funnel — posledných 30 dní</h2>
            <div className="funnel">
              {([
                { label: 'Zaevidované', value: detail.registered30Days },
                { label: 'Predkontované', value: detail.preAccounted30Days },
                { label: 'Zaúčtované', value: detail.posted30Days }
              ] as const).map(({ label, value }) => {
                const pct = detail.registered30Days === 0
                  ? 0
                  : Math.round(value * 100 / detail.registered30Days);
                return (
                  <div key={label} className="funnel-row">
                    <span className="funnel-label">{label}</span>
                    <div className="funnel-bar-track">
                      <div className="funnel-bar-fill" style={{ width: `${pct}%` }} />
                    </div>
                    <span className="funnel-value">
                      {value} <span className="muted-label">({pct} %)</span>
                    </span>
                  </div>
                );
              })}
            </div>
          </section>

          <section className="detail-card">
            <h2>Typ dokladov — posledných 30 dní</h2>
            <div className="type-breakdown">
              {([
                { label: 'Manuálne', value: detail.manual30Days },
                { label: 'Importované', value: detail.imported30Days },
                { label: 'Platforma / API', value: detail.platform30Days }
              ] as const).map(({ label, value }) => (
                <div key={label} className="type-row">
                  <span>{label}</span>
                  <strong>{value}</strong>
                </div>
              ))}
            </div>
          </section>

          <section className="detail-card">
            <h2>Backlog — nezaúčtované doklady</h2>
            <div className="aging-grid">
              {([
                { label: '0–3 dni', value: detail.backlog0to3Days, cls: 'watch' },
                { label: '4–7 dní', value: detail.backlog4to7Days, cls: 'bad' },
                { label: '8–30 dní', value: detail.backlog8to30Days, cls: 'bad' },
                { label: '30+ dní', value: detail.backlog30DaysPlus, cls: 'critical' }
              ] as const).map(({ label, value, cls }) => (
                <div key={label} className={`aging-bucket ${value > 0 ? cls : 'ok'}`}>
                  <span className="aging-count">{value}</span>
                  <span className="aging-label">{label}</span>
                </div>
              ))}
            </div>
          </section>

          <section className="detail-card full-width">
            <h2>Mesačné zaúčtovanie — posledných 12 mesiacov</h2>
            <div className="heatmap">
              {detail.monthlyBreakdown.map((m) => {
                const rate = m.postingRate;
                const cls = rate === null ? 'heatmap-empty'
                  : rate >= 98 ? 'heatmap-great'
                  : rate >= 95 ? 'heatmap-ok'
                  : rate >= 90 ? 'heatmap-warn'
                  : 'heatmap-bad';
                const label = new Intl.DateTimeFormat('sk-SK', { month: 'short', year: '2-digit' })
                  .format(new Date(m.year, m.month - 1, 1));
                return (
                  <div
                    key={`${m.year}-${m.month}`}
                    className={`heatmap-cell ${cls}`}
                    title={`${label}: ${rate !== null ? rate.toFixed(1) + ' %' : 'bez dát'}`}
                  >
                    <span className="heatmap-month">{label}</span>
                    <span className="heatmap-rate">{rate !== null ? `${rate.toFixed(0)} %` : '–'}</span>
                    <span className="heatmap-docs">{m.registered} / {m.posted}</span>
                  </div>
                );
              })}
            </div>
            <div className="heatmap-legend">
              <span className="heatmap-great">≥ 98 %</span>
              <span className="heatmap-ok">95–97 %</span>
              <span className="heatmap-warn">90–94 %</span>
              <span className="heatmap-bad">&lt; 90 %</span>
              <span className="heatmap-empty">bez dát</span>
            </div>
          </section>

        </div>
      )}
    </div>
  );
}
```

- [ ] **Step 6.2: Add CSS for detail page**

Append to `src/KrosExtension.Web/ClientApp/src/styles.css`:

```css
/* ---- Company link button in table ---- */
.company-link {
  background: none;
  border: none;
  padding: 0;
  cursor: pointer;
  text-align: left;
  display: flex;
  flex-direction: column;
  gap: 2px;
  color: inherit;
}
.company-link:hover strong {
  text-decoration: underline;
}

/* ---- Snapshot age ---- */
.snapshot-age {
  font-size: 0.78rem;
  color: #888;
  margin-bottom: 0.5rem;
}

/* ---- Detail page layout ---- */
.back-header {
  display: flex;
  align-items: center;
  gap: 1rem;
}
.back-btn {
  font-size: 0.9rem;
  white-space: nowrap;
}
.muted-label {
  color: #888;
  font-weight: 400;
}
.detail-layout {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 1rem;
  margin-top: 1rem;
}
.detail-card {
  background: #1a1a2e;
  border: 1px solid #2d2d4e;
  border-radius: 8px;
  padding: 1rem;
}
.detail-card h2 {
  font-size: 0.82rem;
  text-transform: uppercase;
  letter-spacing: 0.06em;
  color: #888;
  margin: 0 0 0.75rem;
}
.detail-card.full-width {
  grid-column: 1 / -1;
}

/* ---- Health score breakdown ---- */
.health-score-hero {
  text-align: center;
  margin-bottom: 0.75rem;
}
.score-pill.large {
  font-size: 2rem;
  padding: 0.4rem 1.2rem;
  border-radius: 10px;
}
.score-breakdown {
  display: flex;
  flex-direction: column;
  gap: 0.45rem;
}
.score-row {
  display: grid;
  grid-template-columns: 150px 1fr 36px;
  align-items: center;
  gap: 0.5rem;
}
.score-label {
  font-size: 0.82rem;
}
.score-bar-track {
  height: 8px;
  background: #2d2d4e;
  border-radius: 4px;
  overflow: hidden;
}
.score-bar-fill {
  height: 100%;
  border-radius: 4px;
  transition: width 0.3s ease;
}
.score-bar-fill.good     { background: #10b981; }
.score-bar-fill.watch    { background: #f59e0b; }
.score-bar-fill.bad      { background: #f97316; }
.score-bar-fill.critical { background: #ef4444; }
.score-value {
  font-size: 0.82rem;
  text-align: right;
}

/* ---- Warning list in detail card ---- */
.warning-list.large .warning-pill {
  font-size: 0.85rem;
  padding: 0.3rem 0.65rem;
}

/* ---- Workflow funnel ---- */
.funnel {
  display: flex;
  flex-direction: column;
  gap: 0.65rem;
}
.funnel-row {
  display: grid;
  grid-template-columns: 130px 1fr 110px;
  align-items: center;
  gap: 0.5rem;
}
.funnel-label {
  font-size: 0.85rem;
}
.funnel-bar-track {
  height: 14px;
  background: #2d2d4e;
  border-radius: 4px;
  overflow: hidden;
}
.funnel-bar-fill {
  height: 100%;
  background: #6366f1;
  border-radius: 4px;
  transition: width 0.3s ease;
}
.funnel-value {
  font-size: 0.82rem;
  text-align: right;
}

/* ---- Type breakdown ---- */
.type-breakdown {
  display: flex;
  flex-direction: column;
  gap: 0.4rem;
}
.type-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  font-size: 0.88rem;
  padding: 0.35rem 0;
  border-bottom: 1px solid #2d2d4e;
}
.type-row:last-child {
  border-bottom: none;
}

/* ---- Backlog aging buckets ---- */
.aging-grid {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 0.5rem;
}
.aging-bucket {
  text-align: center;
  padding: 0.75rem 0.5rem;
  border-radius: 6px;
  background: #2d2d4e;
  border: 1px solid transparent;
}
.aging-bucket.ok       { border-color: #10b981; }
.aging-bucket.watch    { border-color: #f59e0b; }
.aging-bucket.bad      { border-color: #f97316; }
.aging-bucket.critical { border-color: #ef4444; background: rgba(239, 68, 68, 0.08); }
.aging-count {
  display: block;
  font-size: 1.6rem;
  font-weight: 700;
  line-height: 1;
}
.aging-label {
  display: block;
  font-size: 0.72rem;
  color: #aaa;
  margin-top: 0.3rem;
}

/* ---- Monthly heatmap ---- */
.heatmap {
  display: flex;
  flex-wrap: wrap;
  gap: 0.4rem;
}
.heatmap-cell {
  width: 88px;
  padding: 0.45rem 0.5rem;
  border-radius: 6px;
  text-align: center;
  border: 1px solid transparent;
  cursor: default;
}
.heatmap-month { display: block; font-size: 0.72rem; color: #aaa; }
.heatmap-rate  { display: block; font-size: 1.05rem; font-weight: 700; margin: 0.1rem 0; }
.heatmap-docs  { display: block; font-size: 0.68rem; color: #888; }
.heatmap-great { background: rgba(16, 185, 129, 0.15); border-color: #10b981; }
.heatmap-ok    { background: rgba(245, 158, 11, 0.12); border-color: #f59e0b; }
.heatmap-warn  { background: rgba(249, 115, 22, 0.12); border-color: #f97316; }
.heatmap-bad   { background: rgba(239, 68, 68, 0.15);  border-color: #ef4444; }
.heatmap-empty { background: #2d2d4e; border-color: #3d3d5e; }
.heatmap-legend {
  display: flex;
  gap: 0.75rem;
  margin-top: 0.75rem;
  font-size: 0.74rem;
}
.heatmap-legend span {
  padding: 0.12rem 0.4rem;
  border-radius: 3px;
}
.heatmap-legend .heatmap-great { background: rgba(16, 185, 129, 0.15); }
.heatmap-legend .heatmap-ok    { background: rgba(245, 158, 11, 0.12); }
.heatmap-legend .heatmap-warn  { background: rgba(249, 115, 22, 0.12); }
.heatmap-legend .heatmap-bad   { background: rgba(239, 68, 68, 0.15); }
.heatmap-legend .heatmap-empty { background: #2d2d4e; }
```

- [ ] **Step 6.3: Build frontend**

```powershell
cd .\src\KrosExtension.Web\ClientApp
npm run build
```

Expected: zero TypeScript errors, `dist/` updated successfully.

- [ ] **Step 6.4: Commit**

```bash
git add src/KrosExtension.Web/ClientApp/src/main.tsx
git add src/KrosExtension.Web/ClientApp/src/styles.css
git commit -m "feat: add Omega company detail page with funnel, aging buckets, monthly heatmap and score breakdown"
```

---

### Task 7: End-to-end verification

- [ ] **Step 7.1: Run full solution**

Terminal 1:
```powershell
dotnet run --project .\src\KrosExtension.Api\KrosExtension.Api.csproj
```

Terminal 2:
```powershell
cd .\src\KrosExtension.Web
dotnet run --launch-profile http
```

Open browser at the Web port (check `launchSettings.json` if unsure).

- [ ] **Step 7.2: Run UpdateData and verify global dashboard**

In browser → Omega → UpdateData button.

Check:
- [ ] Table shows companies ordered worst health score first
- [ ] Health score pills are colour-coded (red < 50, orange 50–69, yellow 70–84, green ≥ 85)
- [ ] Warning badges use new flag codes; no `DATA_QUALITY` appears
- [ ] Snapshot age badge shows approximate time since last update

- [ ] **Step 7.3: Verify company detail drill-down**

Click any company name in the global table.

Check:
- [ ] Detail page loads with company name in header
- [ ] Health score hero shows large pill + 5 breakdown bars with correct colours
- [ ] Workflow funnel shows Zaevidované / Predkontované / Zaúčtované with percentages
- [ ] Type breakdown shows Manuálne / Importované / Platforma values
- [ ] Backlog aging shows 4 buckets with colour-coded borders
- [ ] Monthly heatmap shows last 12 months coloured by posting rate
- [ ] "Späť" button returns to global table with no network call

- [ ] **Step 7.4: Final clean build**

```powershell
dotnet build .\KrosExtension.sln
cd .\src\KrosExtension.Web\ClientApp; npm run build
```

Both: zero errors, zero warnings.
