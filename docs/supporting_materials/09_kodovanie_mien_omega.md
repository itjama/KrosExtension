# Kódovanie používateľských mien Omega – vyriešené čítanie

Overené 21. 10. 2026 na `dbo.T004_Uzivatelia.C100_Meno` z dvoch obnovených databáz. Záver nahrádza skoršie tvrdenie, že mená Omegy neboli dekódované.

## Výsledok

Meno je uložené ako hexadecimálny text s jednoduchým reťazeným XOR. Ide o reverzibilné kódovanie/obfuskáciu, nie jednosmerný hash mena. Dekódovanie poskytlo zmysluplné mená a názvy rolí vo všetkých 27 riadkoch (15 rôznych kódovaných hodnotách).

Pozorované mená sú veľkými písmenami. Z tejto vzorky nemožno určiť, či ich aplikácia pri ukladaní automaticky prevádza na veľké písmená.

## Algoritmus čítania

1. Hexadecimálny text rozdeliť na dvojice a previesť na bajty `c[0] ... c[n-1]`.
2. Posledný bajt odložiť. Pri všetkých skúmaných hodnotách nasleduje za obsahom mena; jeho význam nie je potvrdený.
3. Prvý bajt mena: `p[0] = c[0] XOR 0x04`.
4. Ďalšie bajty mena: `p[i] = c[i] XOR c[i-1]`, pre `i = 1 ... n-2`.
5. Výsledné bajty interpretovať cez Windows-1250. To správne obnoví aj Ú, Č, Ý a Í v názvoch rolí.

Spätné vytvorenie **obsahovej časti** je `c[0] = p[0] XOR 0x04` a `c[i] = p[i] XOR c[i-1]`. Táto časť sa po opätovnom zakódovaní presne zhodovala vo všetkých 27 záznamoch. Spätný test overuje implementáciu transformácie; sám osebe nenahrádza porovnanie s používateľským rozhraním KROS.

Posledný bajt sa medzi rôznymi menami líši. Jednoduchý súčet bajtov neposkytol všeobecné vysvetlenie. Môže ísť o pomocnú alebo kontrolnú hodnotu, ale to je zatiaľ iba hypotéza. Preto je dekodér určený na čítanie, nie na vytváranie alebo prepis záznamov v Omege. Úplný algoritmus vytvárania koncového bajtu zostáva otvorený.

## Príklad DANKA

Uložené: `40014f0445f1`.

| Krok | Výpočet | Výsledok |
|---|---|---|
| 1 | 0x40 XOR 0x04 | 0x44 = D |
| 2 | 0x01 XOR 0x40 | 0x41 = A |
| 3 | 0x4f XOR 0x01 | 0x4e = N |
| 4 | 0x04 XOR 0x4f | 0x4b = K |
| 5 | 0x45 XOR 0x04 | 0x41 = A |
| koniec | 0xf1 | pomocný bajt, význam neoverený |

## Dekódované používateľské záznamy typu 0

### IMPEXTA SK, s.r.o. – x_100001904

| ID | Meno | C103_Spravca |
|---:|---|---:|
| 1 | IRENKA | -1 |
| 7 | DANKA | 0 |
| 8 | SILVIA | 0 |
| 9 | KATKA | 0 |
| 10 | DENISKA | -1 |
| 11 | GABIKA | 0 |
| 12 | MARIA | 0 |
| 14 | MARTINKA | -1 |
| 31 | LENKA | 0 |

### Mybox s. r. o. – x_100121801

| ID | Meno | C103_Spravca |
|---:|---|---:|
| 1 | ILDI | -1 |
| 7 | DANKA | -1 |
| 8 | IRENKA | -1 |
| 10 | SILVIA | 0 |
| 11 | DENISKA | 0 |
| 12 | KATKA | 0 |
| 13 | MARTINKA | 0 |
| 27 | LENKA | -1 |

Hodnota -1 je nenulový príznak v stĺpci správcu. Ide o historický stav záloh, nie potvrdenie aktuálneho prístupu týchto osôb do produkcie.

## Záznamy typu 1 v oboch databázach

| ID | Dekódovaný názov |
|---:|---|
| 2 | ÚČTOVNÍK |
| 3 | POMOCNÝ ÚČTOVNÍK |
| 4 | ÚČTOVNÍK FAKTURANT |
| 5 | FAKTURANT |
| 6 | SKLADNÍK |

Názvy teraz silno podporujú interpretáciu týchto záznamov ako rolí/profilov oprávnení. Presné dedenie a význam všetkých kódov práv zostávajú neoverené; tieto riadky sa nemajú rátať ako samostatné osoby.

## Použiteľný dekodér

[decode_omega_name.ps1](decode_omega_name.ps1) prijme jednu hexadecimálnu hodnotu a vráti meno aj samostatný koncový bajt. Nepripája sa k SQL Serveru a nič nezapisuje do databázy.

```powershell
.\decode_omega_name.ps1 -HexName '40014f0445f1'
# Name: DANKA
# TrailingByteHex: f1
# TrailingByteVerified: False
```

Pre správcu záloh možno tento postup použiť na zobrazenie používateľských mien zo skúmaných schém. Pri inom formáte alebo verzii má program výsledok označiť za neoverený. Algoritmus bol odvodený z dát, nie potvrdený zdrojovým kódom výrobcu. Preskúmaný zoznam používateľských SQL funkcií/procedúr neukázal zjavný dekodér mien; klient Omega na tomto počítači nebol nájdený v kontrolovaných bežných umiestneniach.

Analýza sa týkala výhradne mien. Z nej nemožno vyvodzovať spôsob ochrany hesiel. Obnovené databázy zostali iba na čítanie; žiadny používateľský záznam sa nemenil.
