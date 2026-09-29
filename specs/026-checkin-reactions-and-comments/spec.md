# Phase 26 — Check-in Reactions & Comments (SPEC)

## 1. Summary

Emoji reactions (fixed palette, toggle) and flat text comments on
check-ins. Visibility gated by the friendship graph (Phase 12).
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
the same visibility helper introduced in Phase 12 — a caller who
cannot see the check-in gets 404 (not 403), to obscure existence.

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

### 6.1. Reactions strip

Renders on the check-in detail screen and as a compact strip on the
feed item.

- Row of 5 emoji chips with count badges.
- Tap = toggle. Optimistic update (immediate visual flip) with
  rollback on failure.
- Long-press an existing count opens the "who reacted" sheet
  (`GET /reactions?include=users`).

### 6.2. Comments sheet

Bottom-sheet modal opened from a comment icon on the check-in card.

- Header: total count.
- Body: list of comments, oldest at top, own comments right-aligned
  with a subtle background.
- Footer: text input (280 char counter), send button disabled until
  non-empty.
- Author or check-in owner sees a delete affordance on each comment.

### 6.3. Feed integration

Feed list response includes:
```json
{
  "reactions": { "counts": { "🔥": 3 }, "mine": ["🔥"] },
  "comments_count": 2
}
```

so the feed can render the strip and the badge without a per-item
round-trip.

## 7. Test plan

### API
- `POST` toggle: same emoji twice → count goes up then down.
- `POST` two different emojis from same user → both persist.
- Palette validation: unknown emoji → 422.
- Visibility: friends-only check-in, non-friend caller → 404 on
  every endpoint.
- Comment length: 281 chars → 422.
- Delete auth: author OK, check-in owner OK, third party 403.
- Cascade: deleting a check-in wipes its reactions and comments.
- MediatR emission: writes trigger the four notification types.

### Mobile
- Optimistic toggle rolls back on 5xx.
- Comment optimistic append rolls back on 5xx.
- Long-press opens the who-reacted sheet with correct users.
- Feed item renders the strip from the batched payload (no extra
  request).

## 8. Non-goals

- Threaded replies (deferred).
- Rich text, markdown, mentions, link previews.
- Reaction on comments (only on the check-in itself).
- Custom emoji uploads.
- Content moderation queue.
