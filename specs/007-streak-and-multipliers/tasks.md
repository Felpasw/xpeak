# Phase 7 — Streak & Category Multipliers (TASKS)

## Legend

- `[T]` TDD, `[S]` sequential, `[P]` parallel.

---

## Section A — Pure modules

- [ ] **T-007-01 `[T][P]`** — `StreakTier`
  - Failing tests: boundary cases for each tier.
  - Implement.
  - Green.

- [ ] **T-007-02 `[T][S]`** — `Multiplier` struct + `MultiplierResolver`
  - Failing tests: composition, cap.
  - Implement.
  - Green.

- [ ] **T-007-03 `[T][S]`** — `XpCalculator.compute/2`
  - Depends on T-007-02.
  - Failing tests: arithmetic, rounding.
  - Implement.
  - Green.

## Section B — Persistence

- [ ] **T-007-04 `[T][S]`** — `multiplier_snapshot` migration
  - Migration with backfill.
  - Test: existing rows get the backfilled shape.
  - Green.

- [ ] **T-007-05 `[T][S]`** — Update `create_check_in/2`
  - Update to compose and persist snapshot.
  - Update existing Phase 5 tests to expect the new field.
  - Add new test: streak 7 + legs → xp 26, snapshot correct.
  - Green.

## Section C — HTTP surface

- [ ] **T-007-06 `[T][S]`** — `POST /check_ins` response addition
  - Failing controller test: snapshot present.
  - Green.

- [ ] **T-007-07 `[T][S]`** — `GET /me.active_multiplier`
  - Failing test.
  - Compute from user's current streak; category/challenge null.
  - Green.

## Section D — Mobile

- [ ] **T-007-08 `[T][S]`** — Check-in success screen breakdown
  - Failing spec: renders `base × cat × streak` chip line.
  - Implement.
  - Green.

- [ ] **T-007-09 `[T][S]`** — Profile active-multiplier badge
  - Failing spec: shows current multiplier + next-tier hint.
  - Implement.
  - Green.

## Section E — Wrap-up

- [ ] **T-007-10 `[P]`** — ADR `0007-multipliers.md`
  - Cap, ordering (streak-before-multiplier), snapshot forward-compat.

- [ ] **T-007-11 `[S]`** — Phase close.

---

## Dependencies

```
A ──▶ B ──▶ C ──▶ D ──▶ E
```

## Bundling strategy for PRs

1. `feat(progression): streak tiers and multiplier resolver` — A
2. `feat(check-ins): persist multiplier snapshot` — B + C
3. `feat(mobile): show multiplier breakdown and active badge` — D
4. `docs(adr): multipliers` — E
