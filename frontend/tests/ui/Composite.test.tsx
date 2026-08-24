import { describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { axe } from 'jest-axe';
import { Badge } from '../../src/components/ui/Badge';
import { Alert } from '../../src/components/ui/Alert';
import { ServiceRow } from '../../src/components/ui/ServiceRow';
import { SidebarItem } from '../../src/components/ui/SidebarItem';

describe('Badge (GetCode / Badge Variants)', () => {
  it('renders every tone without accessibility violations', async () => {
    const { container } = render(
      <div>
        <Badge tone="success">Available</Badge>
        <Badge tone="warning">Slow</Badge>
        <Badge tone="danger">Down</Badge>
        <Badge tone="info">New</Badge>
        <Badge tone="brand">Hot</Badge>
        <Badge>Default</Badge>
      </div>,
    );
    expect(screen.getByText('Available')).toHaveClass('gc-badge--success');
    expect(await axe(container)).toHaveNoViolations();
  });
});

describe('Alert (GetCode / Alert Variants)', () => {
  it('danger/warning are assertive live regions; success/info are polite', () => {
    render(
      <div>
        <Alert tone="danger" title="Payment failed">
          Try another method.
        </Alert>
        <Alert tone="warning" title="Quote expiring" />
        <Alert tone="success" title="Order placed" />
        <Alert tone="info" title="Maintenance tonight" />
      </div>,
    );

    // Alert/status roles are unnamed by ARIA; assert role + text instead.
    const alerts = screen.getAllByRole('alert');
    expect(alerts).toHaveLength(2);
    expect(alerts.map((el) => el.textContent)).toEqual(['Payment failedTry another method.', 'Quote expiring']);

    const statuses = screen.getAllByRole('status');
    expect(statuses).toHaveLength(2);
    expect(statuses[0].textContent).toContain('Order placed');
    expect(statuses[1].textContent).toContain('Maintenance tonight');
  });

  it('has no accessibility violations', async () => {
    const { container } = render(<Alert tone="info" title="Info">Body text</Alert>);
    expect(await axe(container)).toHaveNoViolations();
  });
});

describe('ServiceRow (GetCode / Service Row Variants)', () => {
  it('available rows are pressable; unavailable rows expose disabled state and block clicks', async () => {
    const onSelect = vi.fn();
    render(
      <div>
        <ServiceRow title="Telegram" meta="IR · Activation" price="0.25 USD" badge={{ tone: 'brand', label: 'Popular' }} onSelect={onSelect} />
        <ServiceRow title="WhatsApp" meta="US · Rental" available={false} onSelect={onSelect} />
        <ServiceRow title="Signal" meta="DE · Activation" price="0.40 USD" />
      </div>,
    );

    const telegram = screen.getByRole('button', { name: /Telegram/ });
    await userEvent.click(telegram);
    expect(onSelect).toHaveBeenCalledOnce();

    const whatsapp = screen.getByRole('button', { name: /WhatsApp/ });
    expect(whatsapp).toBeDisabled();
    expect(whatsapp).toHaveAttribute('aria-disabled', 'true');
    await userEvent.click(whatsapp);
    expect(onSelect).toHaveBeenCalledOnce(); // unchanged

    // Informational row renders without button semantics.
    expect(screen.queryByRole('button', { name: /Signal/ })).toBeNull();
  });

  it('has no accessibility violations in both states', async () => {
    const { container } = render(
      <div>
        <ServiceRow title="Telegram" price="0.25 USD" onSelect={() => {}} />
        <ServiceRow title="WhatsApp" available={false} />
      </div>,
    );
    expect(await axe(container)).toHaveNoViolations();
  });
});

describe('SidebarItem (GetCode / Sidebar Item Variants)', () => {
  it('marks the active item with aria-current', () => {
    render(
      <nav aria-label="Main">
        <SidebarItem href="/app/orders" active>
          Orders
        </SidebarItem>
        <SidebarItem href="/app/wallet">Wallet</SidebarItem>
      </nav>,
    );
    const active = screen.getByRole('link', { name: 'Orders' });
    expect(active).toHaveAttribute('aria-current', 'page');
    expect(screen.getByRole('link', { name: 'Wallet' })).not.toHaveAttribute('aria-current');
  });

  it('has no accessibility violations', async () => {
    const { container } = render(
      <nav aria-label="Main">
        <SidebarItem href="/app/orders" active>
          Orders
        </SidebarItem>
        <SidebarItem href="/app/wallet">Wallet</SidebarItem>
      </nav>,
    );
    expect(await axe(container)).toHaveNoViolations();
  });
});
