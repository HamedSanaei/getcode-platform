'use client';

import { useEffect, useState } from 'react';
import Link from 'next/link';
import { Alert } from '@/components/ui/Alert';
import { SidebarItem } from '@/components/ui/SidebarItem';
import { loadPrincipal, type Principal, type PrincipalState } from '@/lib/api/principal';
import './admin.css';

/**
 * M09-001 admin navigation model. Each entry declares the canonical
 * capability it surfaces (Penpot `GetCode · 09 Admin` boards). Entries are
 * hidden when the principal lacks the capability — UX only: the matching API
 * routes enforce the same capability server-side regardless of this UI.
 */
export interface AdminNavItem {
  href: string;
  label: string;
  capability: string;
}

export const ADMIN_NAV_ITEMS: AdminNavItem[] = [
  { href: '/admin', label: 'Overview', capability: 'admin.access' },
  { href: '/admin/providers', label: 'Provider operations', capability: 'providers.manage' },
  { href: '/admin/catalog-mapping', label: 'Catalog mapping', capability: 'providers.manage' },
  { href: '/admin/pricing', label: 'Pricing', capability: 'pricing.manage' },
  { href: '/admin/orders', label: 'Orders & refunds', capability: 'orders.read' },
  { href: '/admin/review', label: 'Manual review', capability: 'orders.read' },
];

export function visibleAdminNavItems(principal: Principal): AdminNavItem[] {
  return ADMIN_NAV_ITEMS.filter((item) => principal.permissions.includes(item.capability));
}

/**
 * UX guard for /admin/*. Server policies remain the boundary; this component
 * only renders the right shell states (loading / anonymous / denied / shell).
 */
export function AdminGuard({ children }: { children: React.ReactNode }) {
  const [state, setState] = useState<PrincipalState>({ kind: 'loading' });

  useEffect(() => {
    let active = true;
    loadPrincipal().then((result) => {
      if (active) {
        setState(result);
      }
    });
    return () => {
      active = false;
    };
  }, []);

  if (state.kind === 'loading') {
    return (
      <div className="admin-shell" role="status">
        <div className="vg-skeleton" aria-hidden="true" />
        <p>Checking your session…</p>
      </div>
    );
  }

  if (state.kind === 'anonymous') {
    return (
      <main className="shell">
        <Alert tone="info" title="Administrator sign-in required">
          <Link href="/auth/sign-in">Sign in</Link> with an administrator account to open this area.
        </Alert>
      </main>
    );
  }

  const items = visibleAdminNavItems(state.principal);
  if (!items.some((item) => item.href === '/admin')) {
    return (
      <main className="shell">
        <Alert tone="danger" title="Permission denied">
          Your account does not have administrator capabilities.
        </Alert>
      </main>
    );
  }

  return (
    <div className="admin-shell" dir="rtl" lang="fa">
      <aside className="admin-sidebar">
        <p className="admin-sidebar-title">مدیریت</p>
        <nav className="admin-nav" aria-label="Admin sections">
          {items.map((item) => (
            <SidebarItem key={item.href} href={item.href}>
              {item.label}
            </SidebarItem>
          ))}
        </nav>
      </aside>
      <section className="admin-content">{children}</section>
    </div>
  );
}
