<!-- markdownlint-disable-next-line MD041 -->
**[→ Read the one-page portfolio](https://terrence721.github.io/GridPulse/portfolio.html)** — the 60-second version, with links back into this repo for anyone who wants to go deeper.

# ⚡ GridPulse — Real-Time Event-Driven Utility Monitoring & Billing Platform

[![CodeQL](https://github.com/Terrence721/GridPulse/actions/workflows/codeql.yml/badge.svg)](https://github.com/Terrence721/GridPulse/actions/workflows/codeql.yml)

<!-- markdownlint-disable-next-line MD036 -->
**Status:** Phase 1 complete, Phase 2 (Kafka) underway — Meter Simulator → Usage Aggregation → Billing all run end-to-end, and REST between services is being replaced with real Kafka events through a real Confluent Schema Registry. See [Build Phases](#-build-phases).

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
React + Redux Toolkit Dashboard          — live usage charts, invoice history, account mgmt
```

All services are orchestrated locally and in CI via **.NET Aspire**.

**Where things actually stand right now (Phase 2):** the Kafka topics above are real — all three services produce and consume Avro-encoded events through a real Confluent Schema Registry, replacing the direct REST calls Phase 1 used first to get the domain logic right. Meter Simulator → Usage Aggregation is verified live end-to-end over Kafka; Billing's Kafka wiring just landed and hasn't yet been verified against the full three-service pipeline — see [Build Phases](#-build-phases).

---

## 📋 Build Phases

Tracked in detail in [`todo.md`](todo.md) (the source of truth) and the [project board](https://github.com/users/Terrence721/projects/9); mirrored here as a quick-glance checklist.

- [x] **Phase 1 — Core loop, no Kafka.** Meter Simulator → Usage Aggregation → Billing via direct REST calls, single Postgres DB.
- [ ] **Phase 2 — Introduce Kafka (in progress).** Replace REST calls between services with Kafka topics; add a schema registry.
- [ ] **Phase 3 — Polyglot + accounts.** Add the Node.js Notification Service and the Account/Customer Service.
- [ ] **Phase 4 — Dashboard.** Build the React/Redux Toolkit dashboard and BFF gateway.
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

All three services — Meter Simulator, Usage Aggregation, and Billing — are wired in and boot together today, communicating entirely over real Kafka:

```text
dotnet run --project src/AppHost
```

This provisions Postgres, Kafka, and a Confluent Schema Registry (all via Docker) and starts the Aspire
dashboard — the link prints in the console. Every service registers itself in
[`src/AppHost/AppHost.cs`](src/AppHost/AppHost.cs), so the same command keeps working as later phases fill in.

Open [`GridPulse.code-workspace`](GridPulse.code-workspace) in VS Code for the configured dev experience (recommended extensions, `dotnet.defaultSolution` pointed at `GridPulse.slnx`, sane file/search excludes for `bin`/`obj`/`node_modules`).

### Prerequisites

- .NET 10 SDK
- [Aspire CLI](https://aspire.dev) 13.5.3 (`irm https://aspire.dev/install.ps1 | iex` on Windows, `curl -sSL https://aspire.dev/install.sh | bash` on Linux/macOS — pass `--version 13.5.3` to match this repo's `Aspire.Hosting.*` package version)
- Docker Desktop (for Postgres, Kafka, and the Confluent Schema Registry)
- Node.js 20+ with [Corepack](https://nodejs.org/api/corepack.html) enabled (`corepack enable`) — resolves the pinned **Yarn 4.18.0** automatically for the Notification Service and React dashboard, Phase 3+

### Package management

All JS/TS packages in this repo (Notification Service, React dashboard, shared tooling) are managed with **Yarn Berry**, pinned to a specific version via `packageManager` in [`package.json`](package.json) so `corepack` resolves the same version everywhere. Shared dependency versions live in [`.yarnrc.yml`](.yarnrc.yml)'s `catalog`/`catalogs`, referenced from each package via the `catalog:` protocol instead of hardcoding a version per package — populated as each JS/TS package is actually added.

---

This project is an original design (see [`docs/gridpulse-design-doc.html`](docs/gridpulse-design-doc.html)) and is intended as a technical portfolio artifact.

## License

[MIT](LICENSE) © 2026 Terrence Daniels
