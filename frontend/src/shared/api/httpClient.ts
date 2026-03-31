import axios from 'axios';
import { env } from '../config/env';
import { clearAuth, revokeLordCookie } from '../../features/admin/auth/adminAuth';

export const httpClient = axios.create({
  baseURL: env.apiBaseUrl,
  timeout: 10000,
  withCredentials: true,
});

httpClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (axios.isAxiosError(error) && error.response?.status === 401) {
      const url = error.config?.url ?? '';
      const isLogin = url.includes('/auth/login');
      const isChangeInitialPassword = url.includes('/auth/change-initial-password');
      if (url.includes('/api/lord') && !isLogin && !isChangeInitialPassword) {
        clearAuth();
        void revokeLordCookie().finally(() => {
          window.location.href = '/lord/login';
        });
      }
    }
    return Promise.reject(error);
  },
);
