import { describe, expect, it } from 'vitest';
import { catalogPageMetadata } from '../../src/lib/api/catalog-metadata';

/**
 * M08-001 SEO metadata contract: canonical paths are derived only from the
 * route (the host comes from the configured canonical host in the root
 * layout's metadataBase) — mirrors/preview hosts cannot leak into SEO.
 */
describe('catalogPageMetadata', () => {
  it('builds clean canonical paths for catalog routes on both hosts', () => {
    expect(catalogPageMetadata({ path: '/numbers', title: 't', description: 'd' }).canonicalPath).toBe('/numbers');
    expect(
      catalogPageMetadata({ path: '/numbers/IR/telegram', title: 't', description: 'd' }).canonicalPath,
    ).toBe('/numbers/IR/telegram');
  });

  it('normalizes trailing slashes and duplicate segments away', () => {
    expect(catalogPageMetadata({ path: '/numbers/', title: 't', description: 'd' }).canonicalPath).toBe('/numbers');
    expect(catalogPageMetadata({ path: '//numbers//IR', title: 't', description: 'd' }).canonicalPath).toBe('/numbers/IR');
  });

  it('carries title and description through unchanged', () => {
    const meta = catalogPageMetadata({
      path: '/numbers/IR',
      title: 'Virtual numbers — IR',
      description: 'Browse Iran numbers.',
    });
    expect(meta.title).toBe('Virtual numbers — IR');
    expect(meta.description).toBe('Browse Iran numbers.');
  });
});
