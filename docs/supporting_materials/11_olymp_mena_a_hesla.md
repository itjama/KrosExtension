# Olymp: formát mien a heslových záznamov

Kontrola 21. 10. 2026 na obnovených databázach ADIKA a EH Clinical. Heslové hodnoty ani soli sa do tohto dokumentu neexportovali.

## Záver

Pozorovaný formát nie je rovnaký ako pri Omege. Mená sú čitateľný text. Heslové záznamy sú v `dbo.A_Uzivatelia.passwd`; starší stĺpec `heslo` je vo všetkých šiestich skúmaných riadkoch NULL.

## Mená

Stĺpec `meno` obsahuje priamo `adika`, `Hosť`, `Gabika` alebo `z5`. Hexadecimálny XOR dekodér Omegy sa na tieto mená nepoužíva.

## Heslové formáty

| Databáza | Používateľ | Stĺpec heslo | Stĺpec passwd |
|---|---|---|---|
| ADIKA | adika | NULL | 44 znakov Base64, po dekódovaní 32 bajtov |
| ADIKA | Hosť | NULL | NULL |
| ADIKA | Gabika | NULL | štruktúrovaný záznam PBKDF2-SHA512 |
| EH Clinical | adika | NULL | 44 znakov Base64, po dekódovaní 32 bajtov |
| EH Clinical | Hosť | NULL | NULL |
| EH Clinical | z5 | NULL | 44 znakov Base64, po dekódovaní 32 bajtov |

NULL sám osebe nepotvrdzuje možnosť prihlásenia bez hesla; správanie aplikácie sa netestovalo.

## Gabika: označený algoritmus

Záznam má tento tvar; uvedené zástupné názvy nie sú skutočné hodnoty:

```text
$pbkdf2-sha512$100000$<soľ v Base64>$<odvodený kľúč v Base64>
```

- Identifikátor algoritmu: `pbkdf2-sha512`.
- Počet iterácií: 100 000.
- Dĺžka soli po Base64 dekódovaní: 16 bajtov.
- Dĺžka odvodeného kľúča: 32 bajtov.
- Celková dĺžka záznamu: 91 znakov.

Štruktúra explicitne označuje funkciu odvodenia kľúča z hesla. PBKDF2 pracuje s heslom, soľou, iteráciami a zvolenou hashovacou funkciou; nejde o spätné XOR dekódovanie hesla. [Microsoft: PBKDF2](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.rfc2898derivebytes.pbkdf2)

Následne používateľ dodal známe heslo Gabiky. Výpočet PBKDF2-HMAC-SHA512 nad jeho UTF-8 bajtmi, s uloženou soľou, 100 000 iteráciami a výstupom 32 bajtov sa presne zhodoval s uloženým výsledkom. Rovnaké heslo prevedené celé na veľké alebo celé na malé písmená sa nezhodovalo. Tým je algoritmus potvrdený aj výpočtom pre tento konkrétny záznam, nie iba označením formátu.

Dodané heslo obsahuje iba ASCII znaky, preto tento test nerozlišuje UTF-8 od iných kódovaní s rovnakou reprezentáciou ASCII. Správanie pri diakritike ani prihlásenie cez klienta Olymp sa netestovalo. Overený heslový výpočet rozlišuje veľkosť písmen. Heslo, soľ a odvodený kľúč neboli uložené do dokumentácie; porovnanie prebehlo v pamäti.

## Ostatné neprázdne záznamy

Hodnoty účtov adika a z5 sú platné Base64 reťazce dekódujúce sa na 32 bajtov. To nepreukazuje konkrétny algoritmus: samotná dĺžka nestačí na tvrdenie, že ide o SHA-256, ani na vylúčenie inej funkcie či šifrovaného obsahu. Base64 je iba zápis bajtov, nie dôkaz reverzibility samotného hesla.

V dodaných dátach teda vedľa seba existujú aspoň dva odlišné formáty stĺpca passwd. Pre bežné zobrazenie používateľov v správcovi záloh nie je potrebné ani vhodné tento stĺpec exportovať.

## Rozsah vykonanej kontroly

Čítali sa mená, stĺpce heslo/passwd a metadáta formátu v izolovaných kópiách. Výstup obsahoval iba dĺžky, identifikátor algoritmu a parametre, nie skutočné heslové hodnoty alebo soli. Nenašla sa používateľská SQL rutina odkazujúca textom na heslo/passwd v preskúmaných databázach; implementácia klienta sa neanalyzovala.

Databázy zostali iba na čítanie. Žiadne heslo sa nemenilo a produkčný server sa nekontaktoval. Analytická inštancia bola po kontrole zastavená.
