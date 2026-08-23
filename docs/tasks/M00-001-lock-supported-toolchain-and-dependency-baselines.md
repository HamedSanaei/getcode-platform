# M00-001: Lock supported toolchain and dependency baselines

- Status: **TODO**
- Milestone: **M00**
- Priority: **P0**
- Depends on: None

## Goal

Lock supported toolchain and dependency baselines.

## Acceptance criteria

- [ ] Select current patched .NET 10 SDK/runtime patch and supported Next.js 16.x patch.
- [ ] Generate and commit frontend lockfile; switch CI and Docker frontend install to deterministic lockfile install.
- [ ] Record Node/package-manager versions and run dependency security audits.
- [ ] No known critical dependency advisory is knowingly accepted without an ADR/risk record.

## Required verification

- [ ] dotnet --info / restore after SDK available
- [ ] frontend install + lint + typecheck + build
- [ ] dependency audit

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.
