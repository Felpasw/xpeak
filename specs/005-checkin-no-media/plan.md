# Phase 5 — Check-in (No Media)

> Status: **planning only**. Companion to `spec.md` and `tasks.md`.

## 1. Goal

Wire the XP engine end-to-end for the first time: a user opens the
app, taps "check in", picks a category, submits, and their `xp` /
`level` / `streak_days` update. Media is **not** required in this
phase — it lands in Phase 6.

## 2. Scope

**In scope**
- `check_ins` schema + Ecto changesets.
- `POST /check_ins` endpoint (auth required).
- `GET /check_ins?limit=&cursor=` — paginated personal history.
- Transactional flow: create check-in → compute XP → update user
  totals → compute streak delta → return payload.
- Streak update: `current_streak_days` and `longest_streak_days`
  based on `performed_at` distinct days.
- Mobile: check-in screen (category picker + submit) + updated
  profile with fresh totals.

**Out of scope**
- Any media attachment (Phase 6).
- Multipliers beyond category weight (Phase 7).
- Feed/timeline emission (Phase 13).
- Editable XP (Phase 9).

## 3. Approach

- Use `Ecto.Multi` to make the whole flow one transaction: insert
  check-in → recompute user totals → return the updated user +
  check-in.
- Streak logic: compare `performed_at` (day) to the user's last
  check-in day. Same day = no change. Consecutive day = +1. Gap = reset
  to 1.
- Time zones: user's `time_zone` field defaults to `"UTC"` for now;
  proper TZ handling arrives in Phase 21 (recap) when it starts to
  matter.

## 4. Artifacts

- `priv/repo/migrations/*_create_check_ins.exs`.
- `priv/repo/migrations/*_add_time_zone_to_users.exs`.
- `lib/xpeak/check_ins.ex` — context with `create_check_in/2`,
  `list_check_ins/2`.
- `lib/xpeak/check_ins/check_in.ex` — schema.
- `lib/xpeak/check_ins/streak_calculator.ex` — pure helper.
- `lib/xpeak_web/controllers/check_in_controller.ex`.
- `lib/xpeak_web/controllers/check_in_json.ex`.
- Mobile: `app/(app)/check-in/page.tsx` + form components +
  `lib/checkins/queries.ts`.
- Full test coverage.

## 5. Dependencies

- Blocked by: Phase 4 (XP engine).
- Blocks: Phase 6 (media), Phase 7 (multipliers), Phase 11 (medals),
  Phase 13 (feed).

## 6. Open questions

1. **Same-day multiple check-ins** — allow? Cap per day (e.g., 3)?
   Or one per category per day?
2. **`performed_at`** — server timestamp (safer) or client-provided
   (allows backfill)? MVP → server timestamp.
3. **Notes / description field** — free text (e.g., 280 chars) on
   the check-in?
4. **Pagination style** — cursor (recommended) or page/offset?

## 7. Success criteria

- `POST /check_ins` with `{ category_slug: "legs" }` returns 201
  with the check-in body + updated user totals.
- User's `xp` grows by the expected amount.
- User's `current_streak_days` becomes 1 on first check-in.
- Consecutive day check-in bumps streak; a 2-day gap resets it.
- `GET /check_ins?limit=20` returns the user's own history only.
- Mobile screen: 3 taps from home → check-in submitted → confetti
  (or lightweight success screen) → back to profile.
- CI green.
