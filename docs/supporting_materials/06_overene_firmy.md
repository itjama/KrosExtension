# Overené názvy firiem z obnovených záloh

Analýza vykonaná 21. 10. 2026. Tieto výsledky nahrádzajú predbežné tvrdenia o nezistených názvoch firiem v dokumentoch 00–03.

| Program | Pôvodná databáza | Firma | IČO |
|---|---|---|---|
| Omega | x_100001904 | IMPEXTA SK, s.r.o. | 36410730 |
| Omega | x_100121801 | Mybox s. r. o. | 57058997 |
| Olymp | mzdyADIKAs_r_o_ | ADIKA s.r.o. | 36385565 |
| Olymp | mzdyEHClinicals_r_o | EH Clinical s.r.o. | 51265559 |

## Omega: presná tabuľka a kľúče

Údaje sú priamo vo firemnej databáze, v `dbo.T000_INI`. Kľúčom nastavenia je `C000_ID`, používateľský kontext je `C010_IDUzivatel`, hodnota je `C097_MemoA` typu text. Firemné nastavenia boli pri používateľskom kontexte 0.

| C000_ID | Význam odvodený z hodnôt | x_100001904 | x_100121801 |
|---:|---|---|---|
| 1001 | ID databázy | 100001904 | 100121801 |
| 1003 | Rok v nastavení | 2026 | 2026 |
| 1010 | Názov firmy | IMPEXTA SK, s.r.o. | Mybox s. r. o. |
| 1017 | IČO | 36410730 | 57058997 |
| 101160 | Verzia uložená v nastavení | 29.50.0 | 29.50.0 |

Nasledujúci výber bol úspešne použitý na oboch kópiách:

```sql
SELECT C000_ID, C010_IDUzivatel,
       CAST(C097_MemoA AS varchar(max)) AS hodnota
FROM dbo.T000_INI
WHERE C010_IDUzivatel = 0
  AND C000_ID IN (1001,1003,1010,1017,101160)
ORDER BY C000_ID;
```

Na prečítanie názvu firmy nie je potrebný master/model ani dekódovanie používateľských mien. Mapovanie platí pre skúmané schémy. Pred automatickým použitím v ďalších verziách treba overiť stĺpce, existenciu a jednoznačnosť kľúčov. Rok a verzia neboli potvrdené otvorením databázy v klientovi Omega.

## Olymp: presná tabuľka a stĺpce

Údaje sú v `dbo.nastavenie`: `orgNazov`, `orgICO`, `orgDIC`. Nastavenia sa opakujú podľa `ID_obdobie`.

| Firma | DIČ | Rozsah ID_obdobie | Počet riadkov nastavenie |
|---|---|---|---:|
| ADIKA s.r.o. | 2020098003 | 202311–202609 | 35 |
| EH Clinical s.r.o. | 2120648574 | 202312–202503 | 16 |

Rozsah nastavení nie je dôkazom spracovaných miezd v každom mesiaci. Dátum zálohy nie je totožný s posledným obdobím údajov.

```sql
SELECT DISTINCT orgNazov, orgICO, orgDIC
FROM dbo.nastavenie;

SELECT MIN(ID_obdobie) AS prve_obdobie,
       MAX(ID_obdobie) AS posledne_obdobie,
       COUNT(*) AS pocet_nastaveni
FROM dbo.nastavenie;
```

Ak ďalšie zálohy obsahujú historické zmeny názvu firmy, katalóg musí zachovať väzbu na obdobie a nevyberať náhodný prvý riadok.

## Dôsledok pre správcu záloh

Po obnovení firemnej kópie možno firmu automaticky identifikovať uvedenými dotazmi. Pre katalóg uchovať program, pôvodný SQL názov, ID, názov firmy, IČO, obdobie, verziu a zdrojovú zálohu. Podobnosť mena súboru nepoužívať na automatické rozhodnutie o produkčnom cieli.
