# Phase 4 — Categories & Pure XP Domain

> Status: **in progress**. Companion to `spec.md` and `tasks.md`.
> Stack: C# 12 + ASP.NET Core 9 + EF Core 9 + Postgres 16.

## 1. Goal

Introduce the XP engine as a pure domain layer plus the group scope
every check-in eventually rides on. Categories are always owned by a
group; scoring numbers live on their own entity so multipliers can
compose in later phases without replicating fields. Zero HTTP endpoints,
zero UI.

## 2. Scope

**In scope**

- `Group` entity + `groups` table with one seed row: the root **Global**
  group every user belongs to.
- `GroupMembership` entity + `group_memberships` join table
  (composite PK on `(user_id, group_id)`).
- `IGroupService` with `AddUserToGlobalAsync` — idempotent. Wired
  into the register and Google-callback flows so every new account
  lands inside the global group in the same request.
- `XpRule` entity + `xp_rules` table — the actual XP numbers
  (`BaseXp`, `WeightMultiplier`). No column defaults; callers supply
  values explicitly.
- `Category` entity + `categories` table — identity + visual + scope,
  owned by a group, points to an `xp_rules` row via `xp_rule_id` FK.
- `XpCalculator` — pure static, `Compute(XpRule)` → int.
- `LevelCurve` — pure static, default `Round(100 * n^1.5)`.
- `LevelUpService` — pure static, `LevelForXp` (binary search) +
  `EvaluateLevelUp(prevXp, newXp)`.
- Public context `IProgressionService` + `ProgressionService`
  (composes repository + pure services). Exposes
  `ListActiveCategoriesAsync`, `GetCategoryBySlugAsync`, `ComputeXp`,
  `XpForLevel`, `LevelForXp`, `EvaluateLevelUp`.

**Out of scope**

- Any category seed. The engine ships; content lands later (admin
  surface in Phase 9 or group-owner CRUD in Phase 17).
- Sub-group CRUD, invitations, membership management (Phase 12+).
- Group-level XP bonuses (Phase 7 will attach `bonus_rule_id`).
- Actual check-in creation (Phase 5).
- Multipliers beyond category weight — streak, group, challenge
  (Phase 7 / Phase 15+).
- Editable configuration surface (Phase 9).
- HTTP endpoints, mobile UI, mutation of `users.xp` / `users.level`.

## 3. Approach

- **Groups own categories.** `categories.group_id` is `NOT NULL`.
  The root group holds whatever globally-available categories exist
  at any moment; sub-groups (Phase 12+) can register their own
  copies with their own `xp_rules` row without duplicating anything
  at the shape level.
- **Every user is in Global by default.** `RegisterEndpoint` and
  `GoogleAccountLinker.FindOrCreateAsync` call
  `IGroupService.AddUserToGlobalAsync` after `UserManager.CreateAsync`.
  The service is idempotent (early-return on existing membership),
  so retries and re-runs are safe.
- **XP rules are their own entity.** `Category` doesn't carry
  `base_xp` / `weight_multiplier` — it references an `xp_rules` row.
  Multiple categories can share one rule (dedup for `chest` + `back`
  at 12 XP × 1.2, for example). The same shape will host
  group-level bonuses (Phase 7), challenge modifiers (Phase 15+) and
  user overrides (Phase 9) without adding fields to those tables.
- **Pure services stay pure.** `XpCalculator`, `LevelCurve` and
  `LevelUpService` are `static` classes with no DI dependencies —
  same input → same output, no DB reads inside. Cheap to test
  (no fixtures, no Testcontainers boot) and safe to embed in
  Hangfire jobs, request paths or future messaging surfaces.
- **Rounding:** `Math.Round(x, MidpointRounding.AwayFromZero)` —
  explicit and intuitive (`0.5 → 1`, `1.5 → 2`); rationale
  captured in ADR-0004.
- **Default level curve:** `Round(100 * n^1.5)`. Cheap early,
  painful late. Tunable in Phase 9.

## 4. Artifacts

- `apps/api/Migrations/*_CreateGroupsRulesAndCategories.cs` — EF Core
  migration for `groups`, `xp_rules`, `categories`, `group_memberships`
  with `citext` extension, check constraints and the single Global
  seed row.
- `apps/api/src/Groups/GroupIds.cs` — well-known Guids (the Global
  root id lives here).
- `apps/api/src/Groups/Entities/{Group, GroupMembership}.cs`.
- `apps/api/src/Groups/Services/{IGroupService, GroupService}.cs`.
- `apps/api/src/Groups/GroupsModule.cs` — `AddGroups()` extension.
- `apps/api/src/Xp/{XpCalculator, LevelCurve, LevelUpService,
  LevelUpResult}.cs` — pure static math namespace. No module, no DI.
- `apps/api/src/Progression/Entities/{XpRule, Category}.cs`.
- `apps/api/src/Progression/Services/{IProgressionService,
  ProgressionService}.cs` + `Repositories/{ICategoryRepository,
  CategoryRepository}.cs`. `ProgressionService` imports
  `Xpeak.Api.Xp` and composes it with the repository.
- `apps/api/src/Progression/ProgressionModule.cs` — `AddProgression()`.
- `apps/api/src/Infrastructure/AppDbContext.cs` — adds `DbSet<Group>`,
  `DbSet<GroupMembership>`, `DbSet<XpRule>`, `DbSet<Category>` and
  their `OnModelCreating` blocks (snake_case, FKs, unique
  `(group_id, slug)`, CHECK constraints on `xp_rules`).
- `apps/api/src/Program.cs` — `.AddGroups().AddProgression()` in the
  service chain (Groups before AuthCore so the register endpoint can
  resolve `IGroupService`).
- `apps/api/src/Auth/Password/Endpoints/RegisterEndpoint.cs` and
  `apps/api/src/Auth/Google/Services/GoogleAccountLinker.cs` — call
  `IGroupService.AddUserToGlobalAsync` after account creation.
- `apps/api.Tests/Groups/*Tests.cs` — persistence + service coverage.
- `apps/api.Tests/Progression/*Tests.cs` — persistence + pure-domain
  coverage.
- `apps/api.Tests/Auth/**/*Tests.cs` — extended: register and Google
  new-user path assert the Global membership row.
- `docs/adr/0004-xp-engine.md` — ADR covering curve, rounding, rule
  extraction, group scoping and no-seed decision.

## 5. Dependencies

- Blocked by: Phases 1, 2, 3 (all shipped).
- Blocks: Phase 5 (check-in), Phase 7 (composed multipliers), Phase 9
  (editable config), Phase 12 (real sub-groups on top of memberships).

## 6. Decisions locked in during implementation

The original spec had four open questions plus a set of assumptions
that got revisited mid-flight. The final decisions:

1. **Default level curve** — `Round(100 * n^1.5)`. L1@100, L5@1118,
   L10@3162, L50@35355, L100@100000.
2. **Starter category set** — **no seed at all**. The engine ships
   without opinionated defaults; content flows in via the admin
   surface (Phase 9) or group-owner CRUD (Phase 17).
3. **Category icon representation** — Cloudinary `public_id` (nullable
   `icon_public_id`). Mobile builds the delivery URL with
   transformations at render time.
4. **Weight multiplier bounds** — `[0.5, 2.5]`. Enforced at the
   entity level (`[Range(0.5, 2.5)]`) **and** at the DB level (`CHECK`
   constraint on `xp_rules.weight_multiplier`).
5. **Scoping** — categories are always owned by a group (`group_id`
   `NOT NULL`). Every user joins the root Global group on register.
6. **Rule extraction** — `base_xp` / `weight_multiplier` live on
   `xp_rules`, not `categories`. Category holds `xp_rule_id`.
   Categories can share a rule.
7. **Rounding** — `MidpointRounding.AwayFromZero` (intuitive, avoids
   banker's rounding surprises).

## 7. Success criteria

- `dotnet test apps/api.Tests` green with the full
  `Xpeak.Api.Tests.Progression` + `Xpeak.Api.Tests.Groups` suites
  plus the extended auth-side assertions.
- `dotnet ef database update` applies the
  `CreateGroupsRulesAndCategories` migration cleanly on a fresh
  Postgres. The Global group exists with `is_root = true`. No
  category rows are created.
- Registering a new user (email/password or Google OAuth) results in
  a membership row into Global inside the same request.
- `IProgressionService.ComputeXp(rule)` returns
  `Round(rule.BaseXp * rule.WeightMultiplier)` for any valid rule.
- `IProgressionService.LevelForXp(300)` returns `1`
  (`XpForLevel(1) = 100`, `XpForLevel(2) = 283`).
- CI green.
