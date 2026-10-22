const unauthorizedEvent = 'auth:unauthorized';

export async function apiFetch(input: RequestInfo, init?: RequestInit): Promise<Response> {
  const response = await fetch(input, init);
  if (response.status === 401) {
    const url = typeof input === 'string' ? input : input instanceof Request ? input.url : String(input);
    // A 401 from the login request itself means a wrong password, not an expired session.
    if (!url.includes('/api/auth/login')) {
      window.dispatchEvent(new Event(unauthorizedEvent));
    }
  }
  return response;
}

export function onUnauthorized(handler: () => void): () => void {
  window.addEventListener(unauthorizedEvent, handler);
  return () => window.removeEventListener(unauthorizedEvent, handler);
}

export async function readError(response: Response): Promise<string> {
  if (response.status === 429) {
    return 'Príliš veľa pokusov. Skús to o chvíľu.';
  }
  const text = (await response.text().catch(() => '')).trim();
  if (!text) {
    return `Požiadavka zlyhala (${response.status}).`;
  }
  try {
    const parsed: unknown = JSON.parse(text);
    if (typeof parsed === 'string') {
      return parsed;
    }
  } catch {
    // not JSON, use the raw text
  }
  return text;
}
