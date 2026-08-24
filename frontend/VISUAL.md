# Visual regression harness (M01-007)

Playwright-based screenshot regression for the shared UI primitives and the
site shell. The fixture surface is `/visual-gallery` — a deterministic page
rendering every primitive in every documented state under both brand contexts
(`getcode`, `pluspremium`) and an RTL context.

## Determinism contract

- **Viewports:** desktop 1440×900, mobile 390×844, DPR 1 (byte-stable scaling).
- **Animations/transitions:** disabled by `expect.toHaveScreenshot.animations`
  plus `reducedMotion` semantics; the fixture itself has no timed animation.
- **Caret** hidden in inputs; fonts awaited via `document.fonts.ready`.
- The gallery renders no dates, randomness or network data.
- Tolerance: `maxDiffPixels: 24`, `threshold: 0.2` — tight enough to catch real
  drift, loose enough to absorb antialiasing noise across minor GPU/driver
  differences.

## Naming and storage conventions

```
frontend/tests/visual/baselines/            ← committed to git (the truth)
  visual-gallery--full--desktop--ltr.png
  visual-gallery--brand-pluspremium--mobile--ltr.png
  visual-gallery--full--desktop--rtl.png
  visual-gallery--section-buttons--mobile--ltr.png
  …(22 baselines: full×3 + section×8, per project)
frontend/test-results/                      ← failure artifacts (gitignored):
  <test>/{expected,actual,diff}.png + trace.zip
frontend/playwright-report/                 ← HTML report (gitignored)
```

Pattern: `visual-gallery--<what>--<project>--<dir>.png`.

## Commands

```sh
npm run visual:test    # compare against committed baselines (CI gate)
npm run visual:update  # regenerate baselines — REVIEW REQUIRED, see below
```

## Baseline update procedure (requires explicit review)

1. Run `npm run visual:update`; inspect every generated PNG in the diff view of
   your VCS. Each changed baseline must be explainable by an intentional,
   reviewed change (component change, token regeneration, viewport addition).
2. Never commit baselines you cannot explain. If a diff appears without a
   corresponding code/design change, treat it as a rendering-environment
   difference, not truth: fix determinism instead of updating.
3. Commit updated baselines in the same commit as the component change that
   caused them, with the reason in the message.

## Platform authority

CI (`ubuntu-latest`) is the authoritative rendering platform: it runs
`visual:test` on every push against committed baselines. Local Windows/macOS
runs are advisory — font rasterization differs per OS, so only update baselines
from CI artifacts if your local platform differs.

## Baseline provenance vs Penpot

The committed baselines encode the current implementation output of the
Penpot-derived primitives (design rev 104). They provide **regression
protection**: any unintended visual drift fails CI immediately. They are not
yet design-truth evidence: capturing approved Penpot output side-by-side and
approving pixel parity requires live Penpot export, which is tracked as the
externally blocked remainder of M01-007. When Penpot access returns:

1. Export each mapped board from `GetCode Design System v1.1 — live HTML
   validation` at the same viewports;
2. Place exports under `frontend/tests/visual/penpot-reference/`;
3. Compare against harness captures; reconcile token/component gaps;
4. Record approval in this file and the task handoff.
