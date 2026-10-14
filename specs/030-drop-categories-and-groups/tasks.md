# Phase 30 — Drop Categories and Groups (TASKS)

> Stack: ASP.NET Core 9 + EF Core 9 + Postgres 16 + xUnit (api);
> Next.js 16 + Capacitor + TanStack Query + Vitest (mobile).

## Legend

- `[T]` TDD (failing test first, show red, then implement).
- `[S]` sequential (must run in listed order).
- `[P]` parallel (safe to run in any order or in parallel with peers).
- `[HUMAN]` human-only step.
- `[ ]` not started, `[~]` in progress, `[x] ✅ commit <hash>` done.

---

## Section A — Pure XP core

- [ ] **T-030-01 `[T][S]`** — `XpCalculator.CheckInXp` constant
  - Depends on nothing.
  - Failing test
    (`apps/api.Tests/Xp/XpCalculatorTests.cs`):
    `XpCalculator.CheckInXp` equals `10`;
    `XpCalculator.Compute()` returns `CheckInXp`.
  - Replace `XpCalculator.Compute(XpRule)` with
    `XpCalculator.Compute()` returning the constant. Delete the
    `XpRule` parameter from every caller — most will shrink in
    the next tasks.
  - Green.

## Section B — CheckIn flow (backend)

- [ ] **T-030-02 `[T][S]`** — `CheckInService` without category/group
  - Depends on T-030-01.
  - Failing tests
    (`apps/api.Tests/CheckIns/CheckInServiceTests`,
    `CheckInPersistenceTests`): happy path arranges only a user,
    posts a check-in with the slimmed input, asserts
    `XpEarned == XpCalculator.CheckInXp` and that the row is
    persisted without `CategoryId` / `GroupId`.
  - Update `CreateCheckInInput` / `CreateCheckInRequest` to
    drop both ids.
  - Rework `CheckInService.CreateAsync` + `PublishAsync` to
    call `XpCalculator.Compute()` directly. Delete the
    Progression lookups for category and rule.
  - Delete `CategoryNotFoundException`.
  - Green.

- [ ] **T-030-03 `[T][S]`** — `POST /check_ins` validator + endpoint
  - Depends on T-030-02.
  - Failing tests
    (`apps/api.Tests/CheckIns/CreateCheckInEndpointTests`):
    endpoint 201s on the slim body; 422 reasons contain neither
    `categoryId` nor `groupId`.
  - Drop both fields from `CreateCheckInRequest` +
    `CreateCheckInRequestValidator`.
  - Green.

- [ ] **T-030-04 `[T][S]`** — Streak per user
  - Depends on T-030-02.
  - Failing tests
    (`apps/api.Tests/CheckIns/StreakServiceTests`): per-user
    arrangement, three consecutive days reports `Current = 3`;
    a one-day gap resets to `Current = 1`.
  - Add `IStreakService.GetForUserAsync(userId, ct)`; drop the
    group overload.
  - Rewrite `StreakService` to read `check_ins` filtered by
    `user_id` only.
  - Introduce `StreakRules.Mode = StreakMode.Daily` as a
    constant; drop `StreakConfig` consumption.
  - Green.

## Section C — Progression + Groups cleanup

- [ ] **T-030-05 `[T][S]`** — Delete `Category` module
  - Depends on T-030-02.
  - Delete entity, repository, interface, endpoint, DTO,
    exception, module registration lines, tests
    (`CategoryRepositoryTests`, `CategoryPersistenceTests`,
    `ListGroupCategoriesEndpointTests`).
  - Drop `DbSet<Category>` + model config from `AppDbContext`.
  - Drop `IProgressionService.GetCategoryByIdAsync`,
    `ListActiveCategoriesAsync`, `GetCategoryBySlugAsync`.
  - Green — backend suite compiles and runs clean against the
    pre-migration schema (the categories table is still there
    for one more task).

- [ ] **T-030-06 `[T][S]`** — Delete `XpRule` entity + repository
  - Depends on T-030-05.
  - Delete `XpRule`, `IXpRuleRepository`, `XpRuleRepository`,
    `XpRulePersistenceTests`, `XpRuleRepositoryTests`.
  - Drop `DbSet<XpRule>` + model config.
  - Drop `IProgressionService.GetRuleAsync` and
    `ComputeXp(XpRule)`.
  - Green.

- [ ] **T-030-07 `[T][S]`** — Delete `Group*` entities + services
  - Depends on T-030-04.
  - Delete `Group`, `GroupMembership`, `GroupConfig`,
    `StreakConfig`, `IGroupService`, `GroupService`,
    `GroupIds`.
  - Drop `DbSet<Group>`, `DbSet<GroupMembership>`,
    `DbSet<GroupConfig>` + model configs.
  - Remove auto-membership calls from `RegisterEndpoint` and
    `GoogleAccountLinker.FindOrCreateAsync` (and update their
    tests to drop the "user joined global" assertion).
  - Green.

## Section D — Schema

- [ ] **T-030-08 `[T][S]`** — `DropCategoriesAndGroups` migration
  - Depends on T-030-07.
  - Failing test
    (`apps/api.Tests/Infrastructure/DropCategoriesAndGroupsMigrationTests.cs`):
    fresh Testcontainers Postgres, run every migration through
    this one, assert `categories`, `xp_rules`,
    `group_memberships`, `group_configs`, `groups` are gone,
    `check_ins` no longer has `category_id` or `group_id`, and
    `challenges` / `challenge_memberships` are intact.
  - `dotnet ef migrations add DropCategoriesAndGroups`.
  - Hand-edit the migration body if the EF generator leaves
    anything stale (constraint names).
  - Keep `AppDbContextModelSnapshot` in sync.
  - Green.

## Section E — Mobile data layer

- [ ] **T-030-09 `[T][S]`** — Shared types + checkin service
  - Depends on T-030-08.
  - Drop `GLOBAL_GROUP_ID`, `Category`, `GroupListItem` and the
    `/groups`-related response types from
    `packages/shared/src/*` and the barrel export.
  - Drop `listCategories` from
    `apps/mobile/src/services/interfaces/checkin.interface.ts`
    and `apps/mobile/src/services/checkin.service.ts`; drop
    `category_id` + `group_id` from the create payload.
  - Update `apps/mobile/test/services/checkin.service.spec.ts`
    (if present) to match.
  - Green.

- [ ] **T-030-10 `[T][S]`** — `useCheckinForm` schema + submit
  - Depends on T-030-09.
  - Failing spec
    (`apps/mobile/test/components/organisms/CheckinForm.spec.tsx`):
    "blocks submit until a category is picked" case removed;
    happy path captures the API payload and asserts no
    `category_id` / `group_id`.
  - Drop `categoryId` from `defaultValues`, zod schema, submit
    payload and the `CHECKIN_MESSAGES.categoryRequired` entry.
  - Green.

## Section F — Mobile presentation

- [ ] **T-030-11 `[T][S]`** — `CheckinForm` without the picker
  - Depends on T-030-10.
  - Delete `<ControlledCategoryPicker>` + its spec +
    `useCategories` from `useCheckIn.ts`.
  - Strip the picker import + usage from `CheckinForm.tsx`.
  - Green — `pnpm --filter @xpeak/mobile test`, `lint` and
    `build` all clean.

## Section G — Docs & wrap-up

- [ ] **T-030-12 `[P]`** — ADR `docs/adr/0030-drop-groups-and-categories.md`
  - Captures the decision, the alternatives (keep both /
    rename / collapse), the Phase 19 reinterpretation, the
    hardcoded XP constant trade-off and the migration name.

- [ ] **T-030-13 `[P]`** — Patch prior specs
  - Add the historical banners defined in
    `specs/030-drop-categories-and-groups/spec.md` §7 to Phases
    4, 5, 7, 19 and update `specs/roadmap.md` Phase 19 heading.

- [ ] **T-030-14 `[S]`** — Phase close
  - `dotnet test apps/api.Tests` green.
  - `pnpm --filter @xpeak/mobile test` green.
  - `dotnet format --verify-no-changes` clean on `apps/api` +
    `apps/api.Tests`.
  - `pnpm --filter @xpeak/mobile lint` clean.
  - `pnpm --filter @xpeak/mobile build` succeeds (static export).
  - CI green on the PR.

---

## Dependencies

```
A1 ──▶ B1 ──▶ B2
         │
         ├──▶ B3 (streak per user)
         │
         └──▶ C1 (delete Category) ──▶ C2 (delete XpRule)
                                            │
         B3 ──▶ C3 (delete Group*)          │
                   │                        │
                   ▼                        ▼
                  D1 (migration) ◄──────────┘
                        │
                        ▼
                  E1 (shared/services) ──▶ E2 (form schema)
                                               │
                                               ▼
                                              F1 (form UI)
                                               │
                                               ▼
                                           G1 (ADR, opt),
                                           G2 (spec patches),
                                           G3 (phase close)
```

## Bundling strategy for PRs

Single bundle PR. Splitting leaves intermediate states where the
API has dropped `category_id` but the migration hasn't run, or
mobile sends fields the API 400s on. Testcontainers + the end-
to-end suite is the only safety net; keep every layer in one
commit range.

Suggested commit sequence inside the PR:

1. `refactor(xp): hardcode CheckInXp constant` — A1
2. `refactor(check-ins): drop category and group ids from input` — B1 + B2
3. `refactor(check-ins): derive streak per user` — B3
4. `refactor(progression): delete Category entity and endpoint` — C1
5. `refactor(progression): delete XpRule entity` — C2
6. `refactor(groups): delete Group entity, membership, config` — C3
7. `feat(db): migration drops categories, xp_rules, groups` — D1
8. `refactor(mobile): drop category + group from shared and services` — E1
9. `refactor(mobile): drop category picker from check-in form` — E2 + F1
10. `docs(adr): drop groups and categories (phase 30)` — G1
11. `docs(specs): patch phases 4/5/7/19 and roadmap` — G2
