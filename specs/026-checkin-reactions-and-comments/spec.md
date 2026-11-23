# Phase 26 — Check-in Reactions & Comments (SPEC)

## 1. Summary

Emoji reactions (fixed palette, toggle) and flat text comments on
check-ins. **MVP visibility is open** — any authenticated user can
see, react to and comment on any check-in. Friend-graph gating is
reintroduced when Phase 12 ships; the visibility helper is wired
through a single extension point so the switch is a one-line change.
Writes emit MediatR notifications for downstream feed and push
consumers.

## 2. Data model

### 2.1. `check_in_reactions` table

| Column         | Type              | Constraints                                     |
|----------------|-------------------|-------------------------------------------------|
| `id`           | uuid              | PK                                              |
| `check_in_id`  | uuid              | FK → `check_ins.id`, not null, cascade delete   |
| `user_id`      | uuid              | FK → `users.id`, not null                       |
| `emoji`        | text              | not null, in the MVP palette (see §3)           |
| `created_at`   | timestamptz       | not null, default `now()`                       |

Indexes:
- `unique (check_in_id, user_id, emoji)` — one row per triple.
- `(check_in_id)` for aggregate queries.

Hard-deleted on toggle-off. No soft-delete.

### 2.2. `check_in_comments` table

| Column         | Type              | Constraints                                     |
|----------------|-------------------|-------------------------------------------------|
| `id`           | uuid              | PK                                              |
| `check_in_id`  | uuid              | FK → `check_ins.id`, not null, cascade delete   |
| `user_id`      | uuid              | FK → `users.id`, not null                       |
| `body`         | text              | not null, length ≤ 280                          |
| `created_at`   | timestamptz       | not null, default `now()`                       |
| `deleted_at`   | timestamptz       | nullable (soft-delete marker)                   |

Indexes:
- `(check_in_id, created_at)` — feed ordering.
- Partial `(check_in_id) where deleted_at is null` — live counts.

## 3. Reaction palette

MVP fixed palette (12 emojis):
`👍`, `💪`, `🔥`, `🎯`, `🎉`, `👏`, `💯`, `🚀`, `⚡`, `🏆`, `😎`, `🫡`.

Rationale: enough breadth to feel expressive without letting the
aggregate turn into 30-way soup on a popular check-in. All positive
/ affirming — no ambiguous faces, no negatives. Validated
server-side via `FluentValidation`; anything outside the whitelist
returns 422.

Extending the palette is a config change (no schema migration).
Opening to any Unicode emoji is deferred; when it happens, the
service must normalize skin-tone modifiers and variation selectors
so `👍`, `👍🏻` … `👍🏿` collapse into the same aggregate bucket.

## 4. Endpoints

All endpoints require `Authorization: Bearer <jwt>` and pass through
a single `ICheckInVisibility` helper. For the MVP the helper returns
true for any authenticated caller on any existing check-in (open
visibility). When Phase 12 ships, the helper becomes the single
seam where the friend-graph rule lands and the endpoints switch to
returning 404 (not 403) to obscure existence — no endpoint code
changes.

### 4.1. `POST /check_ins/{id}/reactions`

Toggle a reaction. Idempotent: same emoji from same user removes the
row, different emoji adds a new row.

Request:
```json
{ "emoji": "🔥" }
```

Response `200`:
```json
{
  "counts": { "🔥": 3, "💪": 1, "🎯": 5 },
  "mine": ["🔥"]
}
```

Failures: `404` (not visible), `422` (emoji not in palette).

### 4.2. `GET /check_ins/{id}/reactions`

Returns the aggregate + the caller's own reactions.

Response `200`: same shape as `POST` response above.

Optional query `?include=users` returns an extra `users` field:
```json
{
  "counts": { "🔥": 3 },
  "mine": ["🔥"],
  "users": {
    "🔥": [
      { "id": "...", "username": "alice", "avatar_url": "..." }
    ]
  }
}
```

Capped at 20 users per emoji; older reactions collapse into
`... and N more`.

### 4.3. `POST /check_ins/{id}/comments`

Create a comment.

Request:
```json
{ "body": "gg mano" }
```

Response `201`:
```json
{
  "id": "...",
  "check_in_id": "...",
  "author": { "id": "...", "username": "...", "avatar_url": "..." },
  "body": "gg mano",
  "created_at": "..."
}
```

Failures: `404`, `422` (empty body / over 280 chars).

### 4.4. `GET /check_ins/{id}/comments`

Cursor-paginated, oldest-first.

Query: `?limit=20&cursor=<opaque>`

Response `200`:
```json
{
  "items": [ /* comment shape from 4.3 */ ],
  "next_cursor": "..."
}
```

Excludes soft-deleted comments.

### 4.5. `DELETE /comments/{id}`

Soft-delete. Allowed for:
- The comment's author.
- The owner of the parent check-in.

Response `204`. Failures: `403` (not author or owner), `404`.

## 5. Domain events (MediatR)

Emitted after commit:

- `CheckInReactionAdded { CheckInId, UserId, Emoji, CreatedAt }`
- `CheckInReactionRemoved { CheckInId, UserId, Emoji }`
- `CheckInCommentPosted { CommentId, CheckInId, AuthorId, Body, CreatedAt }`
- `CheckInCommentDeleted { CommentId, CheckInId, DeletedByUserId }`

Phase 13 (activity timeline) subscribes to add its feed rows.
Phase 24 (push notifications) subscribes to decide push routing
with its own coalescing rules.

## 6. Mobile

### 6.1. Dedicated detail route

Opening a check-in from any feed card navigates to
`/checkin/view?id=<uuid>`. The route is the single surface for
reactions and comments — there is no bottom-sheet modal, no
in-place expansion. The URL is deep-linkable and shareable.

Layout, top to bottom:

1. **Header** — author avatar + `@username` + relative timestamp.
2. **Media hero** — square-ish full-width image (or video). Falls
   back to a muted dumbbell placeholder when the check-in has no
   media.
3. **Title + notes** — the check-in's title and the author's notes.
4. **XP card** — XP earned with a compact breakdown of the
   `scoring_snapshot.multipliers` (base + each multiplier that
   applied).
5. **Reactions strip** — row of chips with counts (see §6.2).
6. **Comments section** — inline list + input (see §6.3).

Back navigation returns the user to the previous scroll position
on the feed (Next's router cache handles this out of the box).

### 6.2. Reactions strip

Renders on the detail route and as a compact read-only badge row
on feed cards.

- 12 emoji chips (the MVP palette) with count badges. Chips with
  zero count collapse under a "+" button to keep the strip tidy.
- Tap = toggle. Optimistic update (immediate visual flip) with
  rollback on failure.
- Tapping an existing count opens a **"who reacted" view** backed
  by `GET /reactions?include=users` — grouped by emoji, up to 20
  users per emoji, overflow collapsed as "... and N more". The view
  is a sub-panel on the detail route (expand/collapse), not a
  modal.

### 6.3. Comments section

Inline on the detail route, directly under the reactions strip.

- Header: total count ("12 comentários").
- Body: paginated list of comments, oldest at top, own comments
  right-aligned with a subtle background. Virtualized when the
  list grows (sentinel-driven fetch-next-page).
- Footer: text input (280 char counter), send button disabled
  until non-empty. Optimistic append on submit.
- Author or check-in owner sees a delete affordance on each
  comment.

### 6.4. Feed integration

Feed list response includes:
```json
{
  "reactions": { "counts": { "🔥": 3 }, "mine": ["🔥"] },
  "comments_count": 2
}
```

so the feed card renders the aggregate chip row and the comment
badge without any per-item round-trip. Tapping the card (or the
reactions / comments affordances) always navigates to the detail
route — no in-feed interactivity besides visual.

## 7. Test plan

### API
- `POST` toggle: same emoji twice → count goes up then down.
- `POST` two different emojis from same user → both persist.
- Palette validation: unknown emoji → 422.
- Visibility (MVP open): any authenticated caller reaches any
  check-in's reactions / comments endpoints with 200/201/204.
- Visibility seam: `ICheckInVisibility` has a test double that
  returns false, so endpoints flip to 404 without code change —
  future-proofs the Phase 12 swap.
- Comment length: 281 chars → 422.
- Delete auth: author OK, check-in owner OK, third party 403.
- Cascade: deleting a check-in wipes its reactions and comments.
- MediatR emission: writes trigger the four notification types.

### Mobile
- Optimistic reaction toggle rolls back on 5xx.
- Comment optimistic append rolls back on 5xx.
- `/checkin/view?id=...` renders header, media, XP breakdown,
  reactions strip and comments section from `GET /check_ins/{id}`
  plus the two aggregates.
- "Who reacted" sub-panel lists the correct users per emoji.
- Feed card renders the strip + comment badge from the batched
  payload (no extra request) and navigates to the detail route on
  tap.

## 8. Non-goals

- Threaded replies (deferred).
- Rich text, markdown, mentions, link previews.
- Reaction on comments (only on the check-in itself).
- Custom emoji uploads.
- Content moderation queue.
