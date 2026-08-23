# M00-002: Enforce clean dependency and module boundaries

- Status: **DONE**
- Milestone: **M00**
- Priority: **P0**
- Depends on: M00-001

## Goal

Enforce clean dependency and module boundaries.

## Acceptance criteria

- [x] Architecture tests enforce Domain/Application forbidden references.
- [x] Document module ownership and cross-module write rule.
- [x] CI fails on a demonstrated forbidden reference test fixture or equivalent verification.

## Required verification

- [x] architecture tests
- [x] full backend build

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.

### Handoff (2026-08-24)

- Files changed: `backend/tests/GetCode.ArchitectureTests/LayerDependencyRules.cs` (new — six IL-based NetArchTest rules), `GetCode.ArchitectureTests.csproj` (+ references to Contracts/Persistence/Infrastructure so they can be policed), `Directory.Packages.props` (+ NetArchTest.Rules 1.3.2), `docs/architecture/BOUNDARIES.md` (layer contract diagram + rule→test enforcement map; ownership/cross-module write rules already existed and were kept).
- Decisions: adopted NetArchTest.Rules for IL-level policy checks — reflection-only assembly checks cannot see usage inside method bodies, which is exactly where layering erodes; the existing reflection-based `DependencyDirectionTests` are kept as belt-and-braces. Domain additionally forbids `System.Net.Http` (no HTTP clients in domain per AGENTS.md) and `Microsoft.Extensions.Logging`.
- Demonstration (required verification): a temporary `GetCode.Domain.ForbiddenReferenceProbe` using `HttpClient` compiled and made `Domain_depends_only_on_the_BCL` fail with exit code 1, naming the offending type; fixture removed and suite returned green. A Serilog probe was blocked even earlier by the missing project/package reference.
- Commands run: `dotnet test GetCode.ArchitectureTests` (8/8 green after fixture removal), full solution build + test run re-checked in this change set.
- Migration/config/operations impact: none.
- Residual risk: none known; future provider adapters must keep SDK types under `Infrastructure/Providers/<Name>` or the ACL test fails CI.
- Next unblocked tasks: M00-003, M00-007 (both depend only on M00-001).
