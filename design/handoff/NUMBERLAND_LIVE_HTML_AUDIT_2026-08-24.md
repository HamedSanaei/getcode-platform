# Numberland live HTML structural audit — 2026-08-24

This record validates the M01 Penpot structure against live HTML downloaded directly from `https://numberland.ir/`. It is a structural/product-flow audit, not a claim that GetCode must copy Numberland branding or business rules.

## Evidence

- The homepage was downloaded successfully with `curl --fail --location --compressed` on 2026-08-24.
- Local audit artifact: `C:\Users\Hamed\AppData\Local\Temp\getcode-numberland-audit\numberland-home.html`.
- Homepage SHA-256: `0435FBE365803A03AD7AB4D62A1B839300FC3266A6842E39B242BF964D51B857`.
- Downloaded homepage size: `874109` bytes on disk.
- The live document declares `lang="fa"`, a mobile viewport, a canonical homepage, desktop CSS and a dedicated mobile stylesheet.
- Seventeen representative pages discovered from homepage links were downloaded with HTTP `200`. The eSIM URL redirected to its canonical `/international-sim/esim-data` route.
- Raw live HTML is not committed because it is volatile and contains request-scoped values such as a CSRF token. This audit preserves the reproducible URL, hash and extracted contract instead.

Representative downloads:

| Family | Live representative |
|---|---|
| Virtual-number service | `/service/telegram` |
| Country catalog | `/country/usa` |
| AI account | `/artificial-intelligence/account`, `/account/openai` |
| Premium account | `/application/account` |
| Gift card | `/giftcard/apple` |
| Credit card | `/cards/virtual-visacard` |
| International SIM | `/international-sim/esim/esim-data` |
| Permanent number | `/google-voice` |
| Blog/help | `/blog/`, `/learn`, `/faq`, `/what-is-virtual-number` |
| Partner/content/legal | `/partners`, `/contact-us`, `/about-us`, `/user-rules` |

## Public route-family inventory

The homepage contains `163` unique internal routes across `20` first-segment families:

| Route family | Unique routes | GetCode disposition |
|---|---:|---|
| `/account` | 68 | Product rail is modeled; individual premium/AI product expansion is outside the current virtual-number milestone. |
| `/giftcard` | 37 | Product rail is modeled; individual gift-card commerce is outside the current virtual-number milestone. |
| `/service` | 20 | Mapped to public catalog/service selection and product detail. |
| `/country` | 13 | Mapped to country catalog/filter and product detail. |
| `/international-sim` | 7 | Product rail is modeled; SIM commerce is outside the current virtual-number milestone. |
| `/cards` | 3 | Product rail is modeled; card commerce is outside the current virtual-number milestone. |
| `/learn` | 2 | Mapped to content/help/article patterns. |
| `/` | 1 | Mapped to public home/catalog entry. |
| `/partners` | 1 | Mapped to reseller surface. |
| `/npr` | 1 | Covered by support/request patterns when enabled as a GetCode product decision. |
| `/google-voice` | 1 | Covered by permanent-number/product-detail patterns. |
| `/developers` | 1 | Link exists in HTML but is commented out; not treated as an active M01 route. |
| `/user-rules` | 1 | Mapped to legal content. |
| `/contact-us` | 1 | Mapped to contact/support. |
| `/blog` | 1 | Mapped to blog/article. |
| `/artificial-intelligence` | 1 | Product rail is modeled; individual product expansion is outside the current milestone. |
| `/application` | 1 | Product rail is modeled; individual product expansion is outside the current milestone. |
| `/about-us` | 1 | Mapped to stable content. |
| `/faq` | 1 | Mapped to FAQ/help. |
| `/what-is-virtual-number` | 1 | Mapped to article/help content. |

## Objective structural findings

- Header/navigation: RTL logo, services mega-menu, blog, help, reseller, contact/legal and login/register entry.
- Product rail: virtual numbers, AI accounts, premium accounts, credit cards, gift cards and SIM products.
- Virtual-number explorer: ordinary, rental and permanent-number tabs; service/country search and filtering; availability/price rows; buy actions.
- Trust/content: product explanation, benefits, trust/payment marks, FAQ and tutorial content.
- Account/commerce: login/register form, purchase actions, online-payment messaging, wallet-refund behavior and post-purchase activation messaging.
- Responsive evidence: the live CSS uses a dedicated `max-width: 480px` mobile sheet and desktop/tablet behavior around `1000px`; Penpot defines explicit 390/1024/1440 contracts.
- Repeated live brand colors include cyan `#009cd7`, orange `#ffb03b`, green `#32b976`, white and pale blue `#f5f8fc`. GetCode keeps the structural relationship while owning its semantic brand tokens.

## Penpot comparison

- `GetCode · 00 Cover & Sitemap` maps public browse → quote → checkout → activation/refund and keeps admin separate.
- `GetCode · 03 Patterns / Product Explorer` contains the same product-rail families and ordinary/rental/permanent-number selection pattern found in the live HTML.
- `GetCode · 04 Public Site` maps home, catalog/country/service selection and product detail in desktop and mobile forms.
- `GetCode · 05–07` map login, checkout/payment, dashboard/wallet, activation, expiry and refund states that are visible or described in the live HTML flow.
- `GetCode · 08` maps blog, article, FAQ/help, reseller, contact and legal families.
- `GetCode · 01`, `02` and `10` provide the documented token, component, RTL, accessibility and responsive contracts required by M01-002 and M01-003.

Penpot verification after adding the live-audit record:

- File revision: `104`.
- Validation errors: `0`.
- Named version: `GetCode Design System v1.1 — live HTML validation`.
- Updated board: `Reference / Numberland Snapshot` — `324404a7-ad1e-8048-8008-87793f2b5f71`.

## M01 verdict

| Task | Verdict | Basis |
|---|---|---|
| M01-001 | `DONE` | All three acceptance criteria are satisfied and the live route/flow structure is mapped. |
| M01-002 | `DONE` | Its three acceptance criteria and both required reviews were already satisfied; browser automation is irrelevant to this task. |
| M01-003 | `DONE` | Its three acceptance criteria and component/variant review were already satisfied; browser automation is irrelevant to this task. |

Owner review remains useful only for visual differences that HTML/CSS plus the preserved screenshot cannot settle—for example exact rendering, image crops or subjective density. Such review does not reopen M01-002 or M01-003 and is not an undocumented blocker for M01-001.
