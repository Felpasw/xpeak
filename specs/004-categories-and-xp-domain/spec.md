# Phase 4 — Categories & Pure XP Domain (SPEC)

> Stack: C# 12 + ASP.NET Core 9 + EF Core 9 + Postgres 16 + xUnit + FsCheck.

## 1. Summary

Four tables (`groups`, `group_memberships`, `xp_rules`, `categories`)
plus a pure `Xpeak.Api.Progression` module with `XpCalculator`,
`LevelCurve` and `LevelUpService`. Introduces the group scope every
check-in eventually references. **Zero HTTP endpoints, zero UI, zero
category seed.**

## 2. Data model

### 2.1. `groups`

| Column       | Type          | Constraints                             |
|--------------|---------------|-----------------------------------------|
| `id`         | `uuid`        | PK                                      |
| `name`       | `text`        | not null                                |
| `is_root`    | `bool`        | not null, default `false`               |
| `created_at` | `timestamptz` | not null                                |
| `updated_at` | `timestamptz` | not null                                |

**Seed (baked into migration):**

```
id      = 00000000-0000-0000-0000-000000000001
name    = "Global"
is_root = true
```

The seed row is the **only** group with `is_root = true`. The schema
does not enforce single-root at the DB level today; it's an invariant
kept by application code (Phase 12+ will add a partial unique index
when sub-group CRUD lands).

### 2.2. `group_memberships`

| Column      | Type          | Constraints                                 |
|-------------|---------------|---------------------------------------------|
| `user_id`   | `uuid`        | PK (composite), FK → `users.id` cascade     |
| `group_id`  | `uuid`        | PK (composite), FK → `groups.id` cascade    |
| `joined_at` | `timestamptz` | not null                                    |

**Indexes:** secondary index on `group_id` (for "list members of a
group" queries later).

Cascade on both sides — deleting a user or a group drops the row.

### 2.3. `xp_rules`

| Column              | Type            | Constraints                                    |
|---------------------|-----------------|------------------------------------------------|
| `id`                | `uuid`          | PK                                             |
| `base_xp`           | `int`           | not null, `CHECK (base_xp > 0)` — no default   |
| `weight_multiplier` | `numeric(4, 2)` | not null, `CHECK (>=0.5 AND <=2.5)` — no default |
| `created_at`        | `timestamptz`   | not null                                       |
| `updated_at`        | `timestamptz`   | not null                                       |

Rules are the reusable, first-class scoring shape. Any consumer that
needs `(base_xp, weight_multiplier)` — categories today, group bonuses
tomorrow (Phase 7), challenge modifiers later (Phase 15+) — points to
a row here instead of replicating the columns.

**No column defaults.** Callers must supply values explicitly.

### 2.4. `categories`

| Column           | Type          | Constraints                                          |
|------------------|---------------|------------------------------------------------------|
| `id`             | `uuid`        | PK                                                   |
| `group_id`       | `uuid`        | not null, FK → `groups.id` restrict                  |
| `xp_rule_id`     | `uuid`        | not null, FK → `xp_rules.id` restrict                |
| `slug`           | `citext`      | not null                                             |
| `name`           | `text`        | not null                                             |
| `icon_public_id` | `text`        | nullable — Cloudinary asset public id                |
| `active`         | `bool`        | not null, default `true`                             |
| `created_at`     | `timestamptz` | not null                                             |
| `updated_at`     | `timestamptz` | not null                                             |

**Indexes:** unique on `(group_id, slug)`. Each group can register
its own `"legs"` with its own rule.

**Postgres extension:** `CREATE EXTENSION IF NOT EXISTS citext` runs
as part of the migration (case-insensitive `slug` matches the
convention already used by AppUser's normalized username lookup).

**FK behavior:** `restrict` on both. Deleting a group blocks if any
category still points to it; deleting a rule blocks if any category
still uses it. Prevents accidental data loss; explicit cleanup
required.

### 2.5. Seed footprint

Migration inserts **one row** total: the Global group. No `xp_rules`
rows, no `categories` rows.

## 3. Domain services (pure, static)

### 3.1. `XpCalculator`

```csharp
namespace Xpeak.Api.Progression.Services;

public static class XpCalculator
{
    public static int Compute(XpRule rule)
    {
        var raw = rule.BaseXp * (double)rule.WeightMultiplier;
        return (int)Math.Round(raw, MidpointRounding.AwayFromZero);
    }
}
```

Rounding: `MidpointRounding.AwayFromZero` — explicit and intuitive
(`0.5 → 1`, `1.5 → 2`). Rationale captured in ADR-0004.

Takes an `XpRule` rather than a `Category`: the calculator only cares
about the scoring numbers, not the category identity. Callers load
the rule (via the repository) and hand it in.

### 3.2. `LevelCurve`

```csharp
namespace Xpeak.Api.Progression.Services;

public static class LevelCurve
{
    public static int XpForLevel(int level)
    {
        if (level < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(level));
        }

        if (level == 0)
        {
            return 0;
        }

        return (int)Math.Round(
            100 * Math.Pow(level, 1.5),
            MidpointRounding.AwayFromZero);
    }
}
```

Cumulative XP required to reach `level` from level 0. Monotonic and
strictly increasing for `level >= 1`.

### 3.3. `LevelUpService`

```csharp
namespace Xpeak.Api.Progression.Services;

public readonly record struct LevelUpResult(
    bool LeveledUp,
    int NewLevel,
    int LevelsGained);

public static class LevelUpService
{
    // Binary search over LevelCurve.XpForLevel; upper bound grows
    // until XpForLevel(hi) > xp. Cheap since the curve is
    // strictly increasing.
    public static int LevelForXp(int xp) { /* ... */ }

    public static LevelUpResult EvaluateLevelUp(int prevXp, int newXp)
    {
        var prev = LevelForXp(prevXp);
        var next = LevelForXp(newXp);
        return new LevelUpResult(next > prev, next, next - prev);
    }
}
```

## 4. Public contexts

### 4.1. `IGroupService` (`Xpeak.Api.Groups.Services`)

The **only** thing exported by `GroupsModule` for now. Consumers
depend on this interface, not on the concrete service or the
`AppDbContext`.

```csharp
public interface IGroupService
{
    Task AddUserToGlobalAsync(Guid userId, CancellationToken ct = default);
}
```

Semantics: idempotent — an existing membership short-circuits with
no work. Called by both `RegisterEndpoint` and
`GoogleAccountLinker.FindOrCreateAsync` (new-user branch).

### 4.2. `IProgressionService` (`Xpeak.Api.Progression`)

The **only** thing exported by `ProgressionModule`. Composes
`ICategoryRepository` with the static pure services.

```csharp
public interface IProgressionService
{
    Task<IReadOnlyList<Category>> ListActiveCategoriesAsync(
        Guid groupId,
        CancellationToken ct = default);

    Task<Category?> GetCategoryBySlugAsync(
        Guid groupId,
        string slug,
        CancellationToken ct = default);

    int ComputeXp(XpRule rule);

    int XpForLevel(int level);

    int LevelForXp(int xp);

    LevelUpResult EvaluateLevelUp(int prevXp, int newXp);
}
```

Category reads are group-scoped from the start — the check-in phase
(Phase 5) will pass the acting group's id.

## 5. Module layout

```
apps/api/src/Groups/
  Entities/
    Group.cs
    GroupMembership.cs
  Services/
    IGroupService.cs
    GroupService.cs
  GroupIds.cs
  GroupsModule.cs

apps/api/src/Progression/
  Entities/
    XpRule.cs
    Category.cs
  Repositories/
    ICategoryRepository.cs
    CategoryRepository.cs
  Services/
    XpCalculator.cs
    LevelCurve.cs
    LevelUpService.cs
    IProgressionService.cs
    ProgressionService.cs
  ProgressionModule.cs
```

**DI registration:**

- `GroupsModule.AddGroups()` — `AddScoped<IGroupService, GroupService>()`.
- `ProgressionModule.AddProgression()` —
  `AddScoped<ICategoryRepository, CategoryRepository>()` +
  `AddScoped<IProgressionService, ProgressionService>()`.
- `Program.cs` chain: `.AddUsers().AddGroups().AddAuthCore(...).AddPasswordAuth().AddGoogleAuth(...).AddProgression()`.
  Groups must register before AuthCore so the register endpoint can
  resolve `IGroupService` from DI.

## 6. Test plan

### 6.1. Test project structure

```
apps/api.Tests/Groups/
  GroupPersistenceTests.cs           # Testcontainers
  GroupMembershipPersistenceTests.cs # Testcontainers
  GroupServiceTests.cs               # Testcontainers

apps/api.Tests/Progression/
  XpRulePersistenceTests.cs          # Testcontainers
  CategoryPersistenceTests.cs        # Testcontainers
  CategoryRepositoryTests.cs         # Testcontainers
  XpCalculatorTests.cs               # xUnit + FsCheck property tests
  LevelCurveTests.cs                 # xUnit + FsCheck property tests
  LevelUpServiceTests.cs             # xUnit, table-driven
  ProgressionServiceTests.cs         # Testcontainers, integration
```

Auth-side test extensions (already merged in T-004-01):

```
apps/api.Tests/Auth/Password/RegisterEndpointTests.cs
  # + assertion: happy path places the user in Global.

apps/api.Tests/Auth/Google/GoogleOAuthEndpointTests.cs
  # + assertion on new-user path: user lands in Global.
```

### 6.2. Coverage targets

- **`GroupPersistenceTests`** — Global seed exists with `is_root = true`;
  exactly one row has `is_root = true`.
- **`GroupMembershipPersistenceTests`** — composite PK rejects
  duplicates; deleting a user cascades and removes memberships.
- **`GroupServiceTests`** — `AddUserToGlobalAsync` inserts the row;
  repeated calls stay at count 1 (idempotent).
- **`XpRulePersistenceTests`** — DB `CHECK` fires on
  `weight_multiplier = 0.4`, `weight_multiplier = 2.6` and
  `base_xp = -1`.
- **`CategoryPersistenceTests`** — missing `group_id` / missing
  `xp_rule_id` rejected; same slug allowed across groups; duplicate
  slug rejected in the same group (citext = case-insensitive);
  multiple categories can share one rule.
- **`CategoryRepositoryTests`** — `ListActiveAsync(groupId)` excludes
  `active = false`; `GetBySlugAsync(groupId, "Legs")` matches `legs`.
- **`XpCalculatorTests`** — FsCheck property:
  `∀ base > 0, ∀ weight ∈ [0.5, 2.5], Compute(new XpRule { BaseXp=base, WeightMultiplier=weight }) > 0`
  and monotonic on `BaseXp` (holding weight constant).
- **`LevelCurveTests`** — table-driven values
  (`{0,0}, {1,100}, {5,1118}, {10,3162}, {50,35355}, {100,100000}`)
  + FsCheck monotonicity property.
- **`LevelUpServiceTests`** — `LevelForXp` round-trip
  (`∀ n ∈ [0..500], LevelForXp(XpForLevel(n)) == n`) +
  `EvaluateLevelUp` boundary cases (exact crossing, no-op inside a
  level, single-level jump, multi-level jump).
- **`ProgressionServiceTests`** — Testcontainers integration:
  seeded categories in a synthetic group round-trip through
  `ListActiveCategoriesAsync` / `GetCategoryBySlugAsync`;
  `ComputeXp` on a loaded rule returns the expected value;
  `LevelForXp(300)` returns `1`.
- **Auth extensions** (shipped in T-004-01):
  - `RegisterEndpointTests.Happy_path_places_the_new_user_in_the_global_group`.
  - `GoogleOAuthEndpointTests.New_user_path_creates_account_and_redirects_with_token`
    — extended assertion on the Global membership.

### 6.3. Fixtures

- Reuse `XpeakWebApplicationFactory` (Testcontainers Postgres per test
  class, migrations applied). Pure-domain tests use plain xUnit with
  FsCheck properties — no fixture, no container.
- Add `FsCheck.Xunit` to `apps/api.Tests/api.Tests.csproj` when
  T-004-02 lands.

## 7. Non-goals

- No category seed data whatsoever.
- No mutation of `users.xp` / `users.level` (Phase 5 wires the
  check-in transaction that calls `EvaluateLevelUp` and persists).
- No admin surface for editing rules or categories (Phase 9).
- No user-defined categories at the endpoint layer (Phase 9 admin +
  Phase 17 group-owner CRUD).
- No group-level bonus multiplier (`groups.bonus_rule_id`) — arrives
  in Phase 7 as a natural extension of the `xp_rules` shape.
- No group CRUD, invitations or membership management beyond the
  automatic Global-on-register hook (Phase 12+).
- No HTTP endpoint listing categories — arrives on demand in Phase 5
  when the check-in flow needs it.
