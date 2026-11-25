# Phase 26 — Check-in Reactions & Comments

> Status: **planning only**. Companion to `spec.md` and `tasks.md`.
> Stack: C# 12 + ASP.NET Core 9 + EF Core 9 + Postgres 16.

## 1. Goal

Turn check-ins from a solo log entry into a social publication:
friends (or a wider audience, depending on visibility) can leave a
one-tap emoji reaction and drop short text comments. Nothing threaded,
no editing, no rich text — the goal is quick, low-friction feedback
that closes the loop between the person who trained and their crew.

## 2. Scope

**In scope**

- `check_in_reactions` table — one row per (check-in, user, emoji)
  triple. A user can hold multiple different reactions on the same
  check-in but the same emoji cannot repeat.
- `check_in_comments` table — flat list of text comments authored
  by any user with visibility. `deleted_at` for soft-delete only
  (author or check-in owner can delete).
- Fixed reaction palette for MVP: 👍 💪 🔥 🎯 🎉 👏 💯 🚀 ⚡ 🏆 😎 🫡
  (12 positive emojis). Opening the picker to any Unicode emoji is
  deferred until we hear it asked for; when we do, we'll normalize
  skin-tone / variation-selector modifiers so `👍` and `👍🏽` collapse
  into the same bucket instead of bloating the aggregate.
- Comment length cap: 280 chars (matches Phase 5 check-in note cap).
- Endpoints (all auth-gated, visibility-checked via
  `ICheckInVisibility` — MVP returns true for any authenticated
  caller; Phase 12 later swaps the implementation without touching
  endpoint code):
  - `POST /check_ins/{id}/reactions` — toggle a reaction (idempotent).
  - `GET  /check_ins/{id}/reactions` — grouped counts + who reacted.
  - `POST /check_ins/{id}/comments` — new comment.
  - `GET  /check_ins/{id}/comments` — paginated by `created_at ASC`.
  - `DELETE /comments/{id}` — soft-delete (author or owner).
- Mobile: dedicated `/checkin/view?id=<uuid>` route rendering the
  check-in with media hero, XP breakdown, reactions strip and
  inline comments section — no bottom-sheet modals. Feed cards
  render the aggregate strip + comment count badge from the
  batched payload and link to the detail route.

**Out of scope**

- Threaded replies (deferred, `ideas.md` §2.5.3).
- Rich text / mentions / links preview (later, if we build a real
  feed engagement layer).
- Custom emoji uploads (deferred).
- Moderation queue / reporting (Phase 12 privacy has blocks; content
  moderation is a much later story — `ideas.md` §2.5.4).
- Notification delivery for reactions / comments — the events fire
  (see Phase 13) but push/email routing is Phase 24's problem.

## 3. Approach

- **Aggregation on read**, not write. Reaction counts and comment
  counts are computed from the base tables with an index; no
  denormalized counter columns until read volume demands it.
- **Visibility is checked once per request** at the endpoint layer
  through an `ICheckInVisibility` seam. The MVP implementation
  returns true for every authenticated caller (open feed); Phase 12
  later provides the friend-graph implementation without the
  endpoint layer caring. Reaction / comment services stay
  visibility-agnostic.
- **Idempotent reaction toggle** — `POST /check_ins/{id}/reactions`
  with the same emoji from the same user removes it (nice for
  double-tap UX). Return the updated aggregate in the same response
  to avoid a follow-up GET.
- **Domain events** — reaction and comment writes emit MediatR
  notifications (`CheckInReactionAdded`, `CheckInCommentPosted`) so
  Phase 13 (activity timeline) can fan them into feeds and Phase 24
  (push) can decide whether the owner gets a notification, without
  either phase coupling directly into this module.
- **Soft-delete for comments only** — reactions hard-delete on
  toggle-off (they don't carry narrative weight).

## 4. Artifacts

- Migration: `AddCheckInReactionsAndComments` (two tables + indexes
  on `check_in_id` and `(check_in_id, user_id, emoji)` unique).
- `Social/Entities/CheckInReaction.cs`, `CheckInComment.cs`.
- `Social/Repositories/ICheckInReactionRepository.cs` +
  `ICheckInCommentRepository.cs`.
- `Social/Services/ICheckInVisibility.cs` — single seam for
  visibility (MVP: `OpenCheckInVisibility` returns true for any
  authenticated caller).
- `Social/Services/ICheckInSocialService.cs` — orchestrates toggle
  and comment CRUD, emits MediatR events.
- `Social/Endpoints/CheckInReactionsEndpoint.cs` + `CheckInCommentsEndpoint.cs`.
- `Social/Dto/*` — request/response DTOs + validators.
- Mobile route: `app/(app)/checkin/view/page.tsx` — the detail
  screen.
- Mobile components: `organisms/ReactionsStrip.tsx`,
  `organisms/CommentsSection.tsx`, `atoms/CheckinMediaHero.tsx`,
  `atoms/XpBreakdown.tsx`. Feed cards (`CheckinCard`) gain the
  aggregate strip + comment badge from the batched payload.

## 5. Dependencies

- **Blocked by**: Phase 5 (check-in exists).
- **Soft-dependency on Phase 12** (friendship + visibility rules) —
  the `ICheckInVisibility` seam ships here so Phase 12 can plug its
  implementation in without touching endpoints. MVP ships with an
  open implementation (any authenticated caller).
- **Blocks**: Phase 24 (push notifications routes reaction/comment
  events). Phase 13 (activity timeline) subscribes to the MediatR
  events emitted here.
- **Adjacent**: Phase 6 (check-in media) — reactions/comments hang
  off the check-in, not the media itself, so ordering vs. Phase 6
  is free. Ship Phase 6 first so the check-in detail screen has
  content worth reacting to.

## 6. Open questions

1. **Reaction palette** — resolved: 12 fixed positive emojis for
   MVP. Open picker deferred; if/when we open it, we normalize
   skin-tone modifiers to avoid 6-way splits of the same signal.
2. **Comment edit** — allow within 5 min of posting or never? MVP
   leans "never" (simpler, no edit history table).
3. **Notification cadence** — coalesce multiple reactions into one
   push ("5 friends reacted to your check-in") or one push per
   reaction? Decide in Phase 24, not here.
4. **Aggregate cache** — `IMemoryCache` on reaction counts once we
   have a hot feed, or straight to the read model / materialized
   view? Defer the decision until we have real read volume.
5. **Comment sort** — ascending (chronological, chat-like) or
   descending (newest first, Instagram-like)? MVP: ascending.

## 7. Success criteria

- `POST /check_ins/{id}/reactions` with `{ "emoji": "🔥" }` toggles
  the reaction and returns the updated grouped count.
- `POST /check_ins/{id}/comments` with `{ "body": "..." }` creates a
  comment; response includes the persisted row.
- `GET /check_ins/{id}/reactions` returns
  `{ counts: { "🔥": 3, "💪": 1 }, mine: ["🔥"] }`.
- `GET /check_ins/{id}/comments?limit=20&cursor=...` returns a
  page of comments oldest-first with author + timestamp.
- `DELETE /comments/{id}` soft-deletes when caller is the author
  or the check-in owner; 403 otherwise.
- Visibility routed through `ICheckInVisibility`: MVP open — every
  authenticated caller succeeds; when a test double or Phase 12
  implementation returns false, endpoints respond with 404 (not
  403) to obscure existence.
- Feed item on mobile renders the aggregate reaction strip and the
  comment count badge without an extra round-trip (data included in
  the feed list response).
- Reaction / comment writes emit MediatR notifications consumed by
  Phase 13 (feed entry) and Phase 24 (push scheduling).
