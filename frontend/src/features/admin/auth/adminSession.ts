const expiresAtKey = 'shs.admin.expiresAt';
const pwdChangeKey = 'shs.admin.requiresPasswordChange';

export function saveAdminSession(
  expiresAt: string,
  options?: { requiresPasswordChange?: boolean },
): void {
  sessionStorage.setItem(expiresAtKey, expiresAt);
  if (options?.requiresPasswordChange) {
    sessionStorage.setItem(pwdChangeKey, '1');
  } else {
    sessionStorage.removeItem(pwdChangeKey);
  }
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

export function adminRequiresPasswordChange(): boolean {
  return sessionStorage.getItem(pwdChangeKey) === '1';
}

export function clearAdminSession(): void {
  sessionStorage.removeItem(expiresAtKey);
  sessionStorage.removeItem(pwdChangeKey);
}
