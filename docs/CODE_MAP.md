# Code map for agents

Use this to avoid broad repository scans.

| Need | Start here |
|---|---|
| Architectural rules | `AGENTS.md`, `docs/architecture/ARCHITECTURE.md`, `docs/architecture/DECISIONS.md` |
| Toolchain/dependency baselines | `docs/architecture/TOOLCHAIN.md`, `global.json`, `Directory.Packages.props`, `frontend/package-lock.json` |
| Current work | `docs/STATUS.md`, `docs/roadmap/TASK_INDEX.md`, assigned file in `docs/tasks/` |
| Domain invariants | `backend/src/GetCode.Domain/<Capability>/` |
| Use cases / ports | `backend/src/GetCode.Application/<Capability>/` |
| EF/PostgreSQL | `backend/src/GetCode.Persistence/` (migrations in `Migrations/`) |
| Provider adapters | `backend/src/GetCode.Infrastructure/Providers/` |
| Logging/redaction/archive | `backend/src/GetCode.Infrastructure/Observability/Logging/` and `docs/architecture/OBSERVABILITY.md` |
| HTTP / host context | `backend/src/GetCode.Api/` |
| Durable background work | `backend/src/GetCode.Worker/` |
| API transport DTOs | `backend/src/GetCode.Contracts/` |
| Dependency tests | `backend/tests/GetCode.ArchitectureTests/` |
| Provider behavior tests | `backend/tests/GetCode.ProviderContractTests/` |
| UI shell / site host | `frontend/src/app/`, `frontend/src/lib/site/` |
| UI feature | `frontend/src/features/` after its task exists |
| Design source/handoff | `design/` and Penpot link in `design/penpot/README.md` |
| Deployment example | `compose.yaml`, `infrastructure/caddy/` |
| GitHub roadmap creation | `scripts/github/` |
