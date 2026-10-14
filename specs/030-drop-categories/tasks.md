# Phase 30 — Drop Categories (TASKS)

> Stack: ASP.NET Core 9 + EF Core 9 + Postgres 16 + xUnit (api);
> Next.js 16 + Capacitor + TanStack Query + Vitest (mobile).

## Legend

- `[T]` TDD (failing test first, show red, then implement).
- `[S]` sequential (must run in listed order).
- `[P]` parallel (safe to run in any order or in parallel with peers).
- `[HUMAN]` human-only step.
- `[ ]` not started, `[~]` in progress, `[x] ✅ commit <hash>` done.

---

## Section A — Progression domain

- [ ] **T-030-01 `[T][S]`** — `XpRuleIds.Default` constant + `GetDefaultRuleAsync`
  - Depends on nothing.
  - Failing test
    (`apps/api.Tests/Progression/XpRuleDefaultTests.cs`):
    `IProgressionService.GetDefaultRuleAsync(ct)` returns the
    seeded row with id `00000000-0000-0000-0000-000000000010`;
    removing the row makes the call return `null`.
  - Add `apps/api/src/Progression/XpRuleIds.cs` holding the Guid
    constant.
  - Extend `IProgressionService` + `ProgressionService` with
    `GetDefaultRuleAsync`.
  - Green.

## Section B — CheckIn flow (backend)

- [ ] **T-030-02 `[T][S]`** — `CheckInService` consumes the default rule
  - Depends on T-030-01.
  - Failing test
    (`apps/api.Tests/CheckIns/CheckInServiceTests`): existing
    create/publish tests, arranged WITHOUT a category and WITHOUT
    `CategoryId` in the input, award XP matching
    `XpCalculator.Compute(defaultRule)`; throws
    `InvalidOperationException` when the default rule row is
    missing.
  - Update `CreateCheckInInput` / request DTO: drop `CategoryId`.
  - Rework `CheckInService.CreateAsync` + `PublishAsync` to load
    the default rule via `GetDefaultRuleAsync` instead of the
    category lookup.
  - Delete `CategoryNotFoundException`.
  - Green.

- [ ] **T-030-03 `[T][S]`** — `POST /check_ins` DTO + validator
  - Depends on T-030-02.
  - Failing test
    (`apps/api.Tests/CheckIns/CreateCheckInEndpointTests`): endpoint
    accepts bodies without `categoryId`, 201 on happy path, 422
    reasons list no longer mentions `categoryId`.
  - Drop `CategoryId` from `CreateCheckInRequest` +
    `CreateCheckInRequestValidator`.
  - Green.

## Section C — Schema drop

- [ ] **T-030-04 `[T][S]`** — `DropCategories` migration
  - Depends on T-030-03.
  - Failing test
    (`apps/api.Tests/Infrastructure/MigrationSmokeTests` — or
    equivalent): migration chain runs clean on a fresh
    Testcontainers Postgres; `categories` table does not exist;
    `check_ins.category_id` column does not exist; the
    `xp_rules` default row survives.
  - `dotnet ef migrations add DropCategories`.
  - Drop FK, drop column, drop table (per spec §2.4).
  - Keep the designer snapshot + `AppDbContextModelSnapshot` in
    sync.
  - Green.

- [ ] **T-030-05 `[T][S]`** — Delete `Category` entity + repository + endpoint
  - Depends on T-030-04.
  - Delete `apps/api/src/Progression/Entities/Category.cs`,
    `apps/api/src/Progression/Repositories/CategoryRepository.cs`,
    `apps/api/src/Progression/Endpoints/ListGroupCategoriesEndpoint.cs`,
    `apps/api/src/Progression/Dto/CategoryResponse.cs` (and any
    other category-only DTO).
  - Drop `DbSet<Category>` from `AppDbContext` + the model config.
  - Drop `IProgressionService.GetCategoryByIdAsync` (and the
    callers already migrated in T-030-02 should be clean).
  - Delete `apps/api.Tests/Progression/CategoryRepositoryTests.cs`
    and `ListGroupCategoriesEndpointTests.cs`.
  - Green — full backend suite must pass.

## Section D — Mobile data layer

- [ ] **T-030-06 `[T][S]`** — Shared types + checkin service
  - Depends on T-030-05.
  - Delete `Category` from `packages/shared/src/progression/types.ts`
    (and the barrel re-export).
  - Drop `listCategories` from
    `apps/mobile/src/services/interfaces/checkin.interface.ts` and
    `apps/mobile/src/services/checkin.service.ts`.
  - Drop the `category_id` field from the `createCheckIn` payload.
  - Update `apps/mobile/test/services/checkin.service.spec.ts` to
    match (if the file exists).
  - Green.

- [ ] **T-030-07 `[T][S]`** — `useCheckinForm` schema + submit
  - Depends on T-030-06.
  - Failing spec
    (`apps/mobile/test/hooks/useCheckinForm.spec.ts` or the
    equivalent in `CheckinForm.spec.tsx`): the form submits with
    no `categoryId`; zod schema no longer errors on an empty
    `categoryId`; `CHECKIN_MESSAGES.categoryRequired` gone.
  - Drop `categoryId` from `defaultValues`, zod schema, submit
    payload and the message record.
  - Green.

## Section E — Mobile presentation

- [ ] **T-030-08 `[T][S]`** — `CheckinForm` without the picker
  - Depends on T-030-07.
  - Failing spec
    (`apps/mobile/test/components/organisms/CheckinForm.spec.tsx`):
    "blocks submit until a category is picked" case is removed;
    the happy-path spec still submits and the API payload has no
    `category_id`.
  - Delete `<ControlledCategoryPicker>` import + usage from
    `CheckinForm.tsx`.
  - Delete `apps/mobile/src/components/atoms/ControlledCategoryPicker.tsx`
    and its spec.
  - Delete `useCategories` from `apps/mobile/src/hooks/useCheckIn.ts`
    (keep the file if it still exports `useCreateCheckIn`).
  - Green — `pnpm --filter @xpeak/mobile test` + `lint` clean.

## Section F — Docs & wrap-up

- [ ] **T-030-09 `[P]`** — ADR `docs/adr/0030-drop-categories.md`
  - Captures: the single-default-rule decision, why challenges
    + streak are enough, the migration plan, the "no 410 Gone,
    unreleased mobile" call-out.

- [ ] **T-030-10 `[P]`** — Patch prior specs
  - Add historical note to
    `specs/004-categories-and-xp-domain/spec.md` (per spec §7).
  - Add historical note to `specs/005-checkin-no-media/spec.md`.

- [ ] **T-030-11 `[S]`** — Phase close
  - `dotnet test apps/api.Tests` green.
  - `pnpm --filter @xpeak/mobile test` green.
  - `dotnet format --verify-no-changes` clean on `apps/api` and
    `apps/api.Tests`.
  - `pnpm --filter @xpeak/mobile lint` clean.
  - `pnpm --filter @xpeak/mobile build` succeeds (next.js static
    export).
  - CI green on the PR.

---

## Dependencies

```
A1 ──▶ B1 ──▶ B2 ──▶ C1 ──▶ C2 ──▶ D1 ──▶ D2 ──▶ E1
                                                   │
                                                   ▼
                                                  F1 (opt), F2, F3
```

## Bundling strategy for PRs

Single bundle PR. The drop touches too many layers to split
without leaving an inconsistent middle state (migration out but
service still looking up Category, or mobile without picker but
API still 422-ing on missing field).

Suggested commit sequence inside the PR:

1. `feat(progression): seed-aware default XpRule accessor` — A1
2. `refactor(check-ins): consume default XpRule, drop CategoryId input` — B1 + B2
3. `feat(db): migration drops categories table + check-in column` — C1
4. `refactor(progression): delete Category entity/repository/endpoint` — C2
5. `refactor(mobile): drop category from checkin service + form` — D1 + D2 + E1
6. `docs(adr): drop categories (phase 30)` — F1
7. `docs(specs): historical notes on phases 4 and 5` — F2
