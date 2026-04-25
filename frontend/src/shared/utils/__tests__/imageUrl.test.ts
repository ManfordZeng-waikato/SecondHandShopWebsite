import { env } from '../../config/env';
import { buildImageUrl } from '../imageUrl';

describe('buildImageUrl', () => {
  const originalImageBaseUrl = env.imageBaseUrl;

  afterEach(() => {
    env.imageBaseUrl = originalImageBaseUrl;
  });

  it('returns the fallback image for null or empty object keys', () => {
    env.imageBaseUrl = 'https://images.example.test';

    expect(buildImageUrl(null)).toBe('https://picsum.photos/seed/fallback/800/500');
    expect(buildImageUrl(undefined)).toBe('https://picsum.photos/seed/fallback/800/500');
    expect(buildImageUrl('')).toBe('https://picsum.photos/seed/fallback/800/500');
  });

  it('joins the configured image base URL with a relative object key', () => {
    env.imageBaseUrl = 'https://images.example.test/';

    expect(buildImageUrl('products/bag.jpg')).toBe('https://images.example.test/products/bag.jpg');
  });

  it('returns an absolute image URL unchanged', () => {
    env.imageBaseUrl = 'https://images.example.test';

    expect(buildImageUrl('https://cdn.example.test/products/bag.jpg')).toBe(
      'https://cdn.example.test/products/bag.jpg',
    );
  });

  it('falls back when the image base URL is not configured', () => {
    env.imageBaseUrl = '';

    expect(buildImageUrl('products/bag.jpg')).toBe('https://picsum.photos/seed/fallback/800/500');
  });
});
