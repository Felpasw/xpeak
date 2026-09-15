# Phase 12 — Friends & Social Graph

> Status: **planning only**.

> **Stack note:** artifacts in this file (module names, package names,
> code samples) were originally written for Elixir/Phoenix. The
> project switched to C# + ASP.NET Core 9 in Phase 2. See
> `specs/roadmap.md` → "Stack migration note" for the mapping table.
> Concepts and endpoint contracts still hold; concrete artifacts get
> rewritten when this phase is picked up.


## 1. Goal

Introduce the friendship graph: add by `@username`,
accept/reject/remove/block, read-only view of a friend's profile,
and per-user privacy settings.

## 2. Scope

**In scope**
- `friendships` table (bidirectional relation).
- `friend_requests` (pending state) or state column on friendships
  — decided in `spec.md`.
- `blocks` table (unilateral, two-way effect).
- `privacy_settings` on `users` (embedded JSON or separate columns).
- Endpoints: search-by-username, send/cancel request,
  accept/reject, remove, block/unblock, list friends, view friend
  profile.
- Mobile: friends tab, search, requests inbox, profile viewer.

**Out of scope**
- Activity feed of friends (Phase 13).
- Global rankings (Phase 14).
- Friend challenges (Phase 16).
- Public/private profile toggle (Q1 below decides).

## 3. Approach

- Model: **mutual friends by default** (both must accept). Simpler
  and matches the fitness/social-privacy intuition — see
  `ideas.md` §2.5.1 for context.
- Store `friendships` as a single row per pair (canonical: `user_a_id
  < user_b_id`) with `state` in `["pending","accepted"]`. Pending
  rows include a `requested_by_id` so we know who to notify.
- Rejecting deletes the pending row (no soft-hide, keep table small).
- Removing an accepted friendship also deletes the row.
- Blocking creates a `blocks` row and cascades: any friendship
  between the two is deleted.
- Privacy defaults: friends-only for check-ins/media/medals; public
  for profile card basics (username, avatar, level, title, streak).

## 4. Artifacts

- Migrations: `create_friendships.exs`, `create_blocks.exs`,
  `add_privacy_to_users.exs`.
- `lib/xpeak/social.ex` — context.
- `lib/xpeak/social/friendship.ex`, `block.ex`.
- `lib/xpeak/social/visibility.ex` — pure helper computing "can X
  see Y's field Z".
- Router: `/friends`, `/friends/requests`, `/blocks`, `/users/:username`
  (public read).
- Mobile: friends tab, search screen, requests inbox, public profile
  viewer, block confirmation modal.

## 5. Dependencies

- Blocked by: Phase 3 (users).
- Blocks: Phase 13 (feed fan-out reads friendships), Phase 14
  (rankings link into public profile view), Phase 16 (friend
  challenges), Phase 17 (group challenges), Phase 22 (push
  notifications for social events).

## 6. Open questions

1. **Profile mode** — mutual-only, or add a per-user toggle
   "public profile" (Instagram-style) that makes friend requests
   auto-accepted? Recommendation: ship mutual-only in this phase,
   consider toggle in Phase 14 when rankings surface public profiles.
2. **Friend cap** — soft-limit 500? Hard limit?
3. **Request expiration** — pending requests auto-cancel after N
   days?
4. **Notifications on accept/reject** — via `notifications` table
   this phase, push delivery Phase 23.

## 7. Success criteria

- User A sends request to `@user_b` → user B sees a pending request
  → accepts → both are `friends`.
- Rejecting a request removes it.
- Blocking user X hides A and X from each other on all read paths.
- Privacy toggles honored: private check-ins invisible to non-friends.
- CI green.
