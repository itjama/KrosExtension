import React, { useEffect, useState } from 'react';
import { createRoot } from 'react-dom/client';
import { createPortal } from 'react-dom';
import { BarChart3, ChevronDown, Database, LogOut, RefreshCw, Save, Server, Settings, Trash2, Users } from 'lucide-react';
import { apiFetch } from './api';
import { AuthGate, ChangePasswordForm, type CurrentUser } from './auth';
import './styles.css';

type Product = 'olymp' | 'omega';
type AuthenticationMode = 'Windows' | 'SqlLogin';
type Section = 'stats-olymp' | 'stats-omega' | 'settings-servers' | 'settings-olymp' | 'settings-omega' | 'admin-users';

type DashboardFilterState = {
  nameQuery: string;
  selectedFlags: string[];
};

type OmegaOrphanPayment = {
  paymentId: number;
  orphanedDocumentId: number;
  documentType: string;
  internalNumber: string;
  paymentDate: string | null;
  amount: number;
  currency: string;
  paymentKind: string;
  note: string | null;
};

type OmegaOrphanPaymentsResult = {
  totalCount: number;
  items: OmegaOrphanPayment[];
};

type DataReadServer = {
  id: string;
  displayName: string;
  serverName: string;
  olympAuthenticationMode: AuthenticationMode | number;
  hasOlympCredentials: boolean;
  omegaAuthenticationMode: AuthenticationMode | number;
  hasOmegaCredentials: boolean;
  trustServerCertificate: boolean;
  isActive: boolean;
};

type DiscoveredCompany = {
  productType: string | number;
  dataReadServerId: string;
  serverDisplayName: string;
  databaseName: string;
  companyName: string;
  isTracked: boolean;
};

type CompanyStatistics = {
  productType: string | number;
  dataReadServerId: string;
  serverDisplayName: string;
  databaseName: string;
  companyName: string;
  accountingYear: number | null;
  last7DaysActivity: number;
  last30DaysActivity: number;
  dailyAverage30Days: number;
};

type OmegaDashboardCompany = {
  dataReadServerId: string;
  serverDisplayName: string;
  databaseName: string;
  companyName: string;
  accountingYear: number | null;
  snapshotAt: string;
  healthScore: number;
  activityScore: number;
  completionScore: number;
  backlogAgingScore: number;
  monthClosingScore: number;
  dataQualityScore: number;
  timelinessScore: number;
  activity7Days: number;
  activity30Days: number;
  previous7DaysActivity: number;
  trendPercent: number | null;
  registered30Days: number;
  posted30Days: number;
  postingRate30Days: number;
  backlogCount: number;
  backlog7DaysPlus: number;
  backlog30DaysPlus: number;
  oldestOpenAgeDays: number | null;
  lastActivityDate: string | null;
  previousMonthPostingRate: number | null;
  historicalPostingRate: number | null;
  imported30Days: number;
  platform30Days: number;
  manual30Days: number;
  stornoRate30Days: number | null;
  preAccountedStuck: number;
  dataQualityIssues: number;
  warningFlags: string[];
  postingLagDfAvgDays: number | null;
  postingLagDfMaxDays: number | null;
  postingLagDfCount: number;
  postingLagOfAvgDays: number | null;
  postingLagOfMaxDays: number | null;
  postingLagOfCount: number;
  postingLagPdAvgDays: number | null;
  postingLagPdMaxDays: number | null;
  postingLagPdCount: number;
  postingLagBvAvgDays: number | null;
  postingLagBvMaxDays: number | null;
  postingLagBvCount: number;
  lateEntryDfAvgDays: number | null;
  lateEntryDfMaxDays: number | null;
  lateEntryDfCount: number;
  lateEntryOfAvgDays: number | null;
  lateEntryOfMaxDays: number | null;
  lateEntryOfCount: number;
  lateEntryPdAvgDays: number | null;
  lateEntryPdMaxDays: number | null;
  lateEntryPdCount: number;
  isStale: boolean;
};

type OmegaMonthlyBreakdown = {
  year: number;
  month: number;
  registered: number;
  posted: number;
  postingRate: number | null;
};

type OmegaCompanyDetail = {
  dataReadServerId: string;
  serverDisplayName: string;
  databaseName: string;
  companyName: string;
  accountingYear: number | null;
  snapshotAt: string;
  healthScore: number;
  activityScore: number;
  completionScore: number;
  backlogAgingScore: number;
  monthClosingScore: number;
  dataQualityScore: number;
  timelinessScore: number;
  registered30Days: number;
  preAccounted30Days: number;
  posted30Days: number;
  platform30Days: number;
  imported30Days: number;
  manual30Days: number;
  backlogTotal: number;
  backlog0to3Days: number;
  backlog4to7Days: number;
  backlog8to30Days: number;
  backlog30DaysPlus: number;
  monthlyBreakdown: OmegaMonthlyBreakdown[];
  warningFlags: string[];
  postingLagDfAvgDays: number | null;
  postingLagDfMaxDays: number | null;
  postingLagDfCount: number;
  postingLagDfOver30Count: number;
  postingLagOfAvgDays: number | null;
  postingLagOfMaxDays: number | null;
  postingLagOfCount: number;
  postingLagOfOver30Count: number;
  postingLagPdAvgDays: number | null;
  postingLagPdMaxDays: number | null;
  postingLagPdCount: number;
  postingLagPdOver30Count: number;
  postingLagBvAvgDays: number | null;
  postingLagBvMaxDays: number | null;
  postingLagBvCount: number;
  postingLagBvOver30Count: number;
  lateEntryDfAvgDays: number | null;
  lateEntryDfMaxDays: number | null;
  lateEntryDfCount: number;
  lateEntryOfAvgDays: number | null;
  lateEntryOfMaxDays: number | null;
  lateEntryOfCount: number;
  lateEntryPdAvgDays: number | null;
  lateEntryPdMaxDays: number | null;
  lateEntryPdCount: number;
};

type OlympDashboardCompany = {
  dataReadServerId: string;
  serverDisplayName: string;
  databaseName: string;
  companyName: string;
  accountingYear: number | null;
  snapshotAt: string;
  healthScore: number;
  activityScore: number;
  payrollCurrentnessScore: number;
  payrollConsistencyScore: number;
  periodRegularityScore: number;
  lastActivityDate: string | null;
  lastProcessedPeriod: string | null;
  currentMonthProcessed: boolean;
  processedPeriodsInYear: number;
  activity7Days: number;
  activity30Days: number;
  trendPercent: number | null;
  warningFlags: string[];
  isStale: boolean;
};

type OlympMonthlyPeriod = {
  periodId: string;
  hasMzda: boolean;
  hasBase: boolean;
  hasOdvod: boolean;
};

type OlympCompanyDetail = {
  dataReadServerId: string;
  serverDisplayName: string;
  databaseName: string;
  companyName: string;
  accountingYear: number | null;
  snapshotAt: string;
  healthScore: number;
  activityScore: number;
  payrollCurrentnessScore: number;
  payrollConsistencyScore: number;
  periodRegularityScore: number;
  lastActivityDate: string | null;
  lastProcessedPeriod: string | null;
  currentMonthProcessed: boolean;
  processedPeriodsInYear: number;
  activity7Days: number;
  activity30Days: number;
  nullStartTimeCount: number;
  missingPeriodsCount: number;
  inconsistentPeriodsCount: number;
  periods: OlympMonthlyPeriod[];
  warningFlags: string[];
};

type RefreshSettings = {
  productType: string | number;
  refreshTime: string;
};

const productLabels: Record<Product, string> = {
  olymp: 'Olymp',
  omega: 'Omega'
};

const FILTER_KEY_OMEGA = 'ledgerpulse.filters.omega';
const FILTER_KEY_OLYMP = 'ledgerpulse.filters.olymp';
const EMPTY_FILTER: DashboardFilterState = { nameQuery: '', selectedFlags: [] };

function Tooltip({ content, children }: { content: string; children: React.ReactNode }) {
  const [pos, setPos] = useState<{ x: number; y: number } | null>(null);
  if (!content) return <>{children}</>;
  return (
    <span
      className="tooltip-host"
      onMouseMove={(e) => setPos({ x: e.clientX, y: e.clientY })}
      onMouseLeave={() => setPos(null)}
    >
      {children}
      {pos && createPortal(
        <div className="tooltip-popup" style={{ left: pos.x + 14, top: pos.y - 44 }}>
          {content}
        </div>,
        document.body
      )}
    </span>
  );
}

function HelpModal({ title, children }: { title: string; children: React.ReactNode }) {
  const [open, setOpen] = useState(false);
  return (
    <>
      <button className="help-btn" onClick={() => setOpen(true)} title="Čo zobrazuje tento pohľad?">?</button>
      {open && (
        <div className="help-overlay" onClick={() => setOpen(false)}>
          <div className="help-modal" onClick={(e) => e.stopPropagation()}>
            <div className="help-modal-header">
              <span>{title}</span>
              <button className="help-close" onClick={() => setOpen(false)}>✕</button>
            </div>
            <div className="help-modal-body">{children}</div>
          </div>
        </div>
      )}
    </>
  );
}

function App({ user, onLogout }: { user: CurrentUser; onLogout: () => Promise<void> }) {
  const [section, setSection] = useState<Section>('stats-omega');
  const [settingsOpen, setSettingsOpen] = useState(false);
  const [adminOpen, setAdminOpen] = useState(false);
  const [changePasswordOpen, setChangePasswordOpen] = useState(false);
  const isAdmin = user.role === 'Admin';
  const [omegaDetail, setOmegaDetail] = useState<{ dataReadServerId: string; databaseName: string } | null>(null);
  const [olympDetail, setOlympDetail] = useState<{ dataReadServerId: string; databaseName: string } | null>(null);

  return (
    <main className="app-shell">
      <aside className="sidebar">
        <div className="brand">LedgerPulse</div>
        <button className={section === 'stats-olymp' ? 'active' : ''} onClick={() => { setSection('stats-olymp'); setOlympDetail(null); }}>
          <BarChart3 size={18} /> Olymp
        </button>
        <button className={section === 'stats-omega' ? 'active' : ''} onClick={() => { setSection('stats-omega'); setOmegaDetail(null); }}>
          <BarChart3 size={18} /> Omega
        </button>
        {isAdmin && (<>
        <button className="submenu-toggle" onClick={() => setSettingsOpen((current) => !current)}>
          <Settings size={18} /> Settings <ChevronDown className={settingsOpen ? 'chevron open' : 'chevron'} size={16} />
        </button>
        {settingsOpen && (
          <div className="submenu">
            <button className={section === 'settings-servers' ? 'active' : ''} onClick={() => setSection('settings-servers')}>
              <Server size={16} /> Servery
            </button>
            <button className={section === 'settings-olymp' ? 'active' : ''} onClick={() => setSection('settings-olymp')}>
              <Database size={16} /> Olymp
            </button>
            <button className={section === 'settings-omega' ? 'active' : ''} onClick={() => setSection('settings-omega')}>
              <Database size={16} /> Omega
            </button>
          </div>
        )}
        <button className="submenu-toggle" onClick={() => setAdminOpen((current) => !current)}>
          <Users size={18} /> Admin <ChevronDown className={adminOpen ? 'chevron open' : 'chevron'} size={16} />
        </button>
        {adminOpen && (
          <div className="submenu">
            <button className={section === 'admin-users' ? 'active' : ''} onClick={() => setSection('admin-users')}>
              <Users size={16} /> Používatelia
            </button>
          </div>
        )}
        </>)}
        <div className="sidebar-user">
          <div className="sidebar-user-name">{user.userName}</div>
          <button onClick={() => setChangePasswordOpen(true)}>Zmeniť heslo</button>
          <button onClick={() => void onLogout()}><LogOut size={16} /> Odhlásiť</button>
        </div>
        <div className="sidebar-copyright">© Iteo, s.r.o. 2026</div>
      </aside>
      {changePasswordOpen && (
        <div className="help-overlay">
          <div className="help-modal" onClick={(e) => e.stopPropagation()}>
            <div className="help-modal-header">
              <span>Zmena hesla</span>
              <button className="help-close" onClick={() => setChangePasswordOpen(false)}>✕</button>
            </div>
            <div className="help-modal-body">
              <ChangePasswordForm onDone={() => setChangePasswordOpen(false)} onCancel={() => setChangePasswordOpen(false)} />
            </div>
          </div>
        </div>
      )}
      <section className="workspace">
        {isAdmin && section === 'admin-users' && <p>Správa používateľov bude doplnená.</p>}
        {isAdmin && section === 'settings-servers' && <ServersPage />}
        {isAdmin && section === 'settings-olymp' && <CompaniesPage product="olymp" />}
        {isAdmin && section === 'settings-omega' && <CompaniesPage product="omega" />}
        {section === 'stats-olymp' && (olympDetail
          ? <OlympCompanyDetailPage
              dataReadServerId={olympDetail.dataReadServerId}
              databaseName={olympDetail.databaseName}
              onBack={() => setOlympDetail(null)}
            />
          : <OlympDashboardPage
              onOpenDetail={(serverId, db) => setOlympDetail({ dataReadServerId: serverId, databaseName: db })}
            />
        )}
        {section === 'stats-omega' && (omegaDetail
          ? <OmegaCompanyDetailPage
              dataReadServerId={omegaDetail.dataReadServerId}
              databaseName={omegaDetail.databaseName}
              onBack={() => setOmegaDetail(null)}
            />
          : <OmegaDashboardPage
              onOpenDetail={(serverId, db) => setOmegaDetail({ dataReadServerId: serverId, databaseName: db })}
            />
        )}
      </section>
    </main>
  );
}

function ServersPage() {
  const [servers, setServers] = useState<DataReadServer[]>([]);
  const [error, setError] = useState('');
  const [serverName, setServerName] = useState('(localdb)\\MSSQLLocalDB');
  const [displayName, setDisplayName] = useState('LocalDB');
  const [olympAuthenticationMode, setOlympAuthenticationMode] = useState<AuthenticationMode>('Windows');
  const [olympUserName, setOlympUserName] = useState('');
  const [olympPassword, setOlympPassword] = useState('');
  const [omegaAuthenticationMode, setOmegaAuthenticationMode] = useState<AuthenticationMode>('Windows');
  const [omegaUserName, setOmegaUserName] = useState('');
  const [omegaPassword, setOmegaPassword] = useState('');
  const [trustServerCertificate, setTrustServerCertificate] = useState(true);

  async function load() {
    setError('');
    const response = await apiFetch('/api/data-read-servers');

    if (!response.ok) {
      setError(await response.text());
      return;
    }

    setServers(await response.json());
  }

  async function addServer() {
    setError('');
    const response = await apiFetch('/api/data-read-servers', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        serverName,
        displayName,
        olymp: {
          authenticationMode: olympAuthenticationMode,
          userName: olympAuthenticationMode === 'SqlLogin' ? olympUserName : null,
          password: olympAuthenticationMode === 'SqlLogin' ? olympPassword : null
        },
        omega: {
          authenticationMode: omegaAuthenticationMode,
          userName: omegaAuthenticationMode === 'SqlLogin' ? omegaUserName : null,
          password: omegaAuthenticationMode === 'SqlLogin' ? omegaPassword : null
        },
        trustServerCertificate
      })
    });

    if (!response.ok) {
      setError(await response.text());
      return;
    }

    await load();
  }

  async function removeServer(id: string) {
    await apiFetch(`/api/data-read-servers/${id}`, { method: 'DELETE' });
    await load();
  }

  useEffect(() => {
    void load();
  }, []);

  return (
    <div className="page">
      <header>
        <h1>Servery</h1>
        <button onClick={load}><RefreshCw size={16} /> Obnoviť</button>
      </header>

      <div className="server-form">
        <div className="form-row">
          <label>
            Názov
            <input value={displayName} onChange={(event) => setDisplayName(event.target.value)} />
          </label>
          <label>
            SQL Server
            <input value={serverName} onChange={(event) => setServerName(event.target.value)} />
          </label>
        </div>

        <div className="credential-grid">
          <CredentialsSection
            title="Olymp"
            mode={olympAuthenticationMode}
            userName={olympUserName}
            password={olympPassword}
            onModeChange={setOlympAuthenticationMode}
            onUserNameChange={setOlympUserName}
            onPasswordChange={setOlympPassword}
          />
          <CredentialsSection
            title="Omega"
            mode={omegaAuthenticationMode}
            userName={omegaUserName}
            password={omegaPassword}
            onModeChange={setOmegaAuthenticationMode}
            onUserNameChange={setOmegaUserName}
            onPasswordChange={setOmegaPassword}
          />
        </div>

        <div className="form-actions">
          <label className="checkbox-label">
            <input
              type="checkbox"
              checked={trustServerCertificate}
              onChange={(event) => setTrustServerCertificate(event.target.checked)}
            />
            Dôverovať certifikátu servera
          </label>
          <button onClick={addServer}><Save size={16} /> Pridať</button>
        </div>
      </div>

      {error && <div className="status error">{error}</div>}

      <table>
        <thead>
          <tr>
            <th>Názov</th>
            <th>Server</th>
            <th>Olymp</th>
            <th>Omega</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          {servers.map((server) => (
            <tr key={server.id}>
              <td>{server.displayName}</td>
              <td>{server.serverName}</td>
              <td>{formatAccess(server.olympAuthenticationMode, server.hasOlympCredentials)}</td>
              <td>{formatAccess(server.omegaAuthenticationMode, server.hasOmegaCredentials)}</td>
              <td>
                <button className="icon-button" onClick={() => removeServer(server.id)} title="Zmazať">
                  <Trash2 size={16} />
                </button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

function CredentialsSection({
  title,
  mode,
  userName,
  password,
  onModeChange,
  onUserNameChange,
  onPasswordChange
}: {
  title: string;
  mode: AuthenticationMode;
  userName: string;
  password: string;
  onModeChange: (mode: AuthenticationMode) => void;
  onUserNameChange: (value: string) => void;
  onPasswordChange: (value: string) => void;
}) {
  return (
    <fieldset>
      <legend>{title}</legend>
      <label>
        Prihlásenie
        <select value={mode} onChange={(event) => onModeChange(event.target.value as AuthenticationMode)}>
          <option value="Windows">Windows</option>
          <option value="SqlLogin">SQL login</option>
        </select>
      </label>
      {mode === 'SqlLogin' && (
        <div className="credential-fields">
          <label>
            Meno
            <input value={userName} onChange={(event) => onUserNameChange(event.target.value)} />
          </label>
          <label>
            Heslo
            <input type="password" value={password} onChange={(event) => onPasswordChange(event.target.value)} />
          </label>
        </div>
      )}
    </fieldset>
  );
}

function formatAccess(mode: AuthenticationMode | number, hasCredentials: boolean) {
  if (mode === 'Windows' || mode === 1) {
    return 'Windows';
  }

  return hasCredentials ? 'SQL login nastavený' : 'SQL login bez hesla';
}

function CompaniesPage({ product }: { product: Product }) {
  const [companies, setCompanies] = useState<DiscoveredCompany[]>([]);
  const [refreshTime, setRefreshTime] = useState('02:00');
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState('');
  const [status, setStatus] = useState('');

  async function loadRefreshSettings() {
    const response = await apiFetch(`/api/${product}/refresh-settings`);

    if (!response.ok) {
      setError(await response.text());
      return;
    }

    const settings = await response.json() as RefreshSettings;
    setRefreshTime(settings.refreshTime);
  }

  async function saveRefreshSettings() {
    setError('');
    setStatus('');

    const response = await apiFetch(`/api/${product}/refresh-settings`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ refreshTime })
    });

    if (!response.ok) {
      setError(await response.text());
      return;
    }

    setStatus('Refresh time uložený.');
  }

  async function discover() {
    setIsLoading(true);
    setError('');
    setStatus('');

    try {
      const response = await apiFetch(`/api/${product}/companies/discover`);

      if (!response.ok) {
        setError(await response.text());
        setCompanies([]);
        return;
      }

      const discoveredCompanies = await response.json() as DiscoveredCompany[];
      setCompanies(discoveredCompanies);
      setStatus(`Načítané: ${discoveredCompanies.length}`);
    } catch (exception) {
      setError(exception instanceof Error ? exception.message : 'Načítanie zlyhalo.');
      setCompanies([]);
    } finally {
      setIsLoading(false);
    }
  }

  async function saveSelection() {
    setError('');
    const response = await apiFetch(`/api/${product}/companies/tracked`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        companies: companies.map((company) => ({
          dataReadServerId: company.dataReadServerId,
          databaseName: company.databaseName,
          companyName: company.companyName,
          isTracked: company.isTracked
        }))
      })
    });

    if (!response.ok) {
      setError(await response.text());
      return;
    }

    setStatus('Výber uložený.');
  }

  useEffect(() => {
    void loadRefreshSettings();
    void discover();
  }, [product]);

  return (
    <div className="page">
      <header>
        <h1>Settings / {productLabels[product]} firmy</h1>
        <div className="actions">
          <button onClick={discover} disabled={isLoading}>
            <RefreshCw size={16} /> {isLoading ? 'Načítavam' : 'Načítať'}
          </button>
          <button onClick={saveSelection}><Save size={16} /> Uložiť výber</button>
        </div>
      </header>

      <div className="server-form compact-form">
        <div className="form-row">
          <label>
            Refresh time
            <input
              type="time"
              value={refreshTime}
              onChange={(event) => setRefreshTime(event.target.value)}
            />
          </label>
          <div className="form-actions inline-actions">
            <button onClick={saveRefreshSettings}><Save size={16} /> Uložiť čas</button>
          </div>
        </div>
      </div>

      {error && <div className="status error">{error}</div>}
      {status && !error && <div className="status">{status}</div>}

      <table className="companies-table">
        <thead>
          <tr>
            <th>
              <label className="checkbox-label">
                <input
                  type="checkbox"
                  checked={companies.length > 0 && companies.every(c => c.isTracked)}
                  ref={(el) => {
                    if (el) el.indeterminate = companies.some(c => c.isTracked) && !companies.every(c => c.isTracked);
                  }}
                  onChange={(e) =>
                    setCompanies(current => current.map(c => ({ ...c, isTracked: e.target.checked })))
                  }
                />
                Sledovať
              </label>
            </th>
            <th>Firma</th>
            <th>Databáza</th>
            <th>Server</th>
          </tr>
        </thead>
        <tbody>
          {!isLoading && companies.length === 0 && (
            <tr>
              <td colSpan={4}>Neboli nájdené žiadne databázy pre {productLabels[product]}.</td>
            </tr>
          )}
          {companies.map((company) => (
            <tr key={`${company.dataReadServerId}-${company.databaseName}`}>
              <td>
                <input
                  type="checkbox"
                  checked={company.isTracked}
                  onChange={(event) =>
                    setCompanies((current) =>
                      current.map((item) =>
                        item === company ? { ...item, isTracked: event.target.checked } : item
                      )
                    )
                  }
                />
              </td>
              <td>{company.companyName}</td>
              <td>{company.databaseName}</td>
              <td>{company.serverDisplayName}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

function StatisticsPage({ product }: { product: Product }) {
  const [statistics, setStatistics] = useState<CompanyStatistics[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState('');

  async function load() {
    setIsLoading(true);
    setError('');

    try {
      const response = await apiFetch(`/api/${product}/companies/statistics`);

      if (!response.ok) {
        setError(await response.text());
        setStatistics([]);
        return;
      }

      setStatistics(await response.json());
    } catch (exception) {
      setError(exception instanceof Error ? exception.message : 'Načítanie štatistík zlyhalo.');
      setStatistics([]);
    } finally {
      setIsLoading(false);
    }
  }

  useEffect(() => {
    void load();
  }, [product]);

  return (
    <div className="page">
      <header>
        <h1>{productLabels[product]} štatistiky</h1>
        <button onClick={load} disabled={isLoading}>
          <RefreshCw size={16} /> {isLoading ? 'Načítavam' : 'Obnoviť'}
        </button>
      </header>

      {error && <div className="status error">{error}</div>}

      <table>
        <thead>
          <tr>
            <th>Firma</th>
            <th>Databáza</th>
            <th>Rok</th>
            <th>7 dní</th>
            <th>30 dní</th>
            <th>Priemer / deň</th>
          </tr>
        </thead>
        <tbody>
          {!isLoading && statistics.length === 0 && (
            <tr>
              <td colSpan={6}>Nie sú vybrané žiadne firmy pre {productLabels[product]}.</td>
            </tr>
          )}
          {statistics.map((item) => (
            <tr key={`${item.dataReadServerId}-${item.databaseName}`}>
              <td>{item.companyName}</td>
              <td>{item.databaseName}</td>
              <td>{item.accountingYear ?? '-'}</td>
              <td>{item.last7DaysActivity}</td>
              <td>{item.last30DaysActivity}</td>
              <td>{item.dailyAverage30Days}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

function OmegaDashboardPage({ onOpenDetail }: { onOpenDetail: (dataReadServerId: string, databaseName: string) => void }) {
  const [companies, setCompanies] = useState<OmegaDashboardCompany[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [isUpdating, setIsUpdating] = useState(false);
  const [error, setError] = useState('');
  const [filters, setFilters] = useState<DashboardFilterState>(() => readFilters(FILTER_KEY_OMEGA));
  const [orphanModal, setOrphanModal] = useState<{ dataReadServerId: string; databaseName: string; companyName: string } | null>(null);

  useEffect(() => {
    writeFilters(FILTER_KEY_OMEGA, filters);
  }, [filters]);

  async function load() {
    setIsLoading(true);
    setError('');

    try {
      const response = await apiFetch('/api/omega/dashboard/companies');

      if (!response.ok) {
        setError(await response.text());
        setCompanies([]);
        return;
      }

      const newCompanies = await response.json() as OmegaDashboardCompany[];
      setCompanies(newCompanies);
      setFilters(f => ({
        ...f,
        selectedFlags: f.selectedFlags.filter(flag =>
          newCompanies.some(c => c.warningFlags.includes(flag))
        )
      }));
    } catch (exception) {
      setError(exception instanceof Error ? exception.message : 'Načítanie Omega dashboardu zlyhalo.');
      setCompanies([]);
    } finally {
      setIsLoading(false);
    }
  }

  async function updateData() {
    setIsUpdating(true);
    setError('');

    try {
      const response = await apiFetch('/api/omega/companies/update-data', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ databaseName: null })
      });

      if (!response.ok) {
        setError(await response.text());
        return;
      }

      await load();
    } catch (exception) {
      setError(exception instanceof Error ? exception.message : 'UpdateData zlyhalo.');
    } finally {
      setIsUpdating(false);
    }
  }

  useEffect(() => {
    void load();
  }, []);

  const availableFlags = [...new Set(companies.flatMap(c => c.warningFlags))]
    .sort((a, b) => {
      const order = (f: string) => { const s = warningSeverity(f); return s === 'critical' ? 0 : s === 'bad' ? 1 : 2; };
      return order(a) - order(b);
    });

  const filteredCompanies = companies.filter(c =>
    c.companyName.toLowerCase().includes(filters.nameQuery.toLowerCase()) &&
    (filters.selectedFlags.length === 0 || filters.selectedFlags.some(f => c.warningFlags.includes(f)))
  );

  return (
    <div className="page">
      <header>
        <h1>Omega dashboard</h1>
        <div className="actions">
          <HelpModal title="Omega dashboard">
            <p>Prehľad sledovaných Omega firiem na základe posledného snapshotu (UpdateData).</p>
            <p><strong>Health (0–100)</strong> — vážený priemer: Aktivita 17 %, Completion 25 %, Backlog aging 23 %, Month closing 15 %, Data quality 5 %, Timeliness 15 %.</p>
            <p><strong>7d / 30d</strong> — počet dokladov (zaevidované + zaúčtované) za posledných 7 a 30 dní.</p>
            <p><strong>% zaúčt.</strong> — podiel zaúčtovaných z registrovaných dokladov za 30 dní.</p>
            <p><strong>Storno</strong> — podiel storno dokladov z originálnych za 30 dní. Nad 10 % sa zobrazí oranžovo.</p>
            <p><strong>Backlog</strong> — celkový počet nezaúčtovaných dokladov v systéme.</p>
            <p><strong>Staré</strong> — nezaúčtované doklady staršie ako 7 dní / 30 dní.</p>
            <p><strong>Trend</strong> — zmena aktivity oproti predchádzajúcemu 7-dňovému oknu v %.</p>
            <p><strong>Warning flags</strong> — automaticky detegované problémy. Klikni na firmu pre podrobný detail.</p>
          </HelpModal>
          <button onClick={load} disabled={isLoading || isUpdating}>
            <RefreshCw size={16} /> {isLoading ? 'Načítavam' : 'Obnoviť'}
          </button>
          <button onClick={updateData} disabled={isLoading || isUpdating}>
            <RefreshCw size={16} /> {isUpdating ? 'UpdateData' : 'UpdateData'}
          </button>
        </div>
      </header>

      {error && <div className="status error">{error}</div>}

      {companies.length > 0 && (
        <div className="snapshot-age">
          Snapshot: {formatSnapshotAge(companies[0].snapshotAt)} starý
        </div>
      )}

      {companies.length > 0 && (
        <div className="filter-bar">
          <div className="filter-bar-row">
            <input
              className="filter-search"
              placeholder="🔍 Hľadať firmu..."
              value={filters.nameQuery}
              onChange={(e) => setFilters(f => ({ ...f, nameQuery: e.target.value }))}
            />
            <button className="filter-reset" onClick={() => setFilters({ ...EMPTY_FILTER })}>
              ✕ Reset
            </button>
            <span className="filter-count">{filteredCompanies.length} / {companies.length}</span>
          </div>
          {availableFlags.length > 0 && (
            <div className="filter-flags">
              <span className="filter-flags-label">Flagy:</span>
              {availableFlags.map(flag => {
                const active = filters.selectedFlags.includes(flag); // display only; onClick re-reads from updater arg for correct batching
                const sev = warningSeverity(flag);
                return (
                  <button
                    key={flag}
                    className={`filter-flag-pill ${sev}${active ? ' active' : ''}`}
                    onClick={() => setFilters(f => {
                      const isActive = f.selectedFlags.includes(flag);
                      return {
                        ...f,
                        selectedFlags: isActive
                          ? f.selectedFlags.filter(s => s !== flag)
                          : [...f.selectedFlags, flag]
                      };
                    })}
                  >
                    {warningLabel(flag)}{active ? ' ✕' : ''}
                  </button>
                );
              })}
            </div>
          )}
        </div>
      )}

      <table className="risk-table">
        <thead>
          <tr>
            <th>Firma</th>
            <th><Tooltip content="Celkové zdravie firmy (0–100). Aktivita 17 % + Completion 25 % + Backlog aging 23 % + Month closing 15 % + Data quality 5 % + Timeliness 15 %">Health</Tooltip></th>
            <th><Tooltip content="Počet dokladov (zaevidovaných + zaúčtovaných) za posledných 7 dní">7 dní</Tooltip></th>
            <th><Tooltip content="Počet dokladov za posledných 30 dní">30 dní</Tooltip></th>
            <th><Tooltip content="Podiel zaúčtovaných z celkovo registrovaných dokladov za 30 dní">% zaúčt.</Tooltip></th>
            <th><Tooltip content="Podiel storno dokladov (zDF/zOF) z originálnych dokladov za posledných 30 dní. Nad 10 % sa aktivuje varovanie HIGH_STORNO_RATE.">Storno</Tooltip></th>
            <th><Tooltip content="Celkový počet nezaúčtovaných dokladov v systéme">Backlog</Tooltip></th>
            <th><Tooltip content="Nezaúčtované doklady staršie ako 7 dní / staršie ako 30 dní">Staré</Tooltip></th>
            <th><Tooltip content="Dátum posledného dokladu zaevidovaného v systéme">Posledná aktivita</Tooltip></th>
            <th><Tooltip content="Zmena aktivity posledných 7 dní oproti predchádzajúcich 7 dní v %">Trend</Tooltip></th>
            <th><Tooltip content="Automaticky detegované problémy z dát">Warning flags</Tooltip></th>
          </tr>
        </thead>
        <tbody>
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
            <tr key={`${company.dataReadServerId}-${company.databaseName}`} className={company.isStale ? 'stale-row' : undefined}>
              <td>
                <button
                  className="company-link"
                  onClick={() => onOpenDetail(company.dataReadServerId, company.databaseName)}
                >
                  <strong>{company.companyName}</strong>
                  <span className="muted-line">{company.databaseName}</span>
                </button>
              </td>
              <td>
                <span className={`score-pill ${scoreClass(company.healthScore)}`}>{company.healthScore}</span>
              </td>
              <td>{company.activity7Days}</td>
              <td>{company.activity30Days}</td>
              <td>{company.postingRate30Days.toFixed(1)}%</td>
              <td>
                {company.stornoRate30Days !== null
                  ? <span className={company.stornoRate30Days > 10 ? 'storno-high' : undefined}>{company.stornoRate30Days.toFixed(1)}%</span>
                  : <span className="muted-label">–</span>}
              </td>
              <td>{company.backlogCount}</td>
              <td>
                <span>{company.backlog7DaysPlus} / {company.backlog30DaysPlus}</span>
                {company.oldestOpenAgeDays !== null && (
                  <span className="muted-line">max {company.oldestOpenAgeDays} dní</span>
                )}
              </td>
              <td>{formatDate(company.lastActivityDate)}</td>
              <td>{formatTrend(company.trendPercent)}</td>
              <td>
                <div className="warning-list">
                  {company.warningFlags.length === 0 && <span className="warning-pill ok">OK</span>}
                  {company.warningFlags.map((warning) => {
                    if (warning === 'ORPHAN_PAYMENTS') {
                      return (
                        <button
                          key={warning}
                          className={`warning-pill ${warningSeverity(warning)} clickable-pill`}
                          title={warningTooltip(warning)}
                          onClick={() => setOrphanModal({
                            dataReadServerId: company.dataReadServerId,
                            databaseName: company.databaseName,
                            companyName: company.companyName,
                          })}
                        >
                          {warningLabel(warning)} ↗
                        </button>
                      );
                    }
                    return (
                      <Tooltip key={warning} content={warningTooltip(warning)}>
                        <span className={`warning-pill ${warningSeverity(warning)}`}>{warningLabel(warning)}</span>
                      </Tooltip>
                    );
                  })}
                </div>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
      {orphanModal && (
        <OrphanPaymentsModal
          dataReadServerId={orphanModal.dataReadServerId}
          databaseName={orphanModal.databaseName}
          companyName={orphanModal.companyName}
          onClose={() => setOrphanModal(null)}
        />
      )}
    </div>
  );
}

function OlympDashboardPage({ onOpenDetail }: { onOpenDetail: (dataReadServerId: string, databaseName: string) => void }) {
  const [companies, setCompanies] = useState<OlympDashboardCompany[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [isUpdating, setIsUpdating] = useState(false);
  const [error, setError] = useState('');
  const [filters, setFilters] = useState<DashboardFilterState>(() => readFilters(FILTER_KEY_OLYMP));

  useEffect(() => {
    writeFilters(FILTER_KEY_OLYMP, filters);
  }, [filters]);

  async function load() {
    setIsLoading(true);
    setError('');

    try {
      const response = await apiFetch('/api/olymp/dashboard/companies');

      if (!response.ok) {
        setError(await response.text());
        setCompanies([]);
        return;
      }

      const newCompanies = await response.json() as OlympDashboardCompany[];
      setCompanies(newCompanies);
      setFilters(f => ({
        ...f,
        selectedFlags: f.selectedFlags.filter(flag =>
          newCompanies.some(c => c.warningFlags.includes(flag))
        )
      }));
    } catch (exception) {
      setError(exception instanceof Error ? exception.message : 'Načítanie Olymp dashboardu zlyhalo.');
      setCompanies([]);
    } finally {
      setIsLoading(false);
    }
  }

  async function updateData() {
    setIsUpdating(true);
    setError('');

    try {
      const response = await apiFetch('/api/olymp/companies/update-data', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ databaseName: null })
      });

      if (!response.ok) {
        setError(await response.text());
        return;
      }

      await load();
    } catch (exception) {
      setError(exception instanceof Error ? exception.message : 'UpdateData zlyhalo.');
    } finally {
      setIsUpdating(false);
    }
  }

  useEffect(() => {
    void load();
  }, []);

  const availableFlags = [...new Set(companies.flatMap(c => c.warningFlags))]
    .sort((a, b) => {
      const order = (f: string) => { const s = olympWarningSeverity(f); return s === 'critical' ? 0 : s === 'bad' ? 1 : 2; };
      return order(a) - order(b);
    });

  const filteredCompanies = companies.filter(c =>
    c.companyName.toLowerCase().includes(filters.nameQuery.toLowerCase()) &&
    (filters.selectedFlags.length === 0 || filters.selectedFlags.some(f => c.warningFlags.includes(f)))
  );

  return (
    <div className="page">
      <header>
        <h1>Olymp dashboard</h1>
        <div className="actions">
          <HelpModal title="Olymp dashboard">
            <p>Prehľad mzdového spracovania sledovaných Olymp firiem na základe posledného snapshotu (UpdateData).</p>
            <p><strong>Health (0–100)</strong> — vážený priemer: Aktivita 25 %, Aktuálnosť miezd 35 %, Konzistencia 25 %, Pravidelnosť 15 %.</p>
            <p><strong>Posledná aktivita</strong> — dátum posledného záznamu v activity logu Olympu.</p>
            <p><strong>Posledné obdobie</strong> — posledné mzdové obdobie nájdené v pracVycMzda (formát RRRR/MM).</p>
            <p><strong>Akt. mesiac</strong> — či bol aktuálny mesiac spracovaný v mzdách (✓/✗).</p>
            <p><strong>Období v roku</strong> — počet spracovaných mzdových období v aktuálnom roku.</p>
            <p><strong>Trend</strong> — zmena aktivity oproti predchádzajúcemu 7-dňovému oknu v %.</p>
            <p>Klikni na firmu pre podrobný detail mzdového spracovania.</p>
          </HelpModal>
          <button onClick={load} disabled={isLoading || isUpdating}>
            <RefreshCw size={16} /> {isLoading ? 'Načítavam' : 'Obnoviť'}
          </button>
          <button onClick={updateData} disabled={isLoading || isUpdating}>
            <RefreshCw size={16} /> {isUpdating ? 'Aktualizujem...' : 'UpdateData'}
          </button>
        </div>
      </header>

      {error && <div className="status error">{error}</div>}

      {companies.length > 0 && (
        <div className="snapshot-age">
          Snapshot: {formatSnapshotAge(companies[0].snapshotAt)} starý
        </div>
      )}

      {companies.length > 0 && (
        <div className="filter-bar">
          <div className="filter-bar-row">
            <input
              className="filter-search"
              placeholder="🔍 Hľadať firmu..."
              value={filters.nameQuery}
              onChange={(e) => setFilters(f => ({ ...f, nameQuery: e.target.value }))}
            />
            <button className="filter-reset" onClick={() => setFilters({ ...EMPTY_FILTER })}>
              ✕ Reset
            </button>
            <span className="filter-count">{filteredCompanies.length} / {companies.length}</span>
          </div>
          {availableFlags.length > 0 && (
            <div className="filter-flags">
              <span className="filter-flags-label">Flagy:</span>
              {availableFlags.map(flag => {
                const active = filters.selectedFlags.includes(flag); // display only; onClick re-reads from updater arg for correct batching
                const sev = olympWarningSeverity(flag);
                return (
                  <button
                    key={flag}
                    className={`filter-flag-pill ${sev}${active ? ' active' : ''}`}
                    onClick={() => setFilters(f => {
                      const isActive = f.selectedFlags.includes(flag);
                      return {
                        ...f,
                        selectedFlags: isActive
                          ? f.selectedFlags.filter(s => s !== flag)
                          : [...f.selectedFlags, flag]
                      };
                    })}
                  >
                    {olympWarningLabel(flag)}{active ? ' ✕' : ''}
                  </button>
                );
              })}
            </div>
          )}
        </div>
      )}

      <table className="risk-table">
        <thead>
          <tr>
            <th>Firma</th>
            <th><Tooltip content="Celkové zdravie firmy (0–100). Aktivita 25 % + Aktuálnosť miezd 35 % + Konzistencia 25 % + Pravidelnosť 15 %">Health</Tooltip></th>
            <th><Tooltip content="Dátum posledného záznamu v activity logu Olympu">Posledná aktivita</Tooltip></th>
            <th><Tooltip content="Posledné mzdové obdobie nájdené v pracVycMzda (formát RRRR/MM)">Posledné obdobie</Tooltip></th>
            <th><Tooltip content="Či bol aktuálny mesiac spracovaný v mzdách">Akt. mesiac</Tooltip></th>
            <th><Tooltip content="Počet spracovaných mzdových období v aktuálnom roku">Období v roku</Tooltip></th>
            <th><Tooltip content="Zmena aktivity posledných 7 dní oproti predchádzajúcich 7 dní v %">Trend</Tooltip></th>
            <th><Tooltip content="Automaticky detegované problémy z payroll dát">Warning flags</Tooltip></th>
          </tr>
        </thead>
        <tbody>
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
            <tr key={`${company.dataReadServerId}-${company.databaseName}`} className={company.isStale ? 'stale-row' : undefined}>
              <td>
                <button
                  className="company-link"
                  onClick={() => onOpenDetail(company.dataReadServerId, company.databaseName)}
                >
                  <strong>{company.companyName}</strong>
                  <span className="muted-line">{company.databaseName}</span>
                </button>
              </td>
              <td>
                <span className={`score-pill ${scoreClass(company.healthScore)}`}>{company.healthScore}</span>
              </td>
              <td>{formatDate(company.lastActivityDate)}</td>
              <td>{formatPeriodId(company.lastProcessedPeriod)}</td>
              <td>
                <span className={company.currentMonthProcessed ? 'score-pill good' : 'score-pill critical'}>
                  {company.currentMonthProcessed ? '✓' : '✗'}
                </span>
              </td>
              <td>{company.processedPeriodsInYear}</td>
              <td>{formatTrend(company.trendPercent)}</td>
              <td>
                <div className="warning-list">
                  {company.warningFlags.length === 0 && <span className="warning-pill ok">OK</span>}
                  {company.warningFlags.map((warning) => (
                    <Tooltip key={warning} content={olympWarningTooltip(warning)}>
                      <span className={`warning-pill ${olympWarningSeverity(warning)}`}>{olympWarningLabel(warning)}</span>
                    </Tooltip>
                  ))}
                </div>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

function OlympCompanyDetailPage({
  dataReadServerId,
  databaseName,
  onBack
}: {
  dataReadServerId: string;
  databaseName: string;
  onBack: () => void;
}) {
  const [detail, setDetail] = useState<OlympCompanyDetail | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState('');

  async function load() {
    setIsLoading(true);
    setError('');

    try {
      const response = await apiFetch(
        `/api/olymp/dashboard/companies/${dataReadServerId}/${encodeURIComponent(databaseName)}`
      );

      if (!response.ok) {
        setError(await response.text());
        return;
      }

      setDetail(await response.json());
    } catch (exception) {
      setError(exception instanceof Error ? exception.message : 'Načítanie detailu zlyhalo.');
    } finally {
      setIsLoading(false);
    }
  }

  useEffect(() => {
    void load();
  }, [dataReadServerId, databaseName]);

  return (
    <div className="page">
      <header>
        <div className="back-header">
          <button className="back-btn" onClick={onBack}>← Späť</button>
          {detail && (
            <h1>
              {detail.companyName}
              <span className="muted-label"> ({detail.databaseName})</span>
            </h1>
          )}
        </div>
        <div className="actions">
          <button onClick={load} disabled={isLoading}>
            <RefreshCw size={16} /> {isLoading ? 'Načítavam' : 'Obnoviť'}
          </button>
        </div>
      </header>

      {error && <div className="status error">{error}</div>}
      {isLoading && <div className="status">Načítavam detail...</div>}

      {detail && (
        <div className="detail-layout">

          <section className="detail-card">
            <h2>Health score</h2>
            <p className="card-desc">Celkové zdravie firmy (0–100) ako vážený priemer 4 zložiek. Snapshot z posledného UpdateData.</p>
            <div className="health-score-hero">
              <span className={`score-pill large ${scoreClass(detail.healthScore)}`}>
                {detail.healthScore}
              </span>
            </div>
            <div className="score-breakdown">
              {([
                { label: 'Aktivita', value: detail.activityScore, weight: '25 %', desc: 'Počet záznamov v activity logu za 30 dní. Nula alebo výrazný pokles = trest.' },
                { label: 'Aktuálnosť miezd', value: detail.payrollCurrentnessScore, weight: '35 %', desc: 'Či je aktuálny mesiac spracovaný a mzdy nie sú oneskorené o viac ako mesiac.' },
                { label: 'Konzistencia', value: detail.payrollConsistencyScore, weight: '25 %', desc: 'Každé mzdové obdobie musí mať záznamy vo všetkých 3 tabuľkách (Mzda, Base, Odvod).' },
                { label: 'Pravidelnosť', value: detail.periodRegularityScore, weight: '15 %', desc: 'Počet spracovaných mzdových období v roku — chýbajúce alebo preskočené mesiace = trest.' }
              ] as const).map(({ label, value, weight, desc }) => (
                <div key={label} className="score-row">
                  <Tooltip content={desc}>
                    <span className="score-label">{label} <span className="muted-label">{weight}</span></span>
                  </Tooltip>
                  <div className="score-bar-track">
                    <div className={`score-bar-fill ${scoreClass(value)}`} style={{ width: `${value}%` }} />
                  </div>
                  <span className="score-value">{value}</span>
                </div>
              ))}
            </div>
          </section>

          <section className="detail-card">
            <h2>Payroll status</h2>
            <p className="card-desc">Kľúčové indikátory mzdového spracovania načítané zo živej databázy.</p>
            <div className="type-breakdown">
              {([
                { label: 'Posledná aktivita', value: formatDate(detail.lastActivityDate) },
                { label: 'Posledné obdobie', value: formatPeriodId(detail.lastProcessedPeriod) },
                { label: 'Akt. mesiac spracovaný', value: detail.currentMonthProcessed ? 'Áno' : 'Nie' },
                { label: 'Období v roku', value: String(detail.processedPeriodsInYear) },
                { label: 'Chýbajúce obdobia', value: String(detail.missingPeriodsCount) },
                { label: 'Nekonzistentné obdobia', value: String(detail.inconsistentPeriodsCount) },
                { label: 'Null timestamps', value: String(detail.nullStartTimeCount) }
              ] as const).map(({ label, value }) => (
                <div key={label} className="type-row">
                  <span>{label}</span>
                  <strong>{value}</strong>
                </div>
              ))}
            </div>
          </section>

          {detail.warningFlags.length > 0 && (
            <section className="detail-card">
              <h2>Varovania</h2>
              <p className="card-desc">Automaticky detegované anomálie v mzdových dátach. Klikni na každý flag pre popis problému.</p>
              <div className="warning-list large">
                {detail.warningFlags.map((flag) => (
                  <Tooltip key={flag} content={olympWarningTooltip(flag)}>
                    <span className={`warning-pill ${olympWarningSeverity(flag)}`}>{olympWarningLabel(flag)}</span>
                  </Tooltip>
                ))}
              </div>
            </section>
          )}

          <section className="detail-card">
            <h2>Aktivita</h2>
            <p className="card-desc">Počet záznamov v activity logu Olympu zo živej databázy.</p>
            <div className="type-breakdown">
              {([
                { label: 'Posledných 7 dní', value: String(detail.activity7Days) },
                { label: 'Posledných 30 dní', value: String(detail.activity30Days) }
              ] as const).map(({ label, value }) => (
                <div key={label} className="type-row">
                  <span>{label}</span>
                  <strong>{value}</strong>
                </div>
              ))}
            </div>
          </section>

          <section className="detail-card full-width">
            <h2>Mzdové obdobia</h2>
            <p className="card-desc">Každá bunka = 1 mzdový mesiac. M = Mzda, B = Base, O = Odvod. Zelená = všetky 3 tabuľky prítomné, červená = niektorá chýba, sivá = žiadne dáta.</p>
            <div className="heatmap">
              {[...detail.periods].reverse().map((p) => {
                const allOk = p.hasMzda && p.hasBase && p.hasOdvod;
                const noneOk = !p.hasMzda && !p.hasBase && !p.hasOdvod;
                const cls = noneOk ? 'heatmap-empty'
                  : allOk ? 'heatmap-great'
                  : 'heatmap-bad';
                return (
                  <div
                    key={p.periodId}
                    className={`heatmap-cell ${cls}`}
                    title={`${formatPeriodId(p.periodId)}: mzda=${p.hasMzda ? '✓' : '✗'} base=${p.hasBase ? '✓' : '✗'} odvod=${p.hasOdvod ? '✓' : '✗'}`}
                  >
                    <span className="heatmap-month">{formatPeriodId(p.periodId)}</span>
                    <span className="heatmap-rate">{allOk ? '✓' : noneOk ? '–' : '!'}</span>
                    <span className="heatmap-docs">
                      {[p.hasMzda ? 'M' : '·', p.hasBase ? 'B' : '·', p.hasOdvod ? 'O' : '·'].join(' ')}
                    </span>
                  </div>
                );
              })}
            </div>
            <div className="heatmap-legend">
              <span className="heatmap-great">Kompletné</span>
              <span className="heatmap-bad">Neúplné</span>
              <span className="heatmap-empty">Bez dát</span>
            </div>
          </section>

        </div>
      )}
    </div>
  );
}

function OmegaCompanyDetailPage({
  dataReadServerId,
  databaseName,
  onBack
}: {
  dataReadServerId: string;
  databaseName: string;
  onBack: () => void;
}) {
  const [detail, setDetail] = useState<OmegaCompanyDetail | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState('');

  async function load() {
    setIsLoading(true);
    setError('');

    try {
      const response = await apiFetch(
        `/api/omega/dashboard/companies/${dataReadServerId}/${encodeURIComponent(databaseName)}`
      );

      if (!response.ok) {
        setError(await response.text());
        return;
      }

      setDetail(await response.json());
    } catch (exception) {
      setError(exception instanceof Error ? exception.message : 'Načítanie detailu zlyhalo.');
    } finally {
      setIsLoading(false);
    }
  }

  useEffect(() => {
    void load();
  }, [dataReadServerId, databaseName]);

  return (
    <div className="page">
      <header>
        <div className="back-header">
          <button className="back-btn" onClick={onBack}>← Späť</button>
          {detail && (
            <h1>
              {detail.companyName}
              <span className="muted-label"> ({detail.databaseName})</span>
            </h1>
          )}
        </div>
        <div className="actions">
          <button onClick={load} disabled={isLoading}>
            <RefreshCw size={16} /> {isLoading ? 'Načítavam' : 'Obnoviť'}
          </button>
        </div>
      </header>

      {error && <div className="status error">{error}</div>}
      {isLoading && <div className="status">Načítavam detail...</div>}

      {detail && (
        <div className="detail-layout">

          <section className="detail-card">
            <h2>Health score</h2>
            <p className="card-desc">Celkové zdravie firmy (0–100) ako vážený priemer 6 zložiek. Snapshot z posledného UpdateData.</p>
            <div className="health-score-hero">
              <span className={`score-pill large ${scoreClass(detail.healthScore)}`}>
                {detail.healthScore}
              </span>
            </div>
            <div className="score-breakdown">
              {([
                { label: 'Aktivita', value: detail.activityScore, weight: '17 %', desc: 'Počet dokladov za 30 dní. Nula alebo výrazný pokles oproti predchádzajúcemu týždňu = trest.' },
                { label: 'Completion', value: detail.completionScore, weight: '25 %', desc: 'Podiel zaúčtovaných z registrovaných dokladov za 30 dní. Cieľ: 100 %.' },
                { label: 'Backlog aging', value: detail.backlogAgingScore, weight: '23 %', desc: 'Nezaúčtované doklady v systéme — čím staršie, tým väčší trest na skóre.' },
                { label: 'Month closing', value: detail.monthClosingScore, weight: '15 %', desc: 'Posting rate predošlého mesiaca (< 95 % = problém) a historický priemer (< 98 % = problém).' },
                { label: 'Data quality', value: detail.dataQualityScore, weight: '5 %', desc: 'Chyby v dátach: doklady s null dátumom, orphan záznamy, neznáme kódy okruhov.' },
                { label: 'Timeliness', value: detail.timelinessScore, weight: '15 %', desc: 'Oneskorenie nahodenia a zaúčtovania dokladov za posl. 90 dní. Nad 14 dní = trest, nad 60 dní = 0.' }
              ] as const).map(({ label, value, weight, desc }) => (
                <div key={label} className="score-row">
                  <Tooltip content={desc}>
                    <span className="score-label">{label} <span className="muted-label">{weight}</span></span>
                  </Tooltip>
                  <div className="score-bar-track">
                    <div className={`score-bar-fill ${scoreClass(value)}`} style={{ width: `${value}%` }} />
                  </div>
                  <span className="score-value">{value}</span>
                </div>
              ))}
            </div>
          </section>

          {detail.warningFlags.length > 0 && (
            <section className="detail-card">
              <h2>Varovania</h2>
              <p className="card-desc">Automaticky detegované anomálie. Klikni na každý flag pre popis problému.</p>
              <div className="warning-list large">
                {detail.warningFlags.map((flag) => (
                  <Tooltip key={flag} content={warningTooltip(flag)}>
                    <span className={`warning-pill ${warningSeverity(flag)}`}>{warningLabel(flag)}</span>
                  </Tooltip>
                ))}
              </div>
            </section>
          )}

          <section className="detail-card">
            <h2>Workflow funnel — posledných 30 dní</h2>
            <p className="card-desc">Koľko dokladov prešlo z evidencie cez predkontáciu až na zaúčtovanie. Ideál: všetky zaevidované sú aj zaúčtované.</p>
            <div className="funnel">
              {([
                { label: 'Zaevidované', value: detail.registered30Days },
                { label: 'Predkontované', value: detail.preAccounted30Days },
                { label: 'Zaúčtované', value: detail.posted30Days }
              ] as const).map(({ label, value }) => {
                const pct = detail.registered30Days === 0
                  ? 0
                  : Math.round(value * 100 / detail.registered30Days);
                return (
                  <div key={label} className="funnel-row">
                    <span className="funnel-label">{label}</span>
                    <div className="funnel-bar-track">
                      <div className="funnel-bar-fill" style={{ width: `${pct}%` }} />
                    </div>
                    <span className="funnel-value">
                      {value} <span className="muted-label">({pct} %)</span>
                    </span>
                  </div>
                );
              })}
            </div>
          </section>

          <section className="detail-card">
            <h2>Typ dokladov — posledných 30 dní</h2>
            <p className="card-desc">Členenie podľa spôsobu vzniku dokladu: ručne zadané, importované zo súboru alebo cez platformu/API.</p>
            <div className="type-breakdown">
              {([
                { label: 'Manuálne', value: detail.manual30Days },
                { label: 'Importované', value: detail.imported30Days },
                { label: 'Platforma / API', value: detail.platform30Days }
              ] as const).map(({ label, value }) => (
                <div key={label} className="type-row">
                  <span>{label}</span>
                  <strong>{value}</strong>
                </div>
              ))}
            </div>
          </section>

          <section className="detail-card">
            <h2>Backlog — nezaúčtované doklady</h2>
            <p className="card-desc">Všetky doklady v systéme, ktoré ešte neboli zaúčtované, rozdelené podľa veku od zaevidovania. Čím starší doklad, tým väčší problém.</p>
            <div className="aging-grid">
              {([
                { label: '0–3 dni', value: detail.backlog0to3Days, cls: 'watch' },
                { label: '4–7 dní', value: detail.backlog4to7Days, cls: 'bad' },
                { label: '8–30 dní', value: detail.backlog8to30Days, cls: 'bad' },
                { label: '30+ dní', value: detail.backlog30DaysPlus, cls: 'critical' }
              ] as const).map(({ label, value, cls }) => (
                <div key={label} className={`aging-bucket ${value > 0 ? cls : 'ok'}`}>
                  <span className="aging-count">{value}</span>
                  <span className="aging-label">{label}</span>
                </div>
              ))}
            </div>
          </section>

          <section className="detail-card">
            <h2>Oneskorenie nahodenia dokladov <span className="muted-label">(posl. 90 dní)</span></h2>
            <p className="card-desc">Počet dní od dátumu vystavenia dokladu po jeho zadanie do systému. Meria, ako rýchlo sa fyzické doklady dostanú do Omegy.</p>
            <div className="late-entry-grid">
              {([
                { label: 'DF — Doručené faktúry', avg: detail.lateEntryDfAvgDays, max: detail.lateEntryDfMaxDays, count: detail.lateEntryDfCount },
                { label: 'OF — Odoslané faktúry', avg: detail.lateEntryOfAvgDays, max: detail.lateEntryOfMaxDays, count: detail.lateEntryOfCount },
                { label: 'PD — Pokladňa',         avg: detail.lateEntryPdAvgDays, max: detail.lateEntryPdMaxDays, count: detail.lateEntryPdCount }
              ]).map(({ label, avg, max, count }) => {
                const cls = avg === null ? 'ok' : avg > 30 ? 'critical' : avg > 14 ? 'bad' : 'ok';
                return (
                  <div key={label} className={`late-entry-row ${cls}`}>
                    <span className="late-entry-label">{label}</span>
                    {count === 0
                      ? <span className="muted-label">žiadne doklady</span>
                      : <>
                          <span className="late-entry-avg">{avg ?? 0} dní <span className="muted-label">avg</span></span>
                          <span className="late-entry-max muted-label">max {max ?? 0} dní</span>
                          <span className="late-entry-count muted-label">{count} dokladov</span>
                        </>
                    }
                  </div>
                );
              })}
            </div>
          </section>

          <section className="detail-card">
            <h2>Oneskorenie zaúčtovania dokladov <span className="muted-label">(posl. 90 dní)</span></h2>
            <p className="card-desc">Počet dní od zaevidovania po zaúčtovanie do hlavnej knihy (HLK). Zahrnuté sú doklady zaúčtované v posledných 90 dňoch.</p>
            <div className="late-entry-grid">
              {([
                { label: 'DF — Doručené faktúry', avg: detail.postingLagDfAvgDays, max: detail.postingLagDfMaxDays, count: detail.postingLagDfCount, over30: detail.postingLagDfOver30Count },
                { label: 'OF — Odoslané faktúry', avg: detail.postingLagOfAvgDays, max: detail.postingLagOfMaxDays, count: detail.postingLagOfCount, over30: detail.postingLagOfOver30Count },
                { label: 'PD — Pokladňa',         avg: detail.postingLagPdAvgDays, max: detail.postingLagPdMaxDays, count: detail.postingLagPdCount, over30: detail.postingLagPdOver30Count },
                { label: 'BV — Bankový výpis',    avg: detail.postingLagBvAvgDays, max: detail.postingLagBvMaxDays, count: detail.postingLagBvCount, over30: detail.postingLagBvOver30Count }
              ]).map(({ label, avg, max, count, over30 }) => {
                const cls = avg === null ? 'ok' : avg > 30 ? 'critical' : avg > 14 ? 'bad' : 'ok';
                const over30Pct = count > 0 ? Math.round(over30 * 100 / count) : 0;
                return (
                  <div key={label} className={`late-entry-row ${cls}`}>
                    <span className="late-entry-label">{label}</span>
                    {count === 0
                      ? <span className="muted-label">žiadne zaúčtované doklady</span>
                      : <>
                          <span className="late-entry-avg">{avg ?? 0} dní <span className="muted-label">avg</span></span>
                          <span className="late-entry-max muted-label">max {max ?? 0} dní</span>
                          <span className="late-entry-count muted-label">{count} dokladov</span>
                          {over30 > 0 && (
                            <span className={`late-entry-over30 ${over30Pct >= 50 ? 'critical' : 'bad'}`}>
                              {over30}× nad 30 dní ({over30Pct} %)
                            </span>
                          )}
                        </>
                    }
                  </div>
                );
              })}
            </div>
          </section>

          <section className="detail-card full-width">
            <h2>Mesačné zaúčtovanie — {detail.accountingYear ?? 'posledných 12 mes.'}</h2>
            <p className="card-desc">Posting rate (% zaúčtovaných z registrovaných dokladov) za každý mesiac. Chýbajúci mesiac s dokladmi je označený ako problém.</p>
            <div className="heatmap">
              {(() => {
                const now = new Date();
                const todayYear = now.getFullYear();
                const todayMonth = now.getMonth() + 1;
                const startYear = detail.accountingYear ?? todayYear;
                const isCurrentYear = startYear >= todayYear;

                const dataMap = new Map(
                  detail.monthlyBreakdown.map(m => [`${m.year}-${m.month}`, m])
                );

                const existingKeys = new Set(detail.monthlyBreakdown.map(m => `${m.year}-${m.month}`));

                const lastEntry = detail.monthlyBreakdown.length > 0
                  ? detail.monthlyBreakdown.reduce((max, m) =>
                      m.year > max.year || (m.year === max.year && m.month > max.month) ? m : max
                    )
                  : null;

                // Current year: fill up to today (months not yet processed = problem)
                // Past year: fill only up to the last existing data month
                const fillEndYear = isCurrentYear ? todayYear : lastEntry?.year ?? startYear;
                const fillEndMonth = isCurrentYear ? todayMonth : lastEntry?.month ?? 0;

                const missingEntries: { year: number; month: number }[] = [];
                if (isCurrentYear || lastEntry) {
                  for (let y = startYear; y <= fillEndYear; y++) {
                    const endM = y === fillEndYear ? fillEndMonth : 12;
                    for (let m = 1; m <= endM; m++) {
                      if (!existingKeys.has(`${y}-${m}`)) {
                        missingEntries.push({ year: y, month: m });
                      }
                    }
                  }
                }

                const allEntries = [
                  ...detail.monthlyBreakdown.map(m => ({ year: m.year, month: m.month, missing: false })),
                  ...missingEntries.map(e => ({ ...e, missing: true }))
                ].sort((a, b) => a.year !== b.year ? a.year - b.year : a.month - b.month);

                return allEntries.map(({ year, month, missing }) => {
                  const m = dataMap.get(`${year}-${month}`);
                  const rate = m?.postingRate ?? null;
                  const registered = m?.registered ?? 0;
                  const posted = m?.posted ?? 0;
                  const cls = missing ? 'heatmap-missing'
                    : rate === null ? 'heatmap-empty'
                    : rate >= 98 ? 'heatmap-great'
                    : rate >= 95 ? 'heatmap-ok'
                    : rate >= 90 ? 'heatmap-warn'
                    : 'heatmap-bad';
                  const label = new Intl.DateTimeFormat('sk-SK', { month: 'short', year: '2-digit' })
                    .format(new Date(year, month - 1, 1));
                  return (
                    <div
                      key={`${year}-${month}`}
                      className={`heatmap-cell ${cls}`}
                      title={`${label}: ${missing ? 'žiadne doklady — problém' : rate !== null ? rate.toFixed(1) + ' %' : 'bez dát'} (${registered} / ${posted})`}
                    >
                      <span className="heatmap-month">{label}</span>
                      <span className="heatmap-rate">{missing ? '!' : rate !== null ? `${rate.toFixed(0)} %` : '–'}</span>
                      <span className="heatmap-docs">{registered} / {posted}</span>
                    </div>
                  );
                });
              })()}
            </div>
            <div className="heatmap-legend">
              <span className="heatmap-great">≥ 98 %</span>
              <span className="heatmap-ok">95–97 %</span>
              <span className="heatmap-warn">90–94 %</span>
              <span className="heatmap-bad">&lt; 90 %</span>
              <span className="heatmap-missing">chýba</span>
              <span className="heatmap-empty">bez dát</span>
            </div>
          </section>

        </div>
      )}
    </div>
  );
}

function scoreClass(score: number) {
  if (score >= 85) {
    return 'good';
  }

  if (score >= 70) {
    return 'watch';
  }

  if (score >= 50) {
    return 'bad';
  }

  return 'critical';
}

function warningSeverity(warning: string) {
  if (['OLD_UNPOSTED_30D', 'ZERO_POSTING', 'BROKEN_MONTH', 'UNKNOWN_CIRCUIT', 'LATE_ENTRY_CRIT', 'POSTING_LAG_CRIT'].includes(warning)) {
    return 'critical';
  }

  if (['OLD_UNPOSTED_7D', 'ACTIVITY_DROP', 'IMPORT_ONLY', 'HIGH_STORNO_RATE',
       'STUCK_PREACCOUNTED', 'ORPHAN_LINES', 'ORPHAN_PAYMENTS', 'NULL_DATE', 'LATE_ENTRY', 'POSTING_LAG'].includes(warning)) {
    return 'bad';
  }

  return 'watch';
}

function warningLabel(warning: string) {
  const labels: Record<string, string> = {
    OLD_UNPOSTED_30D: '30d backlog',
    OLD_UNPOSTED_7D: '7d backlog',
    ZERO_POSTING: 'no posting',
    ACTIVITY_DROP: 'drop',
    IMPORT_ONLY: 'import only',
    BROKEN_MONTH: 'broken month',
    HIGH_STORNO_RATE: 'storno',
    STUCK_PREACCOUNTED: 'stuck',
    NULL_DATE: 'null date',
    ORPHAN_LINES: 'orphan lines',
    ORPHAN_PAYMENTS: 'orphan payments',
    UNKNOWN_CIRCUIT: 'unknown circuit',
    LATE_ENTRY: 'neskoré nahodenie',
    LATE_ENTRY_CRIT: 'neskoré nahodenie!',
    POSTING_LAG: 'pomalé zaúčtovanie',
    POSTING_LAG_CRIT: 'pomalé zaúčtovanie!'
  };

  return labels[warning] ?? warning;
}

function olympWarningSeverity(warning: string) {
  if (['CURRENT_MONTH_MISSING', 'PAYROLL_MONTHS_BEHIND', 'INACTIVE_30D'].includes(warning)) {
    return 'critical';
  }

  if (['INACTIVE_14D', 'MISSING_PERIODS', 'PAYROLL_INCONSISTENCY', 'NULL_STARTTIME'].includes(warning)) {
    return 'bad';
  }

  return 'watch';
}

function olympWarningLabel(warning: string) {
  const labels: Record<string, string> = {
    CURRENT_MONTH_MISSING: 'mesiac chýba',
    PAYROLL_MONTHS_BEHIND: 'oneskorené mzdy',
    INACTIVE_30D: '30d neaktivita',
    INACTIVE_14D: '14d neaktivita',
    MISSING_PERIODS: 'chýbajúce obdobia',
    PAYROLL_INCONSISTENCY: 'nekonzistencia',
    NULL_STARTTIME: 'null timestamp',
    LOW_ACTIVITY: 'nízka aktivita'
  };

  return labels[warning] ?? warning;
}

function warningTooltip(warning: string) {
  const tooltips: Record<string, string> = {
    OLD_UNPOSTED_30D: 'Existujú nezaúčtované doklady staršie ako 30 dní',
    OLD_UNPOSTED_7D: 'Existujú nezaúčtované doklady staršie ako 7 dní',
    ZERO_POSTING: 'Za 30 dní sú registrované doklady, ale žiadne zaúčtované',
    ACTIVITY_DROP: 'Aktivita posledných 7 dní klesla pod 50 % predchádzajúceho 7-dňového okna',
    IMPORT_ONLY: 'Doklady prichádzajú cez import/platformu, ale žiadne nie sú zaúčtované',
    BROKEN_MONTH: 'Predchádzajúci mesiac má <95 % alebo historický priemer <98 % zaúčtovaných',
    HIGH_STORNO_RATE: 'Storno rate >10 % za posledných 30 dní — veľa rušených dokladov',
    STUCK_PREACCOUNTED: 'Doklady sú predkontované, ale stále nie sú zaúčtované',
    NULL_DATE: 'Existujú doklady s chýbajúcim dátumom zaevidovania',
    ORPHAN_LINES: 'Riadky dokladov bez zodpovedajúcej hlavičky — narušená integrita dát',
    ORPHAN_PAYMENTS: 'Platby/odpočty bez zodpovedajúcej hlavičky — narušená integrita dát',
    UNKNOWN_CIRCUIT: 'Doklady s kódom okruhu, ktorý neexistuje v číselníku T025',
    LATE_ENTRY: 'Priemerné oneskorenie nahodenia dokladu >14 dní (dátum vystavenia vs. dátum zaevidovania)',
    LATE_ENTRY_CRIT: 'Priemerné oneskorenie nahodenia dokladu >30 dní — doklady prichádzajú s výrazným oneskorením',
    POSTING_LAG: 'Priemerné oneskorenie zaúčtovania >14 dní (dátum zaevidovania vs. dátum zaúčtovania)',
    POSTING_LAG_CRIT: 'Priemerné oneskorenie zaúčtovania >30 dní — doklady nahodené, ale zaúčtovávané s veľkým oneskorením'
  };
  return tooltips[warning] ?? '';
}

function olympWarningTooltip(warning: string) {
  const tooltips: Record<string, string> = {
    CURRENT_MONTH_MISSING: 'Aktuálny mesiac nebol nájdený v mzdovom spracovaní',
    PAYROLL_MONTHS_BEHIND: 'Mzdy sú oneskorené — posledné spracované obdobie je o viac ako mesiac staré',
    INACTIVE_30D: 'Žiadna aktivita v Olympe za posledných 30 dní',
    INACTIVE_14D: 'Žiadna aktivita v Olympe za posledných 14 dní',
    MISSING_PERIODS: 'Niektoré mzdové obdobia v rámci roka v dátach chýbajú',
    PAYROLL_INCONSISTENCY: 'Niektoré obdobia nie sú prítomné vo všetkých 3 tabuľkách (Mzda / Base / Odvod)',
    NULL_STARTTIME: 'Záznamy v activity logu s prázdnym časom začatia (StartTime)',
    LOW_ACTIVITY: 'Aktivita je výrazne nižšia ako normálne'
  };
  return tooltips[warning] ?? '';
}

function formatDate(value: string | null) {
  if (!value) {
    return '-';
  }

  return new Intl.DateTimeFormat('sk-SK').format(new Date(value));
}

function formatTrend(value: number | null) {
  if (value === null) {
    return '-';
  }

  return `${value > 0 ? '+' : ''}${value.toFixed(1)}%`;
}

function formatSnapshotAge(isoDate: string | null) {
  if (!isoDate) return null;
  const diffHours = Math.floor((Date.now() - new Date(isoDate).getTime()) / 3_600_000);
  if (diffHours < 1) return '< 1 hod';
  if (diffHours < 24) return `${diffHours} hod`;
  return `${Math.floor(diffHours / 24)} dní`;
}

function formatPeriodId(periodId: string | null) {
  if (!periodId || periodId.length !== 6) return periodId ?? '–';
  return `${periodId.slice(0, 4)}/${periodId.slice(4)}`;
}

function OrphanPaymentsModal({ dataReadServerId, databaseName, companyName, onClose }: {
  dataReadServerId: string;
  databaseName: string;
  companyName: string;
  onClose: () => void;
}) {
  const [result, setResult] = useState<OmegaOrphanPaymentsResult | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    async function load() {
      try {
        const response = await apiFetch(
          `/api/omega/dashboard/companies/${dataReadServerId}/${encodeURIComponent(databaseName)}/orphan-payments`
        );
        if (!response.ok) {
          setError(await response.text());
          return;
        }
        setResult(await response.json() as OmegaOrphanPaymentsResult);
      } catch {
        setError('Chyba pri načítaní dát.');
      } finally {
        setIsLoading(false);
      }
    }
    load();
  }, []);

  return createPortal(
    <div className="help-overlay" onClick={onClose}>
      <div className="orphan-modal" onClick={(e) => e.stopPropagation()}>
        <div className="help-modal-header">
          <span>Orphan payments — {companyName}</span>
          <button className="help-close" onClick={onClose}>✕</button>
        </div>
        <div className="orphan-modal-body">
          {isLoading && <p className="muted-label">Načítavam...</p>}
          {error && <p className="error-text">{error}</p>}
          {result && (
            <>
              {result.totalCount > 50 && (
                <p className="orphan-notice">
                  Zobrazených 50 z {result.totalCount} záznamov — opravte a obnovte dáta.
                </p>
              )}
              {result.totalCount === 0 && (
                <p className="muted-label">Žiadne orphan payments — problém bol opravený.</p>
              )}
              {result.items.length > 0 && (
                <div className="orphan-scroll">
                  <table className="orphan-table">
                    <thead>
                      <tr>
                        <th>ID záznamu</th>
                        <th>ID chýb. dokladu</th>
                        <th>Typ</th>
                        <th>Číslo</th>
                        <th>Dátum</th>
                        <th>Suma</th>
                        <th>Mena</th>
                        <th>Druh</th>
                        <th>Poznámka</th>
                      </tr>
                    </thead>
                    <tbody>
                      {result.items.map((item) => (
                        <tr key={item.paymentId}>
                          <td>{item.paymentId}</td>
                          <td className="orphan-id-cell">{item.orphanedDocumentId}</td>
                          <td>{item.documentType}</td>
                          <td>{item.internalNumber}</td>
                          <td>{item.paymentDate ? formatDate(item.paymentDate) : '–'}</td>
                          <td className="orphan-amount-cell">{item.amount.toFixed(2)}</td>
                          <td>{item.currency}</td>
                          <td>{item.paymentKind}</td>
                          <td className="orphan-note-cell">{item.note ?? ''}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </>
          )}
        </div>
      </div>
    </div>,
    document.body
  );
}

function readFilters(key: string): DashboardFilterState {
  try {
    const raw = localStorage.getItem(key);
    if (!raw) return { ...EMPTY_FILTER };
    const parsed = JSON.parse(raw) as DashboardFilterState;
    if (
      typeof parsed.nameQuery !== 'string' ||
      !Array.isArray(parsed.selectedFlags) ||
      !parsed.selectedFlags.every((f) => typeof f === 'string')
    ) {
      return { ...EMPTY_FILTER };
    }
    return parsed;
  } catch {
    return { ...EMPTY_FILTER };
  }
}

function writeFilters(key: string, state: DashboardFilterState): void {
  if (!state.nameQuery && state.selectedFlags.length === 0) {
    localStorage.removeItem(key);
  } else {
    localStorage.setItem(key, JSON.stringify(state));
  }
}

createRoot(document.getElementById('root')!).render(
  <AuthGate>{(user, logout) => <App user={user} onLogout={logout} />}</AuthGate>
);
