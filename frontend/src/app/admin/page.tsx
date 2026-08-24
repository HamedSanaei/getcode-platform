'use client';

import { useEffect, useState } from 'react';
import { Alert } from '@/components/ui/Alert';

interface OverviewPayload {
  serverTimeUtc: string;
}

/**
 * M09-001 admin overview (Penpot `Admin / Overview` board `…877889b74e32`).
 * Renders the loading / error / ready states; the data anchor grows with the
 * later admin tasks (provider ops, catalog mapping, pricing, refunds, review).
 * The API itself enforces `admin.access`; this page renders whatever an
 * authorized session receives and surfaces errors honestly.
 */
export default function AdminOverviewPage() {
  const [state, setState] = useState<'loading' | 'error' | 'ready'>('loading');
  const [payload, setPayload] = useState<OverviewPayload | null>(null);

  useEffect(() => {
    let active = true;
    fetch('/api/admin/overview', { headers: { accept: 'application/json' } })
      .then(async (response) => {
        if (!response.ok) {
          throw new Error(`overview failed: ${response.status}`);
        }
        return (await response.json()) as OverviewPayload;
      })
      .then((body) => {
        if (!active) return;
        setPayload(body);
        setState('ready');
      })
      .catch(() => {
        if (!active) return;
        setState('error');
      });
    return () => {
      active = false;
    };
  }, []);

  if (state === 'loading') {
    return (
      <div role="status" className="catalog-empty">
        Loading overview…
      </div>
    );
  }

  if (state === 'error' || payload === null) {
    return (
      <Alert tone="danger" title="Could not load the overview">
        Refresh to try again. If this persists, your session may lack the required capability.
      </Alert>
    );
  }

  const serverTime = new Date(payload.serverTimeUtc);
  const formatted = Number.isNaN(serverTime.getTime())
    ? payload.serverTimeUtc
    : serverTime.toISOString().slice(0, 16).replace('T', ' ') + ' UTC';

  return (
    <section aria-labelledby="admin-overview-heading">
      <h1 id="admin-overview-heading" className="catalog-section-title">
        نمای کلی مدیریت
      </h1>
      <p>اتصال برقرار است؛ ماژول‌های عملیاتی به‌تدریج در همین مسیر فعال می‌شوند.</p>
      <dl>
        <dt>زمان سرور</dt>
        <dd>{formatted}</dd>
      </dl>
    </section>
  );
}
