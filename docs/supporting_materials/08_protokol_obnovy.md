# Protokol obnovy a systémové databázy

Vykonané 21. 10. 2026. Tento dokument aktualizuje pôvodný stav „bez obnovenia“ v dokumentoch 00–03. Dokumenty 04–05 zostávajú návrhom budúceho správcu záloh a obnovy produkcie; produkčná obnova nebola vykonaná.

## Prostredie

- Zdroj: sedem `.bak` v `C:\Users\TUF\Documents\Kros extension\db`.
- HEADERONLY: všetky majú BackupType=1, Position=1, SoftwareVersionMajor=15 a Build=2000, teda SQL Server 2019.
- EncryptorType bol prázdny; obnova nevyžadovala dodanie externých kľúčov. Toto nie je všeobecný audit všetkých foriem šifrovania.
- Vytvorená samostatná LocalDB inštancia `(localdb)\KrosAnalysis20261021`, SQL Server 2025, verzia 17.0.4025.3.
- Každý cieľ dostal prefix `inspect_` a nové dátové/logové súbory cez MOVE v pracovnom adresári tejto úlohy `work\sql-data`. REPLACE sa nepoužil.
- Vykonané HEADERONLY, FILELISTONLY, VERIFYONLY, RESTORE, CHECKDB a SELECT na potrebné metadáta.

## Výsledok

| Kópia | Obnova | CHECKDB |
|---|---|---|
| inspect_x_100001904 | úspešná | bez hlásených chýb |
| inspect_x_100121801 | úspešná | bez hlásených chýb |
| inspect_mzdyADIKAs_r_o_ | úspešná | bez hlásených chýb |
| inspect_mzdyEHClinicals_r_o | úspešná | bez hlásených chýb |
| inspect_model | úspešná | bez hlásených chýb |
| inspect_msdb | úspešná | bez hlásených chýb |
| inspect_master | obnovená, ONLINE, čitateľná | 1 alokačná a 5 chýb konzistencie |

Prvý strojový protokol označil master ako Failed, pretože obnova a CHECKDB boli v spoločnom bloku. Následná kontrola potvrdila úspešné vytvorenie a ONLINE stav; chybu vrátil kontrolný krok.

Chyby master sa týkajú `sys.syssingleobjrefs` a alokácie stránky 1:10. Záloha systémovej databázy bola obnovená ako používateľská kópia na novšom hlavnom vydaní SQL Servera. Bez overenia na zodpovedajúcom SQL Serveri 2019 preto tieto chyby nemožno vyhlásiť za poškodenie pôvodnej databázy. Žiadna oprava typu REPAIR nebola spustená.

## Master a model

V master sa medzi používateľskými objektmi našla tabuľka `CommandLog` a procedúry `CommandExecute`, `DatabaseBackup`, `DatabaseIntegrityCheck`, `IndexOptimize`. Nenašla sa tu samostatná používateľská tabuľka registra firiem. Model nemal používateľské tabuľky.

Pre identitu štyroch dodaných firiem sú rozhodujúce ich vlastné tabuľky, popísané v [overených firmách](06_overene_firmy.md).

## Msdb a chýbajúce zálohy

Msdb obsahuje históriu záloh v `dbo.backupset` a úlohy v `dbo.sysjobs`, vrátane názvov pre denné, týždenné, mesačné a manuálne zálohy Omega/Olymp. Úlohy neboli prenesené do aktívnej msdb ani spustené. Enabled=1 samo nepreukazuje aktívny plán alebo úspešné vykonanie; plány a história úloh neboli auditované.

História eviduje aj databázy **Kros** a **Olymp_INI**, so zálohami zachytenými 16. 10. 2026. Tieto súbory neboli dodané. Môžu obsahovať spoločné nastavenia, ale bez ich schémy to nie je potvrdené. História záloh tiež nezaručuje, že súbory na pôvodnom úložisku ešte existujú.

Pre správcu záloh je preto vhodné samostatne podporiť firemné databázy aj prípadné spoločné databázy a externé súbory. Na jednoduché zobrazenie názvu týchto štyroch firiem spoločné databázy potrebné nie sú.

## Stav po analýze

Analytické databázy sú nastavené READ_ONLY. Inštancia sa po dokončení zastaví; kópie zostávajú zachované pre ďalšie skúmanie. Originálne zálohy neboli zapisované a produkčný SQL01 sa nekontaktoval. Predchádzajúci hash nebol zaznamenaný, preto sa netvrdí nezávislé porovnanie hashov pred/po.

Kópie boli otvorené novším SQL enginom. Na obnovu pôvodnej produkcie sa majú používať pôvodné vhodné zálohy, nie automaticky nové zálohy týchto upgradovaných kópií.

Otvorenie v klientovi KROS, vecná správnosť účtovníctva/miezd, dekódovanie mien Omega a úplné pôvodné SQL loginy zostávajú mimo overeného výsledku. Presná lokalizácia názvov firiem a aplikačných používateľov je už potvrdená.
