# Phase 15 — Solo Challenges (TASKS)

## Legend
`[T]` TDD, `[S]` sequential, `[P]` parallel.

---

## Section A — Schema

- [ ] **T-015-01 `[T][S]`** — challenges + categories + memberships
      migrations/schemas
  - Failing tests: constraints, indexes, mode/visibility whitelists.
  - Green.

## Section B — Domain

- [ ] **T-015-02 `[T][S]`** — `Challenges.create_challenge/2`
  - Failing tests: creates all rows atomically (Multi).
  - Green.

- [ ] **T-015-03 `[T][P]`** — `ProgressCalculator` — total_xp
  - Failing tests.
  - Green.

- [ ] **T-015-04 `[T][P]`** — `ProgressCalculator` — total_check_ins
  - Failing tests.
  - Green.

- [ ] **T-015-05 `[T][P]`** — `ProgressCalculator` — consecutive_days
  - Failing tests.
  - Green.

- [ ] **T-015-06 `[T][S]`** — `MultiplierResolver` challenge bonus
  - Depends on Phase 7 module.
  - Failing test: check-in in whitelisted category during active
    membership → bonus applied.
  - Failing test: non-whitelisted category → no bonus.
  - Multiple memberships → max applied.
  - Implement.
  - Green.

## Section C — Lifecycle

- [ ] **T-015-07 `[T][S]`** — `LifecycleJob` (Oban)
  - Failing tests: upcoming→active, active→completed on target hit,
    active→failed on deadline.
  - Emits `challenge.finished` event (uses Phase 13 emitter).
  - Green.

## Section D — HTTP surface

- [ ] **T-015-08 `[T][S]`** — CRUD endpoints
  - Failing tests: create, list, detail, patch (upcoming only),
    cancel.
  - Green.

- [ ] **T-015-09 `[T][S]`** — `GET /challenges/:id/progress`
  - Failing tests.
  - Green.

## Section E — Mobile

- [ ] **T-015-10 `[T][S]`** — Challenges client + tab scaffold
  - `lib/challenges/queries.ts`.
  - Failing spec: segmented tabs.
  - Green.

- [ ] **T-015-11 `[T][S]`** — Create wizard
  - Failing spec: happy path creates a challenge with all steps.
  - Green.

- [ ] **T-015-12 `[T][S]`** — Detail screen with progress
  - Failing spec: renders progress bar + recent check-ins.
  - Green.

## Section F — Wrap-up

- [ ] **T-015-13 `[P]`** — ADR `0015-solo-challenges.md`.
- [ ] **T-015-14 `[S]`** — Phase close.

---

## Bundling strategy for PRs

1. `feat(challenges): schema (challenges + categories + memberships)` — A
2. `feat(challenges): create + progress calculators` — B(1-5)
3. `feat(challenges): multiplier resolver integration` — B6
4. `feat(challenges): lifecycle job + endpoints` — C + D
5. `feat(mobile): challenges tab + create wizard + detail` — E
6. `docs(adr): solo challenges` — F
