import axios from 'axios';
import { env } from '../config/env';
import { clearAdminSession } from '../../features/admin/auth/adminSession';

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
        clearAdminSession();
        window.location.href = '/lord/login';
      }
    }
    return Promise.reject(error);
  },
);
