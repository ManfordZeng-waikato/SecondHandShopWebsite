const tokenKey = 'shs.admin.token';
const expiresAtKey = 'shs.admin.expiresAt';

export function saveAdminSession(token: string, expiresAt: string): void {
  sessionStorage.setItem(tokenKey, token);
  sessionStorage.setItem(expiresAtKey, expiresAt);
}

export function getAdminToken(): string | null {
  const token = sessionStorage.getItem(tokenKey);
  const expiresAt = sessionStorage.getItem(expiresAtKey);

  if (!token || !expiresAt) return null;

  if (new Date(expiresAt) <= new Date()) {
    clearAdminSession();
    return null;
  }

  return token;
}

export function isAdminLoggedIn(): boolean {
  return getAdminToken() !== null;
}

export function clearAdminSession(): void {
  sessionStorage.removeItem(tokenKey);
  sessionStorage.removeItem(expiresAtKey);
}
