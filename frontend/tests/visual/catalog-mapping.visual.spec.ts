import { expect, test } from '@playwright/test';
import type { Page } from '@playwright/test';

/**
 * M09-003 mapping management page captures
 * (Penpot `Admin · Catalog mapping` board `…87789ca603d0`).
 * Internal API mocked at the route layer; deterministic offline.
 */

const CAPABLE_PRINCIPAL = {
  userId: '01900000-0000-7000-8000-000000000009',
  roles: ['m09-mapping-admin'],
  permissions: ['admin.access'],
};

const PROVIDERS = [
  {
    providerKey: 'alpha',
    displayName: 'Alpha Numbers',
    isEnabled: true,
    supportsActivation: true,
    supportsRental: false,
    mappings: [
      { kind: 'Country', externalCode: '49', canonicalStableKey: 'DE' },
      { kind: 'Service', externalCode: 'tg-1', canonicalStableKey: 'telegram' },
    ],
  },
  { providerKey: 'beta', displayName: 'Beta Tel', isEnabled: false, supportsActivation: true, supportsRental: true, mappings: [] },
];

async function settle(page: Page): Promise<void> {
  await page.route('**/api/auth/principal', (route) => route.fulfill({ json: CAPABLE_PRINCIPAL }));
  await page.route('**/api/admin/providers', (route) => route.fulfill({ json: PROVIDERS }));
  await page.goto('/admin/catalog-mapping');
  await page.evaluate(() => document.fonts.ready);
  const loading = page.getByRole('status').filter({ hasText: /loading/i });
  while ((await loading.count()) > 0) {
    await page.waitForTimeout(50);
  }
  await page.waitForTimeout(50);
}

test.describe('mapping management page', () => {
  test('ready state lists providers with resolved mappings', async ({ page }, testInfo) => {
    await settle(page);
    const shot = await page.screenshot({ fullPage: true });
    await expect(shot).toMatchSnapshot(`mapping--ready--${testInfo.project.name}.png`, { maxDiffPixels: 24 });
    await expect(page.getByText('Alpha Numbers')).toBeVisible();
    await expect(page.getByText(/49 → DE/u)).toBeVisible();
  });

  test('load failure renders an error alert instead of the table', async ({ page }, testInfo) => {
    await page.route('**/api/auth/principal', (route) => route.fulfill({ json: CAPABLE_PRINCIPAL }));
    await page.route('**/api/admin/providers', (route) => route.abort());
    await page.goto('/admin/catalog-mapping');
    await page.evaluate(() => document.fonts.ready);
    await page.waitForTimeout(100);
    const shot = await page.screenshot({ fullPage: true });
    await expect(shot).toMatchSnapshot(`mapping--error--${testInfo.project.name}.png`, { maxDiffPixels: 24 });
    await expect(page.getByText(/could not load providers/i)).toBeVisible();
  });
});
