import { expect, test } from '@playwright/test';
import { requireSmokeBackend, type SmokeBackendState } from './smoke-support';

let backend: SmokeBackendState;

test.describe('public catalog smoke E2E', () => {
  test.beforeAll(async () => {
    backend = await requireSmokeBackend();
  });

  test('catalog and product detail load against the real backend', async ({ page }) => {
    await page.goto('/products');

    await expect(page.getByText('All Products')).toBeVisible();
    await expect(page.getByText(/items? found/i)).toBeVisible();

    await page.goto(`/products/${backend.productSlug}`);
    await expect(page.getByRole('link', { name: 'Send Inquiry' })).toBeVisible();
  });
});
