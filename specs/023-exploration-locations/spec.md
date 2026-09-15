# Phase 23 — Exploration / Locations (SPEC)

## 1. Data model

### 1.1. `locations`

| Column          | Type    | Constraints                        |
|-----------------|---------|------------------------------------|
| `id`            | uuid    | PK                                 |
| `name`          | string  | not null                           |
| `kind`          | string  | nullable (e.g., "gym", "park",
                              "track", "studio")                   |
| `lat`           | float   | not null                           |
| `lng`           | float   | not null                           |
| `region_slug`   | string  | nullable (city or region key)      |
| `country_code`  | string  | nullable                           |
| `created_by`    | uuid    | FK → users                         |
| `verified`      | boolean | not null, default `false`          |
| `inserted_at`/`updated_at` | utc_datetime_usec | not null |

Index on `(lat, lng)` for bounding-box queries.

### 1.2. `user_visited_locations`

| Column            | Type              | Constraints                       |
|-------------------|-------------------|-----------------------------------|
| `id`              | uuid              | PK                                |
| `user_id`         | uuid              | FK → users                        |
| `location_id`     | uuid              | FK → locations                    |
| `first_visit_at`  | utc_datetime_usec | not null                          |
| `visit_count`     | integer           | not null, default `1`             |
| `last_visit_at`   | utc_datetime_usec | not null                          |

Unique index: `(user_id, location_id)`.

### 1.3. `check_ins` addition

- `location_id` (FK → locations, nullable).

### 1.4. `users` addition

- `home_region_slug` (string, nullable) — derived from most-frequent
  check-in region, or user-set.

## 2. Location matcher

```elixir
defmodule Xpeak.Exploration.LocationMatcher do
  @radius_m 50

  def match_or_prompt(lat, lng) do
    candidates = locations_within(lat, lng, @radius_m)
    case candidates do
      [] -> {:none}
      [one] -> {:found, one}
      many -> {:multiple, many}   # let the user pick
    end
  end
end
```

Called by the mobile client before submitting a check-in — it
either passes `location_id` or triggers the "new location" flow.

## 3. First-visit bonus

- `MultiplierResolver.resolve/3` gains awareness of "is this user's
  first-ever visit to this location".
- Bonus: 1.25× (adds to product; cap still applies).
- Applies only once per user × location.

## 4. Regional leaderboard scope

- Extend Phase 14 scopes with `xp_all_time:region:<slug>` and
  `xp_weekly:region:<slug>`.
- Requires `home_region_slug` populated (auto-derive from majority
  of visited locations).

## 5. Endpoints

- `GET /locations?lat=&lng=&radius=` — search nearby.
- `POST /locations` — create new.
- `GET /me/visited_locations` — list.
- `GET /users/:username/visited_locations` — public (per Phase 12
  visibility).
- `GET /locations/:id/challenges` — challenges tied to this
  location.
- `GET /rankings/region/:slug/xp_all_time` — regional board.

## 6. Location-tied challenges

- New optional field on challenges: `location_id`.
- Only check-ins at that location count toward this challenge's
  progress.
- Enforcement in `ProgressCalculator` and `MultiplierResolver`.

## 7. Mobile

### 7.1. Check-in — location step

- If GPS provided and a nearby location exists → pre-fills and
  shows a small "at <name>" chip.
- If none → prompt "This is a new place. Save it?" with name +
  kind picker.

### 7.2. Visited locations screen

- List or map view.
- Each row: location name, first visit, times visited, badge if
  it's been visited N times.

### 7.3. Home region banner

- "You've been visiting <city>" hint if we can derive it.

## 8. Test plan

- Matcher within/outside radius.
- First-visit bonus applied once.
- Regional scope in rankings works.
- Location-tied challenge only counts matching check-ins.
- Mobile flows.

## 9. Non-goals

- Gym-owner accounts.
- Import from Google Places.
- Crowd density.
