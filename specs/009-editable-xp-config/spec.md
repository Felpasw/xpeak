# Phase 9 — Editable XP & Level Configuration (SPEC)

## 1. Data model

### 1.1. `settings`

| Column        | Type   | Constraints                                |
|---------------|--------|--------------------------------------------|
| `id`          | uuid   | PK                                         |
| `key`         | string | not null, unique                           |
| `kind`        | string | not null, in `["number","string","json"]`  |
| `value`       | jsonb  | not null                                   |
| `min`         | jsonb  | nullable (for numbers)                     |
| `max`         | jsonb  | nullable (for numbers)                     |
| `description` | string | nullable                                   |
| `updated_at`  | utc_datetime_usec | not null                        |

Seeded keys: `multiplier.cap` (3.0), `level_curve.base` (100),
`level_curve.exponent` (1.5).

### 1.2. `streak_tiers`

| Column          | Type    | Constraints                    |
|-----------------|---------|--------------------------------|
| `id`            | uuid    | PK                             |
| `min_days`      | integer | not null                       |
| `max_days`      | integer | nullable                       |
| `multiplier`    | decimal | not null                       |
| `position`      | integer | not null                       |

Seeded with the Phase 7 defaults.

### 1.3. `admin_events`

| Column        | Type    | Constraints                       |
|---------------|---------|-----------------------------------|
| `id`          | uuid    | PK                                |
| `actor_id`    | uuid    | FK → `users.id`                   |
| `action`      | string  | not null (e.g., `settings.updated`) |
| `subject_type`| string  | not null                          |
| `subject_id`  | uuid    | not null                          |
| `diff`        | jsonb   | not null (`{ before, after }`)    |
| `inserted_at` | utc_datetime_usec | not null                |

### 1.4. `users.role`

- Column `role` (string, not null, default `"user"`, in
  `["user","admin"]`).

## 2. Modules

### 2.1. `Xpeak.Config`

```elixir
def get_number(key), do: ...
def get_string(key), do: ...
def get_json(key), do: ...
def put(key, value, actor), do: ...  # writes + audit + PubSub
```

- ETS table `:xpeak_config_cache`.
- `Xpeak.Config.CacheWarmer` GenServer starts on app boot, loads
  from DB, subscribes to `"config:updated"` PubSub topic.

### 2.2. Updates to Phase 7 modules

- `StreakTier.multiplier_for/1` — reads from `streak_tiers` table
  cached in ETS.
- `MultiplierResolver.@cap` → `Config.get_number("multiplier.cap")`.
- `LevelCurve.xp_for_level/1` reads `base` and `exponent` from
  `Config`.

## 3. Admin surface

### 3.1. Routing

- `/admin` — Kaffy or a plain LiveView index.
- Protected by `XpeakWeb.Plugs.RequireAdmin` (loads user via
  Guardian pipeline, checks `user.role == "admin"`, else 403).

### 3.2. Screens

- Settings: table with editable rows (respect `min`/`max`).
- Streak tiers: table with add/remove/reorder.
- Categories: CRUD.
- Audit log: read-only, filter by actor and date.

## 4. Bootstrap

- Mix task: `mix xpeak.grant_admin <email>`.
- Or env: `INITIAL_ADMIN_EMAIL` — on app boot, if set and user
  exists, grant admin (idempotent).

## 5. Test plan

- `Xpeak.Config`:
  - Round-trip get/put.
  - Cache invalidation on PubSub event.
  - Min/max enforced.
- `StreakTier` re-read from DB after `Config.put` on tier row.
- Admin plug: 403 for non-admin, 200 for admin.
- Audit log: every write produces a row with the diff.

## 6. Non-goals

- Multi-tenant admin.
- Fine-grained permission per setting (single admin role).
- API for third-party integrations to write settings.
