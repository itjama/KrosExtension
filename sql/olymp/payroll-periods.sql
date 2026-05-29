/*
Run in an OLYMP database.
Parameters:
  @FromDate date inclusive
  @ToDate   date inclusive

ID_obdobie is YYYYMM in analysed data. This query reports only whether a
payroll output exists for a month, not the number of employee output rows.
*/
WITH period_ids AS (
    SELECT DISTINCT ID_obdobie
    FROM dbo.pracVycMzda
    WHERE ID_obdobie BETWEEN 190001 AND 299912
      AND ID_obdobie % 100 BETWEEN 1 AND 12
),
processed AS (
    SELECT DATEFROMPARTS(ID_obdobie / 100, ID_obdobie % 100, 1) AS ActivityDate
    FROM period_ids
)
SELECT 'olymp.payroll_period.processed' AS Metric,
       ActivityDate,
       CAST(1 AS bigint) AS ActivityCount
FROM processed
WHERE ActivityDate BETWEEN DATEFROMPARTS(YEAR(@FromDate), MONTH(@FromDate), 1)
                       AND @ToDate
ORDER BY ActivityDate;

