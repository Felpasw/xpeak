# Phase 2 — Application Skeleton (ASP.NET Core API + Next.js/Capacitor)

> Status: **planning only**. No code written yet. Companion to
> `spec.md` (details) and `tasks.md` (execution breakdown).

## 1. Goal

Bring up the two application skeletons in the monorepo:

- `apps/api` — C# on .NET 9 with **ASP.NET Core Minimal APIs**
  (`dotnet new webapi --use-controllers false --auth None`),
  Postgres-backed via EF Core 9, xUnit-first.
- `apps/mobile` — Next.js 15 (App Router) with Capacitor wrapping
  for Android/iOS, Vitest for unit tests, mobile-first foundation.
- `packages/shared` — TypeScript workspace package for cross-app
  types and constants (scaffold only, empty for now).

Everything wired into the CI pipeline created in Phase 1, everything
runnable locally with a single command, everything covered by at
least one smoke test that proves the app boots.

## 2. Scope

**In scope**
- .NET 9 Minimal API bootstrap.
- EF Core 9 + `Npgsql.EntityFrameworkCore.PostgreSQL` wired to
  `AppDbContext` (empty for now — models arrive from Phase 3).
- Health check endpoint (`GET /health`) with an integration test.
- CORS policy for `http://localhost:3001` and
  `capacitor://localhost` in dev.
- Postgres service via `docker-compose.yml` for local dev, plus an
  `api` service using a dev Dockerfile.
- Next.js 15 bootstrap (App Router, strict TS, ESLint).
- Vitest + Testing Library on the mobile app.
- Capacitor bootstrap (`@capacitor/core`, `@capacitor/cli`) with
  Android and iOS projects generated.
- Mobile-first base layout (viewport meta, safe-area handling, a
  single "Hello XPeak" screen).
- `packages/shared` workspace package with a barrel export scaffold.
- CI expansion: `pnpm test/build` on the mobile app; `dotnet test`
  + `dotnet format --verify-no-changes` on the api.
- `.env.example` files for both apps.
- Per-app README with local dev instructions.

**Out of scope (later phases)**
- Any domain logic (auth, users, XP, categories, check-ins,
  challenges). This phase produces empty apps, not features.
- Real design system pass — just the minimum to prove the mobile
  shell renders.
- Deploying either app anywhere.
- Native store setup (bundle IDs, provisioning profiles).
- Media storage provider wiring (defer until Phase 6).
- Hangfire actually processing jobs (dep + config added later).

## 3. Rationale

Bootstrap before feature work so every later phase can start with:
- a repo layout that already matches the product brief;
- test frameworks in place, so the TDD rule (write failing test
  first) is enforceable from task one;
- CI that catches regressions the moment domain code appears;
- a `docker compose up` that spins Postgres + the API without each
  dev reinventing the setup.

Skipping this phase means every subsequent feature would need to
argue about tooling before writing the failing test — the exact
friction we want gone.

## 4. High-level structure after this phase

```
xpeak/
  apps/
    api/
      api.csproj
      Program.cs                       # host + DI + endpoints wiring
      global.json                      # .NET SDK pin
      VERSION                          # single source of truth for release-please
      appsettings.json
      appsettings.Development.json
      Dockerfile.dev
      Endpoints/
        HealthEndpoints.cs
      Infrastructure/
        AppDbContext.cs
      Domain/                          # (empty; models land from Phase 3)
      README.md
      .env.example
    api.Tests/
      api.Tests.csproj
      HealthEndpointTests.cs           # integration test with WebApplicationFactory
      Support/
        XpeakWebApplicationFactory.cs  # Testcontainers-backed Postgres
    mobile/
      app/                             # Next.js App Router
      components/
      lib/
      public/
      android/                         # Capacitor Android project
      ios/                             # Capacitor iOS project
      capacitor.config.ts
      next.config.ts
      package.json
      tsconfig.json
      vitest.config.ts
      README.md
      .env.example
  packages/
    shared/
      src/index.ts
      package.json
      tsconfig.json
  docker-compose.yml                   # postgres + api (dev)
  .github/workflows/ci.yml             # extended with api-test + mobile-test jobs
```

## 5. Local dev experience target

After this phase, a fresh clone should get to a running stack with:

```sh
# one-time
pnpm install

# every day (Docker-first)
docker compose up -d                   # postgres + api
pnpm --filter @xpeak/mobile dev        # http://localhost:3001
```

And a smoke test path:

```sh
curl http://localhost:5000/health      # {"status":"ok",...}
open http://localhost:3001             # "Hello XPeak"
```

If a dev prefers the .NET SDK locally (not required):

```sh
cd apps/api
dotnet restore
dotnet run                             # http://localhost:5000
```

## 6. TDD stance for this phase

Even for scaffolding, tests go first where meaningful:

- Api: an **integration test** for `/health` written **before** the
  endpoint method — uses `WebApplicationFactory<Program>` against a
  Testcontainers Postgres.
- Mobile: Vitest smoke test asserting the root page renders "Hello
  XPeak" written **before** the page file.
- Any non-trivial config (e.g., CORS allow-list) covered by an
  integration test.

Pure scaffolding steps (running `dotnet new`, running
`create-next-app`) obviously don't have a test-first — but the
moment we add project-specific behavior on top, the failing-test-
first rule kicks in.

## 7. Dependencies & risks

| Risk / dep                                                       | Mitigation                                                              |
|------------------------------------------------------------------|-------------------------------------------------------------------------|
| .NET SDK version drift dev↔CI                                    | `global.json` at `apps/api/` pins the SDK; CI reads it directly.        |
| Capacitor iOS project needs Xcode / macOS                        | Generate the folder; iOS build validated later on a Mac.                |
| Postgres port `5432` already in use on host                      | Compose maps host `5433` → container `5432`; documented in README.      |
| release-please must bump the .NET version                        | Keep an `apps/api/VERSION` file; `HealthEndpoints` reads it. Fan-out via `extra-files`. |
| Two dev servers on different ports                               | Document ports (5000 api / 3001 mobile) in each README.                 |
| Cold Docker build slow first time                                | Multi-stage `Dockerfile.dev` caches `dotnet restore` in its own layer.  |

## 8. Decisions (locked) and remaining open questions

**Locked:**
- ✅ Backend stack: **C# + ASP.NET Core 9** (Minimal APIs).
- ✅ ORM: **Entity Framework Core 9** with Npgsql provider.
- ✅ Test stack: **xUnit + FluentAssertions +
  Microsoft.AspNetCore.Mvc.Testing + Testcontainers.PostgreSql**.
- ✅ Postgres via `docker-compose.yml` on host port **5433**
  (avoids clash with a local Postgres on 5432).
- ✅ ID strategy: **`Guid` (UUID v4)** for PKs — matches Phase 3
  decision.
- ✅ Docker-first workflow (no requirement to install .NET SDK on
  host).

**Still open (decide before we cut code):**
1. **Design system on mobile** — commit to Tailwind + shadcn/ui
   now, or ship a plain-CSS "Hello XPeak" this phase?
2. **PWA manifest** — include in this phase, or wait for the
   "Mobile polish" phase (Block I)?
3. **Shared package name** — `@xpeak/shared` or `@xpeak/types`?
4. **CORS allow-list** — dev-only `http://localhost:3001` for now,
   plus a placeholder for the eventual mobile app scheme
   (`capacitor://localhost`)?
5. **API base URL contract** — `NEXT_PUBLIC_API_URL` env var with
   default to `http://localhost:5000`?
6. **Structured logging** — introduce Serilog now or defer to a
   later observability phase?

## 9. Interaction with Phase 1 (release-please)

- The `.release-please-manifest.json` stays single-package at root
  (decision from Phase 1 §6.A).
- Add `extra-files` entries to `release-please-config.json`
  pointing to `apps/api/VERSION` and to
  `apps/mobile/package.json`.
- `apps/api/VERSION` is the single source of truth for the API
  version. The `HealthEndpoints` reads it at runtime; no MSBuild
  gymnastics required. The .csproj can also expose it as a
  `<Version>` element wired via `<PropertyGroup><Version>$(FileReadAllText VERSION)</Version></PropertyGroup>`
  when we get to release engineering (later phase).

## 10. Success criteria

- `docker compose up -d postgres` brings Postgres up cleanly on
  port 5433.
- `docker compose up -d api` builds and boots the API without
  errors.
- `curl localhost:5000/health` returns
  `{"status":"ok","version":"0.0.1","time":"..."}`.
- `pnpm --filter @xpeak/mobile dev` boots Next.js on port 3001.
- Browser at `http://localhost:3001` shows "Hello XPeak".
- `pnpm --filter @xpeak/mobile build` succeeds.
- `dotnet test` in `apps/api.Tests` passes with the health
  integration test (green).
- `dotnet format --verify-no-changes` returns clean.
- CI (extended from Phase 1) runs all of the above on PRs and is
  green on this phase's own PR.
- A new `feat(api): ...` commit merged to `main` produces a
  release-please Release PR that bumps root `package.json`,
  `apps/api/VERSION`, and `apps/mobile/package.json` in one PR.

## 11. Next step

Answer the questions in §8, then `tasks.md` gets a final review
and we open the first working branch for Section A.
