# Phase 16 — Friend Challenges (SPEC)

## 1. Data model

### 1.1. `challenge_invitations`

| Column         | Type              | Constraints                                     |
|----------------|-------------------|-------------------------------------------------|
| `id`           | uuid              | PK                                              |
| `challenge_id` | uuid              | FK → challenges                                 |
| `invited_by`   | uuid              | FK → users                                      |
| `invitee_id`   | uuid              | FK → users (nullable — group phase adds link)   |
| `state`        | string            | not null, in `["pending","accepted","rejected","expired"]` |
| `code`         | string            | nullable, unique (Phase 17 populates for links) |
| `expires_at`   | utc_datetime_usec | not null                                        |
| `inserted_at`/`updated_at` | utc_datetime_usec | not null              |

Indexes: `(challenge_id, invitee_id)`, `(code)` unique when not
null.

## 2. Business rules

- `invited_by` must be a member of the challenge.
- `invitee_id` must be a friend (mutual) of `invited_by`.
- Cannot invite same user twice while a pending invitation exists.
- Accepting → creates a `challenge_memberships` row and updates
  invitation state.

## 3. Context additions

```elixir
def invite(challenge, inviter, invitee_user) do
  # validates friendship + membership + duplicate
  # inserts invitation
  # inserts notification for invitee
  # emits activity event `challenge.invited` (private, self-scope)
end

def accept_invitation(user, invitation_id) do
  # validates invitee_id
  # transitions state
  # inserts membership
  # emits `challenge.joined` event
end

def reject_invitation(user, invitation_id) do
  # transitions to rejected
end
```

## 4. Endpoints

- `POST /challenges/:id/invitations` — `{ invitee_username }`.
- `GET /challenges/:id/invitations?state=pending` — list invitations
  for a challenge (members only).
- `GET /me/challenge_invitations?state=pending` — user's inbound
  invitations.
- `POST /challenge_invitations/:id/accept`.
- `POST /challenge_invitations/:id/reject`.
- `DELETE /challenge_invitations/:id` — cancel (inviter only).

## 5. Mobile

### 5.1. Challenge from a friend's profile

- On the public profile viewer (Phase 12), when friends: button
  **Challenge @user**.
- Opens a shortened wizard (target, categories, dates only —
  visibility fixed to friend/private).
- Creates challenge with owner as the requester, invitee added
  automatically.

### 5.2. Invitations inbox

- Section in the challenges tab: **Invitations**.
- Each invitation: challenge title + inviter + expiration + accept /
  reject.

### 5.3. Leaderboard on the challenge detail

- List of members ranked by their progress (percent for
  target-based; days for streak-based).

## 6. Test plan

- Cannot invite a non-friend → 422.
- Cannot invite same user twice → 422.
- Accept creates membership + emits event.
- Reject transitions state, no membership.
- Expiration Oban job flips pending to expired.
- Leaderboard sort is stable across ties.
- Mobile: profile → challenge wizard → invitation appears on
  invitee's account.

## 7. Non-goals

- Public discovery.
- Share links (Phase 17).
- Auto-suggest opponents.
