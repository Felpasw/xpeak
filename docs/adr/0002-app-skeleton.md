# ADR 0002 — Application skeleton (API + Mobile)

- **Status:** Accepted
- **Date:** 2026-09-15
- **Context:** Phase 2 (`specs/002-app-bootstrap/`).

## Context

Phase 2 delivers two runnable-but-empty applications so every later
phase can start with the TDD loop already in place, without arguing
about tooling. We also need CI that gates merges on real test runs
in both stacks.

## Decision

### Backend — `apps/api/`

- **C# on .NET 9** with **ASP.NET Core 9 Minimal APIs**.
- **EF Core 9** + `Npgsql.EntityFrameworkCore.PostgreSQL` for
  persistence.
- **xUnit** + **AwesomeAssertions** (OSS fork of FluentAssertions
  8+; the original went proprietary) + `Microsoft.AspNetCore.Mvc
  .Testing` + `Testcontainers.PostgreSql` for integration tests
  that hit a real Postgres.
- `HealthEndpoints.cs` exposes `GET /health` with a
  `{ status, version, time }` contract. The `VERSION` file is
  copied to the output folder and read via
  `AppContext.BaseDirectory` so it works identically under
  `dotnet run` and inside the test harness.
- `AppDbContext` is defined but empty; models arrive from Phase 3
  onwards.
- **NuGet lockfile** enabled
  (`<RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>`)
  so CI can use `--locked-mode`.
- **Dev-only Dockerfile** (`Dockerfile.dev`) uses the SDK image
  with `dotnet watch` and a two-stage layer that caches `dotnet
  restore --use-lock-file` between restore-relevant changes and
  source changes. Production dockerfile is a later-phase concern.

### Backend — style

- **`dotnet format`** is the formatter; CI verifies with
  `--verify-no-changes`.
- **`.editorconfig`** overrides indent to 4 spaces for
  `.cs/.csproj/.sln/.props/.targets` (matches .NET convention).
- **Test naming** follows the **Osherove** convention:
  `Method_Scenario_ExpectedBehavior` with PascalCase segments.

### Mobile — `apps/mobile/`

- **Next.js 16 (App Router)** with **Tailwind CSS 4** (bootstrapped
  via `create-next-app --tailwind`).
- **Static export** (`output: 'export'`, `images.unoptimized: true`)
  so Capacitor can wrap the built assets natively.
- **Capacitor 7** with Android and iOS platforms generated (iOS
  needs `pod install` on macOS to complete — the folder is committed
  so a Mac dev can pick it up).
- **Vitest** + **Testing Library** for unit tests, `jsdom`
  environment.
- **ESLint** with `eslint-config-next` + `eslint-config-prettier`.
- `package.json` is `@xpeak/mobile`, private, versioned at `0.0.1`
  and kept in sync by release-please.

### Infra

- **Docker-first workflow** — no requirement to install the .NET
  SDK on the host. `docker compose up -d api` runs postgres +
  api; `pnpm --filter @xpeak/mobile dev` runs the mobile web
  preview.
- Postgres is exposed on **host port 5433** (avoids clash with a
  local Postgres on 5432).
- **Release-please** now fans versions into two extra files:
  `apps/api/VERSION` and `apps/mobile/package.json`.

### CI

`.github/workflows/ci.yml` gains two required jobs (both feature
in the branch protection Ruleset from Phase 1):

- **`API — format + build + test`** — `dotnet format
  --verify-no-changes`, `dotnet build -warnaserror`, `dotnet test`
  against a `postgres:16` service container. Testcontainers.PostgreSql
  is used for the test-scoped DB.
- **`Mobile — test + build`** — `pnpm --filter @xpeak/mobile test`
  and `pnpm --filter @xpeak/mobile build`.

The vestigial `Lint` job introduced in Phase 1 is removed — devs
can still run `pnpm --filter @xpeak/mobile lint` locally.

## Alternatives considered

- **Elixir + Phoenix** — originally planned. Rejected mid-phase on
  ergonomic/aesthetic grounds. Concepts and endpoint contracts
  from the earlier specs still hold; artifacts were rewritten
  against C#/ASP.NET Core.
- **Ruby on Rails** — evaluated. Rejected on aesthetic grounds
  (test bootstrap produced but not carried forward).
- **Nest.js / AdonisJS** — would have kept us in Node; rejected in
  favor of stepping outside of Node entirely.

## Consequences

- Every phase from 3 onwards writes ASP.NET Core artifacts (EF Core
  entities, endpoints, DI-registered services, xUnit + Testcontainers
  tests).
- Auth in Phase 3 uses **ASP.NET Identity + JWT bearer +
  Microsoft.AspNetCore.Authentication.Google** (spec already
  rewritten in the same PR).
- Background jobs will use **Hangfire** (chosen for its Postgres
  storage and dashboard) when the first job-driven feature lands.
- The Mobile app is bound to Next 16 + Capacitor 7. Both are recent;
  we accept the ecosystem risk of running on the leading edge.

## Revisit triggers

- If Next 17 changes the App Router API meaningfully, revisit the
  static-export + Capacitor bridge.
- If we onboard a second maintainer, revisit the Docker-first
  policy (SDK-on-host may be preferred for hot reload speed).
- If Testcontainers becomes flaky on GH-hosted runners, migrate to
  self-hosted or drop to a shared `postgres:16` service without
  Testcontainers.
