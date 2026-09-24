# Phase 4 — Categories & Pure XP Domain (TASKS)

> Stack: C# 12 + ASP.NET Core 9 + EF Core 9 + Postgres 16 + xUnit + FsCheck.

## Legend

- `[T]` TDD (write failing test first, show red, then implement).
- `[S]` sequential (must run in listed order).
- `[P]` parallel (safe to run in any order or in parallel with peers).
- `[HUMAN]` human-only step (do not attempt to automate).
- `[ ]` not started, `[~]` in progress, `[x] ✅ commit <hash>` done.

---

## Section A — Groups, rules, categories & auth hook

- [x] ✅ commit `9317486` **T-004-01 `[T][S]`** — Groups module,
  XpRule + Category entities, migration and register/OAuth hook
  - Failing tests (compile-error red on missing namespaces, then
    behavioural red): Group root seed, membership composite PK,
    cascade on user delete, `GroupService` idempotency, XpRule CHECK
    constraints (weight bounds + negative base_xp), Category FK
    presence, per-group slug uniqueness, rule sharing.
  - Introduced `Xpeak.Api.Groups` module (`GroupIds`, `Entities/`,
    `Services/`, `GroupsModule`).
  - Introduced `Xpeak.Api.Progression.Entities` with `XpRule` and
    `Category` (Category holds `GroupId` + `XpRuleId` FKs, not raw
    scoring numbers).
  - Extended `AppDbContext`: `DbSet<Group>`, `DbSet<GroupMembership>`,
    `DbSet<XpRule>`, `DbSet<Category>`, snake_case mapping, CHECK
    constraints on `xp_rules`, unique `(group_id, slug)`, seed of the
    Global group.
  - `dotnet ef migrations add CreateGroupsRulesAndCategories`.
  - `Program.cs` chain gains `.AddGroups()` right after `.AddUsers()`.
  - `RegisterEndpoint` and `GoogleAccountLinker` call
    `IGroupService.AddUserToGlobalAsync` after account creation.
  - `RegisterEndpointTests` gains a Global-membership assertion;
    `GoogleOAuthEndpointTests` new-user path extended likewise.
  - 79 tests total (64 pre-existing + 15 new). Zero regression.
  - `dotnet format --verify-no-changes` clean on both projects.

## Section B — Pure domain services

- [x] ✅ commit `1319aab` **T-004-02 `[T][P]`** — `XpCalculator`
  - `FsCheck.Xunit@3.3.0` added to `apps/api.Tests/api.Tests.csproj`
    (first property-based tests in the repo).
  - `XpCalculatorTests.cs` (10): table-driven cases mirroring the
    reference weights, banker-vs-away rounding proof, two FsCheck
    properties (`Compute > 0` for any valid rule, monotonic on
    `BaseXp` when weight is fixed).
  - `apps/api/src/Progression/Services/XpCalculator.cs` implemented
    (static, `Math.Round(..., MidpointRounding.AwayFromZero)`,
    signature `Compute(XpRule rule)`).

- [x] ✅ commit `1319aab` **T-004-03 `[T][P]`** — `LevelCurve`
  - `LevelCurveTests.cs` (9): zero, 5 reference values from
    `spec.md` §6.2, negative rejection, two FsCheck properties
    (strictly increasing for positive levels, non-decreasing from
    zero).
  - `apps/api/src/Progression/Services/LevelCurve.cs` implemented
    (static, `XpForLevel(0) = 0`, `XpForLevel(n) = Round(100 * n^1.5)`
    for `n >= 1`, throws on negative).

- [x] ✅ commit `c886eee` **T-004-04 `[T][S]`** — `LevelUpService`
  - `LevelUpService.cs` implemented (static, exponential-then-binary
    search over `LevelCurve.XpForLevel`, `EvaluateLevelUp(prev, new)`
    returns `LevelUpResult`).
  - `LevelUpResult.cs` — readonly record struct
    `(LeveledUp, NewLevel, LevelsGained)`.
  - `LevelUpServiceTests.cs` (13): 11 table-driven `LevelForXp`
    cases (0, 99, 100, 282, 283, 300, 519, 520, 3161, 3162, 100000),
    negative rejection, FsCheck round-trip property, and 5
    `EvaluateLevelUp` boundary cases (exact crossing, no-op, jump,
    no-change, zero-to-first).

## Section C — Repository & public context

- [x] ✅ commit `c886eee` **T-004-05 `[T][S]`** — `ICategoryRepository`
  + `CategoryRepository`
  - `ICategoryRepository` / `CategoryRepository` under
    `apps/api/src/Progression/Repositories/`. All reads take
    `groupId` as first argument (no cross-group reads).
  - `CategoryRepositoryTests.cs` (5, Testcontainers):
    `ListActiveAsync` excludes `active = false` and is group-scoped;
    `GetBySlugAsync` matches case-insensitively via citext, returns
    null across groups and for missing slugs.

- [x] ✅ commit `c886eee` **T-004-06 `[T][S]`** — `IProgressionService`
  + `ProgressionService` + `ProgressionModule.AddProgression()`
  - `IProgressionService` / `ProgressionService` — composition seam
    only, no business logic. Category reads through the repo;
    arithmetic through the static services.
  - `ProgressionModule.AddProgression()` extension, wired into
    `Program.cs` after `.AddGroups()`.
  - `ProgressionServiceTests.cs` (1 end-to-end integration): resolve
    via DI, seed a synthetic group + rule + category, verify
    listing, `GetBySlug`, `ComputeXp(15,1.4)=21`,
    `XpForLevel(10)=3162`, `LevelForXp(3162)=10`,
    `EvaluateLevelUp(99,100)=(true,1,1)` in one pass.

## Section D — Wrap-up

- [x] **T-004-07 `[P]`** — ADR `docs/adr/0004-xp-engine.md`
  (this commit) — covers curve choice, rounding, rule extraction,
  group scoping, no-seed decision, module public surface, and the
  tunability path via Phase 9.

- [x] **T-004-08 `[S]`** — Phase close (this commit)
  - `dotnet test apps/api.Tests` green (122 tests).
  - `dotnet format --verify-no-changes` clean on both projects.
  - Phase 4 flipped to `🛠️` in `specs/roadmap.md`.
  - This file updated with commit hashes for every task.

---

## Dependencies

```
A ──▶ B ──▶ C ──▶ D
      │
      └─▶ (B tasks can run in parallel among themselves; C.05 needs A)
```

## Bundling strategy for commits

Branch: `XPK-15/felpa-xp-domain`.

1. ✅ `feat(groups): add root group, memberships and rule-scoped categories`
   — T-004-01 (`9317486`).
2. ✅ `docs(spec): rewrite phase 4 docs to match shipped model`
   — mid-flight spec sync (`5eebae9`).
3. ✅ `feat(progression): add xp calculator and level curve` —
   T-004-02, T-004-03 (`1319aab`).
4. ✅ `feat(progression): add level up service, repository and context`
   — T-004-04, T-004-05, T-004-06 (`c886eee`).
5. ✅ `docs(adr): xp engine + phase close` — T-004-07 + T-004-08
   (this commit).
