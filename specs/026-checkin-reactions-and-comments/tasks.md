# Phase 26 — Check-in Reactions & Comments (TASKS)

## Legend

- `[T]` TDD, `[S]` sequential, `[P]` parallel, `[HUMAN]` human-only.

---

## Section A — Schema & entities

- [ ] **T-026-01 `[T][S]`** — `check_in_reactions` +
  `check_in_comments` migration
  - Failing tests: FK cascade, unique `(check_in_id, user_id, emoji)`,
    length check on `body`.
  - Implement `AddCheckInReactionsAndComments` migration + entity
    configs.
  - Green.

## Section B — Reactions

- [ ] **T-026-02 `[T][S]`** — `ICheckInReactionRepository`
  - Failing tests: `AddAsync` upserts, `RemoveAsync` deletes,
    `GetAggregateAsync` returns grouped counts.
  - Implement.
  - Green.

- [ ] **T-026-03 `[T][S]`** — Reaction palette validator
  - Failing test: unknown emoji → validation error.
  - Implement `EmojiPaletteValidator` + config-driven whitelist.
  - Green.

- [ ] **T-026-04 `[T][S]`** — `POST /check_ins/{id}/reactions`
  toggle endpoint
  - Failing tests: happy toggle on, toggle off, visibility 404,
    palette 422.
  - Implement handler + MediatR event emission.
  - Green.

- [ ] **T-026-05 `[T][S]`** — `GET /check_ins/{id}/reactions`
  aggregate endpoint
  - Failing tests: counts + `mine`; `?include=users` returns the
    capped user list.
  - Implement.
  - Green.

## Section C — Comments

- [ ] **T-026-06 `[T][S]`** — `ICheckInCommentRepository`
  - Failing tests: `AddAsync`, `SoftDeleteAsync`,
    `ListPageAsync(cursor, limit)`.
  - Implement.
  - Green.

- [ ] **T-026-07 `[T][S]`** — `POST /check_ins/{id}/comments`
  endpoint
  - Failing tests: happy, visibility 404, empty body 422, over-cap
    422.
  - Implement handler + MediatR event.
  - Green.

- [ ] **T-026-08 `[T][S]`** — `GET /check_ins/{id}/comments`
  cursor pagination
  - Failing tests: page 1, page 2 via cursor, excludes soft-deleted.
  - Implement.
  - Green.

- [ ] **T-026-09 `[T][S]`** — `DELETE /comments/{id}` soft-delete
  - Failing tests: author allowed, check-in owner allowed, third
    party 403.
  - Implement + MediatR event.
  - Green.

## Section D — Feed integration

- [ ] **T-026-10 `[T][S]`** — Enrich check-in feed list response
  with `reactions` + `comments_count`
  - Failing tests: batched read returns aggregate without N+1.
  - Implement via a single grouped query (`GROUP BY check_in_id`).
  - Green.

## Section E — Cross-phase wiring

- [ ] **T-026-11 `[T][S]`** — Phase 13 handler for reaction /
  comment events → activity feed row
  - Failing tests: writing a reaction produces the expected
    `activity_events` row.
  - Implement `INotificationHandler<CheckInReactionAdded>` +
    `CheckInCommentPosted`.
  - Green.

- [ ] **T-026-12 `[S]`** — Notes for Phase 24: coalescing rules for
  reaction / comment push scheduling
  - Document in `docs/adr/00XX-reaction-comment-notifications.md`.
  - No code — Phase 24 owns delivery.

## Section F — Mobile

- [ ] **T-026-13 `[T][S]`** — `ReactionsStrip` organism
  - Failing spec: renders 5 chips with counts, tap toggles with
    optimistic update, rollback on failure.
  - Implement.
  - Green.

- [ ] **T-026-14 `[T][S]`** — `CommentsSheet` organism
  - Failing spec: opens on icon tap, renders paginated comments,
    optimistic append on send, delete affordance for author /
    owner.
  - Implement bottom sheet + list virtualization.
  - Green.

- [ ] **T-026-15 `[T][S]`** — Wire strip + sheet into check-in
  detail screen and feed item
  - Failing spec: feed item shows aggregate without extra request.
  - Implement.
  - Green.

## Section G — Wrap-up

- [ ] **T-026-16 `[P]`** — ADR `0XXX-reactions-and-comments.md`
  - Palette choice, aggregate-on-read, soft vs hard delete,
    MediatR event names.

- [ ] **T-026-17 `[S]`** — Phase close.

---

## Dependencies

```
A ──▶ B ──▶ C ──▶ D
                 ↓
                 E ──▶ F ──▶ G
```

## Bundling strategy for PRs

1. `feat(social): check-in reactions and comments schema` — A
2. `feat(social): reactions endpoints and palette validator` — B
3. `feat(social): comments endpoints with soft-delete` — C
4. `feat(feed): batch reactions and comment counts into check-in list` — D
5. `feat(feed): wire reaction and comment events into activity timeline` — E
6. `feat(mobile): reactions strip + comments sheet` — F
7. `docs(adr): reactions and comments` — G
