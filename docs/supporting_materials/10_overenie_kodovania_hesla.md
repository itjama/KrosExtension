# Overenie kódovania hesla – záznam DANKA

Overené 21. 10. 2026 na analytických kópiách `x_100001904` a `x_100121801`. Používateľ poskytol známe heslo na porovnanie. Heslo ani uložená heslová hodnota nie sú súčasťou tohto dokumentu.

## Výsledok

V oboch databázach sa hodnota `dbo.T004_Uzivatelia.C101_Heslo` pre DANKA dala prečítať rovnakou XOR transformáciou ako používateľské meno. Výsledok sa presne zhodoval s používateľom dodaným heslom po prevode na veľké písmená.

| Kontrola | IMPEXTA SK | Mybox |
|---|---|---|
| Formát uloženého reťazca | hexadecimálny | hexadecimálny |
| Dĺžka uloženého reťazca | 22 znakov | 22 znakov |
| Dĺžka dekódovaného obsahu | 10 znakov | 10 znakov |
| Zhoda s dodaným heslom v pôvodnej veľkosti písmen | nie | nie |
| Zhoda s dodaným heslom prevedeným na veľké písmená | áno | áno |

Pre tieto dva záznamy teda nejde o jednosmerný hash hesla. Je uložená spätne čitateľná podoba hesla vo veľkých písmenách.

## Rozsah potvrdenia

Transformácia je rovnaká ako pri menách: hex na bajty, prvý obsahový bajt XOR 0x04, ďalšie obsahové bajty XOR predchádzajúci uložený bajt, interpretácia Windows-1250. Posledný uložený bajt bol vynechaný; jeho význam sa týmto porovnaním neurčil.

Výsledok silno naznačuje normalizáciu hesiel na veľké písmená v tejto verzii. Prihlásenie cez klienta Omega sa však netestovalo, takže toto nie je samostatný dôkaz správania všetkých prihlasovacích ciest ani všetkých verzií programu. Je to potvrdenie uloženého obsahu dvoch konkrétnych historických záznamov.

## Dôsledok pre správcu záloh

Prístup k týmto databázovým zálohám môže umožniť obnoviť aj aplikačné heslá. Správca záloh má heslové stĺpce vylúčiť z bežného exportu, protokolov a používateľského prehľadu. Katalóg mien a práv ich nepotrebuje.

## Vykonané operácie

Čítal sa iba heslový stĺpec pre záznam s kódovaným menom DANKA v dvoch analytických databázach. Porovnanie prebehlo v pamäti a výstup obsahoval len dĺžky a výsledky zhody. Heslá ostatných používateľov sa nečítali. Databázové záznamy ani heslá sa nemenili a produkčný server sa nekontaktoval.
