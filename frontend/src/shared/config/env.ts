const fallbackApiBaseUrl = 'https://localhost:7266';

export const env = {
  apiBaseUrl: import.meta.env.VITE_API_BASE_URL ?? fallbackApiBaseUrl,
  imageBaseUrl: import.meta.env.VITE_IMAGE_BASE_URL ?? '',
};
