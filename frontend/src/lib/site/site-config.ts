export type SiteConfig = {
  key: "primary" | "pluspremium";
  host: string;
  brandKey: "getcode" | "getcode-pluspremium";
  canonicalHost: string;
};

const normalizeHost = (host: string) => host.trim().toLowerCase().split(":")[0];

export function resolveSiteConfig(requestHost: string): SiteConfig {
  const host = normalizeHost(requestHost);
  const primary = normalizeHost(process.env.GETCODE_PRIMARY_HOST ?? "getcode.example");
  const plusPremium = normalizeHost(process.env.GETCODE_PLUSPREMIUM_HOST ?? "vnumber.pluspremium.ir");
  const canonical = normalizeHost(process.env.GETCODE_CANONICAL_HOST ?? primary);

  if (host === plusPremium) {
    return { key: "pluspremium", host, brandKey: "getcode-pluspremium", canonicalHost: canonical };
  }

  return { key: "primary", host: host || primary, brandKey: "getcode", canonicalHost: canonical };
}
