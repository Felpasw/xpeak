# Phase 8 — Themed Titles & Themes (SPEC)

## 1. Data model

### 1.1. `themes` table

| Column      | Type    | Constraints                        |
|-------------|---------|------------------------------------|
| `id`        | uuid    | PK                                 |
| `slug`      | citext  | not null, unique                   |
| `name`      | string  | not null                           |
| `description` | string | nullable                          |
| `is_system` | boolean | not null, default `false`          |
| `active`    | boolean | not null, default `true`           |
| `inserted_at`/`updated_at` | utc_datetime_usec | not null |

### 1.2. `level_title_tiers` table

| Column        | Type    | Constraints                        |
|---------------|---------|------------------------------------|
| `id`          | uuid    | PK                                 |
| `theme_id`    | uuid    | FK → `themes.id`, not null         |
| `min_level`   | integer | not null                           |
| `max_level`   | integer | nullable (terminal tier)           |
| `title`       | string  | not null                           |
| `position`    | integer | not null                           |

Indexes: `(theme_id, position)`, `(theme_id, min_level)`.

Constraint: within a theme, ranges must not overlap. Enforced by
migration + changeset guard.

### 1.3. `users` addition

- `theme_id` (FK → themes, nullable, defaults to the seeded default
  theme on user creation via `Repo.insert` hook or `Accounts.register_user/1`).

### 1.4. Seeds

**Medieval** (default):
- 1–9 "Apprentice"
- 10–24 "Warrior"
- 25–49 "Knight"
- 50–99 "Mage"
- 100+ "War God"

**Sci-Fi**:
- 1–9 "Rookie"
- 10–24 "Trooper"
- 25–49 "Officer"
- 50–99 "Commander"
- 100+ "Overlord"

## 2. Context

`Xpeak.Titles`:

- `list_themes/0`.
- `get_theme_by_slug/1`.
- `resolve(user_or_level, theme)` — returns
  `{ current: %{title, min_level, max_level}, next: %{title,
  min_level} | nil, progress_to_next: 0.0..1.0 }`.

## 3. Endpoints

### 3.1. `GET /me` addition

```json
"current_title": { "title": "Warrior", "min_level": 10, "max_level": 24 },
"next_title": { "title": "Knight", "min_level": 25 },
"progress_to_next": 0.37
```

### 3.2. `PATCH /me/theme`

Request: `{ "theme_id": "uuid" }` or `{ "theme_slug": "sci-fi" }`.
Response `200`: updated `/me` payload.

Failures: `404` (unknown theme), `422` (inactive theme).

### 3.3. `GET /themes`

Public read-only list of active themes with their tiers (for the
mobile picker).

## 4. Mobile

### 4.1. Profile card

- Shows `@username · Warrior · Level 17`.
- Progress bar: level progress + tier progress (two stacked bars or
  one).

### 4.2. Theme picker (settings)

- List of themes with a preview of the tier names.
- Radio-style selection; confirming calls `PATCH /me/theme`.

## 5. Test plan

- Migration constraint: overlapping ranges rejected.
- `Titles.resolve/2`:
  - Level 0 → first tier, progress 0.0.
  - Level in the middle of a range → correct tier, progress
    calculated.
  - Level above terminal tier → terminal tier, next = nil, progress
    = 1.0.
- `GET /themes` returns all seeded themes with tiers.
- `PATCH /me/theme` updates `current_title` shape.
- Mobile picker renders themes; selecting one persists.

## 6. Non-goals

- Custom user themes.
- Themed asset packs (forms/illustrations — Phase 22).
- Runtime tier editing (Phase 9).
