/*
Run when registering or periodically validating an OMEGA database.
All outputs are aggregate technical checks and contain no document content.
*/
WITH registration_dates AS (
    SELECT C023_CisloInterneKodOkruh AS Code,
           TRY_CONVERT(date, CONCAT(
               C116_RokZaevidovania, '-',
               C115_MesZaevidovania, '-',
               C114_DenZaevidovania
           )) AS ActivityDate
    FROM dbo.T040_EUD
    WHERE ISNULL(C001_Vymazany, 0) = 0
      AND C023_CisloInterneKodOkruh IN ('DF', 'OF', 'DPF', 'OPF', 'zDF', 'zOF')
)
SELECT 'missing_registration_date' AS CheckKey,
       COUNT_BIG(*) AS IssueCount
FROM registration_dates
WHERE ActivityDate IS NULL
UNION ALL
SELECT 'orphan_eud_item',
       COUNT_BIG(*)
FROM dbo.T041_EUD_Polozky AS p
LEFT JOIN dbo.T040_EUD AS h ON h.C000_ID = p.C010_IDEUD
WHERE h.C000_ID IS NULL
UNION ALL
SELECT 'orphan_eud_payment',
       COUNT_BIG(*)
FROM dbo.T047_EUD_UhradyOdpocty AS p
LEFT JOIN dbo.T040_EUD AS h ON h.C000_ID = p.C010_IDEUD
WHERE h.C000_ID IS NULL
UNION ALL
SELECT 'unknown_invoice_code',
       COUNT_BIG(*)
FROM dbo.T040_EUD AS e
LEFT JOIN dbo.T025_OkruhyUctovnychDokladov AS k
    ON k.C021_Kod = e.C023_CisloInterneKodOkruh
WHERE ISNULL(e.C001_Vymazany, 0) = 0
  AND e.C023_CisloInterneKodOkruh IN ('DF', 'OF', 'DPF', 'OPF', 'zDF', 'zOF')
  AND k.C021_Kod IS NULL;
