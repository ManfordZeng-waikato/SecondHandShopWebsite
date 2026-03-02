import axios from 'axios';
import { env } from '../config/env';
import { clearAdminSession, getAdminToken } from '../../features/admin/auth/adminSession';

export const httpClient = axios.create({
  baseURL: env.apiBaseUrl,
  timeout: 10000,
});

httpClient.interceptors.request.use((config) => {
  const token = getAdminToken();
  if (token && config.url?.includes('/api/admin')) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

httpClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (axios.isAxiosError(error) && error.response?.status === 401) {
      const url = error.config?.url ?? '';
      if (url.includes('/api/admin') && !url.includes('/auth/login')) {
        clearAdminSession();
        window.location.href = '/admin/login';
      }
    }
    return Promise.reject(error);
  },
);
