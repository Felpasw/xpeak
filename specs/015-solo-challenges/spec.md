# Phase 15 — Solo Challenges (SPEC)

## 1. Data model

### 1.1. `challenges`

| Column               | Type              | Constraints                                        |
|----------------------|-------------------|----------------------------------------------------|
| `id`                 | uuid              | PK                                                 |
| `owner_id`           | uuid              | FK → users                                         |
| `title`              | string            | not null                                           |
| `description`        | string            | nullable                                           |
| `mode`               | string            | not null, in `["solo","friend","group"]`           |
| `visibility`         | string            | not null, in `["public","private"]`, default `private` |
| `win_rule`           | jsonb             | not null (see §2)                                  |
| `xp_multiplier`      | decimal           | not null, default `1.0`, check `0.5..1.5`          |
| `starts_at`          | utc_datetime_usec | not null                                           |
| `ends_at`            | utc_datetime_usec | not null                                           |
| `member_cap`         | integer           | nullable                                           |
| `state`              | string            | not null, in `["upcoming","active","completed","failed","cancelled"]` |
| `inserted_at`/`updated_at` | utc_datetime_usec | not null                                |

Indexes: `owner_id`, `state`, `(mode, visibility)`.

### 1.2. `challenge_categories`

| Column         | Type   | Constraints                            |
|----------------|--------|----------------------------------------|
| `id`           | uuid   | PK                                     |
| `challenge_id` | uuid   | FK → challenges, not null              |
| `category_id`  | uuid   | FK → categories, not null              |

Unique index: `(challenge_id, category_id)`.

### 1.3. `challenge_memberships`

| Column         | Type              | Constraints                                     |
|----------------|-------------------|-------------------------------------------------|
| `id`           | uuid              | PK                                              |
| `challenge_id` | uuid              | FK → challenges                                 |
| `user_id`      | uuid              | FK → users                                      |
| `role`         | string            | not null, in `["owner","member"]`               |
| `joined_at`    | utc_datetime_usec | not null                                        |
| `left_at`      | utc_datetime_usec | nullable                                        |
| `progress`     | jsonb             | not null, default `{}` (cached, recomputed on read) |
| `result`       | string            | nullable, in `["completed","failed","cancelled"]` |

Unique index: `(challenge_id, user_id)`.

## 2. Win rule shapes (`jsonb`)

- `{ "kind": "total_xp", "target": 1500 }`
- `{ "kind": "total_check_ins", "target": 30 }`
- `{ "kind": "consecutive_days", "target": 21 }`

Extensible: `kind` acts as a tag, resolver dispatches per kind.

## 3. Modules

### 3.1. `Xpeak.Challenges`

- `create_challenge(user, attrs)` — creates challenge + membership
  (owner) + categories.
- `join(challenge, user)` — solo challenges: only the owner can
  "join" (implicit at create).
- `leave(membership)` — sets `left_at`, `result = "cancelled"`.
- `get_membership(challenge, user)`.
- `active_memberships_for(user, category)` — feeds
  `MultiplierResolver`.
- `progress(membership)` — pure function over check-ins.

### 3.2. `Xpeak.Challenges.ProgressCalculator`

```elixir
@spec compute(Membership.t(), [CheckIn.t()]) :: %{
  value: number(), target: number(), percent: float(),
  achieved?: boolean(), days_left: non_neg_integer()
}
```

Dispatches on `challenge.win_rule.kind`.

### 3.3. `MultiplierResolver.resolve/3` update

- Load `active_memberships_for(user, category)`.
- Combine memberships' `xp_multiplier` (max if multiple? or product?
  — decision: **max**, to avoid stacking exploits).
- Persist which challenge attributed the bonus in the check-in's
  `multiplier_snapshot.challenge_attribution` (list of membership_ids).

## 4. Endpoints

- `GET /challenges?owner=me&state=active` — list of own challenges.
- `GET /challenges/:id` — detail.
- `POST /challenges` — create + auto-join owner.
- `PATCH /challenges/:id` — restricted updates (title, description;
  win rule editable only while `upcoming`).
- `POST /challenges/:id/cancel`.
- `GET /challenges/:id/progress` — current progress for the caller
  (owner in solo).

## 5. Lifecycle

- Oban cron every 15 min:
  - Transition `upcoming` → `active` when `starts_at` reached.
  - Transition `active` → `completed` if `ProgressCalculator`
    reports `achieved?: true`.
  - Transition `active` → `failed` if `ends_at` passed and
    `achieved?: false`.
  - Emit `challenge.finished` event on rollover.

## 6. Mobile

### 6.1. Challenges tab

- Segmented: **Active** | **Upcoming** | **Past**.
- List items: title, category chips, days left / result.

### 6.2. Create wizard

- 1. Title + description.
- 2. Pick categories (multi).
- 3. Win rule (kind picker + target).
- 4. Duration (start/end dates).
- 5. Bonus multiplier slider (1.0–1.5).
- 6. Confirm.

### 6.3. Detail screen

- Progress bar (percent).
- Time remaining.
- Category chips.
- Recent check-ins that count toward the challenge.
- Buttons: **Cancel challenge** (with confirm).

## 7. Test plan

- Schema constraints (multiplier bounds, unique memberships).
- `Challenges.create_challenge/2` — creates challenge + membership +
  categories atomically.
- `ProgressCalculator` — one test module per win rule kind.
- `MultiplierResolver` — challenge bonus applied only for whitelisted
  categories during the window; excluded when membership.left_at set.
- Lifecycle job transitions states correctly at rollover.
- Controllers — CRUD + progress endpoint.
- Mobile — wizard creates challenge; detail screen updates.

## 8. Non-goals

- Multi-member joining logic.
- Public discovery / search (Phase 17).
- Insignia awarded on completion (Phase 18).
