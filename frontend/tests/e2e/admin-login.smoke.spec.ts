import { expect, test } from '@playwright/test';
import { requireSmokeBackend } from './smoke-support';

const adminUser = process.env.PLAYWRIGHT_ADMIN_USER;
const adminPassword = process.env.PLAYWRIGHT_ADMIN_PASSWORD;

function requireEnv(name: string, value: string | undefined): string {
  if (!value) {
    throw new Error(`${name} must be set for smoke E2E tests.`);
  }
  return value;
}

test.describe('admin smoke E2E', () => {
  test.beforeAll(async () => {
    await requireSmokeBackend();
  });

  test('admin login reaches the product workspace against the real backend', async ({ page }) => {
    await page.goto('/lord/login');
    await page.getByLabel('Username').fill(requireEnv('PLAYWRIGHT_ADMIN_USER', adminUser));
    await page.getByLabel('Password').fill(requireEnv('PLAYWRIGHT_ADMIN_PASSWORD', adminPassword));
    await page.getByRole('button', { name: 'Sign in' }).click();

    await expect(page).toHaveURL(/\/lord\/products$/);
    await expect(page.getByRole('heading', { name: /product management/i })).toBeVisible();
  });
});
