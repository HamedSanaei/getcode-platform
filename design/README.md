# GetCode UI / Penpot workflow

**Penpot is the source of truth for GetCode UI/UX.** Code is an implementation of approved designs, not a competing design source.

## Required sequence

1. Product requirement and user flow are written.
2. Foundations are defined in Penpot: color, typography, spacing, grid, radius, elevation, motion/accessibility guidance.
3. Reusable Penpot components and variants are built.
4. Page/pattern designs are composed from those components.
5. Responsive states and interaction/error/loading/empty states are designed.
6. Handoff records token/component mapping.
7. Next.js implementation is created.
8. Visual regression compares implementation against the approved reference.

## Penpot file sections

```text
00 Cover & changelog
01 Foundations
02 Components
03 Patterns
04 Public site
05 Checkout
06 Customer dashboard
07 Activation / OTP flow
08 Admin
09 Responsive & edge states
```

## Multi-domain design

There is one component system. Host differences use brand tokens, not forked page copies. `getcode` and `getcode-pluspremium` may differ in approved branding, but behavior and components stay shared unless a product decision says otherwise.

## Handoff requirement

Every implemented UI task must include:

- Penpot page/component reference;
- responsive behavior;
- states (loading/empty/error/success/disabled);
- accessibility notes;
- token mapping;
- screenshot/visual regression baseline after the test harness is introduced.
