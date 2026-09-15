# Phase 5 — Check-in (No Media) (TASKS)

## Legend

- `[T]` TDD, `[S]` sequential, `[P]` parallel, `[HUMAN]` human-only.

---

## Section A — Schema + user progression fields

- [ ] **T-005-01 `[T][S]`** — `check_ins` migration + schema
  - Failing schema tests (FK integrity, `xp_earned` positive,
    `notes` max length).
  - Migration + `lib/xpeak/check_ins/check_in.ex`.
  - Green.

- [ ] **T-005-02 `[T][S]`** — Users: `time_zone` + `last_check_in_on`
  - Migration adding both fields.
  - Update `User` schema + relevant tests.
  - Green.

## Section B — Domain logic

- [ ] **T-005-03 `[T][P]`** — `StreakCalculator` (pure)
  - Failing tests: first check-in, same-day, consecutive, gap,
    longest update.
  - Implement.
  - Green.

- [ ] **T-005-04 `[T][S]`** — `Xpeak.CheckIns.create_check_in/2`
  - Depends on T-005-01/02/03 + Phase 4 progression.
  - Failing test: happy path bumps xp/level/streak in a single txn.
  - Failing test: unknown category → error tuple, transaction rolls
    back.
  - Implement with `Ecto.Multi`.
  - Green.

- [ ] **T-005-05 `[T][S]`** — `Xpeak.CheckIns.list_check_ins/2`
  - Failing test: pagination cursor round-trip.
  - Implement.
  - Green.

## Section C — HTTP surface

- [ ] **T-005-06 `[T][S]`** — `POST /check_ins`
  - Failing controller test: 201 happy, 401 no token, 422 unknown
    slug, 422 notes too long.
  - Controller + JSON view.
  - Green.

- [ ] **T-005-07 `[T][S]`** — `GET /check_ins`
  - Failing test: returns only current user's rows, ordered desc,
    respects `limit` and `cursor`.
  - Controller.
  - Green.

## Section D — Mobile

- [ ] **T-005-08 `[T][S]`** — Check-ins client
  - `lib/checkins/queries.ts` (`useCreateCheckIn`, `useCheckIns`).
  - Vitest with MSW.
  - Green.

- [ ] **T-005-09 `[T][S]`** — Check-in screen
  - Failing spec: renders category grid, disables submit without
    selection.
  - Implement `app/(app)/check-in/page.tsx`.
  - Green.

- [ ] **T-005-10 `[T][S]`** — Level-up overlay
  - Failing spec: overlay renders when response has `leveled_up:
    true`.
  - Implement + wire into check-in success flow.
  - Green.

- [ ] **T-005-11 `[T][P]`** — Profile refresh
  - Spec: after check-in mutation, `useMe` refetches; profile shows
    new xp/level/streak.
  - Wire cache invalidation.
  - Green.

## Section E — Wrap-up

- [ ] **T-005-12 `[P]`** — ADR `0005-check-in-and-streak.md`
  - Streak semantics (UTC-only for now), transaction boundary.

- [ ] **T-005-13 `[S]`** — Phase close.

---

## Dependencies

```
A ──▶ B ──▶ C ──▶ D ──▶ E
```

## Bundling strategy for PRs

1. `feat(check-ins): add schema and user progression fields` — A
2. `feat(check-ins): domain logic and streak calculator` — B
3. `feat(check-ins): rest endpoints` — C
4. `feat(mobile): check-in screen and level-up overlay` — D
5. `docs(adr): check-in and streak` — E
