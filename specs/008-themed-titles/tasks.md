# Phase 8 — Themed Titles & Themes (TASKS)

## Legend
`[T]` TDD, `[S]` sequential, `[P]` parallel.

---

## Section A — Schema

- [ ] **T-008-01 `[T][S]`** — themes + tiers migrations & schemas
  - Failing tests: schema validations, overlapping-range guard.
  - Migrations + schemas.
  - Green.

- [ ] **T-008-02 `[T][S]`** — users.theme_id migration
  - Backfill: set to default theme id for existing users.
  - Green.

## Section B — Seeds

- [ ] **T-008-03 `[S]`** — Seed Medieval + Sci-Fi themes
  - `priv/repo/seeds/themes.exs`.
  - Test: seeded rows present.

## Section C — Context

- [ ] **T-008-04 `[T][S]`** — `Xpeak.Titles.resolve/2`
  - Failing tests per `spec.md` §5.
  - Implement.
  - Green.

## Section D — HTTP surface

- [ ] **T-008-05 `[T][S]`** — `GET /themes`
  - Failing controller test.
  - Implement + JSON view.
  - Green.

- [ ] **T-008-06 `[T][S]`** — `PATCH /me/theme`
  - Failing tests: happy, 404 unknown, 422 inactive.
  - Implement.
  - Green.

- [ ] **T-008-07 `[T][S]`** — `GET /me` addition
  - Extend tests to assert `current_title`, `next_title`,
    `progress_to_next`.
  - Update JSON view.
  - Green.

## Section E — Mobile

- [ ] **T-008-08 `[T][S]`** — Profile card with title
  - Failing spec: renders `@username · <title> · Level N`.
  - Implement.
  - Green.

- [ ] **T-008-09 `[T][S]`** — Theme picker settings screen
  - Failing spec: list + selection + persist.
  - Implement.
  - Green.

## Section F — Wrap-up

- [ ] **T-008-10 `[P]`** — ADR `0008-themed-titles.md`.
- [ ] **T-008-11 `[S]`** — Phase close.

---

## Bundling strategy for PRs

1. `feat(titles): themes and level tiers schemas + seeds` — A + B
2. `feat(titles): context + endpoints` — C + D
3. `feat(mobile): title on profile + theme picker` — E
4. `docs(adr): themed titles` — F
