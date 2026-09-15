# Phase 5 — Check-in (No Media) (SPEC)

## 1. Summary

First end-to-end write path: check-in → XP → level/streak update.
No media, no multipliers beyond category weight.

## 2. Data model

### 2.1. `check_ins` table

| Column                | Type              | Constraints                    |
|-----------------------|-------------------|--------------------------------|
| `id`                  | uuid              | PK                             |
| `user_id`             | uuid              | FK → `users.id`, not null      |
| `category_id`         | uuid              | FK → `categories.id`, not null |
| `performed_at`        | utc_datetime_usec | not null, default `now()`      |
| `duration_minutes`    | integer           | nullable, check `> 0`          |
| `notes`               | string            | nullable, max 280 chars        |
| `xp_earned`           | integer           | not null, check `> 0`          |
| `inserted_at`/`updated_at` | utc_datetime_usec | not null              |

Indexes: `(user_id, performed_at DESC)`, `(user_id, category_id)`.

### 2.2. `users` addition

- Add `time_zone` (string, default `"UTC"`).
- Add `last_check_in_on` (date, nullable) — cached day of the last
  check-in for cheap streak math.

## 3. Endpoints

### 3.1. `POST /check_ins`

Auth required.

Request:
```json
{ "category_slug": "legs", "duration_minutes": 45, "notes": "leg day 🔥" }
```

Response `201`:
```json
{
  "check_in": {
    "id": "uuid",
    "category": { "slug": "legs", "name": "Legs", "icon": "🦵" },
    "performed_at": "2026-01-01T12:34:56.000000Z",
    "duration_minutes": 45,
    "notes": "leg day 🔥",
    "xp_earned": 21
  },
  "user": {
    "id": "uuid",
    "level": 1,
    "xp": 21,
    "current_streak_days": 1,
    "longest_streak_days": 1,
    "leveled_up": true,
    "levels_gained": 1
  }
}
```

Failures:
- `401` — missing/invalid token.
- `422` — unknown category, invalid duration, notes too long, or
  same-day cap exceeded (see `plan.md` §6 Q1).

### 3.2. `GET /check_ins?limit=20&cursor=<opaque>`

Auth required. Returns the current user's check-ins, most recent
first.

Response `200`:
```json
{
  "check_ins": [ /* ... */ ],
  "next_cursor": "opaque-string-or-null"
}
```

## 4. Domain logic

### 4.1. `Xpeak.CheckIns.create_check_in/2`

```elixir
def create_check_in(user, attrs) do
  Ecto.Multi.new()
  |> Multi.run(:category, fn _repo, _ -> fetch_category(attrs) end)
  |> Multi.insert(:check_in, fn %{category: c} ->
       CheckIn.changeset(%CheckIn{}, user, c, attrs)
     end)
  |> Multi.update(:user, fn %{check_in: ci, category: _} ->
       User.progression_changeset(user, %{
         xp: user.xp + ci.xp_earned,
         level: LevelUpService.level_for_xp(user.xp + ci.xp_earned),
         current_streak_days: StreakCalculator.next_current(user, ci),
         longest_streak_days: StreakCalculator.next_longest(user, ci),
         last_check_in_on: DateTime.to_date(ci.performed_at)
       })
     end)
  |> Repo.transaction()
end
```

### 4.2. `Xpeak.CheckIns.StreakCalculator` (pure)

```elixir
@spec next_current(User.t(), CheckIn.t()) :: pos_integer()
def next_current(%User{last_check_in_on: nil}, _), do: 1
def next_current(%User{last_check_in_on: last, current_streak_days: n}, ci) do
  today = DateTime.to_date(ci.performed_at)
  case Date.diff(today, last) do
    0 -> n            # same day, no change
    1 -> n + 1        # consecutive
    _ -> 1            # gap → reset
  end
end
```

`next_longest/2` returns `max(user.longest_streak_days, next_current(...))`.

## 5. Mobile

### 5.1. `/check-in` screen

- Header: "New check-in".
- Category picker (grid of icons, tap to select).
- Optional duration slider (0–180 min).
- Optional notes input (280 char cap).
- "Log check-in" button — disabled until a category is picked.
- Submit → `POST /check_ins` → success toast/animation → redirect
  to `/profile`.

### 5.2. Profile update

- `useMe` cache invalidated on successful check-in.
- If `user.leveled_up === true` in the response, show a level-up
  overlay (simple: `"You're now level N"` + confetti or a small
  animation).

## 6. Test plan

### API
- Schema: xp_earned check, notes length, FK integrity.
- Context:
  - Happy path: creates check-in, bumps `xp`, updates streak (1).
  - Consecutive-day: bumps streak to 2.
  - Gap-day: resets to 1.
  - Same-day: (per Q1 decision) either allowed or 422.
  - Level up: crossing a level boundary sets `leveled_up: true`.
- Controller:
  - `POST /check_ins` happy, 401 without token, 422 unknown slug.
  - `GET /check_ins` pagination cursor round-trip.

### Mobile (Vitest)
- Check-in screen renders categories from mocked API.
- Submitting without a category is blocked.
- On 201, navigates to `/profile` and shows level-up overlay when
  applicable.

## 7. Non-goals

- Media uploads.
- Undo / edit / delete check-in (future).
- Timezone-aware streak math beyond UTC.
