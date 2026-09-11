<!-- markdownlint-disable-next-line MD041 -->
**[→ Read the design document](https://terrence721.github.io/GridPulse/gridpulse-design-doc.html)** — the full architecture and rationale in one page. (A one-page portfolio case study will replace/join this link once there's enough built to write one.)

# ⚡ GridPulse — Real-Time Event-Driven Utility Monitoring & Billing Platform

[![CodeQL](https://github.com/Terrence721/GridPulse/actions/workflows/codeql.yml/badge.svg)](https://github.com/Terrence721/GridPulse/actions/workflows/codeql.yml)

<!-- markdownlint-disable-next-line MD036 -->
**Status:** Phase 1 in progress — scaffolding the core REST loop (Meter Simulator → Usage Aggregation → Billing) on a single Postgres database, before Kafka enters the picture.

GridPulse is a self-contained, event-driven platform simulating a utility company's meter-reading, usage-aggregation, and billing pipeline. It's an original portfolio project (not a fork or a cloned tutorial) built to demonstrate principal-level system design: distributed systems, microservices decomposition, event streaming, resilient service-to-service communication, CI/CD, and production-grade observability. The domain is utility metering/billing, but the pipeline shape — high-volume telemetry in, aggregation, billing out — generalizes to IoT sensor networks, subscription usage billing, or order processing.

## 🧭 Start Here

- **[Design Document](docs/gridpulse-design-doc.html)** — full architecture rationale, service breakdown, event contracts, cross-cutting concerns, and interview talking points.
- **[`todo.md`](todo.md)** — the source of truth for what's done and what's left, phase by phase.
- **[Wiki](https://github.com/Terrence721/GridPulse/wiki)** — short pointers into the docs, one page per service as each one lands.
- **[Project board](https://github.com/users/Terrence721/projects/9)** — Backlog/Planned/In Progress/Verification & QA/Done, synced with `todo.md`.

It's being built to demonstrate:

- Original architecture and decision-making across a full event-driven microservices stack — not a cloned reference app
- The real stack used on the job: .NET Core / .NET Aspire, Angular + NgRx, Kafka, GitHub Actions CI/CD, OpenTelemetry-based observability
- Polyglot service design (a Node.js Notification Service alongside the .NET core) with consistent event contracts across the language boundary
- Domain modeling with proper OOP/SOLID design (rate plans as a Strategy pattern)
- A system deployable end-to-end and demoable live, not just describable

This is not a commercial-grade billing system (no real payment processing, no NERC/FERC compliance), not built for massive scale, and not a mobile app — web dashboard only.

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
        │  produces                       hourly/daily usage, writes to TimescaleDB
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
Notification Service (Node.js)          — polyglot by design: email/webhook notifications

Account/Customer Service (.NET)         — owns customer & meter registration, REST API
        │
        ▼
BFF / API Gateway (.NET Aspire)         — aggregates data for the UI, OIDC auth
        │
        ▼
Angular + NgRx Dashboard                — live usage charts, invoice history, account mgmt
```

All services are orchestrated locally and in CI via **.NET Aspire**.

**Where things actually stand right now (Phase 1):** the Kafka topics above are simplified to direct REST calls between Meter Simulator → Usage Aggregation → Billing, against a single Postgres database. The goal of Phase 1 is to get the domain logic right before introducing the event bus — see [Build Phases](#-build-phases).

---

## 📋 Build Phases

Tracked in detail in [`todo.md`](todo.md) (the source of truth) and the [project board](https://github.com/users/Terrence721/projects/9); mirrored here as a quick-glance checklist.

- [ ] **Phase 1 — Core loop, no Kafka.** Meter Simulator → Usage Aggregation → Billing via direct REST calls, single Postgres DB.
- [ ] **Phase 2 — Introduce Kafka.** Replace REST calls between services with Kafka topics; add a schema registry.
- [ ] **Phase 3 — Polyglot + accounts.** Add the Node.js Notification Service and the Account/Customer Service.
- [ ] **Phase 4 — Dashboard.** Build the Angular/NgRx dashboard and BFF gateway.
- [ ] **Phase 5 — CI/CD.** GitHub Actions build/test/containerize/deploy pipeline.
- [ ] **Phase 6 — Observability & resiliency.** OpenTelemetry, dashboards, retries/circuit breakers, dead-letter queues, chaos testing.

## Repository Layout

```text
src/            .NET services and the Aspire AppHost/ServiceDefaults
tests/          Unit and integration test projects
docs/           Design doc and any supplementary docs
```

(Populated incrementally — see Build Phases above.)

## 🖥 Running Locally

Not yet runnable — this section will be filled in once the Phase 1 AppHost exists.

### Prerequisites

- .NET 10 SDK
- Docker Desktop (for Postgres, and later Kafka)
- Node.js 20+ with [Corepack](https://nodejs.org/api/corepack.html) enabled (`corepack enable`) — resolves the pinned **Yarn 4.18.0** automatically for the Notification Service and Angular dashboard, Phase 3+

### Package management

All JS/TS packages in this repo (Notification Service, Angular dashboard, shared tooling) are managed with **Yarn Berry**, pinned to a specific version via `packageManager` in [`package.json`](package.json) so `corepack` resolves the same version everywhere. Shared dependency versions live in [`.yarnrc.yml`](.yarnrc.yml)'s `catalog`/`catalogs`, referenced from each package via the `catalog:` protocol instead of hardcoding a version per package — populated as each JS/TS package is actually added.

---

This project is an original design (see [`docs/gridpulse-design-doc.html`](docs/gridpulse-design-doc.html)) and is intended as a technical portfolio artifact.

## License

[MIT](LICENSE) © 2026 Terrence Daniels
