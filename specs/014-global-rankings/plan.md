# Phase 14 — Global Rankings & Discovery

> Status: **planning only**.

> **Stack note:** artifacts in this file (module names, package names,
> code samples) were originally written for Elixir/Phoenix. The
> project switched to C# + ASP.NET Core 9 in Phase 2. See
> `specs/roadmap.md` → "Stack migration note" for the mapping table.
> Concepts and endpoint contracts still hold; concrete artifacts get
> rewritten when this phase is picked up.


## 1. Goal

Public leaderboards across multiple scopes. Auto-enrolled on
signup, opt-out via privacy toggle. Ranking rows link into the
public profile viewer from Phase 12 with "Add friend" quick action.

## 2. Scope

**In scope**
- Ranking scopes:
  - All-time XP.
  - Weekly XP (ISO week).
  - Monthly XP.
  - Longest current streak.
  - Longest streak ever.
  - Per-category XP.
  - Friends-scoped versions (same boards limited to friends).
- `rankings_snapshots` table + Oban job that computes snapshots.
- Endpoints for each scope with pagination + "my rank".
- Mobile: rankings tab with scope selector and "add friend" inline
  action.

**Out of scope**
- Regional rankings (Phase 22 exploration adds location).
- Real-time (websocket) rank updates.
- Historical trending / graphs of past periods.

## 3. Approach

- Ranking data is derived from `users.xp`, `users.current_streak_days`,
  `check_ins` (for time-bounded scopes) and `activity_events` (for
  weekly/monthly XP delta).
- **All-time boards** — no snapshot needed; just an indexed query
  (`ORDER BY xp DESC LIMIT 100`). Cache the top 100 in ETS/Cachex.
- **Time-bounded boards** — computed by an Oban job:
  - Hourly for the current period.
  - Immediately on rollover for the closed period (Sunday 23:59 UTC
    → freeze last-week board).
- Snapshots stored in `rankings_snapshots` keyed by `(scope, period,
  user_id)` with `rank`, `value`, `delta` from previous period.
- Privacy: users with `show_in_global_rankings = false` are excluded
  from all scoped queries and snapshots.

## 4. Artifacts

- Migrations: `create_rankings_snapshots.exs`,
  `add_show_in_global_rankings_to_users.exs`.
- `lib/xpeak/rankings.ex` — read API.
- `lib/xpeak/rankings/snapshot_job.ex` — Oban worker.
- `lib/xpeak/rankings/scope.ex` — value type + enum.
- Endpoints: `GET /rankings/:scope`, `GET /rankings/:scope/me`.
- Mobile: rankings tab with scope selector.
- Cache layer (Cachex).

## 5. Dependencies

- Blocked by: Phases 3 (users), 5 (check-ins), 7 (multipliers so
  values are meaningful), 12 (friends for friends-scoped and privacy
  respect).
- Blocks: Phase 22 (regional scope adds on top).

## 6. Open questions

1. **Cache backend** — Cachex (in-memory only) or Redis when we
   introduce it? Cachex works up to a certain scale.
2. **Weekly period start** — ISO week (Mon 00:00 UTC) or Sunday?
   ISO week is default for now.
3. **Snapshot retention** — 12 months? Forever?
4. **Ties** — rank shared or dense? Recommend dense (`1, 2, 2, 3`)
   for clarity.

## 7. Success criteria

- `GET /rankings/xp_all_time?limit=100` returns the top 100 users
  ordered by XP.
- `GET /rankings/xp_all_time/me` returns the caller's rank + value.
- Opt-out user disappears from all boards within one snapshot cycle.
- Weekly rollover freezes the closed period correctly.
- Mobile rankings tab renders each scope; row click opens public
  profile.
- CI green.
