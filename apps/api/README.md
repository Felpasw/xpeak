# Xpeak API

ASP.NET Core 9 Minimal API for XPeak. Postgres via EF Core.
Docker-first: no need to install the .NET SDK on the host.

## Prerequisites

- Docker + Docker Compose v2
- (Optional) .NET 9 SDK if you prefer running `dotnet` locally

## Run it

From the repo root:

```sh
docker compose up -d --build api
```

The API will be at http://localhost:5000. Sanity check:

```sh
curl -s http://localhost:5000/health | jq
# { "status": "ok", "version": "0.0.1", "time": "..." }
```

Stop everything:

```sh
docker compose down
# or `docker compose down -v` to also wipe the postgres volume
```

Tail logs:

```sh
docker compose logs -f api
```

## Tests

Integration tests use `Microsoft.AspNetCore.Mvc.Testing` +
`Testcontainers.PostgreSql`. They need the host Docker socket, and
should run via the .NET SDK container with `--group-add docker`.

```sh
docker run --rm \
  -v "$PWD/../..":/workspace \
  -v /var/run/docker.sock:/var/run/docker.sock \
  -w /workspace/apps/api.Tests \
  --user "$(id -u):$(id -g)" \
  --group-add "$(getent group docker | cut -d: -f3)" \
  -e HOME=/tmp -e DOTNET_CLI_HOME=/tmp -e DOTNET_NOLOGO=1 \
  mcr.microsoft.com/dotnet/sdk:9.0 \
  dotnet test --nologo
```

## Formatting

```sh
# Auto-fix
docker run --rm -v "$PWD":/app -w /app --user "$(id -u):$(id -g)" \
  -e HOME=/tmp -e DOTNET_CLI_HOME=/tmp -e DOTNET_NOLOGO=1 \
  mcr.microsoft.com/dotnet/sdk:9.0 dotnet format

# Verify (CI-friendly)
docker run --rm -v "$PWD":/app -w /app --user "$(id -u):$(id -g)" \
  -e HOME=/tmp -e DOTNET_CLI_HOME=/tmp -e DOTNET_NOLOGO=1 \
  mcr.microsoft.com/dotnet/sdk:9.0 dotnet format --verify-no-changes
```

## Optional: run without Docker

Install .NET 9 SDK on the host (see `global.json`).

```sh
docker compose up -d postgres   # still need postgres
cd apps/api
dotnet restore --use-lock-file
ConnectionStrings__Postgres="Host=localhost;Port=5433;Database=xpeak_dev;Username=xpeak;Password=xpeak" \
  dotnet run
```

## Project structure

```
apps/api/
  Program.cs                       # host + DI + endpoints wiring
  Endpoints/HealthEndpoints.cs     # GET /health
  Infrastructure/AppDbContext.cs   # EF Core context (empty for now)
  Domain/                          # domain models (arrive from Phase 3)
  api.csproj                       # SDK + package refs + VERSION copy
  global.json                      # .NET SDK pin (9.0.100)
  packages.lock.json               # NuGet lockfile
  VERSION                          # single source of truth for release-please
  appsettings.json                 # base config
  appsettings.Development.json     # dev connection string + verbose EF logs
  Dockerfile.dev                   # dev image with `dotnet watch`
  .env.example                     # env template
```

## Release version

The current API version lives in `apps/api/VERSION` and is bumped by
`release-please` at the repo root. The health endpoint reads it at
request time from `AppContext.BaseDirectory/VERSION` (the csproj
copies it to the output folder on build).
