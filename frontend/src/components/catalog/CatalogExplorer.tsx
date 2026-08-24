'use client';

import { useMemo, useState } from 'react';
import Link from 'next/link';
import { Button } from '@/components/ui/Button';
import { TextField } from '@/components/ui/TextField';
import type { OfferDto, ServiceDto } from '@/lib/api/catalog';
import './catalog.css';

/**
 * Client-side search + filter over the paged public catalog reads
 * (M08-001 `Public / Home` + `Public / Catalog` boards). The server API is
 * deterministic and paged but has no server-side search yet; filtering here is
 * presentation-level only, never a substitute for server authorization.
 */
export function CatalogExplorer({
  services,
  offers,
  countries,
  activeCountry,
}: {
  services: ServiceDto[];
  offers: OfferDto[];
  countries: { stableKey: string; displayName: string }[];
  activeCountry?: string;
}) {
  const [query, setQuery] = useState('');
  const [visibleCount, setVisibleCount] = useState(12);

  const normalized = query.trim().toLocaleLowerCase('en');

  const filteredServices = useMemo(
    () =>
      services.filter((s) => !normalized || s.displayName.toLocaleLowerCase('en').includes(normalized)),
    [services, normalized],
  );

  const filteredOffers = useMemo(
    () =>
      offers.filter(
        (o) =>
          (!activeCountry || o.countryCode === activeCountry) &&
          (!normalized ||
            o.serviceName.toLocaleLowerCase('en').includes(normalized) ||
            o.countryName.toLocaleLowerCase('en').includes(normalized)),
      ),
    [offers, activeCountry, normalized],
  );

  const visibleOffers = filteredOffers.slice(0, visibleCount);
  const nothingAtAll = filteredServices.length === 0 && filteredOffers.length === 0;

  return (
    <div className="catalog-page">
      <TextField
        label="Search"
        hideLabel
        type="search"
        placeholder="Search service or country…"
        value={query}
        onChange={(event) => {
          setQuery(event.target.value);
          setVisibleCount(12);
        }}
      />

      <nav aria-label="Countries" className="catalog-country-strip">
        <Link className="catalog-chip" data-active={!activeCountry} href="/numbers">
          All countries
        </Link>
        {countries.map((c) => (
          <Link
            key={c.stableKey}
            className="catalog-chip"
            data-active={activeCountry === c.stableKey}
            href={`/numbers/${c.stableKey}`}
          >
            {c.displayName}
          </Link>
        ))}
      </nav>

      {nothingAtAll ? (
        <p className="catalog-empty" role="status">
          No results for “{query}”. Try a different service or country.
        </p>
      ) : (
        <>
          <h2 className="catalog-section-title" id="catalog-services-heading">
            Services
          </h2>
          {filteredServices.length === 0 ? (
            <p className="catalog-empty" role="status">
              No matching services.
            </p>
          ) : (
            <ul aria-labelledby="catalog-services-heading" className="catalog-grid">
              {filteredServices.map((s) => {
                const firstOffer = filteredOffers.find((o) => o.serviceSlug === s.stableKey);
                return (
                  <li key={s.stableKey}>
                    <Link
                      className="gc-service-row"
                      href={firstOffer ? `/numbers/${firstOffer.countryCode}/${s.stableKey}` : `/numbers`}
                    >
                      <span className="gc-service-row__title">{s.displayName}</span>
                    </Link>
                  </li>
                );
              })}
            </ul>
          )}

          <h2 className="catalog-section-title" id="catalog-offers-heading">
            Available numbers
          </h2>
          {visibleOffers.length === 0 ? (
            <p className="catalog-empty" role="status">
              Nothing available here right now — check back soon.
            </p>
          ) : (
            <ul aria-labelledby="catalog-offers-heading" className="catalog-grid">
              {visibleOffers.map((o) => (
                <li key={o.stableKey}>
                  <a className="gc-service-row" href={`/numbers/${o.countryCode}/${o.serviceSlug}`}>
                    <span className="gc-service-row__title">{o.serviceName}</span>
                    <span className="gc-service-row__meta">{o.countryName}</span>
                  </a>
                </li>
              ))}
            </ul>
          )}

          {visibleCount < filteredOffers.length ? (
            <div className="catalog-load-more">
              <Button
                buttonStyle="secondary"
                onClick={() => setVisibleCount((n) => n + 12)}
              >
                Load more ({filteredOffers.length - visibleCount} remaining)
              </Button>
            </div>
          ) : null}
        </>
      )}
    </div>
  );
}
