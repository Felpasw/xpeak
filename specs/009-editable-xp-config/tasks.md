# Phase 9 — Editable XP & Level Configuration (TASKS)

## Legend
`[T]` TDD, `[S]` sequential, `[P]` parallel, `[HUMAN]` human-only.

---

## Section A — Data model

- [ ] **T-009-01 `[T][S]`** — Migrations
  - `settings`, `streak_tiers`, `admin_events`, `users.role`.
  - Schemas + failing tests.
  - Green.

- [ ] **T-009-02 `[S]`** — Seeds
  - Seed initial settings + streak tiers (mirror Phase 7 defaults).

## Section B — Config service

- [ ] **T-009-03 `[T][S]`** — `Xpeak.Config` read API + ETS cache
  - Failing tests: `get_number/1`, `get_string/1`, `get_json/1`.
  - `Xpeak.Config.CacheWarmer` GenServer.
  - Add to supervision tree.
  - Green.

- [ ] **T-009-04 `[T][S]`** — `Xpeak.Config.put/3` + PubSub
  - Failing tests: write persists, publishes, cache updates.
  - Enforce min/max.
  - Write to audit log.
  - Green.

## Section C — Wire domain modules to Config

- [ ] **T-009-05 `[T][S]`** — `StreakTier` reads from DB
  - Failing test: change a `streak_tiers` row → `multiplier_for/1`
    reflects the change.
  - Refactor Phase 7 module.
  - Regression: all previous tests green.

- [ ] **T-009-06 `[T][S]`** — `MultiplierResolver.cap` from Config
  - Failing test: change `multiplier.cap` → next check-in caps at
    the new value.
  - Refactor.
  - Green.

- [ ] **T-009-07 `[T][S]`** — `LevelCurve` from Config
  - Failing test: change `level_curve.exponent` → new xp_for_level
    values.
  - Refactor.
  - Green.

## Section D — Admin surface

- [ ] **T-009-08 `[T][S]`** — `RequireAdmin` plug
  - Failing tests: 403 non-admin, 200 admin.
  - Implement.
  - Green.

- [ ] **T-009-09 `[S]`** — Mount admin at `/admin`
  - Kaffy vs. LiveView decided in `plan.md` §6 Q1.
  - Route + minimal navigation.

- [ ] **T-009-10 `[T][S]`** — Editable rows (settings, streak_tiers,
      categories)
  - Failing tests through the admin UI (LiveViewTest or plain
    controller tests for Kaffy).
  - Wire.
  - Green.

- [ ] **T-009-11 `[T][S]`** — Audit log read-only view
  - Failing test: change → new row in `admin_events` visible in the
    list.
  - Green.

## Section E — Bootstrap

- [ ] **T-009-12 `[HUMAN][S]`** — Grant first admin
  - Run `mix xpeak.grant_admin <email>` (or set env).
  - Verify login → `/admin` accessible.

## Section F — Wrap-up

- [ ] **T-009-13 `[P]`** — ADR `0009-editable-configuration.md`.
- [ ] **T-009-14 `[S]`** — Phase close.

---

## Bundling strategy for PRs

1. `feat(config): schemas, seeds, and read cache` — A + B(read)
2. `feat(config): writes + pubsub + audit log` — B(write)
3. `refactor(progression): read tiers/cap/curve from config` — C
4. `feat(admin): admin surface and role-based access` — D
5. `chore(admin): first admin bootstrap task` — E
6. `docs(adr): editable configuration` — F
