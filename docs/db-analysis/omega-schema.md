# Omega databáza — schéma a analýza
> Vzorová DB: `x_720566404` (Canberra-Packard, s.r.o.)

## Kľúčové tabuľky

### T040_EUD — Hlavičky účtovných dokladov (core)
Centrálna tabuľka. Každý riadok = jeden účtovný doklad.

| Stĺpec | Popis |
|--------|-------|
| `C000_ID` | PK |
| `C001_Vymazany` | 0 = aktívny, nenulové = vymazaný (vždy filtrovať) |
| `C023_CisloInterneKodOkruh` | Typ dokladu — kód okruhu (DF, OF, zDF, zOF, BV, ID, PD...) |
| `C027_KodZauctoval` | NULL/prázdny = nezaúčtovaný, vyplnený = zaúčtoval meno usera |
| `C028_KodZaevidoval` | Kto zaevidoval doklad |
| `C108_PREdkontovany` | 0 = nie, nenulové = predkontovaný (pripravený na zaúčtovanie) |
| `C114_DenZaevidovania`, `C115_Mes...`, `C116_Rok...` | Dátum zaevidovania (3 stĺpce — treba CONCAT + TRY_CONVERT) |
| `C117_Den...`, `C118_Mes...`, `C119_Rok...` Poslednej opravy | Dátum poslednej zmeny (proxy pre dátum zaúčtovania) |
| `C060-C062` Vystavenia | Dátum vystavenia dokladu |
| `C066_DenSplatnosti`, `C067_Mes...`, `C068_Rok...` | Dátum splatnosti (3 stĺpce) |
| `C138_PovodDokladu` | Pôvod: 0=manuálny, nenulové=import |
| `C187_PlatformaId` | Prázdny=nie platforma, vyplnený=cez platformu (Filuta, atď.) |
| `C211_SumaSpolu` | Celková suma dokladu v EUR |
| `C220_Uhradene` | Uhradená časť |
| `C224_Odpocitane` | Odpočítaná časť (zápočet) |
| `C227_SumaSpoluZaevidovanaTM` | Suma v cudzej mene |
| `C254_PohladavkaZavazok` | Pohľadávka vs záväzok flag |

### Typy dokladov (C023_CisloInterneKodOkruh)
| Kód | Popis |
|-----|-------|
| `DF` | Doručená faktúra (prijaté faktúry) |
| `OF` | Odoslaná faktúra (vydané faktúry) |
| `DPF` | Doručená predplatová faktúra |
| `OPF` | Odoslaná predplatová faktúra |
| `zDF` | Storno doručenej faktúry |
| `zOF` | Storno odoslanej faktúry |
| `BV` | Bankový výpis |
| `ID` | Interný doklad |
| `PD` | Pokladničný doklad |
| `DD` | Dobropisy doručené |
| `OD` | Dobropisy odoslané |

### T041_EUD_Polozky — Riadky dokladov
- FK: `C010_IDEUD` → `T040_EUD.C000_ID`
- Orphan detection: LEFT JOIN kde T040_EUD.C000_ID IS NULL

### T047_EUD_UhradyOdpocty — Platby a zápočty
- FK: `C010_IDEUD` → `T040_EUD.C000_ID`
- Orphan detection: LEFT JOIN kde T040_EUD.C000_ID IS NULL

### T025_OkruhyUctovnychDokladov — Číselník okruhov
- `C021_Kod` = kód okruhu
- Používa sa pre UNKNOWN_CIRCUIT detekciu

### Ďalšie relevantné tabuľky
| Tabuľka | Popis |
|---------|-------|
| `T004_Uzivatelia` | Používatelia (login, meno) |
| `T003_Zamykanie` | Uzamknuté obdobia |
| `T008_PoslednePouziteID` | Číselné rady pre doklady |

## Reálne dáta — vzorová firma
- **319 DF** (všetky zaúčtované), **41 DPF**, **14 OF**, **2 OPF**
- **344 BV** (bankové výpisy — 338 predkontovaných, všetky zaúčtované)
- **12 storno dokladov** (10×zOF, 2×zDF) — ~3% storno rate → OK
- **0 nezaúčtovaných** — zdravá firma

### Posting lag (reálne)
| Okruh | Priemerný lag | Max lag |
|-------|--------------|---------|
| DF | 33 dní | 542 dní |
| DPF | 36 dní | 171 dní |
| OF | 267 dní | 836 dní |
| OPF | 383 dní | 725 dní |

> OF/OPF majú extrémne vysoký lag — vydané faktúry sa zaúčtovávajú veľmi neskoro (normálne?).

## SQL vzory

### Zaúčtovanosť
```sql
-- IsPosted
CASE WHEN NULLIF(LTRIM(RTRIM(C027_KodZauctoval)),'') IS NULL THEN 0 ELSE 1 END

-- RegisteredDate
TRY_CONVERT(date, CONCAT(C116_RokZaevidovania,'-',C115_MesZaevidovania,'-',C114_DenZaevidovania))

-- SplatnostDate
TRY_CONVERT(date, CONCAT(C068_RokSplatnosti,'-',C067_MesSplatnosti,'-',C066_DenSplatnosti))

-- DueDate vs today (po splatnosti)
TRY_CONVERT(date, CONCAT(C068_RokSplatnosti,'-',C067_MesSplatnosti,'-',C066_DenSplatnosti)) < CAST(GETDATE() AS date)
```

### Zostatok (nezaplatené)
```sql
C211_SumaSpolu - ISNULL(C220_Uhradene,0) - ISNULL(C224_Odpocitane,0) AS Zostatok
```
