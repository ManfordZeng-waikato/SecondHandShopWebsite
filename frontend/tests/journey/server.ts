import type { Page, Route } from '@playwright/test';

const now = '2026-04-25T00:00:00Z';

export const journeyProduct = {
  id: 'product-1',
  title: 'Vintage Oak Chair',
  slug: 'vintage-oak-chair',
  description: 'A sturdy oak chair with a warm finish.',
  price: 125,
  status: 'Available',
  categoryId: 'category-1',
  categoryName: 'Furniture',
  images: [
    {
      id: 'image-1',
      objectKey: 'products/vintage-oak-chair.jpg',
      displayUrl: 'https://images.example.test/products/vintage-oak-chair.jpg',
      altText: 'Vintage Oak Chair',
      sortOrder: 0,
      isPrimary: true,
    },
  ],
  createdAt: now,
  updatedAt: now,
};

export async function registerJourneyApi(page: Page) {
  await page.route('**/api/lord/auth/me', async (route) => {
    const cookie = route.request().headers().cookie ?? '';
    if (!cookie.includes('journey-admin=1')) {
      await route.fulfill({ status: 401, json: { message: 'Unauthorized' } });
      return;
    }

    await route.fulfill({
      status: 200,
      json: {
        isAuthenticated: true,
        userId: 'admin-1',
        userName: 'journey-admin',
        email: 'admin@example.test',
        role: 'Admin',
        mustChangePassword: false,
      },
    });
  });

  await page.route('**/api/lord/auth/login', async (route) => {
    const body = route.request().postDataJSON() as { userName?: string; password?: string };
    if (body.userName !== 'journey-admin' || body.password !== 'correct-password') {
      await route.fulfill({ status: 401, json: { message: 'Invalid credentials' } });
      return;
    }

    await route.fulfill({
      status: 200,
      headers: {
        'set-cookie': 'journey-admin=1; Path=/; SameSite=Lax',
      },
      json: {
        expiresAt: '2026-04-25T01:00:00Z',
        requiresPasswordChange: false,
      },
    });
  });

  await page.route('**/api/lord/auth/logout', async (route) => {
    await route.fulfill({ status: 204 });
  });

  await page.route('**/api/lord/auth/refresh', async (route) => {
    await route.fulfill({ status: 200, json: { expiresAt: '2026-04-25T01:00:00Z' } });
  });

  await page.route('**/api/lord/products**', async (route) => {
    await route.fulfill({
      status: 200,
      json: {
        items: [
          {
            id: journeyProduct.id,
            title: journeyProduct.title,
            slug: journeyProduct.slug,
            price: journeyProduct.price,
            status: journeyProduct.status,
            categoryName: journeyProduct.categoryName,
            imageCount: 1,
            primaryImageUrl: journeyProduct.images[0].displayUrl,
            isFeatured: false,
            featuredSortOrder: null,
            createdAt: now,
            updatedAt: now,
          },
        ],
        page: 1,
        pageSize: 20,
        totalCount: 1,
        totalPages: 1,
        hasNextPage: false,
        hasPreviousPage: false,
        isFallback: false,
      },
    });
  });

  await page.route('**/api/categories', async (route) => {
    await route.fulfill({
      status: 200,
      json: [
        {
          id: 'category-1',
          name: 'Furniture',
          slug: 'furniture',
          sortOrder: 0,
          isActive: true,
        },
      ],
    });
  });

  await page.route('**/api/products/search**', async (route) => {
    await route.fulfill({
      status: 200,
      json: {
        items: [
          {
            id: journeyProduct.id,
            title: journeyProduct.title,
            slug: journeyProduct.slug,
            price: journeyProduct.price,
            coverImageUrl: journeyProduct.images[0].displayUrl,
            categoryName: journeyProduct.categoryName,
            status: journeyProduct.status,
            createdAt: now,
          },
        ],
        page: 1,
        pageSize: 24,
        totalCount: 1,
        totalPages: 1,
        hasNextPage: false,
        hasPreviousPage: false,
        isFallback: false,
      },
    });
  });

  await page.route('**/api/products/slug/vintage-oak-chair', async (route) => {
    await route.fulfill({ status: 200, json: journeyProduct });
  });

  await page.route('**/api/products/product-1', async (route) => {
    await route.fulfill({ status: 200, json: journeyProduct });
  });

  await page.route('**/api/inquiries', async (route) => {
    await route.fulfill({ status: 201, json: { inquiryId: 'inquiry-1' } });
  });
}

export async function passTurnstileAutomatically(page: Page) {
  await page.addInitScript(() => {
    window.turnstile = {
      render: (_container, options) => {
        window.setTimeout(() => options.callback?.('journey-turnstile-token'), 0);
        return 'journey-widget';
      },
      execute: () => undefined,
      reset: () => undefined,
      remove: () => undefined,
    };
  });

  await page.route('https://challenges.cloudflare.com/turnstile/v0/api.js?render=explicit', async (route: Route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/javascript',
      body: '',
    });
  });
}
