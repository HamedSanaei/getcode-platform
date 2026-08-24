import { expect, test } from '@playwright/test';
import type { Page, Route } from '@playwright/test';

/**
 * M08-001 public-catalog page captures. The backend is not part of the
 * harness; /api/catalog/* responses are intercepted with deterministic
 * fixtures so the pages render their real code paths offline.
 *
 * Naming: catalog--<page>--<project>--<state>.png
 */

const COUNTRIES = {
  items: [
    { stableKey: 'IR', displayName: 'Iran', displayOrder: 1 },
    { stableKey: 'US', displayName: 'United States', displayOrder: 2 },
    { stableKey: 'DE', displayName: 'Germany', displayOrder: 3 },
  ],
  page: 1,
  pageSize: 100,
  totalCount: 3,
  totalPages: 1,
};

const SERVICES = {
  items: [
    { stableKey: 'telegram', displayName: 'Telegram', displayOrder: 1 },
    { stableKey: 'whatsapp', displayName: 'WhatsApp', displayOrder: 2 },
    { stableKey: 'signal', displayName: 'Signal', displayOrder: 3 },
  ],
  page: 1,
  pageSize: 100,
  totalCount: 3,
  totalPages: 1,
};

const OFFERS = {
  items: [
    { stableKey: 'ir-telegram-activation', countryCode: 'IR', serviceSlug: 'telegram', countryName: 'Iran', serviceName: 'Telegram', productType: 'Activation' },
    { stableKey: 'us-whatsapp-rental', countryCode: 'US', serviceSlug: 'whatsapp', countryName: 'United States', serviceName: 'WhatsApp', productType: 'Rental' },
    { stableKey: 'ir-whatsapp-activation', countryCode: 'IR', serviceSlug: 'whatsapp', countryName: 'Iran', serviceName: 'WhatsApp', productType: 'Activation' },
  ],
  page: 1,
  pageSize: 50,
  totalCount: 3,
  totalPages: 1,
};

async function mockCatalogApi(page: Page): Promise<void> {
  const json = (body: unknown) => (route: Route) => route.fulfill({ json: body });
  await page.route('**/api/catalog/countries*', json(COUNTRIES));
  await page.route('**/api/catalog/services*', json(SERVICES));
  await page.route('**/api/catalog/offers*', json(OFFERS));
}

async function settle(page: Page, url: string): Promise<void> {
  await mockCatalogApi(page);
  // Server components stream: wait until every network request (incl. the
  // internal API round-trips behind Suspense) has settled and no loading
  // status remains, otherwise fullPage captures race the fallback.
  await page.goto(url, { waitUntil: 'networkidle' });
  await page.evaluate(() => document.fonts.ready);
  const loading = page.getByRole('status').filter({ hasText: /loading/i });
  while ((await loading.count()) > 0) {
    await page.waitForTimeout(50);
  }
  await page.waitForTimeout(50);
}

test.describe('public catalog pages', () => {
  test('home', async ({ page }, testInfo) => {
    await settle(page, '/');
    const shot = await page.screenshot({ fullPage: true });
    await expect(shot).toMatchSnapshot(`catalog--home--${testInfo.project.name}.png`, { maxDiffPixels: 24 });
  });

  test('catalog browse', async ({ page }, testInfo) => {
    await settle(page, '/numbers');
    const shot = await page.screenshot({ fullPage: true });
    await expect(shot).toMatchSnapshot(`catalog--numbers--${testInfo.project.name}.png`, { maxDiffPixels: 24 });
  });

  test('country filtered', async ({ page }, testInfo) => {
    await settle(page, '/numbers/IR');
    const shot = await page.screenshot({ fullPage: true });
    await expect(shot).toMatchSnapshot(`catalog--country-IR--${testInfo.project.name}.png`, { maxDiffPixels: 24 });
  });

  test('product available', async ({ page }, testInfo) => {
    await settle(page, '/numbers/IR/telegram');
    const shot = await page.screenshot({ fullPage: true });
    await expect(shot).toMatchSnapshot(`catalog--product-available--${testInfo.project.name}.png`, { maxDiffPixels: 24 });
  });

  test('product unavailable shows fallback with related offers', async ({ page }, testInfo) => {
    await settle(page, '/numbers/US/signal');
    const shot = await page.screenshot({ fullPage: true });
    await expect(shot).toMatchSnapshot(`catalog--product-unavailable--${testInfo.project.name}.png`, { maxDiffPixels: 24 });
  });

  test('catalog error state when API fails', async ({ page }, testInfo) => {
    await page.route('**/api/catalog/**', (route) => route.abort());
    await page.goto('/numbers', { waitUntil: 'networkidle' });
    await page.evaluate(() => document.fonts.ready);
    const loading = page.getByRole('status').filter({ hasText: /loading/i });
    while ((await loading.count()) > 0) {
      await page.waitForTimeout(50);
    }
    await page.waitForTimeout(50);
    const shot = await page.screenshot({ fullPage: true });
    await expect(shot).toMatchSnapshot(`catalog--error--${testInfo.project.name}.png`, { maxDiffPixels: 24 });
  });
});
