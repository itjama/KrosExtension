/*
Run in an OMEGA database only for an authorised user-detail endpoint.
Parameters:
  @FromDate date inclusive
  @ToDate   date inclusive

Important: C028_KodZaevidoval and C027_KodZauctoval are attribution fields
stored on the document. Present them as "recorded on document", not as
audit-proof evidence of the logged-in user who performed an action.
The query never reads T004_Uzivatelia, which contains credential fields.
*/
WITH documents AS (
    SELECT CASE e.C023_CisloInterneKodOkruh
               WHEN 'DF'  THEN 'received_invoice'
               WHEN 'OF'  THEN 'sent_invoice'
               WHEN 'DPF' THEN 'received_advance_invoice'
               WHEN 'OPF' THEN 'sent_advance_invoice'
               WHEN 'zDF' THEN 'received_invoice_z'
               WHEN 'zOF' THEN 'sent_invoice_z'
           END AS DocumentKind,
           TRY_CONVERT(date, CONCAT(
               e.C116_RokZaevidovania, '-',
               e.C115_MesZaevidovania, '-',
               e.C114_DenZaevidovania
           )) AS ActivityDate,
           NULLIF(LTRIM(RTRIM(e.C028_KodZaevidoval)), '') AS RegisteredByKey,
           NULLIF(LTRIM(RTRIM(e.C027_KodZauctoval)), '') AS AccountedByKey
    FROM dbo.T040_EUD AS e
    WHERE ISNULL(e.C001_Vymazany, 0) = 0
      AND e.C023_CisloInterneKodOkruh IN ('DF', 'OF', 'DPF', 'OPF', 'zDF', 'zOF')
),
events AS (
    SELECT CONCAT('omega.', DocumentKind, '.registered_by_recorded_user') AS Metric,
           ActivityDate,
           RegisteredByKey AS RecordedUserKey
    FROM documents
    WHERE RegisteredByKey IS NOT NULL
    UNION ALL
    SELECT CONCAT('omega.', DocumentKind, '.accounted_by_recorded_user') AS Metric,
           ActivityDate,
           AccountedByKey AS RecordedUserKey
    FROM documents
    WHERE AccountedByKey IS NOT NULL
)
SELECT Metric,
       ActivityDate,
       RecordedUserKey,
       COUNT_BIG(*) AS ActivityCount
FROM events
WHERE ActivityDate BETWEEN @FromDate AND @ToDate
GROUP BY Metric, ActivityDate, RecordedUserKey
ORDER BY ActivityDate, Metric, RecordedUserKey;

