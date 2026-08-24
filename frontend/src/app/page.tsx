import Link from 'next/link';
import { Alert } from '@/components/ui/Alert';
import { CatalogExplorer } from '@/components/catalog/CatalogExplorer';
import { fetchCountries, fetchOffers, fetchServices } from '@/lib/api/catalog';
import type { CatalogPage, CountryDto, OfferDto, ServiceDto } from '@/lib/api/catalog';

type CatalogData = {
  services: CatalogPage<ServiceDto>;
  offers: CatalogPage<OfferDto>;
  countries: CatalogPage<CountryDto>;
};

async function loadCatalog(): Promise<CatalogData | null> {
  try {
    const [services, offers, countries] = await Promise.all([
      fetchServices(),
      fetchOffers(),
      fetchCountries(),
    ]);
    return { services, offers, countries };
  } catch {
    return null;
  }
}

export default async function HomePage() {
  const data = await loadCatalog();

  let body;
  if (data === null) {
    body = (
      <Alert tone="danger" title="Catalog is temporarily unavailable">
        Please refresh in a moment.
      </Alert>
    );
  } else if (data.services.items.length === 0) {
    body = (
      <p className="catalog-empty" role="status">
        The catalog is being prepared. Check back soon.
      </p>
    );
  } else {
    body = (
      <CatalogExplorer
        services={data.services.items}
        offers={data.offers.items}
        countries={data.countries.items.map((c) => ({ stableKey: c.stableKey, displayName: c.displayName }))}
      />
    );
  }

  return (
    <main className="shell">
      <h1 className="catalog-section-title">Get a virtual number in seconds</h1>
      <p>
        Pick a service, choose a country, receive your activation codes. Wallet-based checkout,
        transparent history.
      </p>
      {body}
      <p style={{ marginTop: 24 }}>
        <Link href="/numbers">Browse the full catalog →</Link>
      </p>
    </main>
  );
}
