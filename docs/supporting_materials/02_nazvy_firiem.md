> Aktualizácia po skutočnom obnovení záloh: aktuálne nálezy sú v [06 – firmy](06_overene_firmy.md), [07 – používatelia](07_overeni_pouzivatelia.md) a [08 – protokol](08_protokol_obnovy.md). Text nižšie zachytáva predbežný stav pred obnovou a v prekonaných častiach už neplatí.
# Kde hľadať názov firmy pri x_číslo

## Čo vieme

KROS uvádza, že názov SQL databázy Omegy má tvar `x_YYYYYYYYY` a číslo je ID databázy programu. Nie je doložené, že ide o IČO. Preto ho správca záloh nesmie automaticky považovať za IČO. [KROS: obnova SQL databáz](https://akademia.kros.sk/faq/podvojne-uctovnictvo/zaloha-obnova-sql-databaz-a-automaticka-zaloha/)

| Miesto | Čo z neho možno očakávať |
|---|---|
| Firemná databáza `x_...` alebo `mzdy...` | Hlavný kandidát na obchodný názov, IČO, adresu a účtovné/mzdové obdobie; presné tabuľky treba objaviť |
| Zoznam pripojených firiem aplikácie | Prepojenie firmy a SQL databázy dostupné používateľovi; fyzické uloženie zoznamu závisí od aplikácie a verzie a zatiaľ nie je overené |
| `master` | Štandardne technický register databáz a serverová konfigurácia; samotné `sys.databases.name` vráti technický názov |
| `model` | Šablóna nových databáz; nie štandardný register obchodných názvov |
| `msdb` | História záloh a úloh; používateľský popis zálohy môže pomôcť, ale nemusí existovať ani byť aktuálny |

Úlohy systémových databáz sú popísané v [dokumentácii Microsoft](https://learn.microsoft.com/en-us/sql/relational-databases/backup-restore/back-up-and-restore-of-system-databases-sql-server). Prípadné vlastné objekty KROS v systémových databázach nemožno bez kontroly vylúčiť. Preto uvedené rozdelenie nie je tvrdením o presnej schéme tvojich záloh.

## Ako zistiť presnú tabuľku

1. Obnoviť jednu Omega a jednu Olymp zálohu do izolovanej SQL inštancie.
2. Získať zoznam používateľských tabuliek, stĺpcov a rozšírených vlastností.
3. Vybrať kandidátov podľa názvov označujúcich firmu, nastavenia, názov, IČO alebo organizáciu. Ak sú názvy skrátené, preskúmať aj malé konfiguračné tabuľky.
4. Prečítať len potrebné stĺpce a overiť, že ide o vlastnú firmu, nie o číselník zákazníkov či dodávateľov.
5. Porovnať názov, IČO, obdobie a ID s údajmi zobrazenými v KROS programe alebo známymi údajmi používateľa.
6. Zaznamenať presný dotaz a verziu schémy; až potom ho zaradiť do automatického čítača metadát.

Prvý orientačný dotaz, určený výhradne na budúcu obnovenú kópiu; nebol spustený:

```sql
SELECT s.name AS schema_name, t.name AS table_name,
       c.name AS column_name, ty.name AS data_type
FROM sys.tables AS t
JOIN sys.schemas AS s ON s.schema_id = t.schema_id
JOIN sys.columns AS c ON c.object_id = t.object_id
JOIN sys.types AS ty ON ty.user_type_id = c.user_type_id
WHERE t.is_ms_shipped = 0
ORDER BY s.name, t.name, c.column_id;
```

## Odporúčanie pre katalóg záloh

Ukladať zvlášť pôvodný SQL názov, ID databázy, program, názov firmy, IČO, obdobie, verziu schémy, čas zálohy a pôvod získaného údaja. Jedna firma môže mať viac databáz alebo období. Názov firmy nemusí byť unikátny ani nemenný.

Pre neznámu schému zobraziť „firma zatiaľ neidentifikovaná“ a technický názov. Automatický výber produkčného cieľa podľa podobnosti názvov nie je prípustný návrh. Ručné pomenovanie v katalógu ponechať oddelene od overeného názvu z databázy.

Pre dve aktuálne Omega zálohy zostáva mapovanie na firmy otvorené. Z obsahu `.bak` nebola načítaná žiadna tabuľka.

