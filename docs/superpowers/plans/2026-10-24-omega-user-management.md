# Omega — manažment používateľov naprieč firmami

Dátum: 24. 10. 2026. Stav: **aktuálny implementačný plán projektu**, zvolený používateľom 24. 10. 2026. Implementácia zatiaľ nezačala; aplikácia ani databázy sa pri príprave a výbere plánu nemenili.

## Cieľ a rozsah

Do menu **Admin → Omega → Manažment používateľov** pridať spoločnú správu účtov programu Omega (podvojné účtovníctvo). Admin uvidí používateľov zo všetkých dostupných firiem, porovná ich heslá, upraví členstvo vo firmách, premenuje používateľa a nastaví spoločné heslo jednou potvrdenou dávkou.

Ide o účty zdrojového programu Omega. Účty webového rozhrania Kros Extension sú samostatné: poskytujú návrhy mien, ale ich heslá, roly ani existencia sa týmito operáciami nemenia. Olymp a všeobecný editor práv nie sú súčasťou tejto funkcie.

## Podklady a potvrdené obmedzenia

- [Index výskumu](../../supporting_materials/README.md) určuje prednosť novších zistení.
- [Firmy](../../supporting_materials/06_overene_firmy.md): názov, rok, IČO a verzia sú v `T000_INI`, pre globálne hodnoty `C010_IDUzivatel = 0`; kľúče 1010, 1003, 1017, 101160.
- [Používatelia a práva](../../supporting_materials/07_overeni_pouzivatelia.md) a [mená/profily](../../supporting_materials/09_kodovanie_mien_omega.md): `T004_Uzivatelia`, lokálne ID `C000_ID`, meno `C100_Meno`, heslo `C101_Heslo`. Do zoznamu osôb zaradiť podporovaný typ `C112_TypZaznamu = 0`, nie profily typu 1. Neznáme typy vykázať osobitne.
- [Úplný codec](../../supporting_materials/13_omega_posledny_bajt.md) a [referenčný kód](../../supporting_materials/omega_codec.py): reverzibilné kódovanie Windows-1250 s kontrolným bajtom. Algoritmus je potvrdený pre skúmané zálohy 29.50 a modul 29.60; neplatný checksum nie je prázdne heslo.
- V overenej ceste zápisu Omega volá `Uprav` a `UCase` pred kódovaním. Uprav, limity polí a úplné správanie klienta treba ešte overiť. Nemožno sľúbiť zachovanie malých písmen ani obnovu pôvodne napísanej veľkosti písmen.
- [Skorší návrh správcu](../../supporting_materials/12_spolocny_spravca_kros.md): miestne ID sa medzi firmami líšia; rovnaké meno nie je dôkaz totožnosti osoby. Časť o neznámom kontrolnom bajte už nahrádza dokument 13.
- Pridanie/odobratie používateľa, závislé záznamy, práva a následné prihlásenie neboli overené v klientovi. Ich presný zápis sa určí experimentom na pracovných kópiách, nie odhadom INSERT/DELETE.

## Rozhodnutia použité v tomto pláne

1. „Všetky firmy“ znamená všetky identifikované databázy Omega na aktívnych nakonfigurovaných serveroch, aj nesledované dashboardom. Nejde o skenovanie záloh na disku. Katalóg obsahuje aj známe, aktuálne nedostupné databázy a zreteľne označí neúplný sken servera.
2. Jedna položka firmy predstavuje jednu databázu a jej účtovný rok. Tá istá firma v dvoch rokoch sú dve položky. Identita je server + databáza, nie názov/IČO/rok.
3. „Min. 6 písmen“ sa interpretuje ako min. 6 znakov. Kontrola malých/veľkých písmen sa nevyžaduje, pretože Omega heslo pred kódovaním prevedie na veľké písmená (rozhodnutie používateľa 24. 10. 2026).
4. Odznačenie pôvodne prítomnej firmy znamená návrh odobratia účtu z tejto firmy. Nejde iba o vynechanie firmy zo zmeny hesla. UI to uvedie pri chipoch aj v potvrdení.
5. Nové heslo sa použije na všetky výsledne označené firmy. Prázdny editbox znamená ponechať heslá existujúcich účtov; pri akomkoľvek pridaní do firmy je nové heslo povinné, aby sa nevyberalo náhodné existujúce heslo.
6. Predvolený nový účet nebude správca. Potrebný profil/práva musia byť výslovne zvolené z klientom overených možností; ak pre firmu nie je overený postup, pridanie do nej je nedostupné s dôvodom. Existujúcim účtom zmena hesla nemení práva.

## Obrazovka a interakcie

### Horná lišta

- **Pridať používateľa** otvorí nový rozbalený riadok: meno s návrhmi z webových používateľov, možnosť vlastného mena, heslo, výber firiem a overeného profilu pre nové účty.
- Meno sa po normalizácii skontroluje v celom načítanom katalógu. Ak už existuje, ponúkne sa jeho existujúci riadok; nevznikne druhý agregovaný používateľ. Skutočnú duplicitu v každej cieľovej DB preverí aj server tesne pred zápisom. Neúplný sken nemožno prezentovať ako dôkaz neexistencie.
- **Odobrať používateľa / Odobrať označených** je aktívne pri označených používateľských riadkoch. Označenie riadka je samostatný checkbox, odlišný od členstva cez firemné chipy. Odobratie navrhne odstránenie všetkých zistených výskytov označených účtov v rozsahu katalógu a vyžiada potvrdenie so zoznamom firiem. Neúplný rozsah explicitne prizná; nesľúbi odstránenie z neprečítaných databáz. Rozpracované zmeny vybraných riadkov treba najprv aplikovať alebo resetovať.
- **Obnoviť zoznam** spustí čítanie aktuálnych dát. Zobrazovať čas skenu, priebeh a počet úspešných/neúspešných firiem. Obnova nesmie prepísať rozpracované návrhy.

### Zbalený riadok používateľa

`[výber] MENO | Heslá: zhodné / rozdielne / neoverené | 10/31 | rozbaliť`

Čitateľ aj menovateľ počítajú databázy v aktuálnom katalógu. Pri chybách ukázať napr. „10 potvrdených / 31; 2 neoverené“, nie nepravdivé presné členstvo. Filtrovanie viditeľných chipov nemení celkový rozsah ani počty. Pri jedinom výskyte uviesť „1 firma“, pri žiadnom „bez účtu“. Zhodu vyhodnotiť iba medzi platne načítanými účtami; pri neúplných údajoch použiť „neoverené“, prípadne doplniť známu nezhodu.

Agregovať podľa overenej normalizácie mena Omega. Zachovať pôvodné lokálne ID pre každú DB. Pri viacerých rovnakých normalizovaných menách v jednej DB zobraziť konflikt a zablokovať zápis do tejto DB, nevybrať prvý riadok. Webové meno je návrh, nie automatické prepojenie identity alebo synchronizácia účtov.

### Rozbalený riadok

Horný riadok obsahuje indikátor zhody hesiel a zoznam všetkých jedinečných dekódovaných uložených hesiel s počtom ich výskytov, editbox **Nové heslo**, **Aplikovať zmenu** a **Reset**. Zoznam má funkciu zobraziť/skryť; heslá sa načítajú až pri rozbalení oprávneným adminom. Prázdne platné heslo, NULL a neplatné kódovanie majú odlišné štítky. Zmena hesla na zhodnú efektívnu hodnotu sa nevykáže ako vykonaná zmena.

Pod editboxom bude priebežný slovný súhrn, napríklad: „Používateľ sa pridá do 3 firiem, v 30 existujúcich firmách sa zmení heslo a z 2 firiem sa odoberie.“ Počty nových účtov a zmien hesla existujúcich účtov sa neprekrývajú. Keď sa nič nezmení, tlačidlo aplikovania je neaktívne.

Nasledujú chipy všetkých firiem: **Názov firmy · rok**. Ak rok chýba, uviesť „rok neznámy“; pri rovnakých názvoch doplniť server/databázu.

| Stav chipu | Zobrazenie | Význam |
|---|---|---|
| Pôvodne prítomný, stále označený | zelený + zaškrtnutie | účet zostáva |
| Pôvodne neprítomný, neoznačený | neutrálny | bez účtu |
| Novo označený | modrý + „Pridať“ | čaká pridanie |
| Pôvodne prítomný, odznačený | červený obrys + „Odobrať“ | čaká odobratie |
| Nedostupný/nepodporovaný/konflikt | sivý + dôvod | stav nemožno meniť |

Farba je vždy doplnená textom/ikonou. Hover nad firmou s existujúcim účtom ukáže aktuálne dekódované uložené heslo, nie plánované nové heslo. Rovnaké zobrazenie musí fungovať cez fokus klávesnice a dotyk. Bez účtu sa zobrazí „Používateľ tu nie je“, pri chybe „Heslo nemožno prečítať“. Tooltip nesmie byť obyčajný trvalo vyplnený HTML title.

**Označiť všetko / Odznačiť všetko** upraví všetky editovateľné chipy v celom katalógu, aj keď je zoznam filtrovaný; popis explicitne uvedie rozsah. Nedostupné položky ponechá nezmenené a vypíše ich počet. Odznačenie všetkého je návrh odobratia zo všetkých dostupných pôvodných firiem, vykoná sa až po potvrdení.

### Premenovanie používateľa

- V rozbalenom riadku bude vedľa aktuálneho mena tlačidlo **Zmeniť**. Po kliknutí sa meno zobrazí v editboxe, predvyplnené aktuálnou hodnotou. Samotné otvorenie editora nie je zmena; odlišné meno už znamená rozpracovaný stav.
- Premenovanie sa potvrdzuje spoločným **Aplikovať zmenu**, spolu s prípadnou zmenou hesla a členstva. Samostatné premenovanie nevyžaduje nové heslo. Reset vráti aj pôvodné meno a ukončí jeho editovanie.
- Nové meno nesmie byť prázdne a musí vyhovovať overenej normalizácii, kódovaniu a dĺžke mena Omega. Kontrola existencie prebehne v UI, pri serverovom náhľade a opäť v transakcii pred zápisom. Porovnáva sa normalizované meno, pričom vlastný účet s rovnakým lokálnym ID nie je duplicita; normalizačne nezmenené meno je bez zmeny.
- Ak iný účet s cieľovým menom existuje kdekoľvek v známom katalógu, premenovanie sa odmietne s uvedením dotknutých firiem; účty sa automaticky nezlúčia. Pri neúplnom skene sa globálna jedinečnosť nedá potvrdiť: náhľad premenovania zostane blokovaný do overenia rozsahu. Súbežnú kolíziu v cieľovej firme vždy odmietne aj writer.
- Premenovať existujúce účty vo výsledne označených firmách; v novo označených firmách vytvoriť účet už s novým menom. Odoberané účty sa nepremenúvajú. Ak ochrana posledného správcu ponechá odznačený účet, zostane v tejto firme bezo zmeny mena aj hesla; nepovažovať ho automaticky za cieľ iných zmien.
- Premenovanie zachová lokálne ID, práva, heslo (ak nebola požadovaná jeho zmena) a historické väzby. Nesmie sa implementovať ako odstránenie a vytvorenie nového účtu. Účty webového rozhrania sa nepremenúvajú.
- Náhľad ukáže **pôvodné meno → nové meno** a konkrétne firmy. Slovný súhrn doplní „Používateľ sa premenuje v N firmách“. Tá istá firma môže obsahovať premenovanie aj zmenu hesla; počty sú označené podľa operácie, nie sčítané ako počet unikátnych firiem.
- Dávka aj rozpracovaný návrh používajú stabilnú identitu (server + databáza + lokálne ID), nie editované meno ako identifikátor účtu. Po čiastočnom úspechu môže prehľad obsahovať staré aj nové meno v samostatných riadkoch; report ich spojí identifikátorom operácie. Opakovanie cieli pôvodné lokálne ID iba v nedokončených firmách a akceptuje vlastné už úspešne premenované účty tejto operácie, nie cudzí účet s rovnakým menom.

### Heslo a rozpracovaný stav

- Rovnaká validácia na serveri aj klientovi: aspoň 6 znakov, bez požiadavky na malé/veľké písmená či číslicu; odmietnuť nepodporované znaky a prekročenie klientom overenej dĺžky. V prvej verzii použiť overené ASCII písmená/číslice; ďalšie znaky povoliť až po overení Uprav, Windows-1250 a klienta. Presný povolený rozsah uviesť pri poli.
- Text pri poli: „Omega ukladá heslo prevedené na veľké písmená. Zobrazené uložené heslo sa preto môže líšiť od zadaného.“ Neaplikovať toto pravidlo na existujúce heslá spätne; staršie slabé heslo nesmie blokovať čítanie či odobratie účtu.
- Reset obnoví posledný načítaný/potvrdený stav chipov aj mena, vymaže nové heslo a zruší nepotvrdené zmeny. Pri novom neuloženom riadku zruší návrh. Reset nevracia už vykonané zápisy.
- Riadok s rozdielom oproti pôvodnému stavu nemožno zbaliť. Ukáže „Najprv aplikujte zmeny alebo použite Reset“. Ani neúspešné aplikovanie alebo zrušenie potvrdenia neodblokuje zbalenie.
- Navigácia, obnovenie dát a odhlásenie musia riešiť rozpracované zmeny bez tichého zahodenia; pri zatvorení prehliadača použiť dostupnú ochranu pred odchodom. Heslá/návrhy sa neukladajú do localStorage.

## Výpočet a potvrdenie dávky

Pre jedného používateľa: B = pôvodné známe členstvo, D = požadované členstvo. Pridať D mínus B; odobrať B mínus D; ponechať prienik B a D. Pri zadanom novom hesle aktualizovať v prieniku len účty s odlišným efektívnym heslom. Novým účtom nastaviť zadané heslo pri vytvorení. Nedostupné/nejednoznačné položky neinterpretovať ako neprítomnosť.

1. Klient pošle návrh serveru na kontrolu a náhľad. Server určuje rozsah z povoleného katalógu, nepreberá ľubovoľný SQL server/databázu od klienta.
2. Server načíta aktuálny stav, validuje meno, heslo, podporu verzie, profil, duplicity, práva a závislosti. Pri zmene oproti pôvodnému snímku vráti konflikt na nové posúdenie.
3. Potvrdenie vypíše používateľa, konkrétne firmy a roky, pridania, odobratia, premenovania (staré → nové meno), zmeny hesla, nezmenené a blokované položky. Heslá v potvrdení neopakovať. Tlačidlá **Potvrdiť zmeny / Zrušiť**.
4. Potvrdenie sa viaže na presný serverový plán, admina, verziu stavu a krátku platnosť. Úprava návrhu ruší platnosť potvrdenia. Prípadné nové heslo držať iba krátkodobo v chránenej serverovej pamäti; po expirácii/reštarte vyžiadať nový náhľad.
5. Zápis transakčne po jednej DB, s opätovným porovnaním dotknutých záznamov a závislostí pod vhodným zámkom. Pri konflikte sa táto DB nemení. Vytváranie ID musí byť overené a odolné voči súbehu, nie nechránené MAX+1.
6. Výsledok po firme: úspech, bez zmeny, preskočené – posledný správca, konflikt, nepodporované, zlyhanie alebo neistý výsledok. Dávka cez viac DB nie je jedna atómová transakcia.
7. Po čiastočnom úspechu znovu načítať úspešné firmy ako nový základ; neúspešné návrhy zostanú rozpracované. Opakovanie vytvorí nový náhľad iba zostávajúcich zmien. Pri strate spojenia najprv zistiť stav operácie, neodosielať slepo znova.

Každá dávka má jedinečný identifikátor a uložené výsledky bez hesiel. Opakované potvrdenie nesmie znovu vykonať dokončené kroky. Neistý commit po páde procesu sa zosúladí s aktuálnou DB pred opakovaním. Na tej istej firme a účte nesmú súbežne bežať konfliktné dávky.

## V každej firme musí zostať aspoň jeden správca

Táto ochrana platí pre odznačenie chipu, odznačenie všetkých firiem aj hromadné odobratie označených používateľov. Rozhoduje stav konkrétnej firmy, nie ID 1 ani domnelý zakladateľ. Po operácii musí zostať aspoň jeden použiteľný správca Omega; zablokovaný účet alebo profil práv sa nesmie automaticky považovať za použiteľného správcu.

- Ak by odobratie účtu odstránilo posledného správcu, v danej firme sa účet **neodstráni**. Odobratia v ostatných firmách pokračujú podľa potvrdeného náhľadu.
- Pri výbere viacerých správcov posúdiť celý súbor odobratí v jednej firme. Ak by zmizli všetci, preskočiť odobratia správcov v tejto firme a vypísať, že admin musí aspoň jedného ponechať. Automaticky nevyberať, koho zachovať; odobratie bežných účtov môže pokračovať, ak firma má použiteľného správcu.
- Kontrola sa vykoná pri náhľade aj v transakcii tesne pred odobratím. Súbeh odoberania rôznych správcov v tej istej DB musí byť serializovaný databázovým zamykaním nad príslušným stavom správcov; kontrola iba jednotlivých účtov nestačí.
- Ak počet/použiteľnosť správcov nemožno spoľahlivo overiť, odobratie sa v tejto firme nevykoná s dôvodom „Nie je možné overiť zostávajúceho správcu“. Ak už firma nemá použiteľného správcu, operácie odoberania zablokovať a stav reportovať; nevytvárať náhradného správcu automaticky.
- V náhľade uviesť zoznam chránených firiem a chip označiť **Ponechaný – posledný správca**. Súhrn napríklad: „Používateľ sa odoberie z 8 firiem. V 2 firmách zostane, pretože je posledným správcom.“ Ak ochrana vznikne až počas zápisu, report uvedie aktuálny dôvod preskočenia.
- Výsledkový report obsahuje firmu, rok, používateľa, požadovanú operáciu a dôvod **Preskočené – posledný správca**. Preskočenie nie je úspešné odobratie ani všeobecná technická chyba.
- Po výsledku sa chránené členstvo zobrazí ako stále prítomné. Pôvodný nesplnený návrh odobratia zostane rozpracovaný s dôvodom, kým ho admin nezruší cez Reset alebo neodstráni príčinu a nepotvrdí nový náhľad. Aplikovať je neaktívne, ak ostali len známe blokované operácie.

## Pridávanie, premenovanie a odoberanie: podmienky správneho zápisu

Pred implementáciou writeru porovnať databázu pred a po operácii vykonanej oficiálnym klientom na ďalšej zapisovateľnej kópii: vytvorenie bežného účtu, priradenie profilu, zmena mena, zmena hesla, odobratie. Pri premenovaní overiť aj zachovanie historických väzieb a prihlásenie pod novým menom. Overiť všetky stĺpce, generovanie ID, `T005_PristupovePrava`, ďalšie väzby, auditné odkazy a prípadné nastavenia mimo firemnej DB. Výsledky uložiť do supporting_materials.

Odobrať znamená odobrať prístup spôsobom overeným v Omege. Ak klient účet deaktivuje namiesto fyzického zmazania, adaptér musí použiť rovnakú sémantiku a UI ju vysvetliť. Nikdy kaskádovo nemaž účtovné doklady ani historické autorstvo. Neoverenú operáciu zablokovať s dôvodom. Nedovoliť odobratie posledného použiteľného správcu; definíciu použiteľnosti overiť aj vo vzťahu k zamknutiu/právam. Kopírovanie profilu alebo celého účtu medzi DB podľa rovnakého číselného ID nie je prípustné.

Práva nových účtov sú zostávajúca implementačná otázka: ponúknuť miestny overený profil, prípadne explicitné mapovanie profilov pre dávku. Profil musí byť v náhľade viditeľný pre každú firmu. Výber mena z webu nesmie preniesť webovú rolu Admin do Omegy.

## Zapojenie do existujúcej aplikácie

Aktuálny projekt má React 19/TypeScript, .NET 10, vlastné Admin účty, konfiguráciu SQL serverov a read-only čítače. `CompanyDiscoveryReader` dnes filtruje názvy Omega cez `^x_[0-9]{9}$`; samotný názov nestačí na potvrdenie schémy. `CompanyDatabaseInfoReader` treba pri tejto práci zosúladiť s globálnymi hodnotami `C010_IDUzivatel = 0` a vracať rok samostatne. Nedostupná firma nesmie ukončiť celý sken.

| Oblasť / navrhované súbory | Zmena |
|---|---|
| `src/KrosExtension.Web/ClientApp/src/main.tsx`, `styles.css` | vnorené Admin → Omega menu, nová sekcia a ochrana odchodu |
| `src/KrosExtension.Web/ClientApp/src/admin/omega/*` | stránka, používateľský riadok, chipy, potvrdenie, výsledky, stavový model |
| `src/KrosExtension.Application/OmegaAdministration/*` | kontrakty katalógu, snímku, návrhu, validácie, náhľadu a vykonania |
| `src/KrosExtension.Infrastructure/OmegaAdministration/*` | codec, čítač, adaptéry verzií, writer, koordinátor dávok |
| `src/KrosExtension.Api/Endpoints/OmegaUserManagementEndpoints.cs` | nové admin-only API |
| `src/KrosExtension.Domain/OmegaAdministration/*` | evidencia dávok a výsledkov bez tajomstiev |
| `src/KrosExtension.Infrastructure/Persistence/*` | konfigurácia/migrácia operácií a zapisovacích pripojení |
| `src/KrosExtension.Infrastructure/DependencyInjection.cs`, `src/KrosExtension.Api/Program.cs` | registrácia služieb a endpointov |
| `tests/KrosExtension.Api.Tests/*` | codec, plánovanie, autorizácia, konflikty a integračné testy |

Nové zápisové pripojenie oddeliť od `CompanyReadConnectionFactory` a existujúcich dashboardov. Nevydávať `ApplicationIntent=ReadOnly` za SQL oprávnenie. Pre správu explicitne nakonfigurovať serverový účet s potrebnými overenými právami; nepovyšovať automaticky existujúci čítací účet. Pri chýbajúcom zápisovom prístupe funguje prehľad, zmeny sú nedostupné. Tajomstvá konfigurácie chrániť existujúcou Data Protection.

Navrhované API pod `/api/admin/omega/user-management`:

- `POST /scans`, `GET /scans/{id}`: spustenie/stav skenu, katalóg a agregované účty bez hesiel.
- `POST /users/{key}/password-details`: zoznam jedinečných uložených hesiel a priradenie k firmám pre rozbalený riadok.
- `POST /previews`: validovaný návrh a konkrétny náhľad s expiráciou.
- `POST /operations`: potvrdenie platného náhľadu s idempotency kľúčom.
- `GET /operations/{id}`: priebeh a výsledky bez hesiel.

Všetko chráni existujúca serverová Admin policy; UI skrytie nestačí. Použiť ochranu mutácií proti CSRF primeranú cookie autentifikácii. Návrhy mien využiť z existujúceho `/api/admin/users` bez prenosu webových hesiel. Heslové odpovede cez HTTPS, `Cache-Control: no-store`; neukladať ich do auditov, logov, telemetrie ani perzistentných snapshotov. Zbaliť/reset/odhlásiť odstráni načítané heslá z UI pamäte. Audit eviduje admina, čas, cieľ, typ operácie a výsledok; aj sprístupnenie hesiel zaznamená bez ich hodnôt.

## Implementačné kroky a výstupy

- [x] **1. Overiť baseline:** build backendu, existujúce testy, frontend build; zachovať nesúvisiace lokálne zmeny. 24. 10. 2026: `dotnet build` 0 chýb/0 varovaní, testy 76/76, `npm run build` OK.
- [ ] **2. Overiť klienta na kópiách:** zdokumentovať Uprav/UCase, dĺžky, mená, práva, create/rename/remove, ochranu zostávajúceho správcu a súbežný prístup. Výstup: nový podklad s podporovanými verziami a presnými zápisovými kontraktmi. Bez splnenia nemožno povoliť príslušné produkčné operácie.
- [ ] **3. Codec a katalóg:** preniesť overený codec do C#, testovať checksum a Windows-1250; odolný sken všetkých Omega DB, mená/rok a oddelenie profilov.
- [ ] **4. Čítací prehľad:** agregácia, počty, stavy neúplných údajov, admin-only detaily hesiel, nové menu a riadky.
- [ ] **5. Editor návrhu:** chipy, nové účty, editor mena s kontrolou duplicity, výber profilu, politika hesla, slovný súhrn, Reset, blokovanie zbalenia/odchodu a potvrdenie. Zatiaľ bez produkčného writeru.
- [ ] **6. Plánovač a writer:** serverový náhľad, kontrola konfliktov, transakcie, oprávnenia, ochrana posledného správcu po firmách vrátane hromadného výberu, premenovanie so zachovaním ID, report preskočení, idempotencia a audit; iba overené operácie/verzie.
- [ ] **7. Integračné a UI overenie:** celý tok cez webový proxy a následné prihlásenie do klienta Omega na zapisovateľných testovacích kópiách.
- [ ] **8. Prevádzkové dokončenie:** zdokumentovať zápisové účty, zálohu/obnovu pred dávkou, výsledky a riešenie čiastočného zlyhania. Produkčné nasadenie je samostatný krok po úspešnom overení.

## Akceptačné scenáre

1. Admin vidí presnú hierarchiu menu; bežný používateľ nedostane API údaje ani heslá (403), neprihlásený 401.
2. Sken zahŕňa aj nesledované firmy; jedna nedostupná DB nezruší výsledky ostatných. Profily typu 1 nie sú osoby. Rovnaké firmy/roky na rôznych serveroch sa nezlejú.
3. Jedno meno vo viacerých DB vytvorí jeden riadok s lokálnymi ID; konflikt dvoch účtov v jednej DB blokuje iba dotknutú operáciu. Duplicitný nový účet nevznikne ani pri súbežnej tvorbe.
4. Stavy zhodné/rozdielne/neoverené, zoznam jedinečných hesiel a hover zodpovedajú uloženým údajom; chybný checksum, NULL a prázdna hodnota sú rozlíšené. Testy používajú syntetické heslá.
5. Nové označenie/odznačenie má správnu farbu a popis; označiť všetko má jasný rozsah. Súhrn aj serverový náhľad majú rovnaké počty bez dvojitého započítania.
6. Prázdne nové heslo zachová staré heslá; pridanie účtu bez nového hesla je zamietnuté. Zadané heslo ovplyvní všetky výsledne označené firmy. Heslo zhodné po normalizácii nevytvorí zbytočný update.
7. Vstupy kratšie než 6 znakov alebo s nepodporovanými znakmi odmietne UI aj API. Klient Omega prijme nové heslo v overenej normalizovanej podobe.
8. Rozpracovaný riadok nemožno zbaliť; Reset obnoví základ. Zrušenie potvrdenia zachová návrh. Po úspechu sa načíta aktuálny stav a riadok sa môže zbaliť.
9. Simulovaná zmena v Omege medzi skenom, náhľadom a zápisom spôsobí konflikt, nie prepísanie. Dvojklik/opakovaný request nevykoná operáciu dvakrát.
10. Zlyhanie jednej DB nestratí výsledky ostatných. Po čiastočnom úspechu Reset nerobí rollback už potvrdených zmien. Neistý výsledok po strate spojenia sa preverí pred opakovaním.
11. Pridanie overeného bežného účtu umožní prihlásenie s očakávanými právami; odobratie zachová doklady a históriu. Posledného správcu nie je možné odobrať.
12. Odobratie účtu z 10 firiem, z ktorých je v 2 posledným správcom, vykoná 8 odobratí a vykáže 2 preskočenia s názvami firiem. Výber všetkých správcov jednej firmy ani dve súbežné dávky nesmú firmu ponechať bez správcu.
13. Tlačidlo Zmeniť otvorí editbox mena; samotné otvorenie neblokuje zbalenie, zmena hodnoty áno. Reset vráti pôvodné meno. Premenovanie bez zadania hesla zachová heslo aj práva.
14. Duplicitné meno vrátane normalizačných variantov odmietne UI aj server; súbežná kolízia neprepíše iný účet. Platné premenovanie zachová lokálne ID a historické väzby, klient Omega umožní prihlásenie pod novým menom.
15. Kombinácia premenovania, hesla a členstva má presný náhľad. Chránená odznačená firma ponechá pôvodný účet bezo zmeny; čiastočný úspech a opakovanie pracujú s pôvodnými lokálnymi ID a nespájajú cudzie účty.
16. Nová funkcia nemení webové účty ani existujúce dashboardy. Heslá sa nenachádzajú v logoch, audite, localStorage ani výsledkoch operácií.

Overenie pri implementácii: `dotnet build`, `dotnet test tests/KrosExtension.Api.Tests/KrosExtension.Api.Tests.csproj`, `npm run build` v ClientApp. Frontend dnes nemá test runner; pridať cielené testy stavového modelu a komponentov vhodným runnerom a vykonať manuálny priechod myšou, klávesnicou a dotykom. Databázové zápisy a testy klienta vykonávať na ďalších pracovných kópiách, nie na pôvodných zálohách ani existujúcich read-only analytických databázach.


