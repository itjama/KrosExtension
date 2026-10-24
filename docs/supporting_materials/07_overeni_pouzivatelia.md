# Používatelia a práva: skutočné nálezy

> Aktualizácia: mená Omega boli následne úspešne dekódované. Presný postup a zoznam mien sú v [09 – kódovanie mien](09_kodovanie_mien_omega.md). Nižšie uvedené tvrdenia o nezistených menách zachytávajú skorší stav; ostatné zistenia zostávajú platné.

Stav zo záloh preskúmaných 21. 10. 2026, nie aktuálny audit produkcie. Táto správa nahrádza predbežné tvrdenia o neznámych tabuľkách v dokumente 03. Heslá, hashe hesiel a aplikačné tajomstvá sa do správy neexportovali.

## Omega

Záznamy používateľov a súvisiacich typov sú v `dbo.T004_Uzivatelia`.

| Stĺpec | Pozorovanie |
|---|---|
| C000_ID | Identifikátor záznamu |
| C100_Meno | Nečitateľný kódovaný/obfuskovaný reťazec |
| C103_Spravca | Príznak správcu; nájdené hodnoty -1 a 0 |
| C003_Zamknuty | Vo všetkých skúmaných riadkoch 0 |
| C104_PristupProgram | Číselné hodnoty prístupu; presný význam neoverený |
| C112_TypZaznamu | Typy 0 a 1; nie každý riadok musí znamenať osobu |
| C012_IDSkupina | Vo všetkých skúmaných riadkoch 0 |
| C105_PristupFormulare, C107_PristupOstatne | Textové stĺpce ďalších oprávnení |

| Databáza | Riadky spolu | Typ 0 | Typ 1 | ID s C103_Spravca=-1 |
|---|---:|---:|---:|---|
| x_100001904 | 14 | 9 | 5 | 1, 10, 14 |
| x_100121801 | 13 | 8 | 5 | 1, 7, 8, 27 |

Typ 1 má v oboch databázach ID 2–6 a záznamy v `dbo.T005_PristupovePrava`. To naznačuje šablóny alebo skupiny práv, ale význam typu nie je potvrdený v aplikácii. Preto nejde o potvrdený počet fyzických používateľov.

Tabuľka `T005_PristupovePrava` má väzbu `C010_IDUzivatel`, úroveň `C011_Uroven`, právo `C101_Pravo`, zdedené právo `C102_ZdedenePravo` a identifikátory radu. Obsahuje 1076 riadkov pre IMPEXTA a 1098 pre Mybox. Číselné hodnoty zatiaľ nie sú preložené na jednotlivé funkcie programu.

```sql
SELECT C000_ID, C100_Meno, C103_Spravca, C003_Zamknuty,
       C104_PristupProgram, C112_TypZaznamu, C012_IDSkupina
FROM dbo.T004_Uzivatelia;
```

Čitateľné mená používateľov Omega zostávajú nezistené. Na ich zobrazenie treba overený dekodér alebo export/zobrazenie cez klienta Omega. Samotná podoba reťazca nedokazuje konkrétny šifrovací algoritmus. Nebola vykonaná zmena ani reset hesiel.

## Olymp

Používatelia sú priamo v `dbo.A_Uzivatelia`. Čitateľné meno je v `meno`, práva v `prava`, identifikátor v `ID_uzivatel`.

| Firma | ID | Meno | prava – surová hodnota | zamknuty |
|---|---:|---|---:|---:|
| ADIKA | 1 | adika | 256 | 0 |
| ADIKA | 2 | Hosť | 16 | 0 |
| ADIKA | 3 | Gabika | 0 | 0 |
| EH Clinical | 1 | adika | 256 | 0 |
| EH Clinical | 2 | Hosť | 16 | 0 |
| EH Clinical | 3 | z5 | 0 | 0 |

Číselné práva nie sú nezávisle zmapované na roly; hodnota 0 sa preto neinterpretuje ako „žiadny prístup“. Historický príznak zamknutia nepreukazuje dnešnú možnosť prihlásenia.

Ďalšou tabuľkou je `dbo.prava` so stĺpcami `ID_uzivatel`, `ID_stredisko`, `ID_objekt`, `prava`. V oboch zálohách je prázdna. Existujú aj tabuľky `pravaPripravene` a dočasné tabuľky práv; ich aplikačný význam nebol dekódovaný.

```sql
SELECT ID_uzivatel, meno, prava, zamknuty
FROM dbo.A_Uzivatelia;
```

Schémy sa líšia aj medzi týmito dvoma zálohami: ADIKA má napríklad stĺpce `loginEmail` a `webPrihlasovanieAktivne`, EH Clinical ich nemá. Čítač musí overovať existenciu stĺpcov.

## SQL loginy a efektívny prístup

Vo všetkých štyroch firemných kópiách výber `sys.database_principals` s `principal_id>4`, bez rolí, nevrátil ďalších používateľov. V členstve databázových rolí sa našiel dbo v db_owner. To neznamená, že do pôvodnej databázy nikto nemal prístup: serverové administrátorské a spoločné účty môžu pristupovať cez dbo.

Pôvodné serverové loginy SQL01 neboli spoľahlivo získané. Serverové katalógy analytickej LocalDB opisujú testovací server, nie originálny SQL01. Obnovená kópia master má navyše chyby CHECKDB. Preto nevydávame miestne loginy za produkčné.

Úplný audit SQL prístupov vyžaduje read-only export loginov a rolí z SQL01 alebo samostatné kompatibilné laboratórium pre systémovú obnovu. Členstvo Windows skupín treba zistiť mimo firemnej databázy. Aplikačné mená a SQL účty sú dve rozdielne vrstvy.
