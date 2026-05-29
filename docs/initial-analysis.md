# KROS OMEGA / OLYMP - uvodna databazova analyza

## Ochrana udajov

Analyza je zalozena na technickej scheme a agregovanych poctoch. Vystupy nemaju
obsahovat mena, adresy, identifikacne cisla osob, bankove ucty, mzdy, sumy
dokladov ani volne texty.

Obnovene databazove subory a generovane reporty su v `data/` a su vylucene z
Git repozitara.

## Dodane vzorky

| Produkt | Typ | Velkost zalohy | Docasna pracovna DB pri analyze |
| --- | --- | ---: | --- |
| OMEGA | SQL Server `.bak` | 7.63 MB | `Kros_Omega_Pilot` |
| OLYMP | SQL Server `.bak` | 4.41 MB | `Kros_Olymp_Pilot` |

Zalohy boli pocas analyzy obnovene iba lokalne do izolovanej instancie
`(localdb)\KrosAnalysis`. Docasne obnovene databazove subory boli po overeni
SQL podkladov odstranene; vstupne `.bak` zalohy zostali nedotknute.

## OMEGA - potvrdene zistenia

- Schema ma 243 pouzivatelskych tabuliek.
- `T040_EUD` je hlavicka evidencie uctovnych dokladov. Obsahuje kod okruhu,
  partner ID a osobitne casti datumov vystavenia, prijatia, zaevidovania a
  poslednej opravy.
- `T041_EUD_Polozky` obsahuje polozky k `T040_EUD`.
- `T020_Partner` je adresar partnerov a nebude sa citat do agregovanych
  reportov.
- `T228_Faktury` je samostatna fakturacna agenda. Jej zaznamy sa nemozu
  automaticky scitat s `T040_EUD`, pretoze mozu predstavovat ten isty doklad v
  inej casti programu.

Pozorovane kody v `T040_EUD`: `DF`, `OF`, `DPF`, `OPF`, `zDF`, `zOF`.
Ciselnik `T025_OkruhyUctovnychDokladov` potvrdil `DF` ako dosle faktury,
`OF` ako odoslane faktury, `DPF` ako dosle preddavkove faktury a `OPF` ako
odoslane preddavkove faktury. Varianty `zDF` a `zOF` zostavaju v reporte
oddelene.

Pre meranie intenzity prace sa ako prvy indikator pouziva datum
zaevidovania z `T040_EUD`, nie datum vystavenia dokladu.

Agregovane porovnanie podla cisla dokladu v dodanej vzorke potvrdilo
prekrytie: `T228/OF` ma 35 zaznamov, `T040_EUD/OF` 14 zaznamov a 10 z nich
sa zhoduje; pri `OPF` sa zhoduje 1 zaznam. Report preto tieto zdroje uvadza
oddelene a nevytvara z nich sucet.

## OLYMP - potvrdene zistenia

- Schema ma 128 pouzivatelskych tabuliek.
- `activityLog` obsahuje cas zaciatku a konca cinnosti a typ aktivity. V
  dodanej vzorke je 499 udalosti od decembra 2023 do maja 2026.
- `pracMzdy`, `pracObdobia` a `pracObdobiaMes` su kandidati pre neskorsie
  metriky spracovania miezd, ale vyzaduju overenie vyznamu period a
  neduplikovania pracovnych zaznamov.
- `ID_obdobie` vo vypoctovych tabulkach je mesiac vo formate `YYYYMM`.
  `pracVycMzda`, `pracVycBase` a `pracVycOdvod` v dodanej vzorke pokryvaju
  29 spracovanych mesiacov `202401` az `202605`.

Zakladna aplikacna metrika moze pouzit agregaciu `activityLog`. Druha bezpecna
metrika moze oznamit, ci bol dany mesiac mzdovo spracovany, podla existencie
riadku v `pracVycMzda`, bez citania osob alebo mzdovych hodnot.

## Pouzivatel a povod dokladu - doplnene zistenia

- OMEGA `T040_EUD` ma pri vsetkych sledovanych fakturacnych dokladoch
  vyplneny kod `zaevidoval` aj `zauctoval`. Kody vsak nie su overene ako
  login pouzivatelov; sluzia iba na detail atribucie zapisanej na doklade.
- OMEGA `T103_VymazaneOpravene` ma auditne ID pouzivatela pre zmeny, ale na
  sledovane fakturacne doklady sa vo vzorke viaze len 26 riadkov.
- OLYMP `activityLog` obsahuje pouzivatelsky identifikator pri vsetkych 499
  aktivitach; agregovany detail podla pouzivatela je technicky mozny.
- Pouzivatelske tabulky OMEGA aj OLYMP obsahuju prihlasovacie polia vratane
  hesiel/secrets a nesmu byt zdrojom vseobecneho exportu.
- OMEGA obsahuje polia `C138_PovodDokladu` a `C187_PlatformaId`, vhodne pre
  rozlisenie povodu dokladu. V dodanej vzorke ma vsetkych 983 aktivnych EUD
  dokladov povod `0` a ziadny platformove ID ani importny text. Na mapovanie
  Digitalnej kancelarie treba vzorku s realnym importovanym dokladom.

## Prvy reportovaci skript

`scripts/Invoke-KrosActivityReport.ps1`:

- prejde `.bak` subory v `data/input/omega` a `data/input/olymp`,
- obnovi ich do lokalnej analytickej LocalDB,
- pouzije anonymny `SourceId`, nie povodny nazov firmy alebo databazy,
- pre kazdu metriku vypocita celkovy pocet a pocty za den, poslednych 7 dni a
  poslednych 30 dni vzhladom na datum vytvorenia zalohy,
- ulozi iba agregovany vystup do `data/output/activity_summary.csv`.

## Nadvazujuce podklady

- `docs/schema-map.md` popisuje zdrojove tabulky, stlpce a overene vazby.
- `docs/backend-data-contract.md` navrhuje metriky, API a bezpecnost buduceho
  backendu.
- `sql/` obsahuje read-only SQL sablony a validacne dotazy pre backend.

## Otvorene overenia

- Potvrdit obchodny vyznam osobitnych kodov `zDF` a `zOF`.
- Overit mapovanie na viacerych databazach a verziach oboch produktov.
- Pre OLYMP zistit nazvy technickych typov `activityLog.ActivityType`, ak sa
  maju niekedy zobrazovat samostatne.
