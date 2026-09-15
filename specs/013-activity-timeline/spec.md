# Phase 13 — Activity Timeline / Feed (SPEC)

## 1. Data model

### 1.1. `activity_events`

| Column          | Type              | Constraints                        |
|-----------------|-------------------|------------------------------------|
| `id`            | uuid              | PK                                 |
| `user_id`       | uuid              | FK → users, not null (event actor) |
| `kind`          | string            | not null                           |
| `version`       | integer           | not null, default `1`              |
| `subject_type`  | string            | nullable                           |
| `subject_id`    | uuid              | nullable                           |
| `payload`       | jsonb             | not null, default `{}`             |
| `visibility`    | string            | not null, in `["public","friends","private"]` |
| `created_at`    | utc_datetime_usec | not null                           |

Indexes: `(user_id, created_at DESC)`, `(kind, created_at DESC)`.

### 1.2. `notifications`

| Column          | Type              | Constraints                        |
|-----------------|-------------------|------------------------------------|
| `id`            | uuid              | PK                                 |
| `user_id`       | uuid              | FK → users, not null (recipient)   |
| `kind`          | string            | not null                           |
| `payload`       | jsonb             | not null, default `{}`             |
| `read_at`       | utc_datetime_usec | nullable                           |
| `created_at`    | utc_datetime_usec | not null                           |

Index: `(user_id, read_at NULLS FIRST, created_at DESC)`.

## 2. Event kinds (v1 payloads)

- `check_in.posted`: `{ check_in_id, category_slug, xp_earned,
  media_count }`
- `level.up`: `{ new_level, title }`
- `medal.earned`: `{ medal_slug, medal_name }`
- `insignia.earned`: (Phase 17) `{ insignia_slug, challenge_id }`
- `trophy.earned`: (Phase 20) `{ trophy_id, challenge_id }`
- `challenge.started`: (Phase 15+) `{ challenge_id, title }`
- `challenge.finished`: (Phase 15+) `{ challenge_id, result }`
- `friend.accepted`: `{ friend_user_id }` — muted from feed by
  default, still recorded.

## 3. Emitter

```elixir
defmodule Xpeak.Feed.Emitter do
  def emit(user, kind, opts \\ []) do
    %ActivityEvent{
      user_id: user.id,
      kind: kind,
      subject_type: opts[:subject_type],
      subject_id: opts[:subject_id],
      payload: opts[:payload] || %{},
      visibility: opts[:visibility] || "friends"
    }
    |> Repo.insert()
  end
end
```

Called from `Ecto.Multi` in the writing context (transaction-safe).

## 4. Feed context

```elixir
defmodule Xpeak.Feed do
  def personal(user, opts), do: ...        # own events
  def friends_feed(user, opts), do: ...    # own + friends'
  def notifications(user, opts), do: ...
  def mark_notification_read(user, id), do: ...
end
```

Friends feed query (simplified):

```sql
SELECT * FROM activity_events e
WHERE e.user_id IN (
    SELECT :me
    UNION
    SELECT other_user_id FROM friendships_view WHERE user_id = :me
  )
  AND NOT EXISTS (
    SELECT 1 FROM blocks b WHERE
      (b.blocker_id = :me AND b.blocked_id = e.user_id) OR
      (b.blocker_id = e.user_id AND b.blocked_id = :me)
  )
  AND (e.visibility = 'public' OR e.visibility = 'friends')
  AND e.created_at < :cursor
ORDER BY e.created_at DESC
LIMIT :limit;
```

`friendships_view` is a small view that returns both sides for
accepted friendships.

## 5. Endpoints

- `GET /feed?scope=me|friends&cursor=&limit=` → `{ events,
  next_cursor }`.
- `GET /notifications?unread_only=` → `{ notifications, unread_count
  }`.
- `POST /notifications/:id/read` → 204.
- `POST /notifications/read_all` → 204.

## 6. Mobile

### 6.1. Feed tab

- Bottom-nav tab or top-level tab in home.
- Segmented control: **Following** (friends feed) | **You**
  (personal).
- Infinite scroll with cursor pagination.
- Empty state: "Add friends to see their check-ins" (with a link to
  search).

### 6.2. Event card renderers

One React component per kind (or a switch), matching payload:

- `check_in.posted` — media grid (1–4 previews), category chip, xp.
- `level.up` — big title change graphic.
- `medal.earned` — medal artwork + name.
- `insignia.earned` / `trophy.earned` — icon + challenge link.
- `friend.accepted` — small line "You and @x are now friends"
  (self-scope only).

### 6.3. Notification bell

- Icon with unread badge count.
- Tap → notifications list.
- Tap an item → deep-link into the referenced object; mark as read.

## 7. Test plan

- Emitter inserts row with proper defaults.
- Feed queries respect friendship + block matrix.
- Cursor pagination is stable.
- Notifications:
  - Friend request received → notification.
  - Friend accepted → notification (for the requester).
- Mobile:
  - Renderers per kind.
  - Empty state.
  - Bell badge updates from `unread_count`.

## 8. Non-goals

- Reactions, comments, sharing.
- Ranking / smart feed.
- Push delivery.
