# Phase 30 — Drop Categories (SPEC)

> Stack: ASP.NET Core 9 + EF Core 9 + Postgres 16 + xUnit (api);
> Next.js 16 + Capacitor + TanStack Query + Vitest (mobile).

## 1. Summary

Remove the `Category` entity, the `category_id` column on
`check_ins`, the `GET /groups/{groupId}/categories` endpoint and
every mobile piece that renders a picker. XP comes from a single
globally-seeded `XpRule` row loaded directly by id. Groups,
streak and challenges are untouched.

## 2. Backend contract

### 2.1. Deleted endpoints

```
GET /groups/{groupId}/categories    → removed
```

Any client hitting it will get a vanilla 404. There is no
versioned successor — the concept is gone.

### 2.2. Changed endpoints

```
POST /check_ins
  body: { title, performedAt, durationMinutes?, notes?, withMedia? }
```

- `categoryId` is **removed** from the request body.
  FluentValidation drops the rule.
- Response shape is unchanged (still returns the created
  `CheckInResponse` + progression delta).

### 2.3. XP calculation

- `CheckInService.CreateAsync` and `.PublishAsync` load
  `XpRuleIds.Default` directly from `IProgressionService`
  (new method `GetDefaultRuleAsync(ct)`).
- `XpCalculator.Compute(rule)` stays unchanged.
- If the default rule row is missing (schema drift, bad migration),
  `CheckInService` throws a loud `InvalidOperationException` — this
  is a bug, not a user-facing error.

### 2.4. Database

Migration name: `DropCategories` (next sequential number).

```sql
ALTER TABLE check_ins
  DROP CONSTRAINT fk_check_ins_categories_category_id,
  DROP COLUMN category_id;

DROP TABLE categories;
```

- `xp_rules` table and its seeded `Default` row stay.
- Composite indexes on `check_ins` that referenced
  `category_id` get recreated without the column (none today, so
  nothing to do).

## 3. Mobile contract

### 3.1. Deleted files

- `apps/mobile/src/components/atoms/ControlledCategoryPicker.tsx`
- `apps/mobile/src/hooks/useCheckIn.ts`
  **only** the `useCategories` export — if the file holds the
  create mutation too, keep the file and drop the hook.
- `apps/mobile/src/services/interfaces/checkin.interface.ts` — strip
  `listCategories` from `ICheckInService` and `Category` type refs.
- `packages/shared/src/progression/types.ts` — the `Category`
  export (keep `XpRule`-shaped types if any).

### 3.2. Changed files

- `apps/mobile/src/components/organisms/CheckinForm.tsx`
  - Drops the `useCategories(GLOBAL_GROUP_ID)` call.
  - Drops `<ControlledCategoryPicker>`.
  - Keeps every other field.
- `apps/mobile/src/hooks/useCheckinForm.ts`
  - Drops `categoryId` from `defaultValues`, zod schema and submit
    payload.
  - Drops the `CHECKIN_MESSAGES.categoryRequired` entry.
- `apps/mobile/src/services/checkin.service.ts`
  - `createCheckIn` payload no longer sends `category_id`.
  - `listCategories` is deleted.

### 3.3. Tests

- `apps/mobile/test/components/organisms/CheckinForm.spec.tsx`
  - Drops the "blocks submit until a category is picked" case.
  - Updates every other case so no category is selected.
- `apps/mobile/test/hooks/useCheckinForm.spec.ts` (if present) —
  mirror the schema change.
- `apps/mobile/test/components/atoms/ControlledCategoryPicker.spec.tsx`
  — deleted.
- `apps/mobile/test/services/checkin.service.spec.ts` —
  `listCategories` cases dropped; `createCheckIn` payload
  assertion drops the category field.

## 4. Backend tests

- `apps/api.Tests/Progression/` — delete `CategoryRepositoryTests`,
  `ListGroupCategoriesEndpointTests`, any Category-specific
  fixture. Keep `XpRuleRepositoryTests`.
- `apps/api.Tests/CheckIns/CheckInServiceTests` + related endpoint
  tests — drop category-arrange steps; create check-ins with the
  slimmed input and assert XP is still awarded from the default
  rule.
- Add `apps/api.Tests/Progression/XpRuleDefaultTests.cs`:
  - Default rule row exists after migration.
  - `IProgressionService.GetDefaultRuleAsync` returns it.
  - Removing the row makes `CheckInService.CreateAsync` throw
    `InvalidOperationException`.

## 5. Error paths

| Scenario                                           | Behavior                               |
|----------------------------------------------------|----------------------------------------|
| Client posts `category_id` on `POST /check_ins`    | Ignored (unknown field, validator strict mode would 400 — keep lax to make the mobile cutover cheap). |
| Default `XpRule` row missing at check-in time      | 500 — this is a schema bug.           |
| Mobile cache still has `useCategories` entry       | N/A — mobile does not persist query cache across releases. |

## 6. Observability

- No new metrics. The `category_hit_count` log line in
  `CheckInService` (if present) goes away.
- The ADR links the migration name + the commit range so future
  reviewers can trace the drop.

## 7. Historical notes patched into prior specs

- `specs/004-categories-and-xp-domain/spec.md` — header note
  "Categories dropped in Phase 30; see
  `docs/adr/0030-drop-categories.md`. The `XpRule` entity remains
  the single scoring surface."
- `specs/005-checkin-no-media/spec.md` — same note, plus a
  sentence clarifying that the check-in form stopped carrying a
  category picker in Phase 30.
