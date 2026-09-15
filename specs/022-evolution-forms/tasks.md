# Phase 22 — Evolution Forms (TASKS)

## Legend
`[T]` TDD, `[S]` sequential, `[P]` parallel.

---

## Section A — Schema + seeds

- [ ] **T-022-01 `[T][S]`** — theme_forms migration/schema
  - Green.

- [ ] **T-022-02 `[S]`** — Seed 10 forms + upload artwork
  - Green.

## Section B — Resolver

- [ ] **T-022-03 `[T][S]`** — `Titles.resolve_form/1`
  - Failing tests: happy, fallback, missing everything → placeholder.
  - Green.

## Section C — Endpoints

- [ ] **T-022-04 `[T][S]`** — `GET /me` additions
  - Failing tests.
  - Extend JSON view.
  - Green.

- [ ] **T-022-05 `[T][S]`** — `GET /themes/:slug/forms`
  - Failing tests: locked/unlocked flags per caller level.
  - Green.

## Section D — Mobile

- [ ] **T-022-06 `[T][S]`** — Profile form illustration
  - Failing spec: renders current form (animated when applicable).
  - Green.

- [ ] **T-022-07 `[T][S]`** — Level-up transition animation
  - Failing spec: after level-up, form cross-fades.
  - Green.

- [ ] **T-022-08 `[T][S]`** — Forms gallery screen
  - Failing spec: grid renders per theme; locked forms blurred.
  - Green.

## Section E — Wrap-up

- [ ] **T-022-09 `[P]`** — ADR `0022-evolution-forms.md`.
- [ ] **T-022-10 `[S]`** — Phase close.

---

## Bundling strategy for PRs

1. `feat(titles): forms schema + seeds + resolver` — A + B
2. `feat(titles): forms endpoints` — C
3. `feat(mobile): profile form + level-up animation + gallery` — D
4. `docs(adr): evolution forms` — E
