# Phase 13 — Activity Timeline / Feed

> Status: **planning only**.

> **Stack note:** artifacts in this file (module names, package names,
> code samples) were originally written for Elixir/Phoenix. The
> project switched to C# + ASP.NET Core 9 in Phase 2. See
> `specs/roadmap.md` → "Stack migration note" for the mapping table.
> Concepts and endpoint contracts still hold; concrete artifacts get
> rewritten when this phase is picked up.


## 1. Goal

Give every user a feed showing their own activity + their friends'
activity in chronological order. Events are produced by every
domain service that "matters" (check-in, level up, medal, insignia,
trophy, challenge start/end, friend accept).

## 2. Scope

**In scope**
- `activity_events` table (append-only log).
- Emitter functions across existing contexts (check-in, level-up,
  medals).
- Feed queries: personal feed (own events) and friends feed (own +
  friends), paginated.
- Fan-out on read initially (query joins friendships). Design a
  path to fan-out on write for scale.
- Feed item rendering per event kind on mobile.
- `notifications` table for "somebody did something involving you"
  events (push delivery lives in Phase 23).

**Out of scope**
- Feed interactions (react, comment, share) — later.
- Push notifications delivery (Phase 23).
- Ranking/aggregating (this is a chronological feed, not a scored one).

## 3. Approach

- `activity_events`: `id, user_id, kind, subject_type, subject_id,
  payload (jsonb), created_at`.
- Kinds: `check_in.posted`, `level.up`, `medal.earned`,
  `insignia.earned` (Phase 17), `trophy.earned` (Phase 20),
  `challenge.started`, `challenge.finished`, `friend.accepted`.
- Emitters live in domain contexts (`CheckIns`, `Achievements`,
  `Challenges` when they exist). They insert an `activity_events`
  row in the same transaction as the primary change.
- Feed query for user U: `WHERE user_id IN (U ∪ friends_of(U))
  ORDER BY created_at DESC LIMIT N`. Index on `(user_id,
  created_at DESC)`.
- Visibility gate: the feed query also honors
  `Social.Visibility.can_see?` (from Phase 12) for each event —
  filter after fetch, or add a cached `visibility` column on the
  event.

## 4. Artifacts

- Migrations: `create_activity_events.exs`,
  `create_notifications.exs`.
- `lib/xpeak/feed.ex` — context.
- `lib/xpeak/feed/activity_event.ex`, `notification.ex`.
- `lib/xpeak/feed/emitter.ex` — helper called by other contexts.
- Update `CheckIns.create_check_in/2` and
  `Achievements.MedalAwarder` to call the emitter.
- Endpoints: `GET /feed?scope=me|friends&cursor=...`,
  `GET /notifications`, `POST /notifications/:id/read`.
- Mobile: feed tab + notification bell + item renderers.

## 5. Dependencies

- Blocked by: Phase 12 (friends graph).
- Blocks: Phase 14 (rankings row clicks into profile which shows
  timeline snippets), Phase 21 (recap reads events), Phase 23
  (push delivery).

## 6. Open questions

1. **Fan-out strategy** — read-side join is simple but breaks past
   a few thousand friends. Threshold to switch to write-side
   fan-out?
2. **Event payload shape** — freeze early or evolve? Recommendation:
   version each kind (`kind: "check_in.posted", version: 1`) so
   payload evolves safely.
3. **Empty state UX** — first-time user sees no events; what nudges
   them to check in / add friends?
4. **Retention** — keep events forever, or archive after 12 months?

## 7. Success criteria

- Every check-in produces an `activity_events` row.
- Every medal awarded produces an event.
- Personal feed shows own events.
- Friends feed shows own + friends' events, sorted correctly.
- Blocked / privacy-hidden events omitted.
- Notifications inbox shows social events (friend request,
  accepted).
- Mobile feed renders each kind with the right card layout.
- CI green.
