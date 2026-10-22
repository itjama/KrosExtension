# Kros Extension – Nasadenie (Deploy)

## Požiadavky

- Windows 10/11 alebo Windows Server 2019+
- .NET 10 Runtime (alebo self-contained build — nie je potrebný)
- SQL Server alebo SQL Server Express
- PowerShell 5.1+, spustený ako **Administrátor**

---

## 1. Build

Spusti na vývojárskom PC z adresára `scripts\`:

```powershell
.\Build-Dist.ps1
```

Skript:
1. Spustí `npm install` + `npm run build` pre frontend
2. Publishuje API do `dist\api\`
3. Publishuje Web do `dist\web\`

---

## 2. Inštalácia na cieľovom PC

Skopíruj na cieľový PC:
- adresár `dist\`
- adresár `scripts\`

Spusti v PowerShell **ako Administrátor**:

```powershell
.\Install-Service.ps1
```

### Parametre (voliteľné)

| Parameter | Popis | Default |
|-----------|-------|---------|
| `-InstallDir` | Kam sa nainštalujú binárky | `C:\KrosExtension` |
| `-SqlServer` | Názov SQL Server inštancie | `.\SQLEXPRESS` |
| `-Database` | Názov databázy | `KrosExtension` |
| `-SourceDir` | Cesta k `dist\` adresáru | `..\dist` (vedľa `scripts\`) |
| `-HostName` | DNS meno, ktoré otvárajú používatelia (len pre výpis adresy) | názov PC |
| `-CertificatePath` + `-CertificatePassword` | PFX certifikát pre HTTPS | – |
| `-CertificateSubject` | Certifikát z `LocalMachine\My` (namiesto PFX) | – |
| `-BootstrapAdminPassword` | Heslo prvého admina (`admin`) | – |

Podrobnosti k HTTPS, certifikátu a prihláseniu v sekcii 7.

### Príklady

```powershell
# Základná inštalácia (SQL Express, C:\KrosExtension)
.\Install-Service.ps1

# Vlastný SQL server a adresár
.\Install-Service.ps1 -SqlServer "MYSERVER" -InstallDir "D:\Services\KrosExtension"

# Vlastná cesta k dist\
.\Install-Service.ps1 -SourceDir "C:\Temp\dist"
```

Skript automaticky:
- Zastaví a odstráni staré verzie služieb (pri aktualizácii)
- Skopíruje súbory do `InstallDir`
- Nastaví connection string v `appsettings.json`
- Vytvorí/aktualizuje `appsettings.Production.json` (Web aj Api) a obmedzí k nim prístup len na Administrátorov a SYSTEM
- Vytvorí adresár kľúčov `C:\ProgramData\KrosExtension\keys` a obmedzí k nemu prístup len na Administrátorov a SYSTEM
- Zaregistruje dve Windows Services (automatický štart)
- Spustí služby

**Databáza sa vytvorí automaticky** pri prvom spustení cez EF Core migrácie.

---

## 3. Odinštalácia

```powershell
# Len odstráni Windows Services (súbory ostanú)
.\Uninstall-Service.ps1

# Odstráni aj súbory
.\Uninstall-Service.ps1 -RemoveFiles

# Vlastný adresár
.\Uninstall-Service.ps1 -RemoveFiles -InstallDir "D:\Services\KrosExtension"
```

---

## 4. Firewall – sprístupnenie v lokálnej sieti

Aplikácia beží na dvoch portoch:

| Služba | Port | Popis |
|--------|------|-------|
| Web UI | **5284** | Používateľské rozhranie (toto otvárajú ostatní) |
| API | **5184** | Backend API (interná komunikácia, zvyčajne nepotrebné otvárať) |

### Pridanie výnimiek vo firewalle

Spusti **na PC kde beží aplikácia** ako Administrátor:

```powershell
netsh advfirewall firewall add rule name="KrosExtension Web" dir=in action=allow protocol=TCP localport=5284
```

> Port 5184 (API) **neotváraj** — API počúva len na localhost. Podrobnosti v sekcii 7.

### Overenie dostupnosti portu

Z iného PC v sieti spusti v PowerShell:

```powershell
Test-NetConnection -ComputerName 192.168.1.x -Port 5284
```

- `TcpTestSucceeded : True` → port je dostupný
- `TcpTestSucceeded : False` → port je blokovaný firewallom (pridaj výnimku vyššie)

### Prístup z iných zariadení

Po otvorení firewallu je aplikácia dostupná na:

```
https://<DNS-meno-servera>:5284
```

IP adresu servera zistíš príkazom `ipconfig` (hľadaj `IPv4 Address`).

---

## 5. Správa služieb

```powershell
# Stav
Get-Service KrosExtensionApi, KrosExtensionWeb

# Reštart
Restart-Service KrosExtensionApi
Restart-Service KrosExtensionWeb

# Logy (Event Viewer)
Get-EventLog -LogName Application -Source "Kros Extension*" -Newest 20
```

---

## 6. Aktualizácia

1. Spusti `Build-Dist.ps1` na vývojárskom PC
2. Skopíruj nový `dist\` na cieľový PC
3. Spusti `Install-Service.ps1` znovu — automaticky zastaví staré služby, nasadí nové a spustí ich

Existujúce `appsettings.Production.json` skript ponechá. Aktualizuje v nich **len** kľúče pre parametre, ktoré pri spustení výslovne zadáš:
- `-CertificatePath` / `-CertificatePassword` / `-CertificateSubject` → certifikát v `web\appsettings.Production.json`
- `-BootstrapAdminPassword` → `Bootstrap:AdminPassword` v `api\appsettings.Production.json`

### Prvá aktualizácia na verziu s prihlásením a HTTPS

Pri prvej aktualizácii zo staršej verzie (bez prihlásenia) **musíš** zadať `-HostName`, certifikát a heslo prvého admina, inak Web nenaštartuje (chýba certifikát) a nikto sa neprihlási (prázdne `AdminPassword`):

```powershell
.\Install-Service.ps1 -HostName ledgerpulse.firma.local -CertificateSubject "ledgerpulse.firma.local" -BootstrapAdminPassword "PrvotneHeslo123"
```

(alebo `-CertificatePath ... -CertificatePassword ...` namiesto `-CertificateSubject`). Ak si už aktualizoval bez týchto parametrov, stačí skript spustiť znova s nimi — doplní chýbajúce kľúče do existujúcich súborov. Potom pokračuj podľa sekcie 7 (Prvé prihlásenie).

---

## 7. Prihlásenie a HTTPS

Web UI beží **iba cez HTTPS** na porte 5284 (HTTP listener neexistuje). API počúva len na `http://localhost:5184`.

### Inštalácia s HTTPS

```powershell
# Certifikát zo súboru PFX
.\Install-Service.ps1 -HostName ledgerpulse.firma.local -CertificatePath C:\certs\lp.pfx -CertificatePassword "heslo" -BootstrapAdminPassword "PrvotneHeslo123"

# Certifikát z úložiska Windows (LocalMachine\My)
.\Install-Service.ps1 -HostName ledgerpulse.firma.local -CertificateSubject "ledgerpulse.firma.local" -BootstrapAdminPassword "PrvotneHeslo123"
```

Skript vytvorí `C:\KrosExtension\web\appsettings.Production.json` a `C:\KrosExtension\api\appsettings.Production.json`, ak neexistujú. Existujúce súbory neprepíše — zmení v nich len certifikát, resp. `Bootstrap:AdminPassword`, ak zadáš príslušný parameter (pozri sekciu 6).

### Bezpečnosť konfigurácie

- **Heslo PFX** (`-CertificatePassword`) je uložené v čitateľnej podobe v `web\appsettings.Production.json`. Ak chceš heslo v konfigurácii nemať, použi certifikát z úložiska Windows (`-CertificateSubject`).
- `Bootstrap:AdminPassword` je v čitateľnej podobe v `api\appsettings.Production.json` — po prvom prihlásení ho vymaž (pozri nižšie).
- Heslá zadané ako parametre príkazového riadku sa uložia do **histórie PowerShellu** (`(Get-PSReadLineOption).HistorySavePath`). Po inštalácii z nej príslušné riadky odstráň, prípadne spusti inštaláciu v relácii bez ukladania histórie (`Set-PSReadLineOption -HistorySaveStyle SaveNothing`).
- Prístup k inštalačnému adresáru (`C:\KrosExtension`) a k adresáru kľúčov `C:\ProgramData\KrosExtension\keys` obmedz len na **Administrátorov a SYSTEM**. Skript to pre oba `appsettings.Production.json` a pre adresár kľúčov robí automaticky; pre celý inštalačný adresár napr.:

```powershell
icacls C:\KrosExtension /inheritance:r /grant:r "*S-1-5-32-544:(OI)(CI)F" "*S-1-5-18:(OI)(CI)F"
```

### DNS

Na internom DNS serveri vytvor záznam (A / CNAME) napr. `ledgerpulse.firma.local` smerujúci na server. Používatelia otvoria `https://ledgerpulse.firma.local:5284`.

### Firewall

- Port **5284** povoľ len z LAN (`-RemoteAddress LocalSubnet` alebo konkrétny rozsah).
- Port **5184** (API) **nikdy neotváraj** — API je dostupné iba z localhost.

```powershell
New-NetFirewallRule -DisplayName "KrosExtension Web (HTTPS)" -Direction Inbound -Protocol TCP -LocalPort 5284 -RemoteAddress LocalSubnet -Action Allow
```

### Certifikát

- **Firemná CA** — vyžiadaj certifikát pre DNS meno servera a nainštaluj ho do `LocalMachine\My` (alebo exportuj ako PFX).
- **Self-signed:**

```powershell
New-SelfSignedCertificate -DnsName ledgerpulse.firma.local -CertStoreLocation Cert:\LocalMachine\My
```

  Verejnú časť (`.cer`) exportuj a na klientskych PC ju importuj do `Trusted Root Certification Authorities` (napr. cez GPO), inak bude prehliadač hlásiť varovanie.

### Prvé prihlásenie

1. Prihlás sa ako `admin` s heslom z `-BootstrapAdminPassword`. Aplikácia ťa vyzve na zmenu hesla.
2. Po zmene hesla **vymaž hodnotu `AdminPassword`** v `C:\KrosExtension\api\appsettings.Production.json` (nechaj prázdny reťazec) a reštartuj `KrosExtensionApi`.

### Núdzová obnova admina

Ak sa žiadny admin nevie prihlásiť (zabudnuté heslo, zablokovaný účet):

1. V databáze `KrosExtension` (napr. cez SQL Server Management Studio) dočasne zablokuj všetkých adminov:

   ```sql
   UPDATE AppUsers SET IsBlocked = 1 WHERE Role = 2;
   ```

   (`Role = 2` je Admin, `1` je bežný používateľ.) Bootstrap admina obnoví iba vtedy, keď neexistuje žiadny aktívny admin.
2. V `C:\KrosExtension\api\appsettings.Production.json` nastav `Bootstrap:AdminPassword` na nové dočasné heslo (min. 10 znakov). `Bootstrap:AdminUserName` určuje, ktorý účet sa obnoví (default `admin`).
3. Reštartuj službu: `Restart-Service KrosExtensionApi`. Účet `AdminUserName` sa odblokuje (alebo vytvorí, ak neexistuje), dostane rolu Admin a heslo z kroku 2.
4. Prihlás sa ako `AdminUserName` s týmto heslom; aplikácia ťa vyzve na zmenu hesla — zmeň ho.
5. Vymaž hodnotu `AdminPassword` v `api\appsettings.Production.json` (prázdny reťazec) a znova reštartuj `KrosExtensionApi`.
6. V **Admin → Používatelia** odblokuj ostatných adminov, ktorých si v kroku 1 zablokoval.

### Záloha kľúčov

Kľúče v `C:\ProgramData\KrosExtension\keys` sú šifrované pomocou DPAPI **viazaného na tento počítač**. Na inom serveri (alebo po preinštalovaní Windows) ich nie je možné použiť ani zo zálohy.

- **Záloha adresára má zmysel len pre obnovu na tom istom počítači a tej istej inštalácii Windows** (napr. po omylom zmazanom adresári).
- **Pri presune na nový server** (alebo ak sa kľúče stratia):
  - používatelia sa musia znova prihlásiť (staré relácie neplatia),
  - uložené SQL heslá serverov na čítanie dát sa nedajú dešifrovať — v aplikácii sa správajú ako prázdne. Pri štarte Api zapíše do logu varovanie s ID servera a stĺpcom (`OlympPassword` / `OmegaPassword`). Takéto servery **odstráň a pridaj znova** so zadaním SQL hesla.
