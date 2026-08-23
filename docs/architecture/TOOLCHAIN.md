# Supported toolchain and dependency baselines

Recorded by **M00-001**. Update this file whenever a baseline changes; CI enforces the
frontend lockfile (`npm ci`) and vulnerable-package audits.

## Baselines (verified 2026-08-24)

| Component | Baseline | Notes |
|---|---|---|
| .NET SDK | `10.0.302` | Pinned in `global.json` with `rollForward: latestFeature` (any 10.0.3xx accepted). |
| .NET runtime / ASP.NET Core | `net10.0`, package band `10.0.11` | Central versions in `Directory.Packages.props`. |
| C# | `14.0`, nullable enable, warnings-as-errors | Root `Directory.Build.props`. |
| Node.js | `24.x LTS` (verified on 24.18.0) | `engines` field in `frontend/package.json`; CI setup-node `24`; Docker `node:24-alpine`. |
| npm | `11.x` (verified on 11.16.0, bundled with Node 24) | Lockfile v3, committed at `frontend/package-lock.json`. |
| Next.js | `16.3.2` | Current patched supported 16.x patch (dist-tag `latest`); supersedes the pre-2026-08-26 baseline named in `START_HERE.md`. |
| React | `19.2.8` | Locked via lockfile. |
| TypeScript | `5.9.3` | Locked via lockfile. |

## Deterministic installs

- Frontend: `npm ci --no-audit --no-fund` everywhere (CI, Dockerfile). Never `npm install`
  in CI/Docker; regenerate the lockfile deliberately via `npm install` in a working copy.
- Backend: central package management (`Directory.Packages.props`); no floating versions.

## Dependency security audits

Run locally before pushing:

```bash
dotnet list GetCode.sln package --vulnerable --include-transitive
dotnet list GetCode.sln package --deprecated
cd frontend && npm audit --audit-level=high
```

CI gates:

- `ci.yml` backend job fails when any project reports vulnerable packages
  (direct or transitive).
- `ci.yml` frontend job runs `npm audit --audit-level=high`.
- CodeQL (`codeql.yml`) covers backend C# and frontend JavaScript/TypeScript.

Result at baseline time: **0 vulnerable / 0 deprecated NuGet packages; 0 npm advisories
(info through critical)** against the exact locked versions above. No advisory was waived,
so no risk ADR is required.

## Upgrade policy

1. Bump the patch/minor in the owning file (`global.json`, `Directory.Packages.props`,
   `frontend/package.json`) plus the lockfile in one commit.
2. Re-run restore/build/test, frontend lint/typecheck/build, and both audits.
3. Record the new baseline in this file with a date.
