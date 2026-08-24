import { afterEach, describe, expect, it } from 'vitest';
import { resolveSiteConfig } from '../../src/lib/site/site-config';

/**
 * Metadata contract tests (M01-006): canonical metadata must point at the
 * configured canonical host only — request-controlled values must never leak
 * into canonical URLs (no open redirects, no SEO hijacking).
 */
function metadataFor(requestHost: string) {
  const site = resolveSiteConfig(requestHost);
  return {
    site,
    metadataBase: new URL(`https://${site.canonicalHost}`),
    robots: site.hostKnown ? undefined : { index: false as const, follow: false as const },
  };
}

afterEach(() => {
  delete process.env.GETCODE_PRIMARY_HOST;
  delete process.env.GETCODE_PLUSPREMIUM_HOST;
  delete process.env.GETCODE_CANONICAL_HOST;
});

describe('canonical metadata (M01-006)', () => {
  it('builds canonical URLs from the configured host, not the request host', () => {
    process.env.GETCODE_PRIMARY_HOST = 'getcode.ir';
    process.env.GETCODE_CANONICAL_HOST = 'www.getcode.ir';

    const { site, metadataBase } = metadataFor('vnumber.pluspremium.ir');
    expect(site.canonicalHost).toBe('www.getcode.ir');

    const canonical = new URL('/orders', metadataBase);
    expect(canonical.origin).toBe('https://www.getcode.ir');
    expect(canonical.href).toBe('https://www.getcode.ir/orders');
  });

  it('request headers cannot inject an alternate canonical origin', () => {
    process.env.GETCODE_PRIMARY_HOST = 'getcode.ir';

    // Even a hostile Host header only affects `site.host`, never the
    // canonical origin: metadataBase is derived from env config alone.
    const hostile = metadataFor('evil.example');
    expect(new URL('/wallet', hostile.metadataBase).hostname).toBe('getcode.ir');
  });

  it('unknown hosts are excluded from search indexes', () => {
    process.env.GETCODE_PRIMARY_HOST = 'getcode.ir';
    process.env.GETCODE_PLUSPREMIUM_HOST = 'vnumber.pluspremium.ir';

    expect(metadataFor('preview-staging.example').robots).toEqual({ index: false, follow: false });
    expect(metadataFor('getcode.ir').robots).toBeUndefined();
    expect(metadataFor('vnumber.pluspremium.ir').robots).toBeUndefined();
  });
});
