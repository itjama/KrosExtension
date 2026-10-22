import React, { useCallback, useEffect, useState } from 'react';
import { apiFetch, onUnauthorized, readError } from './api';

export type CurrentUser = { id: string; userName: string; role: 'Admin' | 'User'; mustChangePassword: boolean };

type AuthState = { status: 'loading' } | { status: 'anonymous' } | { status: 'user'; user: CurrentUser };

export function AuthGate({ children }: { children: (user: CurrentUser, logout: () => Promise<void>) => React.ReactNode }) {
  const [state, setState] = useState<AuthState>({ status: 'loading' });

  useEffect(() => {
    let cancelled = false;
    apiFetch('/api/auth/me')
      .then(async (response) => {
        if (cancelled) return;
        if (response.ok) {
          setState({ status: 'user', user: await response.json() as CurrentUser });
        } else {
          setState({ status: 'anonymous' });
        }
      })
      .catch(() => { if (!cancelled) setState({ status: 'anonymous' }); });
    const unsubscribe = onUnauthorized(() => setState({ status: 'anonymous' }));
    return () => { cancelled = true; unsubscribe(); };
  }, []);

  const logout = useCallback(async () => {
    try {
      await apiFetch('/api/auth/logout', { method: 'POST' });
    } finally {
      setState({ status: 'anonymous' });
    }
  }, []);

  if (state.status === 'loading') {
    return <div className="auth-page" />;
  }
  if (state.status === 'anonymous') {
    return <LoginForm onLoggedIn={(user) => setState({ status: 'user', user })} />;
  }
  if (state.user.mustChangePassword) {
    return (
      <div className="auth-page">
        <div className="auth-card">
          <h1>Zmena hesla</h1>
          <p className="auth-hint">Pred pokračovaním si musíš zmeniť heslo.</p>
          <ChangePasswordForm onDone={(user) => setState({ status: 'user', user })} />
          <button type="button" className="auth-link" onClick={() => void logout()}>Odhlásiť</button>
        </div>
      </div>
    );
  }
  return <>{children(state.user, logout)}</>;
}

function LoginForm({ onLoggedIn }: { onLoggedIn: (user: CurrentUser) => void }) {
  const [userName, setUserName] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);

  async function submit(event: React.FormEvent) {
    event.preventDefault();
    setBusy(true);
    setError('');
    try {
      const response = await fetch('/api/auth/login', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ userName, password })
      });
      if (response.ok) {
        onLoggedIn(await response.json() as CurrentUser);
      } else {
        setError(await readError(response));
      }
    } catch {
      setError('Server neodpovedá.');
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="auth-page">
      <form className="auth-card" onSubmit={submit}>
        <h1>LedgerPulse</h1>
        <label>
          Meno
          <input value={userName} autoComplete="username" autoFocus onChange={(event) => setUserName(event.target.value)} />
        </label>
        <label>
          Heslo
          <input type="password" value={password} autoComplete="current-password" onChange={(event) => setPassword(event.target.value)} />
        </label>
        {error && <div className="auth-error">{error}</div>}
        <button type="submit" disabled={busy || !userName || !password}>Prihlásiť</button>
        <p className="auth-hint">Zabudnuté heslo? Kontaktuj admina.</p>
      </form>
    </div>
  );
}

export function ChangePasswordForm({ onDone, onCancel }: { onDone: (user: CurrentUser) => void; onCancel?: () => void }) {
  const [currentPassword, setCurrentPassword] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [repeat, setRepeat] = useState('');
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);

  async function submit(event: React.FormEvent) {
    event.preventDefault();
    if (newPassword.length < 10) {
      setError('Heslo musí mať aspoň 10 znakov.');
      return;
    }
    if (newPassword === currentPassword) {
      setError('Nové heslo musí byť iné ako súčasné.');
      return;
    }
    if (newPassword !== repeat) {
      setError('Heslá sa nezhodujú.');
      return;
    }
    setBusy(true);
    setError('');
    try {
      const response = await apiFetch('/api/auth/change-password', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ currentPassword, newPassword })
      });
      if (response.ok) {
        onDone(await response.json() as CurrentUser);
      } else if (response.status !== 401) {
        setError(await readError(response));
      }
    } catch {
      setError('Server neodpovedá.');
    } finally {
      setBusy(false);
    }
  }

  return (
    <form className="auth-form" onSubmit={submit}>
      <label>
        Súčasné heslo
        <input type="password" value={currentPassword} autoComplete="current-password" onChange={(event) => setCurrentPassword(event.target.value)} />
      </label>
      <label>
        Nové heslo
        <input type="password" value={newPassword} autoComplete="new-password" onChange={(event) => setNewPassword(event.target.value)} />
      </label>
      <label>
        Nové heslo znova
        <input type="password" value={repeat} autoComplete="new-password" onChange={(event) => setRepeat(event.target.value)} />
      </label>
      {error && <div className="auth-error">{error}</div>}
      <div className="auth-actions">
        <button type="submit" disabled={busy || !currentPassword || !newPassword || !repeat}>Zmeniť heslo</button>
        {onCancel && <button type="button" onClick={onCancel}>Zrušiť</button>}
      </div>
    </form>
  );
}
