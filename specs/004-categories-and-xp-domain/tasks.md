# Phase 4 — Categories & Pure XP Domain (TASKS)

## Legend

- `[T]` TDD, `[S]` sequential, `[P]` parallel, `[HUMAN]` human-only.
- `[ ]` not started, `[~]` in progress, `[x] ✅ commit <hash>` done.

---

## Section A — Category schema & seeds

- [ ] **T-004-01 `[T][S]`** — `categories` migration + schema
  - Failing test: schema validations (slug uniqueness, weight bounds).
  - Migration per `spec.md` §2.1.
  - `lib/xpeak/progression/category.ex`.
  - Green.

- [ ] **T-004-02 `[S]`** — Seed system categories
  - `priv/repo/seeds/categories.exs` with the 10 categories per
    `spec.md` §2.2.
  - Register in `priv/repo/seeds.exs` runner.
  - Test that `Repo.all(Category)` returns 10 rows after seed.

## Section B — Pure domain services

- [ ] **T-004-03 `[T][P]`** — `XpCalculator`
  - Failing tests including property-based (`stream_data`).
  - Implement `Xpeak.Progression.XpCalculator`.
  - Green.

- [ ] **T-004-04 `[T][P]`** — `LevelCurve`
  - Failing tests: values, monotonicity.
  - Implement `Xpeak.Progression.LevelCurve`.
  - Green.

- [ ] **T-004-05 `[T][S]`** — `LevelUpService`
  - Depends on T-004-04.
  - Failing tests: `level_for_xp/1` round-trip, `level_up?/2`
    boundary cases.
  - Implement using binary search on the curve.
  - Green.

## Section C — Public context

- [ ] **T-004-06 `[T][S]`** — `Xpeak.Progression` context API
  - Failing tests: `list_categories/0` returns only active,
    `compute_xp/1` delegates correctly.
  - Implement.
  - Green.

## Section D — Wrap-up

- [ ] **T-004-07 `[P]`** — ADR `0004-xp-engine.md`
  - Curve choice, rationale, tunability path via Phase 9.

- [ ] **T-004-08 `[S]`** — Phase close
  - All green, ADR merged, roadmap flipped.

---

## Dependencies

```
A ──▶ B ──▶ C ──▶ D
```

## Bundling strategy for PRs (branches)

Suggested single branch per bundle. Branch prefix `XPK-N`
(sequential, TBD when we start).

1. `feat(progression): add categories schema and seeds` — A
2. `feat(progression): add xp calculator and level curve` — B
3. `feat(progression): add level up service and context api` — C
4. `docs(adr): xp engine` — D
