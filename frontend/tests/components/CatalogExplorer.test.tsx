import { describe, expect, it } from 'vitest';
import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { CatalogExplorer } from '../../src/components/catalog/CatalogExplorer';

const services = [
  { stableKey: 'telegram', displayName: 'Telegram', displayOrder: 1 },
  { stableKey: 'whatsapp', displayName: 'WhatsApp', displayOrder: 2 },
];

const offers = [
  {
    stableKey: 'ir-telegram-activation',
    countryCode: 'IR',
    serviceSlug: 'telegram',
    countryName: 'Iran',
    serviceName: 'Telegram',
    productType: 'Activation',
  },
  {
    stableKey: 'us-whatsapp-rental',
    countryCode: 'US',
    serviceSlug: 'whatsapp',
    countryName: 'United States',
    serviceName: 'WhatsApp',
    productType: 'Rental',
  },
  // Provider-unavailable entries still appear; the card renders the unavailable state.
  {
    stableKey: 'de-signal-activation',
    countryCode: 'DE',
    serviceSlug: 'signal',
    countryName: 'Germany',
    serviceName: 'Signal',
    productType: 'Activation',
  },
];

const countries = [
  { stableKey: 'IR', displayName: 'Iran' },
  { stableKey: 'US', displayName: 'United States' },
  { stableKey: 'DE', displayName: 'Germany' },
];

function renderExplorer(overrides?: { activeCountry?: string }) {
  return render(
    <CatalogExplorer
      services={services}
      offers={offers}
      countries={countries}
      activeCountry={overrides?.activeCountry}
    />,
  );
}

describe('CatalogExplorer (M08-001)', () => {
  it('renders countries as filter chips with all-countries default', () => {
    renderExplorer();
    expect(screen.getByRole('link', { name: 'All countries' })).toHaveAttribute('href', '/numbers');
    expect(screen.getByRole('link', { name: 'Iran' })).toHaveAttribute('href', '/numbers/IR');
  });

  it('filters offers by search query and shows per-section no-result statuses', async () => {
    const user = userEvent.setup();
    renderExplorer();

    await user.type(screen.getByRole('searchbox'), 'signal');
    // Signal has no service entry but its offer still matches by name.
    expect(screen.getByRole('list', { name: 'Available numbers' })).toHaveTextContent('Signal');

    await user.clear(screen.getByRole('searchbox'));
    await user.type(screen.getByRole('searchbox'), 'zzz-nothing');
    // When nothing at all matches, a single combined no-results status shows.
    const statuses = screen.getAllByRole('status');
    expect(statuses).toHaveLength(1);
    expect(statuses[0]).toHaveTextContent(/no results/i);
  });

  it('shows the load-more control only while hidden offers remain', async () => {
    const manyOffers = Array.from({ length: 15 }, (_, i) => ({
      ...offers[0],
      stableKey: `offer-${i}`,
    }));
    const user = userEvent.setup();
    render(<CatalogExplorer services={[]} offers={manyOffers} countries={countries} />);

    const loadMore = screen.getByRole('button', { name: /load more/i });
    expect(loadMore).toHaveTextContent('3 remaining');
    await user.click(loadMore);
    expect(screen.queryByRole('button', { name: /load more/i })).not.toBeInTheDocument();
  });

  it('scopes available offers to the active country chip target', () => {
    renderExplorer({ activeCountry: 'US' });
    const offerList = within(screen.getByRole('list', { name: 'Available numbers' }));
    expect(offerList.getByText('WhatsApp')).toBeInTheDocument();
    expect(offerList.queryByText('Telegram')).not.toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'United States' })).toHaveAttribute('data-active', 'true');
  });
});
