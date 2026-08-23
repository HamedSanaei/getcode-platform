# M00-003: Establish local PostgreSQL/Redis development environment

- Status: **DONE**
- Milestone: **M00**
- Priority: **P0**
- Depends on: M00-001

## Goal

Establish local PostgreSQL/Redis development environment.

## Acceptance criteria

- [x] Compose starts PostgreSQL and Redis with health checks and durable local volumes.
- [x] Secrets remain local/env-only and examples are non-production.
- [x] Developer README documents reset/start/stop procedures.

## Required verification

- [x] docker compose config
- [x] health checks

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.

### Handoff (2026-08-24)

- Files changed: `compose.yaml` (PostgreSQL volume mount moved from `/var/lib/postgresql/data` to `/var/lib/postgresql` with an explanatory comment), `README.md` (Node 24 baseline, full start/stop/reset/logs procedures, dependency-baseline note replacing the resolved 2026-08-26 Next.js security warning).
- Bug fixed during verification: the starter mounted the PostgreSQL data volume at the pre-18 path. PostgreSQL 18+ Docker images refuse to start ("mount point boundary issues") and store data under major-version subdirectories of `/var/lib/postgresql`. The original mount made both fresh starts and durable restarts impossible; verified broken then fixed.
- Verification performed on this machine: `docker compose config` exit 0; `docker compose up -d postgres redis` → both containers reach `(healthy)` via their compose healthchecks; `psql SELECT version()` → PostgreSQL 18.6; `redis-cli ping/SET/GET`; `docker compose restart postgres redis` → services return to healthy and the Redis key set before restart survives (AOF + named volume), confirming durability.
- Secrets: only `${VAR:-local-default}` interpolation from `.env` (git-ignored); `.env.example` values remain non-production placeholders. No credential added to the repository or tests.
- Migration/config/operations impact: developers must run `docker compose down -v` once after pulling this change if they had created a volume with the old mount path (no production impact; local dev only).
- Residual risk: none known for local use; production deployment remains a later milestone task.
- Next unblocked tasks: M00-005 (integration-test infrastructure), M00-007 (structured logging).
