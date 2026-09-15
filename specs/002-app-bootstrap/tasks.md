# Phase 2 — Application Skeleton (TASKS)

> Companion to `spec.md` (what) and `plan.md` (why). Atomic
> execution steps. Nothing implemented yet.

## Legend

- `[T]` — TDD required: failing test first.
- `[S]` — sequential: order matters.
- `[P]` — parallelizable with sibling `[P]` tasks.
- `[HUMAN]` — human-only step (no automation possible).

Status glyphs:
- `[ ]` not started
- `[~]` in progress
- `[x] ✅ commit <hash>` done

---

## Section A — Repo baseline for two apps

- [x] **T-002-01 `[S]`** — Root `packages/tsconfig-base.json`
  (already added in XPK-3 pre-work).
- [x] **T-002-02 `[P]`** — Root Prettier + EditorConfig
  (`.prettierrc`, `.prettierignore`, `.editorconfig` — already
  added).
- [x] **T-002-03 `[S]`** — `docker-compose.yml` with Postgres on
  host `5433` (added; needs `api` service in T-002-14).

## Section B — `packages/shared` skeleton

- [x] **T-002-04 `[S]`** — Scaffold `packages/shared`
  (`package.json`, `tsconfig.json`, `src/index.ts`) — already
  added.

## Section C — ASP.NET Core API bootstrap

- [x] **T-002-05 `[S]`** — Bootstrap ASP.NET Core Web API
  (`dotnet new webapi -o apps/api --use-controllers false --auth None`
  via Docker) — done in XPK-3 pre-work.

- [x] **T-002-06 `[S]`** — Pin SDK via `apps/api/global.json` and
  add `apps/api/VERSION` (`0.0.1`).

- [ ] **T-002-07 `[S]`** — Enable NuGet lockfile
  - Add `<RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>`
    to `api.csproj`.
  - Run `dotnet restore --use-lock-file` to generate
    `packages.lock.json`.
  - Deliverable: updated `api.csproj`, `packages.lock.json`
    committed.

- [x] **T-002-08 `[S]`** — Add EF Core + Npgsql packages,
  `AppDbContext`, wire `AddDbContext<AppDbContext>` in
  `Program.cs` (done in XPK-3 pre-work).

- [x] **T-002-09 `[S]`** — CORS policy `XpeakDev` mounted in
  `Program.cs` (done).

- [ ] **T-002-10 `[T][S]`** — Test project + failing health test
  - Create `apps/api.Tests` project via
    `dotnet new xunit -o apps/api.Tests --framework net9.0`.
  - Reference `api.csproj`.
  - Add packages: `Microsoft.AspNetCore.Mvc.Testing`,
    `Testcontainers.PostgreSql`, `FluentAssertions`.
  - Add `Support/XpeakWebApplicationFactory.cs` — starts a
    Testcontainers Postgres and overrides the `AppDbContext`
    connection string.
  - **Write** `HealthEndpointTests.cs`:
    - `Get_returns_ok_with_status_version_and_time`
    - `Preflight_from_localhost_3001_is_allowed`
    - `Preflight_from_capacitor_scheme_is_allowed`
  - Run `dotnet test` — expect **RED** (nothing wired end-to-end
    yet). Actually /health is implemented but Testcontainers
    plumbing is new — the RED here is "test project doesn't even
    compile yet".
  - Deliverable: `apps/api.Tests/` with red-then-green suite.

- [ ] **T-002-11 `[S]`** — Make T-002-10 green
  - Wire `WebApplicationFactory<Program>` correctly (override
    connection string, warm the DB).
  - Adjust CORS test to use `OPTIONS` + proper preflight headers.
  - Run `dotnet test` — expect green.
  - Deliverable: green suite.

- [ ] **T-002-12 `[P]`** — `dotnet format` baseline
  - Run `dotnet format` once on the tree so future
    `--verify-no-changes` runs stay clean.
  - Deliverable: no diff after subsequent `dotnet format
    --verify-no-changes`.

- [ ] **T-002-13 `[P]`** — Dev Dockerfile
  - `apps/api/Dockerfile.dev` per `spec.md` §2.9 (two-stage,
    layered restore, `dotnet watch run`).
  - Manual test: `docker build -f apps/api/Dockerfile.dev -t
    xpeak-api-dev apps/api` succeeds.
  - Deliverable: `Dockerfile.dev`, `.dockerignore`.

- [ ] **T-002-14 `[S]`** — Extend `docker-compose.yml` with `api`
  service
  - Service `api` builds `apps/api/Dockerfile.dev`, depends on
    postgres healthy, mounts `./apps/api:/app`, exposes `5000:5000`.
  - Env `ConnectionStrings__Postgres` points at `postgres:5432`.
  - Manual test: `docker compose up -d api && curl -f
    localhost:5000/health` succeeds.
  - Deliverable: updated `docker-compose.yml`.

- [ ] **T-002-15 `[P]`** — `apps/api/.env.example` + README
  - Env vars: `ASPNETCORE_ENVIRONMENT`, `ConnectionStrings__Postgres`,
    `ASPNETCORE_URLS`.
  - README per `spec.md` §2.11 (Docker-first + optional local
    SDK).
  - Deliverable: `.env.example`, `README.md`.

## Section D — Next.js + Capacitor bootstrap

- [ ] **T-002-16 `[S]`** — Answer prerequisites
  - Confirm design system decision (`plan.md` §8 Q1).
  - Confirm PWA scope (`plan.md` §8 Q2).
  - Deliverable: answers pasted into this task before T-002-17.

- [ ] **T-002-17 `[S]`** — Bootstrap Next.js app
  - Run the `pnpm create next-app` command from `spec.md` §3.1.
  - Rename package to `@xpeak/mobile` in
    `apps/mobile/package.json`.
  - Add `workspace:*` dep on `@xpeak/shared`.
  - Configure `tsconfig.json` to extend
    `../../packages/tsconfig-base.json` per `spec.md` §3.3.
  - `pnpm install` at repo root runs clean.
  - Deliverable: `apps/mobile/` populated.

- [ ] **T-002-18 `[T][S]`** — Vitest install + failing smoke spec
  - Install deps from `spec.md` §3.5.
  - Add `vitest.config.ts` + `vitest.setup.ts`.
  - **Write** `app/__tests__/page.test.tsx` expecting "Hello
    XPeak" in the DOM.
  - `pnpm --filter @xpeak/mobile test` — expect red.
  - Deliverable: red spec, config files.

- [ ] **T-002-19 `[S]`** — Root layout + Hello page (make green)
  - `app/layout.tsx` per `spec.md` §3.6.
  - `app/page.tsx` renders "Hello XPeak".
  - `pnpm --filter @xpeak/mobile test` — green.
  - Deliverable: two files under `apps/mobile/app/`.

- [ ] **T-002-20 `[S]`** — Capacitor init and platforms
  - Install deps from `spec.md` §3.7.
  - Run `cap init` with app name/id.
  - Set `next.config.ts` static export options.
  - Add npm scripts for `build:web`, `cap:sync`, `cap:open:*`.
  - Run `cap add android`.
  - Run `cap add ios` (skip if on Linux — document in README).
  - Deliverable: `capacitor.config.ts`, `android/`, (optionally
    `ios/`).

- [ ] **T-002-21 `[P]`** — `apps/mobile/.env.example` + README
  - Env var: `NEXT_PUBLIC_API_URL=http://localhost:5000`.
  - README per `spec.md` §3.9 (dev, test, build, native).
  - Deliverable: `.env.example`, `README.md`.

- [ ] **T-002-22 `[P]`** — ESLint tuning + Prettier integration
  - Add `eslint-config-prettier` and register it in the Next lint
    config.
  - `pnpm --filter @xpeak/mobile lint` clean.
  - Deliverable: updated ESLint config.

## Section E — CI expansion + Lint cleanup

- [ ] **T-002-23 `[T][S]`** — `api-test` job
  - Failing state: current CI doesn't run .NET tests.
  - Add the job per `spec.md` §7.1 (Postgres service,
    setup-dotnet, cache NuGet, format/build/test).
  - Push branch, watch CI go green.
  - Deliverable: updated `.github/workflows/ci.yml`.

- [ ] **T-002-24 `[T][S]`** — `mobile-test` job + drop `lint` job
  - Add job per `spec.md` §7.2 (test, build).
  - Remove the vestigial `lint` job from `ci.yml`.
  - Push branch, watch CI go green.
  - Deliverable: updated `.github/workflows/ci.yml`.

- [ ] **T-002-25 `[HUMAN]`** — Update branch protection
  - Add `api-test` and `mobile-test` to the list of required
    checks on `main` (via `Rulesets → main-protection`).
  - Deliverable: setting applied in the GitHub UI.

## Section F — release-please version wiring

- [ ] **T-002-26 `[S]`** — `extra-files` in
  `release-please-config.json`
  - Add entries for `apps/api/VERSION` and
    `apps/mobile/package.json` `$.version` per `spec.md` §8.
  - Deliverable: updated `release-please-config.json`.

- [ ] **T-002-27 `[T][S]`** — Dry-run bump propagation
  - Merge a trivial `feat(api): ...` PR into `main`.
  - Observe: release-please opens a Release PR bumping root
    `package.json`, `apps/api/VERSION`, and
    `apps/mobile/package.json` in a single Release PR.
  - Auto-merge fires after CI green.
  - Deliverable: link to the published release, screenshots of
    the three version bumps.

## Section G — End-to-end validation

- [ ] **T-002-28 `[T][S]`** — Local stack smoke test
  - `docker compose up -d`
  - `curl -s localhost:5000/health | jq` matches the contract.
  - `pnpm --filter @xpeak/mobile dev` boots on port 3001.
  - Browser at `http://localhost:3001` shows "Hello XPeak".
  - Deliverable: screenshots pasted into
    `docs/adr/0002-app-skeleton.md`.

- [ ] **T-002-29 `[P]`** — ADR
  - `docs/adr/0002-app-skeleton.md` documenting bootstrap
    decisions: C# on .NET 9, Minimal APIs, EF Core + Npgsql,
    Testcontainers for integration tests, Capacitor platform
    caveats on Linux, release-please version fan-out,
    Docker-first workflow (SDK optional on host).
  - Deliverable: ADR file.

- [ ] **T-002-30 `[S]`** — Cleanup and phase close
  - Revert any sandbox commits used for dry-runs.
  - Mark all tasks `[x]` with their commit hashes.
  - Confirm phase-close conditions from `spec.md` §9.
  - Deliverable: clean `main`, ready for Phase 3 (Auth + basic
    profile).

---

## Summary of deliverables

- `packages/tsconfig-base.json`
- `packages/shared/{package.json,tsconfig.json,src/index.ts}`
- `.prettierrc`, `.prettierignore`, `.editorconfig`
- `docker-compose.yml`
- `apps/api/` full ASP.NET Core 9 skeleton (`api.csproj`,
  `Program.cs`, `Endpoints/HealthEndpoints.cs`,
  `Infrastructure/AppDbContext.cs`, `Domain/`, `VERSION`,
  `global.json`, `packages.lock.json`, `appsettings*.json`,
  `Dockerfile.dev`, `.dockerignore`, `README.md`, `.env.example`)
- `apps/api.Tests/` xUnit test project with `HealthEndpointTests`
  + Testcontainers-backed `WebApplicationFactory`
- `apps/mobile/` full Next.js 15 skeleton (`app/`,
  `capacitor.config.ts`, `android/`, `[ios/]`, `next.config.ts`,
  `vitest.config.ts`, `vitest.setup.ts`, `README.md`,
  `.env.example`)
- `.github/workflows/ci.yml` extended with `api-test` and
  `mobile-test`, `lint` job removed
- `release-please-config.json` extended with `extra-files`
- `docs/adr/0002-app-skeleton.md`

## Dependencies

```
A (root baseline)
  ↓
B (shared package)  ─── parallel with ───  C (ASP.NET Core API)
                                              ↓
                                           D (Next.js + Capacitor)
                                              ↓
                                           E (CI expansion + lint drop)
                                              ↓
                                           F (release-please wiring)
                                              ↓
                                           G (E2E validation + ADR)
```

## Bundling strategy for commits

Because we're doing this as a single PR (XPK-3), suggested
squash-friendly staging:

1. `chore(repo): add shared tsconfig, prettier, editorconfig` — A
   (already staged)
2. `chore(repo): add docker-compose with postgres` — A (already
   staged)
3. `chore(shared): scaffold @xpeak/shared package` — B (already
   staged)
4. `feat(api): bootstrap asp.net core 9 minimal api with health endpoint` — C
   (T-002-05 → T-002-15)
5. `test(api): integration test suite with testcontainers postgres` — C
   (T-002-10, T-002-11)
6. `feat(mobile): bootstrap next.js app with vitest and capacitor` — D
   (T-002-16 → T-002-22)
7. `ci: add api and mobile test jobs, drop lint` — E
8. `ci(release): fan out version bumps to api and mobile` — F
9. `docs(adr): app skeleton decisions` — G (T-002-29)

Since this is one PR, we squash-merge with a single subject at the
end (probably
`feat: bootstrap api (asp.net core 9) and mobile (next.js + capacitor)`).
