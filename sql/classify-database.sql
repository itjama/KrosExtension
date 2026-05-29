/*
Run in the target company database.
Returns a product only when the minimum supported table signature exists.
This query reads schema metadata only.
*/
SELECT CASE
    WHEN OBJECT_ID(N'dbo.T040_EUD', N'U') IS NOT NULL
     AND OBJECT_ID(N'dbo.T025_OkruhyUctovnychDokladov', N'U') IS NOT NULL
        THEN 'OMEGA'
    WHEN OBJECT_ID(N'dbo.activityLog', N'U') IS NOT NULL
        THEN 'OLYMP'
    ELSE 'UNSUPPORTED'
END AS Product,
CASE WHEN OBJECT_ID(N'dbo.T040_EUD', N'U') IS NOT NULL
       AND COL_LENGTH(N'dbo.T040_EUD', N'C001_Vymazany') IS NOT NULL
       AND COL_LENGTH(N'dbo.T040_EUD', N'C023_CisloInterneKodOkruh') IS NOT NULL
       AND COL_LENGTH(N'dbo.T040_EUD', N'C114_DenZaevidovania') IS NOT NULL
       AND COL_LENGTH(N'dbo.T040_EUD', N'C115_MesZaevidovania') IS NOT NULL
       AND COL_LENGTH(N'dbo.T040_EUD', N'C116_RokZaevidovania') IS NOT NULL
     THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS HasOmegaRegisteredActivity,
CASE WHEN OBJECT_ID(N'dbo.T040_EUD', N'U') IS NOT NULL
       AND COL_LENGTH(N'dbo.T040_EUD', N'C028_KodZaevidoval') IS NOT NULL
       AND COL_LENGTH(N'dbo.T040_EUD', N'C027_KodZauctoval') IS NOT NULL
     THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS HasOmegaRecordedAttribution,
CASE WHEN OBJECT_ID(N'dbo.T040_EUD', N'U') IS NOT NULL
       AND COL_LENGTH(N'dbo.T040_EUD', N'C138_PovodDokladu') IS NOT NULL
       AND COL_LENGTH(N'dbo.T040_EUD', N'C187_PlatformaId') IS NOT NULL
       AND COL_LENGTH(N'dbo.T040_EUD', N'C108_PREdkontovany') IS NOT NULL
       AND COL_LENGTH(N'dbo.T040_EUD', N'C027_KodZauctoval') IS NOT NULL
     THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS HasOmegaOriginAndStage,
CASE WHEN OBJECT_ID(N'dbo.T228_Faktury', N'U') IS NOT NULL
       AND COL_LENGTH(N'dbo.T228_Faktury', N'C022_KodEvidencia') IS NOT NULL
       AND COL_LENGTH(N'dbo.T228_Faktury', N'C060_DenVyst') IS NOT NULL
       AND COL_LENGTH(N'dbo.T228_Faktury', N'C061_MesVyst') IS NOT NULL
       AND COL_LENGTH(N'dbo.T228_Faktury', N'C062_RokVyst') IS NOT NULL
     THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS HasOmegaInvoicingAgenda,
CASE WHEN OBJECT_ID(N'dbo.activityLog', N'U') IS NOT NULL
       AND COL_LENGTH(N'dbo.activityLog', N'StartTime') IS NOT NULL
     THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS HasOlympApplicationActivity,
CASE WHEN OBJECT_ID(N'dbo.activityLog', N'U') IS NOT NULL
       AND COL_LENGTH(N'dbo.activityLog', N'StartTime') IS NOT NULL
       AND COL_LENGTH(N'dbo.activityLog', N'UserName') IS NOT NULL
     THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS HasOlympUserActivity,
CASE WHEN OBJECT_ID(N'dbo.pracVycMzda', N'U') IS NOT NULL
       AND COL_LENGTH(N'dbo.pracVycMzda', N'ID_obdobie') IS NOT NULL
       AND OBJECT_ID(N'dbo.pracVycBase', N'U') IS NOT NULL
       AND COL_LENGTH(N'dbo.pracVycBase', N'ID_obdobie') IS NOT NULL
       AND OBJECT_ID(N'dbo.pracVycOdvod', N'U') IS NOT NULL
       AND COL_LENGTH(N'dbo.pracVycOdvod', N'ID_obdobie') IS NOT NULL
     THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS HasOlympPayrollPeriods;
