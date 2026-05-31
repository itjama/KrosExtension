# Analýza metrík — overenie existujúcich a návrh nových
> Cieľ: sledovať, či sa účtovníctvo a mzdy spracúvajú priebežne

---

## OMEGA — Overenie existujúcich metrík

### ✅ Korektné metriky

| Metrika | Hodnotenie | Poznámka |
|---------|-----------|----------|
| **Activity7Days / Activity30Days** | ✅ Správne | Súčet zaevidovaných + zaúčtovaných. Mierny problém: zaúčtované doklady zaevidované v okne sú počítané dvakrát (raz ako Registered, raz ako Posted). Štatisticky zanedbateľné. |
| **BacklogCount** | ✅ Správne | C027_KodZauctoval je NULL/prázdny = nezaúčtovaný. Korektné. |
| **Backlog7DaysPlus / 30DaysPlus** | ✅ Správne | Filtrovanie podľa RegisteredDate ≤ dátum - 7/30 dní. Korektné. |
| **PostingRate30Days** | ✅ Správne | Posted/Registered za 30 dní. |
| **StornoRate30Days** | ✅ Správne | zDF+zOF vs DF+OF+DPF+OPF. Správny rozsah okruhu. Môže prekročiť 100% (10 storno, 1 originál = 1000%). |
| **PreviousMonthPostingRate** | ✅ Správne | Zaúčtovanie za predchádzajúci mesiac. |
| **HistoricalPostingRate** | ✅ Správne | Všetky mesiace pred predchádzajúcim. |
| **PreAccountedStuck** | ✅ Správne | C108_PREdkontovany ≠ 0 AND nie zaúčtovaný. |
| **NullDateCount** | ✅ Správne | RegisteredDate IS NULL. |
| **OrphanLineCount** | ✅ Správne | T041_EUD_Polozky bez headra. |
| **OrphanPaymentCount** | ✅ Správne | T047_EUD_UhradyOdpocty bez headra. |
| **UnknownCircuitCount** | ✅ Správne | Doklady s kódom okruhu nie v T025. |

### ⚠ Nedostatky existujúcich metrík

1. **Activity počíta len DF/OF/DPF/OPF/zDF/zOF** — BV (bankové výpisy), ID (interné doklady), PD (pokladňa) sú vylúčené. Toto je zámerné (core účtovné doklady), ale znamená to, že firma len s bankovými výpismi bude mať Activity=0.

2. **IMPORT_ONLY warning** — kontroluje Platform30Days + Imported30Days > 0 AND Posted30Days = 0. Ale Posted30Days = 0 môže nastať aj legitímne (nová firma, žiadne zaúčtovanie). Mohlo by byť presnejšie.

3. **TrendPercent** — porovnáva Activity7Days (aktuálna 7d) vs Previous7DaysActivity (predchádzajúca 7d). Obidve zahŕňajú Registered + Posted — konzistentné.

---

## OLYMP — Overenie existujúcich metrík

### ✅ Korektné metriky

| Metrika | Hodnotenie | Poznámka |
|---------|-----------|----------|
| **Activity7Days/30Days** | ✅ Správne | activityLog.StartTime — priame prihlásenia/akcie |
| **NullStartTimeCount** | ✅ Správne | Indikátor korupcie záznamu |
| **LastProcessedPeriod** | ✅ Správne | MAX(ID_obdobie) z union pracVycMzda/Base/Odvod |
| **CurrentMonthProcessed** | ✅ Správne | MAX(ID_obdobie) >= CurrentPeriod |
| **MonthsBehind** | ✅ Správne | (rok×12+mes) rozdiel. Upozornenie pri ≥2 mesiacoch |
| **PeriodConsistency** | ✅ Správne | Každé obdobie musí byť vo všetkých 3 tabuľkách |
| **MissingPeriodsCount** | ✅ Správne | Chýbajúce mesiace v rade od začiatku roku |

### ⚠ Nedostatky existujúcich metrík

1. **CURRENT_MONTH_MISSING** — trigguje len po 15. dni mesiaca. Pred 15. je normálne, že aktuálny mesiac nie je spracovaný. Toto je správne.

2. **LOW_ACTIVITY** — porovnáva 7d vs 30d priemerný týždeň. Problem: 30d/4 môže byť skreslený ak bol napríklad dovolenka celý mesiac. Ale je to hrubá metrika, postačí.

3. **Activity metrí len activityLog** — nepodchytáva či prebieha skutočná práca (výpočet miezd, export). Firma môže byť "aktívna" (prihlásia sa) ale nič nespočíta.

---

## Návrh nových metrík

### OMEGA — Nové metriky

#### 1. Posting Lag (priemernú oneskorenie zaúčtovania)
**Čo meria:** Priemerný počet dní od zaevidovania po zaúčtovanie za posledných 30 dní.
**Prečo:** Firma môže mať 100% zaúčtovanosť ale zaúčtovávať s 60-dňovým oneskorením.
**SQL základ:**
```sql
AVG(DATEDIFF(day,
  TRY_CONVERT(date, CONCAT(C116_Rok,'-',C115_Mes,'-',C114_Den)),
  TRY_CONVERT(date, CONCAT(C119_Rok,'-',C118_Mes,'-',C117_Den))
))
-- WHERE IsPosted=1 AND RegisteredDate BETWEEN @From30 AND @Today
```
**Prahové hodnoty:** 0–3 dní = výborne, 4–7 = OK, 8–14 = varovanie, >14 = problém.
**Warning:** `HIGH_POSTING_LAG` keď priemerný lag > 7 dní

#### 2. Overdue Backlog (nezaúčtované faktúry po splatnosti)
**Čo meria:** Počet nezaúčtovaných dokladov (DF/OF/DPF/OPF), ktorým uplynula splatnosť.
**Prečo:** Existujúci BacklogCount meria všetky nezaúčtované. Táto metrika identifikuje kritické — po splatnosti.
**SQL základ:**
```sql
SUM(CASE WHEN IsPosted=0 
  AND TRY_CONVERT(date, CONCAT(C068_Rok,'-',C067_Mes,'-',C066_Den)) < CAST(@Today AS date)
  AND DocumentCode IN ('DF','OF','DPF','OPF')
  THEN 1 ELSE 0 END) AS OverdueBacklog
```
**Warning:** `OVERDUE_BACKLOG` keď OverdueBacklog > 0

#### 3. Duplicate External Number Detection
**Čo meria:** Počet prípadov kde rovnaká firma (C005_IDFirma_Partner) má viac dokladov s rovnakým externým číslom (C031_CisloExterne).
**Prečo:** Duplikátne faktúry — zaúčtovanie tej istej faktúry dvakrát.
**SQL základ:**
```sql
SELECT COUNT(*) FROM (
  SELECT C005_IDFirma_Partner, C031_CisloExterne
  FROM T040_EUD
  WHERE C023 IN ('DF','OF') AND C031_CisloExterne != '' AND C001_Vymazany=0
  GROUP BY C005_IDFirma_Partner, C031_CisloExterne
  HAVING COUNT(*) > 1
) AS dups
```
**Warning:** `DUPLICATE_INVOICES`

#### 4. Unmatched Receivables Age
**Čo meria:** Maximálny vek faktúry, ktorá má zostatok > 0 (C211 - C220 - C224 > 0) a pritom je zaúčtovaná. Toto sú pohľadávky, ktoré dlho nikto nevybral.
**SQL základ:**
```sql
MAX(DATEDIFF(day, RegisteredDate, @Today)) 
WHERE IsPosted=1 AND (C211_SumaSpolu - ISNULL(C220_Uhradene,0) - ISNULL(C224_Odpocitane,0)) > 0.01
```
**Note:** Vyžaduje overenie — v tejto DB sú všetky doklady zaúčtované, takže zostatok by mal byť 0.

---

### OLYMP — Nové metriky

#### 1. Payroll Export Status (výplaty odoslané)
**Čo meria:** Či sú výplaty za posledné spracované obdobie exportované/odoslané (bankPrevody.uhradene).
**Prečo:** Firma môže mať mzdy spočítané (pracVycMzda vyplnené) ale výplaty nevykonané.
**SQL základ:**
```sql
SELECT 
  COUNT(*) AS TotalPayments,
  SUM(CASE WHEN uhradene=1 THEN 1 ELSE 0 END) AS SentPayments
FROM bankPrevody WHERE X_obdobie = @LastPeriod
```
**Warning:** `PAYROLL_NOT_EXPORTED` keď TotalPayments > 0 AND SentPayments = 0
**Note:** V testovacej DB sú všetky uhradene=0 — overit či firma vôbec exportuje cez Olymp (niektoré to robia cez internet banking priamo).

#### 2. Payroll Accounting Status (zaúčtovanie miezd)
**Čo meria:** Či sú mzdy za posledné obdobie zaúčtované do účtovníctva (zauctovanie tabuľka).
**Prečo:** Mzdy musia byť nielen vypočítané ale aj zaúčtované — prepojenie Olymp → Omega. Zauctovanie tabuľka v testovacej DB je prázdna.
**SQL základ:**
```sql
SELECT COUNT(*) FROM zauctovanie WHERE ID_obdobie = @LastPeriod
-- 0 = nezaúčtované
```
**Warning:** `PAYROLL_NOT_ACCOUNTED` keď 0 záznamov pre posledné obdobie
**Note:** Overit sémantiku — mohlo by byť že zaúčtovanie prebieha len raz ročne alebo sa nepoužíva.

#### 3. Employee Count Change
**Čo meria:** Zmena počtu zamestnancov oproti predchádzajúcemu mesiacu.
**Prečo:** Nástup/odchod zamestnancov môže vysvetliť zmeny v mzdových sumách.
**SQL základ:**
```sql
SELECT ID_obdobie, COUNT(DISTINCT ID_osoby) AS PocetZam FROM pracVycMzda
WHERE ID_obdobie IN (@LastPeriod, @PrevPeriod) GROUP BY ID_obdobie
```
**Použitie:** Informatívna metrika (zobrazenie v detaile), nie varovanie.

#### 4. Absence Rate (nemocnosť a dovolenky)
**Čo meria:** Podiel hodín nemoci (hodNemoc) z celkových odpracovateľných hodín.
**Prečo:** Vysoká nemocnosť môže indikovať problémy, ale aj vysvetliť nízku aktivitu.
**SQL základ:**
```sql
SELECT 
  SUM(hodNemoc) * 100.0 / NULLIF(SUM(hodOdprac + hodNemoc), 0) AS AbsenceRate
FROM pracVycMzda WHERE ID_obdobie = @LastPeriod
```

#### 5. Payroll Command Delay (oneskorenie vytvorenia príkazov)
**Čo meria:** Počet dní medzi koncom mzdového obdobia a dátumom vystavenia bankPrevPrik.
**Prečo:** Normálne by výplaty mali byť odoslané do 5. dňa nasledujúceho mesiaca. Oneskorenie je problém.
**SQL základ:**
```sql
-- bankPrevPrik.vystavene je int YYYYMMDD
-- @LastPeriod je YYYYMM
DATEDIFF(day, 
  DATEFROMPARTS(@LastPeriod / 100, @LastPeriod % 100, 1), -- prvý deň obdobia
  TRY_CONVERT(date, CAST(vystavene AS varchar(8)))         -- dátum vystavenia príkazu
) - DAY(EOMONTH(DATEFROMPARTS(@LastPeriod/100, @LastPeriod%100, 1))) -- mínus počet dní v mesiaci
```
**Reálne dáta:** 202604 vystavený 20260506 = ~5 dní po konci apríla — OK.
**Warning:** `PAYROLL_PAYMENT_DELAYED` keď oneskorenie > 10 dní.

---

## Prioritizácia implementácie

### Vysoká priorita (priamy dopad na monitoring)
1. **OMEGA: HIGH_POSTING_LAG** — firma zaúčtuje neskoro, ale reporting to nevidí
2. **OMEGA: OVERDUE_BACKLOG** — nezaúčtované faktúry po splatnosti = financiálne riziko
3. **OLYMP: PAYROLL_NOT_EXPORTED** — výplaty nespracované

### Stredná priorita
4. **OLYMP: PAYROLL_NOT_ACCOUNTED** — prepojenie mzdy→účtovníctvo (overiť sémantiku)
5. **OLYMP: PAYROLL_PAYMENT_DELAYED** — oneskorené vyplatenie miezd
6. **OLYMP: Employee Count Change** — informatívne, pre detailový pohľad

### Nízka priorita (data quality)
7. **OMEGA: DUPLICATE_INVOICES** — duplikátne faktúry
8. **OLYMP: ABSENCE_RATE** — nemocnosť
