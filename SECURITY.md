# Security Policy

GridPulse is a portfolio project — a self-contained simulation, not a production system handling real user data, payments, or utility infrastructure. That said, if you find a genuine security issue (e.g., a vulnerable dependency, an injection flaw, exposed secrets), please report it responsibly rather than filing a public issue.

## Reporting a Vulnerability

Please use GitHub's private vulnerability reporting for this repository:

**[Report a vulnerability →](https://github.com/Terrence721/GridPulse/security/advisories/new)**

This opens a private draft security advisory visible only to the repository owner — it won't be public until (and unless) it's published.

## Response Expectations

This is a solo-maintained project, so response times are best-effort rather than guaranteed. Confirmed issues will be fixed and disclosed transparently in [`todo.md`](todo.md), consistent with how every other finding in this repo is tracked.

## Scope

No live, publicly-deployed instance of GridPulse currently exists — everything runs locally via `dotnet run --project src/AppHost`. Stripe integration is real but confined to test mode (no live card data, no real money movement — see [docs/architecture.md](docs/architecture.md)); regulatory compliance remains an intentionally out-of-scope area, per the design doc.
