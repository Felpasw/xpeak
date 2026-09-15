# Phase 25 — Mobile Polish & PWA Fallback (TASKS)

## Legend
`[T]` TDD, `[S]` sequential, `[P]` parallel.

---

## Section A — Offline queue

- [ ] **T-025-01 `[T][S]`** — IndexedDB queue module
  - Failing specs: enqueue, list, delete, backoff.
  - Green.

- [ ] **T-025-02 `[T][S]`** — Worker syncing on online events
  - Failing spec: pending items uploaded on connectivity.
  - Green.

- [ ] **T-025-03 `[T][S]`** — Queue UI (badge + viewer)
  - Failing specs.
  - Green.

## Section B — Media compression

- [ ] **T-025-04 `[T][S]`** — Photo compression pipeline
  - Failing specs: output within size budget.
  - Green.

- [ ] **T-025-05 `[S]`** — Video compression (behind flag)
  - Per `plan.md` §6 Q1.
  - Green if enabled.

## Section C — PWA

- [ ] **T-025-06 `[S]`** — Manifest + icons
  - Green.

- [ ] **T-025-07 `[S]`** — Service worker (precache + runtime cache)
  - Green.

- [ ] **T-025-08 `[T][S]`** — Install prompt behavior
  - Failing spec: shown after N sessions.
  - Green.

## Section D — Performance

- [ ] **T-025-09 `[T][S]`** — Bundle analysis + code-split heavy screens
  - Baseline measurement, then split.
  - Bundle size assertions in CI.
  - Green.

- [ ] **T-025-10 `[T][S]`** — Lighthouse CI budget
  - Add job to `.github/workflows/ci.yml`.
  - Fail on regression.
  - Green.

## Section E — Error reporting

- [ ] **T-025-11 `[S]`** — Sentry on mobile
  - Wire SDK, source maps, PII scrub.

- [ ] **T-025-12 `[S]`** — Sentry on api
  - Wire SDK.

## Section F — Accessibility

- [ ] **T-025-13 `[T][S]`** — axe-core CI check on hero pages
  - Fail on AA violations.
  - Green.

- [ ] **T-025-14 `[S]`** — Focus management + alt text sweep
  - Manual pass, per-component fixes.

## Section G — Wrap-up

- [ ] **T-025-15 `[P]`** — ADR `0025-mobile-polish.md`.
- [ ] **T-025-16 `[S]`** — Phase close (v1.0 release milestone).

---

## Bundling strategy for PRs

1. `feat(mobile): offline check-in queue` — A
2. `feat(mobile): media compression pipeline` — B
3. `feat(mobile): pwa manifest + service worker + install prompt` — C
4. `perf(mobile): bundle split + lighthouse CI` — D
5. `feat(observability): sentry on mobile + api` — E
6. `feat(a11y): axe-core CI + focus + alt text` — F
7. `docs(adr): mobile polish` — G
