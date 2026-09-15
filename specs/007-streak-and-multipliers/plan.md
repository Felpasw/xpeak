# Phase 7 — Streak & Category Multipliers

> Status: **planning only**. Companion to `spec.md` and `tasks.md`.

## 1. Goal

Turn the simple `xp_earned = base_xp × weight` from Phase 4 into a
proper composed multiplier: **streak tier × category weight ×
challenge bonus**. Snapshot the multiplier stack on each check-in
so historical analysis (Phase 21 recap) can trust the numbers.

## 2. Scope

**In scope**
- `Xpeak.Progression.MultiplierResolver` — pure function returning
  the composed multiplier from a user + category + optional
  challenge.
- Streak-tier table: `{ min_days, max_days, multiplier }` (e.g.,
  1–2 = 1.0, 3–6 = 1.1, 7–13 = 1.25, 14–29 = 1.5, 30+ = 2.0).
- `multiplier_snapshot` JSONB column on `check_ins` capturing
  `{ streak: 1.25, category: 1.4, challenge: 1.0, total: 1.75 }`.
- Update `create_check_in/2` from Phase 5 to use the resolver.
- Challenge multiplier stub — returns 1.0 until Phase 15 wires real
  challenges; API-ready.

**Out of scope**
- Group bonuses (Phase 19).
- Editable multiplier config (Phase 9).
- Streak-freeze / "life" mechanic (deferred).

## 3. Approach

- Multipliers stack **multiplicatively** with a hard cap (default
  3.0). Cap makes exploits bounded.
- Streak tier table lives in `config/config.exs` for now, migrates
  to DB in Phase 9.
- Snapshot format is stable: additive changes only (never rename
  fields).
- Resolver is pure — takes plain data structs, returns a
  `Multiplier.t()`.

## 4. Artifacts

- `lib/xpeak/progression/multiplier.ex` — struct + JSON encoder.
- `lib/xpeak/progression/streak_tier.ex` — tier table + lookup.
- `lib/xpeak/progression/multiplier_resolver.ex` — composition +
  cap.
- Migration: `add_multiplier_snapshot_to_check_ins.exs`.
- Update `XpCalculator.compute/1` → `compute/2` accepting the
  multiplier.
- Update `Xpeak.CheckIns.create_check_in/2` to compute and persist
  snapshot.
- Update `CheckInJson` to expose the snapshot (opt-in field).

## 5. Dependencies

- Blocked by: Phases 4, 5.
- Blocks: Phase 9 (editable config lifts tier table to DB), Phase 15
  (challenge multiplier gets real values), Phase 21 (recap reads
  the snapshot).

## 6. Open questions

1. **Streak tier bands** — the table above OK, or different bands?
2. **Overall cap** — 3.0 too generous? 2.5?
3. **Show the breakdown on the check-in success screen?** ("+21 XP:
   base 15 × legs 1.4 × streak 7d 1.25")
4. **How to display active multiplier on the profile?** — small
   badge showing current streak-tier multiplier?

## 7. Success criteria

- `MultiplierResolver.resolve/3` returns the right composed value
  for known input tuples (spec-cased).
- Check-in with a 7-day streak, category `legs` (1.4) produces
  `xp_earned = round(15 * 1.4 * 1.25) = 26`.
- `multiplier_snapshot` present and correct on each new row.
- Cap enforced: absurd inputs saturate at 3.0.
- Existing Phase 5 tests still pass (regression-free).
- CI green.
