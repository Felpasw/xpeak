# Phase 12 — Friends & Social Graph (SPEC)

## 1. Data model

### 1.1. `friendships`

| Column           | Type    | Constraints                                    |
|------------------|---------|------------------------------------------------|
| `id`             | uuid    | PK                                             |
| `user_a_id`      | uuid    | FK → users, not null                           |
| `user_b_id`      | uuid    | FK → users, not null                           |
| `state`          | string  | not null, in `["pending","accepted"]`          |
| `requested_by_id`| uuid    | FK → users, not null                           |
| `accepted_at`    | utc_datetime_usec | nullable                             |
| `inserted_at`/`updated_at` | utc_datetime_usec | not null            |

- Unique index `(user_a_id, user_b_id)`.
- Constraint: `user_a_id < user_b_id` (canonical ordering).
- Constraint: `requested_by_id in (user_a_id, user_b_id)`.

### 1.2. `blocks`

| Column          | Type    | Constraints              |
|-----------------|---------|--------------------------|
| `id`            | uuid    | PK                       |
| `blocker_id`    | uuid    | FK → users, not null     |
| `blocked_id`    | uuid    | FK → users, not null     |
| `inserted_at`   | utc_datetime_usec | not null       |

Unique index `(blocker_id, blocked_id)`.

### 1.3. `users` additions

- `privacy` (JSONB, not null, default per below) with keys:
  - `friend_requests_from`: `"everyone" | "friends_of_friends" | "no_one"`
  - `check_ins_visibility`: `"friends" | "friends_of_friends" | "only_me"`
  - `medals_visibility`: `"public" | "friends" | "only_me"`
  - `show_in_friend_feed`: `boolean`
- Defaults: `everyone`, `friends`, `public`, `true`.

## 2. Modules

### 2.1. `Xpeak.Social`

- `search_users(query)` — by exact `@username` (case-insensitive).
- `send_friend_request(from, to)`.
- `accept_friend_request(user, friendship_id)`.
- `reject_friend_request(user, friendship_id)`.
- `cancel_friend_request(user, friendship_id)`.
- `remove_friend(user, other)`.
- `block(user, other)` — deletes friendship if present, inserts
  `blocks` row.
- `unblock(user, other)`.
- `friends_of(user)` — accepted friendships.
- `friendship_state(user, other)` → `:none | :pending_sent |
  :pending_received | :friends | :blocked_by_me | :blocked_by_them`.

### 2.2. `Xpeak.Social.Visibility` (pure)

```elixir
@spec can_see?(viewer :: User.t() | nil, target :: User.t(), field :: atom(), context :: map()) :: boolean()
```

Fields: `:profile_card`, `:check_ins`, `:medals`,
`:trophy_case`, `:activity_feed`.

## 3. Endpoints

### 3.1. Search

`GET /users/search?q=felpa` → `{ "users": [{ id, username, avatar,
level, current_title, friendship_state }] }`.

### 3.2. Friend requests

- `POST /friend_requests` — `{ target_username }` → 201 with the
  pending friendship.
- `POST /friend_requests/:id/accept` — 200.
- `POST /friend_requests/:id/reject` — 204.
- `DELETE /friend_requests/:id` — cancel (only requester can).
- `GET /friend_requests?direction=incoming|outgoing` — list.

### 3.3. Friends

- `GET /friends` → list of accepted friendships with basic user info.
- `DELETE /friends/:friend_user_id` → remove.

### 3.4. Blocks

- `POST /blocks` `{ target_username }`.
- `DELETE /blocks/:blocked_user_id`.
- `GET /blocks` (self only).

### 3.5. Public profile

`GET /users/:username` → returns the profile card + visible fields
per `Visibility`.

Blocked users get `404` (indistinguishable from "user doesn't
exist").

### 3.6. Privacy settings

- `GET /me/privacy` — returns current.
- `PATCH /me/privacy` — partial update.

## 4. Mobile

### 4.1. Friends tab

- Segmented control: **Friends** | **Requests** | **Blocked**.
- Friends: search bar + list with quick actions.
- Requests: incoming (Accept/Reject) + outgoing (Cancel).
- Blocked: unblock action.

### 4.2. Public profile viewer

- Renders whatever `Visibility` allows.
- Sticky bottom bar with contextual actions:
  - Not friends → **Add friend**.
  - Pending sent → **Cancel request**.
  - Pending received → **Accept** / **Reject**.
  - Friends → **Remove friend** (with confirm) / **Block**.

### 4.3. Search

- Type `@` + username → live search with debouncing.
- Result tile shows current friendship state.

## 5. Test plan

- Schema: canonical ordering constraint, unique index.
- `Social`:
  - Send/accept/reject/cancel flow, including edge cases (already
    friends → 422, already pending → 422).
  - Blocking cascades deletion of friendship.
  - Blocked user can't send request (422).
- `Visibility`:
  - Non-friend sees only public fields.
  - Friend sees per-privacy settings.
  - Blocked sees nothing.
- Endpoints: happy + main failures + blocked-user 404.
- Mobile: segmented tabs render right lists, action buttons trigger
  right mutations.

## 6. Non-goals

- Following (unilateral) — deferred.
- Suggested friends / mutual-friends counter — deferred.
- In-app messaging.
