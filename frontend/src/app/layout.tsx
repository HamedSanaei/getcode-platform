import type { Metadata } from 'next';
import { getCurrentSite } from '@/lib/site/get-current-site';
import { AppShell } from '@/components/shell/AppShell';
import './globals.css';
import '@/components/shell/shell.css';

export async function generateMetadata(): Promise<Metadata> {
  const site = await getCurrentSite();

  // Canonical URLs are built exclusively from the configured canonical host
  // (env), never from the request — mirrors/preview hosts cannot hijack SEO
  // and there is no open-redirect surface. Unknown hosts stay out of indexes.
  const robots = site.hostKnown ? undefined : { index: false as const, follow: false as const };

  return {
    metadataBase: new URL(`https://${site.canonicalHost}`),
    title: {
      default: 'GetCode',
      template: '%s | GetCode',
    },
    description: 'GetCode virtual number platform',
    alternates: {
      canonical: '/',
    },
    ...(robots ? { robots } : {}),
  };
}

export default async function RootLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  const site = await getCurrentSite();
  return (
    <html lang="fa" dir="rtl" data-brand={site.brandKey}>
      <body>
        <AppShell>{children}</AppShell>
      </body>
    </html>
  );
}
