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
netsh advfirewall firewall add rule name="KrosExtension API" dir=in action=allow protocol=TCP localport=5184
```

> Port 5184 (API) stačí otvoriť len ak chceš Swagger (`/swagger`) prístupný z iných PC. Pre bežné používanie stačí port 5284.

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
http://<IP-adresa-servera>:5284
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
