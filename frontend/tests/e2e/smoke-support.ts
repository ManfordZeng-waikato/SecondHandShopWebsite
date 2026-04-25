import { request } from '@playwright/test';

const apiBase = process.env.PLAYWRIGHT_API_BASE_URL ?? 'https://localhost:7266';

export interface SmokeBackendState {
  productId: string;
  productSlug: string;
}

export async function requireSmokeBackend(): Promise<SmokeBackendState> {
  const context = await request.newContext({
    baseURL: apiBase,
    ignoreHTTPSErrors: true,
  });

  try {
    const response = await context.get('/api/products/search?pageSize=1&status=Available', {
      failOnStatusCode: false,
      timeout: 5000,
    });
    if (!response.ok()) {
      throw new Error(`Backend API smoke discovery failed with status ${response.status()}.`);
    }

    const body = await response.json();
    const firstProduct = body.items?.[0];
    if (!firstProduct?.id || !firstProduct?.slug) {
      throw new Error('Backend API smoke discovery did not return an available product.');
    }

    return {
      productId: firstProduct.id,
      productSlug: firstProduct.slug,
    };
  } catch (error) {
    throw new Error(
      `Backend API is required for smoke E2E tests. Start the API/Postgres stack before running the smoke project. ${String(error)}`,
    );
  } finally {
    await context.dispose();
  }
}
