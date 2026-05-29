import React, { useEffect, useState } from 'react';
import { createRoot } from 'react-dom/client';
import { BarChart3, ChevronDown, Database, RefreshCw, Save, Server, Settings, Trash2 } from 'lucide-react';
import './styles.css';

type Product = 'olymp' | 'omega';
type AuthenticationMode = 'Windows' | 'SqlLogin';
type Section = 'stats-olymp' | 'stats-omega' | 'settings-servers' | 'settings-olymp' | 'settings-omega';

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
  duplicateDocumentLinks: number;
  dataQualityIssues: number;
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

function App() {
  const [section, setSection] = useState<Section>('stats-omega');
  const [settingsOpen, setSettingsOpen] = useState(true);

  return (
    <main className="app-shell">
      <aside className="sidebar">
        <div className="brand">Kros Extension</div>
        <button className={section === 'stats-olymp' ? 'active' : ''} onClick={() => setSection('stats-olymp')}>
          <BarChart3 size={18} /> Olymp
        </button>
        <button className={section === 'stats-omega' ? 'active' : ''} onClick={() => setSection('stats-omega')}>
          <BarChart3 size={18} /> Omega
        </button>
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
      </aside>
      <section className="workspace">
        {section === 'settings-servers' && <ServersPage />}
        {section === 'settings-olymp' && <CompaniesPage product="olymp" />}
        {section === 'settings-omega' && <CompaniesPage product="omega" />}
        {section === 'stats-olymp' && <StatisticsPage product="olymp" />}
        {section === 'stats-omega' && <OmegaDashboardPage />}
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
    const response = await fetch('/api/data-read-servers');

    if (!response.ok) {
      setError(await response.text());
      return;
    }

    setServers(await response.json());
  }

  async function addServer() {
    setError('');
    const response = await fetch('/api/data-read-servers', {
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
    await fetch(`/api/data-read-servers/${id}`, { method: 'DELETE' });
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
    const response = await fetch(`/api/${product}/refresh-settings`);

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

    const response = await fetch(`/api/${product}/refresh-settings`, {
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
      const response = await fetch(`/api/${product}/companies/discover`);

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
    const response = await fetch(`/api/${product}/companies/tracked`, {
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

      <table>
        <thead>
          <tr>
            <th>Sledovať</th>
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
      const response = await fetch(`/api/${product}/companies/statistics`);

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

function OmegaDashboardPage() {
  const [companies, setCompanies] = useState<OmegaDashboardCompany[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [isUpdating, setIsUpdating] = useState(false);
  const [error, setError] = useState('');

  async function load() {
    setIsLoading(true);
    setError('');

    try {
      const response = await fetch('/api/omega/dashboard/companies');

      if (!response.ok) {
        setError(await response.text());
        setCompanies([]);
        return;
      }

      setCompanies(await response.json());
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
      const response = await fetch('/api/omega/companies/update-data', {
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

  return (
    <div className="page">
      <header>
        <h1>Omega dashboard</h1>
        <div className="actions">
          <button onClick={load} disabled={isLoading || isUpdating}>
            <RefreshCw size={16} /> {isLoading ? 'Načítavam' : 'Obnoviť'}
          </button>
          <button onClick={updateData} disabled={isLoading || isUpdating}>
            <RefreshCw size={16} /> {isUpdating ? 'UpdateData' : 'UpdateData'}
          </button>
        </div>
      </header>

      {error && <div className="status error">{error}</div>}

      <table className="risk-table">
        <thead>
          <tr>
            <th>Firma</th>
            <th>Health</th>
            <th>7 dní</th>
            <th>30 dní</th>
            <th>% zaúčt.</th>
            <th>Backlog</th>
            <th>Staré</th>
            <th>Posledná aktivita</th>
            <th>Trend</th>
            <th>Warning flags</th>
          </tr>
        </thead>
        <tbody>
          {!isLoading && companies.length === 0 && (
            <tr>
              <td colSpan={10}>Omega dashboard zatiaľ nemá snapshoty. Spusti UpdateData.</td>
            </tr>
          )}
          {companies.map((company) => (
            <tr key={`${company.dataReadServerId}-${company.databaseName}`}>
              <td>
                <strong>{company.companyName}</strong>
                <span className="muted-line">{company.databaseName}</span>
              </td>
              <td>
                <span className={`score-pill ${scoreClass(company.healthScore)}`}>{company.healthScore}</span>
              </td>
              <td>{company.activity7Days}</td>
              <td>{company.activity30Days}</td>
              <td>{company.postingRate30Days.toFixed(1)}%</td>
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
                  {company.warningFlags.map((warning) => (
                    <span key={warning} className={`warning-pill ${warningSeverity(warning)}`}>{warningLabel(warning)}</span>
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
  if (['OLD_UNPOSTED_30D', 'ZERO_POSTING', 'BROKEN_MONTH', 'UNKNOWN_CIRCUIT'].includes(warning)) {
    return 'critical';
  }

  if (['OLD_UNPOSTED_7D', 'ACTIVITY_DROP', 'IMPORT_ONLY', 'DUPLICATES',
       'ORPHAN_LINES', 'ORPHAN_PAYMENTS', 'NULL_DATE'].includes(warning)) {
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
    DUPLICATES: 'duplicates',
    NULL_DATE: 'null date',
    ORPHAN_LINES: 'orphan lines',
    ORPHAN_PAYMENTS: 'orphan payments',
    UNKNOWN_CIRCUIT: 'unknown circuit'
  };

  return labels[warning] ?? warning;
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

createRoot(document.getElementById('root')!).render(<App />);
