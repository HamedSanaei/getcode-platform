import { getCurrentSite } from "@/lib/site/get-current-site";

export default async function HomePage() {
  const site = await getCurrentSite();
  return (
    <main className="shell">
      <section className="placeholder-card">
        <p className="eyebrow">GETCODE PLATFORM</p>
        <h1>رابط کاربری پس از تأیید طراحی Penpot پیاده‌سازی می‌شود.</h1>
        <p>
          این صفحه عمداً یک placeholder است تا طراحی محصول قبل از Design System و صفحات Penpot به‌صورت تصادفی در کد شکل نگیرد.
        </p>
        <code>site: {site.key}</code>
      </section>
    </main>
  );
}
