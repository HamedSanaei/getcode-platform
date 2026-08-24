'use client';

import type { ReactNode } from 'react';

/**
 * GetCode / Alert Variants — Penpot axis: Tone
 * (board 324404a7-ad1e-8048-8008-87741e908937).
 * Politeness: `polite` renders role="status" (live-region announcements),
 * `assertive` renders role="alert". Default follows the tone's urgency.
 */
export type AlertTone = 'success' | 'warning' | 'danger' | 'info';

const TONE_CLASS: Record<AlertTone, string> = {
  success: 'gc-alert--success',
  warning: 'gc-alert--warning',
  danger: 'gc-alert--danger',
  info: 'gc-alert--info',
};

export interface AlertProps {
  tone?: AlertTone;
  title: ReactNode;
  children?: ReactNode;
  /** Overrides the default live-region politeness for the tone. */
  politeness?: 'polite' | 'assertive';
}

export function Alert({ tone = 'info', title, children, politeness }: AlertProps) {
  const role = politeness ?? (tone === 'danger' || tone === 'warning' ? 'alert' : 'status');
  return (
    <div className={`gc-alert ${TONE_CLASS[tone]}`} role={role}>
      <div>
        <strong>{title}</strong>
        {children ? <div>{children}</div> : null}
      </div>
    </div>
  );
}
