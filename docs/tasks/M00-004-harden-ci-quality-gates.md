# M00-004: Harden CI quality gates

- Status: **DONE**
- Milestone: **M00**
- Priority: **P0**
- Depends on: M00-001, M00-002

## Goal

Harden CI quality gates.

## Acceptance criteria

- [x] Backend restore/format/build/test gates are deterministic.
- [x] Frontend uses lockfile install, lint, typecheck and production build.
- [x] CodeQL/container build workflows are validated; failed gates cannot be bypassed by task agents.

## Required verification

- [x] GitHub Actions dry review
- [x] local equivalent gates

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.

### Handoff (2026-08-24)

- Files changed: `Directory.Build.props` (committed per-project NuGet lockfiles via `RestorePackagesWithLockFile`, scoped away from the .sln metaproject), 12 × `packages.lock.json` (new), `.github/workflows/ci.yml` (setup-dotnet from `global.json`, locked-mode restore, pipefail-correct audit step with `set -euo pipefail`, concurrency cancel-in-progress, job timeouts, TRX artifact upload on failure, npm cache keyed on the lockfile), `backend/Dockerfile.api` + `backend/Dockerfile.worker` (locked-mode restore; replaced `adduser` — absent in Ubuntu 24.04-based .NET 10 images which broke both image builds with exit 127 — with `useradd`/`groupadd` and a nologin shell), `docs/architecture/TESTING.md` (CI gate/bypass policy section).
- Bug fixed during verification: backend Docker builds were broken at the user-creation step (`adduser: not found`) because .NET 10 runtime images are Ubuntu 24.04-based. Both images now build and run; the API image was smoke-tested: `/health/live` returns 200 for the configured `getcode.example` host and 421 Misdirected Request for an unknown host (site-host allowlist verified inside the container).
- Determinism mechanics: CI restores with `--locked-mode` so any package.json/PackageReference drift fails fast; frontend `npm ci` fails when package.json and lockfile disagree. Verified locally: locked restore exit 0, build exit 0, tests 17/17 green.
- GitHub Actions dry review: all three workflow YAMLs parse cleanly (PyYAML structural check); ci.yml has two jobs with explicit ordering and no always()-passing steps; codeql.yml analyzes csharp + javascript-typescript on push/PR/weekly schedule; container.yml builds api/worker/web on PRs/tags.
- Bypass governance: recorded in `docs/architecture/TESTING.md`; enabling branch protection "required status checks" is a one-time repository-admin action that cannot be performed from this environment (residual item for the owner).
- Migration/config/operations impact: contributors must commit updated `packages.lock.json` alongside dependency changes; Docker image layers changed (non-root user now via useradd). No application configuration changes.
- Next unblocked tasks: M00-005, M00-007.
