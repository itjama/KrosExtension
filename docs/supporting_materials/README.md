# Podklady pre ďalšie plánovanie Kros Extension

Zjednotené 24. 10. 2026. Hlavný projekt: `E:\Projects\Kros extension`.

Sem ukladať ďalšie overené zistenia, rozbory a podklady pre návrhy. Podrobné implementačné plány ukladať do `docs/superpowers/plans/`. Pri zmene záveru aktualizovať tento index a odkazy v hlavnom AGENTS.md; pracovný stav a otvorené kroky viesť v PLAN.md. Podklady sa týkajú zdrojových databáz KROS Omega/Olymp. Používatelia a autentifikácia samotnej aplikácie Kros Extension sú samostatná téma.

## Aktuálne zistenia podľa tém

| Téma | Dokument | Čo poskytuje |
|---|---|---|
| Firmy a ich identifikácia | [06 – overené firmy](06_overene_firmy.md) | Tabuľky, kľúče, názvy firiem, IČO a dotazy Omega/Olymp |
| Používatelia a práva | [07 – používatelia](07_overeni_pouzivatelia.md) | Aplikačné tabuľky a príznaky práv, oddelenie od SQL loginov |
| Čitateľné mená Omega | [09 – dekódované mená](09_kodovanie_mien_omega.md) | Zoznam mien a profilov; neistota koncového bajtu je už vyriešená dokumentom 13 |
| Kompletný codec Omega | [13 – kontrolný bajt](13_omega_posledny_bajt.md) | Implementačne potvrdený algoritmus, súčet, maska 0x75 a overenie na zálohách |
| Overenie hesla Omega | [10 – známy pár](10_overenie_kodovania_hesla.md) | Potvrdenie spätnej transformácie a veľkých písmen pre konkrétne záznamy |
| Mená a heslové formáty Olymp | [11 – Olymp](11_olymp_mena_a_hesla.md) | Čitateľné mená, overený PBKDF2-HMAC-SHA512 a ďalší neurčený 32-bajtový formát |
| Obnova a systémové databázy | [08 – protokol obnovy](08_protokol_obnovy.md) | Výsledky CHECKDB, obmedzenia master a chýbajúce zálohy Kros/Olymp_INI |
| Správca záloh | [04 – návrh](04_spravca_zaloh.md) | Izolované obnovenie, katalóg, kontroly a prevádzkové požiadavky |
| Obnova aktuálnych údajov | [05 – postup](05_obnova_produkcie.md) | Návrh odstávky, záchrannej zálohy, obnovy a návratu |
| Spoločná správa mnohých firiem | [12 – návrh správcu](12_spolocny_spravca_kros.md) | Požiadavky na hromadnú správu zdrojových KROS účtov; nejde o potvrdenie implementácie |

## Kód a dôkazy

- [omega_codec.py](omega_codec.py): úplné kódovanie/dekódovanie s kontrolným bajtom, bez prístupu do databáz. Volajúci rieši aplikačnú normalizáciu vstupu samostatne.
- [omega_codec_test_results.json](omega_codec_test_results.json): úplná zhoda 27 mien, 15 jedinečných vektorov a zamietnutie zmenených kontrolných bajtov.
- [omega_password_checksum_results.json](omega_password_checksum_results.json): súhrn kontrolných súčtov 27 heslových polí, bez hesiel a uložených heslových reťazcov.
- [decode_omega_name.ps1](decode_omega_name.ps1): starší pomocný dekodér; posledný bajt ešte neoveruje. Pre nové plánovanie použiť kompletný Python modul a dokument 13.

## Platnosť a prekonané závery

- Dokumenty 00–03 zachytávajú predbežnú fázu pred skutočným obnovením. Ako hlavné podklady používať 06–08.
- Dokument 13 nahrádza tvrdenia v 09, 10 a 12 o neznámom poslednom bajte: ide o overovaný kontrolný súčet.
- Dokument 09 dopĺňa pôvodne nečitateľné mená z dokumentu 07. Dokument 11 obsahuje aj následné výpočtové overenie PBKDF2 zo známeho hesla.
- Dokumenty 04, 05 a 12 sú návrhy. Aktuálny stav implementovaného projektu je v hlavnom PLAN.md a zdrojových súboroch.
- Úplný codec Omega je potvrdený statickou analýzou podpísaného modulu 29.60 a zhodou s dodanými zálohami uvádzajúcimi 29.50.0. Úplný produkčný postup zmeny účtu a následné prihlásenie cez KROS ešte neboli týmto výskumom otestované.
- Pôvodné SQL loginy SQL01, význam všetkých príznakov práv a obsah nedodaných databáz Kros/Olymp_INI zostávajú neoverené.

## Pôvod materiálov

Importované správy 00–12 a starší dekodér pochádzajú z pôvodného pracovného priečinka Codex pod `C:\Users\TUF\Documents\Codex\2026-10-21\referenced-chatgpt-conversation-this-is-an\outputs`. Dokument 13, úplný codec a súhrnné výsledky testov pochádzajú z `C:\Users\TUF\Documents\Kros extension`.

Zdrojové kópie boli ponechané. Cieľom pre ďalšie plánovacie podklady je už tento priečinok. Do projektu sa pri tejto konsolidácii nekopírovali databázy, inštalačné moduly ani pracovné súbory s heslovými hodnotami.
