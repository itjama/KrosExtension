# Kros Extension

Zakladny .NET 10 backend postaveny nad Clean Architecture:

- `KrosExtension.Domain` - domenove entity a pravidla bez zavislosti na infrastrukture.
- `KrosExtension.Application` - use-cases, kontrakty a aplikacne sluzby.
- `KrosExtension.Infrastructure` - EF Core, MSSQL a implementacie externych zavislosti.
- `KrosExtension.Api` - Minimal API, Swagger a hostovanie aj ako Windows Service.

## Spustenie

```powershell
dotnet restore
dotnet build
dotnet run --project .\src\KrosExtension.Api\KrosExtension.Api.csproj
```

Swagger UI bude dostupne na `/swagger`.

## Konfiguracia databazy

Connection string pre vlastnu konfiguracnu databazu sa cita z `ConnectionStrings:DefaultConnection` v `src/KrosExtension.Api/appsettings.json`.

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=KrosExtension;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

## Databazy firiem

Projekt rozlisuje dva typy databaz:

- `KrosExtension` - nasa konfiguracna databaza pre nastavenia, dostupne databazy a mapovanie firiem.
- Databazy firiem - externe databazy, ku ktorym sa pristupuje iba na citanie.

Zoznam SQL Serverov, na ktorych sa ma citat katalog databaz, je v sekcii `DatabaseCatalog:Servers`.

Aktualny smer je ukladat tieto servery do konfiguracnej databazy `KrosExtension`
cez API `/api/data-read-servers`. Hesla sa cez API odpovede nevracaju.

Discovery firiem:

- `GET /api/olymp/companies/discover`
- `GET /api/omega/companies/discover`

Ulozenie sledovanych firiem:

- `PUT /api/olymp/companies/tracked`
- `PUT /api/omega/companies/tracked`

## Frontend

Frontend je samostatny projekt `src/KrosExtension.Web`.

- .NET 10 host je pripraveny na beh ako Windows Service.
- Klientska cast je `React + TypeScript + Vite` v `ClientApp`.
- V development mode Vite proxy smeruje `/api` na backend `http://localhost:5184`.
- Produkcny build sa servuje z `ClientApp/dist`.

```powershell
cd .\src\KrosExtension.Web\ClientApp
npm install
npm run build
cd ..\
dotnet run --launch-profile http
```

Web bude dostupny na `http://localhost:5284`.

## Windows Service

Projekt je pripraveny na hostovanie ako Windows Service cez `UseWindowsService()`. Po publikovani je mozne sluzbu vytvorit napr. cez `sc.exe` alebo PowerShell `New-Service`.
