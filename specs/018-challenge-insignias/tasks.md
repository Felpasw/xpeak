# Phase 18 — Insignias (TASKS)

## Legend
`[T]` TDD, `[S]` sequential, `[P]` parallel.

---

## Section A — Schema + seeds

- [ ] **T-018-01 `[T][S]`** — insignias + user_insignias migrations
  - Green.

- [ ] **T-018-02 `[S]`** — Seed 5 default insignias + upload artwork
  - Green.

## Section B — Awarder

- [ ] **T-018-03 `[T][S]`** — Ranking function per win kind
  - Failing tests per kind.
  - Green.

- [ ] **T-018-04 `[T][S]`** — `InsigniaAwarder.on_challenge_finished/1`
  - Failing tests: podium tiers, participant threshold, idempotent.
  - Green.

- [ ] **T-018-05 `[T][S]`** — Wire into challenge LifecycleJob
  - Failing test: state transition triggers awarder.
  - Emit `insignia.earned` events + notifications.
  - Green.

## Section C — HTTP surface

- [ ] **T-018-06 `[T][S]`** — Insignias endpoints
  - `GET /me/insignias`, `GET /users/:username/insignias`.
  - Failing tests: visibility respected.
  - Green.

## Section D — Mobile

- [ ] **T-018-07 `[T][S]`** — Achievements section (medals + insignias
      combined)
  - Failing spec.
  - Green.

- [ ] **T-018-08 `[T][S]`** — Insignia detail modal
  - Failing spec.
  - Green.

## Section E — Wrap-up

- [ ] **T-018-09 `[P]`** — ADR `0018-insignias.md`.
- [ ] **T-018-10 `[S]`** — Phase close.

---

## Bundling strategy for PRs

1. `feat(achievements): insignias schema + seeds` — A
2. `feat(achievements): insignia awarder + lifecycle wiring` — B
3. `feat(achievements): insignias endpoints` — C
4. `feat(mobile): achievements section with insignias` — D
5. `docs(adr): insignias` — E
