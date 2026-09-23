# Phase 4 — Categories & Pure XP Domain

> Status: **planning only**. Companion to `spec.md` and `tasks.md`.
> Stack: C# 12 + ASP.NET Core 9 + EF Core 9 + Postgres 16.

## 1. Goal

Introduce the XP engine as a pure domain layer: workout categories
with their own base XP and weight, plus deterministic services that
calculate XP for a given check-in and decide whether it triggers a
level-up. **No HTTP endpoints, no UI, no `users.xp` mutation yet** —
this phase produces the arithmetic that every downstream phase in
Block B (5, 6, 7) leans on.

## 2. Scope

**In scope**

- `Category` entity + `categories` table + 10 system-seeded rows.
- `XpCalculator` — pure static class, 100% branch coverage.
- `LevelCurve` — pure static class, default `Round(100 * n^1.5)`.
- `LevelUpService` — pure static class, `LevelForXp` (binary search
  over the curve) + `EvaluateLevelUp(prevXp, newXp)`.
- Public context `IProgressionService` + `ProgressionService`
  registered via `ProgressionModule.AddProgression()`. Exposes
  `ListActiveCategoriesAsync`, `GetCategoryBySlugAsync`, `ComputeXp`,
  `XpForLevel`, `LevelForXp`, `EvaluateLevelUp`.

**Out of scope**

- Actual check-in creation (Phase 5).
- Multipliers beyond category weight — streak, group, challenge
  (Phase 7 for streak/group, Phase 15+ for challenge).
- Editable configuration surface (Phase 9).
- Any HTTP endpoint or mobile UI.
- Mutation of `users.xp` / `users.level`.

## 3. Approach

- Categories live in a `categories` table with `slug` (unique, `citext`),
  `name`, `icon_public_id` (nullable Cloudinary asset id), `base_xp`
  (`int`), `weight_multiplier` (`decimal(4,2)`, check `>= 0.5 AND <= 2.5`),
  `is_system` (`bool`), `active` (`bool`), `created_at` / `updated_at`
  (`timestamptz`).
- Seed the 10 starter categories via EF Core `HasData` in
  `AppDbContext.OnModelCreating` so the seed lives inside the migration
  and applies uniformly across every environment (dev, test, prod).
  Consistent with how the project already handles fixed configuration.
- All arithmetic (`XpCalculator`, `LevelCurve`, `LevelUpService`) lives
  in `static` classes with no DI dependencies — same input → same
  output, no DB reads inside the calculator. This makes them cheap to
  test (no fixtures, no Testcontainers boot) and safe to embed in
  Hangfire jobs, request paths, or future gRPC/messaging surfaces.
- The default level curve `XpForLevel(n) = Round(100 * n^1.5)`:
  cheap early, painful late. Tunable in Phase 9.
- Rounding: `Math.Round(x, MidpointRounding.AwayFromZero)` — explicit
  and intuitive; documented in the ADR.

## 4. Artifacts

- `apps/api/Migrations/*_CreateCategories.cs` — EF Core migration for
  the `categories` table (schema + `HasData` seed baked in).
- `apps/api/src/Progression/Entities/Category.cs` — sealed entity.
- `apps/api/src/Progression/Services/XpCalculator.cs` — static, pure.
- `apps/api/src/Progression/Services/LevelCurve.cs` — static, pure.
- `apps/api/src/Progression/Services/LevelUpService.cs` — static, pure.
- `apps/api/src/Progression/Repositories/CategoryRepository.cs` — reads
  `categories` from `AppDbContext` (list active, get by slug).
- `apps/api/src/Progression/Services/IProgressionService.cs` +
  `ProgressionService.cs` — orchestrator that composes repository +
  pure services; the only thing exported by the module.
- `apps/api/src/Progression/ProgressionModule.cs` —
  `AddProgression()` extension registering the repository, service
  and any options.
- `apps/api/src/Infrastructure/AppDbContext.cs` — add `DbSet<Category>`
  and `OnModelCreating` block (snake_case, check constraint, unique
  index on `slug`, `HasData` seed).
- `apps/api/src/Program.cs` — one line: `.AddProgression()` in the
  service chain.
- `apps/api.Tests/Progression/*Tests.cs` — full test coverage.
- `docs/adr/0004-xp-engine.md` — ADR for the curve choice,
  rounding, and tunability path.

## 5. Dependencies

- Blocked by: Phases 1, 2, 3 (all shipped).
- Blocks: Phase 5 (check-in creation), Phase 7 (composed multipliers),
  Phase 9 (editable config surface).

## 6. Open questions (proposed defaults)

The 4 open questions from the original Elixir plan, resolved with
sensible defaults. Any of them can flip in review.

1. **Default level curve formula** — **decided: `Round(100 * n^1.5)`**.
   Fibonacci-like grows too fast (level 20 costs > 100k XP);
   piecewise adds config complexity we push to Phase 9. `n^1.5` gives
   L1@100, L5@1118, L10@3162, L50@35355, L100@100000.
2. **Starter category set** — **decided: keep the 10 in `spec.md`
   §2.2**. Yoga / pilates / swim / martial arts land as user-defined
   categories in Phase 9 (they need weight tuning discussions we don't
   want to block this phase on).
3. ✅ **Category icon representation** — Cloudinary `public_id`
   (nullable). Seeds live under the `categories/` folder; mobile
   client builds the URL with transformations at render time.
4. **Weight multiplier bounds** — **decided: `[0.5, 2.5]`**. Enforced
   both at the entity level (`[Range(0.5, 2.5)]` on the property) and
   at the DB level (`CHECK` constraint in the migration).

## 7. Success criteria

- `dotnet test apps/api.Tests` green with the full
  `Xpeak.Api.Tests.Progression` suite (xUnit + FsCheck for property
  tests).
- `dotnet ef database update` applies the `CreateCategories` migration
  cleanly on a fresh Postgres and populates the 10 system categories.
- `IProgressionService.ComputeXp` returns the expected XP for the
  seeded weights (e.g. `legs` → `Round(15 * 1.4) = 21`).
- `IProgressionService.LevelForXp(300)` returns `1` (since
  `XpForLevel(1) = 100`, `XpForLevel(2) = 283`, `XpForLevel(3) = 520`).
- CI green.
