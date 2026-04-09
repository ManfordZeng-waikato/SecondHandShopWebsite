import axios, { type AxiosResponse } from 'axios';
import { env } from '../config/env';
import {
  clearAuth,
  revokeLordCookie,
  setSessionHintExpiresAt,
} from '../../features/admin/auth/adminAuth';
import { notifyAdminUnauthorized } from '../../features/admin/auth/adminUnauthorizedNavigation';

export const httpClient = axios.create({
  baseURL: env.apiBaseUrl,
  timeout: 10000,
  withCredentials: true,
});

function applySessionExpiresHeader(response: AxiosResponse): void {
  const h = response.headers;
  const raw =
    (typeof h.get === 'function' ? h.get('x-admin-session-expires-at') : undefined) ??
    (typeof h.get === 'function' ? h.get('X-Admin-Session-Expires-At') : undefined);
  if (typeof raw === 'string' && raw.length > 0) {
    setSessionHintExpiresAt(raw);
  }
}

httpClient.interceptors.response.use(
  (response) => {
    applySessionExpiresHeader(response);
    return response;
  },
  (error) => {
    if (axios.isAxiosError(error) && error.response) {
      applySessionExpiresHeader(error.response);
    }
    if (axios.isAxiosError(error) && error.response?.status === 401) {
      const url = error.config?.url ?? '';
      const isLogin = url.includes('/auth/login');
      const isChangeInitialPassword = url.includes('/auth/change-initial-password');
      const isMe = url.includes('/auth/me');
      const isRefresh = url.includes('/auth/refresh');
      if (url.includes('/api/lord') && !isLogin && !isChangeInitialPassword && !isMe && !isRefresh) {
        clearAuth();
        void revokeLordCookie().finally(() => {
          notifyAdminUnauthorized();
        });
      }
    }
    return Promise.reject(error);
  },
);
