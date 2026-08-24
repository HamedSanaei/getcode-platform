import { afterEach, describe, expect, it } from 'vitest';
import { resolveSiteConfig } from '../../src/lib/site/site-config';

const ENV_KEYS = ['GETCODE_PRIMARY_HOST', 'GETCODE_PLUSPREMIUM_HOST', 'GETCODE_CANONICAL_HOST'] as const;

afterEach(() => {
  for (const key of ENV_KEYS) delete process.env[key];
});

describe('resolveSiteConfig (M01-006 host resolution)', () => {
  it('resolves the configured primary host with the getcode brand', () => {
    process.env.GETCODE_PRIMARY_HOST = 'getcode.ir';
    const site = resolveSiteConfig('getcode.ir');
    expect(site).toMatchObject({ key: 'primary', brandKey: 'getcode', hostKnown: true });
  });

  it('resolves the pluspremium host with its own brand context', () => {
    process.env.GETCODE_PLUSPREMIUM_HOST = 'vnumber.pluspremium.ir';
    const site = resolveSiteConfig('vnumber.pluspremium.ir');
    expect(site).toMatchObject({ key: 'pluspremium', brandKey: 'pluspremium', hostKnown: true });
  });

  it('normalizes case and strips ports before matching', () => {
    process.env.GETCODE_PRIMARY_HOST = 'GetCode.ir';
    expect(resolveSiteConfig('GETCODE.IR:443').hostKnown).toBe(true);
    process.env.GETCODE_PLUSPREMIUM_HOST = 'vnumber.pluspremium.ir';
    expect(resolveSiteConfig('VNumber.PlusPremium.ir:8080').key).toBe('pluspremium');
  });

  it('flags unknown hosts explicitly instead of silently pretending', () => {
    process.env.GETCODE_PRIMARY_HOST = 'getcode.ir';
    process.env.GETCODE_PLUSPREMIUM_HOST = 'vnumber.pluspremium.ir';
    const site = resolveSiteConfig('evil-mirror.example');
    expect(site.key).toBe('primary'); // safe fallback
    expect(site.brandKey).toBe('getcode');
    expect(site.hostKnown).toBe(false); // explicit — metadata must noindex
  });

  it('canonical host comes only from configuration, defaulting to primary', () => {
    process.env.GETCODE_PRIMARY_HOST = 'getcode.ir';
    process.env.GETCODE_PLUSPREMIUM_HOST = 'vnumber.pluspremium.ir';
    expect(resolveSiteConfig('vnumber.pluspremium.ir').canonicalHost).toBe('getcode.ir');

    process.env.GETCODE_CANONICAL_HOST = 'www.getcode.ir';
    expect(resolveSiteConfig('vnumber.pluspremium.ir').canonicalHost).toBe('www.getcode.ir');
  });

  it('brand keys match the data-brand scopes emitted by the token bridge', () => {
    process.env.GETCODE_PRIMARY_HOST = 'getcode.ir';
    process.env.GETCODE_PLUSPREMIUM_HOST = 'vnumber.pluspremium.ir';
    expect(['getcode', 'pluspremium']).toContain(resolveSiteConfig('getcode.ir').brandKey);
    expect(resolveSiteConfig('vnumber.pluspremium.ir').brandKey).toBe('pluspremium');
  });
});
