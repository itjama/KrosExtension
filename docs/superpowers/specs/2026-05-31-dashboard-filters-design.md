# Dashboard Filters — Design Spec

**Date:** 2026-05-31  
**Scope:** Omega dashboard + Olymp dashboard (client-side only, no backend changes)  
**File changed:** `src/KrosExtension.Web/ClientApp/src/main.tsx`

---

## What We're Building

A filter bar above the company table in both `OmegaDashboardPage` and `OlympDashboardPage`. Users can filter companies by name and by warning flags. Filter state persists in localStorage separately for each dashboard.

---

## UI Layout

Filter bar sits between the page `<header>` and the `<table>`, always visible (no toggle).

```
┌─────────────────────────────────────────────────────────────┐
│ [🔍 Hľadať firmu...              ] [✕ Reset]  12 / 20       │
│ Flagy: [30d backlog ✕] [no posting] [drop] [storno] [stuck] │
└─────────────────────────────────────────────────────────────┘
```

- **Top row:** text input (flex-grow) + Reset button + count badge `filtered / total`
- **Second row:** label "Flagy:" + pill chips for each flag present in current data
- **Active pill:** colored by severity — critical flags red (`warning-pill critical`), bad flags orange (`warning-pill bad`), watch flags yellow. Inactive pills are neutral gray.
- **Reset button:** always visible. Clears both fields and removes the localStorage entry.

---

## Filter State

```typescript
type DashboardFilterState = {
  nameQuery: string;
  selectedFlags: string[];
};

const EMPTY_FILTER: DashboardFilterState = { nameQuery: '', selectedFlags: [] };
```

localStorage keys:
- `ledgerpulse.filters.omega`
- `ledgerpulse.filters.olymp`

State is read from localStorage on component mount. Invalid/missing entries fall back to `EMPTY_FILTER`. State is written to localStorage on every change via `useEffect`.

---

## Filter Logic

Applied client-side to the `companies` array already in state:

1. **Name filter** (case-insensitive substring): `company.companyName.toLowerCase().includes(nameQuery.toLowerCase())`
2. **Flag filter** (OR): `selectedFlags.length === 0 || selectedFlags.some(f => company.warningFlags.includes(f))`
3. **Combined:** both conditions must be true (AND)

Result: `filteredCompanies` derived value, recomputed on every render. The table renders `filteredCompanies`, the count badge shows `filteredCompanies.length / companies.length`.

---

## Flag Pills

Pills are generated dynamically from flags that appear in the **current loaded data** — not a static hardcoded list. This means if no company has `ORPHAN_LINES`, that pill does not appear.

```typescript
const severityOrder = (flag: string, severityFn: (f: string) => string) => {
  const s = severityFn(flag);
  return s === 'critical' ? 0 : s === 'bad' ? 1 : 2;
};
const availableFlags = [...new Set(companies.flatMap(c => c.warningFlags))]
  .sort((a, b) => severityOrder(a, severityFn) - severityOrder(b, severityFn));
```

Pills use the existing `warningLabel()` / `olympWarningLabel()` functions for display text, and `warningSeverity()` / `olympWarningSeverity()` for color class. Sorted by severity (critical first, then bad, then watch). Each pill toggles its flag in `selectedFlags` on click.

---

## Omega vs Olymp Differences

| | Omega | Olymp |
|---|---|---|
| localStorage key | `ledgerpulse.filters.omega` | `ledgerpulse.filters.olymp` |
| Max flag count | 16 flags | 8 flags |
| Label function | `warningLabel()` | `olympWarningLabel()` |
| Severity function | `warningSeverity()` | `olympWarningSeverity()` |

Both dashboards use the identical filter bar structure and logic — only the label/severity functions differ.

---

## Implementation Notes

- All changes in `main.tsx` only. No new files.
- No backend changes — purely client-side derived filtering.
- The `companies` state array is never mutated; filtering produces a new `filteredCompanies` array.
- `useEffect` writes filter state to localStorage whenever it changes.
- If localStorage contains invalid JSON, catch and fall back to `EMPTY_FILTER`.
- The filter bar is a small inline JSX block — no new component needed given the existing single-file pattern.

---

## Out of Scope

- Sorting by columns (not requested)
- Server-side filtering / API query params (all data already loaded)
- Saving filters to backend / user profiles
- Filter by server / accountingYear
