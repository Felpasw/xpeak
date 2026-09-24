# Phase 5 — Check-in (No Media) (TASKS)

> Stack: C# 12 + ASP.NET Core 9 + EF Core 9 + Postgres 16 + xUnit + FsCheck.

## Legend

- `[T]` TDD (failing test first, show red, then implement).
- `[S]` sequential (must run in listed order).
- `[P]` parallel (safe to run in any order or in parallel with peers).
- `[HUMAN]` human-only step.
- `[ ]` not started, `[~]` in progress, `[x] ✅ commit <hash>` done.

---

## Section A — Schema + user timezone + group configs

- [ ] **T-005-01 `[T][S]`** — `check_ins` + `group_configs` +
  `users.time_zone`
  - Failing tests
    (`apps/api.Tests/CheckIns/CheckInPersistenceTests.cs`,
    `apps/api.Tests/Groups/GroupConfigTests.cs`):
    `xp_earned <= 0` rejected, `duration_minutes = 0` rejected,
    `notes` > 280 rejected, unknown FKs rejected, Global has a
    `group_configs` row after migration, `StreakConfig` JSONB
    round-trips.
  - Add `apps/api/src/CheckIns/Entities/CheckIn.cs` with the
    `ScoringSnapshot` JSON property (EF conversion).
  - Add `apps/api/src/Groups/Entities/GroupConfig.cs` with the
    `StreakConfig` JSON property.
  - Extend `AppUser` with `TimeZone` (default `"UTC"`, max 64).
  - Extend `AppDbContext`: `DbSet<CheckIn>`, `DbSet<GroupConfig>`,
    `OnModelCreating` mappings (snake_case, CHECK constraints,
    unique/composite indexes, JSON column types).
  - Seed a `group_configs` row for the Global group via `HasData`.
  - `dotnet ef migrations add AddCheckInsAndGroupConfigs`.
  - Green.

## Section B — Group services extension

- [ ] **T-005-02 `[T][S]`** — `IGroupService.IsMemberAsync` +
  `IGroupConfigService`
  - Failing test (`GroupConfigTests.cs`): `IsMemberAsync` returns
    true for a user in the group, false otherwise;
    `GetStreakConfigAsync(GroupIds.Global)` returns
    `StreakConfig("daily", null, null)`.
  - Extend `IGroupService` + `GroupService` with
    `IsMemberAsync(userId, groupId, ct)`.
  - Add `IGroupConfigService` + `GroupConfigService` under
    `apps/api/src/Groups/Services/`. Register in
    `GroupsModule.AddGroups()`.
  - Green.

## Section C — Progression service extension

- [ ] **T-005-03 `[T][S]`** — `IProgressionService.GetCategoryByIdAsync`
  + `GetRuleAsync`
  - Failing test (`apps/api.Tests/Progression/ProgressionServiceTests.cs`
    extended): both methods return the seeded rows; return `null`
    for unknown ids.
  - Extend `IProgressionService` + `ProgressionService` with the
    two methods; extend `ICategoryRepository` (or use `AppDbContext`
    inside `ProgressionService`, whichever keeps the module thin).
  - Green.

## Section D — Streak calculator (pure) + streak service (derived)

- [ ] **T-005-04 `[T][P]`** — `StreakCalculator` (pure static)
  - Failing test (`apps/api.Tests/CheckIns/StreakCalculatorTests.cs`):
    empty list, single date matching `asOf`, N consecutive back
    from `asOf`, gap resets current, longest preserved, non-daily
    throws. FsCheck property: `current <= longest`.
  - Implement
    `apps/api/src/CheckIns/Services/StreakCalculator.cs` per
    `spec.md` §4.3.
  - Green.

- [ ] **T-005-05 `[T][S]`** — `IStreakService` + `StreakService`
  - Depends on T-005-01 (needs `CheckIns` table) + T-005-02
    (needs `IGroupConfigService`).
  - Failing test
    (`apps/api.Tests/CheckIns/StreakServiceTests.cs`,
    Testcontainers): fresh user → `(0,0)`; after first check-in
    inserted → `(1,1)`; two consecutive days → `(2,2)`; two groups
    isolated.
  - Implement under `apps/api/src/CheckIns/Services/`.
  - Green.

## Section E — Check-in orchestration

- [ ] **T-005-06 `[T][S]`** — `ICheckInService.CreateAsync`
  - Depends on T-005-01..05.
  - Failing test
    (`apps/api.Tests/CheckIns/CheckInServiceTests.cs`):
    happy path bumps `user.Xp` + `user.Level`,
    `check_ins.group_id == category.group_id` even when the client
    payload tries to override it (invariant), non-member throws
    `NotAGroupMemberException` (no row inserted), unknown category
    throws `CategoryNotFoundException`, scoring snapshot round-trip.
  - Implement `apps/api/src/CheckIns/Services/CheckInService.cs`
    per `spec.md` §4.1 (single transaction, post-commit streak
    query).
  - Register in `CheckInsModule.AddCheckIns()`; wire into
    `Program.cs` (`.AddCheckIns()`).
  - Green.

- [ ] **T-005-07 `[T][S]`** — `ICheckInRepository` +
  `CheckInRepository` (list with cursor)
  - Depends on T-005-01.
  - Failing test
    (`apps/api.Tests/CheckIns/CheckInRepositoryTests.cs`):
    scoped to caller (never leaks another user), ordered
    `performed_at DESC`, cursor round-trip returns non-overlapping
    pages, `group_id` filter narrows results.
  - Implement under `apps/api/src/CheckIns/Repositories/`.
  - Green.

## Section F — HTTP endpoints

- [ ] **T-005-08 `[T][S]`** — `POST /check_ins`
  - Depends on T-005-06.
  - Failing test
    (`apps/api.Tests/CheckIns/CreateCheckInEndpointTests.cs`):
    201 happy + payload shape (check_in + user); 401 no token;
    403 non-member; 404 unknown category; 422 `notes` too long /
    `duration <= 0`.
  - Add
    `apps/api/src/CheckIns/Endpoints/CreateCheckInEndpoint.cs`
    + DTOs; wire in `CheckInsModule.UseCheckIns()`.
  - Green.

- [ ] **T-005-09 `[T][S]`** — `GET /check_ins`
  - Depends on T-005-07.
  - Failing test
    (`apps/api.Tests/CheckIns/ListCheckInsEndpointTests.cs`):
    returns caller's rows only, ordered desc, cursor round-trip,
    `group_id` filter respected.
  - Add
    `apps/api/src/CheckIns/Endpoints/ListCheckInsEndpoint.cs`.
  - Green.

## Section G — Mobile

- [ ] **T-005-10 `[T][S]`** — Check-in API client
  - Add
    `apps/mobile/src/services/interfaces/checkin.interface.ts`,
    `apps/mobile/src/services/checkin.service.ts`,
    `apps/mobile/src/hooks/useCheckIn.ts` (mutation + list query).
  - Vitest with MSW: mutation posts, list paginates, cache
    invalidation triggers on success.
  - Green.

- [ ] **T-005-11 `[T][S]`** — Check-in screen
  - Depends on T-005-10.
  - Failing spec
    (`apps/mobile/test/app/(app)/checkin/page.spec.tsx`): renders
    category grid, submit disabled without selection, submitting
    fires the mutation with `{category_id}`.
  - Implement
    `apps/mobile/src/app/(app)/checkin/page.tsx` (replaces the
    current `ComingSoon`) +
    `apps/mobile/src/components/organisms/CheckinForm.tsx`.
  - Green.

- [ ] **T-005-12 `[T][S]`** — Level-up overlay
  - Depends on T-005-11.
  - Failing spec: overlay renders when the response has
    `leveled_up === true`; hides otherwise; dismisses on tap.
  - Implement
    `apps/mobile/src/components/atoms/LevelUpOverlay.tsx`; wire
    into check-in success flow.
  - Green.

- [ ] **T-005-13 `[T][P]`** — Profile refresh
  - Depends on T-005-11.
  - Spec: after check-in mutation, `useMe` cache is invalidated;
    profile shows fresh `xp` / `level` on next render.
  - Wire cache invalidation in `useCheckIn`.
  - Green.

## Section H — Wrap-up

- [ ] **T-005-14 `[P]`** — ADR `docs/adr/0005-check-in-and-streak.md`
  - Derived-not-cached streak, per-group scope.
  - Group scoping of check-ins (denormalized `group_id`).
  - `scoring_snapshot` structured shape (forward-compat with
    Phase 7 multipliers).
  - Cursor pagination shape.
  - Membership enforcement layer (service, not middleware).
  - `group_configs` as extensibility target; why not `xp_config`
    generic table today.
  - Streak calc supporting only `"daily"` in Phase 5; weekly path
    schema-ready.

- [ ] **T-005-15 `[S]`** — Phase close
  - `dotnet test apps/api.Tests` green.
  - `pnpm --filter @xpeak/mobile test` green.
  - `dotnet format --verify-no-changes` clean on both projects.
  - `pnpm --filter @xpeak/mobile lint` clean.
  - CI green on the PR.
  - Flip Phase 5 to `🛠️` in `specs/roadmap.md`; remove from the
    "still pending stack rewrite" list.
  - Update this file: `[ ]` → `[x] ✅ commit <hash>` on each task.

---

## Dependencies

```
A ──▶ B ──▶ D ──▶ E ──▶ F ──▶ G ──▶ H
      │       ▲
      └─── C ─┘
```

- B (Groups extensions) + C (Progression extensions) can start in
  parallel once A lands.
- D (Streak calc) is pure and can start any time after A; D → E
  (Streak service needs config from B and check-ins table from A).
- G (Mobile) can begin after F (endpoints) is green — MSW mocks
  don't need the real API but the shape is locked by F.

## Bundling strategy for commits

Branch: `XPK-16/felpa-checkin`.

1. `feat(check-ins): add schema, user timezone and group configs`
   — T-005-01.
2. `feat(groups): extend groups module with membership and config services`
   — T-005-02.
3. `feat(progression): extend progression service with by-id lookups`
   — T-005-03.
4. `feat(check-ins): add streak calculator and streak service`
   — T-005-04, T-005-05.
5. `feat(check-ins): add check-in service and repository`
   — T-005-06, T-005-07.
6. `feat(check-ins): add http endpoints`
   — T-005-08, T-005-09.
7. `feat(mobile): add check-in screen and level-up overlay`
   — T-005-10 → T-005-13.
8. `docs(adr): check-in and streak` — T-005-14 + T-005-15.
