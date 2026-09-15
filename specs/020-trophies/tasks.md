# Phase 20 — Trophies (TASKS)

## Legend
`[T]` TDD, `[S]` sequential, `[P]` parallel.

---

## Section A — Schema + seeds

- [ ] **T-020-01 `[T][S]`** — trophies + user_trophies migrations
  - Green.

- [ ] **T-020-02 `[T][S]`** — challenges.custom_trophy_id migration
  - Green.

- [ ] **T-020-03 `[S]`** — Seed system trophies + upload artwork
  - Green.

## Section B — Awarder

- [ ] **T-020-04 `[T][S]`** — Awarder rules per challenge shape
  - Failing tests: solo hardcore, group #1, idempotent.
  - Green.

- [ ] **T-020-05 `[T][S]`** — Wire into LifecycleJob
  - Failing test: transition triggers awarder + emits event.
  - Green.

## Section C — Upload flow

- [ ] **T-020-06 `[T][S]`** — Presign endpoint for trophy upload
  - Failing tests: mime whitelist, size cap.
  - Green.

- [ ] **T-020-07 `[T][S]`** — Create custom trophy endpoint
  - Failing tests: only challenge owner, storage_key must exist.
  - Green.

## Section D — Read endpoints

- [ ] **T-020-08 `[T][S]`** — `GET /trophies?scope=system`
  - Failing test.
  - Green.

- [ ] **T-020-09 `[T][S]`** — `GET /me/trophies`,
      `GET /users/:username/trophies`
  - Failing tests: visibility respected.
  - Green.

## Section E — Mobile

- [ ] **T-020-10 `[T][S]`** — Challenge create wizard trophy step
  - Failing spec: three options + preview + upload flow.
  - Green.

- [ ] **T-020-11 `[T][S]`** — Trophy case on profile
  - Failing spec: renders animated art, tap opens modal.
  - Green.

## Section F — Wrap-up

- [ ] **T-020-12 `[P]`** — ADR `0020-trophies.md`.
- [ ] **T-020-13 `[S]`** — Phase close.

---

## Bundling strategy for PRs

1. `feat(achievements): trophies schema + seeds + custom_trophy_id` — A
2. `feat(achievements): trophy awarder wired into lifecycle` — B
3. `feat(achievements): trophy upload + endpoints` — C + D
4. `feat(mobile): trophy step in wizard + trophy case` — E
5. `docs(adr): trophies` — F
