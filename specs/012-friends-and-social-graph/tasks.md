# Phase 12 — Friends & Social Graph (TASKS)

## Legend
`[T]` TDD, `[S]` sequential, `[P]` parallel.

---

## Section A — Schema

- [ ] **T-012-01 `[T][S]`** — friendships + blocks migration/schema
  - Canonical ordering constraint.
  - Failing tests, migration, schema.
  - Green.

- [ ] **T-012-02 `[T][S]`** — users.privacy JSONB column
  - Default per `spec.md` §1.3.
  - Green.

## Section B — Context

- [ ] **T-012-03 `[T][S]`** — `Xpeak.Social` core
  - Failing tests for request lifecycle (send/accept/reject/cancel/remove).
  - Implement.
  - Green.

- [ ] **T-012-04 `[T][S]`** — Blocking + cascade delete
  - Failing tests: block deletes friendship, prevents new request.
  - Implement.
  - Green.

- [ ] **T-012-05 `[T][P]`** — `Social.Visibility` pure module
  - Failing tests per field × relationship matrix.
  - Implement.
  - Green.

## Section C — HTTP surface

- [ ] **T-012-06 `[T][S]`** — Search + friend requests endpoints
  - Failing tests for `GET /users/search`, `POST /friend_requests`,
    accept/reject/cancel/list.
  - Implement.
  - Green.

- [ ] **T-012-07 `[T][S]`** — Friends + blocks endpoints
  - `GET /friends`, `DELETE /friends/:id`, `POST/DELETE/GET /blocks`.
  - Failing tests, implement.
  - Green.

- [ ] **T-012-08 `[T][S]`** — Public profile `GET /users/:username`
  - Failing tests: visibility respected, blocked → 404.
  - Implement using `Visibility`.
  - Green.

- [ ] **T-012-09 `[T][S]`** — Privacy settings endpoints
  - `GET/PATCH /me/privacy`.
  - Failing tests.
  - Green.

## Section D — Retrofits

- [ ] **T-012-10 `[T][S]`** — Retrofit medals endpoint from Phase 11
      with visibility
  - Failing test: non-friend request → 404 if privacy hidden.
  - Update `GET /users/:username/medals`.
  - Green.

## Section E — Mobile

- [ ] **T-012-11 `[T][S]`** — Friends client + queries
  - `lib/social/queries.ts` (all mutations + lists).
  - Vitest with MSW.
  - Green.

- [ ] **T-012-12 `[T][S]`** — Friends tab (segmented control)
  - Failing spec: all three tabs render.
  - Implement.
  - Green.

- [ ] **T-012-13 `[T][S]`** — Search screen with debounce
  - Failing spec: types `@`, sees results, taps to view.
  - Implement.
  - Green.

- [ ] **T-012-14 `[T][S]`** — Public profile viewer
  - Failing spec: renders public fields; sticky action bar changes
    with `friendship_state`.
  - Implement.
  - Green.

- [ ] **T-012-15 `[T][S]`** — Privacy settings screen
  - Failing spec: toggles persist via PATCH.
  - Implement.
  - Green.

## Section F — Wrap-up

- [ ] **T-012-16 `[P]`** — ADR `0012-friends-and-social-graph.md`.
- [ ] **T-012-17 `[S]`** — Phase close.

---

## Bundling strategy for PRs

1. `feat(social): friendships + blocks schema` — A
2. `feat(social): context (requests, friends, blocks, visibility)` — B
3. `feat(social): search + requests endpoints` — C1
4. `feat(social): friends, blocks, privacy endpoints` — C2 + D
5. `feat(mobile): friends tab + search + public profile` — E1
6. `feat(mobile): privacy settings screen` — E2
7. `docs(adr): friends and social graph` — F
