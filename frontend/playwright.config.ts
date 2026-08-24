import { defineConfig, devices } from '@playwright/test';

/**
 * M01-007 visual regression harness.
 *
 * Storage conventions:
 * - Baselines: tests/visual/baselines/<spec-name>/<test>--<viewport>--<dir>.png
 *   (committed to git; updated ONLY via the reviewed `visual:update` flow).
 * - Failure artifacts: test-results/ (diff/actual/expected triplets; gitignored).
 * - Report: playwright-report/ (gitignored).
 *
 * Determinism contract:
 * - Fixed viewports: desktop 1440x900, mobile 390x844 (DPR 1 for byte stability).
 * - Animations/transitions disabled (`disableAnimations` + reducedMotion=reduce).
 * - Caret hidden in inputs.
 * - Specs wait for `document.fonts.ready` before capturing.
 * - The fixture surface (/visual-gallery) is fully static: no dates, no
 *   randomness, no network-dependent content.
 */

const PORT = Number(process.env.VISUAL_PORT ?? 3100);

export default defineConfig({
  testDir: 'tests/visual',
  outputDir: 'test-results',
  snapshotDir: 'tests/visual/baselines',
  snapshotPathTemplate: '{snapshotDir}/{testFileDir}/{arg}{ext}',
  fullyParallel: false,
  workers: 1,
  retries: 0,
  timeout: 60_000,
  expect: {
    toHaveScreenshot: {
      // CI runners and local machines can differ by a hair on antialiasing;
      // keep the tolerance tight but not byte-exact.
      maxDiffPixels: 24,
      threshold: 0.2,
      animations: 'disabled',
      caret: 'hide',
    },
  },
  use: {
    baseURL: `http://127.0.0.1:${PORT}`,
    screenshot: 'only-on-failure',
    trace: 'retain-on-failure',
  },
  reporter: [['list'], ['html', { outputFolder: 'playwright-report', open: 'never' }]],
  webServer: {
    command: 'npm run start -- --port 3100 --hostname 127.0.0.1',
    url: `http://127.0.0.1:${PORT}/visual-gallery`,
    reuseExistingServer: !process.env.CI,
    timeout: 120_000,
  },
  projects: [
    { name: 'desktop', use: { ...devices['Desktop Chrome'], viewport: { width: 1440, height: 900 }, deviceScaleFactor: 1 } },
    { name: 'mobile', use: { ...devices['Desktop Chrome'], viewport: { width: 390, height: 844 }, deviceScaleFactor: 1, isMobile: true } },
  ],
});
