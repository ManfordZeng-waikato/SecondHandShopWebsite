/**
 * Barrel for admin auth: persistence hint + server sync live in adminAuthStore.
 * Prefer useAdminAuth() in components; use these helpers from non-React modules (e.g. axios interceptors).
 */
import { env } from '../../../shared/config/env';
import {
  getAdminAuthSnapshot as readSnapshot,
  markLordUnauthorizedAndClear,
  setSessionHintExpiresAt,
  type AdminCurrentUser as CurrentUserType,
} from './adminAuthStore';

export {
  initializeAdminAuth,
  getAdminAuthSnapshot,
  markLordUnauthorizedAndClear,
  setSessionHintExpiresAt,
  clearSessionHint,
  type AdminAuthPublicState,
  type AdminCurrentUser,
  type AdminMeDto,
} from './adminAuthStore';

export { AdminAuthProvider, useAdminAuth } from './AdminAuthProvider';

/** True only after bootstrap finished and /me returned 200. */
export function isAuthInitialized(): boolean {
  return readSnapshot().isAuthInitialized;
}

/** True when bootstrap done and server considers the admin signed in. */
export function isAuthenticated(): boolean {
  const s = readSnapshot();
  return s.isAuthInitialized && s.isAuthenticated;
}

export function getMustChangePassword(): boolean {
  return readSnapshot().mustChangePassword;
}

export function getCurrentUser(): CurrentUserType | null {
  return readSnapshot().currentUser;
}

/** Access token is HttpOnly; not readable in the browser. */
export function getAdminAccessToken(): null {
  return null;
}

export async function revokeLordCookie(): Promise<void> {
  try {
    await fetch(`${env.apiBaseUrl}/api/lord/auth/logout`, {
      method: 'POST',
      credentials: 'include',
    });
  } catch {
    /* ignore */
  }
}

/** Renews HttpOnly JWT + cookie; call periodically while signed in so idle forms do not lose the session. */
export async function refreshAdminSession(): Promise<void> {
  try {
    const res = await fetch(`${env.apiBaseUrl}/api/lord/auth/refresh`, {
      method: 'POST',
      credentials: 'include',
    });
    if (!res.ok) {
      return;
    }
    const data = (await res.json()) as { expiresAt?: string };
    if (typeof data.expiresAt === 'string') {
      setSessionHintExpiresAt(data.expiresAt);
    }
  } catch {
    /* ignore */
  }
}

/** Call after successful login with API expiresAt (JWT clock); then await initializeAdminAuth(). */
export function persistSessionAfterLogin(expiresAt: string): void {
  setSessionHintExpiresAt(expiresAt);
}

/** Full client-side logout of admin state + session hint (HttpOnly cookie cleared separately). */
export function clearAuth(): void {
  markLordUnauthorizedAndClear();
}
