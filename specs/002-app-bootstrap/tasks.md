# Phase 2 — Application Skeleton (TASKS)

> Companion to `spec.md` (what) and `plan.md` (why). Atomic execution
> steps. Nothing implemented yet.

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

- [ ] **T-002-01 `[S]`** — Root `packages/tsconfig-base.json`
  - Strict TS base (as in `spec.md` §5).
  - Deliverable: `packages/tsconfig-base.json`.

- [ ] **T-002-02 `[P]`** — Root Prettier + EditorConfig
  - `.prettierrc`, `.prettierignore`, `.editorconfig`.
  - Deliverable: three files at repo root.

- [ ] **T-002-03 `[S]`** — `docker-compose.yml` with Postgres
  - Service, volume, port per `spec.md` §6.
  - Manual test: `docker compose up -d postgres && docker compose ps`
    — healthy.
  - Deliverable: `docker-compose.yml` at repo root.

---

## Section B — `packages/shared` skeleton

- [ ] **T-002-04 `[S]`** — Scaffold `packages/shared`
  - `package.json` (name `@xpeak/shared`, private, workspace-only).
  - `tsconfig.json` extending base.
  - `src/index.ts` with an empty `export {}`.
  - Deliverable: three files under `packages/shared/`.

---

## Section C — Phoenix API bootstrap

- [ ] **T-002-05 `[S]`** — Answer prerequisites
  - Confirm Elixir + Erlang versions (`plan.md` §8 Q1).
  - Install locally via asdf.
  - `mix archive.install hex phx_new --force` on host.
  - Deliverable: version numbers pasted into this task before
    starting T-002-06.

- [ ] **T-002-06 `[S]`** — Bootstrap Phoenix app
  - Run the `mix phx.new` command from `spec.md` §2.1.
  - Add `apps/api/.tool-versions` with the chosen Erlang + Elixir
    versions.
  - `mix deps.get` runs clean.
  - Deliverable: `apps/api/` populated.

- [ ] **T-002-07 `[S]`** — Add dependencies and configure
  - Update `mix.exs` per `spec.md` §2.3 (add `corsica`, `ex_machina`,
    `mox`, `credo`, `dialyxir`).
  - `mix deps.get` clean.
  - Deliverable: updated `mix.exs`, `mix.lock`.

- [ ] **T-002-08 `[S]`** — Configure `mix.exs` to read VERSION
  - Create `apps/api/VERSION` with `0.0.1`.
  - Update `mix.exs` to read `@version File.read!("VERSION") |>
    String.trim()`.
  - `iex -S mix -e "IO.puts Application.spec(:xpeak, :vsn)"` prints
    `0.0.1`.
  - Deliverable: `VERSION` + updated `mix.exs`.

- [ ] **T-002-09 `[T][S]`** — Health controller test (failing)
  - **Write** `test/xpeak_web/controllers/health_controller_test.exs`
    covering `GET /health` → 200 with the JSON contract from
    `spec.md` §2.6.
  - `mix test` — expect red (route/controller missing).
  - Deliverable: red test in the working tree (do NOT stage until
    T-002-10 lands green).

- [ ] **T-002-10 `[S]`** — Implement health endpoint (make green)
  - Add route in `lib/xpeak_web/router.ex`.
  - Add `lib/xpeak_web/controllers/health_controller.ex`.
  - `mix test` — expect green.
  - Deliverable: router + controller + green suite.

- [ ] **T-002-11 `[T][S]`** — CORS test + config
  - Failing test: preflight `OPTIONS /health` from origin
    `http://localhost:3001` returns proper `access-control-allow-*`
    headers.
  - Failing test: preflight from `capacitor://localhost` also passes.
  - Add `Corsica` to `XpeakWeb.Endpoint` with the allow-list from
    `spec.md` §2.4.
  - `mix test` — green.
  - Deliverable: endpoint change + tests.

- [ ] **T-002-12 `[P]`** — Credo + formatter baseline
  - `mix credo.gen.config` in `apps/api/`.
  - Tune `.credo.exs` per `spec.md` §2.8.
  - `mix format` on the whole tree.
  - `mix format --check-formatted && mix credo --strict` clean.
  - Deliverable: `.credo.exs` + formatted tree.

- [ ] **T-002-13 `[P]`** — Dialyzer PLT bootstrap
  - `mix dialyzer --plt` on host (long first run).
  - Add `.dialyzer_ignore.exs` (empty).
  - Deliverable: PLT scripts baseline; not required in CI initially
    (added later once it's warm).

- [ ] **T-002-14 `[P]`** — Dev Dockerfile
  - `apps/api/Dockerfile` per `spec.md` §2.9.
  - Manual test: `docker build -t xpeak-api-dev apps/api` succeeds.
  - Deliverable: `Dockerfile`, `.dockerignore`.

- [ ] **T-002-15 `[P]`** — `apps/api/.env.example` + README
  - Env vars: `DATABASE_URL`, `SECRET_KEY_BASE`, `PORT`, `PHX_HOST`.
  - README per `spec.md` §2.10.
  - Deliverable: `.env.example`, `README.md`.

---

## Section D — Next.js + Capacitor bootstrap

- [ ] **T-002-16 `[S]`** — Answer prerequisites
  - Confirm design system decision (`plan.md` §8 Q2).
  - Confirm PWA scope (`plan.md` §8 Q3).
  - Deliverable: answers pasted into this task before T-002-17.

- [ ] **T-002-17 `[S]`** — Bootstrap Next.js app
  - Run the `pnpm create next-app` command from `spec.md` §3.1.
  - Rename package to `@xpeak/mobile` in `apps/mobile/package.json`.
  - Add `workspace:*` dep on `@xpeak/shared`.
  - Configure `tsconfig.json` to extend
    `../../packages/tsconfig-base.json` per `spec.md` §3.3.
  - `pnpm install` at repo root runs clean.
  - Deliverable: `apps/mobile/` populated.

- [ ] **T-002-18 `[T][S]`** — Vitest install + failing smoke spec
  - Install deps from `spec.md` §3.5.
  - Add `vitest.config.ts` + `vitest.setup.ts`.
  - **Write** `app/__tests__/page.test.tsx` expecting "Hello XPeak"
    in the DOM.
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
  - Env var: `NEXT_PUBLIC_API_URL=http://localhost:4000`.
  - README per `spec.md` §3.9 (dev, test, build, native).
  - Deliverable: `.env.example`, `README.md`.

- [ ] **T-002-22 `[P]`** — ESLint tuning + Prettier integration
  - Add `eslint-config-prettier` and register it in the Next lint
    config.
  - `pnpm --filter @xpeak/mobile lint` clean.
  - Deliverable: updated ESLint config.

---

## Section E — CI expansion

- [ ] **T-002-23 `[T][S]`** — `api-test` job
  - Failing state: current CI doesn't run Elixir tests (obvious).
  - Add the job per `spec.md` §7.1 (Postgres service, setup-beam,
    cache, format/credo/compile/ecto/test).
  - Push branch, watch CI go green.
  - Deliverable: updated `.github/workflows/ci.yml`.

- [ ] **T-002-24 `[T][S]`** — `mobile-test` job
  - Add job per `spec.md` §7.2 (lint, test, build).
  - Push branch, watch CI go green.
  - Deliverable: updated `.github/workflows/ci.yml`.

- [ ] **T-002-25 `[HUMAN]`** — Update branch protection
  - Add `api-test` and `mobile-test` to the list of required checks
    on `main`.
  - Deliverable: setting applied in the GitHub UI.

---

## Section F — release-please version wiring

- [ ] **T-002-26 `[S]`** — `extra-files` in `release-please-config.json`
  - Add entries for `apps/api/VERSION` and
    `apps/mobile/package.json` `$.version` per `spec.md` §8.
  - Deliverable: updated `release-please-config.json`.

- [ ] **T-002-27 `[T][S]`** — Dry-run bump propagation
  - Merge a trivial `feat(api): ...` PR into `main`.
  - Observe: release-please opens a Release PR bumping root
    `package.json`, `apps/api/VERSION`, and
    `apps/mobile/package.json` in a single Release PR.
  - Auto-merge fires (after CI green + approval).
  - Deliverable: link to the published release, screenshots of the
    three version bumps.

---

## Section G — End-to-end validation

- [ ] **T-002-28 `[T][S]`** — Local stack smoke test
  - `docker compose up -d postgres`
  - `(cd apps/api && mix deps.get && mix ecto.setup && mix
    phx.server)`
  - `curl -s localhost:4000/health | jq` matches the contract.
  - `pnpm --filter @xpeak/mobile dev` boots on port 3001.
  - Browser at `http://localhost:3001` shows "Hello XPeak".
  - Deliverable: screenshots pasted into
    `docs/adr/0002-app-skeleton.md`.

- [ ] **T-002-29 `[P]`** — ADR
  - `docs/adr/0002-app-skeleton.md` documenting bootstrap decisions:
    Phoenix / Elixir / Erlang versions, Next version, design system
    choice, Capacitor platform caveats on Linux, release-please
    version fan-out, Redis intentionally omitted at this stage.
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
- `apps/api/` full Phoenix skeleton (`mix.exs`, `mix.lock`, `lib/`,
  `config/`, `priv/`, `test/`, `VERSION`, `.tool-versions`,
  `.formatter.exs`, `.credo.exs`, `.dialyzer_ignore.exs`,
  `Dockerfile`, `README.md`, `.env.example`)
- `apps/mobile/` full Next.js 15 skeleton (`app/`,
  `capacitor.config.ts`, `android/`, `[ios/]`, `next.config.ts`,
  `vitest.config.ts`, `vitest.setup.ts`, `README.md`,
  `.env.example`)
- `.github/workflows/ci.yml` extended with `api-test` and
  `mobile-test`
- `release-please-config.json` extended with `extra-files`
- `docs/adr/0002-app-skeleton.md`

## Dependencies

```
A (root baseline)
  ↓
B (shared package)  ─── parallel with ───  C (Phoenix API)
                                              ↓
                                           D (Next.js + Capacitor)
                                              ↓
                                           E (CI expansion)
                                              ↓
                                           F (release-please wiring)
                                              ↓
                                           G (E2E validation + ADR)
```

## Bundling strategy for commits

Suggested PR bundles (each = one squash commit on `main`):

1. `chore(repo): add shared tsconfig, prettier, editorconfig` — A
2. `chore(repo): add docker-compose with postgres` — A
3. `chore(shared): scaffold @xpeak/shared package` — B
4. `feat(api): bootstrap phoenix api with health endpoint` — C
   (T-002-06 → T-002-15)
5. `feat(mobile): bootstrap next.js app with vitest and capacitor` — D
   (T-002-16 → T-002-22)
6. `ci: add api and mobile test jobs` — E
7. `ci(release): fan out version bumps to api and mobile` — F
8. `docs(adr): app skeleton decisions` — G (T-002-29)

Each bundle stops at `git add` and waits for explicit approval before
committing.
