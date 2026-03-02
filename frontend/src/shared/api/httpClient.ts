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
      if (url.includes('/api/lord') && !url.includes('/auth/login')) {
        clearAdminSession();
        window.location.href = '/lord/login';
      }
    }
    return Promise.reject(error);
  },
);
