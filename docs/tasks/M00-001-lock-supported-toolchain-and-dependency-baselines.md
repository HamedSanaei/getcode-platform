# M00-001: Lock supported toolchain and dependency baselines

- Status: **DONE**
- Milestone: **M00**
- Priority: **P0**
- Depends on: None

## Goal

Lock supported toolchain and dependency baselines.

## Acceptance criteria

- [x] Select current patched .NET 10 SDK/runtime patch and supported Next.js 16.x patch.
- [x] Generate and commit frontend lockfile; switch CI and Docker frontend install to deterministic lockfile install.
- [x] Record Node/package-manager versions and run dependency security audits.
- [x] No known critical dependency advisory is knowingly accepted without an ADR/risk record.

## Required verification

- [x] dotnet --info / restore after SDK available
- [x] frontend install + lint + typecheck + build
- [x] dependency audit

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.

### Handoff (2026-08-24)

- Files changed: `global.json` (SDK pinned `10.0.302`, `rollForward: latestFeature`), `Directory.Packages.props` (+ `xunit.runner.visualstudio` 4.0.0), all five `backend/tests/*.csproj` (+ adapter reference), `frontend/package.json` (Node `>=24 <25` engines, `audit` script), `frontend/package-lock.json` (new, lockfile v3), `frontend/tsconfig.json` (Next.js 16 auto-added `.next/dev/types/**/*.ts` include), `frontend/Dockerfile` (`node:24-alpine`, `npm ci`, copies `package-lock.json`), `.github/workflows/ci.yml` (node 24, `npm ci`, NuGet vulnerable-package gate, `npm audit --audit-level=high`), `backend/tests/GetCode.IntegrationTests/ApiLivenessTests.cs` (xUnit1051 root-cause fix), `docs/architecture/TOOLCHAIN.md` (new baseline record).
- Decisions: kept npm as the frontend package manager (already used by CI/Docker; no new tool); standardized the whole toolchain on Node 24 LTS (the only locally installed Node; verified end-to-end) replacing node 22 in CI/Docker; locked Next.js at `16.3.2` (current `latest` dist-tag, supersedes the pre-2026-08-26 baseline named in `START_HERE.md`; npm audit reports zero advisories).
- Baseline audit results (2026-08-24): `dotnet list package --vulnerable --include-transitive` → none; `--deprecated` → none; `npm audit` → 0 advisories across all severities. No waiver needed, so no risk ADR required.
- Commands run: `dotnet restore GetCode.sln`, `dotnet build GetCode.sln -c Release`, `dotnet test GetCode.sln -c Release` (11/11 green), `dotnet format --verify-no-changes` (green), frontend install/lint/typecheck/build, `npm audit`, Docker web image build + smoke run (HTTP 200). Fixes required to make gates truthful: (1) pre-existing xUnit1051 analyzer error in `GetCode.IntegrationTests/ApiLivenessTests.cs` — root-cause fix using `TestContext.Current.CancellationToken`; (2) xunit.v3 projects had no VSTest adapter so `dotnet test` silently ran zero tests — added `xunit.runner.visualstudio` 4.0.0 centrally and to all five test projects; no analyzer suppression or test weakening.
- Migration/config/operations impact: Docker web image now builds from `node:24-alpine` with deterministic `npm ci`; CI uses Node 24. No database or runtime configuration changes.
- Residual risk: GitHub Actions runners resolve their own .NET SDK via `dotnet-version: '10.0.x'`; `global.json` keeps feature-band flexibility so runner patch drift cannot break CI. Next patch releases should follow `docs/architecture/TOOLCHAIN.md` upgrade policy.
- Next unblocked tasks: M00-002 (depends on M00-001) and M00-003.
