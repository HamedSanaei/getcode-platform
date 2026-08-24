import Link from 'next/link';
import { Suspense } from 'react';
import type { Metadata } from 'next';
import { Alert } from '@/components/ui/Alert';
import { Badge } from '@/components/ui/Badge';
import { OfferCard } from '@/components/catalog/OfferCard';
import { fetchOffers, type OfferDto } from '@/lib/api/catalog';

type PageProps = { params: Promise<{ country: string; service: string }> };

export async function generateMetadata({ params }: PageProps): Promise<Metadata> {
  const { country, service } = await params;
  return {
    title: `${service} numbers — ${country}`,
    description: `Availability and activation details for ${service} virtual numbers in ${country}.`,
    alternates: { canonical: `/numbers/${country}/${service}` },
  };
}

/**
 * M08-001 `Public / Product Detail` boards (desktop `…8775c5f49319`, mobile
 * `…8775de22f8ee`). Required states: available, unavailable, quote refresh,
 * related offers. Quotes require an authenticated wallet session (M05/M06), so
 * the CTA routes anonymous visitors through sign-in — it never fakes a price.
 */
export default async function ProductPage({ params }: PageProps) {
  const { country, service } = await params;

  return (
    <main className="shell">
      <nav aria-label="Breadcrumb">
        <Link href="/numbers">← Browse numbers</Link>
      </nav>
      <Suspense fallback={<p role="status" className="catalog-empty">Loading…</p>}>
        <ProductLoader countryCode={country} serviceSlug={service} />
      </Suspense>
    </main>
  );
}

async function ProductLoader({ countryCode, serviceSlug }: { countryCode: string; serviceSlug: string }) {
  let offers: OfferDto[] | null = null;
  try {
    offers = (await fetchOffers()).items;
  } catch {
    offers = null;
  }

  if (offers === null) {
    return (
      <Alert tone="danger" title="Could not load this product">
        Please refresh in a moment.
      </Alert>
    );
  }

  const exact = offers.find((o) => o.countryCode === countryCode && o.serviceSlug === serviceSlug);
  if (!exact) {
    return (
      <div className="catalog-page">
        <Alert tone="warning" title="Currently unavailable">
          This combination is not being offered right now. Related offers are listed below.
        </Alert>
        <RelatedOffers offers={offers} countryCode={countryCode} excludeSlug={serviceSlug} />
      </div>
    );
  }

  return (
    <div className="catalog-page">
      <section aria-labelledby="product-heading" className="gc-service-row" style={{ cursor: 'default' }}>
        <div className="gc-service-row__body">
          <h1 id="product-heading" className="gc-service-row__title">
            {exact.serviceName}
          </h1>
          <p className="gc-service-row__meta">
            {exact.countryName} · {exact.productType}
          </p>
        </div>
        <Badge tone="success">Available</Badge>
      </section>

      {/* Quote refresh state: prices come only from an authenticated quote (M05),
          never from the public surface. The CTA routes to sign-in. */}
      <div style={{ display: 'flex', gap: 12, alignItems: 'center' }}>
        <Link href="/auth/sign-in" className="gc-button gc-button--primary gc-button--md">
          Sign in to get a live quote
        </Link>
        <span className="gc-service-row__meta">Prices refresh at quote time.</span>
      </div>

      <RelatedOffers offers={offers} countryCode={countryCode} excludeSlug={serviceSlug} />
    </div>
  );
}

function RelatedOffers({
  offers,
  countryCode,
  excludeSlug,
}: {
  offers: OfferDto[];
  countryCode: string;
  excludeSlug: string;
}) {
  const related = offers.filter((o) => o.countryCode === countryCode && o.serviceSlug !== excludeSlug).slice(0, 6);
  if (related.length === 0) {
    return null;
  }
  return (
    <section aria-labelledby="related-heading">
      <h2 id="related-heading" className="catalog-section-title">
        Other services in this country
      </h2>
      <ul className="catalog-grid" style={{ listStyle: 'none', margin: 0, padding: 0 }}>
        {related.map((o) => (
          <li key={o.stableKey}>
            <OfferCard offer={o} />
          </li>
        ))}
      </ul>
    </section>
  );
}
