import React, { useEffect, useState } from 'react';
import { Plus, RefreshCw } from 'lucide-react';
import { apiFetch, readError } from '../api';

type Role = 'Admin' | 'User';

type AdminUser = {
  id: string;
  userName: string;
  role: Role;
  isBlocked: boolean;
  mustChangePassword: boolean;
  isLockedOut: boolean;
  createdAt: string;
  lastLoginAt: string | null;
};

const jsonHeaders = { 'Content-Type': 'application/json' };

function statusLabels(user: AdminUser): string {
  const labels: string[] = [];
  if (user.isBlocked) {
    labels.push('zablokovaný');
  } else {
    labels.push('aktívny');
  }
  if (user.mustChangePassword) {
    labels.push('musí zmeniť heslo');
  }
  if (user.isLockedOut) {
    labels.push('zamknutý');
  }
  return labels.join(', ');
}

function formatLastLogin(value: string | null): string {
  return value ? new Date(value).toLocaleString('sk-SK') : '—';
}

export function UsersPage({ currentUserId }: { currentUserId: string }) {
  const [users, setUsers] = useState<AdminUser[]>([]);
  const [error, setError] = useState('');
  const [formOpen, setFormOpen] = useState(false);
  const [newUserName, setNewUserName] = useState('');
  const [newRole, setNewRole] = useState<Role>('User');
  const [tempPassword, setTempPassword] = useState<{ userName: string; password: string } | null>(null);
  const [copied, setCopied] = useState(false);

  async function load() {
    const response = await apiFetch('/api/admin/users');
    if (!response.ok) {
      setError(await readError(response));
      return;
    }
    setUsers(await response.json());
  }

  useEffect(() => {
    void load();
  }, []);

  function showTempPassword(userName: string, password: string | null | undefined) {
    setCopied(false);
    setTempPassword({ userName, password: password ?? '' });
  }

  async function createUser() {
    setError('');
    const response = await apiFetch('/api/admin/users', {
      method: 'POST',
      headers: jsonHeaders,
      body: JSON.stringify({ userName: newUserName.trim(), role: newRole })
    });
    if (!response.ok) {
      setError(await readError(response));
      return;
    }
    const created: { user: AdminUser; temporaryPassword: string | null } = await response.json();
    setFormOpen(false);
    setNewUserName('');
    setNewRole('User');
    showTempPassword(created.user.userName, created.temporaryPassword);
    await load();
  }

  async function resetPassword(user: AdminUser) {
    setError('');
    const response = await apiFetch(`/api/admin/users/${user.id}/reset-password`, { method: 'POST' });
    if (!response.ok) {
      setError(await readError(response));
      return;
    }
    const result: { temporaryPassword: string | null } = await response.json();
    showTempPassword(user.userName, result.temporaryPassword);
    await load();
  }

  async function setBlocked(user: AdminUser, blocked: boolean) {
    setError('');
    const response = await apiFetch(`/api/admin/users/${user.id}/${blocked ? 'block' : 'unblock'}`, { method: 'POST' });
    if (!response.ok) {
      setError(await readError(response));
    }
    await load();
  }

  async function changeRole(user: AdminUser, role: Role) {
    setError('');
    const response = await apiFetch(`/api/admin/users/${user.id}/role`, {
      method: 'PUT',
      headers: jsonHeaders,
      body: JSON.stringify({ role })
    });
    if (!response.ok) {
      setError(await readError(response));
    }
    await load();
  }

  async function copyPassword() {
    if (!tempPassword) {
      return;
    }
    try {
      await navigator.clipboard.writeText(tempPassword.password);
      setCopied(true);
    } catch {
      setCopied(false);
    }
  }

  return (
    <div className="page">
      <header>
        <h1>Používatelia</h1>
        <div className="actions">
          <button onClick={() => void load()}><RefreshCw size={16} /> Obnoviť</button>
          <button onClick={() => setFormOpen((open) => !open)}><Plus size={16} /> Nový</button>
        </div>
      </header>

      {formOpen && (
        <div className="server-form">
          <div className="form-row">
            <label>
              Meno
              <input value={newUserName} onChange={(event) => setNewUserName(event.target.value)} autoFocus />
            </label>
            <label>
              Rola
              <select value={newRole} onChange={(event) => setNewRole(event.target.value as Role)}>
                <option value="User">Používateľ</option>
                <option value="Admin">Admin</option>
              </select>
            </label>
          </div>
          <div className="form-actions">
            <button onClick={() => setFormOpen(false)}>Zrušiť</button>
            <button onClick={() => void createUser()} disabled={!newUserName.trim()}>Vytvoriť</button>
          </div>
        </div>
      )}

      {error && <div className="status error">{error}</div>}

      <table className="companies-table">
        <thead>
          <tr>
            <th>Meno</th>
            <th>Rola</th>
            <th>Stav</th>
            <th>Posledné prihlásenie</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          {users.map((user) => {
            const isSelf = user.id === currentUserId;
            return (
              <tr key={user.id}>
                <td>{user.userName}</td>
                <td>{user.role === 'Admin' ? 'Admin' : 'Používateľ'}</td>
                <td>{statusLabels(user)}</td>
                <td>{formatLastLogin(user.lastLoginAt)}</td>
                <td>
                  <div className="actions">
                    {!isSelf && (
                      <>
                        <button onClick={() => void resetPassword(user)}>Reset hesla</button>
                        <button onClick={() => void setBlocked(user, !user.isBlocked)}>
                          {user.isBlocked ? 'Odblokovať' : 'Zablokovať'}
                        </button>
                        <select
                          value={user.role}
                          onChange={(event) => void changeRole(user, event.target.value as Role)}
                          title="Rola"
                        >
                          <option value="User">Používateľ</option>
                          <option value="Admin">Admin</option>
                        </select>
                      </>
                    )}
                  </div>
                </td>
              </tr>
            );
          })}
        </tbody>
      </table>

      {tempPassword && (
        <div className="help-overlay">
          <div className="help-modal" onClick={(event) => event.stopPropagation()}>
            <div className="help-modal-header">
              <h2>Dočasné heslo: {tempPassword.userName}</h2>
            </div>
            <div className="help-modal-body">
              <p><code className="temp-password">{tempPassword.password}</code></p>
              <p>Dočasné heslo sa už znova nezobrazí. Odovzdaj ho používateľovi, pri prvom prihlásení si ho zmení.</p>
              <div className="form-actions">
                <button onClick={() => void copyPassword()}>{copied ? 'Skopírované' : 'Kopírovať'}</button>
                <button onClick={() => setTempPassword(null)}>Zavrieť</button>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
