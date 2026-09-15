# Phase 21 — Metrics & Recap (TASKS)

## Legend
`[T]` TDD, `[S]` sequential, `[P]` parallel.

---

## Section A — Rollup

- [ ] **T-021-01 `[T][S]`** — metric_daily migration/schema
  - Green.

- [ ] **T-021-02 `[T][S]`** — `RollupJob` (nightly)
  - Failing tests on synthetic dataset.
  - Green.

- [ ] **T-021-03 `[T][S]`** — Opportunistic today-row update on
      check-in
  - Failing test.
  - Extend `create_check_in/2`.
  - Green.

## Section B — Metrics read API

- [ ] **T-021-04 `[T][S]`** — `Metrics.dashboard/2`
  - Failing tests per period.
  - Green.

- [ ] **T-021-05 `[T][S]`** — `Metrics.heatmap/2`
  - Failing tests.
  - Green.

- [ ] **T-021-06 `[T][S]`** — Endpoints
  - `GET /me/metrics`, `GET /me/metrics/heatmap`.
  - Failing tests.
  - Green.

## Section C — Recap

- [ ] **T-021-07 `[T][S]`** — recaps migration/schema
  - Green.

- [ ] **T-021-08 `[T][S]`** — `RecapGenerator.perform/1`
  - Failing tests: payload shape complete.
  - Idempotent.
  - Green.

- [ ] **T-021-09 `[T][S]`** — Trigger (cron or anniversary — per Q1)
  - Failing tests: enqueues jobs correctly.
  - Green.

- [ ] **T-021-10 `[T][S]`** — Endpoints
  - `GET /recaps`, `GET /recaps/:period`,
    `POST /recaps/:period/regenerate` (admin).
  - Failing tests: 200 vs. 202 behavior.
  - Green.

- [ ] **T-021-11 `[S]`** — Shareable card rendering
  - Per Q4 decision.
  - Failing test asserts the artifact URL becomes available.
  - Implement.

## Section D — Mobile

- [ ] **T-021-12 `[T][S]`** — Home snapshot section
  - Per Q5 (A or B).
  - Failing spec: renders totals + streak calendar mini.
  - Green.

- [ ] **T-021-13 `[T][S]`** — Full metrics screen
  - Failing specs per section.
  - Charts library per Q2.
  - Green.

- [ ] **T-021-14 `[T][S]`** — Recap story viewer
  - Failing spec: card navigation, auto-play, share.
  - Green.

## Section E — Wrap-up

- [ ] **T-021-15 `[P]`** — ADR `0021-metrics-and-recap.md`.
- [ ] **T-021-16 `[S]`** — Phase close.

---

## Bundling strategy for PRs

1. `feat(metrics): rollup schema + nightly job + today updater` — A
2. `feat(metrics): dashboard + heatmap endpoints` — B
3. `feat(metrics): recap generator + endpoints` — C
4. `feat(mobile): home snapshot + full metrics screen` — D1 + D2
5. `feat(mobile): recap story viewer` — D3
6. `docs(adr): metrics and recap` — E
