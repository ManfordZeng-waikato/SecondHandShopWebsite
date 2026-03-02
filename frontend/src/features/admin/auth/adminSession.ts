const expiresAtKey = 'shs.admin.expiresAt';

export function saveAdminSession(expiresAt: string): void {
  sessionStorage.setItem(expiresAtKey, expiresAt);
}

export function isAdminLoggedIn(): boolean {
  const expiresAt = sessionStorage.getItem(expiresAtKey);
  if (!expiresAt) return false;

  if (new Date(expiresAt) <= new Date()) {
    clearAdminSession();
    return false;
  }

  return true;
}

export function clearAdminSession(): void {
  sessionStorage.removeItem(expiresAtKey);
}
