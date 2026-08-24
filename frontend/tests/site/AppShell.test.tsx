import { describe, expect, it } from 'vitest';
import { render, screen } from '@testing-library/react';
import { axe } from 'jest-axe';
import { AppShell } from '../../src/components/shell/AppShell';
import { BottomNav } from '../../src/components/shell/BottomNav';

// jsdom has no Next.js router; stub usePathname via the module used by BottomNav.
vi.mock('next/navigation', () => ({
  usePathname: () => '/orders',
}));
import { vi } from 'vitest';

describe('AppShell (M01-006)', () => {
  it('renders desktop header navigation and mobile bottom navigation landmarks', () => {
    render(
      <AppShell>
        <p>Page content</p>
      </AppShell>,
    );

    expect(screen.getByRole('banner')).toBeTruthy();
    expect(screen.getByRole('navigation', { name: 'Main' })).toBeTruthy();
    expect(screen.getByRole('navigation', { name: 'Primary' })).toBeTruthy();
    expect(screen.getByRole('main')).toHaveTextContent('Page content');
    expect(screen.getAllByRole('link', { name: /GetCode home|Orders|Wallet|Account|Home/ }).length).toBeGreaterThan(0);
  });

  it('BottomNav marks the active route with aria-current', () => {
    render(<BottomNav />);
    expect(screen.getByRole('link', { name: 'Orders' })).toHaveAttribute('aria-current', 'page');
    expect(screen.getByRole('link', { name: 'Home' })).not.toHaveAttribute('aria-current');
  });

  it('has no accessibility violations', async () => {
    const { container } = render(
      <AppShell>
        <p>Content</p>
      </AppShell>,
    );
    expect(await axe(container)).toHaveNoViolations();
  });
});
