# Phase 30 — Drop Categories (PLAN)

> Stack: ASP.NET Core 9 + EF Core 9 + Postgres 16 + xUnit (api);
> Next.js 16 + Capacitor + TanStack Query + Vitest (mobile).

## 1. Goal

Delete the `Category` concept across the whole product. Check-ins
stop carrying a category, the `/groups/{groupId}/categories` endpoint
goes away and the Mobile category picker disappears. XP keeps being
awarded on every check-in via a single globally-seeded `XpRule`.

Rationale: categories were a hold-over from the pre-Challenge
product shape. With Phase 15 shipped, challenges are the social
unit and the "every check-in contributes" rule removed the only
remaining reason categories existed (category whitelist on
challenges). Keeping a list the user has to pick from on every
check-in is UX friction with no gameplay return — all the
multiplier shaping will live on streaks + challenges from here on.

## 2. Non-goals

- **Not** touching Groups. `Group`, `GroupMembership`, `GroupConfig`
  and `GLOBAL_GROUP_ID` stay as they are — they still carry streak
  config and scope check-ins + memberships. The "rename groups"
  conversation is a separate decision.
- **Not** changing the XP math. `XpCalculator.Compute(rule)` keeps
  its current shape; only the lookup path changes
  (`Category → XpRule` becomes a direct lookup of the default rule).
- **Not** touching Phase 7 (streak × challenge multipliers). Those
  land later; whatever they ship with will compose on top of the
  single default rule.
- **Not** editable per-user XP (that is Phase 9's job).

## 3. High-level approach

### 3.1. Backend

1. Load `XpRule` directly by a well-known id (the one already seeded
   in migration `20261002171145_SeedDefaultXpRuleAndGlobalCategories`
   — `00000000-0000-0000-0000-000000000010`, `base_xp = 10`,
   `weight_multiplier = 1.0`). Expose it as a constant
   `XpRuleIds.Default` in the Progression domain.
2. `ICheckInService.CreateAsync` drops `CategoryId` from its input;
   the service loads the default rule once (cheap) and hands it to
   `XpCalculator.Compute`.
3. Drop `Category` entity, repository, service methods, DTO,
   endpoint (`GET /groups/{groupId}/categories`), seed data for
   `categories` table, and the `category_id` column + FK on
   `check_ins`.
4. One migration adds this up in a single step:
   - `DROP TABLE categories` (with FK cleanup).
   - `ALTER TABLE check_ins DROP COLUMN category_id`.
   - Keep the seeded `xp_rules` row intact.

### 3.2. Mobile

1. `services/checkin.service.ts` drops `listCategories` and the
   `categoryId` field from `createCheckIn` payloads.
2. `useCheckinForm.ts` drops `categoryId` from the zod schema,
   defaults and submit payload.
3. `CheckinForm.tsx` deletes `<ControlledCategoryPicker>` and the
   `useCategories` call.
4. `ControlledCategoryPicker` + `useCategories` hook stop having
   any callers — delete the files.
5. Shared type `Category` and its exports go away.
6. Any `GLOBAL_GROUP_ID` usage that was only there to feed
   `useCategories(GLOBAL_GROUP_ID)` stays only where it still
   serves the check-in group id or categoryless Group concerns.

### 3.3. Specs & docs

- Open ADR `docs/adr/0030-drop-categories.md` capturing the
  decision + the single-default-rule fallback.
- Patch `specs/004-categories-and-xp-domain/spec.md` with a short
  historical note at the top ("Categories were removed in Phase 30
  — see ADR 0030. The `XpRule` entity stays as the sole scoring
  surface").
- Patch `specs/005-checkin-no-media/spec.md` with the same note.

## 4. Open questions

- **Q1:** Keep `base_xp = 10` or bump the default now that it is
  the single XP source? Phase 9 will make this tunable; shipping
  Phase 30 at `10` keeps the current numbers stable, so default:
  no change.
- **Q2:** Does `GET /categories` need a graceful 410 Gone for in-
  flight mobile clients? Mobile is unreleased; answer is no — the
  endpoint just disappears.
- **Q3:** Any existing check-in rows with `category_id` set keep
  their XP (the amount is already materialized on
  `check_ins.xp_earned`). The migration drops the column and the
  historical XP is preserved.

## 5. Rollout

Single PR, no feature flag. Mobile and API migrate together.
Testcontainers runs the full migration chain so the integration
tests confirm the drop is clean.
