# Phase 19 — Group Bonuses & Group Streak (TASKS)

## Legend
`[T]` TDD, `[S]` sequential, `[P]` parallel.

---

## Section A — Schema

- [ ] **T-019-01 `[T][S]`** — check_ins GPS columns
  - Migration + schema update.
  - Green.

- [ ] **T-019-02 `[T][S]`** — group_workouts + group_workout_check_ins
  - Green.

- [ ] **T-019-03 `[T][S]`** — group_streaks
  - Green.

## Section B — Detector

- [ ] **T-019-04 `[T][S]`** — `GroupWorkoutDetector.maybe_attach/1`
  - Failing tests: attach + not attach cases.
  - Bounding box + Haversine.
  - Green.

- [ ] **T-019-05 `[T][S]`** — Wire into `CheckIns.create_check_in/2`
  - Failing test: check-in with GPS + eligible → group workout row.
  - Green.

## Section C — Multiplier

- [ ] **T-019-06 `[T][S]`** — Extend `MultiplierResolver` for group
  - Failing tests: 2/3+/5+ tiers, cap.
  - Green.

- [ ] **T-019-07 `[T][S]`** — Snapshot payload evolves
  - Failing test: `multiplier_snapshot` includes `group`.
  - Green.

## Section D — Group streak

- [ ] **T-019-08 `[T][S]`** — `GroupStreakUpdater` (Oban cron)
  - Failing tests: increment when threshold met, reset otherwise.
  - Green.

## Section E — HTTP surface

- [ ] **T-019-09 `[T][S]`** — Endpoints
  - `GET /challenges/:id/group_streak`, `GET /group_workouts/:id`.
  - Failing tests.
  - Green.

## Section F — Mobile

- [ ] **T-019-10 `[T][S]`** — GPS permission prompt
  - Failing spec.
  - Wire Capacitor Geolocation.
  - Green.

- [ ] **T-019-11 `[T][S]`** — Group workout badge on check-in card
  - Failing spec.
  - Green.

- [ ] **T-019-12 `[T][S]`** — Group streak indicator on challenge detail
  - Failing spec.
  - Green.

## Section G — Wrap-up

- [ ] **T-019-13 `[P]`** — ADR `0019-group-bonuses.md`.
- [ ] **T-019-14 `[S]`** — Phase close.

---

## Bundling strategy for PRs

1. `feat(social): gps + group workout schemas` — A
2. `feat(social): group workout detector wired into check-in` — B
3. `feat(social): group multiplier + snapshot` — C
4. `feat(social): group streak updater + endpoints` — D + E
5. `feat(mobile): gps prompt + group workout badge + streak indicator` — F
6. `docs(adr): group bonuses` — G
