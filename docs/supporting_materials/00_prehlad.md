> Aktualizácia po skutočnom obnovení záloh: aktuálne nálezy sú v [06 – firmy](06_overene_firmy.md), [07 – používatelia](07_overeni_pouzivatelia.md) a [08 – protokol](08_protokol_obnovy.md). Text nižšie zachytáva predbežný stav pred obnovou a v prekonaných častiach už neplatí.
# Databázy KROS: výsledky kontroly a návrh správcu záloh

> Najnovší výsledok: [dekódované mená Omega a algoritmus](09_kodovanie_mien_omega.md). Firmy a tabuľky sú potvrdené v dokumentoch 06–08; zvyšok tohto prehľadu je predbežný historický stav.

Stav k 21. 10. 2026. Zdroj: `C:\Users\TUF\Documents\Kros extension\db`.

## Hlavné závery

- V adresári je sedem záloh s overenou hlavičkou `MSSQLBAK`: štyri firemné a tri systémové.
- `x_100001904` a `x_100121801` sú technické názvy databáz Omegy. Konkrétne obchodné názvy ich firiem zatiaľ nie sú zistené.
- Názvy firiem treba primárne hľadať vo firemných databázach a v zozname pripojených firiem aplikácie. Neexistuje potvrdenie, že ich tieto zálohy majú v konkrétnej tabuľke v `master` alebo `model`.
- Prístupy majú dve samostatné vrstvy: SQL účty a používateľov aplikácie KROS. Zoznam SQL účtov nemusí byť zoznamom účtovníkov používajúcich program.
- Pre bezpečné prezeranie odporúčam samostatnú SQL inštanciu alebo virtuálny počítač, vlastné dátové súbory a účet bez prístupu do produkcie. Obnova aktuálnej firmy je samostatný, výslovne spúšťaný proces.

## Dokumenty

1. [Inventár a hranice overenia](01_inventar.md)
2. [Kde hľadať názvy firiem](02_nazvy_firiem.md)
3. [Používatelia a prístupové práva](03_pouzivatelia_a_pristupy.md)
4. [Návrh správcu záloh](04_spravca_zaloh.md)
5. [Bezpečná obnova aktuálnych dát](05_obnova_produkcie.md)

## Čo sa vykonalo

Prečítal sa zoznam súborov, ich veľkosti a prvých osem bajtov. Skontrolovala sa dostupnosť miestnych SQL nástrojov a služieb a verejná dokumentácia KROS/Microsoft. Zálohy sa nemenili, neobnovovali ani nepripájali. Vytvorené boli iba tieto Markdown dokumenty.

Na tomto počítači je klient `sqlcmd`. Kontrola služieb neukázala SQL Server; nástroj `sqllocaldb` sa nenašiel medzi dostupnými príkazmi. Nie je tým vylúčený vzdialený SQL server ani iná neprístupná inštalácia. Žiadne pripojenie na produkčný SQL01 sa neskúšalo.

Presné názvy tabuliek a stĺpcov KROS, konkrétne firmy a konkrétnych používateľov bude možné potvrdiť až po obnovení firemných záloh v izolovanom prostredí. Táto správa nepredstiera vykonanú analýzu obsahu tabuliek.

