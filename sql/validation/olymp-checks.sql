/*
Run when registering or periodically validating an OLYMP database.
Outputs describe period consistency only; no employee rows or wage values are
returned.
*/
WITH wage_periods AS (
    SELECT DISTINCT ID_obdobie
    FROM dbo.pracVycMzda
),
base_periods AS (
    SELECT DISTINCT ID_obdobie
    FROM dbo.pracVycBase
),
deduction_periods AS (
    SELECT DISTINCT ID_obdobie
    FROM dbo.pracVycOdvod
)
SELECT 'invalid_payroll_period_id' AS CheckKey,
       COUNT_BIG(*) AS IssueCount
FROM wage_periods
WHERE ID_obdobie NOT BETWEEN 190001 AND 299912
   OR ID_obdobie % 100 NOT BETWEEN 1 AND 12
UNION ALL
SELECT 'payroll_period_missing_base',
       COUNT_BIG(*)
FROM wage_periods AS w
LEFT JOIN base_periods AS b ON b.ID_obdobie = w.ID_obdobie
WHERE b.ID_obdobie IS NULL
UNION ALL
SELECT 'payroll_period_missing_deductions',
       COUNT_BIG(*)
FROM wage_periods AS w
LEFT JOIN deduction_periods AS d ON d.ID_obdobie = w.ID_obdobie
WHERE d.ID_obdobie IS NULL
UNION ALL
SELECT 'activity_without_start_time',
       COUNT_BIG(*)
FROM dbo.activityLog
WHERE StartTime IS NULL;

