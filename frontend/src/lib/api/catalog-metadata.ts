/**
 * SEO metadata helpers for public catalog pages (M08-001).
 *
 * Canonical URLs are built exclusively from the configured canonical host via
 * the root layout's metadataBase — never from the request Host — so mirrors
 * and preview hosts cannot hijack SEO on either brand domain.
 */

export interface CatalogPageMetadata {
  title: string;
  description: string;
  canonicalPath: string;
}

export function catalogPageMetadata(parts: {
  siteName?: string;
  path: string;
  title: string;
  description: string;
}): CatalogPageMetadata {
  const cleanPath = `/${parts.path.split('/').filter(Boolean).join('/')}`;
  return {
    title: parts.title,
    description: parts.description,
    canonicalPath: cleanPath,
  };
}
