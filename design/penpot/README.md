# Penpot source

Penpot remains the collaborative UI source of truth. Do not export a `.penpot` file into Git on every design edit; Git stores handoff contracts and approved token snapshots.

- Workspace/team ID: `502b4555-3f5f-807a-8008-85a72154af8c`
- File: [New File 1 — GetCode pages](https://design.penpot.app/#/workspace?team-id=502b4555-3f5f-807a-8008-85a72154af8c&file-id=c269caa0-e456-818c-8008-85a77340be64&page-id=324404a7-ad1e-8048-8008-87726817b6ab&layout=layers)
- File ID: `c269caa0-e456-818c-8008-85a77340be64`
- GetCode cover page ID: `324404a7-ad1e-8048-8008-87726817b6ab`
- Owner: Hamed / current connected Penpot team
- Design-system version: `1.1.0`
- Detailed board-to-feature mapping: [`../handoff/PENPOT_PAGE_MAP.md`](../handoff/PENPOT_PAGE_MAP.md)

The shared file also contains pre-existing Directam work. GetCode-owned pages are isolated by the `GetCode ·` page prefix and must not modify or depend on the unrelated pages.

## Reference provenance

The `Reference / Numberland Snapshot` board records both the public Numberland snapshot captured by Gridinsoft on 2025-10-10 and the live-HTML validation completed on 2026-08-24.

The live homepage was downloaded directly with curl and hashed; 163 internal routes across 20 route families were extracted, and 17 representative pages were downloaded successfully. The objective HTML/CSS structure was compared with the Penpot sitemap, product patterns, responsive contract and page map. Full evidence is recorded in [`../handoff/NUMBERLAND_LIVE_HTML_AUDIT_2026-08-24.md`](../handoff/NUMBERLAND_LIVE_HTML_AUDIT_2026-08-24.md).

Named Penpot version: `GetCode Design System v1.1 — live HTML validation`. Owner review is reserved for visual differences that cannot be objectively resolved from HTML/CSS and the preserved screenshot; it is not a blocker for already-satisfied M01-002 or M01-003 criteria.
