# Phase 11 — Streak Medals

> Status: **planning only**.

## 1. Goal

First achievement type: **medals** awarded automatically when a user
hits streak milestones (7, 30, 100, 365 days). Displayable on the
profile.

## 2. Scope

**In scope**
- `medals` (definitions) + `user_medals` (earned) tables.
- Seed 4 default streak medals with default artwork.
- `Xpeak.Achievements.MedalAwarder` — checks after every check-in
  whether the user crossed a new streak milestone.
- `GET /me/medals` and inclusion on `GET /users/:username/medals`
  (public/friend read).
- Mobile: medals grid on the profile.

**Out of scope**
- Challenge insignias (Phase 17).
- Trophies (Phase 20).
- Custom medal artwork per user/challenge (deferred).
- Timeline emission (Phase 13 wires the event).

## 3. Approach

- Awarder runs at the end of `create_check_in/2` (in the same
  transaction). If a new milestone is crossed, insert a `user_medals`
  row.
- Milestones are data-driven (medals table has `streak_days_required`).
- Cache "medals earned by user" is denormalized only if it becomes
  expensive to query — start with a straight SELECT.

## 4. Artifacts

- Migrations: `create_medals.exs`, `create_user_medals.exs`.
- `lib/xpeak/achievements.ex` — context.
- `lib/xpeak/achievements/medal.ex`, `user_medal.ex`,
  `medal_awarder.ex`.
- Update `create_check_in/2` to call the awarder.
- Endpoints + mobile grid.

## 5. Dependencies

- Blocked by: Phase 5 (check-in exists), Phase 6 (media/storage for
  medal artwork).
- Blocks: Phase 13 (timeline emits `medal.earned`), Phase 17
  (insignias reuse the achievements pattern).

## 6. Open questions

1. **Default milestones** — 7/30/100/365? Add 3? Add 1000?
2. **Retroactive award** — for users who already had long streaks
   before this phase, run a backfill?
3. **Medal artwork** — bundled with the app or served from storage?

## 7. Success criteria

- On hitting streak day 7, a `user_medals` row is created.
- Medal appears on the user's profile with correct artwork.
- Never awarded twice (idempotent on repeated crossings after streak
  reset).
- CI green.
