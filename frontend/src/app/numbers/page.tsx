import { Suspense } from 'react';
import type { Metadata } from 'next';
import { Alert } from '@/components/ui/Alert';
import { CatalogExplorer } from '@/components/catalog/CatalogExplorer';
import { fetchCountries, fetchOffers, fetchServices } from '@/lib/api/catalog';

export async function generateMetadata(): Promise<Metadata> {
  return {
    title: 'Browse numbers by country and service',
    description: 'Filter the full catalog of available virtual numbers by country and service.',
    alternates: { canonical: '/numbers' },
  };
}

/**
 * M08-001 `Public / Catalog` boards (desktop `…8775bb814780`, mobile
 * `…8775d38d8fc2`). Required states: filters, no results, load-more pagination
 * and provider-unavailable fallback (rendered as unavailable offers).
 */
export default function NumbersPage() {
  return (
    <main className="shell">
      <h1 className="catalog-section-title">Browse numbers</h1>
      <Suspense fallback={<p role="status" className="catalog-empty">Loading catalog…</p>}>
        <CatalogLoader />
      </Suspense>
    </main>
  );
}

async function CatalogLoader() {
  let data;
  try {
    const [services, offers, countries] = await Promise.all([
      fetchServices(),
      fetchOffers(),
      fetchCountries(),
    ]);
    data = { services, offers, countries };
  } catch {
    data = null;
  }

  if (data === null) {
    return (
      <Alert tone="danger" title="Catalog is temporarily unavailable">
        Please refresh in a moment.
      </Alert>
    );
  }

  return (
    <CatalogExplorer
      services={data.services.items}
      offers={data.offers.items}
      countries={data.countries.items.map((c) => ({ stableKey: c.stableKey, displayName: c.displayName }))}
    />
  );
}
