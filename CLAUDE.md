# Kros Extension — Claude Notes

## SQL queries

When writing SQL against Omega or Olymp databases, only use column names that are explicitly visible in the existing source code (e.g. `OmegaDashboardSnapshotCalculator.cs`, `CompanyDatabaseInfoReader.cs`). Never guess or invent column names. If a column is needed but not known, ask the user or suggest running `INFORMATION_SCHEMA.COLUMNS` first.
