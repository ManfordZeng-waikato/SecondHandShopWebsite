import { expect, test } from '@playwright/test';

const adminUser = process.env.PLAYWRIGHT_ADMIN_USER;
const adminPassword = process.env.PLAYWRIGHT_ADMIN_PASSWORD;
const inquiryProductId = process.env.PLAYWRIGHT_INQUIRY_PRODUCT_ID;

test.describe('critical storefront and admin flows', () => {
  test('admin login redirects to product workspace', async ({ page }) => {
    test.skip(!adminUser || !adminPassword, 'Admin credentials not available — is the backend running with dev seed?');

    await page.goto('/lord/login');
    await page.getByLabel('Username').fill(adminUser!);
    await page.getByLabel('Password').fill(adminPassword!);
    await page.getByRole('button', { name: 'Sign in' }).click();

    await expect(page).toHaveURL(/\/lord\/products$/);
    await expect(page.getByText('Products', { exact: false })).toBeVisible();
  });

  test('public catalog page renders browseable product grid', async ({ page }) => {
    await page.goto('/products');

    await expect(page.getByText('All Products')).toBeVisible();
    await expect(page.getByRole('textbox', { name: /search products/i })).toBeVisible();
  });

  test('public inquiry page enforces contact validation before Turnstile execution', async ({ page }) => {
    test.skip(!inquiryProductId, 'No available product found — is the backend running with products in the database?');

    await page.goto(`/products/${inquiryProductId}/inquiry`);
    await page.getByRole('button', { name: /send inquiry|submit inquiry/i }).click();

    await expect(page.getByText(/please provide at least one contact method/i)).toBeVisible();
  });
});
