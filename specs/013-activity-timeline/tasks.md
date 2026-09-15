# Phase 13 — Activity Timeline / Feed (TASKS)

## Legend
`[T]` TDD, `[S]` sequential, `[P]` parallel.

---

## Section A — Schema

- [ ] **T-013-01 `[T][S]`** — activity_events + notifications
      migrations/schemas
  - Indexes per `spec.md` §1.
  - Failing tests, migrations, schemas.
  - Green.

## Section B — Emitter

- [ ] **T-013-02 `[T][S]`** — `Xpeak.Feed.Emitter.emit/3`
  - Failing tests: default visibility, subject fields.
  - Implement.
  - Green.

- [ ] **T-013-03 `[T][S]`** — Retrofit CheckIns to emit `check_in.posted`
      and `level.up`
  - Failing tests: after check-in, exactly one `check_in.posted` +
    optional `level.up`.
  - Extend Multi.
  - Green.

- [ ] **T-013-04 `[T][S]`** — Retrofit MedalAwarder to emit `medal.earned`
  - Failing test: awarding medal produces the event.
  - Green.

## Section C — Feed queries

- [ ] **T-013-05 `[T][S]`** — `Feed.personal/2`
  - Failing test: own events, ordered, paginated.
  - Implement.
  - Green.

- [ ] **T-013-06 `[T][S]`** — `Feed.friends_feed/2`
  - Failing tests: includes friends' events; excludes blocked;
    respects visibility.
  - Implement (with friendships_view or inline CTE).
  - Green.

## Section D — Notifications

- [ ] **T-013-07 `[T][S]`** — Emit notifications for social events
  - Retrofit `Social.send_friend_request` + `accept` to insert
    notifications for the counterparty.
  - Failing tests.
  - Green.

- [ ] **T-013-08 `[T][S]`** — Notifications context
  - `list/2`, `mark_read/2`, `mark_all_read/1`.
  - Failing tests.
  - Green.

## Section E — HTTP surface

- [ ] **T-013-09 `[T][S]`** — `GET /feed`
  - Failing tests for both scopes.
  - Implement.
  - Green.

- [ ] **T-013-10 `[T][S]`** — Notifications endpoints
  - `GET /notifications`, `POST /notifications/:id/read`,
    `POST /notifications/read_all`.
  - Failing tests.
  - Green.

## Section F — Mobile

- [ ] **T-013-11 `[T][S]`** — Feed queries + tab scaffold
  - `lib/feed/queries.ts`.
  - Failing specs.
  - Green.

- [ ] **T-013-12 `[T][S]`** — Event card renderers
  - One component per kind.
  - Failing specs per kind.
  - Green.

- [ ] **T-013-13 `[T][S]`** — Notification bell + list
  - Failing spec: badge count reflects unread; tap opens list; tap
    item marks read + navigates.
  - Green.

## Section G — Wrap-up

- [ ] **T-013-14 `[P]`** — ADR `0013-activity-feed.md`.
- [ ] **T-013-15 `[S]`** — Phase close.

---

## Bundling strategy for PRs

1. `feat(feed): schema + emitter` — A + B(emitter)
2. `feat(feed): retrofit check-in and medal emitters` — B(retrofits)
3. `feat(feed): feed queries + endpoints` — C + E(feed)
4. `feat(feed): notifications context + endpoints` — D + E(notifications)
5. `feat(mobile): feed tab + event cards` — F1 + F2
6. `feat(mobile): notification bell + list` — F3
7. `docs(adr): activity feed` — G
