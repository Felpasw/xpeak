# Phase 27 — Check-in Wizard with XP Preview (SPEC)

> Stack: C# 12 + ASP.NET Core 9 + EF Core 9 + Postgres 16 + xUnit,
> React 19 + Next 16 (Turbopack) + motion/react + react-hook-form + zod.

## 1. Summary

Turn the single-screen check-in form into a 4-step wizard with a
final review step that calls a new **non-persisting preview
endpoint** on the API and projects the user's XP bar, level, and
streak after submit — so the user sees exactly what they'll gain
before confirming. Zero changes to the actual write path
(`POST /check_ins`); the preview reuses the same scoring code
in a read-only mode.

## 2. Flow

1. **Category** — grid of categories (same picker as today). Can't
   advance without a selection.
2. **Details** — title (required, 1..60 chars) + notes (optional,
   ≤ 280 chars). Can't advance with an empty / too-long title.
3. **When & how long** — `performedOn` (date, today ± 7 days per
   current backfill rule) + `durationMinutes` (optional). Can't
   advance with future / too-old date or invalid duration.
4. **Review** — shows the collected values, calls
   `POST /check_ins/preview` on step enter, renders:
   - Projected XP earned (big number).
   - Multiplier breakdown (same `scoringSnapshot` shape as a real
     check-in — `base_xp` + each multiplier line).
   - Animated XP progress bar from `user.xp` to
     `user.xp + previewXp`, with level pill updating if the
     projection crosses a boundary.
   - Streak projection (`current → projected`) with the group's
     unit (`day` / `week`).
   - Primary action = `AnimatedBorderFab` (same `✓` FAB from the
     current form) which fires `POST /check_ins` with the already
     validated payload.
   - Secondary action = back to step 3.

Wizard supports **back** at every step (preserves data). Forward
navigation is gated by `react-hook-form.trigger(step-fields)`.

If `POST /check_ins/preview` fails (network, 5xx), step 4 renders
the review values and the XP breakdown placeholder with a retry
button; submit stays enabled (worst case the real `/check_ins`
response surfaces the actual numbers after commit).

## 3. API

### 3.1. `POST /check_ins/preview`

**Auth:** JWT required.

**Request:** identical to `POST /check_ins` plus any media intent
counters introduced by Phase 6 (so the preview and the real create
share the DTO exactly). Phase 27 only adds the preview endpoint;
the request shape mirrors whatever `POST /check_ins` already
accepts at the time this ships.

```json
{
  "category_id": "uuid",
  "title": "Perna B",
  "performed_at": "2026-10-01T12:00:00Z",
  "duration_minutes": 45,
  "notes": "hard set"
}
```

**Response `200`:**
```json
{
  "preview": {
    "xp_earned": 21,
    "scoring_snapshot": {
      "base_xp": 15,
      "multipliers": [
        { "source": "category_weight", "value": 1.4 }
      ],
      "total": 21
    }
  },
  "projection": {
    "user": {
      "xp": { "current": 180, "after": 201 },
      "level": { "current": 2, "after": 2, "leveled_up": false, "levels_gained": 0 }
    },
    "streak": {
      "group_id": "uuid",
      "unit": "day",
      "current": 3,
      "projected": 4,
      "advances": true
    }
  }
}
```

- `advances` is `true` when the `performed_at` date is a day after
  the latest existing check-in in that group (daily mode).
  Weekly mode mirrors `StreakCalculator` behaviour — out of scope
  if Phase 9/11 hasn't shipped it yet; endpoint throws the same
  `NotSupportedException` the real create would.
- No row is written, no streak cache touched, no transaction opened.

**Failure responses:** mirror `POST /check_ins`.
- `401` — missing / invalid token.
- `403` — not a member of `category.group_id`.
- `404` — unknown `category_id`.
- `422` — same validation surface (`duration <= 0`, `notes > 280`,
  `title` length, `performed_at` out of window).

**Rate limit:** reuse the `check-in-create` policy — the preview
costs almost the same as a create (same loads, no write), so a
separate budget would just invite abuse.

## 4. Backend shape

Extract the scoring + projection path out of
`CheckInService.CreateAsync` into a pure method that both the
create and the preview call:

```csharp
public interface ICheckInService
{
    Task<CreateCheckInResult> CreateAsync(Guid userId, CreateCheckInInput input, CancellationToken ct = default);
    Task<CheckInPreviewResult> PreviewAsync(Guid userId, CreateCheckInInput input, CancellationToken ct = default);
}
```

`PreviewAsync` steps:

1. Load category + verify membership + load `XpRule` (same as
   create — zero duplication, both call a shared private
   `BuildScoringAsync`).
2. `ComputeXp(rule)` → `scoring_snapshot`.
3. Read current `user.xp` / `user.level`.
4. Project `newXp = user.xp + xp`,
   `newLevel = LevelUpService.LevelForXp(newXp)`, derive
   `leveled_up` + `levels_gained`.
5. Call `IStreakService.ProjectAsync(userId, category.GroupId,
   performedAt, ct)` — new method that computes what the streak
   **would be** if a check-in landed on `performed_at`.

No DB writes. No transaction. Returns `CheckInPreviewResult`.

### 4.1. `IStreakService.ProjectAsync`

```csharp
public interface IStreakService
{
    Task<StreakInfo> ComputeAsync(Guid userId, Guid groupId, DateOnly asOf, CancellationToken ct = default);
    Task<StreakProjection> ProjectAsync(Guid userId, Guid groupId, DateOnly asOf, CancellationToken ct = default);
}

public sealed record StreakProjection(
    StreakInfo Current,
    StreakInfo Projected,
    bool Advances);
```

Implementation: load the same distinct dates `ComputeAsync` reads;
run `StreakCalculator.Compute(dates, asOf)` for `Current`, then
`StreakCalculator.Compute(dates ∪ {asOf}, asOf)` for `Projected`;
`Advances = Projected.Current > Current.Current`.

## 5. Mobile shape

```
apps/mobile/src/components/organisms/
  CheckinWizard.tsx                  ← replaces CheckinForm as the /checkin page content
  CheckinWizard/
    StepCategory.tsx
    StepDetails.tsx
    StepSchedule.tsx
    StepReview.tsx
    XpProjectionBar.tsx              ← animated XP bar + level pill
    StreakProjection.tsx             ← current → projected chip
    useCheckinWizardSteps.ts         ← step controller: index, next/prev, gating
```

The existing `CheckinForm.tsx` is deleted — the wizard owns the
whole surface. `useCheckinForm.ts` stays as the data layer:
schema, submit mutation, `isPending`, level-up overlay dispatch.
The wizard adds a parallel `useCheckinPreview()` hook that:

- Watches the form values for step 4.
- `POST /check_ins/preview` via TanStack Query (`useMutation`
  with manual trigger on step enter, cached by payload hash so
  going back and forth doesn't thrash).
- Exposes `{ preview, projection, isPending, error, refetch }`.

Transitions: `motion/react` `AnimatePresence` + slide-x +
opacity. The `AnimatedBorderFab` stays on step 4 as the submit
action; previous steps use a lighter "next" button with the same
glass styling.

## 6. Non-goals

- Multi-category batch check-ins.
- Editing a check-in from the review step after submission.
- Showing the preview before the user picked a category — step 1
  blocks preview fetch.
- Push / toast on level-up projection (the real level-up overlay
  still runs on actual submit; preview just hints at it).
- Any change to how XP is actually earned — preview is read-only
  by construction.
- Media step wiring — Phase 6 owns the media flow. If 006 ships
  first, step 3 or an inserted step 3.5 gets the picker; this
  spec defers that call to the 006 PR that lands media required.

## 7. Test plan

### 7.1. Backend (`apps/api.Tests/CheckIns/`)

- `CheckInPreviewServiceTests` — happy path matches the real
  create for the same input (same `xp_earned`, same
  `scoring_snapshot`, projected `level` / `streak.projected`
  equal to the actual values after a subsequent `CreateAsync`).
- `CheckInPreviewEndpointTests` — 200 happy, 401 no token, 403
  non-member, 404 unknown category, 422 validation, idempotency
  (two preview calls followed by a `GET /check_ins` returns
  empty).
- `StreakProjectionTests` — `Advances == true` only when
  `asOf` is strictly after the latest existing date; `Advances
  == false` on same-day; `Projected.Current == Current.Current + 1`
  on consecutive day; gap resets to `Projected.Current == 1`.

### 7.2. Mobile (`apps/mobile/test/components/organisms/`)

- `CheckinWizard.spec.tsx`:
  - Starts on step 1, submit button not rendered.
  - Can't advance past step 1 without category.
  - Can't advance past step 2 with empty title; error renders.
  - Can't advance past step 3 with future date; error renders.
  - On entering step 4, calls `/check_ins/preview` once with the
    collected values.
  - Renders the projected XP delta, the animated bar reaches
    `preview.after` width.
  - Submit button on step 4 fires `/check_ins` and navigates to
    `/profile` on success (same as current form).
  - Back from step 4 preserves previously entered values.
  - If `/check_ins/preview` 5xx, review renders placeholder +
    retry; submit still works.

### 7.3. Shared types

- `packages/shared/src/checkin/types.ts` gets
  `CheckInPreviewResponse`, `CheckInProjection`,
  `StreakProjection` (snake→camel through the existing
  response transformer).

## 8. Dependencies

- **Phase 5** — core check-in flow, scoring, streak derivation.
  Required.
- **Phase 6** — media. Not blocking: wizard ships without a media
  step; when 006 lands, a follow-up adds the picker as a wizard
  step before review (one task, no re-architecture).
- **Phase 7** — compound multipliers. Preview automatically
  reflects any new multipliers because `scoring_snapshot` is the
  single source of truth.
