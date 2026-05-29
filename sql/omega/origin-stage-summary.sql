/*
Run in an OMEGA database.
Parameters:
  @FromDate date inclusive
  @ToDate   date inclusive

C138_PovodDokladu is a source/origin field and C187_PlatformaId is an
associated platform identifier. Their concrete value mapping must be
confirmed on a database containing a Digital Office import before labels
such as "manual" or "Digital Office" are exposed in UI.
*/
WITH documents AS (
    SELECT CASE e.C023_CisloInterneKodOkruh
               WHEN 'DF'  THEN 'omega.received_invoice'
               WHEN 'OF'  THEN 'omega.sent_invoice'
               WHEN 'DPF' THEN 'omega.received_advance_invoice'
               WHEN 'OPF' THEN 'omega.sent_advance_invoice'
               WHEN 'zDF' THEN 'omega.received_invoice_z'
               WHEN 'zOF' THEN 'omega.sent_invoice_z'
           END AS DocumentKind,
           TRY_CONVERT(date, CONCAT(
               e.C116_RokZaevidovania, '-',
               e.C115_MesZaevidovania, '-',
               e.C114_DenZaevidovania
           )) AS ActivityDate,
           e.C138_PovodDokladu AS OriginCode,
           CASE WHEN NULLIF(LTRIM(RTRIM(e.C187_PlatformaId)), '') IS NULL
                THEN CAST(0 AS bit) ELSE CAST(1 AS bit) END AS HasPlatformId,
           e.C108_PREdkontovany AS PostingPreparationFlag,
           CASE WHEN NULLIF(LTRIM(RTRIM(e.C027_KodZauctoval)), '') IS NULL
                THEN CAST(0 AS bit) ELSE CAST(1 AS bit) END AS HasRecordedAccountant
    FROM dbo.T040_EUD AS e
    WHERE ISNULL(e.C001_Vymazany, 0) = 0
      AND e.C023_CisloInterneKodOkruh IN ('DF', 'OF', 'DPF', 'OPF', 'zDF', 'zOF')
)
SELECT DocumentKind,
       OriginCode,
       HasPlatformId,
       PostingPreparationFlag,
       HasRecordedAccountant,
       COUNT_BIG(*) AS DocumentCount
FROM documents
WHERE ActivityDate BETWEEN @FromDate AND @ToDate
GROUP BY DocumentKind, OriginCode, HasPlatformId, PostingPreparationFlag, HasRecordedAccountant
ORDER BY DocumentKind, OriginCode, PostingPreparationFlag, HasRecordedAccountant;

