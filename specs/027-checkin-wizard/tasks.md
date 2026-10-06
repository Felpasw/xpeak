# Phase 27 — Check-in Wizard with XP Preview (TASKS)

> Stack: C# 12 + ASP.NET Core 9 + EF Core 9 + Postgres 16 + xUnit,
> React 19 + Next 16 (Turbopack) + motion/react + react-hook-form + zod.

## Legend

- `[T]` TDD (failing test first, show red, then implement).
- `[S]` sequential (must run in listed order).
- `[P]` parallel (safe to run in any order or alongside peers).
- `[HUMAN]` human-only step.
- `[ ]` not started, `[~]` in progress, `[x] ✅ commit <hash>` done.

---

## Section A — Backend scoring refactor + preview service

- [ ] **T-027-01 `[T][S]`** — Extract shared `BuildScoringAsync`
  on `CheckInService`
  - Failing test (`apps/api.Tests/CheckIns/CheckInServiceTests.cs`
    extended): calling the extracted method on a known
    `(user, category)` returns the same
    `(xp, scoringSnapshot)` the existing `CreateAsync` persists.
  - Refactor `CreateAsync` to delegate the scoring block (load
    category + membership + rule + compute) to the private method.
    No behavior change.
  - Green.

- [ ] **T-027-02 `[T][S]`** — `IStreakService.ProjectAsync` +
  `StreakProjection` DTO
  - Failing test (`apps/api.Tests/CheckIns/StreakProjectionTests.cs`):
    empty history + today → `(0, 1, true)`;
    consecutive day → `(n, n+1, true)`;
    same-day duplicate → `(n, n, false)`;
    gap → `(0, 1, true)`;
    non-daily config throws `NotSupportedException`.
  - Implement `ProjectAsync` on `StreakService` using the same
    date-fetch as `ComputeAsync` and two calls to
    `StreakCalculator.Compute` (with and without `asOf`).
  - Green.

- [ ] **T-027-03 `[T][S]`** — `ICheckInService.PreviewAsync` +
  `CheckInPreviewResult`
  - Depends on T-027-01, T-027-02.
  - Failing test (`apps/api.Tests/CheckIns/CheckInPreviewServiceTests.cs`):
    preview + subsequent create for the same input yield identical
    `xp_earned`, `scoring_snapshot`; `projection.user.level.after`
    matches actual `user.level` after create; `projection.streak.projected`
    matches the real streak after create; `403` for non-member,
    `404` for unknown category.
  - Implement `PreviewAsync` using `BuildScoringAsync` +
    `ProjectAsync`. No DB writes, no transaction.
  - Green.

## Section B — HTTP surface

- [ ] **T-027-04 `[T][S]`** — `POST /check_ins/preview` endpoint
  - Depends on T-027-03.
  - Failing test (`apps/api.Tests/CheckIns/CheckInPreviewEndpointTests.cs`):
    200 happy with payload shape per `spec.md` §3.1; 401 no
    token; 403 non-member; 404 unknown category; 422 validation
    (title length, duration ≤ 0, performed_at out of window,
    notes > 280); idempotency — two preview calls followed by
    `GET /check_ins` returns zero rows for that user.
  - Implement
    `apps/api/src/CheckIns/Endpoints/PreviewCheckInEndpoint.cs`,
    wire into `CheckInsModule.UseCheckIns()`. Rate-limit under
    the same policy as `CreateCheckInEndpoint`.
  - Green.

## Section C — Shared types

- [ ] **T-027-05 `[T][S]`** — Shared TS types for preview
  - Depends on T-027-04.
  - Failing test (`packages/shared/test/checkin/preview.spec.ts`):
    a sample snake-cased API response round-trips through the
    existing response transformer into the new
    `CheckInPreviewResponse` camelCased shape.
  - Add `CheckInPreviewResponse`, `CheckInProjection`,
    `StreakProjection` to
    `packages/shared/src/checkin/types.ts`.
  - Green.

## Section D — Mobile data layer

- [ ] **T-027-06 `[T][S]`** — `useCheckinPreview()` hook
  - Depends on T-027-05.
  - Failing spec (`apps/mobile/test/hooks/useCheckinPreview.spec.tsx`):
    calling with a valid payload fires `/check_ins/preview` and
    exposes `{preview, projection, isPending, error}`; same
    payload hash reuses cache (second call doesn't refetch);
    changed payload refetches; `refetch()` forces a refire.
  - Implement
    `apps/mobile/src/hooks/useCheckinPreview.ts` on TanStack
    Query. Cache key = JSON-stable payload hash.
  - Green.

## Section E — Mobile wizard

- [ ] **T-027-07 `[T][S]`** — `useCheckinWizardSteps` controller
  - Failing spec (`apps/mobile/test/hooks/useCheckinWizardSteps.spec.tsx`):
    starts at index 0; `next()` with invalid step fields keeps
    index + exposes error; `next()` with valid fields advances;
    `prev()` goes back; `jumpTo(n)` only works for indices
    already visited; `isLast` and `isFirst` flags correct.
  - Implement under
    `apps/mobile/src/components/organisms/CheckinWizard/useCheckinWizardSteps.ts`.
  - Green.

- [ ] **T-027-08 `[T][S]`** — Step view components
  - Depends on T-027-07.
  - Failing spec per step (`apps/mobile/test/components/organisms/CheckinWizard/`):
    - `StepCategory.spec.tsx` — renders categories, selection
      binds to `categoryId`.
    - `StepDetails.spec.tsx` — title + notes bound, error renders
      for empty title.
    - `StepSchedule.spec.tsx` — date + duration bound, error
      renders for future date / duration ≤ 0.
    - `StepReview.spec.tsx` — renders collected values; on mount
      fires `useCheckinPreview`; shows loader while pending;
      renders XP delta + breakdown on resolve; shows retry on
      error.
  - Implement the four step components under
    `apps/mobile/src/components/organisms/CheckinWizard/`.
  - Green.

- [ ] **T-027-09 `[T][S]`** — `XpProjectionBar` + `StreakProjection`
  atoms
  - Depends on T-027-05 (types).
  - Failing spec:
    - `XpProjectionBar.spec.tsx` — renders two width keyframes
      (current → after), level pill updates when `after` crosses
      `levelsGained > 0`.
    - `StreakProjection.spec.tsx` — renders `current → projected`
      with the right unit label; dim when `advances === false`.
  - Implement under `apps/mobile/src/components/atoms/`.
  - Green.

- [ ] **T-027-10 `[T][S]`** — `CheckinWizard` composition +
  swap on `/checkin`
  - Depends on T-027-07, T-027-08, T-027-09.
  - Failing spec
    (`apps/mobile/test/components/organisms/CheckinWizard.spec.tsx`):
    - Starts on step 1; submit action not rendered.
    - Can't advance past 1 without category.
    - Can't advance past 2 with empty title.
    - Can't advance past 3 with future date.
    - On step 4 enter, fires `/check_ins/preview` once.
    - Projected XP delta visible, bar reaches `after` width.
    - Submit on step 4 fires `/check_ins`, navigates to
      `/profile` on success.
    - Back from step 4 preserves values.
    - `/check_ins/preview` 5xx → placeholder + retry; submit
      still works.
  - Implement
    `apps/mobile/src/components/organisms/CheckinWizard.tsx`
    composing step controller + step views + preview + submit.
    `AnimatePresence` + slide-x transitions.
  - Delete `apps/mobile/src/components/organisms/CheckinForm.tsx`.
  - Update `apps/mobile/src/app/(app)/checkin/page.tsx` to render
    `CheckinWizard`.
  - Green.

## Section F — Wrap-up

- [ ] **T-027-11 `[P]`** — ADR `docs/adr/0027-checkin-preview.md`
  - Why preview is server-authoritative, why no debounce, why
    the same rate-limit as create, why no server-side cache.

- [ ] **T-027-12 `[S]`** — Phase close.
  - Flip the roadmap marker from 📋 to 🛠️ when the release tag
    ships.
  - Confirm the `/checkin` route renders the wizard end-to-end
    on-device.

---

## Dependencies

```
A1 ──▶ A3 ──▶ B ──▶ C ──▶ D ──▶ E (step by step) ──▶ F
A2 ──┘
```

## Bundling strategy for PRs

1. `feat(check-ins): extract shared scoring + add streak projection` — A1 + A2.
2. `feat(check-ins): add preview service + endpoint` — A3 + B.
3. `feat(shared): preview response types` — C.
4. `feat(mobile): preview hook + wizard controller` — D + E7.
5. `feat(mobile): step views + XP projection visuals` — E8 + E9.
6. `feat(mobile): check-in wizard swap on /checkin` — E10.
7. `docs(adr): record check-in preview decision` — F11.
8. Phase close — F12.
