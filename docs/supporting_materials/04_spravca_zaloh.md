# Návrh správcu záloh Omega/Olymp

Toto je návrh budúceho programu, nie už nainštalovaná alebo odskúšaná aplikácia.

## Odporúčaná architektúra

**Katalóg na HDD → kontrola SQL zálohy → izolovaná obnova → čítač metadát → zobrazenie firmy a prístupov.**

Produkčná obnova má samostatný režim a samostatné oprávnenie. Bežný skener a analytický účet nemajú mať možnosť meniť produkciu.

| Súčasť | Potrebné správanie |
|---|---|
| Katalóg | Skenuje vybrané adresáre, rozpozná formát podľa obsahu, eviduje veľkosť a hash SHA-256; nemení originály |
| SQL pracovník | Číta metadáta záloh a vykonáva obnovu na výslovne určenej testovacej inštancii |
| Úložisko kópií | Vlastný povolený adresár pre všetky dátové/logové súbory každej úlohy |
| Čítače Omega/Olymp | Verziované dotazy len na potrebné metadáta; neznáma schéma sa označí ako nepodporovaná |
| Katalóg výsledkov | Uloží identitu firmy, čas a stav kontroly, nie hromadnú kópiu účtovných či mzdových dát |
| Denník operácií | Kto, kedy, akú zálohu a cieľ použil; výsledok, chyba, trvanie a identifikátor úlohy |

Samostatná SQL inštancia chráni pred zámenou databázy, no pri rovnakom počítači stále zdieľa výkon a disk s produkciou. Pre silnejšie oddelenie použiť samostatný počítač alebo VM bez prístupu do produkčných zdieľaní.

## Čo bude potrebné pripraviť

1. SQL Server Database Engine kompatibilný so zálohami, jeho verziu a edíciu vybrať po prečítaní metadát. Samotný `sqlcmd` či SSMS nestačí. Novší server nie je automatická záruka podpory všetkých starých záloh a funkcií; preferovať kompatibilnú verziu zodpovedajúcu prevádzke.
2. Licenciu SQL vhodnú na skutočný spôsob použitia a objem dát; nepredpokladať automaticky vhodnosť edície Developer pre prevádzkový nástroj.
3. Účet SQL služby s čítaním záloh a zápisom do testovacích dátových adresárov. Cesta k zálohe sa vyhodnocuje na SQL serveri, nie na počítači s používateľským rozhraním. Pri sieti použiť vhodnú UNC cestu alebo riadené skopírovanie.
4. Dostatočný disk podľa `FILELISTONLY`, rezerva na rast, kontroly a súbežné úlohy; limity počtu súčasných obnovovaní.
5. Ak sú zálohy šifrované alebo databáza používa TDE, príslušné certifikáty/kľúče s privátnymi kľúčmi a potrebné heslá. Ukladať ich oddelene od bežného katalógu.
6. Ak sa má kópia otvárať aj v KROS programe: zodpovedajúca verzia, licencia a oddelené pomocné súbory a integrácie. Samotná SQL analýza a otvorenie v KROS sú dva odlišné testy.

Šifrovaná obnova vyžaduje zodpovedajúci kľúč/certifikát. SQL podporuje obnovu pod iným názvom a na nové súbory. [Microsoft: obnova na nové miesto](https://learn.microsoft.com/en-us/sql/relational-databases/backup-restore/restore-a-database-to-a-new-location-sql-server)

## Bezpečný priebeh jednej úlohy

1. Počkať na dokončenie zápisu zdrojového súboru; zaznamenať jeho identitu a hash. Pred obnovou opätovne overiť, že zdroj nebol nahradený.
2. Načítať LABELONLY, HEADERONLY a FILELISTONLY. Vybrať konkrétny backup set; jeden súbor môže obsahovať viac záloh a jedna záloha môže byť rozdelená medzi viac súborov.
3. Overiť typ, kompletnosť médií, verziu a požadované kľúče. Pri chýbajúcom súbore alebo neznámom type skončiť vysvetlením.
4. Spustiť VERIFYONLY. Úspech znamená overenie zálohy v rozsahu tohto príkazu, nie ešte zdravú firemnú databázu.
5. Vytvoriť unikátny cieľ, napríklad `inspect_<id_ulohy>`. Overiť cieľovú inštanciu, neexistenciu databázy a kolízií súborov.
6. Obnoviť s `MOVE` pre každý súbor do vlastného adresára. V analytickom režime zakázať `WITH REPLACE`, prepis existujúcej databázy aj použitie pôvodných fyzických ciest.
7. Po obnove vykonať `DBCC CHECKDB` a čítanie metadát cez obmedzený účet. Pre čistú SQL analýzu možno kópiu následne nastaviť iba na čítanie; KROS pri otvorení môže potrebovať zápis.
8. Zobraziť oddelene stavy „formát rozpoznaný“, „overenie zálohy úspešné“, „obnovenie úspešné“, „kontrola konzistencie úspešná“ a „firma identifikovaná“.
9. Čistiť len databázy a súbory vedené v registri vlastných úloh. Samotný prefix názvu nie je dostatočný dôkaz vlastníctva. Po páde programu najprv zosúladiť stav úloh so serverom.

Pri prvom vydaní podporovať úplné databázové zálohy. Diferenciálne a logové zálohy pridať až s kontrolou LSN, vetvy obnovy, správnej základnej zálohy a poradia. Dátumy v názvoch súborov na skladanie reťazca nestačia.

## Osobitosti KROS

Názov `inspect_...` je vhodný na SQL prieskum. Pre otvorenie v Omege nemožno predpokladať podporu ľubovoľného premenovania. KROS dokumentuje tvar `x_ID`; na samostatnej inštancii možno pre aplikačný test zachovať pôvodný názov, pokiaľ neexistuje, a klienta pripojiť výhradne do testovacieho prostredia. [KROS: názov pri obnove](https://akademia.kros.sk/faq/podvojne-uctovnictvo/zaloha-obnova-sql-databaz-a-automaticka-zaloha/)

Pomocné adresáre, dokumenty, fotografie a integračné nastavenia môžu byť mimo samotnej SQL databázy. Obnovená kópia nesmie počas aplikačného testu používať produkčné adresáre ani odosielať reálne výstupy. Historický návod Olympu výslovne spomína zdieľané dokumenty/fotografie. [KROS: Olymp na MS SQL](https://new.kros.sk/wp-content/uploads/2018/09/SQL_postup_Olymp.pdf)

## Rozsah prvého vydania a overenie

Prvé vydanie: katalóg, vyhľadávanie podľa firmy/IČO/obdobia, izolovaná obnova úplnej zálohy, zistenie identity a čitateľný protokol. Produkčnú obnovu zaradiť až po odskúšaní samostatného postupu.

Akceptačné skúšky: platná Omega/Olymp záloha; neúplný alebo poškodený súbor; chýbajúci kľúč; nedostatok disku; existujúci cieľ; chýbajúca časť zálohy; neznáma schéma; zmena zdroja počas práce; pád programu a opätovné spustenie. V každom chybovom scenári musia originálne zálohy a produkcia zostať nedotknuté.
