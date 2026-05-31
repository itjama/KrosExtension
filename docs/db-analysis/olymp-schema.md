# Olymp databáza — schéma a analýza
> Vzorová DB: `mzdyCanberra_Packar` (Canberra-Packard, s.r.o.)

## Kľúčové tabuľky

### activityLog — Aktivita používateľov (core)
Každý riadok = jedno prihlásenie/akcia v Olympe.

| Stĺpec | Popis |
|--------|-------|
| `StartTime` | datetime — čas začiatku akcie. NULL = chyba záznamu |
| Ďalšie stĺpce (9 celkom) | menej dôležité pre monitoring |

**Reálne dáta:**
- 4–34 záznamov/mesiac, 1–8 aktívnych dní/mesiac
- Máj 2026: len 4 záznamy (1 deň) — môže byť nízka aktivita

### pracVycMzda — Mzdové výpočty (core)
Jeden riadok = mzda jedného zamestnanca za jedno obdobie.

| Stĺpec | Popis |
|--------|-------|
| `ID_osoby` | FK na pracovnici |
| `ID_obdobie` | YYYYMM formát (napr. 202605) |
| `zaklMzda` | Základná mzda |
| `hrubaMzda` | Hrubá mzda |
| `cistaMzda` | Čistá mzda |
| `hodOdprac` | Odpracované hodiny |
| `hodNadcas` | Nadčasové hodiny |
| `hodNemoc` | Hodiny PN |
| `hodDovol` | Hodiny dovolenky |
| `danZuct` | Zúčtovaná daň |
| `mzdaPlatba` | Suma k výplate |
| `davkyNp`, `davkySt` | Nemocenské, sociálne dávky |

### pracVycBase — Mzdová základňa (odvody)
| Stĺpec | Popis |
|--------|-------|
| `ID_osoby`, `ID_obdobie` | Identifikácia |
| `vypocitane` | Flag — vypočítané |
| `nastavenie1`, `nastavenie2` | Konfigurácia |
| `prPrijem` | Príjem pre odvody |
| `pracHod`, `sviatHod` | Pracovné a sviatkové hodiny |

### pracVycOdvod — Odvody (sociálne, zdravotné)
108 stĺpcov — detailný rozpis všetkých odvodov.

### pracovnici — Zamestnanci
86 stĺpcov — kompletné osobné dáta (meno, adresa, RČ, atď.)
- **22 záznamov** v tejto DB

### pracEvidPP — Pracovnoprávne vzťahy (zmluvy)
| Stĺpec | Popis |
|--------|-------|
| `ID_osoby` | FK na pracovnici |
| `ID_datum` | Dátum záznamu (int, YYYYMMDD formát) |
| `zmenaStavu` | Typ zmeny (nástup, zmena, ukončenie) |
| `ukoncene` | 1 = ukončený PP |
| `typUvazku` | Typ pracovného vzťahu |
| `uvazok` | Úväzok v hodinách |
| `odmena` | Dohodnutá odmena/mzda |
| `sposobKon` | Spôsob ukončenia |

### bankPrevody — Platobné príkazy pre výplaty
| Stĺpec | Popis |
|--------|-------|
| `ID_prevPrik` | FK na bankPrevPrik (skupinu príkazov) |
| `X_obdobie` | YYYYMM |
| `uhradene` | 0=neodoslané, 1=odoslané/zaplatené |
| `ciastka` | Suma výplaty |
| `prijemca` | Meno príjemcu |
| `ucet`, `IBAN` | Číslo účtu/IBAN |

**⚠ Reálne dáta:** `uhradene=0` pre VŠETKY záznamy — výplaty nie sú označené ako odoslané.
Môže byť normálne (firma nepoužíva elektronický export z Olympu) alebo problém.

### bankPrevPrik — Skupiny platobných príkazov
| Stĺpec | Popis |
|--------|-------|
| `X_obdobie` | YYYYMM |
| `vystavene` | Dátum vystavenia (int, YYYYMMDD) |
| `ciastka` | Celková suma skupiny |
| `typPrikazu` | H=hlavný, J=iny (napr. zrážky) |

**Reálne dáta:**
- Príkazy vystavované ~1 mesiac po mzdovom období (napr. 202604 vystavený 20260506)
- Dva typy: H (hlavné výplaty, väčšia suma), J (zrážky/iné, malá suma)

### dovolenky — Dovolenky per zamestnanec per mesiac
| Stĺpec | Popis |
|--------|-------|
| `id_obdobie` | YYYYMM |
| `id_osoby` | FK na pracovnici |
| `navrhnuta` | Navrhnutá dovolenka (dni) |
| `schvalena` | Schválená dovolenka |
| `cerpana` | Vyčerpaná dovolenka |

### zauctovanie — Zaúčtovanie miezd do účtovníctva
| Stĺpec | Popis |
|--------|-------|
| `ID_Uzivatel` | Kto zaúčtoval |
| `ID_obdobie` | YYYYMM |
| `typZrazky` | Typ zrážky |
| `celkom` | Suma |

**⚠ Reálne dáta: PRÁZDNA TABUĽKA** — mzdy nie sú zaúčtované do účtovníctva.

### pracMzdy — Mzdové listy (dlhodobé záznamy)
55 stĺpcov — zhrnutie miezd per zamestnanec (nie per mesiac).

### pracObdobia — Pracovné obdobia per zamestnanec
Odpracované dni/hodiny v mesiaci per zamestnanec.

### pracRocZuct — Ročné zúčtovanie dane
67 stĺpcov — ročné daňové zúčtovanie.

## Reálne dáta — vzorová firma

### Počet zamestnancov po mesiacoch
| Rok/Mes | Počet |
|---------|-------|
| 2024/01–02 | 20 |
| 2024/03 | 19 |
| 2024/04–2025/06 | 18 |
| 2025/07–09 | 19 |
| 2025/10–12 | 18 |
| 2026/01 | 16 ↓ |
| 2026/02–05 | 16–17 |

Trend: mierny pokles (z 20 na ~17 zamestnancov).

### Mzdové obdobia
- Najstaršie: 202401, najnovšie: 202605
- Všetky mesice pokryté kontinuálne — dobré
- pracVycBase/Odvod obsahujú rovnaké obdobia → konzistentné

### Výplaty (bankPrevPrik)
- Vystavené vždy ~4–6 týždňov po mzdovom období
- Typický cyklus: mzdy za 202604 → príkaz vystavený 20260506

## SQL vzory

### Aktuálne obdobie YYYYMM
```sql
DECLARE @CurrentPeriod int = YEAR(GETDATE()) * 100 + MONTH(GETDATE());
```

### Mesiace oneskorenia
```sql
-- LastProcessedPeriod = MAX z pracVycMzda.ID_obdobie
(YEAR(GETDATE()) * 12 + MONTH(GETDATE())) - (lastYear * 12 + lastMonth)
```

### Aktívni zamestnanci v poslednom spracovanom období
```sql
SELECT COUNT(DISTINCT ID_osoby) FROM pracVycMzda WHERE ID_obdobie = @LastPeriod
```

### Výplaty odoslané
```sql
SELECT COUNT(*) FROM bankPrevody WHERE X_obdobie = @Period AND uhradene = 1
-- uhradene=0 znamená: príkaz vytvorený ale ešte neodoslaný/nezaplatený
```

### Zaúčtovanie miezd
```sql
SELECT COUNT(*) FROM zauctovanie WHERE ID_obdobie = @Period
-- 0 = mzdy za toto obdobie ešte nie sú zaúčtované do účtovníctva
```
