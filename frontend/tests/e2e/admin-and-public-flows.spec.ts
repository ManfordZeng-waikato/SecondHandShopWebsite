import { expect, test } from '@playwright/test';

const adminUser = process.env.PLAYWRIGHT_ADMIN_USER;
const adminPassword = process.env.PLAYWRIGHT_ADMIN_PASSWORD;
const inquiryProductId = process.env.PLAYWRIGHT_INQUIRY_PRODUCT_ID;
const inquiryProductSlug = process.env.PLAYWRIGHT_INQUIRY_PRODUCT_SLUG;

test.describe('critical storefront and admin flows', () => {
  test('admin can open customers and analytics workspaces after login', async ({ page }) => {
    test.skip(!adminUser || !adminPassword, 'Admin credentials not available — is the backend running with dev seed?');

    await page.goto('/lord/login');
    await page.getByLabel('Username').fill(adminUser!);
    await page.getByLabel('Password').fill(adminPassword!);
    await page.getByRole('button', { name: 'Sign in' }).click();

    await expect(page).toHaveURL(/\/lord\/products$/);

    await page.getByRole('link', { name: 'Customers' }).click();
    await expect(page).toHaveURL(/\/lord\/customers$/);
    await expect(page.getByRole('heading', { name: 'Customer Management' })).toBeVisible();

    await page.getByRole('link', { name: 'Analytics' }).click();
    await expect(page).toHaveURL(/\/lord\/analytics$/);
    await expect(page.getByRole('heading', { name: 'Sales & Demand Insights' })).toBeVisible();
  });

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

  test('public product detail page exposes inquiry entrypoint for an available item', async ({ page }) => {
    test.skip(!inquiryProductSlug, 'No product slug available from the public API.');

    await page.goto(`/products/${inquiryProductSlug}`);

    await expect(page.getByRole('link', { name: 'Send Inquiry' })).toBeVisible();
    await page.getByRole('link', { name: 'Send Inquiry' }).click();

    await expect(page).toHaveURL(new RegExp(`/products/.+/inquiry$`));
    await expect(page.getByText('Interested in this item?')).toBeVisible();
  });

  test('public inquiry page enforces contact validation before Turnstile execution', async ({ page }) => {
    test.skip(!inquiryProductId, 'No available product found — is the backend running with products in the database?');

    await page.goto(`/products/${inquiryProductId}/inquiry`);
    await page.getByRole('button', { name: /send inquiry|submit inquiry/i }).click();

    await expect(page.getByText(/please provide at least one contact method/i)).toBeVisible();
  });
});
