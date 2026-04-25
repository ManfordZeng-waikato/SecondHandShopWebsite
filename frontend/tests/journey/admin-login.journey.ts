import { expect, test } from '@playwright/test';
import { registerJourneyApi } from './server';

test.describe('admin login journey', () => {
  test('admin logs in and lands in the product workspace', async ({ page }) => {
    await registerJourneyApi(page);

    await page.goto('/lord/login');
    await page.getByLabel('Username').fill('journey-admin');
    await page.getByLabel('Password').fill('correct-password');
    await page.getByRole('button', { name: 'Sign in' }).click();

    await expect(page).toHaveURL(/\/lord\/products$/);
    await expect(page.getByRole('heading', { name: /product management/i })).toBeVisible();
    await expect(page.getByText('Vintage Oak Chair')).toBeVisible();
  });
});
