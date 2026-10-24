> Aktualizácia po skutočnom obnovení záloh: aktuálne nálezy sú v [06 – firmy](06_overene_firmy.md), [07 – používatelia](07_overeni_pouzivatelia.md) a [08 – protokol](08_protokol_obnovy.md). Text nižšie zachytáva predbežný stav pred obnovou a v prekonaných častiach už neplatí.
# Inventár záloh

Kontrola: 21. 10. 2026. Cesty sú relatívne k `C:\Users\TUF\Documents\Kros extension\db`.

| Súbor | Bajty | MiB | Zaradenie |
|---|---:|---:|---|
| `SQL01_master_FULL_20261018_210001.bak` | 4781056 | 4,56 | systémová |
| `SQL01_model_FULL_20261018_210001.bak` | 434176 | 0,41 | systémová |
| `SQL01_msdb_FULL_20261018_210001.bak` | 11973632 | 11,42 | systémová |
| `Omega/x_100001904/SQL01_x_100001904_FULL_20260930_231502.bak` | 8667648 | 8,27 | Omega |
| `Omega/x_100121801/SQL01_x_100121801_FULL_20260930_231502.bak` | 5676544 | 5,41 | Omega |
| `Olymp/SQL01_mzdyADIKAs_r_o__FULL_20260930_230001.bak` | 3379200 | 3,22 | Olymp |
| `Olymp/SQL01_mzdyEHClinicals_r_o_FULL_20260930_230004.bak` | 1680896 | 1,60 | Olymp |

Všetkých sedem súborov má hlavičku `MSSQLBAK`, zodpovedajúcu natívnej zálohe Microsoft SQL Server. Označenie FULL, server SQL01, dátumy a názvy databáz sú zatiaľ údaje z názvov súborov, nie z výsledku `RESTORE HEADERONLY`. Hlavička sama nepotvrdzuje integritu ani úplnosť zálohy.

Názvy Olymp záloh naznačujú firmy ADIKA a EH Clinical. Presný právny názov, IČO a obdobie nie sú overené z databázy. Mená súborov nemajú byť rozhodujúcim identifikátorom pri obnove.

## Čo ešte treba zistiť cez SQL Server

| Kontrola | Účel |
|---|---|
| `RESTORE HEADERONLY` | Skutočná databáza, typ a pozícia backup setu, čas zálohy, verzia pôvodného SQL Servera a údaje pre obnovovací reťazec |
| `RESTORE LABELONLY` | Identifikácia média a kontrola, či záloha pozostáva z viacerých súborov |
| `RESTORE FILELISTONLY` | Logické súbory, ich typy a veľkosti potrebné na obnovu |
| `RESTORE VERIFYONLY` | Kontrola čitateľnosti a úplnosti zálohy |
| Izolované obnovenie a `DBCC CHECKDB` | Praktická overiteľnosť obnovy a kontrola konzistencie obnovenej databázy |

`VERIFYONLY` nenahrádza obnovu ani kontrolu štruktúry dát. [Microsoft: VERIFYONLY](https://learn.microsoft.com/en-us/sql/t-sql/statements/restore-statements-verifyonly-transact-sql)

Veľkosť `.bak` nie je požiadavka na voľný disk. Rozbalené dátové a logové súbory môžu byť podstatne väčšie; treba počítať aj s priestorom na kontroly a pomocné operácie.

Systémové zálohy sú podľa názvu z 18. 10., firemné z 30. 9. 2026. Prístupy a konfigurácia preto nemusia predstavovať rovnaký časový stav.

Natívne SQL zálohy a archívy vytvorené funkciou programu KROS treba rozlišovať a spracovávať podľa formátu. [KROS: záloha a obnova SQL](https://akademia.kros.sk/faq/podvojne-uctovnictvo/zaloha-obnova-sql-databaz-a-automaticka-zaloha/)

