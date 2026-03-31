import { useEffect, type PropsWithChildren } from 'react';
import { useSyncExternalStore } from 'react';
import {
  getAdminAuthSnapshot,
  getServerAdminAuthSnapshot,
  initializeAdminAuth,
  subscribeAdminAuth,
} from './adminAuthStore';

export function AdminAuthProvider({ children }: PropsWithChildren) {
  useEffect(() => {
    void initializeAdminAuth();
  }, []);

  return <>{children}</>;
}

/** Subscribe to admin auth snapshot (initialized via /me on app load). */
export function useAdminAuth() {
  return useSyncExternalStore(subscribeAdminAuth, getAdminAuthSnapshot, getServerAdminAuthSnapshot);
}
