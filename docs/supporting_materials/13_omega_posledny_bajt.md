# Omega: posledný bajt je kontrolný súčet

Overené 24. 10. 2026 priamou statickou analýzou oficiálneho modulu Omega a porovnaním s obnovenými zálohami. Tento výsledok nahrádza predchádzajúce tvrdenie v dokumentoch 09–12, že účel posledného bajtu nie je známy.

## Presný význam

Posledný uložený bajt nie je súčasť mena ani hesla. Je to osembitový kontrolný súčet vnútorných kódovaných bajtov, na ktorý sa následne použije XOR maska `0x75`.

Ak `c[0] ... c[n-1]` označuje bajty získané z hexadecimálneho reťazca vrátane posledného kontrolného bajtu, platí:

```text
kontrolny_bajt = (sum(c[i] XOR 0x75, i = 0 ... n-2) MOD 256) XOR 0x75
```

Kontrola je úspešná práve vtedy, keď `kontrolny_bajt == c[n-1]`. Pri výpočte sa posledný bajt do súčtu nezahrnie.

Program kontroluje súčet ešte pred dekódovaním obsahu. Ak nesedí, funkcia `Odhesluj` v preskúmanej implementácii vráti neúspech. Nestačí teda pri tvorbe nového záznamu posledný bajt náhodne zvoliť alebo ponechať z pôvodného hesla.

Ak sa na posledný bajt omylom aplikuje bežné reťazené dekódovanie znakov, vzniknutý znak nemá význam ako súčasť hesla. Kontrolný bajt sa spracúva osobitne.

## Príklad na mene DANKA

Uložený reťazec: `40014f0445f1`.

Obsahové bajty sú `40 01 4f 04 45`, posledný bajt je `f1`.

```text
40 XOR 75 = 35
01 XOR 75 = 74
4f XOR 75 = 3a
04 XOR 75 = 71
45 XOR 75 = 30

0x35 + 0x74 + 0x3a + 0x71 + 0x30 = 0x184
0x184 MOD 256 = 0x84
0x84 XOR 0x75 = 0xf1
```

Výsledok je presne uložený posledný bajt. Všetky čísla v tomto príklade okrem 256 sú hexadecimálne.

## Úplný postup kódovania

Samotná funkcia Zahesluj má vnútorný počiatočný stav `0x71`. Pre každý vstupný znak, reprezentovaný bajtom `p`:

1. `stav = stav XOR p`.
2. Pripočítať tento vnútorný stav do kontrolného súčtu.
3. Do výstupu uložiť `stav XOR 0x75`.
4. Po poslednom znaku pripojiť `(sucet MOD 256) XOR 0x75`.
5. Výsledné bajty zapísať hexadecimálne.

Skorší dekodér používal pre prvý znak konštantu `0x04`. To zostáva správne, pretože `0x71 XOR 0x75 = 0x04`. Pri ďalších znakoch sa maska pri XOR susedných obsahových bajtov vyruší. Chýbajúca časť bola kontrola súčtu pred dekódovaním.

V preskúmanej ceste zápisu hesla `UzivatelHelper.DajSqlZapisNoveHeslo` sa pred `Zahesluj` volajú `Uprav` a `UCase`. Samotný codec preto netreba zamieňať s úplným spracovaním vstupu v používateľskom rozhraní. Priložený modul text automaticky nemení na veľké písmená ani neodstraňuje medzery. Presné správanie Uprav nebolo predmetom tejto analýzy.

Pre skúmané slovenské dáta je overené Windows-1250. Kód výrobcu používa VB Asc/Chr, takže prípadné odlišné jazykové prostredie treba overiť samostatne.

## Dôkazy a výsledky testov

| Overenie | Výsledok |
|---|---|
| Úplné zakódovanie 27 mien vrátane koncového bajtu | 27/27 presných zhôd |
| Rôzne mená/profily v tejto vzorke | 15 rôznych hodnôt |
| Kontrolný súčet heslových polí x_100001904 | 14/14 platných |
| Kontrolný súčet heslových polí x_100121801 | 13/13 platných |
| Zamietnutie zmeneného kontrolného bajtu v našom dekodéri | 27/27 |
| Diakritika v názve ÚČTOVNÍK | úplná zhoda |
| Prázdny obsah podľa preskúmaného algoritmu | kóduje sa ako `75` |

Test prázdneho obsahu je testom codec algoritmu, nie tvrdením, že klient dovolí nastaviť prázdne heslo. Heslové záznamy sa pri tejto kontrole nedekódovali: čítali sa iba na výpočet súčtu a výsledkom boli súhrnné počty. Žiadne heslá alebo ich uložené hodnoty sa nedostali do výstupného dokumentu.

## Overený zdroj implementácie

Zdroj: [oficiálny archív verzií Omega](https://www.kros.sk/omega/archiv-verzii), jeho odkaz na [aktualizačný balík 29.60](https://ftpkros.sk/files/versions/omega/29.60/upgrade.zip).

- Súbor `Intro.exe`, FileVersion/ProductVersion `29.60.00.00`.
- Overenie Authenticode: `Valid`, podpis `Kros a.s.`.
- SHA-256 súboru: `1EDAF840E9433DE05607AD8E800540C491852B9DFA12FCCA9863A67CB0EFA3D3`.
- Kódovanie: `OmegaProject.Spolocne.Zahesluj`, metadata token `0x0601D5E0`, RVA `0x01516CB8`.
- Kontrola a dekódovanie: `OmegaProject.Spolocne.Odhesluj`, metadata token `0x0601D5DF`, RVA `0x01516A38`.
- Použitie pri zápise hesla: `OmegaProject.Modules.Uzivatelia.Helpers.UzivatelHelper.DajSqlZapisNoveHeslo`.

Analyzovaný program je 29.60; databázové nastavenia skúmaných záloh uvádzajú 29.50.0. Zhodu algoritmu pre dodané zálohy potvrdzujú vyššie uvedené testy. Nevydávame to za všeobecnú záruku pre každú historickú alebo budúcu verziu.

Balík nebol nainštalovaný a jeho programy ani rutiny neboli spustené. Čítali sa len metadáta a IL. Nevykonal sa reset hesla ani zápis do databáz; produkčný SQL01 sa nekontaktoval. Analytická LocalDB bola po overení zastavená.

## Dopad na spoločného správcu

Samotný koncový bajt už nie je prekážkou generovania správneho reťazca. [omega_codec.py](omega_codec.py) obsahuje samostatné funkcie `encode`, `decode`, `checksum_byte` a `is_valid`; do databáz sa nepripája a nič v nich nemení. `decode` pri nesúlade kontrolného súčtu vyvolá chybu.

Pred hromadným produkčným resetom ešte zostáva overiť celý zápis a prihlásenie v klientovi na pracovnej kópii. Táto úloha potvrdila formát a jeho kontrolu, nie kompletný prevádzkový postup resetu.

Kontrolný súčet má len 8 bitov a je určený na kontrolu konzistencie tohto formátu. Nejde o kryptografickú ochranu hesla; reverzibilita obsahu zostáva zachovaná.
