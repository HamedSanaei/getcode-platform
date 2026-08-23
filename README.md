# GetCode Platform

GetCode is a multi-domain virtual-number commerce platform starter. It is intentionally structured as a **DDD-oriented modular monolith** with a clean dependency direction, a separate Next.js frontend, background worker, PostgreSQL source of truth, Redis for ephemeral concerns, provider anti-corruption layers, and an agent-ready delivery plan.

## Product hosts

One application/codebase is designed to serve at least two hosts simultaneously:

- the independent GetCode domain (replace the placeholder once selected);
- `vnumber.pluspremium.ir`.

The hosts are **not separate tenants**. Users, wallets, orders, catalog, providers and operational data are shared. Host-specific concerns (public URLs, branding, SEO canonical policy and return URLs) are isolated behind Site Context.

## Architecture at a glance

```text
Independent GetCode domain ─┐
                            ├─> Edge / reverse proxy ─> Next.js 16.x
vnumber.pluspremium.ir ─────┘                          │
                                                      └─ /api/*
                                                           │
                                                    ASP.NET Core 10 API
                                                           │
                    ┌──────────────────────────────────────┼────────────────────────┐
                    │                                      │                        │
               PostgreSQL                              Redis                ASP.NET Worker
             source of truth                      cache/locks/rate             │
                                                                           Provider ports
                                                                               │
                                                                    Provider A/B/C adapters
```

Core style: **Modular Monolith + Clean Architecture + DDD-oriented + Ports & Adapters**.

## Repository layout

```text
backend/          ASP.NET Core API, Worker, clean layers and test projects
frontend/         Next.js application; host-aware presentation only
design/           Penpot workflow, design tokens and UI handoff contracts
docs/             architecture, ADRs, roadmap, tasks, runbooks and status
infrastructure/   edge proxy and deployment assets
scripts/          developer, log-maintenance and GitHub bootstrap helpers
.github/          CI, security, dependency updates and contribution templates
.agents/          agent roles and execution/review playbooks
```

## Start here

1. Read [`AGENTS.md`](AGENTS.md) even if you are a human contributor.
2. Read [`docs/architecture/ARCHITECTURE.md`](docs/architecture/ARCHITECTURE.md).
3. Read [`docs/roadmap/MILESTONES.md`](docs/roadmap/MILESTONES.md) and [`docs/roadmap/TASK_INDEX.md`](docs/roadmap/TASK_INDEX.md).
4. Execute tasks in dependency order, one task per branch/PR where practical.
5. UI work starts in **Penpot**, not in React. See [`design/README.md`](design/README.md).

## Local prerequisites

- .NET 10 SDK (the repository uses `global.json` with roll-forward inside .NET 10)
- Node.js 22 LTS or newer supported version
- Docker/Compose for PostgreSQL and Redis
- Git

## Quick local infrastructure

```bash
cp .env.example .env
docker compose up -d postgres redis
```

Backend and frontend bootstrap commands are documented in their respective READMEs.

> Security note (2026-08-24): Next.js announced a scheduled security release for 2026-08-26. Before first dependency lock or any deployment, M00-001 requires selecting the latest patched supported Next.js 16.x release and committing a lockfile. Do not deploy the placeholder dependency range without that gate.

## Status

This ZIP is a **starter architecture**, not a feature-complete product. Business capabilities are deliberately represented by explicit boundaries and tasks instead of speculative implementation.
