import type { ReactNode } from 'react';
import { Badge, type BadgeTone } from './Badge';

/**
 * GetCode / Service Row Variants — Penpot axis: State
 * (board 324404a7-ad1e-8048-8008-87741a2355ea).
 * Presentational only: availability is expressed via aria-disabled; pricing
 * and routing stay with the caller. `price` renders as plain text.
 */
export interface ServiceRowProps {
  title: ReactNode;
  meta?: ReactNode;
  price?: string;
  badge?: { tone: BadgeTone; label: ReactNode };
  available?: boolean;
  /** Rendered as a button (interactive rows) or a plain element (informational). */
  onSelect?: () => void;
}

export function ServiceRow({ title, meta, price, badge, available = true, onSelect }: ServiceRowProps) {
  const body = (
    <>
      <span className="gc-service-row__body">
        <span className="gc-service-row__title">{title}</span>
        {meta ? <span className="gc-service-row__meta">{meta}</span> : null}
      </span>
      {badge ? <Badge tone={badge.tone}>{badge.label}</Badge> : null}
      {price ? <span className="gc-service-row__price">{price}</span> : null}
    </>
  );

  if (onSelect) {
    return (
      <button
        type="button"
        className="gc-service-row"
        aria-disabled={available ? undefined : true}
        disabled={!available}
        onClick={available ? onSelect : undefined}
      >
        {body}
      </button>
    );
  }

  return (
    <div className="gc-service-row" aria-disabled={available ? undefined : true}>
      {body}
    </div>
  );
}
