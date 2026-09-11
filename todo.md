# 📝 TODO

**Last Updated:** September 11, 2026

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
| Project tracking | GitHub wiki, GitHub Pages (serving `docs/`), and a [project board](https://github.com/users/Terrence721/projects/9) (Backlog/Planned/In Progress/Verification & QA/Done) all set up |

**Actually still open, right now:** all of Phase 1's actual service code (7 items) plus Phases 2-6 at a high level — see **Still to do** below.

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

### Project tracking

| Date | What |
| - | - |
| 2026-09-11 | GitHub wiki initialized (Home page only for now — service pages land one at a time as each is actually built). |
| 2026-09-11 | GitHub Pages enabled, serving `docs/` at `terrence721.github.io/GridPulse` — makes the design doc linkable as a live page instead of a repo-relative file. |
| 2026-09-11 | [Project board](https://github.com/users/Terrence721/projects/9) created — same Backlog/Planned/In Progress/Verification & QA/Done shape as the other repos' boards. 15 issues filed and triaged: 3 closed (Done), 7 Planned (the rest of Phase 1, in build order), 5 Backlog (Phases 2-6, not yet broken into concrete tasks). |
| 2026-09-11 | This project added to the [GitHub profile README](https://github.com/Terrence721/Terrence721) and the [portfolio hub](https://terrence721.github.io/) as an early-stage card. |

## 🚧 Still to do

**Phase 1 — Core loop, no Kafka** (Meter Simulator → Usage Aggregation → Billing via direct REST calls, single Postgres DB — see [docs/gridpulse-design-doc.html](docs/gridpulse-design-doc.html)):

| # | Item | Status |
| - | - | - |
| 1 | `GridPulse.sln` + `src`/`tests` skeleton | Planned — [#4](https://github.com/Terrence721/GridPulse/issues/4) |
| 2 | Aspire backbone: `ServiceDefaults` + `AppHost` | Planned — [#5](https://github.com/Terrence721/GridPulse/issues/5) |
| 3 | Meter Simulator worker service | Planned — [#6](https://github.com/Terrence721/GridPulse/issues/6) |
| 4 | Usage Aggregation service (REST + EF Core/Postgres) | Planned — [#7](https://github.com/Terrence721/GridPulse/issues/7) |
| 5 | Billing service (rate-plan Strategy pattern) | Planned — [#8](https://github.com/Terrence721/GridPulse/issues/8) |
| 6 | Unit tests: UsageAggregation + Billing | Planned — [#9](https://github.com/Terrence721/GridPulse/issues/9) |
| 7 | End-to-end Phase 1 smoke check | Planned — [#10](https://github.com/Terrence721/GridPulse/issues/10) |

**Phases 2-6** — not yet broken into concrete tasks, tracked as one Backlog item each until their turn comes:

| # | Item | Status |
| - | - | - |
| 8 | Phase 2 — Introduce Kafka + schema registry | Backlog — [#11](https://github.com/Terrence721/GridPulse/issues/11) |
| 9 | Phase 3 — Node.js Notification Service + Account/Customer Service | Backlog — [#12](https://github.com/Terrence721/GridPulse/issues/12) |
| 10 | Phase 4 — React/Redux Toolkit dashboard + BFF gateway | Backlog — [#13](https://github.com/Terrence721/GridPulse/issues/13) |
| 11 | Phase 5 — CI/CD pipeline | Backlog — [#14](https://github.com/Terrence721/GridPulse/issues/14) |
| 12 | Phase 6 — Observability + resiliency hardening | Backlog — [#15](https://github.com/Terrence721/GridPulse/issues/15) |
