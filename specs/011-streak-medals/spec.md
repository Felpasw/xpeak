# Phase 11 — Streak Medals (SPEC)

## 1. Data model

### 1.1. `medals`

| Column                  | Type    | Constraints                        |
|-------------------------|---------|------------------------------------|
| `id`                    | uuid    | PK                                 |
| `slug`                  | citext  | not null, unique                   |
| `name`                  | string  | not null                           |
| `description`           | string  | nullable                           |
| `streak_days_required`  | integer | not null, check `> 0`              |
| `artwork_storage_key`   | string  | not null                           |
| `is_system`             | boolean | not null, default `true`           |
| `active`                | boolean | not null, default `true`           |

Seeded: `streak-7`, `streak-30`, `streak-100`, `streak-365`.

### 1.2. `user_medals`

| Column        | Type    | Constraints                                     |
|---------------|---------|-------------------------------------------------|
| `id`          | uuid    | PK                                              |
| `user_id`     | uuid    | FK → `users.id`, not null                       |
| `medal_id`    | uuid    | FK → `medals.id`, not null                      |
| `earned_at`   | utc_datetime_usec | not null                              |

Unique index: `(user_id, medal_id)` — a medal is earned at most
once per user.

## 2. Modules

### 2.1. `Xpeak.Achievements.MedalAwarder`

```elixir
@spec check_streak_medals(User.t()) :: {:ok, [UserMedal.t()]}
def check_streak_medals(user) do
  eligible = list_medals_by_max_streak(user.current_streak_days)
  already_earned = Repo.all(...)  # user's existing medal_ids
  to_award = Enum.reject(eligible, & &1.id in already_earned)

  Enum.map(to_award, &insert_user_medal(user, &1))
end
```

Called inside `create_check_in/2`'s `Ecto.Multi`.

## 3. Endpoints

### 3.1. `GET /me/medals`

Auth required. Response:
```json
{ "medals": [
    { "slug": "streak-7", "name": "Week Warrior",
      "artwork_url": "...", "earned_at": "..." }
  ]
}
```

### 3.2. `GET /users/:username/medals`

Public (subject to Phase 12 privacy). Same shape.

## 4. Mobile

### 4.1. Profile medals section

- Grid of earned medals (default 6 columns, wraps).
- Tap a medal → modal with name, description, `earned_at`, artwork
  large.
- Empty state: "No medals yet — keep training."

### 4.2. Newly-earned notification

- After a check-in that awards a medal, the level-up overlay (Phase
  5) also shows the new medal.
- Simple stack: level-up card → medal card → dismiss.

## 5. Test plan

- Migration constraints (unique idx).
- `MedalAwarder`:
  - New award on crossing threshold.
  - Idempotent: streak resets to 0 and hits 7 again → not awarded
    twice.
  - Multiple medals at once (e.g., first check-in from a user who
    somehow arrived at day 30 — shouldn't happen normally).
- Endpoints: correct listing, respect ownership.
- Mobile: grid renders, tap opens modal, empty state.

## 6. Non-goals

- Custom medal artwork.
- Non-streak medals (e.g., "100 check-ins" — could be added later
  as data if we generalize).
- Sharing medals externally (Phase 21 recap covers that).
