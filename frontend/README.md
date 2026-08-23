# GetCode Web

Next.js frontend. It owns presentation, SSR/SEO, host-aware branding and browser UX. Business rules remain in ASP.NET Core.

## Design rule

No production UI page starts as free-form React. The flow is:

`Requirements -> UX flow -> Penpot foundations/components/pages -> tokens/handoff -> Next.js implementation -> visual regression`

See `../design/README.md`.

## Dependency lock security gate

The starter intentionally does **not** include a generated lockfile because on 2026-08-24 Next.js had announced a security release scheduled for 2026-08-26. Task M00-001 requires selecting the patched supported 16.x version, running the package-manager audit, and committing the resulting lockfile before implementation/deployment.

After that task, CI must switch from `npm install` to `npm ci`.
