# Phase 20 — Trophies (Customizable Artwork)

> Status: **planning only**.

## 1. Goal

Trophies as a **third achievement category**, distinct from medals
(streak-based) and insignias (challenge tier). Trophies are
reserved for high-signal wins (#1 in a group challenge, hardcore
solo completion) and their artwork is **always customizable** —
default from the app, or curated catalog, or uploaded by the
challenge owner.

## 2. Scope

**In scope**
- `trophies` (definitions) + `user_trophies` (earned) tables.
- Curated trophy catalog (5–10 default trophies).
- Challenge owners can attach a custom trophy asset (image or GIF)
  to their challenge.
- Awarder that fires on challenge completion for the winner(s).
- Feed emission + notification.
- Mobile: trophy case on profile alongside medals and insignias.

**Out of scope**
- Trading / gifting trophies.
- Sharing trophies as standalone shareable cards (Phase 21 recap
  covers that surface).

## 3. Approach

- A trophy definition can be either:
  - **System catalog** (`is_system: true`).
  - **Custom** created by a challenge owner and attached to that
    specific challenge (`challenge_id` set).
- When a challenge is created, the owner picks: default catalog
  trophy OR uploads a custom one. Choice stored on the challenge:
  `custom_trophy_id` (nullable).
- On challenge completion, `TrophyAwarder` awards:
  - Solo hardcore win (custom rule per challenge? MVP: solo with
    `total_xp` win kind + xp_multiplier ≥ 1.3 counts as hardcore).
  - #1 in a group challenge.
- Idempotent per user × challenge.

## 4. Artifacts

- Migrations: `create_trophies.exs`, `create_user_trophies.exs`,
  `add_custom_trophy_id_to_challenges.exs`.
- `lib/xpeak/achievements/trophy.ex`, `user_trophy.ex`,
  `trophy_awarder.ex`.
- Extend challenge create flow to accept trophy upload / catalog
  pick.
- Endpoints for trophies list + upload flow (reuses Phase 6
  storage).
- Mobile: challenge create wizard trophy step; trophy case on
  profile.

## 5. Dependencies

- Blocked by: Phases 6, 15, 17, 18.
- Blocks: Phase 21 (recap features trophies), Phase 22 (level forms
  echo trophy aesthetic).

## 6. Open questions

1. **"Hardcore" definition for solo trophies** — the placeholder
   above OK, or opt-in flag "eligible for trophy" on the challenge?
2. **Trophy count cap per challenge** — one winner or top 3 in
   group?
3. **Group challenge default trophy** — always pick 1st place only,
   or also lower tiers get a variant?
4. **Custom trophy artwork moderation** — content moderation
   pipeline exists in principle; MVP: no moderation, trust the owner.

## 7. Success criteria

- Owner creates group challenge and uploads a custom trophy PNG.
- On completion, #1 finisher gets the trophy in `user_trophies`.
- Trophy renders in the profile trophy case.
- Solo hardcore win awards the default catalog trophy for that
  challenge template.
- Feed event + notification fired.
- CI green.
