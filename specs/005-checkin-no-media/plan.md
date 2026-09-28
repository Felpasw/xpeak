# Phase 5 — Check-in (No Media)

> Status: **planning only**. Companion to `spec.md` and `tasks.md`.
> Stack: C# 12 + ASP.NET Core 9 + EF Core 9 + Postgres 16.

## 1. Goal

Wire the XP engine end-to-end for the first time. A user opens the
app, taps "check in", picks a category from the group they're
active in, submits, and their `xp`/`level` update globally while a
per-group streak becomes derivable from the raw check-in trail. No
media, no compound multipliers yet.

## 2. Scope

**In scope**

- `check_ins` table — one row per submission, denormalized `group_id`
  from the picked category so hot queries stay single-table.
- `group_configs` table — 1:1 with `groups`, holds
  `streak_config` (JSONB) plus future per-group toggles.
- `AppUser` gains `time_zone` (string, default `"UTC"`); XP/level
  stay as global counters. **No cached streak fields.**
- `POST /check_ins` — auth required, membership-scoped, single
  transaction that inserts the check-in and updates
  `users.xp` / `users.level`.
- `GET /check_ins?limit=&cursor=&group_id=` — cursor-paginated
  personal history, optionally filtered to a group.
- `IStreakService` — derives current & longest streak per
  `(user, group)` from `check_ins` on demand. No cache.
- `StreakCalculator` — pure static that turns a list of distinct
  dates + config into `(current, longest)`.
- Membership enforcement in the service layer before insert.
- `scoring_snapshot` (JSONB) persisted on every check-in for audit
  and future breakdown UI.
- Mobile: `/checkin` screen (category picker + optional
  duration/notes + submit), level-up overlay, `useMe` cache
  invalidation on success.

**Out of scope**

- Any media attachment (Phase 6).
- Compound multipliers — streak / group / challenge bonuses
  (Phase 7). The `scoring_snapshot` shape is forward-compatible.
- **Weekly streak calc** — schema on `group_configs.streak_config`
  supports `{"mode": "weekly", "required_days": N}`, but Phase 5's
  `StreakCalculator` only implements `"daily"`. Non-daily configs
  throw `NotSupportedException`. Weekly lands in Phase 9 (editable
  config) or Phase 11 (medals).
- Editable XP / streak config surface (Phase 9).
- Feed / timeline event emission (Phase 13).
- Group CRUD, invitations, sub-group creation (Phase 12+).
- Undo / edit / delete a check-in.
- Timezone-aware streak math beyond UTC.

## 3. Approach

- **Single transaction** in `CheckInService.CreateAsync`:
  1. Load category (+ verify exists).
  2. Verify user is a member of `category.GroupId`.
  3. Load the referenced `XpRule`.
  4. `XpCalculator.Compute(rule)` → `xp_earned` + build
     `scoring_snapshot`.
  5. `BeginTransactionAsync` → insert `CheckIn`, update
     `user.Xp += xp_earned`, `user.Level =
     LevelUpService.LevelForXp(user.Xp)` → `Commit`.
  6. **After commit**, compute the derived streak for the
     `(user, category.GroupId)` pair to include in the response.
- **Streak is derived, never cached.** `StreakService` queries
  `check_ins` for distinct dates where the user checked in against
  that group, walks backwards from today to find `current`, scans
  all dates for `longest`. Hot path bounded by
  `(user_id, group_id, performed_at DESC)` index; typical result
  set well under 2000 rows even at years of use.
- **`group_id` on `check_ins` is populated by the service** from
  `category.GroupId` — never accepted from the client. Test locks
  the invariant.
- **`scoring_snapshot` is a structured JSONB** with a `base_xp`
  scalar and an array of `{source, value}` multipliers. Phase 5
  emits one multiplier (`category_weight`); Phase 7 appends
  `streak`, `group_bonus`, `challenge` without a schema change.
- **`group_configs` is seeded for the Global group** inside the
  Phase 5 migration (`HasData`), value `{"mode":"daily"}`. Future
  groups get their own row via a `GroupService.CreateAsync` flow
  (Phase 12+); for now the Global row is the only one that exists.
- **Pagination uses an opaque cursor** = base64 of
  `(performed_at, id)`. Stable under new inserts, cheaper than
  offset at scale.

## 4. Artifacts

**API**

- `apps/api/Migrations/*_AddCheckInsAndGroupConfigs.cs` — new
  tables + `AppUser.time_zone` column + `group_configs` seed for
  the Global group.
- `apps/api/src/CheckIns/Entities/CheckIn.cs`.
- `apps/api/src/CheckIns/Dto/*.cs` — request/response DTOs.
- `apps/api/src/CheckIns/Services/{ICheckInService, CheckInService}.cs`
  — orchestration (create + list).
- `apps/api/src/CheckIns/Services/{IStreakService, StreakService}.cs`
  — derived streak query helper.
- `apps/api/src/CheckIns/Services/StreakCalculator.cs` — pure static,
  `Compute(IReadOnlyList<DateOnly>, DateOnly asOf, StreakConfig)`.
- `apps/api/src/CheckIns/Repositories/{ICheckInRepository, CheckInRepository}.cs`
  — reads (list + cursor pagination).
- `apps/api/src/CheckIns/Endpoints/{CreateCheckInEndpoint, ListCheckInsEndpoint}.cs`.
- `apps/api/src/CheckIns/CheckInsModule.cs` — `AddCheckIns()` +
  `UseCheckIns()`.
- `apps/api/src/Groups/Entities/GroupConfig.cs` — new entity.
- `apps/api/src/Groups/Services/{IGroupConfigService, GroupConfigService}.cs`
  — read the config for a group.
- `apps/api/src/Groups/Services/IGroupService.cs` — extended with
  `IsMemberAsync(userId, groupId)`.
- `apps/api/src/Groups/GroupsModule.cs` — register the new service.
- `apps/api/src/Progression/Services/IProgressionService.cs` —
  extended with `GetCategoryByIdAsync(categoryId, ct)` and
  `GetRuleAsync(ruleId, ct)` for the check-in flow.
- `apps/api/src/Users/Entities/AppUser.cs` — add `TimeZone`.
- `apps/api/src/Infrastructure/AppDbContext.cs` — new DbSets,
  `OnModelCreating` blocks, `HasData` for the Global group_config.
- `apps/api/src/Program.cs` — `.AddCheckIns()` in the chain +
  `.UseCheckIns()` in the pipeline.

**Tests**

- `apps/api.Tests/CheckIns/*Tests.cs` — persistence, streak calc
  (pure), streak service (derived), check-in service (transaction,
  membership rejection, group_id invariant), endpoints.
- `apps/api.Tests/Groups/GroupConfigTests.cs` — global seed, JSONB
  round-trip, `IsMemberAsync` add coverage.
- FsCheck property tests on `StreakCalculator` invariants
  (monotonicity, same-day is no-op, gap resets).

**Mobile**

- `apps/mobile/src/services/interfaces/checkin.interface.ts`.
- `apps/mobile/src/services/checkin.service.ts`.
- `apps/mobile/src/hooks/useCheckIn.ts` (mutation + list query).
- `apps/mobile/src/app/(app)/checkin/page.tsx` — replaces the
  current `ComingSoon` placeholder.
- `apps/mobile/src/components/organisms/CheckinForm.tsx`.
- `apps/mobile/src/components/atoms/LevelUpOverlay.tsx`.
- `apps/mobile/test/**` — matching Vitest coverage.

**ADR**

- `docs/adr/0005-check-in-and-streak.md` — captures streak-derived
  vs cached, group scoping of check-ins, `scoring_snapshot` shape,
  cursor pagination, membership enforcement layer.

## 5. Dependencies

- Blocked by: Phase 4 (XP engine + Groups + XpRule shape).
- Blocks: Phase 6 (media on top of check-in), Phase 7 (multipliers
  read `scoring_snapshot`), Phase 11 (streak medals read streak),
  Phase 13 (feed emits check-in events), Phase 14 (rankings query
  aggregate XP over `check_ins`).

## 6. Decisions locked in (from design review)

1. **`check_ins.group_id` denormalized** from the picked category.
   Rationale: hot queries per group stay single-table, audit trail
   is immutable if categories ever move groups.
2. **Membership check in the service layer** before insert.
   `IGroupService.IsMemberAsync(userId, groupId)`. Rejects with
   403.
3. **Payload uses `category_id`** (`Guid`), not `category_slug`.
   Slug is no longer unique with group scoping; the mobile client
   already has the id from the category listing.
4. **`scoring_snapshot` JSONB from day 1** with a structured shape
   (`base_xp` + `multipliers[]`). Phase 5 emits one multiplier
   entry; Phase 7 appends without a schema change.
5. **Streak is per-group and derived on demand** from `check_ins`.
   No cache table. Zero risk of desync, zero orphaned data.
6. **Streak config lives in `group_configs`** (1:1 with `groups`),
   not inline on `Group`. Extensible target for Phase 7+ per-group
   toggles.
7. **Streak calc supports `"daily"` only in Phase 5.** Non-daily
   configs throw `NotSupportedException`. Weekly lands later.
8. Same-day multiple check-ins: **allowed, no cap.** Same day = no
   streak change; each check-in still awards its own XP.
9. `performed_at`: **server timestamp** (`TimeProvider.GetUtcNow()`).
   No client backfill in MVP.
10. Notes: **free text, nullable, max 280 chars** (DB CHECK).
11. Pagination: **cursor-based**, opaque base64 of
    `(performed_at, id)`.

## 7. Success criteria

- `POST /check_ins` with a valid `category_id` returns 201, the
  check-in body (including `scoring_snapshot`), the updated user
  (`xp`, `level`, `leveled_up`, `levels_gained`), and a top-level
  `streak` object (`current`, `longest`, `unit`) scoped to the
  check-in's group.
- A user who is not a member of the category's group gets 403.
- Same-day second check-in in the same group does not bump the
  streak but does award XP.
- Consecutive-day check-in bumps `streak.current` by 1.
- 2-day gap resets `streak.current` to 1 on the next check-in.
- `GET /check_ins?limit=20` returns the caller's own history,
  ordered newest first, with a working `next_cursor`.
- `dotnet test apps/api.Tests` green with the new suites +
  pre-existing 122 tests still green.
- `pnpm --filter @xpeak/mobile test` green with new coverage.
- Mobile: 3 taps from `/home` to a submitted check-in with the
  level-up overlay when applicable and the profile totals fresh.
- CI green on the PR.
