# Agent Notes

## Working Agreement

- Keep `PLAN.md` updated during work when the plan, status, or next steps change.
- Store detailed implementation plans in `docs/superpowers/plans/`. Store research findings and supporting evidence in `docs/supporting_materials/`. `PLAN.md` remains the lightweight status and next-steps summary.
- Use `PLAN.md` as the lightweight project memory so the workspace does not need to be rescanned from scratch every turn.
- Before meaningful edits, check the current plan and update it if the work changes direction.
- Mark completed items in `PLAN.md` as soon as they are actually done.
- Do not revert user changes unless the user explicitly asks for it.

## Project Practices

- Prefer small, focused changes that match the existing project structure.
- Read relevant files before changing behavior.
- Verify changes with the project’s existing build/test commands when available.
- Record unresolved questions, assumptions, and follow-up tasks in `PLAN.md`.

## Notes To Improve Later

- Add project-specific build, test, and run commands once confirmed.
- Add extension architecture notes once the main files are reviewed.
- Add release or packaging steps once known.

## Supporting Materials

- Project root: `E:\Projects\Kros extension`. Save future research results and planning evidence in `docs/supporting_materials/`, and keep its [topic index](docs/supporting_materials/README.md) current.
- Read the index before relying on older reports: later verified findings supersede preliminary assumptions. These materials concern source KROS company databases; Kros Extension's own application users/authentication are separate.
- Company identities and schema queries: [verified Omega/Olymp companies](docs/supporting_materials/06_overene_firmy.md).
- Source application users and permissions: [tables and access findings](docs/supporting_materials/07_overeni_pouzivatelia.md), supplemented by [decoded Omega names and profiles](docs/supporting_materials/09_kodovanie_mien_omega.md).
- Omega name/password codec: [verified checksum and complete algorithm](docs/supporting_materials/13_omega_posledny_bajt.md), [standalone Python codec](docs/supporting_materials/omega_codec.py), and [known-password verification](docs/supporting_materials/10_overenie_kodovania_hesla.md). Document 13 supersedes earlier statements that the trailing byte is unknown.
- Olymp name/password storage: [verified formats and PBKDF2 comparison](docs/supporting_materials/11_olymp_mena_a_hesla.md).
- Backups and restore: [backup-manager design](docs/supporting_materials/04_spravca_zaloh.md), [production restore proposal](docs/supporting_materials/05_obnova_produkcie.md), and [actual restore/check results](docs/supporting_materials/08_protokol_obnovy.md).
- Future bulk administration of source KROS users across companies: [shared-manager requirements](docs/supporting_materials/12_spolocny_spravca_kros.md). Treat this as a proposal; follow PLAN.md/source for current implementation status. Client-tested account writes, full rights interpretation, and original SQL01 logins remain unresolved by this research.


## SQL queries

When writing SQL against Omega or Olymp databases, only use column names that are explicitly visible in the existing source code (e.g. `OmegaDashboardSnapshotCalculator.cs`, `CompanyDatabaseInfoReader.cs`). Never guess or invent column names. If a column is needed but not known, ask the user or suggest running `INFORMATION_SCHEMA.COLUMNS` first.