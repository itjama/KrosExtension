# SQL podklady pre backend

Sablony v tomto priecinku su urcene na vykonanie v kontexte jednej cielovej
firemnej databazy cez read-only pripojenie.

## Poradie pouzitia

1. Pri registracii zdroja spustit `classify-database.sql`.
2. Podla produktu spustit prislusny dotaz v `validation/`.
3. Ak validacia nema kriticke chyby, povolit metricke dotazy v
   `omega/` alebo `olymp/`.

Volitelne dotazy sa spustaju iba pri prislusnej capability:

| Capability | Dotazy |
| --- | --- |
| `HasOmegaRegisteredActivity` | `omega/activity-by-day.sql`, `validation/omega-checks.sql` |
| `HasOmegaRecordedAttribution` | `omega/activity-by-recorded-user.sql` |
| `HasOmegaOriginAndStage` | `omega/origin-stage-summary.sql` |
| `HasOmegaInvoicingAgenda` | `omega/invoicing-by-day.sql`, `validation/omega-invoicing-overlap.sql` |
| `HasOlympApplicationActivity` | `olymp/activity-by-day.sql` |
| `HasOlympUserActivity` | `olymp/activity-by-user.sql` |
| `HasOlympPayrollPeriods` | `olymp/payroll-periods.sql`, `validation/olymp-checks.sql` |

## Parametre metrickych dotazov

Backend musi viazat parametre ako SQL parametre, nie ich skladat do textu:

| Parameter | Typ | Vyznam |
| --- | --- | --- |
| `@FromDate` | `date` | Zaciatok intervalu vratane. |
| `@ToDate` | `date` | Koniec intervalu vratane. |

## Ochrana udajov

Zakladne dotazy nevracaju osoby, partnerov, sumy, bankove udaje, poznamky
ani mena pouzivatelov. Vsetky vysledky maju byt agregovane pred odoslanim do
frontendu.

Dotazy `activity-by-recorded-user.sql` a `activity-by-user.sql` su vynimka
urcena len pre autorizovany detail podla pouzivatela. Vrateny
`RecordedUserKey` je osobny udaj; backend ho nesmie zapisovat do beznych
logov ani spristupnit pouzivatelovi bez prislusneho opravnenia.
