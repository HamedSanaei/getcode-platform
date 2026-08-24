import Link from 'next/link';
import { Badge } from '@/components/ui/Badge';
import type { OfferDto } from '@/lib/api/catalog';

/**
 * Public offer row (M08-001, Penpot `Public / Catalog` boards — Service Row
 * variants board `…87741a2355ea`). Provider routing data never appears here —
 * only canonical country/service/product. Unavailable offers render in the
 * design-system disabled state.
 */
export function OfferCard({ offer, available = true }: { offer: OfferDto; available?: boolean }) {
  const href = `/numbers/${offer.countryCode}/${offer.serviceSlug}`;
  if (!available) {
    return (
      <div className="gc-service-row" data-state="unavailable" aria-disabled="true">
        <div className="gc-service-row__body">
          <span className="gc-service-row__title">{offer.serviceName}</span>
          <span className="gc-service-row__meta">
            {offer.countryName} · {offer.productType}
          </span>
        </div>
        <Badge tone="danger">Unavailable</Badge>
      </div>
    );
  }
  return (
    <Link className="gc-service-row" href={href}>
      <div className="gc-service-row__body">
        <span className="gc-service-row__title">{offer.serviceName}</span>
        <span className="gc-service-row__meta">
          {offer.countryName} · {offer.productType}
        </span>
      </div>
      <span aria-hidden="true">
        ›
      </span>
    </Link>
  );
}
