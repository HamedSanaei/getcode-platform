import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { ADMIN_NAV_ITEMS, visibleAdminNavItems } from '../../src/components/admin/AdminShell';
import type { Principal, PrincipalState } from '../../src/lib/api/principal';

function principalWith(permissions: string[]): Principal {
  return { userId: '01900000-0000-7000-8000-000000000001', roles: ['test-role'], permissions };
}

function stubPrincipalEndpoint(state: PrincipalState): void {
  const body = state.kind === 'authenticated' ? state.principal : null;
  const status = state.kind === 'authenticated' ? 200 : 401;
  vi.stubGlobal(
    'fetch',
    vi.fn(async () => new Response(JSON.stringify(body), {
      status,
      headers: { 'content-type': 'application/json' },
    })),
  );
}

describe('admin navigation capability model (M09-001)', () => {
  it('declares a canonical capability for every admin surface', () => {
    const capabilities = new Set(ADMIN_NAV_ITEMS.map((item) => item.capability));
    // Canonical permission strings only — no hard-coded role names.
    for (const capability of capabilities) {
      expect(capability).toMatch(/^[a-z]+\.[a-z]+$/);
    }
    expect(ADMIN_NAV_ITEMS.length).toBeGreaterThanOrEqual(5);
  });

  it('hides surfaces the principal lacks and shows the ones it has', () => {
    const items = visibleAdminNavItems(principalWith(['admin.access', 'orders.read']));
    expect(items.map((i) => i.href)).toEqual(['/admin', '/admin/orders', '/admin/review']);
  });

  it('renders nothing navigable for a zero-permission authenticated user', () => {
    expect(visibleAdminNavItems(principalWith([]))).toHaveLength(0);
  });

  it('shows all declared surfaces when every capability is granted', () => {
    const everything = [...new Set(ADMIN_NAV_ITEMS.map((item) => item.capability))];
    expect(visibleAdminNavItems(principalWith(everything))).toHaveLength(ADMIN_NAV_ITEMS.length);
  });
});

describe('AdminGuard states (M09-001)', () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  beforeEach(() => {
    vi.resetModules();
  });

  it('shows a sign-in prompt to anonymous visitors instead of the shell', async () => {
    stubPrincipalEndpoint({ kind: 'anonymous' });
    const { AdminGuard } = await import('../../src/components/admin/AdminShell');
    render(<AdminGuard>content</AdminGuard>);
    // info tone renders as a polite live region; wait past the loading state
    expect(await screen.findByText(/administrator sign-in required/i)).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /sign in/i })).toHaveAttribute('href', '/auth/sign-in');
    expect(screen.queryByText('content')).not.toBeInTheDocument();
    expect(screen.queryByRole('navigation')).not.toBeInTheDocument();
  });

  it('shows permission denied for authenticated users without any capability', async () => {
    stubPrincipalEndpoint({ kind: 'authenticated', principal: principalWith([]) });
    const { AdminGuard } = await import('../../src/components/admin/AdminShell');
    render(<AdminGuard>content</AdminGuard>);
    expect(await screen.findByText(/permission denied/i)).toBeInTheDocument();
    expect(screen.queryByText('content')).not.toBeInTheDocument();
    expect(screen.queryByRole('navigation')).not.toBeInTheDocument();
  });

  it('renders the shell for capable principals', async () => {
    stubPrincipalEndpoint({ kind: 'authenticated', principal: principalWith(['admin.access']) });
    const { AdminGuard } = await import('../../src/components/admin/AdminShell');
    render(<AdminGuard>content</AdminGuard>);
    expect(await screen.findByText('مدیریت')).toBeInTheDocument();
    expect(screen.getByText('content')).toBeInTheDocument();
    expect(screen.getAllByRole('link', { name: /overview/i }).length).toBeGreaterThan(0);
  });
});
