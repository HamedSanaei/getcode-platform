# GetCode Penpot page and implementation map

Canonical file: [Penpot — GetCode cover](https://design.penpot.app/#/workspace?team-id=502b4555-3f5f-807a-8008-85a72154af8c&file-id=c269caa0-e456-818c-8008-85a77340be64&page-id=324404a7-ad1e-8048-8008-87726817b6ab&layout=layers)

This map is the stable handoff contract between Penpot and the Next.js tasks. Implementations must cite the Penpot page and board name/ID they map, reuse named library assets, and cover the responsive/state requirements listed here.

## Foundations and reusable assets

| Penpot page | Page ID | Contract |
|---|---|---|
| `GetCode · 00 Cover & Sitemap` | `324404a7-ad1e-8048-8008-87726817b6ab` | Sitemap, critical flows, source-reference provenance |
| `GetCode · 01 Foundations` | `324404a7-ad1e-8048-8008-8772681cdddf` | Color, type, spacing, radius, grid, accessibility and RTL rules |
| `GetCode · 02 Components` | `324404a7-ad1e-8048-8008-87726820902a` | Buttons, fields, tabs, badges, service rows, alerts, country cards, header, bottom nav and sidebar |
| `GetCode · 03 Patterns` | `324404a7-ad1e-8048-8008-87726823fd52` | Product Explorer, Quote/Checkout and authenticated shell compositions |
| `GetCode · 10 Responsive & States` | `324404a7-ad1e-8048-8008-8772683b0884` | 1440/1024/390 breakpoints, state matrix, two-brand token contract and handoff gate |

Token sets:

- `GetCode/Core`: 53 primitive and semantic tokens.
- `GetCode/Brand/GetCode`: 7 active GetCode brand tokens.
- `GetCode/Brand/PlusPremium`: 7 alternate brand tokens; components are not forked.

Variant groups:

- `GetCode / Button Variants` — `Style`, `State`, `Size` — board `324404a7-ad1e-8048-8008-87746e33fea2`.
- `GetCode / Field Variants` — `State`, `Type` — board `324404a7-ad1e-8048-8008-877413864530`.
- `GetCode / Tab Variants` — `State` — board `324404a7-ad1e-8048-8008-877415456507`.
- `GetCode / Badge Variants` — `Tone` — board `324404a7-ad1e-8048-8008-877417ce7eb7`.
- `GetCode / Service Row Variants` — `State` — board `324404a7-ad1e-8048-8008-87741a2355ea`.
- `GetCode / Alert Variants` — `Tone` — board `324404a7-ad1e-8048-8008-87741e908937`.
- `GetCode / Sidebar Item Variants` — `State` — board `324404a7-ad1e-8048-8008-8774233749fc`.

## Product surfaces

| Surface / likely route | Penpot page | Boards (name — ID) | Required states |
|---|---|---|---|
| `/`, catalog entry | `GetCode · 04 Public Site` | `Public / Home / Desktop` — `324404a7-ad1e-8048-8008-8775af48b096`; `Public / Home / Mobile` — `324404a7-ad1e-8048-8008-8775ced5ead6` | loading, empty, search, selected service, unavailable service |
| `/numbers`, `/numbers/[country]` | `GetCode · 04 Public Site` | `Public / Catalog / Desktop` — `324404a7-ad1e-8048-8008-8775bb814780`; `Public / Catalog / Mobile` — `324404a7-ad1e-8048-8008-8775d38d8fc2` | filters, no results, pagination/load more, provider-unavailable fallback |
| `/numbers/[country]/[service]` | `GetCode · 04 Public Site` | `Public / Product Detail / Desktop` — `324404a7-ad1e-8048-8008-8775c5f49319`; `Public / Product Detail / Mobile` — `324404a7-ad1e-8048-8008-8775de22f8ee` | available, unavailable, quote refresh, related offers |
| `/auth/*` | `GetCode · 05 Auth & Checkout` | `Auth / Login / Desktop` — `324404a7-ad1e-8048-8008-8776b27352cb`; `Auth / OTP / Mobile` — `324404a7-ad1e-8048-8008-8776b3100eb1` | default, invalid phone, invalid/expired OTP, resend timer, loading/disabled |
| `/checkout` | `GetCode · 05 Auth & Checkout` | `Checkout / Desktop` — `324404a7-ad1e-8048-8008-8776b3a59fe4`; `Checkout / Mobile` — `324404a7-ad1e-8048-8008-8776b4fd84a1`; `Payment / Results` — `324404a7-ad1e-8048-8008-8776b4a88636` | quote expiry, wallet/redirect payment, pending, success, failure, duplicate-submit guard |
| `/app` | `GetCode · 06 Customer Dashboard` | `Customer / Dashboard / Desktop` — `324404a7-ad1e-8048-8008-87771b92e52d`; `Customer / Dashboard / Mobile` — `324404a7-ad1e-8048-8008-87775e744118` | KPI loading, empty orders, partial async progress, API error |
| `/app/orders` | `GetCode · 06 Customer Dashboard` | `Customer / Orders / Desktop` — `324404a7-ad1e-8048-8008-877721e38492` | active, received, expired, failed, refunded, manual review |
| `/app/wallet` | `GetCode · 06 Customer Dashboard` | `Customer / Wallet / Desktop` — `324404a7-ad1e-8048-8008-8777502646d0` | loading, empty history, debit, credit, refund, pagination |
| `/app/settings`, `/app/support` | `GetCode · 06 Customer Dashboard` | `Customer / Settings & Support / Desktop` — `324404a7-ad1e-8048-8008-8777575f8e75` | field validation, save success/error, ticket empty/loading/success |
| `/app/orders/[id]/activation` | `GetCode · 07 Activation & OTP` | `Activation / Live / Desktop` — `324404a7-ad1e-8048-8008-8777982e27e7`; `Activation / Live / Mobile` — `324404a7-ad1e-8048-8008-8777a024aeea`; `Activation / State Gallery` — `324404a7-ad1e-8048-8008-87779d42c9e6` | reserving, waiting, received, expired, refunded, manual review |
| `/blog`, `/blog/[slug]` | `GetCode · 08 Content & Support` | `Content / Blog / Desktop` — `324404a7-ad1e-8048-8008-8777fea99ecd`; `Content / Article / Desktop` — `324404a7-ad1e-8048-8008-8778088587c9`; `Content / Blog / Mobile` — `324404a7-ad1e-8048-8008-87783f2fd764` | loading, empty category, search, article not found |
| `/help`, `/faq` | `GetCode · 08 Content & Support` | `Content / Help & FAQ / Desktop` — `324404a7-ad1e-8048-8008-87780f585066`; `Content / Help / Mobile` — `324404a7-ad1e-8048-8008-8778421740ed` | search, expanded/collapsed FAQ, no result, ticket CTA |
| `/reseller` | `GetCode · 08 Content & Support` | `Content / Reseller / Desktop` — `324404a7-ad1e-8048-8008-877815d6b755` | form default, validation, submit loading, success/error |
| `/contact`, `/about`, legal routes | `GetCode · 08 Content & Support` | `Content / Contact & Legal / Desktop` — `324404a7-ad1e-8048-8008-87781b590aeb` | stable content, not-found and unavailable contact channel |
| `/admin/*` | `GetCode · 09 Admin` | Overview `324404a7-ad1e-8048-8008-877889b74e32`; provider ops `324404a7-ad1e-8048-8008-8778939f1ad8`; catalog mapping `324404a7-ad1e-8048-8008-87789ca603d0`; pricing `324404a7-ad1e-8048-8008-8778a7817d95`; orders/refunds `324404a7-ad1e-8048-8008-8778b21203f9`; mobile review `324404a7-ad1e-8048-8008-8778bc4d61bf` | permission denied, loading/empty/error, provider degradation, dangerous-action confirmation, audit/manual review |

## Verification record

- Penpot file validation errors: `0` at revision `104` on 2026-08-24.
- Named Penpot version: `GetCode Design System v1.1 — live HTML validation`.
- Variant errors: `0` across seven variant groups.
- Board containment audit: no board/container overflow after responsive reflow corrections.
- Visual spot checks: cover, foundations, auth desktop, public catalog mobile, activation-state gallery and Numberland reference board.
- Live HTML audit (2026-08-24): direct curl download succeeded; 163 internal routes in 20 families were inventoried and 17 representative pages returned HTTP 200. The objective structure maps to the current Penpot sitemap, Product Explorer, public/auth/customer/activation/content surfaces and responsive contract.
- Audit evidence: [`NUMBERLAND_LIVE_HTML_AUDIT_2026-08-24.md`](NUMBERLAND_LIVE_HTML_AUDIT_2026-08-24.md).
- Residual review: owner review is limited to visual differences that HTML/CSS and the preserved screenshot cannot objectively settle. It does not reopen M01-001, M01-002 or M01-003.
