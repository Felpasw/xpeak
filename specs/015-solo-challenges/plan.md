# Phase 15 — Solo Challenges

> Status: **planning only**.

> **Stack note:** artifacts in this file (module names, package names,
> code samples) were originally written for Elixir/Phoenix. The
> project switched to C# + ASP.NET Core 9 in Phase 2. See
> `specs/roadmap.md` → "Stack migration note" for the mapping table.
> Concepts and endpoint contracts still hold; concrete artifacts get
> rewritten when this phase is picked up.


## 1. Goal

First challenge modality: a user creates a personal challenge (goal,
categories, duration, optional XP bonus multiplier) and tracks
their own progress. Zero social layer here — challenges are between
the user and themselves.

## 2. Scope

**In scope**
- `challenges` + `challenge_memberships` + `challenge_categories`
  tables (schema future-proof for group challenges).
- Challenge kinds: solo only in this phase.
- Win rules: `total_xp`, `total_check_ins`, `consecutive_days`
  (start with these three).
- Challenge XP bonus multiplier feeds into `MultiplierResolver`
  (from Phase 7 — the `challenge` slot becomes real for check-ins
  that fall within active memberships).
- Endpoints for CRUD, join/leave (solo = 1 member), progress query.
- Mobile: challenges tab (personal history), create screen, detail
  screen with progress.

**Out of scope**
- Friend/group challenges (Phases 16, 17).
- Insignias/trophies (Phases 18, 20).
- Public discovery (Phase 17 layers it).

## 3. Approach

- Even though this phase ships solo-only, the schema supports
  multiple members and visibility. Phase 17 flips flags to enable.
- Challenge XP bonus applies **only** to check-ins in categories
  the challenge whitelists, during the challenge window, for
  members with an active membership.
- Progress computation is a pure function over check-ins tagged to
  the membership.

## 4. Artifacts

- Migrations: `create_challenges.exs`,
  `create_challenge_categories.exs`,
  `create_challenge_memberships.exs`.
- `lib/xpeak/challenges.ex` — context.
- `lib/xpeak/challenges/challenge.ex`, `membership.ex`,
  `progress_calculator.ex` (pure).
- Update `MultiplierResolver.resolve/3` to load active challenge
  memberships for the user + category and apply the bonus.
- Update `CheckIns.create_check_in/2` to record challenge
  attribution when relevant.
- Endpoints + mobile UX.

## 5. Dependencies

- Blocked by: Phases 4, 5, 6, 7.
- Blocks: Phase 16 (friend challenges), Phase 17 (group challenges),
  Phase 18 (insignias awarded on completion), Phase 19 (group
  bonuses), Phase 20 (trophies).

## 6. Open questions

1. **Multiple active solo challenges per user** — allow N or cap at
   1? Recommendation: allow multiple (e.g., "30 days of legs" + "500
   km run in 3 months" simultaneously).
2. **Bonus multiplier bounds** — cap challenge multiplier at 1.5×?
   Interacts with the global 3.0× cap from Phase 7.
3. **Post-hoc challenge completion** — can the user retroactively
   join a challenge and get credit for past check-ins?
   Recommendation: **no**, only check-ins performed after joining
   count.
4. **Editing a running challenge** — allowed (with restrictions) or
   locked?
5. **Solo challenge visibility on profile** — always visible to
   friends, or private by default?

## 7. Success criteria

- Create a solo challenge → membership auto-created.
- Check-in during the window in a valid category → XP includes
  challenge bonus; snapshot reflects the multiplier.
- Progress endpoint returns correct percentage.
- Ending a challenge (deadline reached) flips its state to
  `completed` or `failed`, emits event.
- Mobile: create wizard works, detail screen updates on refresh.
- CI green.
