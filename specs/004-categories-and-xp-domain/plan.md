# Phase 4 — Categories & Pure XP Domain

> Status: **planning only**. Companion to `spec.md` and `tasks.md`.

## 1. Goal

Introduce the XP engine as a pure domain layer: workout categories
with their own base XP and weight, plus deterministic services that
calculate XP for a given check-in and decide whether it triggers a
level-up. **No UI, no check-in endpoint yet** — this phase produces
the arithmetic that everything else in Block B leans on.

## 2. Scope

**In scope**
- `Category` schema + system-seeded categories.
- `Xpeak.Progression.XpCalculator` — pure functions with 100%
  branch coverage.
- `Xpeak.Progression.LevelCurve` — configurable curve; ships with a
  default logarithmic function.
- `Xpeak.Progression.LevelUpService` — decides how many levels a
  delta of XP promotes.
- No mutation of the `users` table yet — the level curve just
  answers questions, doesn't persist.

**Out of scope**
- Actual check-in creation (Phase 5).
- Multipliers beyond category weight (Phase 7).
- Editable configuration surface (Phase 9).
- Any UI (Phase 5+).

## 3. Approach

- Categories live in a `categories` table with `slug` (unique),
  `name`, `icon`, `base_xp`, `weight_multiplier`, `is_system`,
  `active`.
- Seed a starter set (chest, back, legs, shoulders, arms, core,
  running, cycling, mobility, other).
- Everything in `Xpeak.Progression.*` is a pure function: same
  input → same output, no DB reads inside the calculator.
- The level curve default: `xp_for_level(n) = round(100 *
  :math.pow(n, 1.5))` — cheap early, painful late. Tunable in
  Phase 9.

## 4. Artifacts

- `priv/repo/migrations/*_create_categories.exs`.
- `lib/xpeak/progression.ex` — public context (`compute_xp/1`,
  `level_for_xp/1`, `xp_for_level/1`).
- `lib/xpeak/progression/category.ex` — schema.
- `lib/xpeak/progression/xp_calculator.ex` — pure calc.
- `lib/xpeak/progression/level_curve.ex` — pure curve.
- `lib/xpeak/progression/level_up_service.ex` — pure orchestrator.
- `priv/repo/seeds/categories.exs` — starter categories.
- Full test coverage of every pure module.

## 5. Dependencies

- Blocked by: Phases 1–3.
- Blocks: Phase 5 (check-in), Phase 7 (multipliers), Phase 9
  (editable config).

## 6. Open questions

1. **Default level curve formula** — logarithmic
   (`round(100 * n^1.5)`) or something else (Fibonacci-like,
   piecewise)?
2. **Starter category set** — the 10 above cover it or we want more
   (yoga, pilates, swim, martial arts)?
3. **Category icon representation** — ship as font-glyph names,
   emoji, or hosted image URLs?
4. **Weight multiplier bounds** — sane range (0.5 – 2.5)?
   Enforce at schema level to prevent absurd values.

## 7. Success criteria

- `mix test` green with the full `test/xpeak/progression/*_test.exs`
  suite.
- Seed loads all starter categories without errors.
- `iex> Xpeak.Progression.compute_xp(%{category_slug: "legs"})`
  returns the expected XP for the seeded weights.
- `iex> Xpeak.Progression.level_for_xp(300)` returns the correct
  level per the curve.
- CI green.
