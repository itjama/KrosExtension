# Obnova aktuálnych dát zo zálohy

Obnova celej databázy vráti jej obsah do zvoleného historického stavu. Nie je zlúčením starých a nových dokladov. Zápisy po tomto bode sa do obnoveného výsledku neprenesú, pokiaľ nie sú zahrnuté vo vhodnom obnovovacom reťazci. Výber jednotlivých záznamov patrí do osobitného postupu podporovaného aplikáciou.

## Navrhovaný postup

1. **Najprv skúšobná obnova.** Overiť zvolenú zálohu v izolácii, konzistenciu, firmu, IČO, obdobie a kompatibilitu s KROS. Záloha z novšieho SQL Servera sa nedá jednoducho vrátiť na starší; pri rozdielnych verziách overiť konkrétnu podporovanú cestu. Pri produkčnej obnove použiť pôvodný vhodný backup set, nie automaticky novú zálohu testovacej kópie upgradovanej na novšom serveri.
2. **Ukázať konkrétny plán.** Server/inštancia, cieľová databáza, firma, záloha, čas obnoveného stavu, dotknuté súbory a očakávaný výpadok. Pri neoverenej identite firmy obnovu zastaviť.
3. **Získať potvrdenie tejto operácie.** Správca záloh musí odlíšiť „pozrieť kópiu“ od „nahradiť aktuálne dáta“. Ide o návrh ochrany v budúcom programe, nie o žiadosť o povolenie obnovy v tejto analýze.
4. **Zastaviť zápisy.** Dohodnúť odstávku, zatvoriť firmu u používateľov a pozastaviť integrácie/úlohy. Po zabezpečení záchrannej zálohy udržať zápisy zastavené až do ukončenia alebo návratu.
5. **Vytvoriť záchrannú zálohu aktuálneho stavu** do nového súboru, vrátane potrebných externých súborov a nastavení. Pre dodatočnú úplnú SQL zálohu použiť podľa plánu `COPY_ONLY`, aby sa nezmenila základňa bežných diferenciálnych záloh. Overiť jej použiteľnosť pred prepisom. [Microsoft: COPY_ONLY](https://learn.microsoft.com/en-us/sql/relational-databases/backup-restore/copy-only-backups-sql-server)
6. **Rozhodnúť o transakčnom logu.** Pri FULL/BULK_LOGGED posúdiť zálohu konca logu a plán obnovy s administrátorom. Pri SIMPLE alebo chýbajúcom reťazci nesľubovať obnovu k ľubovoľnej sekunde. Ak aktuálna databáza nie je zálohovateľná, bežný postup zastaviť a riešiť výslovný havarijný scenár.
7. **Znova overiť cieľ tesne pred zásahom.** Výlučný prístup, správna inštancia, presný cieľ, cesty a vybraný backup set. Až potom obnoviť vybranú firemnú databázu podľa pripraveného plánu. Obnovu vykonáva oddelená privilegovaná služba, nie bežný analytický účet.
8. **Skontrolovať výsledok pred vpustením používateľov.** Stav databázy, CHECKDB, SQL prístupy, otvorenie správnej firmy v kompatibilnej verzii KROS a zmysluplné kontrolné údaje, napríklad obdobie a posledné doklady.
9. **Uvoľniť prevádzku alebo sa vrátiť.** Pri úspechu obnoviť prístupy a integrácie. Pri chybe udržať odstávku a obnoviť záchrannú zálohu podľa pripraveného návratového plánu. Ak už používatelia medzičasom zapisovali, návrat by zahodil aj tieto nové zmeny a vyžaduje nové rozhodnutie.

## Čo sa automaticky neobnovuje

Zálohy `master`, `model` a `msdb` sa pri bežnom návrate jednej firmy nenahrádzajú. Ich obnova môže zasiahnuť celú inštanciu a vyžaduje osobitný administrátorský postup s verziovými obmedzeniami. [Microsoft: obnova systémových databáz](https://learn.microsoft.com/en-us/sql/relational-databases/backup-restore/back-up-and-restore-of-system-databases-sql-server)

Ani obnovenie firemnej databázy samo neopraví všetky serverové loginy, klientské pripojenia, externé súbory a aplikačné závislosti. Program má tieto položky evidovať a po obnove overiť, nie ich naslepo prepísať.

## Údaje potrebné pred realizáciou

- Presná inštancia SQL, jej verzia/edícia a spôsob prihlásenia oprávneného správcu.
- Verzie Omega/Olymp a informácia, či je SQL server aj na tomto počítači alebo vzdialený.
- Overené mapovanie databáz na firmy, IČO a obdobia.
- Režim obnovy, existujúci plán záloh a dostupnosť diferenciálnych/logových záloh.
- Umiestnenie produkčných dát, príloh, pomocných súborov a integračných nastavení.
- Požadovaný bod návratu, prijateľná strata novších údajov a dĺžka odstávky.
- Samostatné testovacie prostredie, disková kapacita a prípadné šifrovacie kľúče.

Žiadna obnova ani zmena produkčných dát v rámci tejto úlohy neprebehla.
