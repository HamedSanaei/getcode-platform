'use client';

import { Alert } from '@/components/ui/Alert';
import { Badge } from '@/components/ui/Badge';
import { Button } from '@/components/ui/Button';
import { ServiceRow } from '@/components/ui/ServiceRow';
import { SidebarItem } from '@/components/ui/SidebarItem';
import { Tabs } from '@/components/ui/Tabs';
import { TextField } from '@/components/ui/TextField';
import './visual-gallery.css';

/**
 * M01-007 visual-regression fixture surface. Renders every shared primitive
 * in every documented state under both brand contexts and both directions,
 * fully statically — no dates, no randomness, no network — so screenshots are
 * byte-deterministic apart from intentional component changes.
 *
 * This page is a harness fixture, not a product route; it is excluded from
 * sitemaps/canonical concerns because it renders identical content everywhere.
 */

function Section({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <section className="vg-section" data-visual-section={title}>
      <h2>{title}</h2>
      <div className="vg-row">{children}</div>
    </section>
  );
}

const GALLERY_CONTENT = (
  <>
    <Section title="buttons">
      <Button>Primary</Button>
      <Button buttonStyle="accent">Accent</Button>
      <Button buttonStyle="secondary">Secondary</Button>
      <Button buttonStyle="ghost">Ghost</Button>
      <Button disabled>Disabled</Button>
      <Button size="sm">Small</Button>
      <Button size="lg">Large</Button>
    </Section>
    <Section title="fields">
      <TextField label="Phone number" placeholder="+98…" hint="We never show your number." />
      <TextField label="Email" type="email" defaultValue="user@example.com" />
      <TextField label="Email" type="email" defaultValue="not-an-email" error="Enter a valid email address" />
      <TextField label="Locked field" disabled defaultValue="read only value" />
      <TextField label="Compact label" hideLabel placeholder="Visually hidden label" />
    </Section>
    <Section title="tabs">
      <div className="vg-tabs">
        <Tabs
          listLabel="Gallery tabs"
          items={[
            { id: 'active', label: 'Active', content: <p>Active panel content.</p> },
            { id: 'idle', label: 'Idle', content: <p>Idle panel content.</p> },
            { id: 'third', label: 'Third', content: <p>Third panel content.</p> },
          ]}
        />
      </div>
    </Section>
    <Section title="badges">
      <Badge tone="success">Available</Badge>
      <Badge tone="warning">Slow</Badge>
      <Badge tone="danger">Down</Badge>
      <Badge tone="info">New</Badge>
      <Badge tone="brand">Popular</Badge>
      <Badge>Neutral</Badge>
    </Section>
    <Section title="alerts">
      <Alert tone="success" title="Order placed">
        Your activation is running.
      </Alert>
      <Alert tone="warning" title="Quote expiring">
        Refresh to lock this price.
      </Alert>
      <Alert tone="danger" title="Payment failed">
        Try another method.
      </Alert>
      <Alert tone="info" title="Maintenance tonight" />
    </Section>
    <Section title="service-rows">
      <ServiceRow title="Telegram" meta="IR · Activation" price="0.25 USD" badge={{ tone: 'brand', label: 'Popular' }} onSelect={() => {}} />
      <ServiceRow title="WhatsApp" meta="US · Rental" price="0.90 USD" onSelect={() => {}} />
      <ServiceRow title="Signal" meta="DE · Activation" available={false} onSelect={() => {}} />
      <ServiceRow title="Manual review" meta="Case #1042 · pending operator" badge={{ tone: 'warning', label: 'In review' }} />
    </Section>
    <Section title="sidebar">
      <nav aria-label="Gallery sidebar" className="vg-sidebar">
        <SidebarItem href="/visual-gallery" active>
          Overview
        </SidebarItem>
        <SidebarItem href="/orders">Orders</SidebarItem>
        <SidebarItem href="/wallet">Wallet</SidebarItem>
      </nav>
    </Section>
    <Section title="states">
      {/* Loading skeleton, empty and error surfaces use primitive vocabulary. */}
      <div className="vg-skeleton" aria-hidden="true" />
      <div className="vg-empty">No orders yet — your activations will appear here.</div>
      <Alert tone="danger" title="Could not load catalog">
        Retry in a moment.
      </Alert>
      <div className="vg-loading-row" aria-hidden="true">
        <span className="vg-spinner" />
        <span>Connecting…</span>
      </div>
    </Section>
  </>
);

export default function VisualGalleryPage() {
  return (
    <main className="vg-page" dir="ltr" lang="en">
      <h1>Visual regression gallery</h1>
      <p className="vg-note">Fixture surface for M01-007 — not a product route.</p>

      <div className="vg-context" data-brand="getcode">
        <h3 className="vg-context-label">Brand: getcode · LTR</h3>
        {GALLERY_CONTENT}
      </div>

      <div className="vg-context" data-brand="pluspremium">
        <h3 className="vg-context-label">Brand: pluspremium · LTR</h3>
        {GALLERY_CONTENT}
      </div>

      <div className="vg-context" dir="rtl" lang="fa">
        <h3 className="vg-context-label">RTL (default brand)</h3>
        {GALLERY_CONTENT}
      </div>
    </main>
  );
}
