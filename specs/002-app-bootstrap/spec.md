# Phase 2 — Application Skeleton (SPEC)

> Companion to `plan.md` (why) and `tasks.md` (how, atomic steps).

## 1. Summary

Scaffold two runnable-but-empty applications inside the monorepo:
`apps/api` (ASP.NET Core 9 Minimal API) and `apps/mobile` (Next.js
15 + Capacitor). Wire both into the CI pipeline from Phase 1.
Prove correctness with a smoke test per app. Ship zero product
features.

## 2. ASP.NET Core API (`apps/api`)

### 2.1. Bootstrap command

Run via the .NET SDK container (Docker-first):

```
docker run --rm -v "$PWD":/workspace -w /workspace \
  --user "$(id -u):$(id -g)" \
  -e HOME=/tmp -e DOTNET_CLI_HOME=/tmp -e DOTNET_NOLOGO=1 \
  mcr.microsoft.com/dotnet/sdk:9.0 \
  sh -c "dotnet new webapi -o apps/api \
           --framework net9.0 --use-controllers false --auth None \
           && dotnet new gitignore -o apps/api"
```

Notes:
- `--use-controllers false` gives us **Minimal APIs**, not the
  legacy controller-based style.
- `--auth None` because we roll our own auth in Phase 3.

### 2.2. Runtime versions

- .NET SDK: pinned via `apps/api/global.json`
  (`sdk.version: "9.0.100"`, `rollForward: "latestMinor"`).
- NuGet packages: locked via `packages.lock.json` after enabling
  `<RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>`
  in `api.csproj` (T-002-07).

### 2.3. Dependencies (NuGet)

Baseline additions on top of what `dotnet new webapi` includes:

- **Runtime:**
  - `Microsoft.EntityFrameworkCore` 9.0.x
  - `Npgsql.EntityFrameworkCore.PostgreSQL` 9.0.x
  - `Microsoft.EntityFrameworkCore.Design` 9.0.x (build-time only)
  - `Microsoft.AspNetCore.OpenApi` 9.0.x (already added by
    template)
- **Tests (in `apps/api.Tests`):**
  - `Microsoft.NET.Test.Sdk`
  - `xunit`, `xunit.runner.visualstudio`
  - `Microsoft.AspNetCore.Mvc.Testing`
  - `Testcontainers.PostgreSql`
  - `FluentAssertions`

### 2.4. Configuration

- `appsettings.json` — base config (defaults).
- `appsettings.Development.json` — dev connection string,
  verbose EF Core logging.
- `Program.cs` reads `Configuration.GetConnectionString("Postgres")`.
- Container/dev connection: `Host=localhost;Port=5433;Database=xpeak_dev;Username=xpeak;Password=xpeak`
  (or `Host=postgres;Port=5432;...` when running from inside the
  compose network).
- CORS policy `XpeakDev` allows
  `http://localhost:3001` and `capacitor://localhost`, any method,
  any header, credentials.
- **OpenAPI** mapped in dev only (`app.MapOpenApi()`).

### 2.5. `Program.cs`

Slim, wires DI + endpoints:

```csharp
using Microsoft.EntityFrameworkCore;
using Xpeak.Api.Endpoints;
using Xpeak.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

const string CorsPolicy = "XpeakDev";
builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicy, policy => policy
        .WithOrigins("http://localhost:3001", "capacitor://localhost")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseCors(CorsPolicy);
app.MapHealthEndpoints();

app.Run();

public partial class Program;
```

### 2.6. Health endpoint

`Endpoints/HealthEndpoints.cs`:

```csharp
namespace Xpeak.Api.Endpoints;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", () =>
        {
            var version = File.ReadAllText("VERSION").Trim();
            return Results.Ok(new HealthResponse(
                Status: "ok",
                Version: version,
                Time: DateTimeOffset.UtcNow
            ));
        })
        .WithName("Health")
        .WithSummary("Reports the app status, version and timestamp.");

        return app;
    }
}

public sealed record HealthResponse(string Status, string Version, DateTimeOffset Time);
```

Response contract:
- Route: `GET /health`
- Status: `200`
- Body:
  ```json
  { "status": "ok", "version": "0.0.1", "time": "2026-01-01T00:00:00Z" }
  ```

### 2.7. Version file

- `apps/api/VERSION` containing the current SemVer string
  (initialized as `0.0.1` to match `.release-please-manifest.json`).
- `HealthEndpoints` reads it via `File.ReadAllText("VERSION")` at
  request time.
- `release-please-config.json` gains an `extra-files` entry pointing
  to `apps/api/VERSION` (type: `generic`, `path: apps/api/VERSION`)
  so future releases keep the file in sync.

### 2.8. `AppDbContext`

`Infrastructure/AppDbContext.cs`:

```csharp
using Microsoft.EntityFrameworkCore;

namespace Xpeak.Api.Infrastructure;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}
```

Empty on purpose — domain models arrive from Phase 3 onwards.

### 2.9. Dev Dockerfile

- `apps/api/Dockerfile.dev` for local dev only (prod dockerfile is
  a later-phase concern).
- Base: `mcr.microsoft.com/dotnet/sdk:9.0` (SDK for `dotnet watch`).
- Two-stage layer strategy:
  1. Copy `*.csproj`, `global.json`, `packages.lock.json`, run
     `dotnet restore --locked-mode` (cached until deps change).
  2. Copy the rest and `dotnet watch run --urls http://0.0.0.0:5000`.
- Working dir: `/app`.

### 2.10. Style + formatter

- `dotnet format` used across the tree.
- Optional: an `.editorconfig` at repo root (already added) governs
  brace style, indent size, `var` usage, using-directive ordering.
- CI runs `dotnet format --verify-no-changes` as part of the
  api-test job.

### 2.11. README

`apps/api/README.md` covers: prerequisites (Docker only — or .NET 9
SDK for local dev), install (`docker compose up -d api`), running
(`docker compose logs -f api`), testing (`docker run ... dotnet
test`), release version (`cat VERSION`).

## 3. Mobile app (`apps/mobile`)

### 3.1. Bootstrap command

```
pnpm create next-app@latest apps/mobile \
  --typescript \
  --tailwind \
  --app \
  --eslint \
  --src-dir=false \
  --import-alias="@/*" \
  --use-pnpm \
  --no-turbopack
```

Tailwind decision deferred to `plan.md` §8 Q1. If we defer the
design system, pass `--no-tailwind` and add it later.

### 3.2. Package name

- `package.json` `name`: `@xpeak/mobile`
- `package.json` `version`: `0.0.1` (kept in sync via
  `release-please` `extra-files`).
- `package.json` `private`: `true`

### 3.3. TypeScript

- `tsconfig.json` extends `../../packages/tsconfig-base.json`
  (shared base already added).
- `"strict": true`, `"noUncheckedIndexedAccess": true`,
  `"noImplicitOverride": true`.

### 3.4. ESLint + Prettier

- Keep the Next.js ESLint preset.
- Add `eslint-config-prettier` to disable style rules that clash
  with Prettier.
- `.prettierrc` at repo root (already added).
- `.prettierignore` at repo root (already added).

### 3.5. Vitest + Testing Library

- Deps: `vitest`, `@vitejs/plugin-react`,
  `@testing-library/react`, `@testing-library/jest-dom`, `jsdom`.
- `vitest.config.ts` in `apps/mobile/`:
  - `environment: 'jsdom'`
  - `setupFiles: ['./vitest.setup.ts']`
  - `globals: true`
- `vitest.setup.ts` imports `@testing-library/jest-dom/vitest`.
- Scripts: `"test": "vitest run"`, `"test:watch": "vitest"`.
- First spec: `app/__tests__/page.test.tsx` asserts the root page
  renders "Hello XPeak".

### 3.6. Root layout & page

- `app/layout.tsx`: sets `<html lang="en">`, viewport meta with
  `width=device-width, initial-scale=1, viewport-fit=cover`,
  safe-area handling via CSS env vars.
- `app/page.tsx`: renders a single `<main>` with the text "Hello
  XPeak" and a small caption "You'll level up here soon." — enough
  for the smoke test, no styling ambition beyond mobile-first
  centering.

### 3.7. Capacitor

- Deps: `@capacitor/core`, `@capacitor/cli` (dev),
  `@capacitor/android`, `@capacitor/ios`.
- Init: `pnpm --filter @xpeak/mobile exec cap init "XPeak"
  "com.xpeak.app" --web-dir=out`.
- `next.config.ts` sets `output: 'export'` and
  `images: { unoptimized: true }` so `pnpm build && pnpm exec cap sync`
  produces a static bundle for the native wrapper.
- Scripts:
  - `"build:web": "next build"`
  - `"cap:sync": "cap sync"`
  - `"cap:open:android": "cap open android"`
  - `"cap:open:ios": "cap open ios"`
- Generate platforms:
  - `pnpm --filter @xpeak/mobile exec cap add android`
  - `pnpm --filter @xpeak/mobile exec cap add ios` (may fail
    without macOS — accept and document).

### 3.8. Environment variables

- `.env.example` with:
  - `NEXT_PUBLIC_API_URL=http://localhost:5000`
- No secrets ever committed. `.env.local` stays gitignored.

### 3.9. README

`apps/mobile/README.md` covers: install (`pnpm install`), dev
(`pnpm --filter @xpeak/mobile dev`), test (`pnpm --filter
@xpeak/mobile test`), build (`pnpm --filter @xpeak/mobile build`),
native (`pnpm --filter @xpeak/mobile cap:sync && cap:open:android`).

## 4. `packages/shared`

- Scaffold only. Empty barrel: `packages/shared/src/index.ts`
  exports nothing yet.
- `package.json` `name`: `@xpeak/shared`, `private: true`,
  `main: "./src/index.ts"`, `types: "./src/index.ts"`.
- `tsconfig.json` extends `../tsconfig-base.json`.
- Consumed via `workspace:*` from `apps/mobile` (added as dep so
  the wiring is proven, even with no exports yet).

## 5. Shared tooling at repo root

- `packages/tsconfig-base.json` — strict base tsconfig that mobile
  and shared extend (already added).
- Root `.prettierrc` (already added).
- Root `.editorconfig` (already added).

## 6. `docker-compose.yml`

At the repo root. Services:

- `postgres` (image `postgres:16-alpine`, env
  `POSTGRES_USER=xpeak/POSTGRES_PASSWORD=xpeak/POSTGRES_DB=xpeak_dev`,
  volume `xpeak_pg_data`, host `5433` → container `5432`).
- `api` (build `./apps/api/Dockerfile.dev`, depends on postgres
  healthy, mounts `./apps/api:/app` for hot reload, port `5000:5000`).

## 7. CI additions (`.github/workflows/ci.yml`)

Two new jobs on top of Phase 1 (and drop the vestigial `lint`
job):

### 7.1. `api-test`

- `runs-on: ubuntu-latest`
- Services: `postgres:16` on port 5432 with health-check.
- Steps:
  - Checkout.
  - `actions/setup-dotnet@v4` with `dotnet-version: 9.0.x`
    (or reads `global.json`).
  - Cache NuGet packages keyed on `packages.lock.json` hash.
  - `dotnet restore --locked-mode`
  - `dotnet format --verify-no-changes`
  - `dotnet build --no-restore --configuration Release
    -warnaserror`
  - `dotnet test apps/api.Tests/api.Tests.csproj --no-build
    --configuration Release --logger "trx" -- RunConfiguration.CollectSourceInformation=true`

### 7.2. `mobile-test`

- Reuses the existing pnpm setup from Phase 1.
- Steps: `pnpm --filter @xpeak/mobile test`,
  `pnpm --filter @xpeak/mobile build`.

### 7.3. Drop `lint` job

The vestigial `lint` job in `ci.yml` (from XPK-2) is removed as
part of this phase — it's still required to be reflected in the
Ruleset on `main` (see `T-002-25`).

Both new jobs added to the required checks list on `main` (branch
protection update in `T-002-25`).

## 8. Version wiring with release-please

Add `extra-files` in `release-please-config.json`:

```
{
  "packages": {
    ".": {
      "release-type": "node",
      "extra-files": [
        { "type": "generic", "path": "apps/api/VERSION" },
        { "type": "json", "path": "apps/mobile/package.json",
          "jsonpath": "$.version" }
      ]
    }
  }
}
```

Result: bumps flow from a single root release into both apps'
version files. `apps/api/VERSION` is the source of truth for the
API; `HealthEndpoints` reads it at request time so no rebuild is
needed for release-please to reflect the bump.

## 9. Success criteria

- `dotnet test` in `apps/api.Tests` passes with the health
  integration test (green).
- `dotnet format --verify-no-changes` returns clean.
- `dotnet build --warnaserror` returns 0 warnings, 0 errors.
- `curl localhost:5000/health` returns
  `{"status":"ok","version":"0.0.1", ...}` when the api container
  is up.
- `pnpm --filter @xpeak/mobile test` passes with the root page
  render spec (green).
- `pnpm --filter @xpeak/mobile build` succeeds (static export
  emitted to `out/`).
- `pnpm --filter @xpeak/mobile cap sync` completes without errors
  on Linux (iOS platform generation may be skipped, documented).
- `docker compose up -d` brings up postgres + api healthy.
- CI runs `api-test` + `mobile-test` on the phase's own PR and
  both are green.
- `release-please` Release PR generated after this phase's merge
  bumps `apps/api/VERSION` and `apps/mobile/package.json` `version`.

## 10. Non-goals (explicit)

- Auth (Identity, JWT, Google OAuth) — Phase 3.
- Any domain model (User, Category, CheckIn) — Phase 3+.
- Hangfire workers actually processing jobs — later.
- Production Dockerfile / deployment — later.
- iOS native build validation on a Mac — later, when we have a
  Mac in the loop.

## 11. Risks and mitigations

| Risk                                                    | Mitigation                                                                 |
|---------------------------------------------------------|----------------------------------------------------------------------------|
| .NET SDK version mismatch dev↔CI                        | `global.json` is the single source of truth; CI reads it directly.         |
| Capacitor iOS platform generation fails on Linux        | Document the caveat in `apps/mobile/README.md`; skip in CI on Linux.       |
| Static export breaks App Router server components       | Restrict the smoke page to client + static; add lint rule later if needed. |
| CORS misconfig blocks mobile → api during dev           | Integration test covers preflight from `capacitor://localhost` and `localhost:3001`. |
| release-please `extra-files` desync                     | `T-002-27` explicitly bumps a manual version to confirm the flow.          |
| Docker on Linux runs Postgres as root and locks volume  | Named volume `xpeak_pg_data`; documented `docker compose down -v` reset.   |
| Testcontainers requires Docker socket in CI             | GitHub Actions ubuntu runners have Docker enabled — Testcontainers works OOTB. |
