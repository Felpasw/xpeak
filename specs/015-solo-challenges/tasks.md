# Phase 15 — Solo Challenges (TASKS)

> Stack: C# 12 + ASP.NET Core 9 + EF Core 9 + Postgres 16 + xUnit
> + Hangfire (lifecycle job). Mobile: Next.js 16 + Capacitor +
> TanStack Query + Vitest.

## Legend

- `[T]` TDD (failing test first, show red, then implement).
- `[S]` sequential (must run in listed order).
- `[P]` parallel (safe to run in any order or in parallel with peers).
- `[DEFERRED]` blocked by an upstream phase — do not start here.
- `[HUMAN]` human-only step.
- `[ ]` not started, `[~]` in progress, `[x] ✅ commit <hash>` done.

---

## Section A — Schema

- [x] **T-015-01 `[T][S]`** ✅ commit `ebd30a4` — `challenges` + `challenge_categories` + `challenge_memberships`
  - Failing tests
    (`apps/api.Tests/Challenges/ChallengePersistenceTests.cs`):
    `mode` must be in `{"solo","friend","group"}`, `visibility` in
    `{"public","private"}`, `state` in
    `{"upcoming","active","completed","failed","cancelled"}`;
    `xp_multiplier` CHECK `0.5..1.5`; `starts_at < ends_at` CHECK;
    unique `(challenge_id, category_id)` on
    `challenge_categories`; unique `(challenge_id, user_id)` on
    `challenge_memberships`.
  - Add
    `apps/api/src/Challenges/Entities/Challenge.cs`,
    `ChallengeCategory.cs`, `ChallengeMembership.cs` and the
    `WinRule` JSONB property on `Challenge` (EF conversion).
  - Extend `AppDbContext`: `DbSet<Challenge>`,
    `DbSet<ChallengeCategory>`, `DbSet<ChallengeMembership>`;
    `OnModelCreating` mappings (snake_case, CHECK constraints,
    indexes per spec §1).
  - `dotnet ef migrations add AddChallenges`.
  - Green.

## Section B — Domain

- [x] **T-015-02 `[T][S]`** ✅ commit `11b853d` — `IChallengeService.CreateAsync`
  - Depends on T-015-01.
  - Failing test
    (`apps/api.Tests/Challenges/ChallengeServiceTests.cs`):
    creates `challenges` + `challenge_categories` +
    `challenge_memberships` (owner auto-joined) in a single
    transaction; rolls back if any row violates a CHECK; emits
    `challenge.created` event (Phase 13 placeholder is fine).
  - Add
    `apps/api/src/Challenges/Dto/CreateChallengeInput.cs`,
    `CreateChallengeResult.cs`.
  - Add `apps/api/src/Challenges/Services/IChallengeService.cs`
    + `ChallengeService.cs` with `CreateAsync`.
  - Green.

- [x] **T-015-03 `[T][S]`** ✅ commit `23eb8d2` — `IProgressCalculator.ComputeAsync` (XP → target_level)
  - Depends on T-015-01.
  - Failing test
    (`apps/api.Tests/Challenges/ProgressCalculatorTests.cs`):
    for a membership, sums `check_ins.xp_earned` where the
    check-in's `category_id` is in `challenge_categories` and
    `performed_at` is within `[starts_at, ends_at]` and the user
    is the membership's user; converts the sum to a level via
    `LevelCurve.LevelForXp` and compares against the challenge's
    `target_level`; returns a `MembershipProgress` with
    `Value = xp accumulated`, `Target = XpForLevel(target_level)`,
    `AchievedAt` set when level reached, null otherwise.
  - Add
    `apps/api/src/Challenges/Services/IProgressCalculator.cs` +
    `ProgressCalculator.cs` with `ComputeAsync`.
  - Register in `ChallengesModule`.
  - Green.

- [~] **T-015-04 `[CANCELLED]`** — `ComputeTotalCheckInsAsync`
  - Cancelled: the `TotalCheckIns` win rule was removed when we
    collapsed to `target_level` as the only win mechanic. If
    "count N check-ins" ever comes back as a rule, re-open this
    task with a new T-015-NN number.

- [~] **T-015-05 `[CANCELLED]`** — `ComputeConsecutiveDaysAsync`
  - Cancelled: the `ConsecutiveDays` win rule was removed with
    the same simplification as T-015-04. Streak-style challenges
    will live on Phase 7's streak infra, not here.

- [ ] **T-015-06 `[T][S][DEFERRED]`** — Challenge bonus multiplier
  - **Deferred** — depends on Phase 7
    (`streak-and-multipliers`) landing first; that phase owns
    `IMultiplierResolver`. Reopen when Phase 7 ships.
  - When unblocked:
    - Failing test: check-in whose `category_id` is in a currently-
      active membership's `challenge_categories` receives the
      challenge's `xp_multiplier`; non-whitelisted category → no
      bonus; multiple overlapping memberships → max applied, not
      summed.
    - Extend the Phase 7 `IMultiplierResolver` chain with the
      challenge-bonus source. Scoring snapshot gains a
      `"challenge_bonus"` multiplier entry per applied challenge.
    - Green.
  - For Phase 15 shipping without T-015-06: challenges still
    affect UI (progress, membership, lifecycle) but check-ins
    accrue XP at the base rate until Phase 7 wires the bonus.

## Section C — Lifecycle

- [~] **T-015-07 `[DEFERRED]`** — `ChallengeLifecycleJob` (Hangfire)
  - **Deferred** — Phase 15 ships without automatic state
    transitions. Challenges still render correctly (state is a
    stored column, owner can `POST /challenges/{id}/cancel`), but
    `upcoming → active`, `active → completed` and `active → failed`
    stay in whatever state the writer set until this job lands.
    Will be reopened as its own ticket once we need lifecycle
    automation (likely around Phase 17 group challenges).
  - When reopened:
    - Failing tests
      (`apps/api.Tests/Challenges/ChallengeLifecycleJobTests.cs`):
      `upcoming → active` when `now >= starts_at`;
      `active → completed` when the win rule target is met;
      `active → failed` when `now > ends_at` without hitting the
      target; `cancelled` is terminal.
    - Add
      `apps/api/src/Challenges/Jobs/ChallengeLifecycleJob.cs` +
      Hangfire recurring job registration (`every 5 minutes`
      initially; tune later).
    - Emits `challenge.finished` via the Phase 13 event emitter
      (or a local placeholder if Phase 13 hasn't landed).
    - Green.

## Section D — HTTP surface

- [x] **T-015-08 `[T][S]`** ✅ commit `2301c07` — Challenges CRUD endpoints
  - Depends on T-015-02.
  - Failing tests
    (`apps/api.Tests/Challenges/ChallengesEndpointTests.cs`):
    - `POST /challenges` — 201 on happy path; 422 on bad win rule
      shape; 403 if `mode === "group"` and the caller is not
      allowed to create group challenges yet (Phase 17 unblocks).
    - `GET /challenges` — lists caller's memberships, filterable
      by `?state=`.
    - `GET /challenges/{id}` — 200 for members or public
      challenges; 404 otherwise (no "forbidden" leak).
    - `PATCH /challenges/{id}` — allowed only when `state ===
      "upcoming"`; 409 otherwise.
    - `POST /challenges/{id}/cancel` — allowed by owner,
      transitions to `cancelled`; 409 if already terminal.
  - Add endpoints under
    `apps/api/src/Challenges/Endpoints/` and DTOs under
    `apps/api/src/Challenges/Dto/`.
  - Green.

- [x] **T-015-09 `[T][S]`** ✅ commit `2301c07` — `GET /challenges/{id}/progress`
  - Depends on T-015-08 and T-015-03..05.
  - Failing test
    (`apps/api.Tests/Challenges/ChallengeProgressEndpointTests.cs`):
    returns `{ totalXp, totalCheckIns, consecutiveDays,
    windowStartsAt, windowEndsAt, winRuleStatus }` for the
    caller; 404 if the caller is not a member of a private
    challenge.
  - Add
    `apps/api/src/Challenges/Endpoints/GetChallengeProgressEndpoint.cs`.
  - Green.

## Section E — Mobile

- [x] **T-015-10 `[T][S]`** ✅ commit `74a9c18` — Shared types + challenges service + hooks
  - Depends on T-015-08 and T-015-09.
  - Add `packages/shared/src/challenges/types.ts` with
    `Challenge`, `ChallengeMembership`, `CreateChallengeRequest`,
    `ChallengeProgressResponse`.
  - Add `apps/mobile/src/services/challenges.service.ts` +
    `apps/mobile/src/services/interfaces/challenges.interface.ts`.
  - Add `apps/mobile/src/hooks/useChallenges.ts` exposing
    `useChallengesList({ state? })`, `useChallenge(id)`,
    `useChallengeProgress(id)`,
    `useCreateChallenge`.
  - Failing spec
    (`apps/mobile/test/hooks/useChallenges.spec.tsx` with MSW):
    list query sends `?state=`; mutation posts and invalidates
    the list key.
  - Green.

- [x] **T-015-11 `[T][S]`** ✅ commit `1418db6` — Challenges tab + create form
  - Depends on T-015-10.
  - New tab `/challenges` with a segmented control
    (`upcoming | active | completed`).
  - Create wizard — multi-step (title/description → categories →
    window + win rule → review) using the existing
    `ControlledInput` / `ControlledDatePicker` /
    `ControlledCategoryPicker` atoms.
  - Failing specs
    (`apps/mobile/test/app/(app)/challenges/page.spec.tsx` and
    `apps/mobile/test/components/organisms/CreateChallengeWizard.spec.tsx`):
    renders segmented tabs; wizard happy path POSTs the expected
    payload and navigates to the new challenge's detail route.
  - Green.

- [x] **T-015-12 `[T][S]`** ✅ commit `da8badd` — Challenge detail screen with progress
  - Depends on T-015-11.
  - New route `/challenges/[id]` renders:
    - Header: title, mode badge, countdown to `ends_at`.
    - Progress strip: `useChallengeProgress(id)` → progress bar
      per win-rule metric.
    - Recent check-ins in-window (reuse `CheckinList` scoped by
      challenge).
  - Failing spec
    (`apps/mobile/test/app/(app)/challenges/[id]/page.spec.tsx`):
    renders header + progress + check-in list; shows a
    "challenge not found" fallback on 404.
  - Green.

## Section F — Wrap-up

- [ ] **T-015-13 `[P]`** — ADR `docs/adr/0015-solo-challenges.md`
  - Decisions: win-rule JSONB over structured columns,
    lifecycle as a cron job vs. event-driven, `MultiplierResolver`
    deferred to Phase 7, group challenges deferred to Phase 17
    (CRUD gate in T-015-08).

- [x] **T-015-14 `[S]`** — Phase close
  - `dotnet test apps/api.Tests` → 258/258 green.
  - `pnpm --filter @xpeak/mobile test` → 75/75 green.
  - `dotnet format --verify-no-changes` → clean on `apps/api` and
    `apps/api.Tests`.
  - `pnpm --filter @xpeak/mobile lint` → clean.
  - CI green on the PR (verified at PR time).

---

## Dependencies

```
A1 ──▶ A2 ──▶ B1 ──▶ C1 ──▶ D1 ──▶ D2
                              │
                              ▼
                             E1 ──▶ E2 ──▶ E3
                                            │
                                            ▼
                                           F1 (opt), F2

B2 and B3 are CANCELLED (TotalCheckIns / ConsecutiveDays win rules
were dropped during spec review). B6 (challenge-bonus multiplier)
is DEFERRED until Phase 7 ships IMultiplierResolver.
```

## Bundling strategy for PRs

1. `feat(challenges): add schema (challenges, categories, memberships)` — A1
2. `feat(challenges): add service + progress calculator` — A2 + B1
3. `feat(challenges): add lifecycle job + CRUD + progress endpoint` — C1 + D1 + D2
4. `feat(mobile): add challenges service, hooks, tab and create wizard` — E1 + E2
5. `feat(mobile): add challenge detail screen with progress` — E3
6. `docs(adr): solo challenges` — F1 (optional)
