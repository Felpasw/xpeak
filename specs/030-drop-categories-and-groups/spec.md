# Phase 30 — Drop Categories and Groups (SPEC)

> Stack: ASP.NET Core 9 + EF Core 9 + Postgres 16 + xUnit (api);
> Next.js 16 + Capacitor + TanStack Query + Vitest (mobile).

## 1. Summary

Delete every schema and code artefact that implements `Category`
or `Group` on top of what the product actually ships (user +
check-in + challenge). After this phase:

- Check-ins carry only user + title + time + optional
  duration/notes/media.
- XP comes from a single constant inside `XpCalculator`.
- Streak is derived per user from the user's own check-ins.
- Challenges are untouched; they already observe check-ins by
  `(user_id, performed_at)` and need nothing from Group.

## 2. Backend contract

### 2.1. Deleted endpoints

```
GET /groups/{groupId}/categories    → removed (404 going forward)
```

No successor and no 410 Gone — mobile is unreleased.

### 2.2. Changed endpoints

```
POST /check_ins
  body: { title, performedAt, durationMinutes?, notes?, withMedia? }

GET  /check_ins                     → unchanged query shape; the
                                      response objects drop
                                      `categoryId` and `groupId`.
```

- `categoryId` and `groupId` are **removed** from the request
  body. FluentValidation drops the matching rules.
- Response shape (`CheckInResponse`) drops the same two fields.
- `GET /me` + any streak surface returns a single `streak`
  number per user (no `groupStreaks` array).

### 2.3. XP calculation

- `XpCalculator.CheckInXp` is `const int = 10` (matches the
  current seeded `base_xp`).
- `XpCalculator.Compute()` returns `CheckInXp` and takes no
  arguments.
- `IProgressionService.GetRuleAsync` and `ComputeXp(XpRule)` are
  deleted. Downstream code that composed multipliers on top
  reads `CheckInXp` directly.
- Phase 9 reclaims this when it ships ("editable XP config"
  will decide whether to migrate to `app_settings` or similar).

### 2.4. Streak

- `IStreakService` keeps the same public shape (`int Current`,
  `int Longest`, `DateTimeOffset? LastCheckInAt`) but the
  per-user flavour: `GetForUserAsync(userId, ct)` with no
  `groupId` overload.
- Implementation reads `check_ins` ordered by `performed_at`
  descending for `user_id = @userId`, folds consecutive days
  (using the user's `TimeZone` already stored on `AppUser`).
- Weekly mode and `StreakConfig` are gone — the constant
  `StreakRules.Mode` is `StreakMode.Daily` and the enum is kept
  for Phase 11 titles.

### 2.5. Database

Migration name: `DropCategoriesAndGroups`.

```sql
-- 1. Drop FKs on check_ins
ALTER TABLE check_ins DROP CONSTRAINT fk_check_ins_categories_category_id;
ALTER TABLE check_ins DROP CONSTRAINT fk_check_ins_groups_group_id;

-- 2. Drop FKs on categories
ALTER TABLE categories DROP CONSTRAINT fk_categories_groups_group_id;
ALTER TABLE categories DROP CONSTRAINT fk_categories_xp_rules_xp_rule_id;

-- 3. Drop FKs on group side
ALTER TABLE group_memberships DROP CONSTRAINT fk_group_memberships_groups_group_id;
ALTER TABLE group_configs    DROP CONSTRAINT fk_group_configs_groups_group_id;

-- 4. Drop columns on check_ins
ALTER TABLE check_ins DROP COLUMN category_id;
ALTER TABLE check_ins DROP COLUMN group_id;

-- 5. Drop tables
DROP TABLE categories;
DROP TABLE xp_rules;
DROP TABLE group_memberships;
DROP TABLE group_configs;
DROP TABLE groups;
```

The exact constraint names come from the EF-generated snapshot;
the migration code reads them off `AppDbContextModelSnapshot`
and is validated by Testcontainers.

### 2.6. Indexes

`ix_check_ins_group_performed_desc` and
`ix_check_ins_user_group_performed_desc` are **removed**.
`ix_check_ins_user_performed_desc` already exists and covers
"recent check-ins for user X" — the only remaining query shape.

## 3. Mobile contract

### 3.1. Deleted files

- `apps/mobile/src/components/atoms/ControlledCategoryPicker.tsx`
  (+ spec).
- `apps/mobile/src/hooks/useCheckIn.ts` — only the `useCategories`
  export; keep the file if it still exports `useCreateCheckIn`
  (it does today).

### 3.2. Changed files

- `apps/mobile/src/components/organisms/CheckinForm.tsx` drops
  the picker import and usage; drops the
  `useCategories(GLOBAL_GROUP_ID)` call.
- `apps/mobile/src/hooks/useCheckinForm.ts`:
  - `defaultValues` drops `categoryId`.
  - zod schema drops `categoryId` and the matching message.
  - submit payload drops `categoryId` and the implicit
    `groupId`.
- `apps/mobile/src/services/checkin.service.ts`:
  - `createCheckIn` payload loses `category_id` and
    `group_id`.
  - `listCategories` is deleted.
- `packages/shared/src/index.ts` drops `GLOBAL_GROUP_ID`,
  `Category`, `GroupListItem` and any surviving group/category
  types. Mobile imports update accordingly.

### 3.3. Test changes

- `apps/mobile/test/components/organisms/CheckinForm.spec.tsx`:
  - Deletes the "blocks submit until a category is picked"
    case.
  - All happy paths drop the `category_id` assertion from the
    captured payload.
- `apps/mobile/test/components/atoms/ControlledCategoryPicker.spec.tsx`
  — deleted.
- `apps/mobile/test/services/checkin.service.spec.ts` (if it
  exists) — `listCategories` cases dropped; `createCheckIn`
  payload assertion drops the two fields.
- Any Vitest fixture that referenced `GLOBAL_GROUP_ID` or a
  `Category` shape updates.

## 4. Backend tests

- Delete:
  - `apps/api.Tests/Progression/CategoryRepositoryTests.cs`
  - `apps/api.Tests/Progression/CategoryPersistenceTests.cs`
  - `apps/api.Tests/Progression/ListGroupCategoriesEndpointTests.cs`
  - `apps/api.Tests/Progression/XpRulePersistenceTests.cs`
  - `apps/api.Tests/Progression/XpRuleRepositoryTests.cs`
  - every group-arrange helper in `apps/api.Tests/Support/`
    that seeded groups/memberships.
- Rewrite:
  - `CheckInServiceTests` / `CheckInPersistenceTests` to arrange
    users directly (no group), expect `XpEarned == CheckInXp`
    on every happy path.
  - `CreateCheckInEndpointTests` to assert the 422 reasons list
    no longer mentions `categoryId` or `groupId`.
  - `StreakServiceTests` to arrange check-ins against a single
    user and assert the derived streak.
- Add:
  - `apps/api.Tests/Infrastructure/DropCategoriesAndGroupsMigrationTests.cs`
    — fresh Testcontainers Postgres, run migrations through
    this one, assert the five tables and two columns are gone
    and the Challenge tables are intact.

## 5. Error paths

| Scenario                                                         | Behavior                                                                 |
|------------------------------------------------------------------|--------------------------------------------------------------------------|
| Client posts `categoryId` or `groupId` on `POST /check_ins`      | Field is ignored (unknown property, no 400 — keeps the cutover cheap).   |
| Hangfire job expects `group_id` on a check-in                    | N/A — no such job exists today.                                          |
| `AppUser.TimeZone` is null when deriving streak                  | Fall back to UTC (same policy Phase 5 already uses).                     |

## 6. Observability

- No new metrics. Any `group_id`-tagged log lines in
  `CheckInService` / `StreakService` drop the tag.
- The ADR links the migration name + the commit range so future
  reviewers can trace the collapse.

## 7. Historical notes to patch

- `specs/004-categories-and-xp-domain/spec.md` — top-of-file
  banner: "Categories, Groups and XpRule were removed in
  Phase 30; see `docs/adr/0030-drop-groups-and-categories.md`.
  Only the pure `XpCalculator` / `LevelCurve` arithmetic
  survives."
- `specs/005-checkin-no-media/spec.md` — top-of-file banner:
  "Group scope on check-ins and the category picker were
  removed in Phase 30 (ADR 0030). Streak is now per-user."
- `specs/007-streak-and-multipliers/spec.md` — note under
  "Streak tracking": "Phase 30 removed the group-scope streak;
  this phase composes a per-user streak with the challenge
  multiplier."
- `specs/019-group-bonuses-and-streak/spec.md` — top-of-file
  banner: "Phase 30 removed Group as a product concept. This
  phase will be re-scoped to challenge-level bonuses/streak
  when it is picked up; see ADR 0030."
- `specs/roadmap.md` — update the Phase 19 entry heading from
  "Group bonuses & group streak" to "Challenge bonuses &
  challenge streak (post Phase 30)".
