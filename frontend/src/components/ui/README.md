# UI components

Components in this folder are implementation counterparts of approved Penpot Design System components (M01-005). Each documents its Penpot component name/variant axes and consumes only the M01-004 token bridge (`src/styles/tokens.css`) — never raw design values.

| Component | Penpot asset | Variant axes | Board ID |
|---|---|---|---|
| `Button.tsx` | `GetCode / Button Variants` | Style × State × Size | `324404a7-ad1e-8048-8008-87746e33fea2` |
| `TextField.tsx` | `GetCode / Field Variants` | State × Type | `324404a7-ad1e-8048-8008-877413864530` |
| `Tabs.tsx` | `GetCode / Tab Variants` | State | `324404a7-ad1e-8048-8008-877415456507` |
| `Badge.tsx` | `GetCode / Badge Variants` | Tone | `324404a7-ad1e-8048-8008-877417ce7eb7` |
| `ServiceRow.tsx` | `GetCode / Service Row Variants` | State | `324404a7-ad1e-8048-8008-87741a2355ea` |
| `Alert.tsx` | `GetCode / Alert Variants` | Tone | `324404a7-ad1e-8048-8008-87741e908937` |
| `SidebarItem.tsx` | `GetCode / Sidebar Item Variants` | State | `324404a7-ad1e-8048-8008-8774233749fc` |

Rules:

- Primitives are presentation-only: no API calls, no business rules, no routing logic.
- State axes map to native semantics where possible (`disabled`, `focus-visible`, `aria-selected`, `aria-current`, `aria-disabled`).
- All spacing/color/radius/typography come from `--gc-*` custom properties; RTL support uses logical CSS properties plus direction-aware arrow keys in Tabs.
- Visual regression coverage lands with the M01-007 harness; interaction and axe accessibility tests live in `frontend/tests/ui/`.
