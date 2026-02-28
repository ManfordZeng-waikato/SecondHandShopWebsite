import { env } from '../config/env';

const FALLBACK_IMAGE = 'https://picsum.photos/seed/fallback/800/500';

export function buildImageUrl(objectKey: string | undefined | null): string {
  if (!objectKey) return FALLBACK_IMAGE;
  if (!env.imageBaseUrl) return FALLBACK_IMAGE;
  return `${env.imageBaseUrl.replace(/\/+$/, '')}/${objectKey}`;
}
