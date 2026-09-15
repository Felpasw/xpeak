# Phase 24 — Push Notifications (TASKS)

## Legend
`[T]` TDD, `[S]` sequential, `[P]` parallel, `[HUMAN]` human-only.

---

## Section A — Provider prereqs (human)

- [ ] **T-024-01 `[HUMAN][S]`** — Provider decision + credentials
  - Per `plan.md` §6 Q1.
  - FCM project + APNs auth key configured.
  - Paste secrets into local `.env` (never committed).

## Section B — Schema

- [ ] **T-024-02 `[T][S]`** — device_tokens migration/schema
  - Green.

- [ ] **T-024-03 `[T][S]`** — notification_preferences JSONB
  - Green.

## Section C — Provider adapters

- [ ] **T-024-04 `[T][S]`** — `Push.Provider` behaviour + `MockAdapter`
  - Failing tests.
  - Green.

- [ ] **T-024-05 `[T][S]`** — `FcmAdapter`
  - Failing tests with Bypass.
  - Green.

- [ ] **T-024-06 `[T][S]`** — `ApnsAdapter`
  - Failing tests with Bypass.
  - Green.

## Section D — Dispatcher

- [ ] **T-024-07 `[T][S]`** — `Notifications.create/1` emits
      PubSub event
  - Failing test.
  - Green.

- [ ] **T-024-08 `[T][S]`** — `Push.Dispatcher` GenServer
  - Failing tests: routes by platform, respects preferences, deletes
    invalid tokens.
  - Add to supervision tree.
  - Green.

## Section E — Jobs

- [ ] **T-024-09 `[T][S]`** — `StreakAtRiskJob` (Oban cron)
  - Failing tests: finds at-risk users, inserts notifications.
  - Green.

## Section F — HTTP surface

- [ ] **T-024-10 `[T][S]`** — Device tokens endpoints
  - `POST /device_tokens`, `DELETE /device_tokens/:id`.
  - Failing tests.
  - Green.

- [ ] **T-024-11 `[T][S]`** — Preferences endpoints
  - `GET/PATCH /me/notification_preferences`.
  - Failing tests.
  - Green.

## Section G — Mobile

- [ ] **T-024-12 `[T][S]`** — Permission + registration flow
  - Failing spec.
  - Wire Capacitor Push plugin.
  - Green.

- [ ] **T-024-13 `[T][S]`** — Foreground/background handlers
  - Failing spec: foreground → badge only; background → tray tap
    deep-links.
  - Green.

- [ ] **T-024-14 `[T][S]`** — Notification preferences screen
  - Failing spec: toggles persist.
  - Green.

## Section H — Wrap-up

- [ ] **T-024-15 `[P]`** — ADR `0024-push-notifications.md`.
- [ ] **T-024-16 `[S]`** — Phase close.

---

## Bundling strategy for PRs

1. `feat(push): device tokens + preferences schema` — B
2. `feat(push): provider behaviour + mock` — C1
3. `feat(push): fcm + apns adapters` — C2 + C3
4. `feat(push): dispatcher + pubsub wiring` — D
5. `feat(push): streak-at-risk job` — E
6. `feat(push): device tokens + preferences endpoints` — F
7. `feat(mobile): permission + tokens + handlers + preferences` — G
8. `docs(adr): push notifications` — H
