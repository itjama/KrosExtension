# Olymp Monitoring Dashboard — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a snapshot-based Olymp monitoring dashboard parallel to the existing Omega pipeline — global risk table for 30–50 companies, health score, warning flags, and a live company-detail drill-down.

**Architecture:** Same pattern as Omega: UpdateData writes `OlympCompanyDashboardSnapshot` rows to `KrosExtension` DB; global dashboard reads latest snapshot per company (fast); company detail queries Olymp DB live on demand. No schema changes to Olymp databases — read-only.

**Tech Stack:** .NET 10 / C# / EF Core / Minimal API / SQL Server; React 19 / TypeScript / Vite (single `main.tsx`)

---

## Reference: Health Score Algorithm

| Component | Weight | Scoring rule |
|-----------|--------|-------------|
| ActivityScore | 25 % | Days since last `StartTime` in `activityLog`: ≤7d→100, 8–14d→80, 15–30d→60, 31–60d→30, 61–90d→15, >90d→0. −15 if any NullStartTime rows exist |
| PayrollCurrentnessScore | 35 % | Months behind current period: 0→100, 0.5 (early in month)→85, 1→50, 2→20, ≥3→0 |
| PayrollConsistencyScore | 25 % | % of processed periods that have records in ALL three tables (pracVycMzda + pracVycBase + pracVycOdvod). No periods→70 (neutral) |
| PeriodRegularityScore | 15 % | Start from 100; −20 per gap month in the expected Jan→last-processed sequence (min 0) |

`HealthScore = round(Activity×0.25 + Currentness×0.35 + Consistency×0.25 + Regularity×0.15)` clamped 0–100

**Colour thresholds:** ≥85→green; 70–84→yellow; 50–69→orange; <50→red

## Reference: Warning System

| Flag | Severity | Trigger |
|------|----------|---------|
| `CURRENT_MONTH_MISSING` | critical | Current month not processed AND today ≥ 15th |
| `PAYROLL_MONTHS_BEHIND` | critical | Last processed period ≥ 2 months behind current |
| `INACTIVE_30D` | critical | No activityLog entry in 30+ days |
| `INACTIVE_14D` | bad | No activityLog entry in 14+ days |
| `MISSING_PERIODS` | bad | ≥1 gap in the expected month sequence |
| `PAYROLL_INCONSISTENCY` | bad | ≥1 period missing mzda/base/odvod |
| `NULL_STARTTIME` | bad | activityLog entries where StartTime IS NULL |
| `LOW_ACTIVITY` | watch | Activity7Days < 30 % of Activity30Days÷4 (sharp weekly drop) |

---

## Reference: What Needs Schema Verification (Task 1)

The following is UNKNOWN and must be confirmed before Task 4 SQL is written:
- Exact format of `ID_obdobie` (is it `202601`? a FK to a periods table? a date?)
- Whether `pracVycMzda`/`pracVycBase`/`pracVycOdvod` store one row per period or many rows per period
- Any date/timestamp columns in `pracVycMzda` besides `ID_obdobie`
- Whether `activityLog` has additional filterable columns (e.g. action type, module)

---

## File Map

| File | Action | Purpose |
|------|--------|---------|
| `src/KrosExtension.Domain/Configuration/OlympCompanyDashboardSnapshot.cs` | **Create** | Snapshot entity |
| `src/KrosExtension.Infrastructure/Persistence/Configurations/OlympCompanyDashboardSnapshotConfiguration.cs` | **Create** | EF column config |
| `src/KrosExtension.Infrastructure/Persistence/ApplicationDbContext.cs` | Modify | Add DbSet |
| `src/KrosExtension.Application/Companies/OlympDashboardCompany.cs` | **Create** | Global view DTO |
| `src/KrosExtension.Application/Companies/OlympMonthlyPeriod.cs` | **Create** | Period detail DTO |
| `src/KrosExtension.Application/Companies/OlympCompanyDetail.cs` | **Create** | Detail view DTO |
| `src/KrosExtension.Application/Companies/IOlympDashboardReader.cs` | **Create** | Global reader interface |
| `src/KrosExtension.Application/Companies/IOlympCompanyDetailReader.cs` | **Create** | Detail reader interface |
| `src/KrosExtension.Infrastructure/Companies/OlympDashboardSnapshotCalculator.cs` | **Create** | SQL + scoring (needs Task 1 output) |
| `src/KrosExtension.Infrastructure/Companies/OlympDashboardReader.cs` | **Create** | Reads snapshots from KrosExtension DB |
| `src/KrosExtension.Infrastructure/Companies/OlympCompanyDetailReader.cs` | **Create** | Live queries against Olymp DB |
| `src/KrosExtension.Infrastructure/Companies/CompanyDataRefreshService.cs` | Modify | Add Olymp snapshot branch |
| `src/KrosExtension.Infrastructure/DependencyInjection.cs` | Modify | Register new services |
| `src/KrosExtension.Api/Endpoints/ProductCompanyEndpoints.cs` | Modify | Add Olymp dashboard endpoints |
| `src/KrosExtension.Infrastructure/Persistence/Migrations/` | **Create** | EF migration |
| `src/KrosExtension.Web/ClientApp/src/main.tsx` | Modify | OlympDashboardPage + OlympCompanyDetailPage |
| `src/KrosExtension.Web/ClientApp/src/styles.css` | Modify | Period heatmap CSS if needed |

---

### Task 1: Schema verification — run against `mzdyCanberra_Packar`

**This task must complete before Task 4 can start.** Run each query in SSMS against `(localdb)\MSSQLLocalDB` database `mzdyCanberra_Packar`. Paste results into the plan as comments.

- [ ] **Step 1.1: Inspect activityLog columns**

```sql
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'activityLog'
ORDER BY ORDINAL_POSITION
```

- [ ] **Step 1.2: Inspect pracVycMzda, pracVycBase, pracVycOdvod columns**

```sql
SELECT TABLE_NAME, COLUMN_NAME, DATA_TYPE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME IN ('pracVycMzda', 'pracVycBase', 'pracVycOdvod')
ORDER BY TABLE_NAME, ORDINAL_POSITION
```

- [ ] **Step 1.3: Inspect ID_obdobie table or understand its format**

```sql
-- Option A: if ID_obdobie is a FK to a periods table, find that table
SELECT TABLE_NAME, COLUMN_NAME, DATA_TYPE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE COLUMN_NAME LIKE '%obdobie%' OR TABLE_NAME LIKE '%obdobie%'
ORDER BY TABLE_NAME, ORDINAL_POSITION
```

```sql
-- Option B: sample values from pracVycMzda to understand ID_obdobie format
SELECT TOP 20 ID_obdobie FROM dbo.pracVycMzda ORDER BY ID_obdobie DESC
```

- [ ] **Step 1.4: Sample activityLog to understand structure**

```sql
SELECT TOP 5 * FROM dbo.activityLog ORDER BY StartTime DESC
```

- [ ] **Step 1.5: Update Task 4 SQL based on findings**

After reviewing results: fill in the exact column names and join conditions in Task 4 before dispatching the Task 4 implementer.

---

### Task 2: Create Domain entity and EF configuration

**Files:**
- Create: `src/KrosExtension.Domain/Configuration/OlympCompanyDashboardSnapshot.cs`
- Create: `src/KrosExtension.Infrastructure/Persistence/Configurations/OlympCompanyDashboardSnapshotConfiguration.cs`
- Modify: `src/KrosExtension.Infrastructure/Persistence/ApplicationDbContext.cs`

- [ ] **Step 2.1: Create `OlympCompanyDashboardSnapshot.cs`**

Follow the same pattern as `OmegaCompanyDashboardSnapshot.cs` (private constructor, all-params public constructor, private-set properties).

```csharp
using KrosExtension.Domain.Common;

namespace KrosExtension.Domain.Configuration;

public sealed class OlympCompanyDashboardSnapshot : Entity
{
    private OlympCompanyDashboardSnapshot()
    {
    }

    public OlympCompanyDashboardSnapshot(
        Guid dataReadServerId,
        string serverDisplayName,
        string databaseName,
        string companyName,
        int? accountingYear,
        DateTimeOffset snapshotAt,
        int healthScore,
        int activityScore,
        int payrollCurrentnessScore,
        int payrollConsistencyScore,
        int periodRegularityScore,
        DateTime? lastActivityDate,
        string? lastProcessedPeriod,
        bool currentMonthProcessed,
        int processedPeriodsInYear,
        long activity7Days,
        long activity30Days,
        long nullStartTimeCount,
        int missingPeriodsCount,
        int inconsistentPeriodsCount,
        decimal? trendPercent,
        string warningFlags)
    {
        DataReadServerId = dataReadServerId;
        ServerDisplayName = serverDisplayName;
        DatabaseName = databaseName;
        CompanyName = companyName;
        AccountingYear = accountingYear;
        SnapshotAt = snapshotAt;
        HealthScore = healthScore;
        ActivityScore = activityScore;
        PayrollCurrentnessScore = payrollCurrentnessScore;
        PayrollConsistencyScore = payrollConsistencyScore;
        PeriodRegularityScore = periodRegularityScore;
        LastActivityDate = lastActivityDate;
        LastProcessedPeriod = lastProcessedPeriod;
        CurrentMonthProcessed = currentMonthProcessed;
        ProcessedPeriodsInYear = processedPeriodsInYear;
        Activity7Days = activity7Days;
        Activity30Days = activity30Days;
        NullStartTimeCount = nullStartTimeCount;
        MissingPeriodsCount = missingPeriodsCount;
        InconsistentPeriodsCount = inconsistentPeriodsCount;
        TrendPercent = trendPercent;
        WarningFlags = warningFlags;
    }

    public Guid DataReadServerId { get; private set; }
    public string ServerDisplayName { get; private set; } = string.Empty;
    public string DatabaseName { get; private set; } = string.Empty;
    public string CompanyName { get; private set; } = string.Empty;
    public int? AccountingYear { get; private set; }
    public DateTimeOffset SnapshotAt { get; private set; }
    public int HealthScore { get; private set; }
    public int ActivityScore { get; private set; }
    public int PayrollCurrentnessScore { get; private set; }
    public int PayrollConsistencyScore { get; private set; }
    public int PeriodRegularityScore { get; private set; }
    public DateTime? LastActivityDate { get; private set; }
    public string? LastProcessedPeriod { get; private set; }
    public bool CurrentMonthProcessed { get; private set; }
    public int ProcessedPeriodsInYear { get; private set; }
    public long Activity7Days { get; private set; }
    public long Activity30Days { get; private set; }
    public long NullStartTimeCount { get; private set; }
    public int MissingPeriodsCount { get; private set; }
    public int InconsistentPeriodsCount { get; private set; }
    public decimal? TrendPercent { get; private set; }
    public string WarningFlags { get; private set; } = string.Empty;
}
```

- [ ] **Step 2.2: Create `OlympCompanyDashboardSnapshotConfiguration.cs`**

```csharp
using KrosExtension.Domain.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KrosExtension.Infrastructure.Persistence.Configurations;

public sealed class OlympCompanyDashboardSnapshotConfiguration
    : IEntityTypeConfiguration<OlympCompanyDashboardSnapshot>
{
    public void Configure(EntityTypeBuilder<OlympCompanyDashboardSnapshot> builder)
    {
        builder.ToTable("OlympCompanyDashboardSnapshots");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.ServerDisplayName).HasMaxLength(256).IsRequired();
        builder.Property(s => s.DatabaseName).HasMaxLength(128).IsRequired();
        builder.Property(s => s.CompanyName).HasMaxLength(256).IsRequired();
        builder.Property(s => s.LastProcessedPeriod).HasMaxLength(16);
        builder.Property(s => s.TrendPercent).HasPrecision(8, 2);
        builder.Property(s => s.WarningFlags).HasMaxLength(1024).IsRequired();

        builder.HasIndex(s => new { s.DataReadServerId, s.DatabaseName, s.SnapshotAt });
        builder.HasIndex(s => s.HealthScore);
    }
}
```

- [ ] **Step 2.3: Add DbSet to `ApplicationDbContext.cs`**

Read the file. Add after the existing `OmegaCompanyDashboardSnapshots` DbSet:

```csharp
    public DbSet<OlympCompanyDashboardSnapshot> OlympCompanyDashboardSnapshots { get; set; }
```

- [ ] **Step 2.4: Build verify**

```powershell
dotnet build "src\KrosExtension.Infrastructure\KrosExtension.Infrastructure.csproj"
```

Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 2.5: Commit**

```bash
git add src/KrosExtension.Domain/Configuration/OlympCompanyDashboardSnapshot.cs
git add src/KrosExtension.Infrastructure/Persistence/Configurations/OlympCompanyDashboardSnapshotConfiguration.cs
git add src/KrosExtension.Infrastructure/Persistence/ApplicationDbContext.cs
git commit -m "feat: add OlympCompanyDashboardSnapshot entity and EF configuration"
```

---

### Task 3: Create Application-layer DTOs and interfaces

**Files:**
- Create: `src/KrosExtension.Application/Companies/OlympDashboardCompany.cs`
- Create: `src/KrosExtension.Application/Companies/OlympMonthlyPeriod.cs`
- Create: `src/KrosExtension.Application/Companies/OlympCompanyDetail.cs`
- Create: `src/KrosExtension.Application/Companies/IOlympDashboardReader.cs`
- Create: `src/KrosExtension.Application/Companies/IOlympCompanyDetailReader.cs`

- [ ] **Step 3.1: Create `OlympDashboardCompany.cs`**

```csharp
namespace KrosExtension.Application.Companies;

public sealed record OlympDashboardCompany(
    Guid DataReadServerId,
    string ServerDisplayName,
    string DatabaseName,
    string CompanyName,
    int? AccountingYear,
    DateTimeOffset SnapshotAt,
    int HealthScore,
    int ActivityScore,
    int PayrollCurrentnessScore,
    int PayrollConsistencyScore,
    int PeriodRegularityScore,
    DateTime? LastActivityDate,
    string? LastProcessedPeriod,
    bool CurrentMonthProcessed,
    int ProcessedPeriodsInYear,
    long Activity7Days,
    long Activity30Days,
    decimal? TrendPercent,
    IReadOnlyList<string> WarningFlags);
```

- [ ] **Step 3.2: Create `OlympMonthlyPeriod.cs`**

```csharp
namespace KrosExtension.Application.Companies;

public sealed record OlympMonthlyPeriod(
    string PeriodId,
    bool HasMzda,
    bool HasBase,
    bool HasOdvod);
```

- [ ] **Step 3.3: Create `OlympCompanyDetail.cs`**

```csharp
namespace KrosExtension.Application.Companies;

public sealed record OlympCompanyDetail(
    Guid DataReadServerId,
    string ServerDisplayName,
    string DatabaseName,
    string CompanyName,
    int? AccountingYear,
    DateTimeOffset SnapshotAt,
    int HealthScore,
    int ActivityScore,
    int PayrollCurrentnessScore,
    int PayrollConsistencyScore,
    int PeriodRegularityScore,
    DateTime? LastActivityDate,
    string? LastProcessedPeriod,
    bool CurrentMonthProcessed,
    int ProcessedPeriodsInYear,
    long Activity7Days,
    long Activity30Days,
    long NullStartTimeCount,
    int MissingPeriodsCount,
    int InconsistentPeriodsCount,
    IReadOnlyList<OlympMonthlyPeriod> Periods,
    IReadOnlyList<string> WarningFlags);
```

- [ ] **Step 3.4: Create `IOlympDashboardReader.cs`**

```csharp
namespace KrosExtension.Application.Companies;

public interface IOlympDashboardReader
{
    Task<IReadOnlyList<OlympDashboardCompany>> GetCompaniesAsync(
        CancellationToken cancellationToken = default);
}
```

- [ ] **Step 3.5: Create `IOlympCompanyDetailReader.cs`**

```csharp
namespace KrosExtension.Application.Companies;

public interface IOlympCompanyDetailReader
{
    Task<OlympCompanyDetail?> GetCompanyDetailAsync(
        Guid dataReadServerId,
        string databaseName,
        CancellationToken cancellationToken = default);
}
```

- [ ] **Step 3.6: Build verify**

```powershell
dotnet build "src\KrosExtension.Application\KrosExtension.Application.csproj"
```

Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 3.7: Commit**

```bash
git add src/KrosExtension.Application/Companies/OlympDashboardCompany.cs
git add src/KrosExtension.Application/Companies/OlympMonthlyPeriod.cs
git add src/KrosExtension.Application/Companies/OlympCompanyDetail.cs
git add src/KrosExtension.Application/Companies/IOlympDashboardReader.cs
git add src/KrosExtension.Application/Companies/IOlympCompanyDetailReader.cs
git commit -m "feat: add Olymp dashboard DTOs and interfaces"
```

---

### Task 4: Implement OlympDashboardSnapshotCalculator

**⚠ PREREQUISITE: Task 1 must be complete. Fill in schema-specific parts below before dispatching this task.**

**File:**
- Create: `src/KrosExtension.Infrastructure/Companies/OlympDashboardSnapshotCalculator.cs`

This is the most complex task. The SQL must be adapted based on Task 1 findings. The scoring algorithm is fully specified in the Reference section above.

The calculator runs two SQL queries against the Olymp company database:
1. **ActivitySql** — reads from `activityLog`
2. **PayrollSql** — reads from `pracVycMzda`, `pracVycBase`, `pracVycOdvod` with `ID_obdobie`

The `SnapshotSql` string below is a template. Lines marked `-- FILL FROM TASK 1` must be completed after schema verification.

- [ ] **Step 4.1: Create `OlympDashboardSnapshotCalculator.cs`**

```csharp
using KrosExtension.Domain.Configuration;
using KrosExtension.Infrastructure.Configuration;
using Microsoft.Data.SqlClient;
using System.Data;

namespace KrosExtension.Infrastructure.Companies;

internal static class OlympDashboardSnapshotCalculator
{
    public static async Task<OlympCompanyDashboardSnapshot?> ReadSnapshotAsync(
        DataReadServer server,
        TrackedCompany company,
        DateTime today,
        CancellationToken cancellationToken)
    {
        var connectionString = DataReadServerConnectionStringFactory.CreateDatabaseConnectionString(
            server, ProductType.Olymp, company.DatabaseName);

        if (connectionString is null)
        {
            return null;
        }

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var activity = await ReadActivityAsync(connection, today, cancellationToken);
        var payroll = await ReadPayrollAsync(connection, today, cancellationToken);

        var scores = CalculateScores(activity, payroll, today);
        var warnings = BuildWarnings(activity, payroll, today);
        var trendPercent = CalculateTrendPercent(activity.Activity7Days, activity.Previous7DaysActivity);

        return new OlympCompanyDashboardSnapshot(
            company.DataReadServerId,
            server.DisplayName,
            company.DatabaseName,
            company.CompanyName,
            company.AccountingYear,
            DateTimeOffset.UtcNow,
            scores.HealthScore,
            scores.ActivityScore,
            scores.PayrollCurrentnessScore,
            scores.PayrollConsistencyScore,
            scores.PeriodRegularityScore,
            activity.LastActivityDate,
            payroll.LastProcessedPeriod,
            payroll.CurrentMonthProcessed,
            payroll.ProcessedPeriodsInYear,
            activity.Activity7Days,
            activity.Activity30Days,
            activity.NullStartTimeCount,
            payroll.MissingPeriodsCount,
            payroll.InconsistentPeriodsCount,
            trendPercent,
            string.Join(',', warnings));
    }

    private static OlympDashboardScores CalculateScores(
        ActivityMetrics activity,
        PayrollMetrics payroll,
        DateTime today)
    {
        // ActivityScore
        var activityScore = activity.DaysSinceLastActivity switch
        {
            null => 0,
            <= 7 => 100,
            <= 14 => 80,
            <= 30 => 60,
            <= 60 => 30,
            <= 90 => 15,
            _ => 0
        };

        if (activity.NullStartTimeCount > 0)
        {
            activityScore = Math.Max(0, activityScore - 15);
        }

        // PayrollCurrentnessScore
        var monthsBehind = payroll.MonthsBehindCurrent;
        var payrollCurrentnessScore = monthsBehind switch
        {
            0 => 100,
            <= 1 when today.Day < 15 => 85,
            <= 1 => 50,
            <= 2 => 20,
            _ => 0
        };

        // PayrollConsistencyScore
        var payrollConsistencyScore = payroll.TotalPeriods == 0
            ? 70
            : ClampScore((int)Math.Round(payroll.ConsistentPeriods * 100m / payroll.TotalPeriods));

        // PeriodRegularityScore
        var periodRegularityScore = ClampScore(100 - payroll.MissingPeriodsCount * 20);

        activityScore = ClampScore(activityScore);
        payrollCurrentnessScore = ClampScore(payrollCurrentnessScore);

        var healthScore = ClampScore((int)Math.Round(
            activityScore * 0.25m +
            payrollCurrentnessScore * 0.35m +
            payrollConsistencyScore * 0.25m +
            periodRegularityScore * 0.15m));

        return new OlympDashboardScores(
            healthScore,
            activityScore,
            payrollCurrentnessScore,
            payrollConsistencyScore,
            periodRegularityScore);
    }

    private static IReadOnlyList<string> BuildWarnings(
        ActivityMetrics activity,
        PayrollMetrics payroll,
        DateTime today)
    {
        var warnings = new List<string>();

        if (payroll.CurrentMonthProcessed == false && today.Day >= 15)
            warnings.Add("CURRENT_MONTH_MISSING");

        if (payroll.MonthsBehindCurrent >= 2)
            warnings.Add("PAYROLL_MONTHS_BEHIND");

        if (activity.DaysSinceLastActivity is null or >= 30)
            warnings.Add("INACTIVE_30D");
        else if (activity.DaysSinceLastActivity >= 14)
            warnings.Add("INACTIVE_14D");

        if (payroll.MissingPeriodsCount > 0)
            warnings.Add("MISSING_PERIODS");

        if (payroll.InconsistentPeriodsCount > 0)
            warnings.Add("PAYROLL_INCONSISTENCY");

        if (activity.NullStartTimeCount > 0)
            warnings.Add("NULL_STARTTIME");

        var expectedWeeklyAvg = activity.Activity30Days / 4m;
        if (expectedWeeklyAvg > 0 && activity.Activity7Days < expectedWeeklyAvg * 0.30m)
            warnings.Add("LOW_ACTIVITY");

        return warnings;
    }

    private static async Task<ActivityMetrics> ReadActivityAsync(
        SqlConnection connection,
        DateTime today,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = ActivitySql;
        command.Parameters.Add(new SqlParameter("@Today", SqlDbType.Date) { Value = today.Date });
        command.Parameters.Add(new SqlParameter("@From7", SqlDbType.Date) { Value = today.AddDays(-6).Date });
        command.Parameters.Add(new SqlParameter("@From30", SqlDbType.Date) { Value = today.AddDays(-29).Date });
        command.Parameters.Add(new SqlParameter("@Prev7From", SqlDbType.Date) { Value = today.AddDays(-13).Date });
        command.Parameters.Add(new SqlParameter("@Prev7To", SqlDbType.Date) { Value = today.AddDays(-7).Date });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            return new ActivityMetrics(null, 0, 0, 0, 0);
        }

        var lastActivityDate = GetNullableDateTime(reader, "LastActivityDate");
        var daysSince = lastActivityDate.HasValue
            ? (int)(today.Date - lastActivityDate.Value.Date).TotalDays
            : (int?)null;

        return new ActivityMetrics(
            daysSince,
            GetInt64(reader, "Activity7Days"),
            GetInt64(reader, "Activity30Days"),
            GetInt64(reader, "Previous7DaysActivity"),
            GetInt64(reader, "NullStartTimeCount"));
    }

    private static async Task<PayrollMetrics> ReadPayrollAsync(
        SqlConnection connection,
        DateTime today,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = PayrollSql;
        command.Parameters.Add(new SqlParameter("@Today", SqlDbType.Date) { Value = today.Date });

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        // First result set: summary row
        if (!await reader.ReadAsync(cancellationToken))
        {
            return new PayrollMetrics(null, false, 0, 0, 0, 0, 0);
        }

        var lastProcessedPeriod = GetNullableString(reader, "LastProcessedPeriod");
        var currentMonthProcessed = GetBoolean(reader, "CurrentMonthProcessed");
        var processedPeriodsInYear = (int)GetInt64(reader, "ProcessedPeriodsInYear");
        var totalPeriods = (int)GetInt64(reader, "TotalPeriods");
        var consistentPeriods = (int)GetInt64(reader, "ConsistentPeriods");
        var missingPeriodsCount = (int)GetInt64(reader, "MissingPeriodsCount");

        var monthsBehind = ComputeMonthsBehind(lastProcessedPeriod, today);

        return new PayrollMetrics(
            lastProcessedPeriod,
            currentMonthProcessed,
            processedPeriodsInYear,
            monthsBehind,
            totalPeriods,
            consistentPeriods,
            missingPeriodsCount);
    }

    // ── FILL FROM TASK 1 ────────────────────────────────────────────────────────
    // Replace CAST(StartTime AS date) with the actual date expression for
    // activityLog if StartTime is stored differently (e.g. varchar, smalldatetime).
    // Replace ID_obdobie references with actual column/join if it is a FK.
    private const string ActivitySql = """
        SELECT
            COUNT(CASE WHEN CAST(StartTime AS date) BETWEEN @From7 AND @Today THEN 1 END) AS Activity7Days,
            COUNT(CASE WHEN CAST(StartTime AS date) BETWEEN @From30 AND @Today THEN 1 END) AS Activity30Days,
            COUNT(CASE WHEN CAST(StartTime AS date) BETWEEN @Prev7From AND @Prev7To THEN 1 END) AS Previous7DaysActivity,
            COUNT(CASE WHEN StartTime IS NULL THEN 1 END) AS NullStartTimeCount,
            MAX(CAST(StartTime AS date)) AS LastActivityDate
        FROM dbo.activityLog
        """;

    // ── FILL FROM TASK 1 ────────────────────────────────────────────────────────
    // This SQL assumes ID_obdobie is stored as an integer in YYYYMM format
    // (e.g. 202601 = January 2026). Adjust if format differs.
    // Adjust table/column names based on Task 1 findings.
    private const string PayrollSql = """
        DECLARE @CurrentPeriod int = YEAR(@Today) * 100 + MONTH(@Today);
        DECLARE @YearStart   int = YEAR(@Today) * 100 + 1;

        WITH all_periods AS (
            SELECT DISTINCT ID_obdobie FROM dbo.pracVycMzda
            UNION
            SELECT DISTINCT ID_obdobie FROM dbo.pracVycBase
            UNION
            SELECT DISTINCT ID_obdobie FROM dbo.pracVycOdvod
        ),
        period_consistency AS (
            SELECT
                p.ID_obdobie,
                CASE WHEN m.ID_obdobie IS NOT NULL THEN 1 ELSE 0 END AS HasMzda,
                CASE WHEN b.ID_obdobie IS NOT NULL THEN 1 ELSE 0 END AS HasBase,
                CASE WHEN o.ID_obdobie IS NOT NULL THEN 1 ELSE 0 END AS HasOdvod
            FROM all_periods p
            LEFT JOIN (SELECT DISTINCT ID_obdobie FROM dbo.pracVycMzda) m ON m.ID_obdobie = p.ID_obdobie
            LEFT JOIN (SELECT DISTINCT ID_obdobie FROM dbo.pracVycBase) b ON b.ID_obdobie = p.ID_obdobie
            LEFT JOIN (SELECT DISTINCT ID_obdobie FROM dbo.pracVycOdvod) o ON o.ID_obdobie = p.ID_obdobie
        ),
        year_periods AS (
            SELECT * FROM period_consistency
            WHERE ID_obdobie >= @YearStart AND ID_obdobie <= @CurrentPeriod
        )
        SELECT
            CAST(MAX(p.ID_obdobie) AS varchar(10)) AS LastProcessedPeriod,
            CAST(CASE WHEN MAX(p.ID_obdobie) >= @CurrentPeriod THEN 1 ELSE 0 END AS bit) AS CurrentMonthProcessed,
            COUNT(*) AS ProcessedPeriodsInYear,
            COUNT(*) AS TotalPeriods,
            SUM(CASE WHEN HasMzda = 1 AND HasBase = 1 AND HasOdvod = 1 THEN 1 ELSE 0 END) AS ConsistentPeriods,
            -- Missing periods: expected months Jan..LastProcessed minus actual count
            CASE
                WHEN MAX(p.ID_obdobie) IS NULL THEN 0
                ELSE (
                    (MAX(p.ID_obdobie) % 100) - 1  -- expected months (0-based)
                    - (COUNT(*) - 1)                 -- actual gaps
                )
            END AS MissingPeriodsCount
        FROM year_periods p
        """;
    // ── END FILL FROM TASK 1 ────────────────────────────────────────────────────

    private static int ComputeMonthsBehind(string? lastProcessedPeriod, DateTime today)
    {
        if (lastProcessedPeriod is null || !int.TryParse(lastProcessedPeriod, out var period))
        {
            return 99;
        }

        var lastYear = period / 100;
        var lastMonth = period % 100;
        var currentTotal = today.Year * 12 + today.Month;
        var lastTotal = lastYear * 12 + lastMonth;
        return Math.Max(0, currentTotal - lastTotal);
    }

    private static decimal? CalculateTrendPercent(long current, long previous)
    {
        if (previous == 0)
        {
            return current == 0 ? 0 : 100;
        }

        return decimal.Round((current - previous) * 100m / previous, 2);
    }

    private static int ClampScore(int value) => Math.Max(0, Math.Min(100, value));

    private static long GetInt64(SqlDataReader reader, string name)
    {
        var value = reader[name];
        return value is null or DBNull ? 0 : Convert.ToInt64(value);
    }

    private static bool GetBoolean(SqlDataReader reader, string name)
    {
        var value = reader[name];
        return value is not null and not DBNull && Convert.ToBoolean(value);
    }

    private static DateTime? GetNullableDateTime(SqlDataReader reader, string name)
    {
        var value = reader[name];
        return value is null or DBNull ? null : Convert.ToDateTime(value);
    }

    private static string? GetNullableString(SqlDataReader reader, string name)
    {
        var value = reader[name];
        return value is null or DBNull ? null : value.ToString();
    }

    private sealed record ActivityMetrics(
        int? DaysSinceLastActivity,
        long Activity7Days,
        long Activity30Days,
        long Previous7DaysActivity,
        long NullStartTimeCount);

    private sealed record PayrollMetrics(
        string? LastProcessedPeriod,
        bool CurrentMonthProcessed,
        int ProcessedPeriodsInYear,
        int MonthsBehindCurrent,
        int TotalPeriods,
        int ConsistentPeriods,
        int MissingPeriodsCount)
    {
        public int InconsistentPeriodsCount => TotalPeriods - ConsistentPeriods;
    }

    private sealed record OlympDashboardScores(
        int HealthScore,
        int ActivityScore,
        int PayrollCurrentnessScore,
        int PayrollConsistencyScore,
        int PeriodRegularityScore);
}
```

- [ ] **Step 4.2: Build verify**

```powershell
dotnet build "src\KrosExtension.Infrastructure\KrosExtension.Infrastructure.csproj"
```

Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 4.3: Commit**

```bash
git add src/KrosExtension.Infrastructure/Companies/OlympDashboardSnapshotCalculator.cs
git commit -m "feat: add OlympDashboardSnapshotCalculator with activity and payroll SQL"
```

---

### Task 5: EF Core migration

**Prerequisite: Tasks 2 and 4 must be complete (entity and DbSet must exist).**

- [ ] **Step 5.1: Add and apply migration**

```powershell
cd "e:\Projects\Kros extension"
dotnet ef migrations add AddOlympDashboardSnapshots --project src\KrosExtension.Infrastructure --startup-project src\KrosExtension.Api
dotnet ef database update --project src\KrosExtension.Infrastructure --startup-project src\KrosExtension.Api
```

Expected: migration file created, database updated with `OlympCompanyDashboardSnapshots` table.

- [ ] **Step 5.2: Commit**

```bash
git add src/KrosExtension.Infrastructure/Persistence/Migrations/
git commit -m "feat: EF migration — add OlympCompanyDashboardSnapshots table"
```

---

### Task 6: Implement readers and register services

**Files:**
- Create: `src/KrosExtension.Infrastructure/Companies/OlympDashboardReader.cs`
- Create: `src/KrosExtension.Infrastructure/Companies/OlympCompanyDetailReader.cs`
- Modify: `src/KrosExtension.Infrastructure/DependencyInjection.cs`

- [ ] **Step 6.1: Create `OlympDashboardReader.cs`**

Follow the exact same pattern as `OmegaDashboardReader.cs` but for Olymp:

```csharp
using KrosExtension.Application.Companies;
using KrosExtension.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KrosExtension.Infrastructure.Companies;

public sealed class OlympDashboardReader(ApplicationDbContext dbContext) : IOlympDashboardReader
{
    public async Task<IReadOnlyList<OlympDashboardCompany>> GetCompaniesAsync(
        CancellationToken cancellationToken = default)
    {
        var snapshots = await dbContext.OlympCompanyDashboardSnapshots
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var latest = snapshots
            .GroupBy(s => new { s.DataReadServerId, s.DatabaseName })
            .Select(g => g.OrderByDescending(s => s.SnapshotAt).First())
            .OrderBy(s => s.HealthScore)
            .ThenBy(s => s.LastActivityDate)
            .ToList();

        return latest.Select(s => new OlympDashboardCompany(
            s.DataReadServerId,
            s.ServerDisplayName,
            s.DatabaseName,
            s.CompanyName,
            s.AccountingYear,
            s.SnapshotAt,
            s.HealthScore,
            s.ActivityScore,
            s.PayrollCurrentnessScore,
            s.PayrollConsistencyScore,
            s.PeriodRegularityScore,
            s.LastActivityDate,
            s.LastProcessedPeriod,
            s.CurrentMonthProcessed,
            s.ProcessedPeriodsInYear,
            s.Activity7Days,
            s.Activity30Days,
            s.TrendPercent,
            SplitWarnings(s.WarningFlags))).ToList();
    }

    private static IReadOnlyList<string> SplitWarnings(string flags) =>
        string.IsNullOrWhiteSpace(flags)
            ? []
            : flags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
```

- [ ] **Step 6.2: Create `OlympCompanyDetailReader.cs`**

Follow the same pattern as `OmegaCompanyDetailReader.cs`. The reader:
1. Fetches the latest `OlympCompanyDashboardSnapshot` from KrosExtension DB (for scores and warnings)
2. Queries the Olymp company DB live for per-period detail (the `period_consistency` CTE from PayrollSql, returned as multiple rows)

```csharp
using KrosExtension.Application.Companies;
using KrosExtension.Domain.Configuration;
using KrosExtension.Infrastructure.Configuration;
using KrosExtension.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace KrosExtension.Infrastructure.Companies;

public sealed class OlympCompanyDetailReader(ApplicationDbContext dbContext) : IOlympCompanyDetailReader
{
    public async Task<OlympCompanyDetail?> GetCompanyDetailAsync(
        Guid dataReadServerId,
        string databaseName,
        CancellationToken cancellationToken = default)
    {
        var server = await dbContext.DataReadServers
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == dataReadServerId && s.IsActive, cancellationToken);

        if (server is null) return null;

        var snapshot = await dbContext.OlympCompanyDashboardSnapshots
            .AsNoTracking()
            .Where(s => s.DataReadServerId == dataReadServerId && s.DatabaseName == databaseName)
            .OrderByDescending(s => s.SnapshotAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (snapshot is null) return null;

        var connectionString = DataReadServerConnectionStringFactory.CreateDatabaseConnectionString(
            server, ProductType.Olymp, databaseName);

        if (connectionString is null) return null;

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var periods = await ReadPeriodsAsync(connection, cancellationToken);

        var warnings = string.IsNullOrWhiteSpace(snapshot.WarningFlags)
            ? (IReadOnlyList<string>)[]
            : snapshot.WarningFlags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return new OlympCompanyDetail(
            snapshot.DataReadServerId,
            snapshot.ServerDisplayName,
            snapshot.DatabaseName,
            snapshot.CompanyName,
            snapshot.AccountingYear,
            snapshot.SnapshotAt,
            snapshot.HealthScore,
            snapshot.ActivityScore,
            snapshot.PayrollCurrentnessScore,
            snapshot.PayrollConsistencyScore,
            snapshot.PeriodRegularityScore,
            snapshot.LastActivityDate,
            snapshot.LastProcessedPeriod,
            snapshot.CurrentMonthProcessed,
            snapshot.ProcessedPeriodsInYear,
            snapshot.Activity7Days,
            snapshot.Activity30Days,
            snapshot.NullStartTimeCount,
            snapshot.MissingPeriodsCount,
            snapshot.InconsistentPeriodsCount,
            periods,
            warnings);
    }

    // FILL FROM TASK 1: adjust ID_obdobie handling if format differs
    private static async Task<IReadOnlyList<OlympMonthlyPeriod>> ReadPeriodsAsync(
        SqlConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            WITH all_periods AS (
                SELECT DISTINCT ID_obdobie FROM dbo.pracVycMzda
                UNION
                SELECT DISTINCT ID_obdobie FROM dbo.pracVycBase
                UNION
                SELECT DISTINCT ID_obdobie FROM dbo.pracVycOdvod
            )
            SELECT
                CAST(p.ID_obdobie AS varchar(10)) AS PeriodId,
                CAST(CASE WHEN m.ID_obdobie IS NOT NULL THEN 1 ELSE 0 END AS bit) AS HasMzda,
                CAST(CASE WHEN b.ID_obdobie IS NOT NULL THEN 1 ELSE 0 END AS bit) AS HasBase,
                CAST(CASE WHEN o.ID_obdobie IS NOT NULL THEN 1 ELSE 0 END AS bit) AS HasOdvod
            FROM all_periods p
            LEFT JOIN (SELECT DISTINCT ID_obdobie FROM dbo.pracVycMzda) m ON m.ID_obdobie = p.ID_obdobie
            LEFT JOIN (SELECT DISTINCT ID_obdobie FROM dbo.pracVycBase) b ON b.ID_obdobie = p.ID_obdobie
            LEFT JOIN (SELECT DISTINCT ID_obdobie FROM dbo.pracVycOdvod) o ON o.ID_obdobie = p.ID_obdobie
            ORDER BY p.ID_obdobie DESC
            """;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var list = new List<OlympMonthlyPeriod>();

        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(new OlympMonthlyPeriod(
                reader.GetString(reader.GetOrdinal("PeriodId")),
                reader.GetBoolean(reader.GetOrdinal("HasMzda")),
                reader.GetBoolean(reader.GetOrdinal("HasBase")),
                reader.GetBoolean(reader.GetOrdinal("HasOdvod"))));
        }

        return list;
    }
}
```

- [ ] **Step 6.3: Register services in `DependencyInjection.cs`**

Add after the existing `IOmegaDashboardReader` and `IOmegaCompanyDetailReader` lines:

```csharp
        services.AddScoped<IOlympDashboardReader, OlympDashboardReader>();
        services.AddScoped<IOlympCompanyDetailReader, OlympCompanyDetailReader>();
```

- [ ] **Step 6.4: Build verify**

```powershell
dotnet build "src\KrosExtension.Infrastructure\KrosExtension.Infrastructure.csproj"
```

Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 6.5: Commit**

```bash
git add src/KrosExtension.Infrastructure/Companies/OlympDashboardReader.cs
git add src/KrosExtension.Infrastructure/Companies/OlympCompanyDetailReader.cs
git add src/KrosExtension.Infrastructure/DependencyInjection.cs
git commit -m "feat: implement OlympDashboardReader, OlympCompanyDetailReader, register services"
```

---

### Task 7: Wire UpdateData + API endpoints

**Files:**
- Modify: `src/KrosExtension.Infrastructure/Companies/CompanyDataRefreshService.cs`
- Modify: `src/KrosExtension.Api/Endpoints/ProductCompanyEndpoints.cs`

- [ ] **Step 7.1: Add Olymp snapshot branch in `CompanyDataRefreshService.cs`**

Read the file. Inside `UpdateDataAsync`, there is already an Omega branch:
```csharp
if (productType == ProductType.Omega) { ... }
```

Add an Olymp branch immediately after it (same structure — delete old snapshots > 30 days, add new snapshot):

```csharp
            if (productType == ProductType.Olymp)
            {
                var cutoff = DateTimeOffset.UtcNow.AddDays(-30);
                var outdated = await dbContext.OlympCompanyDashboardSnapshots
                    .Where(s => s.DataReadServerId == company.DataReadServerId
                             && s.DatabaseName == company.DatabaseName
                             && s.SnapshotAt < cutoff)
                    .ToListAsync(cancellationToken);

                if (outdated.Count > 0)
                {
                    dbContext.OlympCompanyDashboardSnapshots.RemoveRange(outdated);
                }

                var snapshot = await OlympDashboardSnapshotCalculator.ReadSnapshotAsync(
                    server,
                    company,
                    DateTime.Today,
                    cancellationToken);

                if (snapshot is not null)
                {
                    dbContext.OlympCompanyDashboardSnapshots.Add(snapshot);
                }
            }
```

- [ ] **Step 7.2: Add Olymp dashboard endpoints in `ProductCompanyEndpoints.cs`**

Read the file. There is already an `if (productType == ProductType.Omega)` block. Add a parallel block for Olymp immediately before or after it:

```csharp
        if (productType == ProductType.Olymp)
        {
            productGroup.MapGet("/dashboard/companies", async (
                IOlympDashboardReader dashboardReader,
                CancellationToken cancellationToken) =>
            {
                var companies = await dashboardReader.GetCompaniesAsync(cancellationToken);
                return Results.Ok(companies);
            }).WithName("GetOlympDashboardCompanies");

            productGroup.MapGet("/dashboard/companies/{dataReadServerId:guid}/{databaseName}", async (
                Guid dataReadServerId,
                string databaseName,
                IOlympCompanyDetailReader detailReader,
                CancellationToken cancellationToken) =>
            {
                var detail = await detailReader.GetCompanyDetailAsync(
                    dataReadServerId, databaseName, cancellationToken);
                return detail is null ? Results.NotFound() : Results.Ok(detail);
            }).WithName("GetOlympCompanyDetail");
        }
```

- [ ] **Step 7.3: Full solution build**

```powershell
dotnet build "KrosExtension.sln"
```

Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 7.4: Smoke test UpdateData**

```powershell
dotnet run --project "src\KrosExtension.Api\KrosExtension.Api.csproj"
```

```powershell
Invoke-RestMethod -Uri 'http://localhost:5000/api/olymp/companies/update-data' -Method POST -ContentType 'application/json' -Body '{"databaseName": null}'
Invoke-RestMethod 'http://localhost:5000/api/olymp/dashboard/companies'
```

Expected: UpdateData returns result with refreshed/skipped counts; dashboard returns array with health scores and warning flags.

- [ ] **Step 7.5: Commit**

```bash
git add src/KrosExtension.Infrastructure/Companies/CompanyDataRefreshService.cs
git add src/KrosExtension.Api/Endpoints/ProductCompanyEndpoints.cs
git commit -m "feat: wire Olymp into UpdateData pipeline and add dashboard API endpoints"
```

---

### Task 8: Frontend — OlympDashboardPage

**File:**
- Modify: `src/KrosExtension.Web/ClientApp/src/main.tsx`

- [ ] **Step 8.1: Add TypeScript types after existing Omega types**

```typescript
type OlympDashboardCompany = {
  dataReadServerId: string;
  serverDisplayName: string;
  databaseName: string;
  companyName: string;
  accountingYear: number | null;
  snapshotAt: string;
  healthScore: number;
  activityScore: number;
  payrollCurrentnessScore: number;
  payrollConsistencyScore: number;
  periodRegularityScore: number;
  lastActivityDate: string | null;
  lastProcessedPeriod: string | null;
  currentMonthProcessed: boolean;
  processedPeriodsInYear: number;
  activity7Days: number;
  activity30Days: number;
  trendPercent: number | null;
  warningFlags: string[];
};

type OlympMonthlyPeriod = {
  periodId: string;
  hasMzda: boolean;
  hasBase: boolean;
  hasOdvod: boolean;
};

type OlympCompanyDetail = {
  dataReadServerId: string;
  serverDisplayName: string;
  databaseName: string;
  companyName: string;
  accountingYear: number | null;
  snapshotAt: string;
  healthScore: number;
  activityScore: number;
  payrollCurrentnessScore: number;
  payrollConsistencyScore: number;
  periodRegularityScore: number;
  lastActivityDate: string | null;
  lastProcessedPeriod: string | null;
  currentMonthProcessed: boolean;
  processedPeriodsInYear: number;
  activity7Days: number;
  activity30Days: number;
  nullStartTimeCount: number;
  missingPeriodsCount: number;
  inconsistentPeriodsCount: number;
  periods: OlympMonthlyPeriod[];
  warningFlags: string[];
};
```

- [ ] **Step 8.2: Add Olymp-specific warning helpers**

Add these two functions alongside the existing `warningSeverity` and `warningLabel`:

```typescript
function olympWarningSeverity(warning: string) {
  if (['CURRENT_MONTH_MISSING', 'PAYROLL_MONTHS_BEHIND', 'INACTIVE_30D'].includes(warning)) {
    return 'critical';
  }

  if (['INACTIVE_14D', 'MISSING_PERIODS', 'PAYROLL_INCONSISTENCY', 'NULL_STARTTIME'].includes(warning)) {
    return 'bad';
  }

  return 'watch';
}

function olympWarningLabel(warning: string) {
  const labels: Record<string, string> = {
    CURRENT_MONTH_MISSING: 'mesiac chýba',
    PAYROLL_MONTHS_BEHIND: 'oneskorené mzdy',
    INACTIVE_30D: '30d neaktivita',
    INACTIVE_14D: '14d neaktivita',
    MISSING_PERIODS: 'chýbajúce obdobia',
    PAYROLL_INCONSISTENCY: 'nekonzistencia',
    NULL_STARTTIME: 'null timestamp',
    LOW_ACTIVITY: 'nízka aktivita'
  };

  return labels[warning] ?? warning;
}
```

- [ ] **Step 8.3: Add `olympDetail` navigation state to `App`**

In the `App` function, after `omegaDetail` state, add:

```typescript
  const [olympDetail, setOlympDetail] = useState<{ dataReadServerId: string; databaseName: string } | null>(null);
```

- [ ] **Step 8.4: Update `stats-olymp` workspace renderer**

Replace:
```typescript
        {section === 'stats-olymp' && <StatisticsPage product="olymp" />}
```

With:
```typescript
        {section === 'stats-olymp' && (olympDetail
          ? <OlympCompanyDetailPage
              dataReadServerId={olympDetail.dataReadServerId}
              databaseName={olympDetail.databaseName}
              onBack={() => setOlympDetail(null)}
            />
          : <OlympDashboardPage
              onOpenDetail={(serverId, db) => setOlympDetail({ dataReadServerId: serverId, databaseName: db })}
            />
        )}
```

Also reset `olympDetail` when the Olymp sidebar button is clicked (add `setOlympDetail(null)` alongside `setSection('stats-olymp')`).

- [ ] **Step 8.5: Add `OlympDashboardPage` component**

Add before `OlympCompanyDetailPage` (which will be added in Task 9):

```typescript
function OlympDashboardPage({ onOpenDetail }: { onOpenDetail: (dataReadServerId: string, databaseName: string) => void }) {
  const [companies, setCompanies] = useState<OlympDashboardCompany[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [isUpdating, setIsUpdating] = useState(false);
  const [error, setError] = useState('');

  async function load() {
    setIsLoading(true);
    setError('');

    try {
      const response = await fetch('/api/olymp/dashboard/companies');

      if (!response.ok) {
        setError(await response.text());
        setCompanies([]);
        return;
      }

      setCompanies(await response.json());
    } catch (exception) {
      setError(exception instanceof Error ? exception.message : 'Načítanie Olymp dashboardu zlyhalo.');
      setCompanies([]);
    } finally {
      setIsLoading(false);
    }
  }

  async function updateData() {
    setIsUpdating(true);
    setError('');

    try {
      const response = await fetch('/api/olymp/companies/update-data', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ databaseName: null })
      });

      if (!response.ok) {
        setError(await response.text());
        return;
      }

      await load();
    } catch (exception) {
      setError(exception instanceof Error ? exception.message : 'UpdateData zlyhalo.');
    } finally {
      setIsUpdating(false);
    }
  }

  useEffect(() => {
    void load();
  }, []);

  return (
    <div className="page">
      <header>
        <h1>Olymp dashboard</h1>
        <div className="actions">
          <button onClick={load} disabled={isLoading || isUpdating}>
            <RefreshCw size={16} /> {isLoading ? 'Načítavam' : 'Obnoviť'}
          </button>
          <button onClick={updateData} disabled={isLoading || isUpdating}>
            <RefreshCw size={16} /> {isUpdating ? 'Aktualizujem...' : 'UpdateData'}
          </button>
        </div>
      </header>

      {error && <div className="status error">{error}</div>}

      {companies.length > 0 && (
        <div className="snapshot-age">
          Snapshot: {formatSnapshotAge(companies[0].snapshotAt)} starý
        </div>
      )}

      <table className="risk-table">
        <thead>
          <tr>
            <th>Firma</th>
            <th>Health</th>
            <th>Posledná aktivita</th>
            <th>Posledné obdobie</th>
            <th>Akt. mesiac</th>
            <th>Období v roku</th>
            <th>Trend</th>
            <th>Warning flags</th>
          </tr>
        </thead>
        <tbody>
          {!isLoading && companies.length === 0 && (
            <tr>
              <td colSpan={8}>Olymp dashboard zatiaľ nemá snapshoty. Spusti UpdateData.</td>
            </tr>
          )}
          {companies.map((company) => (
            <tr key={`${company.dataReadServerId}-${company.databaseName}`}>
              <td>
                <button
                  className="company-link"
                  onClick={() => onOpenDetail(company.dataReadServerId, company.databaseName)}
                >
                  <strong>{company.companyName}</strong>
                  <span className="muted-line">{company.databaseName}</span>
                </button>
              </td>
              <td>
                <span className={`score-pill ${scoreClass(company.healthScore)}`}>{company.healthScore}</span>
              </td>
              <td>{formatDate(company.lastActivityDate)}</td>
              <td>{company.lastProcessedPeriod ?? '–'}</td>
              <td>
                <span className={company.currentMonthProcessed ? 'score-pill good' : 'score-pill critical'}>
                  {company.currentMonthProcessed ? '✓' : '✗'}
                </span>
              </td>
              <td>{company.processedPeriodsInYear}</td>
              <td>{formatTrend(company.trendPercent)}</td>
              <td>
                <div className="warning-list">
                  {company.warningFlags.length === 0 && <span className="warning-pill ok">OK</span>}
                  {company.warningFlags.map((warning) => (
                    <span key={warning} className={`warning-pill ${olympWarningSeverity(warning)}`}>
                      {olympWarningLabel(warning)}
                    </span>
                  ))}
                </div>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
```

- [ ] **Step 8.6: Build frontend (expect one error for missing OlympCompanyDetailPage)**

```powershell
cd .\src\KrosExtension.Web\ClientApp; npm run build
```

Expected: may fail with "Cannot find name 'OlympCompanyDetailPage'" — that is OK; Task 9 fixes it.

- [ ] **Step 8.7: Commit**

```bash
git add src/KrosExtension.Web/ClientApp/src/main.tsx
git commit -m "feat: add OlympDashboardPage with risk table, health score, warnings"
```

---

### Task 9: Frontend — OlympCompanyDetailPage

**File:**
- Modify: `src/KrosExtension.Web/ClientApp/src/main.tsx`

- [ ] **Step 9.1: Add `OlympCompanyDetailPage` component**

Add before `scoreClass` function:

```typescript
function OlympCompanyDetailPage({
  dataReadServerId,
  databaseName,
  onBack
}: {
  dataReadServerId: string;
  databaseName: string;
  onBack: () => void;
}) {
  const [detail, setDetail] = useState<OlympCompanyDetail | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState('');

  async function load() {
    setIsLoading(true);
    setError('');

    try {
      const response = await fetch(
        `/api/olymp/dashboard/companies/${dataReadServerId}/${encodeURIComponent(databaseName)}`
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
                { label: 'Aktivita', value: detail.activityScore, weight: '25 %' },
                { label: 'Aktuálnosť miezd', value: detail.payrollCurrentnessScore, weight: '35 %' },
                { label: 'Konzistencia', value: detail.payrollConsistencyScore, weight: '25 %' },
                { label: 'Pravidelnosť', value: detail.periodRegularityScore, weight: '15 %' }
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

          <section className="detail-card">
            <h2>Payroll status</h2>
            <div className="type-breakdown">
              {([
                { label: 'Posledná aktivita', value: formatDate(detail.lastActivityDate) },
                { label: 'Posledné obdobie', value: detail.lastProcessedPeriod ?? '–' },
                { label: 'Akt. mesiac spracovaný', value: detail.currentMonthProcessed ? 'Áno' : 'Nie' },
                { label: 'Období v roku', value: String(detail.processedPeriodsInYear) },
                { label: 'Chýbajúce obdobia', value: String(detail.missingPeriodsCount) },
                { label: 'Nekonzistentné obdobia', value: String(detail.inconsistentPeriodsCount) },
                { label: 'Null timestamps', value: String(detail.nullStartTimeCount) }
              ] as const).map(({ label, value }) => (
                <div key={label} className="type-row">
                  <span>{label}</span>
                  <strong>{value}</strong>
                </div>
              ))}
            </div>
          </section>

          {detail.warningFlags.length > 0 && (
            <section className="detail-card">
              <h2>Varovania</h2>
              <div className="warning-list large">
                {detail.warningFlags.map((flag) => (
                  <span key={flag} className={`warning-pill ${olympWarningSeverity(flag)}`}>
                    {olympWarningLabel(flag)}
                  </span>
                ))}
              </div>
            </section>
          )}

          <section className="detail-card">
            <h2>Aktivita</h2>
            <div className="type-breakdown">
              {([
                { label: 'Posledných 7 dní', value: String(detail.activity7Days) },
                { label: 'Posledných 30 dní', value: String(detail.activity30Days) }
              ] as const).map(({ label, value }) => (
                <div key={label} className="type-row">
                  <span>{label}</span>
                  <strong>{value}</strong>
                </div>
              ))}
            </div>
          </section>

          <section className="detail-card full-width">
            <h2>Mzdové obdobia</h2>
            <div className="heatmap">
              {detail.periods.map((p) => {
                const allOk = p.hasMzda && p.hasBase && p.hasOdvod;
                const noneOk = !p.hasMzda && !p.hasBase && !p.hasOdvod;
                const cls = noneOk ? 'heatmap-empty'
                  : allOk ? 'heatmap-great'
                  : 'heatmap-bad';
                return (
                  <div
                    key={p.periodId}
                    className={`heatmap-cell ${cls}`}
                    title={`${p.periodId}: mzda=${p.hasMzda ? '✓' : '✗'} base=${p.hasBase ? '✓' : '✗'} odvod=${p.hasOdvod ? '✓' : '✗'}`}
                  >
                    <span className="heatmap-month">{p.periodId}</span>
                    <span className="heatmap-rate">{allOk ? '✓' : noneOk ? '–' : '!'}</span>
                    <span className="heatmap-docs">
                      {[p.hasMzda ? 'M' : '·', p.hasBase ? 'B' : '·', p.hasOdvod ? 'O' : '·'].join(' ')}
                    </span>
                  </div>
                );
              })}
            </div>
            <div className="heatmap-legend">
              <span className="heatmap-great">Kompletné</span>
              <span className="heatmap-bad">Neúplné</span>
              <span className="heatmap-empty">Bez dát</span>
            </div>
          </section>

        </div>
      )}
    </div>
  );
}
```

- [ ] **Step 9.2: Build frontend (must be clean)**

```powershell
cd .\src\KrosExtension.Web\ClientApp; npm run build
```

Expected: zero TypeScript errors.

- [ ] **Step 9.3: Commit**

```bash
git add src/KrosExtension.Web/ClientApp/src/main.tsx
git commit -m "feat: add OlympCompanyDetailPage with score breakdown, payroll status, period heatmap"
```

---

### Task 10: End-to-end verification

- [ ] **Step 10.1: Run full solution**

Terminal 1: `dotnet run --project src\KrosExtension.Api\KrosExtension.Api.csproj`
Terminal 2: `cd src\KrosExtension.Web; dotnet run --launch-profile http`

- [ ] **Step 10.2: Verify UpdateData and global dashboard**

Browser → Olymp → UpdateData.

Check:
- [ ] Table shows companies ordered worst health first
- [ ] Health score pills colour-coded
- [ ] `lastProcessedPeriod` shows period string
- [ ] `currentMonthProcessed` shows ✓ or ✗
- [ ] Warning flags use Olymp codes (CURRENT_MONTH_MISSING, INACTIVE_*, etc.)

- [ ] **Step 10.3: Verify company detail**

Click company name.

Check:
- [ ] 4-bar health score breakdown (Aktivita, Aktuálnosť, Konzistencia, Pravidelnosť)
- [ ] Payroll status panel
- [ ] Period heatmap — each cell shows M/B/O presence
- [ ] ✓ for complete periods, ! for partial, – for missing

- [ ] **Step 10.4: Final build check**

```powershell
dotnet build KrosExtension.sln
cd src\KrosExtension.Web\ClientApp; npm run build
```

Both: zero errors.
