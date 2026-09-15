# Phase 18 — Insignias for Challenge Completion (SPEC)

## 1. Data model

### 1.1. `insignias`

| Column              | Type    | Constraints                        |
|---------------------|---------|------------------------------------|
| `id`                | uuid    | PK                                 |
| `slug`              | citext  | not null, unique                   |
| `name`              | string  | not null                           |
| `description`       | string  | nullable                           |
| `tier`              | string  | not null, in `["gold","silver","bronze","participant","effort"]` |
| `artwork_storage_key` | string | not null                          |
| `is_system`         | boolean | not null, default `true`           |

Seed: 5 rows (`challenge-gold`, `challenge-silver`,
`challenge-bronze`, `challenge-participant`, `challenge-effort`).

### 1.2. `user_insignias`

| Column          | Type              | Constraints                         |
|-----------------|-------------------|-------------------------------------|
| `id`            | uuid              | PK                                  |
| `user_id`       | uuid              | FK → users                          |
| `insignia_id`   | uuid              | FK → insignias                      |
| `challenge_id`  | uuid              | FK → challenges                     |
| `earned_at`     | utc_datetime_usec | not null                            |

Unique index: `(user_id, challenge_id)` — one insignia per user per
challenge.

## 2. Awarder

`Xpeak.Achievements.InsigniaAwarder`:

```elixir
def on_challenge_finished(challenge) do
  members = list_memberships(challenge, %{state: :finished})
  ranked = rank_by_finish(members)

  ranked
  |> Enum.with_index(1)
  |> Enum.map(fn {membership, rank} ->
       tier = tier_for(rank, membership.result, membership.progress)
       insert_user_insignia(membership.user_id, challenge.id, tier)
     end)
end
```

Ranking rule per win kind:

- `total_xp`: highest xp first.
- `total_check_ins`: most check-ins first.
- `consecutive_days`: longest streak first, tie broken by earliest
  `finished_at`.

Tier assignment:

- Rank 1 → gold
- Rank 2 → silver
- Rank 3 → bronze
- Achieved but not podium → participant
- Not achieved but ≥ threshold (see `plan.md` §6 Q1) → effort

## 3. Endpoints

- `GET /users/:username/insignias` — public per Phase 12 visibility.
- `GET /me/insignias` — self.

## 4. Mobile

- Profile "Achievements" section combines medals + insignias +
  (eventually) trophies.
- Insignia card tap → modal with challenge summary + tier.

## 5. Test plan

- Ranking correctness per win kind.
- Tie-breaking.
- Idempotency (running the awarder twice doesn't duplicate).
- Feed event and notification emitted.
- Mobile: insignias section renders.

## 6. Non-goals

- Custom artwork per challenge (Phase 20 for trophies covers custom
  art; insignias stay system-only).
- Skipping tiers (e.g., no gold if <3 finishers).
