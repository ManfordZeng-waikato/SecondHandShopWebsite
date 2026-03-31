import { env } from '../../../shared/config/env';

/**
 * HttpOnly JWT cannot be read in JS. We keep a lightweight session hint (expiry from login response) for UX only;
 * authorization decisions use GET /me (database-backed) via initializeAdminAuth.
 */
const SESSION_HINT_KEY = 'shs.admin.sessionHint';
const LEGACY_AUTH_KEY = 'shs.admin.auth';
const LEGACY_EXPIRES_KEY = 'shs.admin.expiresAt';

/** API shape for GET /api/lord/auth/me (camelCase JSON). */
export interface AdminMeDto {
  isAuthenticated: boolean;
  userId: string;
  userName: string;
  email: string;
  role: string;
  mustChangePassword: boolean;
}

export interface AdminCurrentUser {
  userId: string;
  userName: string;
  email: string;
  role: string;
}

export interface AdminAuthPublicState {
  isAuthInitialized: boolean;
  isAuthenticated: boolean;
  mustChangePassword: boolean;
  currentUser: AdminCurrentUser | null;
}

const bootstrapping: AdminAuthPublicState = {
  isAuthInitialized: false,
  isAuthenticated: false,
  mustChangePassword: false,
  currentUser: null,
};

const loggedOut: AdminAuthPublicState = {
  isAuthInitialized: true,
  isAuthenticated: false,
  mustChangePassword: false,
  currentUser: null,
};

let snapshot: AdminAuthPublicState = bootstrapping;
const listeners = new Set<() => void>();
let initSeq = 0;

function emit() {
  listeners.forEach((l) => l());
}

export function subscribeAdminAuth(listener: () => void) {
  listeners.add(listener);
  return () => listeners.delete(listener);
}

export function getAdminAuthSnapshot(): AdminAuthPublicState {
  return snapshot;
}

export function getServerAdminAuthSnapshot(): AdminAuthPublicState {
  return bootstrapping;
}

export function setSessionHintExpiresAt(expiresAt: string): void {
  sessionStorage.setItem(SESSION_HINT_KEY, JSON.stringify({ expiresAt }));
}

export function clearSessionHint(): void {
  sessionStorage.removeItem(SESSION_HINT_KEY);
}

/** Clears in-memory auth used by guards and removes legacy keys from older builds. */
export function markLordUnauthorizedAndClear(): void {
  clearSessionHint();
  sessionStorage.removeItem(LEGACY_AUTH_KEY);
  sessionStorage.removeItem(LEGACY_EXPIRES_KEY);
  sessionStorage.removeItem('shs.admin.requiresPasswordChange');
  snapshot = { ...loggedOut };
  emit();
}

function migrateLegacyStorageToHint(): void {
  const unified = sessionStorage.getItem(LEGACY_AUTH_KEY);
  if (unified) {
    try {
      const o = JSON.parse(unified) as { expiresAt?: string };
      if (typeof o.expiresAt === 'string') {
        setSessionHintExpiresAt(o.expiresAt);
      }
    } catch {
      /* ignore */
    }
    sessionStorage.removeItem(LEGACY_AUTH_KEY);
  }
  const legacyExp = sessionStorage.getItem(LEGACY_EXPIRES_KEY);
  if (legacyExp && !sessionStorage.getItem(SESSION_HINT_KEY)) {
    setSessionHintExpiresAt(legacyExp);
    sessionStorage.removeItem(LEGACY_EXPIRES_KEY);
    sessionStorage.removeItem('shs.admin.requiresPasswordChange');
  }
}

/**
 * Calls GET /me with credentials. Server + DB are the source of truth for mustChangePassword and account status.
 */
export async function initializeAdminAuth(): Promise<void> {
  const seq = ++initSeq;
  migrateLegacyStorageToHint();
  snapshot = { ...bootstrapping };
  emit();

  try {
    const res = await fetch(`${env.apiBaseUrl}/api/lord/auth/me`, {
      method: 'GET',
      credentials: 'include',
    });

    if (seq !== initSeq) return;

    if (res.status === 401) {
      markLordUnauthorizedAndClear();
      return;
    }

    if (!res.ok) {
      markLordUnauthorizedAndClear();
      return;
    }

    const data = (await res.json()) as AdminMeDto;

    if (!data.isAuthenticated || typeof data.userId !== 'string') {
      markLordUnauthorizedAndClear();
      return;
    }

    snapshot = {
      isAuthInitialized: true,
      isAuthenticated: true,
      mustChangePassword: data.mustChangePassword === true,
      currentUser: {
        userId: data.userId,
        userName: data.userName,
        email: data.email,
        role: data.role,
      },
    };
    emit();
  } catch {
    if (seq !== initSeq) return;
    markLordUnauthorizedAndClear();
  }
}
