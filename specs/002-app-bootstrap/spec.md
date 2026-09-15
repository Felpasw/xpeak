# Phase 2 — Application Skeleton (SPEC)

> Companion to `plan.md` (why) and `tasks.md` (how, atomic steps).

## 1. Summary

Scaffold two runnable-but-empty applications inside the monorepo:
`apps/api` (Phoenix 1.7, API mode) and `apps/mobile` (Next.js 15 +
Capacitor). Wire both into the CI pipeline from Phase 1. Prove
correctness with a smoke test per app. Ship zero product features.

## 2. Phoenix API (`apps/api`)

### 2.1. Bootstrap command

```
mix archive.install hex phx_new --force
mix phx.new apps/api \
  --app xpeak \
  --module Xpeak \
  --database postgres \
  --binary-id \
  --no-html \
  --no-assets \
  --no-live \
  --no-mailer \
  --install
```

Notes:
- `--app xpeak` sets the OTP application atom.
- `--module Xpeak` sets the root module namespace (`Xpeak`,
  `XpeakWeb`).
- `--binary-id` makes every schema use UUID PKs by default (matches
  the Phase 3 decision to use UUIDs for users).
- `--no-html`, `--no-assets`, `--no-live` strip the HTML/LiveView
  layers. Phoenix Channels (WebSocket) remain available for later
  realtime work without adding LiveView back.
- `--no-mailer` because email flows land later (or never — see
  Phase 3 non-goals).
- `--install` runs `mix deps.get` and `mix ecto.setup` at the end.

### 2.2. Runtime versions

- Elixir + Erlang/OTP: pinned via `apps/api/.tool-versions` (asdf).
  Version choice deferred to `plan.md` §8 Q1.
- Hex packages: locked in `mix.lock`.

### 2.3. Dependencies (`mix.exs`)

Baseline additions on top of what `phx.new` includes:

- **Web / runtime:**
  - `corsica` — CORS handling (drops `Plug.Cors` custom code).
  - `plug_cowboy` or `bandit` — `bandit` is the Phoenix 1.7 default;
    keep it.
- **Testing:**
  - `ex_machina` — factories.
  - `mox` — behaviour-based mocking (for later external integrations).
- **Style / quality:**
  - `credo` — linter.
  - `dialyxir` — Dialyzer wrapper (runs opportunistically, not on
    every commit — see §7.3).
- **Config:**
  - `dotenvy` or `envx` (or plain `System.get_env` in `runtime.exs`
    — final choice in task). Dev/test only.

### 2.4. Configuration

- `config/config.exs`: base config (endpoint, repo).
- `config/dev.exs`: dev DB, `code_reloader: true`, `debug_errors:
  true`, listens on port `4000`.
- `config/test.exs`: test DB (`xpeak_test`), `pool:
  Ecto.Adapters.SQL.Sandbox`, endpoint on port `4002`.
- `config/prod.exs`: minimal — real config comes from
  `runtime.exs`.
- `config/runtime.exs`: `DATABASE_URL`, `SECRET_KEY_BASE`, `PORT`,
  `PHX_HOST` read from ENV.
- **CORS** (Corsica) mounted in `XpeakWeb.Endpoint` before the
  router in dev/test: allow `http://localhost:3001` and
  `capacitor://localhost`. Prod allow-list stays empty until deploy
  phase.

### 2.5. Router

`lib/xpeak_web/router.ex`:

```elixir
defmodule XpeakWeb.Router do
  use XpeakWeb, :router

  pipeline :api do
    plug :accepts, ["json"]
  end

  scope "/", XpeakWeb do
    pipe_through :api

    get "/health", HealthController, :show
  end
end
```

### 2.6. Health endpoint

- Route: `GET /health`
- Controller: `XpeakWeb.HealthController.show/2`
- Response: `{"status":"ok","version":"<VERSION>","time":"<ISO8601>"}`
- Status code: `200`
- `<VERSION>` reads `File.read!(Path.join(:code.priv_dir(:xpeak),
  "..") <> "/VERSION") |> String.trim()` **or** simpler:
  `Application.spec(:xpeak, :vsn) |> to_string()` since `mix.exs`
  reads the same file at compile time.

### 2.7. Version file

- `apps/api/VERSION` containing the current SemVer string
  (initialized as `0.0.1` to match `.release-please-manifest.json`).
- `apps/api/mix.exs` reads it:

  ```elixir
  @version File.read!("VERSION") |> String.trim()

  def project do
    [
      app: :xpeak,
      version: @version,
      # ...
    ]
  end
  ```

- `release-please-config.json` gains an `extra-files` entry pointing
  to `apps/api/VERSION` (type: `generic`, `path: apps/api/VERSION`)
  so future releases keep the file in sync.

### 2.8. Formatter, Credo, Dialyzer

- `apps/api/.formatter.exs` — default from `phx.new` (respects Phoenix
  imports).
- `apps/api/.credo.exs` — start from `mix credo.gen.config`; enable
  `--strict` mode; keep defaults sensible (some `Design.*` checks
  relaxed for generated code).
- `apps/api/.dialyzer_ignore.exs` — empty file; populated as we
  encounter false positives.
- Dialyzer PLTs cached in CI under `_build/dev/*.plt`.

### 2.9. Dev Dockerfile

- `apps/api/Dockerfile` for local dev only (prod dockerfile is a
  later-phase concern).
- Base: `hexpm/elixir:<elixir>-erlang-<otp>-alpine-<alpine>`.
- Installs `build-base`, `git`, `openssl-dev`, `postgresql-client`.
- Copies `mix.exs`, `mix.lock`, runs `mix deps.get`, then copies
  the rest.
- `CMD ["mix", "phx.server"]`.

### 2.10. README

`apps/api/README.md` covers: prerequisites (asdf, Erlang, Elixir,
Postgres), install (`asdf install && mix deps.get && mix
ecto.setup`), running (`mix phx.server`), testing (`mix test`),
linting (`mix format --check-formatted && mix credo --strict`),
release version (`mix run -e "IO.puts Application.spec(:xpeak,
:vsn)"`).

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

Tailwind decision deferred to `plan.md` §8 Q2. If we defer the design
system, pass `--no-tailwind` and add it later.

### 3.2. Package name

- `package.json` `name`: `@xpeak/mobile`
- `package.json` `version`: `0.0.1` (kept in sync via `release-please`
  `extra-files`).
- `package.json` `private`: `true`

### 3.3. TypeScript

- `tsconfig.json` extends `../../packages/tsconfig-base.json` (shared
  base created in §5).
- `"strict": true`, `"noUncheckedIndexedAccess": true`,
  `"noImplicitOverride": true`.

### 3.4. ESLint + Prettier

- Keep the Next.js ESLint preset.
- Add `eslint-config-prettier` to disable style rules that clash with
  Prettier.
- `.prettierrc` at repo root (shared).
- `.prettierignore` at repo root.

### 3.5. Vitest + Testing Library

- Deps: `vitest`, `@vitejs/plugin-react`, `@testing-library/react`,
  `@testing-library/jest-dom`, `jsdom`.
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
  `width=device-width, initial-scale=1, viewport-fit=cover`, safe-area
  handling via CSS env vars.
- `app/page.tsx`: renders a single `<main>` with the text "Hello
  XPeak" and a small caption "You'll level up here soon." — enough
  for the smoke test, no styling ambition beyond mobile-first
  centering.

### 3.7. Capacitor

- Deps: `@capacitor/core`, `@capacitor/cli` (dev),
  `@capacitor/android`, `@capacitor/ios`.
- Init: `pnpm --filter @xpeak/mobile exec cap init "XPeak"
  "com.xpeak.app" --web-dir=out`.
- `next.config.ts` sets `output: 'export'` and `images: { unoptimized:
  true }` so `pnpm build && pnpm exec cap sync` produces a static
  bundle for the native wrapper.
- Scripts:
  - `"build:web": "next build"`
  - `"cap:sync": "cap sync"`
  - `"cap:open:android": "cap open android"`
  - `"cap:open:ios": "cap open ios"`
- Generate platforms:
  - `pnpm --filter @xpeak/mobile exec cap add android`
  - `pnpm --filter @xpeak/mobile exec cap add ios` (may fail without
    macOS — accept and document).

### 3.8. Environment variables

- `.env.example` with:
  - `NEXT_PUBLIC_API_URL=http://localhost:4000`
- No secrets ever committed. `.env.local` stays gitignored.

### 3.9. README

`apps/mobile/README.md` covers: install (`pnpm install`), dev
(`pnpm --filter @xpeak/mobile dev`), test (`pnpm --filter
@xpeak/mobile test`), build (`pnpm --filter @xpeak/mobile build`),
native (`pnpm --filter @xpeak/mobile cap:sync && cap:open:android`).

## 4. `packages/shared`

- Scaffold only. Empty barrel: `packages/shared/src/index.ts` exports
  nothing yet.
- `package.json` `name`: `@xpeak/shared`, `private: true`,
  `main: "./src/index.ts"`, `types: "./src/index.ts"`.
- `tsconfig.json` extends `../tsconfig-base.json`.
- Consumed via `workspace:*` from `apps/mobile` (added as dep so the
  wiring is proven, even with no exports yet).

## 5. Shared tooling at repo root

- `packages/tsconfig-base.json` — strict base tsconfig that mobile and
  shared extend.
- Root `.prettierrc` (single source of truth for both apps).
- Root `.editorconfig` (line endings, indent width, final newline).

## 6. `docker-compose.yml`

At the repo root. Services:

- `postgres` (image `postgres:16-alpine`, env
  `POSTGRES_USER=xpeak/POSTGRES_PASSWORD=xpeak/POSTGRES_DB=xpeak_dev`,
  volume `xpeak_pg_data`).
- Optional: `api` service depending on `plan.md` §8 Q4.

Ports:
- `5432` → host `5432` (or `54322` if the host already runs Postgres —
  documented in README).

Redis intentionally omitted: Oban stores jobs in Postgres and we
don't need a separate cache yet.

## 7. CI additions (`.github/workflows/ci.yml`)

Two new jobs on top of Phase 1:

### 7.1. `api-test`

- `runs-on: ubuntu-latest`
- Services: `postgres:16` on port 5432 with health-check.
- Steps:
  - Checkout.
  - `erlef/setup-beam@v1` reading `otp-version` and `elixir-version`
    from `apps/api/.tool-versions`.
  - Cache `apps/api/deps/` and `apps/api/_build/` keyed on
    `mix.lock` hash.
  - `mix deps.get --only test`
  - `mix format --check-formatted`
  - `mix credo --strict`
  - `mix compile --warnings-as-errors`
  - `mix ecto.create --quiet`
  - `mix ecto.migrate --quiet`
  - `mix test`

### 7.2. `mobile-test`

- Reuses the existing pnpm setup from Phase 1.
- Steps: `pnpm --filter @xpeak/mobile lint`,
  `pnpm --filter @xpeak/mobile test`,
  `pnpm --filter @xpeak/mobile build`.

Both jobs added to the required checks list on `main` (branch
protection update in `T-002-23`).

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
version files. `apps/api/mix.exs` reads `VERSION` at compile time so
no separate manifest edit is needed on the api side.

## 9. Success criteria

- `mix test` in `apps/api` passes with the health controller test
  (green).
- `mix format --check-formatted`, `mix credo --strict`, and
  `mix compile --warnings-as-errors` all return clean.
- `curl localhost:4000/health` returns
  `{"status":"ok","version":"0.0.1", ...}` when the app is booted
  locally.
- `pnpm --filter @xpeak/mobile test` passes with the root page
  render spec (green).
- `pnpm --filter @xpeak/mobile build` succeeds (static export
  emitted to `out/`).
- `pnpm --filter @xpeak/mobile cap sync` completes without errors on
  Linux (iOS platform generation may be skipped, documented).
- `docker compose up -d postgres` starts the service and Phoenix
  connects on `mix ecto.setup`.
- CI runs `api-test` + `mobile-test` on the phase's own PR and both
  are green.
- `release-please` Release PR generated after this phase's merge
  bumps `apps/api/VERSION` and `apps/mobile/package.json` `version`.

## 10. Non-goals (explicit)

- Auth (Guardian, hashing) — Phase 3.
- Any domain model (User, Category, CheckIn) — Phase 3+.
- Oban workers actually processing jobs — later.
- Production Dockerfile / deployment — later.
- iOS native build validation on a Mac — later, when we have a Mac
  in the loop.

## 11. Risks and mitigations

| Risk                                                    | Mitigation                                                                 |
|---------------------------------------------------------|----------------------------------------------------------------------------|
| Elixir/OTP version mismatch dev↔CI                      | `.tool-versions` is the single source of truth; CI reads it directly.      |
| Capacitor iOS platform generation fails on Linux        | Document the caveat in `apps/mobile/README.md`; skip in CI on Linux.       |
| Static export breaks App Router server components       | Restrict the smoke page to client + static; add lint rule later if needed. |
| CORS misconfig blocks mobile → api during dev           | Controller test covers preflight from `capacitor://localhost` and localhost:3001. |
| release-please `extra-files` desync                     | `T-002-25` explicitly bumps a manual version to confirm the flow.          |
| Docker on Linux runs Postgres as root and locks volume  | Named volume `xpeak_pg_data`; documented `docker compose down -v` reset.   |
| Cold BEAM builds slow down CI                           | Cache `deps/` and `_build/` keyed on `mix.lock` hash.                      |
