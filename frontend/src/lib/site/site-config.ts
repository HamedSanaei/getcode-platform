/**
 * Host → site resolution (M01-006).
 *
 * Policy (explicit, tested):
 * - Only the two configured hosts are "known": GETCODE_PRIMARY_HOST and
 *   GETCODE_PLUSPREMIUM_HOST. Port and case are normalized away.
 * - Unknown hosts fall back to the primary site config but are flagged with
 *   `hostKnown: false` so metadata can exclude them from search indexes —
 *   mirror/preview domains must not outrank the canonical host.
 * - Canonical host comes only from GETCODE_CANONICAL_HOST (default: primary
 *   host). It is never derived from the request, so no open-redirect surface
 *   exists.
 */
export type SiteKey = 'primary' | 'pluspremium';
/** Aligns with the `data-brand` scopes emitted by the M01-004 token bridge. */
export type BrandKey = 'getcode' | 'pluspremium';

export interface SiteConfig {
  key: SiteKey;
  host: string;
  brandKey: BrandKey;
  canonicalHost: string;
  /** False when the request host is neither configured host. */
  hostKnown: boolean;
}

const normalizeHost = (host: string) => host.trim().toLowerCase().split(':')[0];

export function resolveSiteConfig(requestHost: string): SiteConfig {
  const host = normalizeHost(requestHost);
  const primary = normalizeHost(process.env.GETCODE_PRIMARY_HOST ?? 'getcode.example');
  const plusPremium = normalizeHost(process.env.GETCODE_PLUSPREMIUM_HOST ?? 'vnumber.pluspremium.ir');
  const canonical = normalizeHost(process.env.GETCODE_CANONICAL_HOST ?? primary);

  if (host === plusPremium) {
    return { key: 'pluspremium', host, brandKey: 'pluspremium', canonicalHost: canonical, hostKnown: true };
  }

  return {
    key: 'primary',
    host: host || primary,
    brandKey: 'getcode',
    canonicalHost: canonical,
    hostKnown: host === primary,
  };
}
