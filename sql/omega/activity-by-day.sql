/*
Run in an OMEGA database.
Parameters supplied by the backend:
  @FromDate date inclusive
  @ToDate   date inclusive

Returns registered accounting-document activity only. Do not add rows from
T228_Faktury to this result: both sources can contain the same invoice.
*/
WITH registered AS (
    SELECT CASE e.C023_CisloInterneKodOkruh
               WHEN 'DF'  THEN 'omega.received_invoice.registered'
               WHEN 'OF'  THEN 'omega.sent_invoice.registered'
               WHEN 'DPF' THEN 'omega.received_advance_invoice.registered'
               WHEN 'OPF' THEN 'omega.sent_advance_invoice.registered'
               WHEN 'zDF' THEN 'omega.received_invoice_z.registered'
               WHEN 'zOF' THEN 'omega.sent_invoice_z.registered'
           END AS Metric,
           TRY_CONVERT(date, CONCAT(
               e.C116_RokZaevidovania, '-',
               e.C115_MesZaevidovania, '-',
               e.C114_DenZaevidovania
           )) AS ActivityDate
    FROM dbo.T040_EUD AS e
    WHERE ISNULL(e.C001_Vymazany, 0) = 0
      AND e.C023_CisloInterneKodOkruh IN ('DF', 'OF', 'DPF', 'OPF', 'zDF', 'zOF')
)
SELECT Metric,
       ActivityDate,
       COUNT_BIG(*) AS ActivityCount
FROM registered
WHERE ActivityDate BETWEEN @FromDate AND @ToDate
GROUP BY Metric, ActivityDate
ORDER BY ActivityDate, Metric;

