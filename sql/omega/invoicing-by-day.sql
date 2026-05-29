/*
Run in an OMEGA database.
Optional metric for documents created in the invoicing agenda T228_Faktury.
It is intentionally separate from accounting-document registration in
T040_EUD, because the same document can appear in both tables.

Parameters:
  @FromDate date inclusive
  @ToDate   date inclusive
*/
WITH invoicing AS (
    SELECT CASE f.C022_KodEvidencia
               WHEN 'OF'  THEN 'omega.sent_invoice.created_in_invoicing'
               WHEN 'OPF' THEN 'omega.sent_advance_invoice.created_in_invoicing'
           END AS Metric,
           TRY_CONVERT(date, CONCAT(
               f.C062_RokVyst, '-',
               f.C061_MesVyst, '-',
               f.C060_DenVyst
           )) AS ActivityDate
    FROM dbo.T228_Faktury AS f
    WHERE f.C022_KodEvidencia IN ('OF', 'OPF')
)
SELECT Metric,
       ActivityDate,
       COUNT_BIG(*) AS ActivityCount
FROM invoicing
WHERE ActivityDate BETWEEN @FromDate AND @ToDate
GROUP BY Metric, ActivityDate
ORDER BY ActivityDate, Metric;

