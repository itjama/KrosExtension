> Aktualizácia po skutočnom obnovení záloh: aktuálne nálezy sú v [06 – firmy](06_overene_firmy.md), [07 – používatelia](07_overeni_pouzivatelia.md) a [08 – protokol](08_protokol_obnovy.md). Text nižšie zachytáva predbežný stav pred obnovou a v prekonaných častiach už neplatí.
# Používatelia a prístupy Omega/Olymp

## Tri rôzne informácie

| Vrstva | Kde ju skúmať | Čo znamená |
|---|---|---|
| Prihlásenie na SQL server | `master`, najmä `sys.server_principals`, `sys.server_role_members`, `sys.server_permissions` | SQL login alebo Windows používateľ/skupina, ktorý sa pripája k serveru |
| Oprávnenia v databáze | Obnovená firemná databáza: `sys.database_principals`, `sys.database_role_members`, `sys.database_permissions` | Databázový používateľ, roly a explicitné oprávnenia |
| Používateľ programu KROS | Aplikačná správa používateľov a zatiaľ neidentifikované aplikačné tabuľky | Meno účtovníka, aplikačná rola a obmedzenia funkcií |

SQL rozlišuje serverové a databázové identity. [Microsoft: Principals](https://learn.microsoft.com/en-us/sql/relational-databases/security/authentication-access/principals-database-engine)

Omega spravuje aplikačné prístupy cez **Firma – Užívatelia, prístupové práva**. To potvrdzuje existenciu samostatnej aplikačnej vrstvy, nie názov jej fyzickej tabuľky. [KROS: používateľské prístupy Omega](https://podpora.kros.sk/migration/faq/nastavenie-u%C5%BE%C3%ADvate%C4%BEsk%C3%BDch-pr%C3%ADstupov-v-programe-omega)

Starší SQL návod Olympu popisuje Windows prihlásenie, systémový účet Olympu aj vlastný SQL účet. Viacerí pracovníci teda môžu používať spoločný technický login. Návod zároveň spomína procedúry potrebné po prenose na nový server. Pred pripojením obnovenej databázy do dnešnej verzie treba ich potrebu overiť. [KROS: Olymp na MS SQL, kapitoly 2.2 a 3.3](https://new.kros.sk/wp-content/uploads/2018/09/SQL_postup_Olymp.pdf)

Ide o historický návod, nie aktuálnu bezpečnostnú konfiguráciu. Jeho odporúčania na široké práva nemožno automaticky preniesť na nový nástroj.

## Čo záloha obsahuje a čo nemusí

Firemná záloha prenáša databázových používateľov a oprávnenia a prípadné aplikačné tabuľky v danej databáze. Bežné serverové loginy sa s ňou neprenesú. Pri inom serveri treba riešiť väzbu používateľa na login cez SID; zhodné meno nestačí. Existujú aj contained users bez bežného serverového loginu, preto treba identifikovať typ autentifikácie.

Záloha `master` je relevantná pre historické serverové identity; neznamená kompletný zoznam osôb pracujúcich v programe. Členstvo Windows skupín je spravované mimo tejto zálohy. Úplný efektívny prístup navyše zahŕňa serverové roly, vlastníctvo databázy, databázové roly, GRANT/DENY a administrátorov.

Výpis `sys.database_permissions` sám osebe nepreukazuje všetky efektívne práva. Zálohy tiež nepreukazujú aktuálne prístupy: stav sa mohol od ich vytvorenia zmeniť.

## Odporúčaný výstup budúcej analýzy

- **SQL vrstva:** meno a typ loginu, stav, členstvo v rolách, databázový používateľ a spôsob mapovania. Bez hesiel a hashov.
- **KROS vrstva:** aplikačné meno/identifikátor, aktívnosť a roly, až keď sa overí schéma.
- **Výnimky:** nespárovaní používatelia, spoločné účty, skupinové oprávnenia a neúplná viditeľnosť metadát.
- **Čas a pôvod:** z ktorej zálohy alebo servera údaje pochádzajú; historický a aktuálny stav osobitne.

Pre prvotnú kontrolu na izolovanej firemnej kópii možno použiť nasledujúci dotaz. Nebol spustený a nie je úplným auditom prístupov:

```sql
SELECT name, type_desc, authentication_type_desc, sid
FROM sys.database_principals
ORDER BY name;

SELECT r.name AS role_name, m.name AS member_name
FROM sys.database_role_members AS rm
JOIN sys.database_principals AS r ON r.principal_id = rm.role_principal_id
JOIN sys.database_principals AS m ON m.principal_id = rm.member_principal_id;
```

Na testovacej inštancii nemožno považovať jej `sys.server_principals` za zoznam loginov pôvodného SQL01. Ten treba získať samostatným oprávneným exportom zo zdroja alebo špecializovaným skúmaním systémovej zálohy v oddelenom laboratóriu. Neobnovovať starý `master` cez funkčný server len kvôli zoznamu účtov.

