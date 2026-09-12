# 📝 TODO

**Last Updated:** September 12, 2026

A phase-by-phase log of what's been done on this repo and what's still open. This is the source of truth for progress — the [README](README.md)'s Build Phases checklist and the [project board](https://github.com/users/Terrence721/projects/9) both mirror this file, not the other way around.

**What this repo is:** an original, from-scratch event-driven platform simulating a utility company's meter-reading, usage-aggregation, and billing pipeline — not a fork or a cloned tutorial. Built to demonstrate the skills a Principal Developer role expects: distributed systems design, microservices decomposition, event streaming, CI/CD, and production-grade observability. See [README.md](README.md) for the project description, [docs/gridpulse-design-doc.html](docs/gridpulse-design-doc.html) for the full architecture, and [LICENSE](LICENSE) for licensing.

## 🏁 Milestones

| # | Milestone | Date | Detail |
| - | - | - | - |
| 1 | Repo bootstrap & tooling foundation | 2026-09-11 | git init, README, MIT LICENSE, design doc converted PDF→HTML, Yarn Berry 4.18.0 + catalog scaffold, CodeQL analysis, GitHub Pages, wiki, and this project board all live before any service code — see "Repository bootstrap" below |

## At a glance

**Done, in full:**

| Item | Detail |
| --- | --- |
| Repo scaffolding | README, MIT LICENSE, `.gitignore`, design doc (PDF → HTML) — [#1](https://github.com/Terrence721/GridPulse/issues/1) |
| Package management | Yarn Berry 4.18.0 pinned via Corepack, catalog scaffold for future JS/TS packages — [#2](https://github.com/Terrence721/GridPulse/issues/2) |
| Code analysis | CodeQL (`csharp` + `actions` today; `javascript-typescript` returns once real JS/TS source exists) — [#3](https://github.com/Terrence721/GridPulse/issues/3) |
| Project tracking | GitHub wiki, GitHub Pages (serving `docs/`), a one-page portfolio, and a [project board](https://github.com/users/Terrence721/projects/9) (Backlog/Planned/In Progress/Verification & QA/Done) all set up |
| Aspire backbone | `GridPulse.slnx`, `ServiceDefaults`, and `AppHost` — Postgres resource wired in and verified end-to-end (container up, `gridpulsedb` created) — [#4](https://github.com/Terrence721/GridPulse/issues/4)/[#5](https://github.com/Terrence721/GridPulse/issues/5) |
| Meter Simulator | 28 meters (both sides of a configurable city block), fail-fast validated config (no hardcoded values anywhere — Aspire parameters + user secrets locally), verified live for 2+ minutes. Only the HTTP publish to Usage Aggregation is left, now unblocked — [#6](https://github.com/Terrence721/GridPulse/issues/6) |
| Usage Aggregation | `POST /readings` with idempotency (duplicate `readingId` ignored) and hourly rollup (`TotalKwh` correctly summed), verified live against real Postgres via curl + `psql` — [#7](https://github.com/Terrence721/GridPulse/issues/7) |

**Actually still open:** Meter Simulator's HTTP call to Usage Aggregation, then Billing (with tests) and the Phase 1 smoke check, plus Phases 2-6 at a high level — see **Still to do** below.

## ✅ Done

### Repository bootstrap

| Date | What |
| - | - |
| 2026-09-11 | `git init`; `.gitignore` (.NET/Aspire/Node/React/Docker); MIT `LICENSE` (Terrence Daniels); `README.md` describing the architecture and 6-phase build plan; default branch renamed `master` → `main`. [#1](https://github.com/Terrence721/GridPulse/issues/1) |
| 2026-09-11 | Original design-doc PDF converted to a styled, readable `docs/gridpulse-design-doc.html` — kept as the single source of truth instead of a binary PDF that diffs poorly in git. [#1](https://github.com/Terrence721/GridPulse/issues/1) |
| 2026-09-11 | Design decision: Phase 4 dashboard switched from Angular + NgRx to **React + TypeScript + Redux Toolkit** (RTK Query for data fetching, playing the same role NgRx Effects would). Made before any frontend code existed — updated everywhere the old stack was named: design doc, README, this file, the wiki, the portfolio card, the profile README, and issue [#13](https://github.com/Terrence721/GridPulse/issues/13)/its milestone. |

### Tooling & CI

| Date | What |
| - | - |
| 2026-09-11 | Yarn Berry pinned to 4.18.0 via Corepack (`packageManager` in `package.json`); `.yarnrc.yml` sets `nodeLinker: node-modules` plus an empty `catalog`/`catalogs` scaffold, populated as each JS/TS package (Notification Service, React dashboard) actually lands. [#2](https://github.com/Terrence721/GridPulse/issues/2) |
| 2026-09-11 | `.github/workflows/codeql.yml` added, scoped to `javascript-typescript` + `actions` (`build-mode: none`, so it runs clean even before any source exists). Replaced GitHub's auto-generated "Advanced setup" template, which had detected zero languages in this still-sparse repo and would have shipped with an empty matrix. [#3](https://github.com/Terrence721/GridPulse/issues/3) |
| 2026-09-11 | **Real CI failure found and fixed:** the first live run of `codeql.yml` failed — `database finalize` hard-errors when CodeQL's JS/TS extractor finds zero matching files, it does not just report zero findings the way `build-mode: none` does for languages that do have source present. `javascript-typescript` removed until real JS/TS code exists (Phase 3+); `csharp` added instead (`build-mode: manual`) since real C# source now exists (`ServiceDefaults`, `AppHost`). [#3](https://github.com/Terrence721/GridPulse/issues/3) |
| 2026-09-11 | **Second real CI failure found and fixed, same day:** the `csharp` fix above then failed its own `dotnet build GridPulse.slnx` step — `ASPIRE009`, `GridPulse.AppHost` needs the Aspire CLI bundle installed, which hosted GitHub runners don't have. Initially worked around with `GridPulse.ci.slnf` (a solution filter excluding `AppHost`, same pattern as `eshop-full`'s `eShop.Web.slnf`) — then reconsidered and replaced with the real fix below, since `AppHost.cs` is about to gain actual orchestration logic worth analyzing. [#3](https://github.com/Terrence721/GridPulse/issues/3) |
| 2026-09-11 | Replaced the `.slnf` workaround: CI now installs the Aspire CLI itself (pinned to 13.5.3, matching the `Aspire.Hosting.*` package version) via `curl -sSL https://aspire.dev/install.sh \| bash -s -- --version 13.5.3` before building — the script self-detects `$GITHUB_ACTIONS` and appends its install dir to `$GITHUB_PATH`, no manual PATH wiring needed. `dotnet build` now targets the real `GridPulse.slnx` (all projects, `AppHost` included) instead of a filtered view. `GridPulse.ci.slnf` removed as dead weight. Also installed the same CLI globally on the local dev machine so `dotnet run`/`aspire` behave identically there. [#3](https://github.com/Terrence721/GridPulse/issues/3) |
| 2026-09-12 | **Third real issue found and fixed — a stale tool-status entry, not a live one:** the Security overview page kept showing a "Code scanning configuration error" banner long after `javascript-typescript` was removed from `codeql.yml`. Root cause: GitHub's code-scanning tool-status tracker doesn't clear a language/category just because a later workflow run stops analyzing it — the 7 failed `/language:javascript-typescript` analyses from the original broken runs (2026-09-11, before the fix above) were still sitting there, permanently "errored," with nothing superseding them. Fixed by deleting all 7 via `DELETE /repos/.../code-scanning/analyses/{id}`; confirmed only `/language:csharp` and `/language:actions` categories remain, and the banner cleared. [#3](https://github.com/Terrence721/GridPulse/issues/3) |

### Project tracking

| Date | What |
| - | - |
| 2026-09-11 | GitHub wiki initialized (Home page only for now — service pages land one at a time as each is actually built). |
| 2026-09-11 | GitHub Pages enabled, serving `docs/` at `terrence721.github.io/GridPulse` — makes the design doc linkable as a live page instead of a repo-relative file. |
| 2026-09-11 | [Project board](https://github.com/users/Terrence721/projects/9) created — same Backlog/Planned/In Progress/Verification & QA/Done shape as the other repos' boards. 15 issues filed and triaged: 3 closed (Done), 7 Planned (the rest of Phase 1, in build order), 5 Backlog (Phases 2-6, not yet broken into concrete tasks). |
| 2026-09-11 | `docs/portfolio.html` added — the one-page portfolio, same design system as `coolify-full`/`saga-full`'s. Content kept honest to Phase 1's actual state rather than copying their case-study structure wholesale: the "ledger" section uses the two real CI failures above instead of app-level findings (there isn't a service yet to find bugs in), and a "Judgment calls" section documents the `.slnf` → real-CLI-install reconsideration. README's top-of-file link now points here instead of the design doc directly. |
| 2026-09-11 | This project added to the [GitHub profile README](https://github.com/Terrence721/Terrence721) and the [portfolio hub](https://terrence721.github.io/) as an early-stage card. |

### Phase 1 — solution & Aspire backbone

| Date | What |
| - | - |
| 2026-09-11 | `GridPulse.slnx` created (`.NET 10`'s new default solution format, chosen over classic `.sln` — cleaner diffs, no GUID soup). `src`/`tests` weren't scaffolded as empty placeholder folders since git doesn't track empty directories; they came into existence naturally once real projects landed. [#4](https://github.com/Terrence721/GridPulse/issues/4) |
| 2026-09-11 | `src/ServiceDefaults` added (shared health checks, OpenTelemetry, service discovery — Microsoft's standard Aspire template, already composition-based with single-responsibility extension methods, checked against this repo's DRY/SOLID/composition-over-inheritance standard and needed no changes). `src/AppHost` scaffolded and given `Aspire.Hosting.PostgreSQL`. [#5](https://github.com/Terrence721/GridPulse/issues/5) |
| 2026-09-11 | Postgres resource wired into `AppHost.cs` (`AddPostgres("postgres").WithDataVolume()` + `AddDatabase("gridpulsedb")`) and verified end-to-end, not just assumed from a clean build: ran the AppHost locally, confirmed the `postgres-nvnytrht` container reached `Running`, and confirmed `gridpulsedb` actually exists via `psql -l` inside the container. Closes out the Aspire backbone. [#5](https://github.com/Terrence721/GridPulse/issues/5) |

### Phase 1 — Meter Simulator (done, blocked on Usage Aggregation for the HTTP publish)

| Date | What |
| - | - |
| 2026-09-11 | Bare scaffold hand-written file by file rather than via `dotnet new` (`.csproj`, `Program.cs`, `Worker.cs`, `appsettings.json`/`.Development.json`, `launchSettings.json`) — each its own commit. [#6](https://github.com/Terrence721/GridPulse/issues/6) |
| 2026-09-11 | **Real build failure found and fixed:** the hand-written `.csproj` used `Microsoft.NET.Sdk.Worker` alone, which doesn't pull in `Microsoft.Extensions.Hosting` as a framework reference — `Host.CreateApplicationBuilder`/`BackgroundService`/`ILogger<T>` all failed to resolve (7 compile errors). Fixed by adding the explicit `Microsoft.Extensions.Hosting` package reference (resolved to 10.0.12); standalone build verified clean afterward. [#6](https://github.com/Terrence721/GridPulse/issues/6) |
| 2026-09-11 | Registered in `GridPulse.slnx`; referenced `ServiceDefaults` and wired `builder.AddServiceDefaults()` into `Program.cs`; re-verified with a standalone `dotnet build`, 0 warnings/errors. **Not yet done:** no reference from `AppHost` (so it isn't orchestrated yet), and no domain logic — `Worker.cs` is still the placeholder template loop, not a meter-reading generator. [#6](https://github.com/Terrence721/GridPulse/issues/6) |
| 2026-09-12 | Wired into `AppHost`: project reference + `AddProject<...>("meter-simulator")`, as two separate commits/approvals. Full solution (`AppHost` + `ServiceDefaults` + `MeterSimulator`) builds clean together for the first time. [#6](https://github.com/Terrence721/GridPulse/issues/6) |
| 2026-09-12 | Domain logic built one file at a time: `MeterReading` (record matching the design doc's event contract — `meterId`/`timestamp`/`kwh`/`readingId`), `MeterReadingGenerator` (produces one synthetic reading per call), `MeterSimulatorOptions` (config), `Worker` rewritten to generate a reading per meter on the configured interval and log it (the actual HTTP POST to Usage Aggregation waits until that service exists — Phase 1's REST design). [#6](https://github.com/Terrence721/GridPulse/issues/6) |
| 2026-09-12 | **Redesigned the meter list, twice, based on real-world feedback:** first pass hardcoded a flat list of meter IDs in `MeterSimulatorOptions`. Reconsidered — a real city block has buildings on both sides of the street, and hardcoding any fixed list isn't realistic or maintainable. Replaced with `StreetName`/`StartingAddress`/`BuildingsPerSide` config values plus a new `CityBlockMeterIdFactory` that computes the actual ID list (even side + odd side) at startup — 28 meters (14 per side) generated from 3 numbers, not spelled out. |
| 2026-09-12 | **End-to-end verification, not just a clean build:** ran `dotnet run --project src/AppHost` for real. Confirmed via the DCP-captured process output (`resource-executable-*_out` under `%TEMP%\aspire-dcp*.dmk\`, since per-resource console output isn't in the AppHost's own CLI log): exactly 28 unique meters covered, each producing a fresh reading with a unique `ReadingId` and randomized kWh every 5 seconds, running clean for 2+ minutes across ~25 ticks per meter with zero crashes. Cleaned up the container/network afterward. |
| 2026-09-12 | **Judgment call — config-per-deployment, no hardcoding, enforced:** asked whether "how does a field engineer set which street" should be per-deployment config, a multi-street list, or an interactive prompt — chose config-per-deployment (fits a headless background worker; a real deployment is one instance per territory). Then reconsidered the config itself: even a fixed default value baked into `MeterSimulatorOptions` is still hardcoding. Removed all three defaults, added `[Required]`/`[Range]` validation plus `.AddOptions<T>().ValidateDataAnnotations().ValidateOnStart()`, so the app now refuses to run at all with missing/invalid `StreetName`/`StartingAddress`/`BuildingsPerSide` — verified live: running with zero config produces exactly the 3 expected validation messages. |
| 2026-09-12 | **Real bug found and fixed — validation ran, but noisily:** the first fail-fast attempt worked (app exited non-zero with a caught, clean message) but a full stack trace still printed first — .NET's Generic Host logs `OptionsValidationException` internally during `StartAsync()`, before any caller's own try/catch ever sees it. Fixed by resolving `IOptions<MeterSimulatorOptions>.Value` manually right after `builder.Build()`, *before* `host.Run()` — validation now happens in our own code, never reaching the Host's internal startup-failure logging path. Re-verified live: clean 3-line output, no stack trace. |
| 2026-09-12 | **Self-caught and corrected — hardcoding just moved, not removed:** first fix for `AppHost` orchestrating the now-required config was `.WithEnvironment("MeterSimulator__StreetName", "MAIN-ST")` with literal values — same hardcoding problem, just relocated from `MeterSimulatorOptions.cs` to `AppHost.cs`. Caught before committing. Replaced with Aspire's `builder.AddParameter(...)` (no default value, by design), which resolves from `AppHost`'s own configuration (`dotnet user-secrets`, never a committed file) and fails `AppHost` itself if unset — the same fail-fast discipline as `MeterSimulator`, one layer up. Local values set via `dotnet user-secrets set "Parameters:meter-simulator-*" ... --project src/AppHost`, landing in `%APPDATA%\Microsoft\UserSecrets\<AppHost's UserSecretsId>\secrets.json` — outside the repo, never committed. |
| 2026-09-12 | Considered and rejected `appsettings.Production.json`: environment variables already take precedence over any `appsettings.{Environment}.json` in .NET's config load order, so a real deployment's env vars would override it anyway — and there's no safe content to put in it that wouldn't either be dead weight (empty) or reintroduce the exact hardcoding this design eliminates (real-looking example values). |

**Where Meter Simulator actually stands:** scaffold, domain logic, configuration, validation, and `AppHost` wiring are all done and verified live. The only remaining piece is the actual HTTP POST to Usage Aggregation, which can't be built until that service exists (Step 5) — readings are logged, not yet sent anywhere.

### Phase 1 — Usage Aggregation (in progress)

| Date | What |
| - | - |
| 2026-09-12 | Scaffold hand-written file by file (`.csproj` on `Microsoft.NET.Sdk.Web`, not `.Sdk.Worker` — this is a REST API, not a background worker; `Program.cs`, `appsettings.json`/`.Development.json`, `launchSettings.json` on ports 5101/7101), registered in `GridPulse.slnx`. [#7](https://github.com/Terrence721/GridPulse/issues/7) |
| 2026-09-12 | Referenced `ServiceDefaults`, wired `builder.AddServiceDefaults()` + `app.MapDefaultEndpoints()` for `/health`. Wired into `AppHost`: project reference + `AddProject<...>("usage-aggregation").WithReference(gridpulseDb).WaitFor(gridpulseDb)` — the `Postgres` dependency other services don't have. [#7](https://github.com/Terrence721/GridPulse/issues/7) |
| 2026-09-12 | Domain model built one file at a time: `MeterReadingRequest` (the incoming POST shape, matching the design doc's `meter.readings.raw` schema), `ProcessedReading` (idempotency tracking, keyed by `ReadingId`), `HourlyUsage` (the rollup record). **Deliberate deviation from the design doc:** `HourlyUsage` is keyed by `MeterId`, not `accountId` as the doc's `usage.aggregated` Kafka schema shows — there's no Account/Customer Service yet to map meters to accounts (that's Phase 3), so account-level aggregation isn't buildable yet. `UsageAggregationDbContext` added with `ReadingId` explicitly configured as `ProcessedReading`'s primary key (EF's naming convention wouldn't infer it) and a unique index on `(MeterId, PeriodStart)` so a rollup row can't be duplicated for the same meter/hour. [#7](https://github.com/Terrence721/GridPulse/issues/7) |
| 2026-09-12 | EF Core migrations added: `Aspire.Npgsql.EntityFrameworkCore.PostgreSQL` (the Aspire client integration — wires up the `DbContext`, health check, and OpenTelemetry from the `WithReference` connection string automatically) plus `Microsoft.EntityFrameworkCore.Design`. A `UsageAggregationDbContextFactory` (`IDesignTimeDbContextFactory`) was needed since EF's design-time tooling can't ask a real `AppHost` for a connection string the way the running app does — its placeholder connection string is never actually used to connect, only to tell the tooling which SQL dialect to generate. `InitialCreate` migration generated via `dotnet ef migrations add`, applied automatically at startup (`dbContext.Database.Migrate()`). Caught and fixed a real compile error along the way: `Microsoft.EntityFrameworkCore`'s `Migrate()` extension method isn't in the Web SDK's implicit usings (EF Core is a separate package from ASP.NET Core), so `Program.cs` needed an explicit `using` the IDE caught immediately. [#7](https://github.com/Terrence721/GridPulse/issues/7) |
| 2026-09-12 | `ReadingProcessor` added: checks `ProcessedReadings` for the incoming `ReadingId` before doing anything else (idempotency — a duplicate returns `false` without reprocessing), then upserts the `(MeterId, PeriodStart)` hourly bucket, adding the new reading's `Kwh` to the running total. Wired up as `POST /readings`, returning `202 Accepted` for a newly processed reading or `200 OK` for a duplicate. [#7](https://github.com/Terrence721/GridPulse/issues/7) |
| 2026-09-12 | **End-to-end verification, not just a clean build:** ran `dotnet run --project src/AppHost`, confirmed `/health` returns `200`, then sent real `POST /readings` requests via curl. Sent one reading (2.5 kWh) — `202 Accepted`. Resent the identical `readingId` — `200 OK` with `"duplicate"`, not reprocessed. Sent a second, genuinely new reading for the same meter in the same hour (1.75 kWh). Verified directly against Postgres via `psql`: `ProcessedReadings` has exactly 2 rows (the duplicate never created a 3rd), and `HourlyUsages` has exactly 1 row for that hour with `TotalKwh = 4.25` — the correct sum. Idempotency and rollup both confirmed against a real database, not assumed from the code. [#7](https://github.com/Terrence721/GridPulse/issues/7) |

**Where Usage Aggregation actually stands:** scaffold, `AppHost`/Postgres wiring, domain model, migrations, and the core idempotency+rollup logic are all built and verified live against a real database. The only remaining piece is Meter Simulator actually calling this endpoint instead of just logging.

## 🚧 Still to do

**Phase 1 — Core loop, no Kafka** (Meter Simulator → Usage Aggregation → Billing via direct REST calls, single Postgres DB — see [docs/gridpulse-design-doc.html](docs/gridpulse-design-doc.html)):

| # | Item | Status |
| - | - | - |
| 1 | Meter Simulator worker service | Unblocked — Usage Aggregation now exists and is verified live; only remaining piece is wiring the actual `POST /readings` HTTP call in place of logging — [#6](https://github.com/Terrence721/GridPulse/issues/6) |
| 2 | Usage Aggregation service (REST + EF Core/Postgres) | Done and verified live (idempotency + hourly rollup confirmed against real Postgres) — [#7](https://github.com/Terrence721/GridPulse/issues/7) |
| 3 | Billing service (rate-plan Strategy pattern) | Planned — [#8](https://github.com/Terrence721/GridPulse/issues/8) |
| 4 | Unit tests: UsageAggregation + Billing | Planned — [#9](https://github.com/Terrence721/GridPulse/issues/9) |
| 5 | End-to-end Phase 1 smoke check | Planned — [#10](https://github.com/Terrence721/GridPulse/issues/10) |

**Phases 2-6** — not yet broken into concrete tasks, tracked as one Backlog item each until their turn comes:

| # | Item | Status |
| - | - | - |
| 6 | Phase 2 — Introduce Kafka + schema registry | Backlog — [#11](https://github.com/Terrence721/GridPulse/issues/11) |
| 7 | Phase 3 — Node.js Notification Service + Account/Customer Service | Backlog — [#12](https://github.com/Terrence721/GridPulse/issues/12) |
| 8 | Phase 4 — React/Redux Toolkit dashboard + BFF gateway | Backlog — [#13](https://github.com/Terrence721/GridPulse/issues/13) |
| 9 | Phase 5 — CI/CD pipeline | Backlog — [#14](https://github.com/Terrence721/GridPulse/issues/14) |
| 10 | Phase 6 — Observability + resiliency hardening | Backlog — [#15](https://github.com/Terrence721/GridPulse/issues/15) |
