import axios from 'axios';
import { env } from '../config/env';

export const httpClient = axios.create({
  baseURL: env.apiBaseUrl,
  timeout: 10000,
});
