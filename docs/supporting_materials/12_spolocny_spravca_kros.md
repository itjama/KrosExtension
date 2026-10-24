# Spoločný správca firiem Omega a Olymp

## Cieľ

Jedno administračné rozhranie pre veľký počet firiem. Správca vyhľadá firmu alebo používateľa, vyberie viac databáz a vykoná rovnakú operáciu bez opakovaného otvárania každej firmy v KROS programe. Správa záloh je súčasťou tohto nástroja, nie jeho jedinou funkciou.

Toto je návrh podľa potrieb používateľa a doteraz preskúmaných záloh. Hromadné zmeny používateľov zatiaľ neboli implementované ani otestované v klientovi KROS.

## Hlavné obrazovky

### Firmy

Zoznam: program, názov firmy, IČO, obdobie, SQL server a databáza, rozpoznaná verzia schémy, posledná dostupná záloha a výsledok jej kontroly. Filtrovanie podľa programu, firmy, obdobia a stavu. Jedna firma môže mať viac období a databáz; názov firmy nie je unikátny identifikátor.

Zdroj jasne označiť: aktuálna databáza alebo historická záloha. Stav používateľa zo zálohy nesmie byť prezentovaný ako aktuálny stav produkcie.

### Používatelia naprieč firmami

Príklad: vyhľadať DANKA a zobraziť všetky databázy, kde sa nachádza, s lokálnym ID, programom a existujúcimi príznakmi práv. Správca môže vybrať konkrétne firmy alebo všetky výsledky filtra.

Rovnaké meno nemusí znamenať tú istú osobu. Centrálne priradenie osoby k lokálnym účtom musí byť overené správcom. Lokálne ID sa medzi databázami nesmú zamieňať. V našich vzorkách má napríklad IRENKA ID 1 v jednej Omege a ID 8 v druhej.

Rozlišovať osoby od profilov práv: v skúmaných Omegách obsahuje typ 1 názvy ÚČTOVNÍK, POMOCNÝ ÚČTOVNÍK, ÚČTOVNÍK FAKTURANT, FAKTURANT a SKLADNÍK. Tieto riadky sa nezahrnú medzi osoby iba preto, že ležia v tabuľke používateľov.

### Hromadné operácie

Plánované operácie: zmena/reset hesla pre vybrané lokálne účty, vytvorenie používateľa, nastavenie alebo porovnanie práv, deaktivácia účtu a report rozdielov. Každá operácia musí byť podporovaná a odskúšaná osobitne pre daný program a schému.

Pred vykonaním zobraziť konkrétny zoznam cieľových databáz, účtov a zmien. Výsledok po firmách: vykonané, preskočené, nepodporované, konflikt, chyba. Pri opakovaní vybrať len nezmenené alebo zlyhané položky, nie automaticky celý pôvodný zoznam.

### Zálohy a obnova

Vyhľadávanie záloh podľa skutočného názvu firmy, skúšobné obnovenie, kontrola konzistencie a samostatná obnova aktuálnych dát. Existujúce postupy sú v dokumentoch 04–05. Pre testy používateľských zmien použiť ďalšie pracovné kópie; existujúce analytické kópie sú iba na čítanie.

## Čo už vieme a čo ešte chýba

| Funkcia | Omega | Olymp |
|---|---|---|
| Identifikovať firmu | overené T000_INI a konkrétne kľúče | overené nastavenie.orgNazov/orgICO |
| Zobraziť mená | overené spätné XOR čítanie C100_Meno | čitateľné A_Uzivatelia.meno |
| Zobraziť uložené príznaky práv | stĺpce a hodnoty overené, úplná interpretácia otvorená | stĺpce a hodnoty overené, úplná interpretácia otvorená |
| Overiť dodané heslo | potvrdená reverzibilná podoba pri DANKA | potvrdený PBKDF2-SHA512 pri Gabike |
| Vytvoriť nový heslový záznam | chýba overenie významu koncového bajtu a zápisu aplikáciou | PBKDF2 formát známy; prijatie nového záznamu klientom neotestované |
| Zmeniť práva alebo pridať účet | treba zistiť väzby, typy a sprievodné zmeny | treba zistiť väzby, kódy a sprievodné zmeny |

Pri Olympe sa PBKDF2 záznam našiel len pri jednom z preskúmaných používateľov. Ostatné neprázdne heslové hodnoty majú odlišný 32-bajtový formát. Nemožno bez testu predpokladať, že starší klient prijme novší formát.

## Technické rozdelenie

Jadro správcu eviduje firmy, priradenia osôb, operácie a výsledky. Oddelené adaptéry Omega/Olymp poznajú konkrétne schémy a podporované zmeny. Pri neznámej schéme zostane nástroj v režime čítania.

Priamy SQL prístup je možná implementačná cesta, keď nie je k dispozícii vhodné podporované rozhranie výrobcu. Poznanie tabuľky samo osebe nestačí na správny zápis. Každý adaptér musí poznať potrebné väzby, kontrolovať pôvodný stav riadka a meniť iba presne určené údaje.

Zápis jednej operácie v jednej databáze má byť transakčný. Celá dávka cez viac databáz alebo serverov sa považuje za viac samostatných výsledkov; nesľubovať jednu spoločnú atómovú operáciu. Ak sa riadok medzi načítaním a zápisom zmenil, oznámiť konflikt a neprepísať ho naslepo.

Účet samotného správcu databáz je odlišný od aplikačných účtov účtovníkov. Nástroj nemá potrebovať poznať staré heslá všetkých pracovníkov na zobrazenie firiem a používateľov. Heslá, ich uložené reprezentácie a soli sa nezapisujú do bežného auditu; audit obsahuje cieľ, typ zmeny, čas, správcu a výsledok.

## Konkrétny postup overenia zápisov

1. Na pracovných kópiách vytvoriť v klientovi KROS testovací účet alebo upraviť existujúci testovací účet.
2. Zachytiť rozdiel pred/po pre zmenu hesla, práv a aktívnosti. Tieto pokusy oddeliť, aby bolo jasné, čo ktorá operácia mení.
3. Pre Omegu overiť aj generovanie posledného bajtu a normalizáciu mena/hesla. Pre Olymp overiť, ktoré klientské verzie prijímajú PBKDF2 a či existujú ďalšie sprievodné polia.
4. Rovnakú zmenu vykonať pripraveným adaptérom na ďalšej kópii a následne otestovať prihlásenie aj príslušné práva v KROS.
5. Až po úspešnom porovnaní povoliť danú operáciu pre rozpoznané schémy a vybrané produkčné databázy.

Žiadna hromadná zmena ani reset používateľa neboli touto dokumentáciou vykonané.

## Odporúčané poradie implementácie

1. Katalóg firiem, zoznam používateľov naprieč firmami a správa záloh – čítanie už má overený základ.
2. Hromadná zmena hesla – po doplnení a aplikačnom overení zapisovacích adaptérov.
3. Hromadné pridávanie/deaktivácia používateľov a profily práv – po zistení všetkých väzieb a významu príznakov.

Výsledkom má byť jedno miesto na každodennú správu mnohých firiem, nie iba prehliadač obsahu záloh.
