/* eslint-disable react-refresh/only-export-components */
import { useEffect, type PropsWithChildren } from 'react';
import { useSyncExternalStore } from 'react';
import { useNavigate } from 'react-router-dom';
import { refreshAdminSession } from './adminAuth';
import { setAdminUnauthorizedNavigator } from './adminUnauthorizedNavigation';
import {
  getAdminAuthSnapshot,
  getServerAdminAuthSnapshot,
  initializeAdminAuth,
  subscribeAdminAuth,
} from './adminAuthStore';

/** Interval slightly below default access token lifetime so idle tabs still renew without hitting 401. */
const SESSION_REFRESH_INTERVAL_MS = 8 * 60 * 1000;

export function AdminAuthProvider({ children }: PropsWithChildren) {
  const navigate = useNavigate();

  useEffect(() => {
    void initializeAdminAuth();
  }, []);

  useEffect(() => {
    setAdminUnauthorizedNavigator(() => {
      navigate('/lord/login', { replace: true });
    });
    return () => setAdminUnauthorizedNavigator(null);
  }, [navigate]);

  useEffect(() => {
    const id = window.setInterval(() => {
      const s = getAdminAuthSnapshot();
      if (s.isAuthInitialized && s.isAuthenticated && !s.mustChangePassword) {
        void refreshAdminSession();
      }
    }, SESSION_REFRESH_INTERVAL_MS);
    return () => window.clearInterval(id);
  }, []);

  return <>{children}</>;
}

/** Subscribe to admin auth snapshot (initialized via /me on app load). */
export function useAdminAuth() {
  return useSyncExternalStore(subscribeAdminAuth, getAdminAuthSnapshot, getServerAdminAuthSnapshot);
}
