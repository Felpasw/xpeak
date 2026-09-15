# Phase 24 — Push Notifications (SPEC)

## 1. Data model

### 1.1. `device_tokens`

| Column          | Type    | Constraints                        |
|-----------------|---------|------------------------------------|
| `id`            | uuid    | PK                                 |
| `user_id`       | uuid    | FK → users                         |
| `token`         | string  | not null, unique                   |
| `platform`      | string  | not null, in `["ios","android","web"]` |
| `locale`        | string  | nullable                           |
| `app_version`   | string  | nullable                           |
| `last_seen_at`  | utc_datetime_usec | not null                 |
| `inserted_at`/`updated_at` | utc_datetime_usec | not null |

### 1.2. `users.notification_preferences` (JSONB)

Keys → boolean (default `true`):

- `friend_requests`
- `friend_activity`
- `level_up`
- `achievements`
- `streak_at_risk`
- `challenge_updates`
- `recap_ready`

## 2. Provider layer

`Xpeak.Push.Provider` behaviour:

```elixir
@callback send(token :: String.t(), payload :: map(), opts :: keyword()) ::
  :ok | {:error, term()}
```

Adapters:
- `FcmAdapter` — Android + web push.
- `ApnsAdapter` — iOS.
- `MockAdapter` — tests.

Config selects adapter per platform.

## 3. Dispatcher

```elixir
defmodule Xpeak.Push.Dispatcher do
  use GenServer

  def start_link(_), do: GenServer.start_link(__MODULE__, nil, name: __MODULE__)

  def init(_) do
    Phoenix.PubSub.subscribe(Xpeak.PubSub, "notifications:new")
    {:ok, nil}
  end

  def handle_info({:notification_created, notification_id}, state) do
    dispatch(notification_id)
    {:noreply, state}
  end
end
```

Called from `Notifications.create/1` (which now emits the PubSub
event).

Dispatch logic:
1. Load notification + user + preferences.
2. If the category is disabled → skip.
3. Load user's device tokens.
4. For each token, build payload (title, body, deep-link, image).
5. Send via appropriate provider adapter.
6. On invalid token → delete token row.

## 4. Streak-at-risk job

`Xpeak.Push.StreakAtRiskJob` (Oban cron):

- Runs daily at 20:00 local (MVP: 20:00 UTC).
- Finds users with `current_streak_days ≥ 3` AND no check-in today.
- Inserts a `notification` (`kind: "streak_at_risk"`) which
  triggers the dispatcher automatically.

## 5. Endpoints

- `POST /device_tokens` — `{ token, platform, app_version, locale
  }` → 201.
- `DELETE /device_tokens/:id` — revoke.
- `GET /me/notification_preferences`.
- `PATCH /me/notification_preferences`.

## 6. Mobile

### 6.1. Permission + registration

- On first login: request push permission.
- On grant: fetch token via Capacitor Push plugin → `POST /device_tokens`.
- On token refresh: repeat.
- On logout: `DELETE /device_tokens/:id`.

### 6.2. Handling incoming pushes

- Foreground: quiet update to the notification bell badge, optional
  inline toast.
- Background: system tray. On tap → app opens to the deep link.

### 6.3. Notification preferences screen

- Toggles for each category from `spec.md` §1.2.
- Persist via `PATCH /me/notification_preferences`.

## 7. Test plan

- `Dispatcher`:
  - Sends via correct provider per platform.
  - Skips when preference disabled.
  - Deletes invalid tokens.
- `StreakAtRiskJob` on synthetic dataset.
- Endpoints: register/revoke/preferences.
- Mobile: permission flow, token registration, deep-link nav.
- MockAdapter used across tests; real adapters covered by integration
  tests behind an env flag.

## 8. Non-goals

- Rich media notifications (images, action buttons — nice-to-have).
- Per-region quiet hours.
- Notification analytics (open rate, etc.).
