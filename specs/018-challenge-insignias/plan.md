# Phase 18 — Insignias for Challenge Completion

> Status: **planning only**.

> **Stack note:** artifacts in this file (module names, package names,
> code samples) were originally written for Elixir/Phoenix. The
> project switched to C# + ASP.NET Core 9 in Phase 2. See
> `specs/roadmap.md` → "Stack migration note" for the mapping table.
> Concepts and endpoint contracts still hold; concrete artifacts get
> rewritten when this phase is picked up.


## 1. Goal

Award **insignias** to users who finish challenges (any modality).
Different tiers per rank / participation. Displayable on the profile
next to medals.

## 2. Scope

**In scope**
- `insignias` (definitions) + `user_insignias` (earned) tables.
- Awarder that fires on challenge state transition to `completed`
  or `failed`.
- Tier system per challenge: gold (#1), silver (#2), bronze (#3),
  participant (finished, didn't podium), effort (participated but
  didn't finish).
- Default artwork for each tier per system challenge template;
  challenge owner can attach custom artwork (Phase 20 trophy has
  its own custom flow).
- Feed emission (`insignia.earned`) and notification.

**Out of scope**
- Trophies (Phase 20) — insignias and trophies coexist.
- Custom per-challenge insignia artwork upload (deferred; ships
  system-only in this phase).

## 3. Approach

- `Achievements.InsigniaAwarder.on_challenge_finished(challenge)`
  runs at the state transition (piggybacks on the lifecycle job
  from Phase 15).
- Ranking based on `ProgressCalculator` outcomes and `finished_at`
  timestamps; ties broken by earlier `finished_at`.
- Idempotent: awarding twice for the same user × challenge is a
  no-op.

## 4. Artifacts

- Migrations: `create_insignias.exs`, `create_user_insignias.exs`.
- `lib/xpeak/achievements/insignia.ex`, `user_insignia.ex`,
  `insignia_awarder.ex`.
- Seed 5 default insignias (gold/silver/bronze/participant/effort).
- Wire awarder into the challenge lifecycle job.
- Endpoint: `GET /users/:username/insignias`.
- Mobile: insignias section on profile alongside medals.

## 5. Dependencies

- Blocked by: Phases 11 (medals pattern), 15/16/17 (challenges).
- Blocks: Phase 21 (recap counts insignias).

## 6. Open questions

1. **Participation threshold for "participant" tier** — completed
   at least 50% of target?
2. **Ties on podium** — split gold or extend to more medals?
3. **Retroactive award** — award for challenges finished before
   this phase?

## 7. Success criteria

- Finishing a challenge #1 → gold insignia awarded.
- Failing to complete a challenge (but participated) → effort or
  participant based on threshold.
- Idempotent.
- Feed event + notification fired.
- Profile shows insignias grid alongside medals.
- CI green.
