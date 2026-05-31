# Dashboard Filters Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a filter bar (name search + warning flag pills) above the company table in OmegaDashboardPage and OlympDashboardPage, persisted separately in localStorage.

**Architecture:** Pure client-side — filter state lives in React `useState`, initialized from `localStorage` on mount, written back on every change. Filtering is a derived `filteredCompanies` array computed at render time from the existing `companies` state. No backend changes.

**Tech Stack:** React 19, TypeScript, single `main.tsx` file, `styles.css` for CSS additions.

---

## File Map

| File | Change |
|---|---|
| `src/KrosExtension.Web/ClientApp/src/main.tsx` | Add type, constants, helpers, filter state + JSX in two dashboard components |
| `src/KrosExtension.Web/ClientApp/src/styles.css` | Add filter bar CSS classes |

---

### Task 1: Foundation — type, constants, localStorage helpers, CSS

**Files:**
- Modify: `src/KrosExtension.Web/ClientApp/src/main.tsx` (lines ~7 and ~1581)
- Modify: `src/KrosExtension.Web/ClientApp/src/styles.css` (append at end)

---

- [ ] **Step 1: Add `DashboardFilterState` type to `main.tsx`**

  Open `main.tsx`. After line 8 (`type AuthenticationMode = ...`), insert:

  ```typescript
  type DashboardFilterState = {
    nameQuery: string;
    selectedFlags: string[];
  };
  ```

- [ ] **Step 2: Add localStorage key constants to `main.tsx`**

  After the `productLabels` const (around line 221–224), insert:

  ```typescript
  const FILTER_KEY_OMEGA = 'ledgerpulse.filters.omega';
  const FILTER_KEY_OLYMP = 'ledgerpulse.filters.olymp';
  const EMPTY_FILTER: DashboardFilterState = { nameQuery: '', selectedFlags: [] };
  ```

- [ ] **Step 3: Add `readFilters` / `writeFilters` helpers to `main.tsx`**

  After the `formatPeriodId` function (near the end of the file, just before `createRoot(...)`), insert:

  ```typescript
  function readFilters(key: string): DashboardFilterState {
    try {
      const raw = localStorage.getItem(key);
      if (!raw) return EMPTY_FILTER;
      const parsed = JSON.parse(raw) as DashboardFilterState;
      if (typeof parsed.nameQuery !== 'string' || !Array.isArray(parsed.selectedFlags)) {
        return EMPTY_FILTER;
      }
      return parsed;
    } catch {
      return EMPTY_FILTER;
    }
  }

  function writeFilters(key: string, state: DashboardFilterState): void {
    if (!state.nameQuery && state.selectedFlags.length === 0) {
      localStorage.removeItem(key);
    } else {
      localStorage.setItem(key, JSON.stringify(state));
    }
  }
  ```

- [ ] **Step 4: Add filter bar CSS to `styles.css`**

  Append at the end of `styles.css`:

  ```css
  /* ---- Filter bar ---- */
  .filter-bar {
    background: #ffffff;
    border: 1px solid #e2e8f0;
    border-radius: 8px;
    display: flex;
    flex-direction: column;
    gap: 8px;
    padding: 10px 14px;
  }

  .filter-bar-row {
    align-items: center;
    display: flex;
    gap: 10px;
  }

  .filter-search {
    flex: 1;
    font-size: 13px;
    min-height: 34px;
  }

  .filter-reset {
    font-size: 12px;
    min-height: 30px;
    padding: 0 10px;
    white-space: nowrap;
  }

  .filter-count {
    color: #64748b;
    font-size: 12px;
    white-space: nowrap;
  }

  .filter-flags {
    align-items: center;
    display: flex;
    flex-wrap: wrap;
    gap: 6px;
  }

  .filter-flags-label {
    color: #64748b;
    font-size: 12px;
  }

  .filter-flag-pill {
    background: #f1f5f9;
    border-color: #cbd5e1;
    border-radius: 999px;
    color: #475569;
    font-size: 11px;
    font-weight: 600;
    min-height: 24px;
    padding: 0 10px;
    white-space: nowrap;
  }

  .filter-flag-pill:hover {
    border-color: #94a3b8;
  }

  .filter-flag-pill.active.critical {
    background: #fee2e2;
    border-color: #ef4444;
    color: #991b1b;
  }

  .filter-flag-pill.active.bad {
    background: #ffedd5;
    border-color: #f97316;
    color: #9a3412;
  }

  .filter-flag-pill.active.watch {
    background: #fef9c3;
    border-color: #ca8a04;
    color: #854d0e;
  }
  ```

- [ ] **Step 5: Verify TypeScript compiles**

  ```
  cd src/KrosExtension.Web/ClientApp
  npm run build
  ```

  Expected: build completes with 0 errors. (New helpers are not yet used — that's fine at this stage.)

- [ ] **Step 6: Commit**

  ```bash
  git add src/KrosExtension.Web/ClientApp/src/main.tsx
  git add src/KrosExtension.Web/ClientApp/src/styles.css
  git commit -m "feat: add filter foundation — DashboardFilterState, helpers, CSS"
  ```

---

### Task 2: Omega filter bar

**Files:**
- Modify: `src/KrosExtension.Web/ClientApp/src/main.tsx` — inside `OmegaDashboardPage` function (lines ~780–938)

---

- [ ] **Step 1: Add filter state to `OmegaDashboardPage`**

  Inside `OmegaDashboardPage`, after the existing `useState` declarations (`companies`, `isLoading`, `isUpdating`, `error`), add:

  ```typescript
  const [filters, setFilters] = useState<DashboardFilterState>(() => readFilters(FILTER_KEY_OMEGA));

  useEffect(() => {
    writeFilters(FILTER_KEY_OMEGA, filters);
  }, [filters]);
  ```

  The `() => readFilters(...)` form is a lazy initializer — it runs only once on mount.

- [ ] **Step 2: Add derived values to `OmegaDashboardPage`**

  In the function body, just before the `return` statement, add:

  ```typescript
  const availableFlags = [...new Set(companies.flatMap(c => c.warningFlags))]
    .sort((a, b) => {
      const order = (f: string) => { const s = warningSeverity(f); return s === 'critical' ? 0 : s === 'bad' ? 1 : 2; };
      return order(a) - order(b);
    });

  const filteredCompanies = companies.filter(c =>
    c.companyName.toLowerCase().includes(filters.nameQuery.toLowerCase()) &&
    (filters.selectedFlags.length === 0 || filters.selectedFlags.some(f => c.warningFlags.includes(f)))
  );
  ```

- [ ] **Step 3: Add filter bar JSX to `OmegaDashboardPage`**

  In the `return` JSX, insert the filter bar **between** the `snapshot-age` div and the `<table>`:

  ```tsx
  <div className="filter-bar">
    <div className="filter-bar-row">
      <input
        className="filter-search"
        placeholder="🔍 Hľadať firmu..."
        value={filters.nameQuery}
        onChange={(e) => setFilters(f => ({ ...f, nameQuery: e.target.value }))}
      />
      <button className="filter-reset" onClick={() => setFilters(EMPTY_FILTER)}>
        ✕ Reset
      </button>
      <span className="filter-count">{filteredCompanies.length} / {companies.length}</span>
    </div>
    {availableFlags.length > 0 && (
      <div className="filter-flags">
        <span className="filter-flags-label">Flagy:</span>
        {availableFlags.map(flag => {
          const active = filters.selectedFlags.includes(flag);
          const sev = warningSeverity(flag);
          return (
            <button
              key={flag}
              className={`filter-flag-pill ${sev}${active ? ' active' : ''}`}
              onClick={() => setFilters(f => ({
                ...f,
                selectedFlags: active
                  ? f.selectedFlags.filter(s => s !== flag)
                  : [...f.selectedFlags, flag]
              }))}
            >
              {warningLabel(flag)}{active ? ' ✕' : ''}
            </button>
          );
        })}
      </div>
    )}
  </div>
  ```

- [ ] **Step 4: Replace `companies.map` with `filteredCompanies.map` in the table body**

  In the `<tbody>` of the Omega table, replace the two existing rows:

  ```tsx
  {!isLoading && companies.length === 0 && (
    <tr>
      <td colSpan={10}>Omega dashboard zatiaľ nemá snapshoty. Spusti UpdateData.</td>
    </tr>
  )}
  {companies.map((company) => (
  ```

  With:

  ```tsx
  {!isLoading && companies.length === 0 && (
    <tr>
      <td colSpan={10}>Omega dashboard zatiaľ nemá snapshoty. Spusti UpdateData.</td>
    </tr>
  )}
  {!isLoading && companies.length > 0 && filteredCompanies.length === 0 && (
    <tr>
      <td colSpan={10}>Žiadna firma nezodpovedá filtru.</td>
    </tr>
  )}
  {filteredCompanies.map((company) => (
  ```

- [ ] **Step 5: Verify TypeScript compiles**

  ```
  cd src/KrosExtension.Web/ClientApp
  npm run build
  ```

  Expected: 0 errors.

- [ ] **Step 6: Manual smoke test in browser**

  Start the dev server (`npm run dev` in ClientApp, and the API server). Open `http://localhost:5174`.

  Verify:
  - Filter bar appears above the Omega table with a search input, Reset button, and count badge.
  - Typing in the search field filters the company list in real time.
  - Clicking a flag pill highlights it and filters the table.
  - Clicking a second pill adds it (OR logic — companies with either flag show).
  - Clicking Reset clears both.
  - Reload the page — filter state is restored from localStorage.

- [ ] **Step 7: Commit**

  ```bash
  git add src/KrosExtension.Web/ClientApp/src/main.tsx
  git commit -m "feat: add filter bar to Omega dashboard"
  ```

---

### Task 3: Olymp filter bar

**Files:**
- Modify: `src/KrosExtension.Web/ClientApp/src/main.tsx` — inside `OlympDashboardPage` function (lines ~940–1086)

---

- [ ] **Step 1: Add filter state to `OlympDashboardPage`**

  Inside `OlympDashboardPage`, after the existing `useState` declarations, add:

  ```typescript
  const [filters, setFilters] = useState<DashboardFilterState>(() => readFilters(FILTER_KEY_OLYMP));

  useEffect(() => {
    writeFilters(FILTER_KEY_OLYMP, filters);
  }, [filters]);
  ```

- [ ] **Step 2: Add derived values to `OlympDashboardPage`**

  Just before the `return` statement, add:

  ```typescript
  const availableFlags = [...new Set(companies.flatMap(c => c.warningFlags))]
    .sort((a, b) => {
      const order = (f: string) => { const s = olympWarningSeverity(f); return s === 'critical' ? 0 : s === 'bad' ? 1 : 2; };
      return order(a) - order(b);
    });

  const filteredCompanies = companies.filter(c =>
    c.companyName.toLowerCase().includes(filters.nameQuery.toLowerCase()) &&
    (filters.selectedFlags.length === 0 || filters.selectedFlags.some(f => c.warningFlags.includes(f)))
  );
  ```

- [ ] **Step 3: Add filter bar JSX to `OlympDashboardPage`**

  Insert the filter bar between the `snapshot-age` div and the `<table>` in `OlympDashboardPage`:

  ```tsx
  <div className="filter-bar">
    <div className="filter-bar-row">
      <input
        className="filter-search"
        placeholder="🔍 Hľadať firmu..."
        value={filters.nameQuery}
        onChange={(e) => setFilters(f => ({ ...f, nameQuery: e.target.value }))}
      />
      <button className="filter-reset" onClick={() => setFilters(EMPTY_FILTER)}>
        ✕ Reset
      </button>
      <span className="filter-count">{filteredCompanies.length} / {companies.length}</span>
    </div>
    {availableFlags.length > 0 && (
      <div className="filter-flags">
        <span className="filter-flags-label">Flagy:</span>
        {availableFlags.map(flag => {
          const active = filters.selectedFlags.includes(flag);
          const sev = olympWarningSeverity(flag);
          return (
            <button
              key={flag}
              className={`filter-flag-pill ${sev}${active ? ' active' : ''}`}
              onClick={() => setFilters(f => ({
                ...f,
                selectedFlags: active
                  ? f.selectedFlags.filter(s => s !== flag)
                  : [...f.selectedFlags, flag]
              }))}
            >
              {olympWarningLabel(flag)}{active ? ' ✕' : ''}
            </button>
          );
        })}
      </div>
    )}
  </div>
  ```

- [ ] **Step 4: Replace `companies.map` with `filteredCompanies.map` in the Olymp table body**

  In the `<tbody>` of the Olymp table, replace:

  ```tsx
  {!isLoading && companies.length === 0 && (
    <tr>
      <td colSpan={8}>Olymp dashboard zatiaľ nemá snapshoty. Spusti UpdateData.</td>
    </tr>
  )}
  {companies.map((company) => (
  ```

  With:

  ```tsx
  {!isLoading && companies.length === 0 && (
    <tr>
      <td colSpan={8}>Olymp dashboard zatiaľ nemá snapshoty. Spusti UpdateData.</td>
    </tr>
  )}
  {!isLoading && companies.length > 0 && filteredCompanies.length === 0 && (
    <tr>
      <td colSpan={8}>Žiadna firma nezodpovedá filtru.</td>
    </tr>
  )}
  {filteredCompanies.map((company) => (
  ```

- [ ] **Step 5: Verify TypeScript compiles**

  ```
  cd src/KrosExtension.Web/ClientApp
  npm run build
  ```

  Expected: 0 errors.

- [ ] **Step 6: Manual smoke test in browser**

  Navigate to the Olymp dashboard. Verify:
  - Filter bar appears with Olymp-specific flag pills (e.g., `mesiac chýba`, `oneskorené mzdy`).
  - Search and flag filtering work independently from Omega (separate localStorage keys).
  - Reloading the page restores the Olymp filter state without affecting the Omega filter state.

- [ ] **Step 7: Commit**

  ```bash
  git add src/KrosExtension.Web/ClientApp/src/main.tsx
  git commit -m "feat: add filter bar to Olymp dashboard"
  ```

---

## Self-Review Notes

- **Spec coverage:** All requirements covered — name filter ✓, flag pills ✓, OR logic ✓, reset ✓, localStorage persistence ✓, Omega/Olymp separated ✓, pills sorted by severity ✓, dynamic pills from current data ✓.
- **Type consistency:** `DashboardFilterState`, `FILTER_KEY_OMEGA`, `FILTER_KEY_OLYMP`, `EMPTY_FILTER` defined in Task 1, used in Tasks 2 and 3 — consistent throughout.
- **No backend changes:** Confirmed — all filtering is client-side.
