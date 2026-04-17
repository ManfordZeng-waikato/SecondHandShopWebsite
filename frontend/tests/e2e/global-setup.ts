import { request } from '@playwright/test';

/**
 * Playwright globalSetup — runs once before all tests.
 * Discovers a valid product ID from the public API so E2E inquiry tests
 * don't need a hardcoded PLAYWRIGHT_INQUIRY_PRODUCT_ID.
 */
export default async function globalSetup() {
  const apiBase =
    process.env.PLAYWRIGHT_API_BASE_URL ?? 'https://localhost:7266';

  const context = await request.newContext({
    baseURL: apiBase,
    ignoreHTTPSErrors: true,
  });

  try {
    const response = await context.get('/api/products/search?pageSize=1&status=Available');
    if (response.ok()) {
      process.env.PLAYWRIGHT_API_AVAILABLE = 'true';
      const body = await response.json();
      const firstProduct = body.items?.[0];
      if (firstProduct?.id) {
        process.env.PLAYWRIGHT_INQUIRY_PRODUCT_ID = firstProduct.id;
      }
      if (firstProduct?.slug) {
        process.env.PLAYWRIGHT_INQUIRY_PRODUCT_SLUG = firstProduct.slug;
      }
    }
  } catch {
    // API not reachable — tests that need the product ID will skip gracefully
  } finally {
    await context.dispose();
  }
}
