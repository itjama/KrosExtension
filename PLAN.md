# Plan

## Current Goal

Continue building the Kros Extension application: a .NET backend and React web UI for configuring read-only access to Kros Olymp/Omega company databases, selecting tracked companies, and later showing statistics.

## What We Have

- Solution `KrosExtension.sln` with Clean Architecture projects:
  - `KrosExtension.Domain`
  - `KrosExtension.Application`
  - `KrosExtension.Infrastructure`
  - `KrosExtension.Api`
  - `KrosExtension.Web`
- .NET 10 backend with Minimal API, Swagger, health checks, and Windows Service hosting.
- Configuration database concept: `KrosExtension` stores our own settings, available databases, data read servers, and tracked company selections.
- External Olymp/Omega company databases are treated as read-only sources.
- EF Core persistence with migrations:
  - `InitialCreate`
  - `AddDataReadServers`
  - `AddTrackedCompaniesAndAuthenticationModes`
- Data read server model supports:
  - display name and SQL Server name
  - Olymp authentication mode and credentials
  - Omega authentication mode and credentials
  - Windows or SQL login mode per product
  - trust server certificate flag
  - API responses do not return passwords
- API endpoints currently present:
  - `GET /health`
  - database/system endpoints
  - `GET /api/data-read-servers`
  - `POST /api/data-read-servers`
  - `DELETE /api/data-read-servers/{id}`
  - `GET /api/olymp/companies/discover`
  - `GET /api/omega/companies/discover`
  - `GET /api/olymp/companies/tracked`
  - `GET /api/omega/companies/tracked`
  - `PUT /api/olymp/companies/tracked`
  - `PUT /api/omega/companies/tracked`
- Discovery rules noted in project config:
  - Olymp databases start with `mzdy`
  - Omega databases match `x_` plus nine digits
- Local test databases were restored to LocalDB:
  - Omega: `x_720566404`
  - Olymp: `mzdyCanberra_Packar`
  - files placed under `data/localdb`
  - after moving the workspace, both databases were detached and reattached so SQL Server points to `E:\Projects\Kros extension\data\localdb`
- Frontend project exists at `src/KrosExtension.Web/ClientApp`.
- Frontend stack:
  - React 19
  - TypeScript
  - Vite
  - lucide-react icons
- Frontend currently has:
  - sidebar navigation: Servery, Olymp, Omega
  - server list/add/delete screen
  - company discovery screens for Olymp and Omega
  - checkbox selection for tracked companies
  - save tracked company selection
- `KrosExtension.Web` serves production build from `ClientApp/dist` and is also Windows Service ready.
- Project moved from `C:\Users\TUF\Documents\Kros extension` to `E:\Projects\Kros extension`.

## Known Issues

- Sandboxed command execution in Codex still fails with `setup refresh failed with status exit code: 1`; commands currently need escalation/workaround.
- Git repository appears to have no commits; after the move, `git status` shows all files as untracked.
- Statistics screens are not implemented yet.

## Next

- Fix/confirm workspace command execution or continue with approved command workaround.
- Review backend build status with `dotnet build`.
- Review frontend build status with `npm run build`.
- Add better UX around loading, empty states, and API errors.
- Prepare scheduled per-product refresh settings and an `UpdateData` backend contract before final dashboard statistics shape is chosen.
- Add initial tests once the next backend behavior is selected.

## Recent Changes

- Added proxying in `KrosExtension.Web` so `/api/...` and `/health` requests from the web frontend are forwarded to the backend configured by `Api:BaseUrl`.
- Fixed mojibake Slovak labels in `ClientApp/src/main.tsx`.
- Enabled string enum JSON handling in the API so frontend values like `Windows` and `SqlLogin` bind correctly.
- Verified `dotnet build`, `npm run build`, `/health`, `/api/data-read-servers`, Olymp discovery, and a temporary add/delete data-read-server smoke test through the web proxy.
- Added frontend controls for Olymp and Omega authentication mode, SQL login username, and SQL login password when adding a data read server.
- Verified a temporary add/delete data-read-server smoke test with separate Olymp and Omega SQL login credentials; API response confirms credentials exist and does not return passwords.
- Added a duplicate data-read-server check so adding an existing server returns `409 Conflict` instead of a database exception.
- Fixed LocalDB attachment paths for `mzdyCanberra_Packar` and `x_720566404`; both now have `HAS_DBACCESS = 1` and can be queried.
- Verified Olymp/Omega discovery through the web proxy; Olymp returns `mzdyCanberra_Packar` and Omega returns `x_720566404`.
- Added visible frontend loading, result count, empty state, and error messages for company discovery.
- Omega discovery now reads company name and accounting year from `T000_INI` and displays `CompanyName (Year)` instead of the raw database name.
- Main navigation changed: top-level `Olymp` and `Omega` are statistics pages; configuration pages moved under expandable `Settings` with `Servery`, `Olymp`, and `Omega`.
- Added backend statistics endpoint `/api/{product}/companies/statistics` for tracked companies with 7-day activity, 30-day activity, and 30-day daily average.
- Shared company identity reading between discovery and statistics, so statistics refresh the real company name and accounting year directly from the selected Olymp/Omega database instead of relying on old saved labels.
- Verified backend and frontend builds after the statistics/navigation changes.
- Verified through the web proxy:
  - Omega discovery/statistics show `MMbau SK, s. r. o. (2026)`.
  - Olymp discovery/statistics show `Canberra-Packard, s.r.o. (2026)`.
- Planned refresh preparation:
  - Add `refresh time` to Settings / Olymp and Settings / Omega.
  - Store refresh time per product in `KrosExtension`.
  - Prepare backend `UpdateData` operation callable for one company or all tracked companies.
  - Future refresh will store processed data locally; dashboard should read local snapshots instead of live company databases.
- Added refresh preparation:
  - New `ProductRefreshSettings` table stores per-product refresh time.
  - `TrackedCompanies` now stores `AccountingYear` locally.
  - `GET/PUT /api/{product}/refresh-settings` reads and saves refresh time.
  - `POST /api/{product}/companies/update-data` refreshes local company metadata for all tracked companies or one `databaseName`.
  - Settings / Olymp and Settings / Omega now show a `Refresh time` input.
  - Statistics now use locally stored company metadata; name/year changes appear after UpdateData or saving a refreshed selection.
  - Verified `dotnet build`, `npm run build`, EF migration application, refresh settings endpoints, and update-data endpoints.
- Added first Omega dashboard snapshot pipeline:
  - New `OmegaCompanyDashboardSnapshots` table stores local aggregated dashboard data in `KrosExtension`.
  - `UpdateData` for Omega now writes a dashboard snapshot per tracked company.
  - New `GET /api/omega/dashboard/companies` returns latest local snapshot per company, sorted by health score.
  - Top-level Omega page now reads the local dashboard endpoint instead of live company databases.
  - Global Omega risk table shows health score, 7/30 day activity, posting rate, backlog, old backlog, last activity, trend, and warning flags.
  - Verified migration, `dotnet build`, `npm run build`, `UpdateData`, and `/api/omega/dashboard/companies`.

## Commands

```powershell
dotnet restore
dotnet build
dotnet run --project .\src\KrosExtension.Api\KrosExtension.Api.csproj
```

```powershell
cd .\src\KrosExtension.Web\ClientApp
npm install
npm run build
cd ..\
dotnet run --launch-profile http
```

## Open Questions

- Should the web UI call the backend API directly as a separate service, or should `KrosExtension.Web` eventually proxy/host API routes too?
- Should passwords be encrypted at rest in the configuration database now, or deferred until service installation hardening?
- What exact statistics should be shown first for tracked companies: counts for 7 days, 30 days, monthly average, or another MVP metric?
