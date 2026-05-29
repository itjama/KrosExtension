# Mapa zdrojov dat pre buducu aplikaciu

Tento dokument popisuje iba polia potrebne na meranie aktivity. Nie je
ucelom kopirovat osobne, mzdove ani financne udaje z databaz.

## Predpoklad nasadenia

Jedna databaza OMEGA alebo OLYMP reprezentuje jednu spracovavanu firmu.
Backend ma mat vlastny konfiguracny zoznam databaz:

| Pole konfiguracie | Vyznam |
| --- | --- |
| `CompanyId` | Interne ID firmy aplikacie; nevytahovat z uctovnych dat. |
| `DisplayName` | Nazov zobrazovany vo frontende, udrziavany mimo zdrojovej DB. |
| `Product` | `OMEGA` alebo `OLYMP`. |
| `ConnectionRef` | Bezpecny odkaz na read-only pripojenie. |
| `Enabled` | Ci sa databaza ma reportovat. |

Tym sa backend vyhne citaniu adresarov partnerov, firemnych identifikatorov a
osobnych udajov len kvoli pomenovaniu firmy.

## Rozpoznanie typu databazy

Backend moze pri registracii databazy overit podpis tabuliek:

| Produkt | Povinne tabulky pre podporovane metriky |
| --- | --- |
| OMEGA | `dbo.T040_EUD`, `dbo.T025_OkruhyUctovnychDokladov` |
| OLYMP | `dbo.activityLog` |

SQL sablona: `sql/classify-database.sql`.

Capability metrik sa overuju aj na urovni potrebnych stlpcov:
`HasOmegaRegisteredActivity`, `HasOmegaInvoicingAgenda`,
`HasOmegaRecordedAttribution`, `HasOmegaOriginAndStage`,
`HasOlympApplicationActivity`, `HasOlympUserActivity` a
`HasOlympPayrollPeriods`. Backend smie spustit dotaz iba vtedy, ked jeho
capability vysla ako dostupna.

## OMEGA

### Evidencia uctovnych dokladov

Primarny zdroj pre aktivitu faktur je `dbo.T040_EUD`.

| Stlpec | Pouzitie v aplikacii |
| --- | --- |
| `C000_ID` | Technicky identifikator dokladu, iba pre vazby a kontrolu duplicity. |
| `C001_Vymazany` | Filter; do metrik patria iba zaznamy s `ISNULL(..., 0) = 0`. |
| `C023_CisloInterneKodOkruh` | Kod druhu dokladu, napr. `DF`, `OF`. |
| `C114_DenZaevidovania`, `C115_MesZaevidovania`, `C116_RokZaevidovania` | Datum aktivity: kedy bol doklad zaevidovany. |
| `C060_DenVystavenia`, `C061_MesVystavenia`, `C062_RokVystavenia` | Datum dokladu; vhodny na ekonomicky prehlad, nie na meranie vykonanej prace. |

Kod dokladu sa popisuje cez `dbo.T025_OkruhyUctovnychDokladov`:

| Kod | Potvrdeny nazov v databaze | Odporucana metrika |
| --- | --- | --- |
| `DF` | Dosle faktury | `omega.received_invoice.registered` |
| `OF` | Odoslane faktury | `omega.sent_invoice.registered` |
| `DPF` | Dosle preddavkove faktury | `omega.received_advance_invoice.registered` |
| `OPF` | Odoslane preddavkove faktury | `omega.sent_advance_invoice.registered` |
| `zDF` | `(z) Dosle faktury` | Zobrazovat oddelene, kym nebude potvrdeny obchodny vyznam. |
| `zOF` | `(z) Odoslane faktury` | Zobrazovat oddelene, kym nebude potvrdeny obchodny vyznam. |

Poznamka: nazvy v tabulke vyssie su v dokumente bez diakritiky; v databazovom
ciselniku su ulozene s diakritikou.

### Implicitne vazby

V analyzovanej verzii nie su deklarovane SQL cudzie kluce. Overene datove
vazby su:

| Detailova tabulka | Vazobny stlpec | Rodic | Overenie vzorky |
| --- | --- | --- | --- |
| `T041_EUD_Polozky` | `C010_IDEUD` | `T040_EUD.C000_ID` | 2525 / 2525 riadkov sa sparovalo. |
| `T047_EUD_UhradyOdpocty` | `C010_IDEUD` | `T040_EUD.C000_ID` | 419 / 419 riadkov sa sparovalo. |
| `T048_EUD_Evidencia_DPH` | `C010_IDEUD` | `T040_EUD.C000_ID` | Vo vzorke prazdna tabulka. |

Pre zakladnu intenzitu prace nie je potrebne tieto detailove tabulky citat.
Vhodne su az pre rozsirenia ako pocet uhrad alebo DPH operacii.

### Atribucia na pouzivatela

`T040_EUD` ma vyplnene polia `C028_KodZaevidoval` a `C027_KodZauctoval`.
V dodanej vzorke maju sledovane fakturacne typy hodnotu v oboch poliach pri
vsetkych dokladoch. Je teda mozne pripravit detail "podla osoby uvedenej na
doklade".

Tieto kody sa vsak v tejto vzorke nezhoduju s `T004_Uzivatelia.C100_Meno`.
Nie je potvrdene, ze ide o prihlaseneho pouzivatela, a nastavenia
`T004_Uzivatelia` navyse naznacuju, ze pole moze byt editovatelne. Tento
detail sa preto nema oznacovat ako audit cinnosti konkretneho loginu.

`T103_VymazaneOpravene` obsahuje `C011_IDUzivatel` pre opravy/vymazania. V
dodanej vzorke sa na sledovane fakturacne doklady naviazalo iba 26 auditnych
riadkov, preto nejde o kompletny zdroj vytvorenia dokladov.

`T004_Uzivatelia` obsahuje aj prihlasovacie polia vratane hesla. Buduci
backend ju nema citat cez `SELECT *` ani poskytovat frontendu.

### Oddelena fakturacna agenda

`dbo.T228_Faktury` obsahuje fakturacnu agendu s kodom v
`C022_KodEvidencia` a datumom vystavenia v poliach `C060_DenVyst`,
`C061_MesVyst`, `C062_RokVyst`.

Tato tabulka sa nesmie scitat s `T040_EUD`:

| Kod | `T228_Faktury` | `T040_EUD` | Zhoda podla cisla dokladu |
| --- | ---: | ---: | ---: |
| `OF` | 35 | 14 | 10 |
| `OPF` | 1 | 2 | 1 |

Ak bude aplikacia zobrazovat tvorbu faktur v agende `T228`, musi to byt
samostatna metrika, napr. `omega.sent_invoice.created_in_invoicing`, nie
sucet k zaevidovanym uctovnym dokladom.

### Povod dokladu a stav spracovania

V `T040_EUD` existuju kandidati pre povod a stav dokladu:

| Stlpec | Predbezny vyznam | Stav overenia |
| --- | --- | --- |
| `C138_PovodDokladu` | Kod povodu dokladu. | V celej vzorke iba hodnota `0`; mapovanie na manualny/importovany nie je potvrdene. |
| `C187_PlatformaId` | ID platformy pri externom povode. | Vo vzorke nepouzite. |
| `C149_ImportText` | Textovy priznak importu. | Vo vzorke nepouzite. |
| `C101_MaPripojeneSubory` | Doklad ma prilohu. | Vo vzorke nepouzite; prilohu netreba zamienat za povod. |
| `C108_PREdkontovany` | Priznak predkontacie. | Vyplneny pri vsetkych sledovanych fakturach. |
| `C027_KodZauctoval` | Atribucia zodpovedna za zauctovanie. | Vyplneny pri vsetkych sledovanych fakturach. |

Schema teda umoznuje pripravit rozdelenie podla `OriginCode` a stavu
spracovania, ale oznacenie "import z Digitalnej kancelarie" sa smie zapnut az
po overeni na databaze, ktora takyto import skutocne obsahuje.

## OLYMP

### Aktivita v aplikacii

Primarny bezpecny zdroj pre intenzitu pouzivania je `dbo.activityLog`.

| Stlpec | Pouzitie v aplikacii |
| --- | --- |
| `StartTime` | Datum/cas aktivity, z ktoreho sa agreguje den. |
| `EndTime` | Volitelne pre buducu metriku trvania; zatial nepouzivat. |
| `ActivityType` | Technicky typ aktivity; typy nemaju vo vzorke nalezeny ciselnik. |

Stlpce `UserName`, `FullName`, `PcUser`, `PcName` a `AdditionalInfo` backend
pre agregovany dashboard nesmie citat ani vracat.

Vo vzorke su typy `11`, `12` a `14`. Bez potvrdenia vyznamu sa vo frontende
ma zobrazovat hlavne suhrn `olymp.application_activity`, nie nazvy
jednotlivych typov.

Pre autorizovany detail je mozne agregovat `activityLog` podla `UserName`.
Vo vzorke je 499 aktivit od 2 pouzivatelov a vsetky aktivity maju vyplneny
`UserName`. Netreba pritom citat `A_Uzivatelia`, ktora obsahuje aj hesla a
dalsie prihlasovacie/secrets polia.

### Spracovane mzdove obdobia

`ID_obdobie` je technicky mesiac vo formate `YYYYMM`. Vo vzorke maju tabulky
`pracVycBase`, `pracVycMzda` a `pracVycOdvod` vystupy pre 29 mesiacov,
`202401` az `202605`.

| Tabulka | Vyznam pre metriku |
| --- | --- |
| `pracVycMzda` | Existencia aspon jedneho riadku pre `ID_obdobie` potvrdzuje vypocitany mzdovy mesiac. |
| `pracVycBase` | Kontrolny zdroj existencie vypoctu. |
| `pracVycOdvod` | Kontrolny zdroj existencie vypoctu odvodov. |
| `stavVyplatyMes` | V dodanej vzorke prazdna; zatial nepouzivat. |

Odporucana verejna metrika je `olymp.payroll_period.processed`: jeden bod za
obdobie, v ktorom existuje vystup v `pracVycMzda`. Pocet vystupnych riadkov
by mohol nepriamo odhalovat pocet pracovnikov, preto nemusi byt sucastou API
pre bezny dashboard.

## Tabulky, ktore dashboard nema citat

| Produkt | Priklady | Dovod |
| --- | --- | --- |
| OMEGA | `T020_Partner`, partner adresy/kontakty/bankove ucty | Identifikacne a kontaktne udaje. |
| OLYMP | `pracovnici`, mzdove hodnoty v `pracVycMzda`, odvody, ucty, zrazky | Osobne a mzdove udaje. |
