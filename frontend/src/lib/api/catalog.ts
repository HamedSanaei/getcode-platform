/**
 * Server-side clients for the public catalog read API (M03-004 surface,
 * ADR-006 same-origin /api/*). React components never touch the database;
 * every read goes through these typed fetchers against INTERNAL_API_URL.
 */

export interface CatalogPage<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface CountryDto {
  stableKey: string;
  displayName: string;
  displayOrder: number;
}

export interface ServiceDto {
  stableKey: string;
  displayName: string;
  displayOrder: number;
}

export interface OfferDto {
  stableKey: string;
  countryCode: string;
  serviceSlug: string;
  countryName: string;
  serviceName: string;
  productType: string;
}

const API_BASE = process.env.INTERNAL_API_URL ?? 'http://127.0.0.1:8080';

async function getJson<T>(path: string): Promise<T> {
  const response = await fetch(new URL(path, API_BASE), {
    headers: { accept: 'application/json' },
    // Catalog reads are cacheable at the edge; revalidate keeps pages fresh
    // without making Redis or request-time provider calls a truth source.
    next: { revalidate: 60 },
  });
  if (!response.ok) {
    throw new Error(`catalog read failed: ${path} -> ${response.status}`);
  }
  return (await response.json()) as T;
}

export function fetchCountries(page = 1): Promise<CatalogPage<CountryDto>> {
  return getJson(`/api/catalog/countries?page=${page}&pageSize=100`);
}

export function fetchServices(page = 1): Promise<CatalogPage<ServiceDto>> {
  return getJson(`/api/catalog/services?page=${page}&pageSize=100`);
}

export function fetchOffers(page = 1): Promise<CatalogPage<OfferDto>> {
  return getJson(`/api/catalog/offers?page=${page}&pageSize=50`);
}
