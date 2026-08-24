import type { ReactNode } from 'react';

/**
 * GetCode / Badge Variants — Penpot axis: Tone
 * (board 324404a7-ad1e-8048-8008-877417ce7eb7).
 */
export type BadgeTone = 'neutral' | 'success' | 'warning' | 'danger' | 'info' | 'brand';

const TONE_CLASS: Record<BadgeTone, string> = {
  neutral: 'gc-badge--neutral',
  success: 'gc-badge--success',
  warning: 'gc-badge--warning',
  danger: 'gc-badge--danger',
  info: 'gc-badge--info',
  brand: 'gc-badge--brand',
};

export function Badge({ tone = 'neutral', children }: { tone?: BadgeTone; children: ReactNode }) {
  return <span className={`gc-badge ${TONE_CLASS[tone]}`}>{children}</span>;
}
