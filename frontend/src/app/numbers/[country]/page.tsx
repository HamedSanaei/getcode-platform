import Link from 'next/link';
import { Suspense } from 'react';
import type { Metadata } from 'next';
import { Alert } from '@/components/ui/Alert';
import { CatalogExplorer } from '@/components/catalog/CatalogExplorer';
import { fetchCountries, fetchOffers, fetchServices } from '@/lib/api/catalog';

type PageProps = { params: Promise<{ country: string }> };

export async function generateMetadata({ params }: PageProps): Promise<Metadata> {
  const { country } = await params;
  return {
    title: `Virtual numbers — ${country}`,
    description: `Browse available ${country} virtual numbers by service.`,
    alternates: { canonical: `/numbers/${country}` },
  };
}

/**
 * M08-001 `Public / Catalog` filtered to one country. Same required states as
 * the unfiltered catalog; the active country chip is highlighted.
 */
export default async function CountryPage({ params }: PageProps) {
  const { country } = await params;

  return (
    <main className="shell">
      <nav aria-label="Breadcrumb">
        <Link href="/numbers">← All countries</Link>
      </nav>
      <h1 className="catalog-section-title">Numbers</h1>
      <Suspense fallback={<p role="status" className="catalog-empty">Loading catalog…</p>}>
        <CountryCatalogLoader activeCountry={country} />
      </Suspense>
    </main>
  );
}

async function CountryCatalogLoader({ activeCountry }: { activeCountry: string }) {
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
      activeCountry={activeCountry}
    />
  );
}
