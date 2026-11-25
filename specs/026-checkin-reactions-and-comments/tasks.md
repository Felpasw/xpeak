# Phase 26 — Check-in Reactions & Comments (TASKS)

## Legend

- `[T]` TDD, `[S]` sequential, `[P]` parallel, `[HUMAN]` human-only,
  `[DEFERRED]` out of this phase's shipping bundle.

---

## Section A — Schema & entities

- [ ] **T-026-01 `[T][S]`** — `check_in_reactions` +
  `check_in_comments` migration + entities
  - Failing tests (`apps/api.Tests/Social/SocialSchemaTests.cs`):
    FK cascade on `check_ins`, unique `(check_in_id, user_id, emoji)`,
    length check on `body` (≤ 280), `deleted_at` default null.
  - Implement `AddCheckInReactionsAndComments` migration + entity
    configs for `CheckInReaction` and `CheckInComment`.
  - Green.

## Section B — Visibility seam

- [ ] **T-026-02 `[T][S]`** — `ICheckInVisibility` seam +
  `OpenCheckInVisibility` MVP implementation
  - Failing tests
    (`apps/api.Tests/Social/CheckInVisibilityTests.cs`):
    returns true for any authenticated caller on an existing
    check-in; returns false when the check-in does not exist.
  - Implement `ICheckInVisibility` in `Social/Services/`,
    `OpenCheckInVisibility` wired via DI. Reaction / comment
    endpoints call this once per request; a false response turns
    into 404 at the endpoint layer.
  - Note for Phase 12: the friend-graph implementation replaces
    `OpenCheckInVisibility` in DI with zero endpoint edits.
  - Green.

## Section C — Reactions

- [ ] **T-026-03 `[T][S]`** — `ICheckInReactionRepository`
  - Failing tests
    (`apps/api.Tests/Social/CheckInReactionRepositoryTests.cs`):
    `AddAsync` persists, `RemoveAsync` deletes the triple,
    `GetAggregateAsync(checkInId)` returns grouped counts,
    `GetMineAsync(checkInId, userId)` returns the caller's emojis.
  - Implement.
  - Green.

- [ ] **T-026-04 `[T][S]`** — Reaction palette validator (12 emojis)
  - Failing test: unknown emoji → validation error with
    `invalid_emoji` code. Whitelist lives in
    `Social/Palette/ReactionPalette.cs` as a static readonly set
    (`👍 💪 🔥 🎯 🎉 👏 💯 🚀 ⚡ 🏆 😎 🫡`).
  - Implement `EmojiPaletteValidator` wired through FluentValidation.
  - Green.

- [ ] **T-026-05 `[T][S]`** — `POST /check_ins/{id}/reactions`
  toggle endpoint
  - Failing tests
    (`apps/api.Tests/Social/CheckInReactionsEndpointTests.cs`):
    first POST adds the row, second POST with same emoji removes it,
    same user can hold multiple different emojis; visibility seam
    returning false → 404; palette failure → 422; response matches
    `{ counts, mine }`.
  - Implement handler + `CheckInReactionAdded` /
    `CheckInReactionRemoved` MediatR events.
  - Green.

- [ ] **T-026-06 `[T][S]`** — `GET /check_ins/{id}/reactions`
  aggregate endpoint
  - Failing tests: returns `{ counts, mine }`; `?include=users`
    returns the capped user list (20 per emoji, overflow dropped).
  - Implement.
  - Green.

## Section D — Comments

- [ ] **T-026-07 `[T][S]`** — `ICheckInCommentRepository`
  - Failing tests
    (`apps/api.Tests/Social/CheckInCommentRepositoryTests.cs`):
    `AddAsync` persists, `SoftDeleteAsync(id)` stamps `deleted_at`,
    `ListPageAsync(checkInId, cursor, limit)` paginates oldest-first,
    excludes soft-deleted rows, `CountVisibleAsync(checkInId)` for
    the feed badge.
  - Implement.
  - Green.

- [ ] **T-026-08 `[T][S]`** — `POST /check_ins/{id}/comments`
  endpoint
  - Failing tests
    (`apps/api.Tests/Social/CheckInCommentsEndpointTests.cs`):
    201 with persisted body; visibility seam false → 404; empty
    body → 422; over-cap (281 chars) → 422.
  - Implement handler + `CheckInCommentPosted` MediatR event.
  - Green.

- [ ] **T-026-09 `[T][S]`** — `GET /check_ins/{id}/comments`
  cursor pagination
  - Failing tests: page 1 returns the oldest `limit` rows + the
    `next_cursor`; page 2 continues; excludes soft-deleted rows.
  - Implement.
  - Green.

- [ ] **T-026-10 `[T][S]`** — `DELETE /comments/{id}` soft-delete
  - Failing tests: comment author allowed → 204; check-in owner
    allowed → 204; third party → 403; already-deleted → 404.
  - Implement handler + `CheckInCommentDeleted` MediatR event.
  - Green.

## Section E — Detail-route API

- [ ] **T-026-11 `[T][S]`** — `GET /check_ins/{id}` individual
  endpoint powering the mobile detail route
  - Failing tests
    (`apps/api.Tests/CheckIns/GetCheckInEndpointTests.cs`):
    returns the check-in with media preview + author snapshot
    (username, avatar) + `reactions` + `commentsCount` inline;
    visibility seam false → 404.
  - Implement a single-row projection + author join.
  - Green.

## Section F — Feed enrichment

- [ ] **T-026-12 `[T][S]`** — Enrich `GET /check_ins` list response
  with `reactions` + `commentsCount` per row
  - Failing test
    (`apps/api.Tests/CheckIns/ListCheckInsEndpointTests.cs`):
    list payload carries the aggregate without N+1 (single grouped
    query per aggregate).
  - Implement via `GROUP BY check_in_id` reads merged into the
    repository.
  - Green.

## Section G — Cross-phase wiring

- [ ] **T-026-13 `[S][DEFERRED]`** — Phase 13 (activity timeline)
  handler for reaction / comment events
  - Timeline phase has not landed yet. MediatR events ship in
    T-026-05, T-026-08 and T-026-10 so Phase 13 can subscribe
    later; no handler lives in Phase 26.

- [ ] **T-026-14 `[P]`** — Notes for Phase 24: coalescing rules
  for reaction / comment push scheduling
  - Document in `docs/adr/00XX-reaction-comment-notifications.md`.
  - No code — Phase 24 owns delivery.

## Section H — Mobile detail route + atoms

- [ ] **T-026-15 `[T][S]`** — Shared types + mobile service / hooks
  for the new endpoints
  - Mirror `CheckInDetail`, `ReactionAggregate`, `Comment` types in
    `packages/shared`.
  - Extend `checkInService` with `detail(id)`, `toggleReaction`,
    `listReactionUsers`, `listComments`, `createComment`,
    `deleteComment`.
  - Extend `useCheckIn` hooks class with `useDetail`,
    `useToggleReaction`, `useListComments`, `useCreateComment`,
    `useDeleteComment`.
  - Green.

- [ ] **T-026-16 `[T][S]`** — `CheckinMediaHero` atom
  - Failing spec: renders a square image when `mediaPreview` is
    set; placeholder dumbbell fallback otherwise.
  - Green.

- [ ] **T-026-17 `[T][S]`** — `XpBreakdown` atom
  - Failing spec: renders `base + multiplier rows + total`; hides
    the multiplier row when the multiplier list is empty.
  - Green.

- [ ] **T-026-18 `[T][S]`** — `ReactionsStrip` organism (12 chips)
  - Failing spec: renders 12 chips with counts (zero-count chips
    collapse into a "+" menu); tap = optimistic toggle with
    rollback on failure; tapping an existing count expands the
    "who reacted" sub-panel fed by `GET /reactions?include=users`.
  - Green.

- [ ] **T-026-19 `[T][S]`** — `CommentsSection` organism (inline)
  - Failing spec: renders the paginated list oldest-first,
    optimistic append on submit, delete affordance for author and
    check-in owner, 280-char counter.
  - Green.

- [ ] **T-026-20 `[T][S]`** — `app/(app)/checkin/view/page.tsx`
  detail route
  - Failing spec: loads `GET /check_ins/{id}`; renders header
    (author + relative timestamp), `CheckinMediaHero`, title +
    notes, `XpBreakdown`, `ReactionsStrip`, `CommentsSection`;
    back navigation preserves the previous scroll position.
  - Green.

- [ ] **T-026-21 `[T][S]`** — `CheckinCard` feed integration
  - Failing spec: card shows aggregate reaction chips (read-only)
    + comment count badge from the batched payload; card body
    links to `/checkin/view?id={id}`.
  - Green.

## Section I — Wrap-up

- [ ] **T-026-22 `[P]`** — ADR `0XXX-reactions-and-comments.md`
  - Palette choice, aggregate-on-read, soft vs hard delete,
    MediatR event names, open-visibility MVP decision.

- [ ] **T-026-23 `[S]`** — Phase close.

---

## Dependencies

```
A ──▶ B ──▶ C ──▶ D ──▶ E ──▶ F
                                ↓
                                H
                                ↓
                                I
```

(`G` is deferred / notes-only — safe to ship without.)

## Bundling strategy for PRs

1. `docs(social): spec + plan + tasks for check-in reactions and comments`
2. `feat(social): check-in reactions and comments schema` — A
3. `feat(social): open check-in visibility seam` — B
4. `feat(social): emoji reaction toggle endpoints` — C
5. `feat(social): comment endpoints with soft-delete` — D
6. `feat(check-ins): individual get endpoint powering the detail route` — E
7. `feat(feed): batch reactions and comment counts into check-in list` — F
8. `feat(mobile): check-in detail route with reactions and comments` — H
9. `docs(adr): reactions and comments` — I
