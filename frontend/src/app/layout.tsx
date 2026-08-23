import type { Metadata } from "next";
import { getCurrentSite } from "@/lib/site/get-current-site";
import "./globals.css";

export async function generateMetadata(): Promise<Metadata> {
  const site = await getCurrentSite();
  return {
    metadataBase: new URL(`https://${site.canonicalHost}`),
    title: {
      default: "GetCode",
      template: "%s | GetCode",
    },
    description: "GetCode virtual number platform",
    alternates: {
      canonical: "/",
    },
  };
}

export default async function RootLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  const site = await getCurrentSite();
  return (
    <html lang="fa" dir="rtl" data-brand={site.brandKey}>
      <body>{children}</body>
    </html>
  );
}
