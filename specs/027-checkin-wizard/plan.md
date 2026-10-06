# Phase 27 — Check-in Wizard with XP Preview

> Status: **planning only**. Companion to `spec.md` and `tasks.md`.
> Stack: C# 12 + ASP.NET Core 9 + EF Core 9 + Postgres 16,
> React 19 + Next 16 (Turbopack) + motion/react + react-hook-form + zod.

## 1. Goal

Replace the single-screen check-in form with a 4-step wizard whose
final step previews the exact XP, level, and streak outcome before
the user commits. The preview must be the truth — a read-only
run of the same scoring code the real create uses, exposed via a
new `POST /check_ins/preview` endpoint. Zero duplication of
business logic on the client.

## 2. Scope

**In scope**

- `POST /check_ins/preview` endpoint — new, non-persisting,
  returns `{preview, projection}` where `projection` carries the
  projected XP, level, and streak after the hypothetical check-in.
- `ICheckInService.PreviewAsync` + a shared private
  `BuildScoringAsync` used by both `CreateAsync` and
  `PreviewAsync`. No scoring logic duplicated.
- `IStreakService.ProjectAsync` returning
  `(current, projected, advances)` by running the same
  `StreakCalculator` against the existing dates plus the
  hypothetical date.
- `CheckInWizard` React component replacing `CheckinForm` as the
  `/checkin` page content. 4 steps, slide-x transitions via
  `motion/react` + `AnimatePresence`.
- `useCheckinPreview()` hook — TanStack Query mutation that fires
  on step-4 enter and whenever the collected payload changes,
  cached by payload hash.
- Visuals on step 4: `XpProjectionBar` (animated width from
  current to projected XP, level pill updates mid-animation if
  the projection crosses the boundary) and `StreakProjection`
  (`current → projected` with the unit from the group config).
- Shared types in `packages/shared/src/checkin/types.ts` for the
  new response shape.
- Rate-limiting on `/check_ins/preview` under the same policy as
  `/check_ins` create — preview reads cost almost the same as a
  create and we don't want a sidecar DoS vector.

**Out of scope**

- Media step inside the wizard — Phase 6 owns that. Follow-up
  after 006 lands adds a step before review; no re-architecture
  needed (the step controller already treats steps as a list).
- Multi-category batch submit, editing after submit, undo.
- Preview-level real-time (debounced) updates on every keystroke
  — preview fires only on step enter + explicit refetch.
- Server-side cache for preview responses. Preview is cheap, the
  payload is already client-known, and caching would risk serving
  a stale `user.xp` snapshot.
- Any change to how XP is actually earned. Preview is read-only.

## 3. Approach

- **Shared scoring path**: extract the current `CreateAsync` work
  up to and including `BuildScoringAsync` into a private method on
  `CheckInService` that returns
  `(category, rule, xpEarned, snapshot)`. Both `CreateAsync` and
  `PreviewAsync` call it. The real create then runs the DB write
  + commits; the preview reads `user.xp` and projects.
- **Streak projection** is pure math on top of
  `StreakCalculator`. `ProjectAsync` loads the same date list
  `ComputeAsync` already loads, calls the calculator twice (with
  and without the hypothetical date), and derives
  `advances = projected.current > current.current`. Keeps the
  calculator pure; service just orchestrates.
- **Wizard state** lives in a single `react-hook-form` instance
  owned by `CheckinWizard`. A `useCheckinWizardSteps` hook holds
  the step index + exposes `next()` / `prev()` / `jumpTo()` and
  gates `next()` with `form.trigger(fieldsForStep(current))` so
  invalid steps show errors inline and block advancement.
- **Preview trigger**: `useEffect` on step index change — when
  the user lands on step 4, fire `previewMutation.mutate(values)`.
  Also expose a manual refetch for the retry button in the error
  state. Cache key = JSON-stable hash of the payload so going
  back → changing a field → forward refires; going back → forward
  with same values reuses the previous result.
- **Visuals**: `motion.div` with `width` animation on the XP bar,
  spring config matching the existing `LevelUpOverlay`. Numbers
  animate with a `motion.span` counting up (shared with
  `/profile`, candidate for a small helper if duplication grows).
- **Submit path unchanged**: step 4's primary action calls the
  existing `useCheckinForm.onSubmit` which already handles
  success / toast / level-up overlay / navigation. The preview
  is purely informational — never gate submit on a preview
  response.

## 4. Risks and mitigations

- **Preview divergence from real create** — mitigated by making
  both call the exact same `BuildScoringAsync` + the same
  `StreakCalculator`. A test locks:
  "preview followed by create for the same input yields identical
  `xp_earned`, `scoring_snapshot`, and projected level matches
  actual level after create".
- **Step state loss on back-navigation** — mitigated by owning
  the form at the wizard level, not per step; `motion/react`
  unmount of a step view does not unmount the form.
- **Preview spam from retries** — mitigated by rate-limit shared
  with create and the client-side cache on payload hash.
- **Phase 6 overlap** — the wizard ships without a media step.
  When 006 lands, the step list grows by one. Keeping the step
  list data-driven (array of step descriptors) means that
  follow-up is additive, not a rewrite.
- **Weekly streak mode** — `StreakCalculator` still throws
  `NotSupportedException` for weekly in Phase 5. Preview inherits
  the throw. Non-blocker as long as Global stays daily;
  revisit when Phase 9/11 unlocks weekly.

## 5. Rollout

- Backend endpoint ships first (behind the same auth/rate as
  create). No feature flag — endpoint is additive and the real
  create path is untouched.
- Mobile wizard replaces `CheckinForm` in one commit. Old
  component deletes with the swap. Tests cover both navigation
  and preview rendering.
- No migration, no config, no secrets.

## 6. Dependencies

- Phase 5 (`005-checkin-no-media`) — required, already shipped.
- Phase 6 (`006-checkin-media`) — not blocking. If 006 is in
  progress when 027 starts, the two can proceed in parallel; the
  wizard just doesn't render a media step yet.
- Phase 7 (compound multipliers) — the preview automatically
  reflects any multipliers because the whole breakdown lives in
  `scoring_snapshot`.
