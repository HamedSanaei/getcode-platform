# Multi-domain architecture

## Hosts

GetCode serves the same application on:

1. an independent domain (TBD);
2. `vnumber.pluspremium.ir`.

They share users, orders, wallets, catalog and providers. This is not tenant isolation.

## Host resolution

Edge validates accepted hosts and routes both to the same runtime. API resolves a `SiteDescriptor`; frontend resolves a site config. Business services receive an abstract public URL/site context only when they genuinely need it.

## Browser API

Preferred browser path is same-origin `/api/*`, routed at the edge to ASP.NET Core. This reduces CORS complexity and allows secure host-scoped cookie/session design.

## Authentication

A cookie issued on the independent root domain cannot be shared with `pluspremium.ir`. Each host can maintain its own secure host-scoped session backed by the same identity. If seamless cross-domain SSO is required, use a central OAuth/OIDC-style redirect/token exchange flow in a later task.

ADR-015 resolves this for v1: no seamless SSO. Sessions are per-host over the shared identity (one account, many independent sessions; cross-host token replay is refused). The UX contract and the OIDC/OAuth requirements that any future SSO must satisfy are recorded in ADR-015; implementation stays gated behind M11-005 product approval.

## Payment return URLs

Do not trust arbitrary return URLs from the browser. Persist/resolve the originating site key and choose a configured allow-listed public base URL after payment verification.

## SEO

One host is canonical for duplicate public content. Canonical host is configurable until the independent production domain/product SEO strategy is finalized. Host-specific pages are allowed only if they provide genuinely distinct content and an explicit SEO decision.
