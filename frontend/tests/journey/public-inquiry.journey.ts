import { expect, test } from '@playwright/test';
import { journeyProduct, passTurnstileAutomatically, registerJourneyApi } from './server';

test.describe('public inquiry journey', () => {
  test('visitor searches catalog, opens a product, and submits an inquiry', async ({ page }) => {
    await passTurnstileAutomatically(page);
    await registerJourneyApi(page);

    await page.goto('/');
    await page.getByRole('textbox', { name: /search products/i }).fill('oak chair');
    await page.keyboard.press('Enter');

    await expect(page).toHaveURL(/\/products\?search=oak%20chair$/);
    await expect(page.getByText(journeyProduct.title)).toBeVisible();

    await page.getByRole('link', { name: new RegExp(journeyProduct.title, 'i') }).first().click();
    await expect(page).toHaveURL(/\/products\/vintage-oak-chair$/);
    await page.getByRole('link', { name: /send inquiry/i }).click();

    await expect(page).toHaveURL('/products/product-1/inquiry');
    await page.getByLabel(/your name/i).fill('Journey Buyer');
    await page.getByLabel(/email/i).fill('buyer@example.test');
    await page.getByLabel(/message/i).fill('Is this chair still available?');
    await page.getByRole('button', { name: /send inquiry/i }).click();

    await expect(page.getByText(/inquiry sent/i)).toBeVisible();
  });
});
