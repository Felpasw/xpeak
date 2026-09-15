# Phase 19 — Group Bonuses & Group Streak (SPEC)

## 1. Data model

### 1.1. `check_ins` addition

- `location_lat` (float, nullable).
- `location_lng` (float, nullable).
- `location_accuracy_m` (integer, nullable).

Indexes: consider a GiST index on `(location_lat, location_lng)`
via PostGIS if we go that route; MVP uses a plain
`Haversine` in SQL with an approx bounding box filter.

### 1.2. `group_workouts`

| Column         | Type              | Constraints                        |
|----------------|-------------------|------------------------------------|
| `id`           | uuid              | PK                                 |
| `challenge_id` | uuid              | FK → challenges                    |
| `started_at`   | utc_datetime_usec | not null                           |
| `member_count` | integer           | not null                           |
| `location_lat` | float             | nullable (centroid)                |
| `location_lng` | float             | nullable                           |

### 1.3. `group_workout_check_ins`

| Column           | Type              | Constraints                     |
|------------------|-------------------|---------------------------------|
| `group_workout_id` | uuid            | FK → group_workouts             |
| `check_in_id`    | uuid              | FK → check_ins, unique          |

Composite PK: `(group_workout_id, check_in_id)`.

### 1.4. `group_streaks`

| Column               | Type              | Constraints                        |
|----------------------|-------------------|------------------------------------|
| `id`                 | uuid              | PK                                 |
| `challenge_id`       | uuid              | FK → challenges, unique            |
| `current_days`       | integer           | not null, default `0`              |
| `longest_days`       | integer           | not null, default `0`              |
| `last_active_on`     | date              | nullable                           |

## 2. Group-workout detector

```elixir
defmodule Xpeak.Social.GroupWorkoutDetector do
  @radius_m 100
  @time_window_min 30

  def maybe_attach(check_in) do
    # If check_in has GPS and user is in ≥1 active group challenge:
    # For each challenge, find recent (30 min) check-ins from other members
    # within @radius_m. If ≥ 1 other → create-or-join a group_workout.
  end
end
```

Called synchronously in `CheckIns.create_check_in/2` after insert.

## 3. `MultiplierResolver` update

Adds a `group` slot:

- `resolve/3` now returns `%Multiplier{streak, category, challenge,
  group, total}` (schema evolves — snapshot `payload` gains `group`
  key without breaking historical rows).
- Group multiplier from group workout size:
  - 2 members → 1.2×
  - 3–4 → 1.5×
  - 5+ → 1.75×
- Combined via product then capped at `multiplier.cap` from Phase 9.

## 4. Group streak updater

`Xpeak.Social.GroupStreakUpdater` (Oban cron, daily 00:15 UTC):

- For each active group challenge:
  - Count distinct members with a check-in yesterday.
  - If `count / total_members ≥ threshold` (default 0.5) → increment
    `current_days`; update `longest_days`.
  - Else → reset `current_days = 0`.

## 5. Endpoints

- `GET /challenges/:id/group_streak` → `{ current_days,
  longest_days, last_active_on, active_today?, threshold, members_active_today }`.
- `GET /group_workouts/:id` → members list + check-in count.

## 6. Mobile

### 6.1. Check-in flow

- Ask for GPS permission on first check-in that lands in a group
  challenge.
- On subsequent check-ins, prompt "Include location?" if not
  already granted.
- Never send GPS if the user declined.

### 6.2. Check-in card badge

- If the check-in is part of a group workout, show a badge:
  "Group workout · 3 members".
- Tap → shows the group workout detail.

### 6.3. Challenge detail

- Group streak indicator: flame + `current_days`, hint if at risk
  ("N members need to check in today").

## 7. Test plan

- `GroupWorkoutDetector` with fixed lat/long:
  - Two users within radius + within window → attached.
  - Same location outside window → not attached.
  - Same time far apart → not attached.
- Multiplier resolver group slot arithmetic + cap.
- `GroupStreakUpdater` on synthetic dataset (yesterday activity).
- Endpoints.
- Mobile: permission prompt logic; badge renders when attached.

## 8. Non-goals

- PostGIS integration (deferred until scale demands).
- Fine-grained privacy on location (MVP: never expose GPS via API).
- Group streak awarding a medal (nice-to-have, later).
