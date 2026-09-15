# Phase 14 — Global Rankings & Discovery (SPEC)

## 1. Data model

### 1.1. `rankings_snapshots`

| Column        | Type              | Constraints                        |
|---------------|-------------------|------------------------------------|
| `id`          | uuid              | PK                                 |
| `scope`       | string            | not null                           |
| `period`      | string            | not null (e.g., `2026-W03`, `all`) |
| `user_id`     | uuid              | FK → users                         |
| `rank`        | integer           | not null                           |
| `value`       | integer           | not null                           |
| `delta`       | integer           | nullable (vs previous period)      |
| `computed_at` | utc_datetime_usec | not null                           |

Unique index: `(scope, period, user_id)`.

### 1.2. `users` addition

- `show_in_global_rankings` (boolean, not null, default `true`).

Add to the `privacy` JSONB (Phase 12) as well for surface
consistency.

## 2. Scopes

Enum values:

- `xp_all_time`
- `xp_weekly`
- `xp_monthly`
- `streak_current`
- `streak_ever`
- `xp_category:<slug>` (parameterized)

For each: also `friends:<scope>` (e.g., `friends:xp_all_time`).

## 3. Rankings context

- `Xpeak.Rankings.list(scope, opts)` → top rows.
- `Xpeak.Rankings.my_rank(user, scope)` → `{ rank, value, delta }`
  or `nil` if user has opted out or hasn't earned any XP.
- `Xpeak.Rankings.friends_scoped(user, scope, opts)` — scoped read.

## 4. Snapshot job

`Xpeak.Rankings.SnapshotJob` (Oban):

- `perform/1` accepts `%{scope: ..., period: ..., mode: :current | :freeze}`.
- Scheduled hourly for current period; scheduled once at rollover
  for the closed period.
- Uses `INSERT ... ON CONFLICT (scope, period, user_id) DO UPDATE`.

## 5. Cache

- `Xpeak.Rankings.Cache` wraps Cachex.
- Keys: `{scope, period, page}`, `{scope, period, :count}`.
- TTL 60–300s depending on scope (all-time longer, weekly shorter).
- Invalidated after each snapshot run.

## 6. Endpoints

- `GET /rankings/:scope?period=<optional>&limit=&cursor=`
  → `{ rows: [ { rank, value, delta, user: {id, username, avatar,
  frame_slug, current_title, level} } ], next_cursor }`.
- `GET /rankings/:scope/me?period=<optional>`
  → `{ rank, value, delta }` or 404 if not enrolled.
- `GET /rankings/scopes` → static list of scopes with human labels.

## 7. Mobile

### 7.1. Rankings tab

- Bottom-nav tab.
- Top: scope selector (chips) + period selector (this week / this
  month / all-time as applicable).
- List: rank number, avatar + frame, `@username`, current title,
  value, delta arrow.
- Row tap → public profile viewer (Phase 12) with sticky "Add
  friend" if not friends.
- Sticky top: "You are #423 · 1,240 XP · ▲ 15".

### 7.2. Empty state

- If opted out: banner explains + link to privacy settings.
- If not yet ranked: "Log your first check-in to appear here".

## 8. Test plan

- Snapshot job produces expected ranks (dense ordering).
- Opt-out excludes the user from list + `my_rank/2` returns nil.
- Delta computation vs previous period is correct.
- Cache invalidated after job.
- Endpoints paginated, respect scope.
- Mobile: scope switch re-fetches; row click opens profile with
  correct action.

## 9. Non-goals

- Real-time updates.
- Regional scopes (Phase 22).
- Ranking-based rewards (nice-to-have).
