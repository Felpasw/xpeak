# Phase 4 — Categories & Pure XP Domain (SPEC)

> Stack: C# 12 + ASP.NET Core 9 + EF Core 9 + Postgres 16 + xUnit + FsCheck.

## 1. Summary

`categories` table + `Xpeak.Api.Progression` module with pure XP and
level services. Zero HTTP endpoints, zero UI.

## 2. Data model

### 2.1. `categories` table

| Column              | Type            | Constraints                                    |
|---------------------|-----------------|------------------------------------------------|
| `id`                | `uuid`          | PK                                             |
| `slug`              | `citext`        | not null, unique                               |
| `name`              | `text`          | not null                                       |
| `icon_public_id`    | `text`          | nullable — Cloudinary asset public id          |
| `base_xp`           | `int`           | not null, default `10`, `CHECK (base_xp > 0)`  |
| `weight_multiplier` | `numeric(4, 2)` | not null, default `1.00`, `CHECK (>=0.5 AND <=2.5)` |
| `is_system`         | `bool`          | not null, default `false`                      |
| `active`            | `bool`          | not null, default `true`                       |
| `created_at`        | `timestamptz`   | not null                                       |
| `updated_at`        | `timestamptz`   | not null                                       |

**Indexes:** unique on `slug`.

**Postgres extension:** `CREATE EXTENSION IF NOT EXISTS citext` runs
as part of the migration (case-insensitive `slug` matches the
convention already used by AppUser's normalized username lookup).

### 2.2. Seed set (`is_system = true`, seeded via EF Core `HasData`)

Icons ship as Cloudinary assets uploaded under the `categories/`
folder (public id examples: `categories/legs`, `categories/chest`).
Seed rows reference the public id; the mobile client builds the
delivery URL with the desired transformations
(`f_auto,q_auto,w_128,h_128,c_fill`) at render time.

| Slug        | Name        | Icon public id         | Base XP | Weight |
|-------------|-------------|------------------------|---------|--------|
| `legs`      | Legs        | `categories/legs`      | 15      | 1.40   |
| `chest`     | Chest       | `categories/chest`     | 12      | 1.20   |
| `back`      | Back        | `categories/back`      | 12      | 1.20   |
| `shoulders` | Shoulders   | `categories/shoulders` | 10      | 1.10   |
| `arms`      | Arms        | `categories/arms`      | 8       | 1.00   |
| `core`      | Core        | `categories/core`      | 8       | 0.90   |
| `running`   | Running     | `categories/running`   | 15      | 1.30   |
| `cycling`   | Cycling     | `categories/cycling`   | 12      | 1.20   |
| `mobility`  | Mobility    | `categories/mobility`  | 6       | 0.80   |
| `other`     | Other       | `categories/other`     | 5       | 1.00   |

Seed `id`s are hard-coded Guids in `AppDbContext.OnModelCreating` so
migrations are deterministic across environments and downstream
foreign keys (Phase 5 `check_ins.category_id`) can reference them
predictably.

Values tunable in Phase 9 (editable XP config surface).

## 3. Domain services (pure, static)

### 3.1. `XpCalculator`

```csharp
namespace Xpeak.Api.Progression.Services;

public static class XpCalculator
{
    public static int Compute(Category category)
    {
        var raw = category.BaseXp * (double)category.WeightMultiplier;
        return (int)Math.Round(raw, MidpointRounding.AwayFromZero);
    }
}
```

Rounding: `MidpointRounding.AwayFromZero` — explicit and intuitive
(`0.5 → 1`, `1.5 → 2`). Rationale captured in ADR-0004.

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

## 4. Public context — `IProgressionService`

The **only** thing exported by `ProgressionModule`. Consumers
(future check-in module, future rankings module, LLM tools) depend
on this interface, not on the concrete service or the repository.

```csharp
namespace Xpeak.Api.Progression;

public interface IProgressionService
{
    Task<IReadOnlyList<Category>> ListActiveCategoriesAsync(
        CancellationToken ct = default);

    Task<Category?> GetCategoryBySlugAsync(
        string slug,
        CancellationToken ct = default);

    int ComputeXp(Category category);

    int XpForLevel(int level);

    int LevelForXp(int xp);

    LevelUpResult EvaluateLevelUp(int prevXp, int newXp);
}
```

Concrete `ProgressionService`:
- Delegates category reads to `ICategoryRepository`.
- Delegates arithmetic to the static classes above.
- No business logic of its own — it's the composition seam.

## 5. Module layout

```
apps/api/src/Progression/
  Entities/
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

`ProgressionModule.AddProgression(this IServiceCollection services)`:

- `AddScoped<ICategoryRepository, CategoryRepository>()`
- `AddSingleton<IProgressionService, ProgressionService>()`
  (the service holds no state; repository is resolved per-call via
  `IServiceProvider`, or the service is Scoped if we prefer symmetry
  — the ADR captures the choice).

`Program.cs` gains a single line: `.AddProgression()` in the service
chain, after `.AddUsers()`.

## 6. Test plan

### 6.1. Test project structure

```
apps/api.Tests/Progression/
  CategoryPersistenceTests.cs        # Testcontainers, real Postgres
  CategoryRepositoryTests.cs         # Testcontainers, real Postgres
  XpCalculatorTests.cs               # xUnit + FsCheck property tests
  LevelCurveTests.cs                 # xUnit + FsCheck property tests
  LevelUpServiceTests.cs             # xUnit, table-driven
  ProgressionServiceTests.cs         # Testcontainers, integration
```

### 6.2. Coverage targets

- **`CategoryPersistenceTests`** — schema constraints:
  - `slug` unique across inserts (case-insensitive via `citext`).
  - `weight_multiplier` bounds enforced by the DB `CHECK` — inserting
    `0.4` or `2.6` throws `DbUpdateException`.
  - Seed run: after `Database.EnsureCreatedAsync()` (or migrations
    applied), exactly 10 rows with `is_system = true` and the
    expected slugs.

- **`CategoryRepositoryTests`** — `ListActiveAsync` returns only
  `active = true`; `GetBySlugAsync("Legs")` matches `legs` (citext).

- **`XpCalculatorTests`** — FsCheck property:
  `∀ base > 0, ∀ weight ∈ [0.5, 2.5], Compute(new Category { BaseXp=base, WeightMultiplier=weight }) > 0`
  and monotonic on `BaseXp` (holding weight constant).

- **`LevelCurveTests`** — table-driven values (`{0, 0}, {1, 100},
  {5, 1118}, {10, 3162}, {50, 35355}, {100, 100000}`) + FsCheck
  monotonicity property.

- **`LevelUpServiceTests`**:
  - `LevelForXp` round-trip: `∀ n ∈ [0..500], LevelForXp(XpForLevel(n)) == n`.
  - `EvaluateLevelUp` boundary cases:
    - `EvaluateLevelUp(99, 100)` → `(true, 1, 1)` (crossing exactly).
    - `EvaluateLevelUp(100, 199)` → `(false, 1, 0)` (no crossing).
    - `EvaluateLevelUp(0, 300)` → `(true, 1, 1)` (single crossing).
    - `EvaluateLevelUp(0, 3162)` → `(true, 10, 10)` (multi-level jump).

- **`ProgressionServiceTests`** — Testcontainers integration:
  - `ListActiveCategoriesAsync` returns the 10 seeded rows.
  - `GetCategoryBySlugAsync("legs")` returns the `Legs` entity;
    `ComputeXp(that)` returns `21`.
  - `ListActiveCategoriesAsync` excludes an inactive row inserted at
    runtime.

### 6.3. Test fixtures

- Reuse the existing `XpeakWebApplicationFactory` (already boots
  Postgres via Testcontainers and applies migrations). For pure-domain
  tests (`XpCalculator`, `LevelCurve`, `LevelUpService`) there is
  **no fixture** — plain xUnit `[Fact]` / `[Theory]` methods with
  FsCheck properties.
- Add `FsCheck.Xunit` to `apps/api.Tests/api.Tests.csproj`.

## 7. Non-goals

- No mutation of `users.xp` or `users.level` in this phase (that
  wires up in Phase 5 when the check-in endpoint calls
  `EvaluateLevelUp` and persists the result inside the check-in
  transaction).
- No admin surface (Phase 9).
- No user-defined categories (Phase 9).
- No HTTP endpoint listing categories (that comes in Phase 5 as part
  of the check-in flow, or earlier if the mobile client needs it —
  decided on demand).
