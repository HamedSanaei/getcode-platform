import { expect, test } from '@playwright/test';
import type { Page } from '@playwright/test';

/**
 * M01-007 visual regression captures.
 *
 * Viewports come from the playwright.config projects (desktop 1440x900,
 * mobile 390x844); every test below runs once per project.
 *
 * Naming convention: visual-gallery--<what>--<viewport>--<dir>.png
 * (arg names drive the `{arg}` part of snapshotPathTemplate).
 *
 * Baseline provenance: these baselines encode the CURRENT implementation
 * output of the approved Penpot-derived primitives (rev 104). They are
 * regression protection, not design-truth evidence. Penpot-side capture and
 * approval of the design-truth baselines is tracked separately; see
 * docs/tasks/M01-007 handoff and frontend/VISUAL.md.
 */

const SECTIONS = [
  'buttons',
  'fields',
  'tabs',
  'badges',
  'alerts',
  'service-rows',
  'sidebar',
  'states',
] as const;

async function settle(page: Page): Promise<void> {
  await page.goto('/visual-gallery');
  // Deterministic capture contract: fonts loaded, no pending animation frames.
  await page.evaluate(() => document.fonts.ready);
  await page.waitForTimeout(50);
}

test.describe('gallery', () => {
  // Full-context captures use explicit buffers: locator-based
  // toHaveScreenshot hits a Playwright internals bug ("data undefined")
  // on these very tall elements.
  test('full page getcode brand context', async ({ page }, testInfo) => {
    await settle(page);
    const shot = await page.screenshot({ fullPage: true });
    await expect(shot).toMatchSnapshot(`visual-gallery--full--${testInfo.project.name}--ltr.png`, { maxDiffPixels: 24 });
  });

  test('full page pluspremium brand context', async ({ page }, testInfo) => {
    await settle(page);
    const shot = await page.screenshot({ fullPage: true });
    await expect(shot).toMatchSnapshot(`visual-gallery--brand-pluspremium--${testInfo.project.name}--ltr.png`, { maxDiffPixels: 24 });
  });

  test('full page RTL context', async ({ page }, testInfo) => {
    await settle(page);
    const ctx = page.locator('main > .vg-context[dir="rtl"]');
    const box = await ctx.boundingBox();
    const shot = await page.screenshot({ clip: box ?? undefined, fullPage: true });
    await expect(shot).toMatchSnapshot(`visual-gallery--full--${testInfo.project.name}--rtl.png`, { maxDiffPixels: 24 });
  });

  for (const section of SECTIONS) {
    test(`section ${section}`, async ({ page }, testInfo) => {
      await settle(page);
      const el = page.locator(`[data-visual-section="${section}"]`).first();
      await expect(el).toHaveScreenshot(`visual-gallery--section-${section}--${testInfo.project.name}--ltr.png`);
    });
  }
});
