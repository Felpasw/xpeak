# Phase 4 — Categories & Pure XP Domain (TASKS)

> Stack: C# 12 + ASP.NET Core 9 + EF Core 9 + Postgres 16 + xUnit + FsCheck.

## Legend

- `[T]` TDD (write failing test first, show red, then implement).
- `[S]` sequential (must run in listed order).
- `[P]` parallel (safe to run in any order or in parallel with peers).
- `[HUMAN]` human-only step (do not attempt to automate).
- `[ ]` not started, `[~]` in progress, `[x] ✅ commit <hash>` done.

---

## Section A — Category entity, schema & seed

- [ ] **T-004-01 `[T][S]`** — `Category` entity + `categories`
  migration + `HasData` seed
  - Failing test (`apps/api.Tests/Progression/CategoryPersistenceTests.cs`):
    inserting `weight_multiplier = 0.4` throws `DbUpdateException`;
    inserting duplicate slug throws; the 10 seed rows exist after
    migration.
  - Add `apps/api/src/Progression/Entities/Category.cs` (sealed
    class, `[Range(0.5, 2.5)]` on `WeightMultiplier`).
  - Add `DbSet<Category>` to `AppDbContext`; extend `OnModelCreating`
    (snake_case, unique index on `slug`, `CHECK (base_xp > 0)`,
    `CHECK (weight_multiplier >= 0.5 AND weight_multiplier <= 2.5)`,
    `HasData` with the 10 rows from `spec.md` §2.2 using stable
    hard-coded Guids).
  - `dotnet ef migrations add CreateCategories` — verify the
    generated SQL includes `CREATE EXTENSION IF NOT EXISTS citext`
    at the top.
  - Green.

## Section B — Pure domain services

- [ ] **T-004-02 `[T][P]`** — `XpCalculator`
  - Failing test (`XpCalculatorTests.cs`): xUnit `[Theory]` + FsCheck
    property (`Compute > 0`, monotonic on `BaseXp`).
  - Implement `apps/api/src/Progression/Services/XpCalculator.cs`
    (static, `Math.Round(..., MidpointRounding.AwayFromZero)`).
  - Green.

- [ ] **T-004-03 `[T][P]`** — `LevelCurve`
  - Failing test (`LevelCurveTests.cs`): table-driven values from
    `spec.md` §6.2 + FsCheck monotonicity property.
  - Implement `apps/api/src/Progression/Services/LevelCurve.cs`
    (static, `XpForLevel(0) = 0`, `XpForLevel(n) = Round(100 * n^1.5)`
    for `n >= 1`, throw on negative).
  - Green.

- [ ] **T-004-04 `[T][S]`** — `LevelUpService`
  - Depends on T-004-03 (curve must exist to search over).
  - Failing test (`LevelUpServiceTests.cs`): `LevelForXp` round-trip
    property + the 4 `EvaluateLevelUp` boundary cases from `spec.md`
    §6.2.
  - Implement `apps/api/src/Progression/Services/LevelUpService.cs`
    (static, binary search on the curve with exponential upper-bound
    growth).
  - Green.

## Section C — Repository & public context

- [ ] **T-004-05 `[T][S]`** — `ICategoryRepository` +
  `CategoryRepository`
  - Depends on T-004-01.
  - Failing test (`CategoryRepositoryTests.cs`, Testcontainers):
    `ListActiveAsync` excludes an inactive row inserted at runtime;
    `GetBySlugAsync("Legs")` matches the seeded `legs` row
    (case-insensitive via `citext`).
  - Implement under `apps/api/src/Progression/Repositories/`.
  - Green.

- [ ] **T-004-06 `[T][S]`** — `IProgressionService` +
  `ProgressionService` + `ProgressionModule.AddProgression()`
  - Depends on T-004-02..05.
  - Failing test (`ProgressionServiceTests.cs`, Testcontainers):
    `ListActiveCategoriesAsync` returns the 10 seeded rows;
    `ComputeXp(GetCategoryBySlugAsync("legs"))` returns `21`;
    `LevelForXp(300)` returns `1`.
  - Implement the service (delegation only, no logic).
  - Add `ProgressionModule.cs` with `AddProgression()` extension.
  - Wire `.AddProgression()` into `Program.cs` after `.AddUsers()`.
  - Green.

## Section D — Wrap-up

- [ ] **T-004-07 `[P]`** — ADR `docs/adr/0004-xp-engine.md`
  - Curve choice (`n^1.5`) with the alternatives considered.
  - Rounding choice (`MidpointRounding.AwayFromZero`).
  - Weight bounds `[0.5, 2.5]` rationale.
  - Tunability path via Phase 9 (values move from `HasData` seed to
    a `xp_config` table with an admin surface).
  - Why the module exports only `IProgressionService` (not the
    repository, not the pure services directly).

- [ ] **T-004-08 `[S]`** — Phase close
  - `dotnet test apps/api.Tests` green (existing 64 tests + new
    Progression suite).
  - `dotnet format --verify-no-changes` clean on both `apps/api` and
    `apps/api.Tests`.
  - CI green on the PR.
  - Mark Phase 4 as `🛠️` in `specs/roadmap.md`.
  - Update this file: `[ ]` → `[x] ✅ commit <hash>` on each task.

---

## Dependencies

```
A ──▶ B ──▶ C ──▶ D
      │
      └─▶ (B tasks can run in parallel among themselves; C.05 needs A)
```

## Bundling strategy for commits

Each bundle = 1 stage for the human to review, aiming for coherent
review chunks rather than 1 commit per task. Suggested branch:
`XPK-N/felpa-xp-domain` (N assigned when the phase starts).

1. `feat(progression): add categories entity, migration and seed`
   — T-004-01 (schema + seed in one bundle so migration is
   reviewed alongside the data it applies).
2. `feat(progression): add xp calculator and level curve`
   — T-004-02, T-004-03 (both pure, independent, small).
3. `feat(progression): add level up service, repository and context`
   — T-004-04, T-004-05, T-004-06 (closes out the module and wires
   it into `Program.cs`).
4. `docs(adr): xp engine` — T-004-07.
5. Phase-close housekeeping (roadmap + tasks.md hashes) can ride
   with bundle 4 or land separately, at your call.
