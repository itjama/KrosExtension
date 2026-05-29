# Project Configuration Notes

## 2026-05-28 - Data Read Servers

Understanding before implementation:

- `KrosExtension` is the configuration database owned by this backend.
- The application must store SQL servers from which Olymp/Omega data can be read.
- On a given SQL server, all Olymp databases share one username and password.
- On the same SQL server, all Omega databases share one username and password.
- Olymp and Omega credentials can be different from each other.
- Company/Olymp/Omega databases are read-only from this application perspective.

Implementation plan:

- Add a configuration entity for a data read server.
- Store per-server Olymp and Omega read credentials.
- Add Minimal API endpoints to add, list and delete these servers.
- Do not return passwords from API responses.
- Update database catalog reading to use configured servers from `KrosExtension`.
- Add and apply an EF Core migration.

## 2026-05-28 - Local Test Databases

Understanding before execution:

- Test backup files for Olymp and Omega are stored in `data/input`.
- Restore the Omega backup into local SQL Server LocalDB with database name `x_720566404`.
- Restore the Olymp backup into local SQL Server LocalDB with database name `mzdyCanberra_Packar`.
- These restored databases will be used as local read-only test data sources for development.

Execution plan:

- Inspect backup files in `data/input`.
- Read logical file names from each backup.
- Restore each backup to `(localdb)\MSSQLLocalDB` using explicit `MOVE` paths.
- Verify restored databases exist.

Execution result:

- Restored Omega backup to database `x_720566404`.
- Restored Olymp backup to database `mzdyCanberra_Packar`.
- Data/log files were placed in `data/localdb`.
- Both databases were verified as `ONLINE` in `(localdb)\MSSQLLocalDB`.

## 2026-05-28 - Backend and Frontend Roadmap

Understanding before implementation:

- The backend remains the primary integration layer for SQL Server, Olymp and Omega data.
- A separate web frontend is required and will use backend APIs for all reads and configuration changes.
- The frontend will have separate Olymp and Omega sections.
- Server configuration must support reading databases from configured SQL servers.
- Olymp databases are identified by names starting with `mzdy`.
- Omega databases are identified by names matching `x_` followed by a nine-digit number.
- Users will choose tracked companies with checkboxes, then save that selection to `KrosExtension`.
- Statistics will later show one row per tracked company with counts for 7 days, 30 days and monthly averages.

Implementation plan:

- Use a standalone `.NET 10` web host project for the frontend.
- Use `React + TypeScript + Vite` for the client UI.
- Make the frontend host runnable as a Windows Service and future Docker-friendly.
- Add authentication mode support for data read servers: Windows or SQL login per product.
- Add tracked company storage to `KrosExtension`.
- Add discovery APIs for Olymp and Omega company databases.
- Add initial frontend project skeleton and wire it into the Visual Studio solution.
