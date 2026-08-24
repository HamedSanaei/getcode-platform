import type { ReactNode } from 'react';
import Link from 'next/link';
import { BottomNav } from './BottomNav';

/**
 * Pattern · Authenticated App Shell (`GetCode · 10 Responsive & States`).
 * Desktop header: `Header / GetCode / Navigation / Header Desktop`.
 * Mobile bottom navigation: `Bottom Nav / GetCode / Navigation / Bottom Mobile`.
 * Structure is identical on both hosts — the brand context comes from the
 * `data-brand` attribute set by the root layout (M01-004/M01-006 contract).
 */
export function AppShell({ children }: { children: ReactNode }) {
  return (
    <div className="gc-shell">
      <header className="gc-header">
        <Link href="/" className="gc-header__brand" aria-label="GetCode home">
          <span className="gc-header__logo" aria-hidden="true" />
          <span className="gc-header__title">GetCode</span>
        </Link>
        <nav className="gc-header__nav" aria-label="Main">
          <a href="/orders">Orders</a>
          <a href="/wallet">Wallet</a>
          <a href="/account">Account</a>
        </nav>
      </header>
      <main className="gc-shell__main">{children}</main>
      <BottomNav />
    </div>
  );
}
