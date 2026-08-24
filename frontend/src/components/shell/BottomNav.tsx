'use client';

import { usePathname } from 'next/navigation';

/**
 * Bottom Nav / GetCode / Navigation / Bottom Mobile — Penpot axis: active item.
 * Client component because the active marker follows the live route.
 */
const ITEMS = [
  { href: '/', label: 'Home' },
  { href: '/orders', label: 'Orders' },
  { href: '/wallet', label: 'Wallet' },
  { href: '/account', label: 'Account' },
];

export function BottomNav() {
  const pathname = usePathname();

  return (
    <nav className="gc-bottom-nav" aria-label="Primary">
      {ITEMS.map((item) => {
        const active = item.href === '/' ? pathname === '/' : pathname.startsWith(item.href);
        return (
          <a
            key={item.href}
            href={item.href}
            className="gc-bottom-nav__item"
            aria-current={active ? 'page' : undefined}
          >
            {item.label}
          </a>
        );
      })}
    </nav>
  );
}
