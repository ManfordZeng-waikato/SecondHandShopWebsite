import { expect, test, type APIRequestContext } from '@playwright/test';

const adminUser = process.env.PLAYWRIGHT_ADMIN_USER;
const adminPassword = process.env.PLAYWRIGHT_ADMIN_PASSWORD;
const inquiryProductId = process.env.PLAYWRIGHT_INQUIRY_PRODUCT_ID;
const inquiryProductSlug = process.env.PLAYWRIGHT_INQUIRY_PRODUCT_SLUG;
const apiBase = process.env.PLAYWRIGHT_API_BASE_URL ?? 'https://localhost:7266';

function requireEnv(value: string | undefined): string {
  if (!value) {
    throw new Error('Expected environment variable to be defined.');
  }
  return value;
}

async function ensureApiAvailable(request: APIRequestContext): Promise<boolean> {
  try {
    const response = await request.get(`${apiBase}/api/products/search?pageSize=1`, {
      failOnStatusCode: false,
      timeout: 5000,
      ignoreHTTPSErrors: true,
    });
    return response.ok();
  } catch {
    return false;
  }
}

async function canLoginWithSeedCredentials(request: APIRequestContext): Promise<boolean> {
  if (!adminUser || !adminPassword) {
    return false;
  }

  try {
    const response = await request.post(`${apiBase}/api/lord/auth/login`, {
      data: {
        userName: adminUser,
        password: adminPassword,
      },
      failOnStatusCode: false,
      timeout: 5000,
      ignoreHTTPSErrors: true,
    });
    return response.ok();
  } catch {
    return false;
  }
}

test.describe('critical storefront and admin flows', () => {
  test('admin can open customers and analytics workspaces after login', async ({ page, request }) => {
    test.skip(!adminUser || !adminPassword, 'Admin credentials not available - is the backend running with dev seed?');
    test.skip(!(await ensureApiAvailable(request)), 'Backend API is not reachable - start PostgreSQL and the ASP.NET Core API first.');
    test.skip(!(await canLoginWithSeedCredentials(request)), 'Configured admin seed credentials are not valid in the current database.');

    await page.goto('/lord/login');
    await page.getByLabel('Username').fill(requireEnv(adminUser));
    await page.getByLabel('Password').fill(requireEnv(adminPassword));
    await page.getByRole('button', { name: 'Sign in' }).click();

    await expect(page).toHaveURL(/\/lord\/products$/);

    await page.getByRole('link', { name: 'Customers' }).click();
    await expect(page).toHaveURL(/\/lord\/customers$/);
    await expect(page.getByRole('heading', { name: 'Customer Management' })).toBeVisible();

    await page.getByRole('link', { name: 'Analytics' }).click();
    await expect(page).toHaveURL(/\/lord\/analytics$/);
    await expect(page.getByRole('heading', { name: 'Sales & Demand Insights' })).toBeVisible();
  });

  test('admin login redirects to product workspace', async ({ page, request }) => {
    test.skip(!adminUser || !adminPassword, 'Admin credentials not available - is the backend running with dev seed?');
    test.skip(!(await ensureApiAvailable(request)), 'Backend API is not reachable - start PostgreSQL and the ASP.NET Core API first.');
    test.skip(!(await canLoginWithSeedCredentials(request)), 'Configured admin seed credentials are not valid in the current database.');

    await page.goto('/lord/login');
    await page.getByLabel('Username').fill(requireEnv(adminUser));
    await page.getByLabel('Password').fill(requireEnv(adminPassword));
    await page.getByRole('button', { name: 'Sign in' }).click();

    await expect(page).toHaveURL(/\/lord\/products$/);
    await expect(page.getByText('Products', { exact: false })).toBeVisible();
  });

  test('public catalog page renders browseable product grid', async ({ page }) => {
    await page.goto('/products');

    await expect(page.getByText('All Products')).toBeVisible();
    await expect(page.getByRole('textbox', { name: /search products/i })).toBeVisible();
  });

  test('public product detail page exposes inquiry entrypoint for an available item', async ({ page, request }) => {
    test.skip(!(await ensureApiAvailable(request)), 'Backend API is not reachable - start PostgreSQL and the ASP.NET Core API first.');
    test.skip(!inquiryProductSlug, 'No product slug available from the public API.');

    await page.goto(`/products/${inquiryProductSlug}`);

    await expect(page.getByRole('link', { name: 'Send Inquiry' })).toBeVisible();
    await page.getByRole('link', { name: 'Send Inquiry' }).click();

    await expect(page).toHaveURL(new RegExp('/products/.+/inquiry$'));
    await expect(page.getByText('Interested in this item?')).toBeVisible();
  });

  test('public inquiry page enforces contact validation before Turnstile execution', async ({ page, request }) => {
    test.skip(!(await ensureApiAvailable(request)), 'Backend API is not reachable - start PostgreSQL and the ASP.NET Core API first.');
    test.skip(!inquiryProductId, 'No available product found - is the backend running with products in the database?');

    await page.goto(`/products/${inquiryProductId}/inquiry`);
    await page.getByLabel('Message').fill('Interested in this item.');
    await page.getByRole('button', { name: /send inquiry|submit inquiry/i }).click();

    await expect(page.getByText(/please provide at least one contact method/i)).toBeVisible();
  });
});
