'use client';

import { useCallback, useEffect, useState } from 'react';
import Link from 'next/link';
import { Alert } from '@/components/ui/Alert';
import { Badge } from '@/components/ui/Badge';
import { Button } from '@/components/ui/Button';
import { TextField } from '@/components/ui/TextField';

/**
 * M09-003 catalog/provider mapping management
 * (Penpot `Admin · Catalog mapping` board `…87789ca603d0`).
 *
 * Flow mirrors the backend contract: list → validate (dry-run preview) → bind.
 * Every mutation requires the CSRF pair and is audited server-side via the
 * transactional outbox; this screen never fabricates success.
 */

interface MappingRow {
  kind: string;
  externalCode: string;
  canonicalStableKey: string;
}

interface ProviderRow {
  providerKey: string;
  displayName: string;
  isEnabled: boolean;
  supportsActivation: boolean;
  supportsRental: boolean;
  mappings: MappingRow[];
}

async function postJson(path: string, body: unknown): Promise<{ ok: boolean; status: number }> {
  // CSRF double-submit: cookie was issued by GET /api/auth/csrf; echo the token.
  const csrfResponse = await fetch('/api/auth/csrf', { headers: { accept: 'application/json' } });
  const csrf = (await csrfResponse.json()) as { requestToken: string };
  const response = await fetch(path, {
    method: 'POST',
    headers: { 'content-type': 'application/json', accept: 'application/json', 'X-XSRF-TOKEN': csrf.requestToken },
    body: JSON.stringify(body),
  });
  return { ok: response.ok, status: response.status };
}

export default function ProviderMappingAdminPage() {
  const [providers, setProviders] = useState<ProviderRow[] | null>(null);
  const [loadError, setLoadError] = useState(false);

  const [registerKey, setRegisterKey] = useState('');
  const [registerName, setRegisterName] = useState('');
  const [bindProvider, setBindProvider] = useState('');
  const [bindKind, setBindKind] = useState('Country');
  const [bindExternal, setBindExternal] = useState('');
  const [bindCanonical, setBindCanonical] = useState('');
  const [preview, setPreview] = useState<{ resolved: boolean; displayName: string | null } | null>(null);  const [message, setMessage] = useState<{ tone: 'success' | 'danger'; text: string } | null>(null);
  const [busy, setBusy] = useState(false);

  const load = useCallback(async () => {
    try {
      const response = await fetch('/api/admin/providers', { headers: { accept: 'application/json' } });
      if (!response.ok) throw new Error(String(response.status));
      setProviders((await response.json()) as ProviderRow[]);
      setLoadError(false);
    } catch {
      setLoadError(true);
    }
  }, []);

  useEffect(() => {
    // Deferred one tick so the initial render stays free of cascading setState.
    const timer = setTimeout(() => { void load(); }, 0);
    return () => clearTimeout(timer);
  }, [load]);

  async function handlePreview(): Promise<void> {
    setBusy(true);
    setMessage(null);
    try {
      const response = await fetch('/api/admin/mappings/preview', {
        method: 'POST',
        headers: { 'content-type': 'application/json' },
        body: JSON.stringify({ providerKey: bindProvider, kind: bindKind, externalCode: bindExternal, canonicalStableKey: bindCanonical }),
      });
      const payload = (await response.json()) as { resolved: boolean; canonicalDisplayName: string | null };
      setPreview({ resolved: payload.resolved, displayName: payload.canonicalDisplayName });
    } catch {
      setPreview({ resolved: false, displayName: null });
    } finally {
      setBusy(false);
    }
  }

  async function handleBind(): Promise<void> {
    setBusy(true);
    setMessage(null);
    const result = await postJson('/api/admin/mappings/bind', {
      providerKey: bindProvider, kind: bindKind, externalCode: bindExternal, canonicalStableKey: bindCanonical,
    });
    if (result.ok) {
      setMessage({ tone: 'success', text: 'نگاشت ذخیره شد و در پیوست ممیزی ثبت گردید.' });
      setPreview(null);
      await load();
    } else {
      setMessage({ tone: 'danger', text: `ذخیره ناموفق بود (کد ${result.status}).` });
    }
    setBusy(false);
  }

  async function handleRegister(): Promise<void> {
    setBusy(true);
    setMessage(null);
    const result = await postJson('/api/admin/providers/register', {
      providerKey: registerKey, displayName: registerName, supportsActivation: true, supportsRental: false,
    });
    if (result.ok) {
      setMessage({ tone: 'success', text: 'ارائه‌دهنده ثبت شد.' });
      setRegisterKey('');
      setRegisterName('');
      await load();
    } else {
      setMessage({ tone: 'danger', text: `ثبت ناموفق بود (کد ${result.status}).` });
    }
    setBusy(false);
  }

  if (loadError) {
    return (
      <Alert tone="danger" title="Could not load providers">
        Refresh to try again.
      </Alert>
    );
  }

  if (providers === null) {
    return (
      <div role="status" className="catalog-empty">
        Loading providers…
      </div>
    );
  }

  return (
    <section aria-labelledby="mappings-heading">
      <h1 id="mappings-heading" className="catalog-section-title">
        نگاشت کاتالوگ و ارائه‌دهندگان
      </h1>

      {message ? <Alert tone={message.tone} title={message.text} /> : null}

      <h2 className="catalog-section-title">Providers</h2>
      {providers.length === 0 ? (
        <p className="catalog-empty" role="status">No providers registered yet.</p>
      ) : (
        <ul className="catalog-grid" style={{ listStyle: 'none', margin: 0, padding: 0 }}>
          {providers.map((p) => (
            <li key={p.providerKey}>
              <div className="gc-service-row" style={{ cursor: 'default', flexDirection: 'column', alignItems: 'stretch', gap: 8 }}>
                <div style={{ display: 'flex', gap: 8, alignItems: 'center', justifyContent: 'space-between' }}>
                  <span className="gc-service-row__title">{p.displayName}</span>
                  <Badge tone={p.isEnabled ? 'success' : 'danger'}>{p.isEnabled ? 'Enabled' : 'Disabled'}</Badge>
                </div>
                {p.mappings.length === 0 ? (
                  <span className="gc-service-row__meta">No mappings yet.</span>
                ) : (
                  <ul style={{ margin: 0, paddingInlineStart: 16 }}>
                    {p.mappings.map((m) => (
                      <li key={`${m.kind}-${m.externalCode}`} className="gc-service-row__meta">
                        {m.kind}: {m.externalCode} → {m.canonicalStableKey}
                      </li>
                    ))}
                  </ul>
                )}
              </div>
            </li>
          ))}
        </ul>
      )}

      <h2 className="catalog-section-title">Register provider</h2>
      <form
        onSubmit={(e) => { e.preventDefault(); void handleRegister(); }}
        style={{ display: 'grid', gap: 12, maxWidth: 420 }}
      >
        <TextField label="Provider key" value={registerKey} onChange={(e) => setRegisterKey(e.target.value)} required />
        <TextField label="Display name" value={registerName} onChange={(e) => setRegisterName(e.target.value)} required />
        <Button type="submit" disabled={busy}>Register</Button>
      </form>

      <h2 className="catalog-section-title">Bind canonical mapping</h2>
      <form
        onSubmit={(e) => { e.preventDefault(); void handleBind(); }}
        style={{ display: 'grid', gap: 12, maxWidth: 420 }}
      >
        <TextField label="Provider key" value={bindProvider} onChange={(e) => setBindProvider(e.target.value)} required />
        <TextField label="Kind (Country or Service)" value={bindKind} onChange={(e) => setBindKind(e.target.value)} required />
        <TextField label="External code" value={bindExternal} onChange={(e) => setBindExternal(e.target.value)} required />
        <TextField label="Canonical stable key" value={bindCanonical} onChange={(e) => setBindCanonical(e.target.value)} required />
        <div style={{ display: 'flex', gap: 12 }}>
          <Button buttonStyle="secondary" type="button" disabled={busy || !bindProvider || !bindExternal || !bindCanonical} onClick={() => void handlePreview()}>
            Preview
          </Button>
          <Button type="submit" disabled={busy || preview?.resolved !== true}>Bind</Button>
        </div>
        {preview ? (
          preview.resolved
            ? <Alert tone="info" title={`Resolves to: ${preview.displayName}`} politeness="polite" />
            : <Alert tone="warning" title="Canonical target not found — nothing will be saved." politeness="polite" />
        ) : null}
      </form>

      <p style={{ marginTop: 24 }}>
        <Link href="/admin">← Overview</Link>
      </p>
    </section>
  );
}
