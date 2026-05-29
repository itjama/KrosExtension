# Datovy kontrakt pre buduci backend

## Ciel

Backend sa pripoji read-only priamo ku kazdej povolenej databaze OMEGA alebo
OLYMP a frontendu poskytne iba casove agregacie aktivity. Aplikacia nema byt
vseobecny prehliadac databazy.

## Pravidla citania

1. Firma sa identifikuje konfiguraciou backendu (`CompanyId`,
   `DisplayName`), nie citanim osobnych alebo firemnych poli zo zdrojovej DB.
2. Kazda databaza sa pri pridani overi cez podpis tabuliek a dostupne
   capabilities potrebnych stlpcov; ulozi sa jej produkt/verzia podporovaneho
   modelu.
3. SQL ucet ma mat iba `SELECT` na povolene tabulky alebo na osobitne views.
4. Endpointy nikdy nevracaju riadky dokladov, osoby, sumy, cisla uctov,
   poznamky, mena pouzivatelov ani pracovnych stanic.
5. OMEGA `T040_EUD` a `T228_Faktury` su rozdielne metriky a nikdy sa
   automaticky nesumuju.
6. OLYMP pocet riadkov mzdovych vystupov sa nezverejnuje v zakladnom
   dashboarde; reportuje sa existencia spracovaneho obdobia.
7. Detail podla pouzivatela je samostatne autorizovana funkcia, pretoze
   identifikator pouzivatela je osobny udaj.
8. Tabulky pouzivatelov sa nesmu vseobecne nacitavat: vo vzorkach obsahuju
   aj hesla alebo prihlasovacie secrets.

## Katalog metrik

| Metric key | Produkt | Zdroj | Casove pole | Hodnota |
| --- | --- | --- | --- | --- |
| `omega.received_invoice.registered` | OMEGA | `T040_EUD`, kod `DF` | Datum zaevidovania | Pocet nevymazanych dokladov. |
| `omega.sent_invoice.registered` | OMEGA | `T040_EUD`, kod `OF` | Datum zaevidovania | Pocet nevymazanych dokladov. |
| `omega.received_advance_invoice.registered` | OMEGA | `T040_EUD`, kod `DPF` | Datum zaevidovania | Pocet nevymazanych dokladov. |
| `omega.sent_advance_invoice.registered` | OMEGA | `T040_EUD`, kod `OPF` | Datum zaevidovania | Pocet nevymazanych dokladov. |
| `omega.received_invoice_z.registered` | OMEGA | `T040_EUD`, kod `zDF` | Datum zaevidovania | Oddelena docasna metrika. |
| `omega.sent_invoice_z.registered` | OMEGA | `T040_EUD`, kod `zOF` | Datum zaevidovania | Oddelena docasna metrika. |
| `omega.sent_invoice.created_in_invoicing` | OMEGA | `T228_Faktury`, kod `OF` | Datum vystavenia | Volitelna, nesucitat s `registered`. |
| `olymp.application_activity` | OLYMP | `activityLog` | `StartTime` | Pocet zaznamenanych aktivit. |
| `olymp.payroll_period.processed` | OLYMP | `pracVycMzda` | `ID_obdobie` -> mesiac | 1 za spracovany mesiac. |

### Autorizovane pouzivatelske detaily

| Metric key | Produkt | Zdroj | Interpretacia |
| --- | --- | --- | --- |
| `omega.*.registered_by_recorded_user` | OMEGA | `T040_EUD.C028_KodZaevidoval` | Osoba/kod zapisany na doklade ako zaevidoval; nie potvrdeny login audit. |
| `omega.*.accounted_by_recorded_user` | OMEGA | `T040_EUD.C027_KodZauctoval` | Osoba/kod zapisany na doklade ako zauctoval; nie potvrdeny login audit. |
| `olymp.application_activity_by_user` | OLYMP | `activityLog.UserName` | Aktivita zaznamenana pre pouzivatela. |

### OMEGA povod a stav

Pre buducu funkcionalitu "manualne vs. importovane vs. zauctovane" su
k dispozicii polia `T040_EUD.C138_PovodDokladu`, `C187_PlatformaId`,
`C108_PREdkontovany` a `C027_KodZauctoval`. V dodanej databaze su vsetky
fakturacne doklady s `C138_PovodDokladu = 0` a bez platformoveho ID, preto
zatial nemoze API pomenovat kod `0` ako manualny ani iny kod ako import z
Digitalnej kancelarie. Na potvrdenie je potrebna vzorka s aspon jednym
realne importovanym dokladom a idealne aj nezaevidovanym/nepredkontovanym
dokladom.

## Navrh API

### `GET /api/companies`

Vrati iba konfiguracne zaznamy, ktore moze pouzivatel vidiet:

```json
[
  {
    "companyId": "company-001",
    "displayName": "Klient 001",
    "systems": ["OMEGA", "OLYMP"]
  }
]
```

### `GET /api/companies/{companyId}/activity?from=2026-04-01&to=2026-05-25&grain=day`

```json
{
  "companyId": "company-001",
  "from": "2026-04-01",
  "to": "2026-05-25",
  "grain": "day",
  "series": [
    {
      "metric": "omega.received_invoice.registered",
      "date": "2026-05-20",
      "count": 2
    },
    {
      "metric": "olymp.payroll_period.processed",
      "date": "2026-05-01",
      "count": 1
    }
  ]
}
```

### `GET /api/companies/{companyId}/summary?asOf=2026-05-25`

Backend moze den/7 dni/30 dni spocitat zo series alebo SQL dotazom:

```json
{
  "companyId": "company-001",
  "asOf": "2026-05-25",
  "metrics": [
    {
      "metric": "omega.received_invoice.registered",
      "today": 0,
      "last7Days": 5,
      "last30Days": 91
    }
  ]
}
```

### `GET /api/companies/{companyId}/activity/users?from=2026-04-01&to=2026-05-25`

Endpoint je dostupny len pouzivatelom s opravnenim na detail podla osoby:

```json
{
  "companyId": "company-001",
  "series": [
    {
      "metric": "olymp.application_activity_by_user",
      "date": "2026-05-06",
      "recordedUserKey": "user-key-visible-only-to-authorised-users",
      "count": 4
    }
  ]
}
```

## SQL sablony

| Subor | Pouzitie |
| --- | --- |
| `sql/classify-database.sql` | Overi, ci pripojena DB vyzera ako podporovana OMEGA alebo OLYMP. |
| `sql/omega/activity-by-day.sql` | Denne pocty zaevidovanych fakturacnych dokladov. |
| `sql/omega/activity-by-recorded-user.sql` | Autorizovany detail OMEGA podla atributu na doklade. |
| `sql/omega/origin-stage-summary.sql` | Technicke rozdelenie podla povodu/stavu; hodnoty povodu zatial bez UI nazvov. |
| `sql/omega/invoicing-by-day.sql` | Volitelna oddelena metrika faktur vytvorenych vo fakturacnej agende. |
| `sql/olymp/activity-by-day.sql` | Denne pocty zaznamenanych aktivit OLYMP. |
| `sql/olymp/activity-by-user.sql` | Autorizovany detail aktivity OLYMP podla pouzivatela. |
| `sql/olymp/payroll-periods.sql` | Mesiace, v ktorych existuje vypocitany mzdovy vystup. |
| `sql/validation/omega-checks.sql` | Povinne agregovane overenie OMEGA. |
| `sql/validation/omega-invoicing-overlap.sql` | Volitelne overenie prekryvania agend OMEGA. |
| `sql/validation/olymp-checks.sql` | Volitelne overenie mzdovych obdobi OLYMP. |

Vsetky sablony sa maju vykonat priamo v cielovej firemnej databaze. Backend
nesmie skladat nazov databazy z HTTP vstupu do dynamickeho SQL.

## Pristup a bezpecnost

- Vytvorit SQL login/uzivatela s read-only opravneniami len na reportovane
  databazy a povolene tabulky alebo views.
- Connection stringy a hesla ukladat v secrets ulozisku backendu, nie v
  repozitari ani vo frontende.
- Frontend komunikuje len s backendom; nikdy priamo so SQL Serverom.
- Logovanie backendu nesmie zaznamenavat connection stringy ani surove
  vysledky databazovych dotazov.
- Pre produkciu doplnit autorizaciu: ktory pouzivatel smie vidiet ktoru
  firmu.
- Zobrazovanie detailu podla pouzivatela vyzaduje osobitne opravnenie a
  audit pristupov vo vlastnej aplikacii.

## Overenia pred implementaciou

- Overit schemu na viacerych databazach a verziach OMEGA/OLYMP.
- Rozhodnut, ci kod `zDF`/`zOF` zobrazovat alebo skryt.
- Potvrdit, ci volitelna metrika z `T228_Faktury` ma pre pouzivatelov
  hodnotu, kedze sa cast dokladov prekryva s `T040_EUD`.
- Overit vyznam `ActivityType` v OLYMP; kym nie je znamy, nepomenovavat
  jednotlive typy vo frontende.
- Dodat OMEGA vzorku obsahujucu import z Digitalnej kancelarie a doklad v
  rozpracovanom stave na potvrdenie kodov povodu a stavoveho workflow.
