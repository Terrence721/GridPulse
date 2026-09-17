<!-- markdownlint-disable-next-line MD041 -->
**[→ Read the one-page portfolio](https://terrence721.github.io/GridPulse/portfolio.html)** — the 60-second version, with links back into this repo for anyone who wants to go deeper.

# ⚡ GridPulse — Real-Time Event-Driven Utility Operations Platform

[![CodeQL](https://github.com/Terrence721/GridPulse/actions/workflows/codeql.yml/badge.svg)](https://github.com/Terrence721/GridPulse/actions/workflows/codeql.yml)

<!-- markdownlint-disable-next-line MD036 -->
**Status:** Phases 1 through 3 all complete, plus three post-Phase-3 features — Green Button/NAESB ESPI data export, a real Stripe payment integration, and Grid Operations (outage detection & work orders), all live-verified. All six services (Account/Customer Service, Meter Simulator, Usage Aggregation, Billing, Grid Operations, Notification Service) run end-to-end over real Kafka events through a real Confluent Schema Registry. See [Build Phases](#-build-phases).

GridPulse is a self-contained, event-driven platform simulating a utility company's meter-reading, usage-aggregation, and billing pipeline. It's an original portfolio project (not a fork or a cloned tutorial) built to demonstrate principal-level system design: distributed systems, microservices decomposition, event streaming, resilient service-to-service communication, CI/CD, and production-grade observability. The domain is utility metering/billing, but the pipeline shape — high-volume telemetry in, aggregation, billing out — generalizes to IoT sensor networks, subscription usage billing, or order processing.

## 🧭 Start Here

- **[Design Document](docs/gridpulse-design-doc.html)** — full architecture rationale, service breakdown, event contracts, cross-cutting concerns, and interview talking points.
- **[`docs/architecture.md`](docs/architecture.md)** — decision-by-decision reasoning (context, alternatives, consequences) for design choices made along the way.
- **[`todo.md`](todo.md)** — the source of truth for what's done and what's left, phase by phase.
- **[Wiki](https://github.com/Terrence721/GridPulse/wiki)** — short pointers into the docs, one page per service as each one lands.
- **[Project board](https://github.com/users/Terrence721/projects/9)** — Backlog/Planned/In Progress/Verification & QA/Done, synced with `todo.md`.

It's being built to demonstrate:

- Original architecture and decision-making across a full event-driven microservices stack — not a cloned reference app
- The real stack used on the job: .NET Core / .NET Aspire, React + TypeScript + Redux Toolkit, Kafka, GitHub Actions CI/CD, OpenTelemetry-based observability
- Polyglot service design (a Node.js Notification Service alongside the .NET core) with consistent event contracts across the language boundary
- Domain modeling with proper OOP/SOLID design (rate plans as a Strategy pattern)
- A system deployable end-to-end and demoable live, not just describable

This is not a commercial-grade billing system (Stripe payment collection is real but confined to test mode — no live card data, no real money movement — and there's no NERC/FERC compliance), not built for massive scale, and not a mobile app — web dashboard only.

---

## 🧭 Why This Project Matters

Most portfolio projects stop at "it works." This one is deliberately scoped to also answer the questions a principal-level interview actually asks: why event-driven over a monolith for this domain (independent scaling of ingestion vs. billing, a natural audit trail of events, easy to add new consumers later), what trade-offs were considered (Kafka over a simpler queue like RabbitMQ, for throughput and replay-ability), how idempotency is handled given Kafka's at-least-once delivery guarantees, how the design would scale to real meter volumes (partitioning on `meterId`, consumer group scaling), and what would change for a real production deployment (multi-region, real payment processing, compliance). The full reasoning for each is in the [design document](docs/gridpulse-design-doc.html).

---

## 🏗 Architecture Overview

Target-state architecture — an event-driven pipeline with a clear separation between ingestion, processing, and presentation:

```text
Meter Simulator (.NET worker)
        │  produces
        ▼
Kafka: meter.readings.raw
        │  consumes
        ▼
Usage Aggregation Service (.NET)        — validates/normalizes readings, rolls up into
        │  produces                       hourly/daily usage, writes to TimescaleDB;
        │                                 also detects meters that stop transmitting
        ▼
Kafka: usage.aggregated
        │  consumes
        ▼
Billing Service (.NET)                  — applies rate plans (Strategy pattern),
        │  produces                       generates invoices
        ▼
Kafka: billing.invoice.generated
        │  consumes
        ▼
Notification Service (Node.js)          — polyglot by design: real webhook notifications, built

Kafka: usage.anomaly.detected            — published by Usage Aggregation when a meter
        │  consumes                        stops transmitting readings
        ▼
Grid Operations Service (.NET)          — correlates quiet meters into outages, tracks
        │                                 tech-filed & auto-detected work orders
        ▼
Grid Operations BFF / API Gateway       — separate from the customer-facing gateway below — Phase 4
        │
        ▼
Outage & Work Order Dashboard           — its own frontend for field crews/dispatchers:
(React + Redux Toolkit)                   outage map, work-order queue — Phase 4

Account/Customer Service (.NET)         — owns customer & meter registration, REST API, built
        │
        ▼
BFF / API Gateway (.NET Aspire)         — aggregates data for the UI, OIDC auth — Phase 4
        │
        ▼
React + Redux Toolkit Dashboard          — live usage charts, invoice history, account mgmt — Phase 4
```

All services are orchestrated locally and in CI via **.NET Aspire**. Beyond the core loop pictured above, Usage Aggregation also exports real Green Button/NAESB ESPI usage data over HTTP, Billing collects real (test-mode) Stripe payments against generated invoices, and Grid Operations tracks real outages and work orders correlated from meters that stop transmitting — see [`docs/architecture.md`](docs/architecture.md) for all three.

**Where things actually stand right now (Phase 3 complete, plus ESPI, Stripe, and Grid Operations):** the Kafka topics above are real — all six services produce and consume Avro-encoded events through a real Confluent Schema Registry, replacing the direct REST calls Phase 1 used first to get the domain logic right. The full six-service pipeline is verified live end-to-end, including a real `Invoice` row landing in Postgres from real Kafka traffic, a real webhook delivered by Notification Service, real ESPI usage data served over HTTP, a real Stripe test-mode payment collected end to end, and real outages/work orders correlated from meters that stopped transmitting — see [Build Phases](#-build-phases).

---

## 📋 Build Phases

Tracked in detail in [`todo.md`](todo.md) (the source of truth) and the [project board](https://github.com/users/Terrence721/projects/9); mirrored here as a quick-glance checklist.

- [x] **Phase 1 — Core loop, no Kafka.** Meter Simulator → Usage Aggregation → Billing via direct REST calls, single Postgres DB.
- [x] **Phase 2 — Introduce Kafka.** Replace REST calls between services with Kafka topics; add a schema registry.
- [x] **Phase 3 — Polyglot + accounts.** Add the Node.js Notification Service and the Account/Customer Service.
- [ ] **Phase 4 — Dashboard.** Build the React/Redux Toolkit dashboard and BFF gateway.
- [ ] **Phase 5 — CI/CD.** GitHub Actions build/test/containerize/deploy pipeline.
- [ ] **Phase 6 — Observability & resiliency.** OpenTelemetry, dashboards, retries/circuit breakers, dead-letter queues, chaos testing.

Three features shipped after Phase 3, ahead of Phase 4: Green Button/NAESB ESPI usage-data export (Usage Aggregation), a real Stripe test-mode payment integration (Billing), and Grid Operations (outage detection & work orders, a new sixth service) — all live-verified. See [`todo.md`](todo.md) for the full write-up of each.

## Repository Layout

```text
src/            .NET services and the Aspire AppHost/ServiceDefaults
tests/          Unit and integration test projects
docs/           Design doc and any supplementary docs
```

(Populated incrementally — see Build Phases above.)

## 🖥 Running Locally

All six services — Account/Customer Service, Meter Simulator, Usage Aggregation, Billing, Grid Operations, and the Node.js Notification Service — are wired in and boot together today, communicating entirely over real Kafka:

```text
dotnet run --project src/AppHost
```

This provisions Postgres, Kafka, and a Confluent Schema Registry (all via Docker) and starts the Aspire
dashboard — the link prints in the console. Every service registers itself in
[`src/AppHost/AppHost.cs`](src/AppHost/AppHost.cs), so the same command keeps working as later phases fill in.

Open [`GridPulse.code-workspace`](GridPulse.code-workspace) in VS Code for the configured dev experience (recommended extensions, `dotnet.defaultSolution` pointed at `GridPulse.slnx`, sane file/search excludes for `bin`/`obj`/`node_modules`).

### Prerequisites

- .NET 10 SDK
- [Aspire CLI](https://aspire.dev) 13.5.4+ (`irm https://aspire.dev/install.ps1 | iex` on Windows, `curl -sSL https://aspire.dev/install.sh | bash` on Linux/macOS) — this repo's `Aspire.Hosting.*` NuGet packages are pinned to 13.5.3; the newer 13.5.4 CLI is confirmed compatible
- Docker Desktop (for Postgres, Kafka, and the Confluent Schema Registry)
- Node.js 20+ with [Corepack](https://nodejs.org/api/corepack.html) enabled (`corepack enable`) — resolves the pinned **Yarn 4.18.0** automatically for the Notification Service, added in Phase 3
- [Stripe CLI](https://stripe.com/docs/stripe-cli) (optional, only for exercising the real payment flow locally) — `stripe listen --forward-to http://localhost:5102/webhooks/stripe` forwards real test-mode webhooks with no public tunnel needed; see `docs/architecture.md`'s Stripe entry for the full local-verification runbook

### Package management

All JS/TS packages in this repo (Notification Service, React dashboard, shared tooling) are managed with **Yarn Berry**, pinned to a specific version via `packageManager` in [`package.json`](package.json) so `corepack` resolves the same version everywhere. Shared dependency versions live in [`.yarnrc.yml`](.yarnrc.yml)'s `catalog`/`catalogs`, referenced from each package via the `catalog:` protocol instead of hardcoding a version per package — populated as each JS/TS package is actually added.

---

This project is an original design (see [`docs/gridpulse-design-doc.html`](docs/gridpulse-design-doc.html)) and is intended as a technical portfolio artifact.

## License

[MIT](LICENSE) © 2026 Terrence Daniels
