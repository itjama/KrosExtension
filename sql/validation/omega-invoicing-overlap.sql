/*
Run only when classify-database.sql returns HasOmegaInvoicingAgenda = 1.
Informational check: rows matching here must not be summed across agendas.
No document numbers are returned.
*/
SELECT f.C022_KodEvidencia AS Code,
       COUNT_BIG(*) AS MatchingDocumentNumbers
FROM dbo.T228_Faktury AS f
JOIN dbo.T040_EUD AS e
    ON e.C023_CisloInterneKodOkruh = f.C022_KodEvidencia
   AND ISNULL(e.C001_Vymazany, 0) = 0
   AND e.C030_CisloInterne = f.C030_CisloFaktury
WHERE f.C022_KodEvidencia IN ('OF', 'OPF')
GROUP BY f.C022_KodEvidencia
ORDER BY f.C022_KodEvidencia;
