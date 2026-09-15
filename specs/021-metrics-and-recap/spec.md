# Phase 21 — Metrics & Recap (SPEC)

## 1. Data model

### 1.1. `metric_daily`

| Column             | Type    | Constraints                     |
|--------------------|---------|---------------------------------|
| `id`               | uuid    | PK                              |
| `user_id`          | uuid    | FK → users, not null            |
| `day`              | date    | not null                        |
| `check_in_count`   | integer | not null, default `0`           |
| `xp_earned`        | integer | not null, default `0`           |
| `category_totals`  | jsonb   | not null, default `{}`          |
| `multiplier_totals` | jsonb  | not null, default `{}`          |

Unique index: `(user_id, day)`.

Populated nightly (00:15 UTC) for the previous day; opportunistic
recompute on check-in for today's row.

### 1.2. `recaps`

| Column         | Type              | Constraints                        |
|----------------|-------------------|------------------------------------|
| `id`           | uuid              | PK                                 |
| `user_id`      | uuid              | FK → users, not null               |
| `period`       | string            | not null (e.g., `2026`)            |
| `payload`      | jsonb             | not null                           |
| `generated_at` | utc_datetime_usec | not null                           |
| `card_url`     | string            | nullable (final summary image)     |

Unique index: `(user_id, period)`.

## 2. Metrics API

`Xpeak.Metrics`:

- `dashboard(user, period)` — returns:
  - Totals: xp, level, title, check_ins, training_days,
    current_streak, longest_streak, next_level_progress.
  - Per-category XP.
  - Per-multiplier source XP.
  - Best day/week/month.
- `heatmap(user, year)` — day-by-day XP intensity for the calendar.

### 2.1. Endpoints

- `GET /me/metrics?period=7d|30d|month|year|all|custom&from=&to=`
- `GET /me/metrics/heatmap?year=2026`

## 3. Recap

### 3.1. `RecapGenerator.perform/1`

Input: `%{ user_id, period }`.

Steps:
1. Aggregate from `metric_daily`, `activity_events`, `user_medals`,
   `user_insignias`, `user_trophies`, `friendships`.
2. Compose payload (see `ideas.md` §2.8.2 for content).
3. Render final summary card image (backend or client — decision
   pending).
4. Insert `recaps` row with `payload` and `card_url`.

### 3.2. Trigger

Per decision (§6.1):

- **Calendar year**: Oban cron 2026-01-01 00:15 UTC iterates all
  eligible users and enqueues one job per user.
- **Anniversary**: cron runs daily, enqueues for users whose signup
  anniversary was yesterday.

### 3.3. Endpoints

- `GET /recaps` — list user's recaps.
- `GET /recaps/:period` — 200 with payload if ready, 202 with ETA
  otherwise.
- `POST /recaps/:period/regenerate` — admin only; re-runs the job.

## 4. Mobile

### 4.1. Metrics on home

- Snapshot section (choose A or B per Q5).
- Contains: title + level + level progress, current streak + streak
  calendar mini, XP to next level, "See full metrics" link.

### 4.2. Full metrics screen

- Filters (period selector).
- Sections:
  - Totals card.
  - Category breakdown chart.
  - Streak calendar (full year, GitHub-style).
  - Multiplier history (stacked bar over time).
  - Best days highlights.

### 4.3. Yearly recap viewer

- Full-screen story cards (Insta-story-like).
- Auto-play with tap-to-skip / hold-to-pause.
- Each card shareable individually.
- Final card is the shareable summary (downloadable + share sheet).

## 5. Test plan

- `RollupJob` on synthetic dataset produces expected `metric_daily`
  rows.
- `Metrics.dashboard/2` returns correct totals per period.
- `Metrics.heatmap/2` correct data density.
- `RecapGenerator` produces expected payload for a fabricated user.
- Endpoint 202 vs. 200 behavior.
- Mobile: snapshot renders; deep screen filters change; recap
  navigates through cards; share sheet triggers.

## 6. Non-goals

- Real-time metrics.
- Multi-year comparison graphs.
- Compare-to-other-users leaderboards (Phase 14 covers competitive
  view separately).
