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

- [ ] **T-004-02 `[T][P]`** — `XpCalculator`
  - Add `FsCheck.Xunit` to `apps/api.Tests/api.Tests.csproj`
    (first use of property-based testing in the repo).
  - Failing test (`XpCalculatorTests.cs`): xUnit `[Theory]` + FsCheck
    property (`Compute > 0` for any valid rule; monotonic on `BaseXp`
    holding weight constant).
  - Implement `apps/api/src/Progression/Services/XpCalculator.cs`
    (static, `Math.Round(..., MidpointRounding.AwayFromZero)`,
    signature `Compute(XpRule rule)`).
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
    `ListActiveAsync(groupId)` excludes rows where `active = false`;
    `GetBySlugAsync(groupId, "Legs")` matches the row with slug
    `legs` (case-insensitive via `citext`); rows in a different
    group are not returned.
  - Implement under `apps/api/src/Progression/Repositories/`.
  - Green.

- [ ] **T-004-06 `[T][S]`** — `IProgressionService` +
  `ProgressionService` + `ProgressionModule.AddProgression()`
  - Depends on T-004-02..05.
  - Failing test (`ProgressionServiceTests.cs`, Testcontainers):
    seed a synthetic group + rule + categories, verify
    `ListActiveCategoriesAsync(groupId)` returns them,
    `GetCategoryBySlugAsync(groupId, "legs")` returns the entity,
    `ComputeXp(loadedRule)` returns the expected value,
    `LevelForXp(300)` returns `1`.
  - Implement the service (delegation only, no logic).
  - Add `ProgressionModule.cs` with `AddProgression()` extension.
  - Wire `.AddProgression()` into `Program.cs` at the end of the
    service chain.
  - Green.

## Section D — Wrap-up

- [ ] **T-004-07 `[P]`** — ADR `docs/adr/0004-xp-engine.md`
  - Curve choice (`n^1.5`) with alternatives considered.
  - Rounding choice (`MidpointRounding.AwayFromZero`).
  - Weight bounds `[0.5, 2.5]` rationale.
  - Why the module exports only `IProgressionService` /
    `IGroupService` (not repository, not pure services).
  - **Rule extraction** — why `xp_rules` is a separate entity from
    `categories`, and how Phase 7 group bonuses / Phase 15
    challenge modifiers / Phase 9 user overrides all reuse the shape.
  - **Group scoping** — why every category is owned by a group and
    every user joins Global on register, versus a flat "system"
    boolean.
  - **No seed** — why the engine ships without any category rows and
    what fills that gap later (Phase 9 admin surface / Phase 17
    group-owner CRUD).
  - Tunability path via Phase 9 (values on `xp_rules` become editable
    via admin surface + audit log).

- [ ] **T-004-08 `[S]`** — Phase close
  - `dotnet test apps/api.Tests` green.
  - `dotnet format --verify-no-changes` clean on both `apps/api` and
    `apps/api.Tests`.
  - CI green on the PR.
  - Flip Phase 4 to `🛠️` in `specs/roadmap.md`; remove from the
    "still pending stack rewrite" list.
  - Update this file: `[ ]` → `[x] ✅ commit <hash>` on each task.

---

## Dependencies

```
A ──▶ B ──▶ C ──▶ D
      │
      └─▶ (B tasks can run in parallel among themselves; C.05 needs A)
```

## Bundling strategy for commits

Each bundle = 1 stage for the human to review. Suggested branch:
`XPK-15/felpa-xp-domain`.

1. ✅ `feat(groups): add root group, memberships and rule-scoped categories`
   — T-004-01 (`9317486`).
2. `feat(progression): add xp calculator and level curve` —
   T-004-02, T-004-03.
3. `feat(progression): add level up service, repository and context` —
   T-004-04, T-004-05, T-004-06.
4. `docs(adr): xp engine (curve, rounding, rule extraction, group scoping)`
   — T-004-07.
5. Phase-close housekeeping (roadmap flip + tasks.md hashes) rides
   with bundle 4 or lands separately.
