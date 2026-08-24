import { describe, expect, it } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { axe } from 'jest-axe';
import { Tabs } from '../../src/components/ui/Tabs';

const items = [
  { id: 'activation', label: 'Activation', content: 'Activation panel' },
  { id: 'history', label: 'History', content: 'History panel' },
  { id: 'wallet', label: 'Wallet', content: 'Wallet panel' },
];

describe('Tabs (GetCode / Tab Variants)', () => {
  /** Panels render in items order; hidden ones stay out of the a11y tree so query with hidden:true. */
  const panels = () => screen.getAllByRole('tabpanel', { hidden: true });
  const panelFor = (id: string) => {
    const index = items.findIndex((item) => item.id === id);
    return panels()[index];
  };

  it('shows the active panel and hides the rest with correct ARIA wiring', () => {
    render(<Tabs items={items} listLabel="Account sections" />);

    const selected = screen.getByRole('tab', { name: 'Activation' });
    expect(selected).toHaveAttribute('aria-selected', 'true');
    expect(panels()).toHaveLength(3);

    expect(panelFor('activation')).not.toHaveAttribute('hidden');
    expect(panelFor('history')).toHaveAttribute('hidden');
    expect(panelFor('wallet')).toHaveAttribute('hidden');
  });

  it('implements the roving tabindex and arrow-key navigation', async () => {
    render(<Tabs items={items} listLabel="Account sections" />);
    const tabs = screen.getAllByRole('tab');
    expect(tabs[0]).toHaveAttribute('tabindex', '0');
    expect(tabs[1]).toHaveAttribute('tabindex', '-1');

    tabs[0].focus();
    await userEvent.keyboard('{ArrowRight}');
    expect(screen.getByRole('tab', { name: 'History' })).toHaveFocus();
    expect(screen.getByRole('tab', { name: 'History' })).toHaveAttribute('aria-selected', 'true');
    expect(panelFor('history')).not.toHaveAttribute('hidden');

    await userEvent.keyboard('{End}');
    expect(screen.getByRole('tab', { name: 'Wallet' })).toHaveFocus();

    await userEvent.keyboard('{Home}');
    expect(screen.getByRole('tab', { name: 'Activation' })).toHaveFocus();
  });

  it('mirrors arrow semantics in right-to-left hosts', async () => {
    const { container } = render(
      <div dir="rtl">
        <Tabs items={items} listLabel="Account sections" />
      </div>,
    );
    // jsdom does not compute direction from ancestors, so force it on the tablist.
    container.querySelector<HTMLDivElement>('[role="tablist"]')!.style.direction = 'rtl';

    screen.getAllByRole('tab')[0].focus();
    await userEvent.keyboard('{ArrowRight}');
    // In RTL, ArrowRight moves to the previous tab.
    expect(screen.getByRole('tab', { name: 'Wallet' })).toHaveFocus();
  });

  it('has no accessibility violations', async () => {
    const { container } = render(<Tabs items={items} listLabel="Account sections" />);
    expect(await axe(container)).toHaveNoViolations();
  });
});
