# Phase 14 — Global Rankings & Discovery (TASKS)

## Legend
`[T]` TDD, `[S]` sequential, `[P]` parallel.

---

## Section A — Schema

- [ ] **T-014-01 `[T][S]`** — rankings_snapshots + users.show_in_global_rankings
  - Failing tests, migration, schema.
  - Green.

## Section B — Context + snapshot job

- [ ] **T-014-02 `[T][S]`** — `Xpeak.Rankings` read API
  - Failing tests: list per scope, my_rank, friends-scoped.
  - Implement (query from snapshots for time-bounded, direct for
    all-time).
  - Green.

- [ ] **T-014-03 `[T][S]`** — `SnapshotJob` (Oban)
  - Failing test: perform inserts/updates snapshot rows correctly;
    ranks dense.
  - Implement.
  - Green.

- [ ] **T-014-04 `[T][S]`** — Schedule (Oban Cron)
  - Config Oban's `Oban.Plugins.Cron` with hourly for current +
    weekly/monthly freeze.
  - Failing test: cron config parsed as expected.
  - Green.

- [ ] **T-014-05 `[T][S]`** — Cache layer (Cachex)
  - Failing test: cache hit avoids DB call; invalidation after job.
  - Implement.
  - Green.

## Section C — Privacy

- [ ] **T-014-06 `[T][S]`** — Opt-out honored
  - Failing tests: opted-out user excluded from list and my_rank.
  - Update queries + snapshot job.
  - Green.

## Section D — HTTP surface

- [ ] **T-014-07 `[T][S]`** — `GET /rankings/scopes`
  - Failing test.
  - Green.

- [ ] **T-014-08 `[T][S]`** — `GET /rankings/:scope`
  - Failing tests: pagination + friends-scoped.
  - Green.

- [ ] **T-014-09 `[T][S]`** — `GET /rankings/:scope/me`
  - Failing tests.
  - Green.

## Section E — Mobile

- [ ] **T-014-10 `[T][S]`** — Rankings client + tab scaffold
  - `lib/rankings/queries.ts`.
  - Failing spec: renders scope + period selectors.
  - Green.

- [ ] **T-014-11 `[T][S]`** — Rankings list rendering
  - Failing spec: rank rows with all fields.
  - Green.

- [ ] **T-014-12 `[T][S]`** — Sticky "my rank" header
  - Failing spec: renders when logged-in user is ranked; hidden
    otherwise.
  - Green.

- [ ] **T-014-13 `[T][S]`** — Row → public profile with "Add friend"
      inline
  - Failing spec: tapping non-friend row shows friend-request CTA.
  - Green.

## Section F — Wrap-up

- [ ] **T-014-14 `[P]`** — ADR `0014-global-rankings.md`.
- [ ] **T-014-15 `[S]`** — Phase close.

---

## Bundling strategy for PRs

1. `feat(rankings): schema + opt-out field` — A + C
2. `feat(rankings): context + snapshot job + cache` — B
3. `feat(rankings): endpoints` — D
4. `feat(mobile): rankings tab and list` — E
5. `docs(adr): global rankings` — F
