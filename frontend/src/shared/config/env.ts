const fallbackApiBaseUrl = 'https://localhost:7266';

export const env = {
  apiBaseUrl: import.meta.env.VITE_API_BASE_URL ?? fallbackApiBaseUrl,
  useMockApi: (import.meta.env.VITE_USE_MOCK_API ?? 'true').toLowerCase() === 'true',
};
