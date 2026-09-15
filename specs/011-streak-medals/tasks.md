# Phase 11 — Streak Medals (TASKS)

## Legend
`[T]` TDD, `[S]` sequential, `[P]` parallel.

---

## Section A — Schema + seeds

- [ ] **T-011-01 `[T][S]`** — medals + user_medals migration/schema
  - Failing tests, migrations, schemas.
  - Green.

- [ ] **T-011-02 `[S]`** — Seed 4 default medals + upload artwork
  - `priv/repo/seeds/medals.exs`.
  - Upload PNGs to media bucket (or bundled).

## Section B — Awarder

- [ ] **T-011-03 `[T][S]`** — `MedalAwarder.check_streak_medals/1`
  - Failing tests: crosses threshold, idempotent, multiple medals.
  - Implement.
  - Green.

- [ ] **T-011-04 `[T][S]`** — Wire into `create_check_in/2`
  - Extend the Multi from Phase 5.
  - Failing test: check-in at day 7 → medal awarded in same txn.
  - Green.

## Section C — HTTP surface

- [ ] **T-011-05 `[T][S]`** — `GET /me/medals`
  - Failing test.
  - Implement.
  - Green.

- [ ] **T-011-06 `[T][S]`** — `GET /users/:username/medals`
  - Failing test (no auth check yet — Phase 12 tightens it).
  - Green.

## Section D — Mobile

- [ ] **T-011-07 `[T][S]`** — Medals grid on profile
  - Failing spec: empty state + populated grid.
  - Implement.
  - Green.

- [ ] **T-011-08 `[T][S]`** — Newly-earned notification stack
  - Failing spec: after check-in awarding a medal, both level-up
    and medal cards show.
  - Implement.
  - Green.

## Section E — Wrap-up

- [ ] **T-011-09 `[P]`** — ADR `0011-streak-medals.md`.
- [ ] **T-011-10 `[S]`** — Phase close.

---

## Bundling strategy for PRs

1. `feat(achievements): medals schema and seeds` — A
2. `feat(achievements): streak medal awarder wired into check-in` — B
3. `feat(achievements): medals endpoints` — C
4. `feat(mobile): medals grid + earned notification` — D
5. `docs(adr): streak medals` — E
