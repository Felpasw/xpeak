# Phase 2 — Application Skeleton (Phoenix API + Next.js/Capacitor)

> Status: **planning only**. No code written yet. Companion to
> `spec.md` (details) and `tasks.md` (execution breakdown).

## 1. Goal

Bring up the two application skeletons in the monorepo:

- `apps/api` — Elixir + Phoenix 1.7 in **API mode** (no HTML/assets/
  LiveView by default), Postgres-backed via Ecto, ExUnit-first.
- `apps/mobile` — Next.js 15 (App Router) with Capacitor wrapping for
  Android/iOS, Vitest for unit tests, mobile-first foundation.
- `packages/shared` — TypeScript workspace package for cross-app types
  and constants (scaffold only, empty for now).

Everything wired into the CI pipeline created in Phase 1, everything
runnable locally with a single command, everything covered by at least
one smoke test that proves the app boots.

## 2. Scope

**In scope**
- Phoenix 1.7 API-only bootstrap
  (`mix phx.new --no-html --no-assets --no-live --binary-id`).
- ExUnit + ExMachina + Credo + Dialyxir set up for the api.
- Health check endpoint (`GET /health`) with a passing request test.
- Postgres service via `docker-compose.yml` for local dev
  (Redis dropped as a hard dep — Oban jobs later live in Postgres;
  Redis can be added when we actually need cache/pubsub).
- Next.js 15 bootstrap (App Router, strict TS, ESLint).
- Vitest + Testing Library on the mobile app.
- Capacitor bootstrap (`@capacitor/core`, `@capacitor/cli`) with
  Android and iOS projects generated.
- Mobile-first base layout (viewport meta, safe-area handling, a
  single "Hello XPeak" screen).
- `packages/shared` workspace package with a barrel export scaffold.
- CI expansion: `pnpm test/lint/build` on the mobile app; `mix
  format --check-formatted / mix credo / mix compile
  --warnings-as-errors / mix test` on the api.
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
- Oban worker actually processing jobs (dep added later when needed).

## 3. Rationale

Bootstrap before feature work so every later phase can start with:
- a repo layout that already matches the product brief;
- test frameworks in place, so the TDD rule (write failing test first)
  is enforceable from task one;
- CI that catches regressions the moment domain code appears;
- a `docker compose up` that spins Postgres without each dev
  reinventing the setup.

Skipping this phase means every subsequent feature would need to
argue about tooling before writing the failing test — the exact
friction we want gone.

## 4. High-level structure after this phase

```
xpeak/
  apps/
    api/
      lib/
        xpeak/                    # domain contexts (empty for now)
          application.ex
          repo.ex
        xpeak_web/                # web layer
          controllers/
            health_controller.ex
          endpoint.ex
          router.ex
          telemetry.ex
        xpeak.ex
        xpeak_web.ex
      config/
        config.exs
        dev.exs
        test.exs
        prod.exs
        runtime.exs
      priv/
        repo/migrations/
      test/
        xpeak_web/
          controllers/
            health_controller_test.exs
        test_helper.exs
        support/
      mix.exs
      mix.lock
      VERSION
      .tool-versions              # asdf: erlang + elixir
      .formatter.exs
      .credo.exs
      .dialyzer_ignore.exs
      Dockerfile                  # dev image
      README.md
      .env.example
    mobile/
      app/                        # Next.js App Router
      components/
      lib/
      public/
      android/                    # Capacitor Android project
      ios/                        # Capacitor iOS project
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
  docker-compose.yml              # postgres (api optional)
  .github/workflows/ci.yml        # extended with api-test + mobile-test jobs
```

## 5. Local dev experience target

After this phase, a fresh clone should get to a running stack with:

```sh
# one-time
pnpm install
docker compose up -d postgres
asdf install                       # picks Erlang + Elixir from .tool-versions
(cd apps/api && mix deps.get && mix ecto.setup)

# every day
docker compose up -d
(cd apps/api && mix phx.server)      # http://localhost:4000
pnpm --filter @xpeak/mobile dev      # http://localhost:3001
```

And a smoke test path:

```sh
curl http://localhost:4000/health   # {"status":"ok",...}
open http://localhost:3001          # "Hello XPeak"
```

## 6. TDD stance for this phase

Even for scaffolding, tests go first where meaningful:

- Api: request test for the health endpoint written **before** the
  route/controller.
- Mobile: Vitest smoke test asserting the root page renders "Hello
  XPeak" written **before** the page file.
- Any non-trivial config (e.g., CORS allow-list) covered by a
  controller test.

Pure scaffolding steps (running `mix phx.new`, running
`create-next-app`) obviously don't have a test-first — but the moment
we add project-specific behavior on top, the failing-test-first rule
kicks in.

## 7. Dependencies & risks

| Risk / dep                                                       | Mitigation                                                              |
|------------------------------------------------------------------|-------------------------------------------------------------------------|
| Elixir requires matching Erlang/OTP; version drift dev↔CI        | `.tool-versions` at `apps/api/` is the single source of truth.          |
| Capacitor iOS project needs Xcode / macOS                        | Generate the folder; iOS build validated later on a Mac.                |
| CI Postgres service adds runtime                                 | Use GH Actions `services: postgres` step, cached `_build/` + `deps/`.   |
| release-please must bump the Phoenix version                     | Keep an `apps/api/VERSION` file; `mix.exs` reads it. Fan-out via `extra-files`. |
| Two dev servers on different ports                               | Document ports (4000 api / 3001 mobile) in each README.                 |
| BEAM `mix` slower than expected on cold start                    | Cache `_build/` and `deps/` aggressively in CI; use `mix compile --warnings-as-errors` only after cache hit. |

## 8. Decisions (locked) and remaining open questions

**Locked:**
- ✅ Backend stack: **Elixir + Phoenix 1.7** (API mode).
- ✅ Postgres via `docker-compose.yml`. Redis is not required at this
  stage.
- ✅ ID strategy: **binary UUIDs** (`--binary-id`).

**Still open (decide before we cut code):**
1. **Elixir/Erlang version** — target Elixir `1.17.x` on OTP `27`
   (latest stable) or hold at Elixir `1.16.x` on OTP `26`?
2. **Design system on mobile** — commit to Tailwind + shadcn/ui now,
   or ship a plain-CSS "Hello XPeak" this phase?
3. **PWA manifest** — include in this phase, or wait for the "Mobile
   polish" phase (Block I)?
4. **Docker compose scope** — Postgres only, or also containerize
   `api` (so `docker compose up` gives a full stack)?
5. **Shared package name** — `@xpeak/shared` or `@xpeak/types`?
6. **CORS allow-list** — dev-only `http://localhost:3001` for now,
   plus a placeholder for the eventual mobile app scheme
   (`capacitor://localhost`)?
7. **API base URL contract** — `NEXT_PUBLIC_API_URL` env var with
   default to `http://localhost:4000`?
8. **Ecto test sandbox** — default `Ecto.Adapters.SQL.Sandbox` in
   `:manual` mode with `async: true` per test module?

## 9. Interaction with Phase 1 (release-please)

- The `.release-please-manifest.json` stays single-package at root
  (decision from Phase 1 §6.A).
- Add `extra-files` entries to `release-please-config.json` pointing
  to `apps/api/VERSION` and to `apps/mobile/package.json`.
- `apps/api/mix.exs` reads the version from `File.read!("VERSION") |>
  String.trim()` so a single string edit updates both the Mix project
  version and any runtime read of `Application.spec(:xpeak, :vsn)`.

## 10. Success criteria

- `docker compose up -d postgres` brings Postgres up cleanly.
- `mix phx.server` in `apps/api` boots without errors.
- `curl localhost:4000/health` returns
  `{"status":"ok","version":"0.0.1","time":"..."}`.
- `pnpm --filter @xpeak/mobile dev` boots Next.js on port 3001.
- Browser at `http://localhost:3001` shows "Hello XPeak".
- `pnpm --filter @xpeak/mobile build` succeeds.
- `mix test` in `apps/api` passes with the health controller test
  green.
- `mix format --check-formatted` + `mix credo --strict` + `mix
  compile --warnings-as-errors` clean.
- `pnpm lint` at the root passes across all packages.
- CI (extended from Phase 1) runs all of the above on PRs and is
  green on this phase's own PR.
- A new `feat(api): ...` commit merged to `main` produces a
  release-please Release PR that bumps root `package.json`,
  `apps/api/VERSION`, and `apps/mobile/package.json` in one PR.

## 11. Next step

Answer the questions in §8, then `tasks.md` gets a final review and
we open the first working branch for Section A.
