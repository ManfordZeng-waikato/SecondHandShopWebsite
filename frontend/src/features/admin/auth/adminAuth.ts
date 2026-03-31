import { env } from '../../../shared/config/env';

/**
 * Single session snapshot for admin UI gating. HttpOnly JWT lives in a cookie; this object must stay
 * consistent with the last login response. If it is missing or malformed, we treat the client as logged out
 * and clear the server cookie to avoid a cookie+UI mismatch.
 */
const AUTH_KEY = 'shs.admin.auth';
const LEGACY_EXPIRES_KEY = 'shs.admin.expiresAt';
const LEGACY_PWD_KEY = 'shs.admin.requiresPasswordChange';

export interface AdminAuthSnapshot {
  expiresAt: string;
  requiresPasswordChange: boolean;
}

function clearLegacyKeys(): void {
  sessionStorage.removeItem(LEGACY_EXPIRES_KEY);
  sessionStorage.removeItem(LEGACY_PWD_KEY);
}

function tryParseUnified(raw: string | null): AdminAuthSnapshot | null {
  if (!raw) return null;
  try {
    const o = JSON.parse(raw) as Record<string, unknown>;
    const expiresAt = o.expiresAt;
    const requiresPasswordChange = o.requiresPasswordChange;
    if (typeof expiresAt !== 'string' || expiresAt.length === 0) return null;
    if (typeof requiresPasswordChange !== 'boolean') return null;
    return { expiresAt, requiresPasswordChange };
  } catch {
    return null;
  }
}

/** Read current snapshot, migrate legacy two-key storage once, or null if untrusted / incomplete. */
export function getAuth(): AdminAuthSnapshot | null {
  const unified = tryParseUnified(sessionStorage.getItem(AUTH_KEY));
  if (unified) return unified;

  const exp = sessionStorage.getItem(LEGACY_EXPIRES_KEY);
  const pwdFlag = sessionStorage.getItem(LEGACY_PWD_KEY);

  if (exp && pwdFlag !== null) {
    const requiresPasswordChange = pwdFlag === '1';
    setAuth(exp, requiresPasswordChange);
    return { expiresAt: exp, requiresPasswordChange };
  }

  if (exp || pwdFlag !== null) {
    clearLegacyKeys();
    sessionStorage.removeItem(AUTH_KEY);
    return null;
  }

  return null;
}

export function setAuth(expiresAt: string, requiresPasswordChange: boolean): void {
  clearLegacyKeys();
  const snap: AdminAuthSnapshot = { expiresAt, requiresPasswordChange };
  sessionStorage.setItem(AUTH_KEY, JSON.stringify(snap));
}

export function clearAuth(): void {
  clearLegacyKeys();
  sessionStorage.removeItem(AUTH_KEY);
}

export function isAuthenticated(): boolean {
  const s = getAuth();
  if (!s) return false;
  if (new Date(s.expiresAt) <= new Date()) {
    clearAuth();
    return false;
  }
  return true;
}

export function getMustChangePassword(): boolean {
  const s = getAuth();
  if (!s) return false;
  if (new Date(s.expiresAt) <= new Date()) return false;
  return s.requiresPasswordChange;
}

/** Clears HttpOnly admin cookie without using the shared axios client (avoids import cycles). */
export async function revokeLordCookie(): Promise<void> {
  try {
    await fetch(`${env.apiBaseUrl}/api/lord/auth/logout`, {
      method: 'POST',
      credentials: 'include',
    });
  } catch {
    /* ignore network errors */
  }
}
