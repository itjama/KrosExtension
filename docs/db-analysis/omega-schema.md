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
| `C117_Den...`, `C118_Mes...`, `C119_Rok...` Poslednej opravy | Dátum poslednej zmeny — **nie** dátum zaúčtovania, môže byť resetnutý hromadnou úpravou |
| `C120_DenUzavierkyHLK`, `C121_Mes...`, `C122_Rok...` | **Dátum zaúčtovania do HLK (Hlavnej knihy)** — správny dátum pre PostingLag výpočet |
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
- FK: `C009_IDEUDPolozka` → `T041_EUD_Polozky` (riadok dokladu, ku ktorému patrí)
- Orphan detection: LEFT JOIN kde T040_EUD.C000_ID IS NULL

| Stĺpec | Typ | Popis |
|--------|-----|-------|
| `C000_ID` | int | PK |
| `C002_Oznaceny` | smallint | |
| `C003_Zamknuty` | smallint | |
| `C009_IDEUDPolozka` | int | FK → T041_EUD_Polozky |
| `C010_IDEUD` | int | FK → T040_EUD.C000_ID |
| `C021_CisloInterneTypOkruh` | smallint | |
| `C023_CisloInterneKodOkruh` | varchar | Kód okruhu (DF, OF, BV...) |
| `C024_CisloInterneKodEvidencia` | varchar | |
| `C025_CisloInterneKodCiselnaRada` | varchar | |
| `C030_CisloInterne` | varchar | Interné číslo dokladu |
| `C060_Den`, `C061_Mes`, `C062_Rok` | smallint | Dátum (3 stĺpce — CONCAT + TRY_CONVERT) |
| `C082_Cas` | varchar | Čas |
| `C097_Poznamka` | text | Poznámka |
| `C100_Typ` | smallint | Typ úhrady/odpočtu |
| `C101_SposobPridania` | smallint | |
| `C102_CRWPriznak` | smallint | |
| `C103_VzajomnyZapocet` | smallint | Vzájomný zápočet flag |
| `C106_MDSyntetickyUcet` | varchar | MD syntetický účet |
| `C107_MDAnalytickyUcet` | varchar | MD analytický účet |
| `C108_DALSyntetickyUcet` | varchar | DAL syntetický účet |
| `C109_DALAnalytickyUcet` | varchar | DAL analytický účet |
| `C130–C134_CisloInyDoklad*` | mix | Odkaz na iný doklad (typ, kód, evidencia, čís. rada, číslo) |
| `C150_PriznakSLD` | smallint | |
| `C151_RucneVysporiadanie` | smallint | |
| `C152_PreSLDOpacneZnamienko` | smallint | |
| `C153_PodTyp` | smallint | |
| `C200_Mena` | varchar | Mena |
| `C201_MnozstvoJednotky` | smallint | |
| `C202_KurzNBS` | float | Kurz NBS |
| `C203_KurzBanka` | float | Kurz banky |
| `C210_SumaSpoluZahranicnaMena` | money | Suma v cudzej mene |
| `C211_SumaSpolu` | money | **Suma celkom v EUR** |
| `C212_Ciastka3M` | money | Čiastka 3. mena |
| `C214_ZakladDPHNizsi` | money | Základ DPH — nižšia sadzba |
| `C215_ZakladDPHVyssi` | money | Základ DPH — vyššia sadzba |
| `C216_ZakladDPHNulovy` | money | Základ DPH — nulová sadzba |
| `C217_ZakladDPHNeobsahuje` | money | Základ — neobsahuje DPH |
| `C218_SumaDPHNizsia` | money | Suma DPH — nižšia sadzba |
| `C219_SumaDPHVyssia` | money | Suma DPH — vyššia sadzba |
| `C221_SumaSpoluNizsia` | money | Suma celkom — nižšia sadzba |
| `C222_SumaSpoluVyssia` | money | Suma celkom — vyššia sadzba |
| `C240_Mena3M` | varchar | Mena 3. mena |
| `C241_MnozstvoJednotky3M` | smallint | |
| `C242_KurzNBS3M` | float | Kurz NBS pre 3. menu |
| `C257_ZakladDPHZnizeny2` | money | Základ DPH — znížená 2 |
| `C258_SumaDPHZnizena2` | money | Suma DPH — znížená 2 |
| `C259_SumaSpoluZnizena2` | money | Suma celkom — znížená 2 |
| `C261_ZakladDPHZnizeny3` | money | Základ DPH — znížená 3 |
| `C262_SumaDPHZnizena3` | money | Suma DPH — znížená 3 |
| `C263_SumaSpoluZnizena3` | money | Suma celkom — znížená 3 |
| `C008_IDEUDPridal` | int | FK → T004_Uzivatelia (kto pridal) |
| `C011_IDStredisko` | int | Stredisko |
| `C012_IDZakazka` | int | Zákazka |
| `C013_IDCinnost` | int | Činnosť |
| `C014_IDPracovnik` | int | Pracovník |

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
