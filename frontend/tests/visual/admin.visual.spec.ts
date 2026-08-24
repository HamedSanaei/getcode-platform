import { expect, test } from '@playwright/test';
import type { Page } from '@playwright/test';

/**
 * M09-001 admin shell captures (Penpot `GetCode · 09 Admin`, overview board
 * `…877889b74e32` + permission-denied pattern). The internal API is mocked at
 * the route layer so states are deterministic offline.
 *
 * Naming: admin--<state>--<project>.png
 */

const CAPABLE_PRINCIPAL = {
  userId: '01900000-0000-7000-8000-000000000001',
  roles: ['platform-admin'],
  permissions: ['admin.access', 'providers.manage', 'pricing.manage', 'orders.read'],
};

const LIMITED_PRINCIPAL = {
  userId: '01900000-0000-7000-8000-000000000002',
  roles: ['support-agent'],
  permissions: ['orders.read'],
};

async function mockPrincipal(page: Page, body: unknown, status = 200): Promise<void> {
  await page.route('**/api/auth/principal', (route) => route.fulfill({ json: body, status }));
}

async function settle(page: Page): Promise<void> {
  await page.goto('/admin');
  await page.evaluate(() => document.fonts.ready);
  const loading = page.getByRole('status').filter({ hasText: /checking|loading/i });
  while ((await loading.count()) > 0) {
    await page.waitForTimeout(50);
  }
  await page.waitForTimeout(50);
}

test.describe('admin shell', () => {
  test('capable principal sees the shell with capability-filtered navigation', async ({ page }, testInfo) => {
    await mockPrincipal(page, CAPABLE_PRINCIPAL);
    // Overview payload for the ready state.
    await page.route('**/api/admin/overview', (route) =>
      route.fulfill({ json: { serverTimeUtc: '2026-08-24T12:00:00Z' } }));
    await settle(page);
    const shot = await page.screenshot({ fullPage: true });
    await expect(shot).toMatchSnapshot(`admin--overview--${testInfo.project.name}.png`, { maxDiffPixels: 24 });
    // Capability-filtered navigation: granted surfaces render in the admin nav.
    await expect(
      page.getByRole('navigation', { name: 'Admin sections' }).getByRole('link', { name: /overview/i }),
    ).toBeVisible();
  });

  test('limited principal sees only permitted surfaces', async ({ page }, testInfo) => {
    await mockPrincipal(page, LIMITED_PRINCIPAL);
    await page.route('**/api/admin/overview', (route) =>
      route.fulfill({ json: { serverTimeUtc: '2026-08-24T12:00:00Z' } }));
    await settle(page);
    const shot = await page.screenshot({ fullPage: true });
    await expect(shot).toMatchSnapshot(`admin--limited--${testInfo.project.name}.png`, { maxDiffPixels: 24 });
    // No admin.access → the guard denies the whole shell (UX layer; API enforces too).
    await expect(page.getByText(/permission denied/i)).toBeVisible();
  });

  test('anonymous visitors get a sign-in prompt', async ({ page }, testInfo) => {
    await mockPrincipal(page, null, 401);
    await settle(page);
    const shot = await page.screenshot({ fullPage: true });
    await expect(shot).toMatchSnapshot(`admin--anonymous--${testInfo.project.name}.png`, { maxDiffPixels: 24 });
    await expect(page.getByText(/administrator sign-in required/i)).toBeVisible();
  });
});
