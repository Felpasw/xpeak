# Phase 20 — Trophies (SPEC)

## 1. Data model

### 1.1. `trophies`

| Column                | Type    | Constraints                        |
|-----------------------|---------|------------------------------------|
| `id`                  | uuid    | PK                                 |
| `slug`                | citext  | nullable, unique when present      |
| `name`                | string  | not null                           |
| `description`         | string  | nullable                           |
| `artwork_storage_key` | string  | not null                           |
| `mime_type`           | string  | not null (image/gif/animated webp) |
| `is_system`           | boolean | not null, default `false`          |
| `challenge_id`        | uuid    | FK → challenges, nullable          |
| `created_by`          | uuid    | FK → users, nullable               |
| `inserted_at`/`updated_at` | utc_datetime_usec | not null      |

Seeded: 5 system trophies (`gold-cup`, `silver-cup`, `bronze-cup`,
`fire-blaze`, `iron-crown`).

### 1.2. `user_trophies`

| Column          | Type              | Constraints                     |
|-----------------|-------------------|---------------------------------|
| `id`            | uuid              | PK                              |
| `user_id`       | uuid              | FK → users                      |
| `trophy_id`     | uuid              | FK → trophies                   |
| `challenge_id`  | uuid              | FK → challenges                 |
| `earned_at`     | utc_datetime_usec | not null                        |

Unique index: `(user_id, challenge_id)` — one trophy per user per
challenge.

### 1.3. `challenges` addition

- `custom_trophy_id` (FK → trophies, nullable).

## 2. Trophy sources

At challenge create time, owner picks one of:

1. **Default** — leave `custom_trophy_id` null. Awarder picks the
   appropriate system trophy based on challenge shape.
2. **Curated catalog** — set `custom_trophy_id` to a system trophy
   (`is_system: true`).
3. **Upload** — presign flow (Phase 6 storage) → creates a
   `trophies` row with `is_system: false, challenge_id: <id>,
   created_by: <owner>` → `custom_trophy_id` set to the new id.

## 3. Awarder

`Xpeak.Achievements.TrophyAwarder`:

- Called from `challenge.state → completed` hook in the lifecycle
  job.
- Determines winner(s):
  - Solo hardcore (per `plan.md` §6 Q1) → owner gets a trophy.
  - Group challenge → rank 1 (or top N per Q2) get trophies.
- Inserts `user_trophies` row(s), idempotent.
- Emits `trophy.earned` event.

## 4. Endpoints

- `POST /trophies/upload/presign` — presigned PUT (image/gif) for
  challenge trophy asset.
- `POST /trophies` — create custom trophy row after upload
  (`{ challenge_id, storage_key, name }`).
- `GET /trophies?scope=system` — list system trophies for the
  catalog picker.
- `GET /me/trophies`, `GET /users/:username/trophies` — earned
  lists.

## 5. Mobile

### 5.1. Challenge create wizard trophy step

- Three options: **Default**, **Pick from catalog**, **Upload
  custom**.
- Preview large.
- Storage upload flow reuses Phase 6 UI.

### 5.2. Trophy case on profile

- Third grid in the achievements section (medals / insignias /
  trophies).
- Tap → modal with big trophy art + challenge summary + earn date.

## 6. Test plan

- Awarder:
  - Solo hardcore → trophy for owner.
  - Group challenge #1 → trophy.
  - Missing custom_trophy_id → default system trophy assigned by
    challenge shape.
  - Idempotent.
- Upload flow: presign + row creation.
- Endpoints.
- Mobile: create wizard with all three options; profile trophy case
  renders animated art correctly.

## 7. Non-goals

- Trading trophies.
- Trophy tiers within a single challenge beyond first place (per
  Q2).
- Anti-cheat / moderation of custom artwork.
