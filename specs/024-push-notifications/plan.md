# Phase 24 — Push Notifications

> Status: **planning only**.

## 1. Goal

Deliver push notifications to the mobile app for events already
being written to the `notifications` table (Phase 13). Cover
streak-at-risk, group activity, level up, achievement earned,
friend request, and recap ready.

## 2. Scope

**In scope**
- Provider integration (FCM + APNs via Capacitor Push, or OneSignal
  — decision pending).
- Device token registration and lifecycle (register / rotate /
  revoke on logout).
- Notification dispatcher: reads new `notifications` rows and sends
  push accordingly.
- Preferences per notification category (opt-out toggles).
- Streak-at-risk detector: daily Oban job that finds users at risk
  of breaking their streak.
- Deep-link handling on tap.

**Out of scope**
- Web push (Phase 25 for PWA).
- SMS / email.
- In-app banners (already covered by notification bell).

## 3. Approach

- **Recommendation:** FCM + APNs via Capacitor's official
  `@capacitor/push-notifications` plugin. Simpler than OneSignal
  once you already have the notifications table.
- Device tokens live on a `device_tokens` table:
  `user_id, token, platform (ios|android|web), locale, app_version,
  last_seen_at`.
- Dispatcher: `NotificationDispatcher` GenServer subscribed to a
  Phoenix.PubSub topic. When a new notification is inserted, publish
  → dispatcher processes → sends via provider adapter.
- Provider adapter: behaviour + FCM adapter + APNs adapter + mock.

## 4. Artifacts

- Migrations: `create_device_tokens.exs`,
  `add_notification_preferences_to_users.exs`.
- `lib/xpeak/push.ex` — context.
- `lib/xpeak/push/dispatcher.ex` — dispatch orchestration.
- `lib/xpeak/push/{fcm_adapter,apns_adapter,mock_adapter}.ex`.
- `lib/xpeak/push/streak_at_risk_job.ex` (Oban cron 20:00 local).
- Endpoints for token registration, preferences.
- Mobile: request permission on first login; register token;
  handle background/foreground events; deep-link routing.

## 5. Dependencies

- Blocked by: Phase 13 (notifications table).
- Blocks: —

## 6. Open questions

1. **Provider** — Capacitor Push (FCM + APNs) or OneSignal wrapper?
2. **Quiet hours** — respect a per-user quiet window?
3. **Bundling** — collapse multiple events into a single notification
   ("5 friends checked in today")?
4. **A/B copy** — need infrastructure for testing message text? MVP
   probably not.

## 7. Success criteria

- On first login, mobile requests push permission and registers
  device token.
- New friend request → push arrives with correct copy + deep link.
- Streak at 24h to lapse → push arrives at 20:00 local.
- Level-up → push arrives.
- Recap ready → push arrives with deep link to the recap.
- Opting out of a category stops that category's pushes but not
  others.
- Logout revokes the device token.
- CI green.
