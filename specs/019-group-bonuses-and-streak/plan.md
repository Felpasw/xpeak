# Phase 19 — Group Bonuses & Group Streak

> Status: **planning only**.

## 1. Goal

Two mechanics that reward group activity:

1. **Group workout bonus** — when N members of a challenge check in
   at the same GPS location within a small time window, everyone
   in that session gets a multiplier boost.
2. **Group streak** — a challenge group has its own collective
   streak: at least K% of members check in on the same day. While
   active, everyone gets a stacked bonus.

## 2. Scope

**In scope**
- Detection of co-located check-ins via GPS + time-window heuristic.
- Group session record (`group_workouts`) linking eligible check-ins.
- Group streak counter (`group_streaks`) per challenge.
- Extension to `MultiplierResolver` for group bonuses (respects the
  overall cap from Phase 9).
- Push notification when a group streak is at risk (via Phase 23
  when it lands).
- Mobile: group session badge on check-in cards, group streak
  indicator on challenge detail.

**Out of scope**
- Manual grouping of check-ins (e.g., "I trained with @friend but
  no GPS") — deferred.
- Cross-challenge group bonuses (a check-in either belongs to a
  group workout inside one challenge, or none).

## 3. Approach

- On check-in creation, if the user is a member of one or more
  active group challenges and provided GPS coordinates:
  - Query recent check-ins (last 30 min) from other members of the
    same challenge within radius (100 m default).
  - If ≥ 2 total members co-present → create/join a
    `group_workouts` row and attach the check-in to it.
- Group workout bonus applied at the resolver level using the
  group's size (`1.2× for 2, 1.5× for 3+`) capped at the global
  cap.
- Group streak updated by an Oban job daily at 00:15 UTC per
  challenge time zone (or globally UTC for MVP).

## 4. Artifacts

- Migrations: `create_group_workouts.exs`,
  `create_group_workout_check_ins.exs`, `create_group_streaks.exs`,
  `add_gps_to_check_ins.exs`.
- `lib/xpeak/social/group_workout_detector.ex` (pure + Repo layer).
- `lib/xpeak/social/group_streak_updater.ex` (Oban).
- Update `MultiplierResolver.resolve/3` for group multiplier.
- Endpoints: `GET /challenges/:id/group_streak`,
  `GET /group_workouts/:id`.
- Mobile: opt-in for GPS on check-in; group session badges.

## 5. Dependencies

- Blocked by: Phases 5, 6, 7, 15, 17.
- Blocks: —

## 6. Open questions

1. **Radius** — 100 m too tight for large gyms? 200 m?
2. **Time window** — 30 min OK for a workout session?
3. **Minimum members for group bonus** — 2 (tight) or 3
   (harder to trigger)?
4. **Group streak threshold** — 50% or 30%?
5. **GPS privacy** — always optional? Stored coarsely? For
   MVP, store raw lat/long with the check-in but never expose it
   publicly (only used for detection, hashed for logs).

## 7. Success criteria

- Two members check in at the same gym within 30 min → both get
  `1.2×` group multiplier applied, group workout record shows both.
- Challenge with 5 members: if 3+ check in daily → group streak
  advances; if fewer → group streak breaks.
- Mobile: check-in card shows "Group workout with @user" badge.
- No regression on solo check-in flow.
- CI green.
