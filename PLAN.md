# Plan

Last reconciled: 2026-10-24 against current source files, documentation, and Git history.

## Current Goal

Implement **Admin → Omega → Manažment používateľov** according to the active [Omega user-management plan](docs/superpowers/plans/2026-10-24-omega-user-management.md). This is the current feature priority, selected by the user on 2026-10-24.

Scope: users across Omega companies, company membership, password comparison/change, username editing with duplicate checks, and per-company protection/reporting for the last remaining administrator. Preserve existing read-only dashboards and keep source Omega accounts separate from web application accounts.

Status: plan selected; implementation has not started. Source-account create/rename/remove and rights behavior must be verified on writable test copies before enabling those writes in production.
## Completed / Implemented

- [x] Clean Architecture solution: Domain, Application, Infrastructure, Api, and Web.
- [x] Configuration database `KrosExtension` with EF Core migrations; external company databases are read-only sources.
- [x] Data read server management with separate Windows/SQL authentication for Olymp and Omega, certificate trust settings, duplicate detection, and no passwords in API responses.
- [x] Company discovery and tracked-company selection for both products; company names and accounting years read from source databases.
- [x] Settings pages for servers, Olymp, and Omega; discovery loading, empty, and error states.
- [x] Per-product refresh time and `UpdateData` for all tracked companies or a selected database.
- [x] Local dashboard snapshots for Omega and Olymp, dashboard list/detail endpoints, health scores, activity metrics, and warning flags.
- [x] Omega dashboard metrics extended with late entries, posting lag, and timeliness scoring; orphan-payment detail endpoint exists.
- [x] Dashboard filters by company name and warning flags with persisted client state.
- [x] `DataRefreshBackgroundService` checks schedules every minute using the host's local time. Last-run dates are held in memory.
- [x] Login/logout, cookie sessions, forced password change, Admin/User roles, login rate limiting, and admin-only configuration endpoints.
- [x] Initial admin bootstrap and user administration UI/API, including protection against an admin resetting their own password.
- [x] SQL passwords encrypted at rest with Data Protection; legacy plaintext migration and startup warnings for secrets that cannot be decrypted.
- [x] Web proxy forwards API/health requests; cookie isolation between users and client IP forwarding are implemented.
- [x] HTTPS hosting and Windows Service build/install/uninstall scripts; deployment, upgrade, admin recovery, and key backup instructions in `readme_deploy.md`.
- [x] Backend test project covering authentication, permissions, bootstrap, user administration, rate limits, encryption, and user domain behavior.

These checkboxes record implementation presence, not a fresh runtime validation of every feature.

## Architecture / Entry Points

- `src/KrosExtension.Api/Program.cs`: migrations, admin bootstrap, secret migration/diagnostics, authentication, endpoint registration, and scheduled refresh service.
- `src/KrosExtension.Api/Endpoints`: product discovery, tracked companies, statistics, dashboard lists/details, refresh settings, UpdateData, authentication, and admin users.
- `src/KrosExtension.Infrastructure`: SQL readers, refresh/snapshot persistence, configuration, and account services.
- `src/KrosExtension.Web/Program.cs`: frontend hosting and backend proxy.
- `src/KrosExtension.Web/ClientApp`: React 19, TypeScript, Vite, lucide-react; dashboards/settings in `src/main.tsx`, authentication in `src/auth.tsx`, users in `src/admin/UsersPage.tsx`.
- `tests/KrosExtension.Api.Tests`: backend test suite.
- `docs/db-analysis`, `docs/schema-map.md`, `docs/backend-data-contract.md`: source database analysis and data contract notes.
- `docs/superpowers/specs` and `docs/superpowers/plans`: detailed feature designs and implementation plans. Older unchecked steps are historical and require reconciliation before reuse.

## Current Workspace / Verification

- Active workspace: `E:\Projects\Kros extension` (previously `C:\Users\TUF\Documents\Kros extension`).
- Git is initialized and has commits. Latest inspected commit: `c61f5f2` — deployment upgrade, admin recovery, and key backup documentation fixes.
- Existing local changes observed before this plan update: `src/KrosExtension.Api/appsettings.json`, `src/KrosExtension.Web/ClientApp/tsconfig.tsbuildinfo`, and untracked `.superpowers/brainstorm` files. Preserve them; they were not changed for this task.
- Normal workspace command execution succeeded during this review; the old command-execution blocker is no longer current.
- Historical verification in the previous plan recorded successful backend/frontend builds, LocalDB reattachment, discovery, refresh settings, UpdateData, and Omega dashboard endpoint checks.
- Historical LocalDB fixtures: `x_720566404` (Omega, MMbau SK, s. r. o.) and `mzdyCanberra_Packar` (Olymp, Canberra-Packard, s.r.o.), with files under `data/localdb`. Their current runtime availability was not rechecked.
- This update reviewed source, documentation, and Git state only. Builds, tests, database access, and deployed services were not rerun or verified on 2026-10-24.

## Next

Follow the active [Omega user-management plan](docs/superpowers/plans/2026-10-24-omega-user-management.md) in order. Selecting it does not itself start implementation or deployment.

- [x] Verify the build/test baseline (2026-10-24: `dotnet build` 0 errors/0 warnings, backend tests 76/76 passed, `npm run build` OK).
- [ ] Verify Omega account writes, normalization and rights on writable test copies.
- [ ] Implement the codec/catalog and read-only administration overview.
- [ ] Implement membership, rename and password drafts, preview, Reset and last-admin reporting.
- [ ] Implement verified writers with concurrency protection, then complete integration/client tests and operating instructions.

### Remaining general verification backlog

The following checks remain recorded for regression and follow-up; choosing a new feature is no longer pending.

- [ ] Establish a fresh baseline with `dotnet build`, the backend test suite, and `npm run build`; record actual results here.
- [ ] Verify the login/password-change flow and Admin/User permissions through the web proxy, including separate simultaneous user sessions.
- [ ] Verify both dashboards, company details, filters, and manual UpdateData against available local test databases.
- [ ] Verify scheduled refresh behavior, including failures and service restarts; decide whether persistent run state and retry behavior are needed.
- [ ] Validate the current distribution on a test installation: HTTPS certificate, API loopback binding, upgrade of existing configuration, admin recovery, and Data Protection key backup/restore.
- [ ] Reconcile older detailed implementation-plan checkboxes with verified outcomes when those features are next touched.
- [x] Select Omega user management as the next feature; focused tests are specified in its active implementation plan.

## Commands

Run from the repository root unless a working directory is specified.

```powershell
dotnet restore
dotnet build
dotnet test .\tests\KrosExtension.Api.Tests\KrosExtension.Api.Tests.csproj
dotnet run --project .\src\KrosExtension.Api\KrosExtension.Api.csproj
```

Frontend (working directory `src/KrosExtension.Web/ClientApp`):

```powershell
npm install
npm run build
npm run dev
```

Web host (working directory `src/KrosExtension.Web`):

```powershell
dotnet run --launch-profile http
```

Distribution (repository root; installation details and prerequisites in `readme_deploy.md`):

```powershell
.\scripts\Build-Dist.ps1
```

Service installation and removal use `scripts/Install-Service.ps1` and `scripts/Uninstall-Service.ps1` on the target machine with administrator rights.

## Open Questions / Assumptions

- Current feature priority is Omega user management. Remaining feature-specific questions concern verified account write semantics, local rights profiles and input normalization; see the active detailed plan.
- Scheduled refresh currently uses the host's local time and in-memory last-run dates. Confirm required timezone, restart, and retry semantics before changing it.
- Production installation, certificate configuration, recovery, and key backup/restore need current environment verification; tooling presence alone does not confirm deployment success.
- The earlier questions about a web proxy, encrypting SQL passwords, and initial dashboard metrics have been resolved by the current implementation.

## Supporting-materials consolidation — 2026-10-24

- [x] Consolidated the prior Omega/Olymp research reports 00–13, both codecs, and aggregate checksum test evidence into `docs/supporting_materials/`; source copies were retained.
- [x] Added a topic index and references from AGENTS.md, including precedence of later verified findings over preliminary reports. Save subsequent research findings/supporting evidence here; detailed implementation plans belong in `docs/superpowers/plans/`.
- [x] Verified copied source-file hashes before indexing, destination file inventory, and relative Markdown links. This was a documentation-only task; application builds/tests and database operations were not rerun.
- Research result: the Omega trailing byte is a verified 8-bit checksum; see document 13. The historical source-account administration proposal in document 12 does not describe the current implementation of Kros Extension's own users.
- Still open for that proposed feature: KROS-client verification of account writes/login, complete interpretation of rights, original SQL01 logins, and missing Kros/Olymp_INI backups. These are research gaps, not new implementation commitments.

## Omega user management plan — 2026-10-24

- [x] Created [detailed Omega user-management plan](docs/superpowers/plans/2026-10-24-omega-user-management.md) for Admin → Omega → Manažment používateľov, using supporting materials and current source structure.
- [ ] Implement after verifying source-client create/remove/rights semantics and password input normalization on writable test copies; no application or source-database changes were made while planning.
- Scope: all discovered Omega company databases on active configured servers, including those not tracked by dashboards. Web users remain separate and only supply name suggestions.
- Proposed behavior: company chips represent desired membership; deselecting an existing membership proposes removal. A new password applies to all resulting selected companies. Adding memberships requires a new password and a verified local rights profile.
- Important constraint: Omega uppercases passwords before encoding. Per user decision on 2026-10-24, the password policy is min. 6 characters only, with no lower/upper-case requirement. Uprav, length limits, complete account writes and client login remain to be verified.
- [x] Extended the Omega plan with per-company last-admin protection: skip/report protected removals, continue eligible companies, and validate combined removals and concurrent operations.
- [x] Added inline username editing via Zmeniť, normalized uniqueness checks, preview/reset behavior and rename with stable local IDs. Rename is included in the required client verification on test copies; no feature code or database writes were performed.

