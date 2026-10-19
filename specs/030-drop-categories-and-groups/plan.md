# Phase 30 — Drop Categories and Groups (PLAN)

> Stack: ASP.NET Core 9 + EF Core 9 + Postgres 16 + xUnit (api);
> Next.js 16 + Capacitor + TanStack Query + Vitest (mobile).

## 1. Goal

Collapse the social model to a single social unit: **Challenges**.
Delete both `Category` and `Group` (and everything that hangs off
either). Check-ins become pure user events, streak becomes a user
property and XP comes from a hardcoded constant that Phase 9 can
move into persisted config later.

## 2. Rationale

Two schema concepts shipped in Phases 4–5 that never turned into
product features:

- **`Category`** — was a per-group list of workout types with their
  own XP weights. Phase 15 removed the `challenge_categories`
  whitelist, which was the only remaining consumer of category
  semantics in gameplay. All that is left is a picker on the
  check-in form that forces the user to classify a workout for no
  reward.
- **`Group`** — the original plan (Phase 4 §5) reserved
  `groups`/`group_memberships`/`group_configs` for a hierarchical
  model where sub-groups (Phase 12+) would be real feature. Phase 15
  shipped Challenges as the social unit without the planned
  `challenges.group_id` FK, and Phase 12 (Friends) covered the
  "persistent crew" use case via the friendship graph rather than
  a group container. Only the seeded `Global` row exists in
  production.

With both concepts vestigial, carrying them forward costs
clarity (every reviewer asks "what are groups?") and schema
weight (five tables plus `check_ins` columns and the
`GLOBAL_GROUP_ID` constant threaded through the mobile code).

Deleting both is the honest move. The ADR
(`docs/adr/0030-drop-groups-and-categories.md`) captures the full
reasoning plus the trade-offs it introduces.

## 3. Non-goals

- **Not** reworking Challenges. The Challenge entity, membership,
  progress and HTTP surface stay exactly as Phase 15 shipped
  them.
- **Not** turning XP amounts into persisted config. The hardcoded
  constant is a stopgap; Phase 9 ("editable XP config") inherits
  the problem of picking where it lives.
- **Not** adding weekly-mode streak. Streak collapses to the
  daily behavior that is actually implemented today — the
  schema-ready weekly fields on `GroupConfig` die with the
  entity.
- **Not** keeping backwards compatibility for mobile. Mobile is
  unreleased; the API cutover is single-shot.
- **Not** redesigning Phase 19. The Phase 19 entry in
  `specs/roadmap.md` gets a one-line patch pointing at the new
  shape ("challenge bonuses + challenge streak" instead of
  "group bonuses + group streak"), but the detail work lives in
  Phase 19's own folder when it is picked up.

## 4. High-level approach

### 4.1. Backend

1. **New constants** in `Xpeak.Api.Xp` for the hardcoded XP
   payout and the hardcoded streak mode:
   - `XpCalculator.CheckInXp` (int, `10`).
   - `StreakRules.Mode` (fixed daily).
2. **`CheckInService`** drops `CategoryId` + `GroupId` from the
   input, pays `XpCalculator.CheckInXp` on every check-in and
   derives streak against the acting user alone.
3. **`StreakService`** stops reading `GroupConfig` and starts
   deriving per-user streak from the user's check-ins.
4. **Progression module** shrinks drastically:
   - Delete `Category`, `CategoryRepository`,
     `ListGroupCategoriesEndpoint`, `CategoryResponse`,
     `CategoryNotFoundException`.
   - Delete `XpRule`, `XpRuleRepository`, `IXpRuleRepository`,
     `XpRulePersistenceTests`.
   - `XpCalculator.Compute(XpRule)` becomes
     `XpCalculator.Compute()` returning the constant.
5. **Groups module** disappears entirely:
   - Delete `Group`, `GroupMembership`, `GroupConfig`,
     `IGroupService`, `GroupService`, `GroupIds` and
     `StreakConfig`.
   - Delete `/groups/{groupId}/categories` endpoint (already
     covered by Category drop).
   - Drop auto-membership logic from `RegisterEndpoint` and
     `GoogleAccountLinker.FindOrCreateAsync`.
6. **One migration — `DropCategoriesAndGroups`** — drops in
   order:
   - FKs: `check_ins → categories`, `check_ins → groups`,
     `categories → groups`, `categories → xp_rules`,
     `group_memberships → groups`, `group_configs → groups`.
   - Columns: `check_ins.category_id`, `check_ins.group_id`.
   - Tables: `categories`, `xp_rules`, `group_memberships`,
     `group_configs`, `groups`.
7. **CheckIn read indexes** that referenced the dropped columns
   get recreated without them. The existing
   `ix_check_ins_user_performed_desc` index is enough for the
   per-user queries that remain.

### 4.2. Mobile

1. Delete `ControlledCategoryPicker` + `useCategories`.
2. `CheckinForm.tsx` loses the picker and the `categoryId`
   zod field.
3. `checkin.service.ts` loses `listCategories`.
4. Delete `GLOBAL_GROUP_ID` from `@xpeak/shared` and strip every
   mobile import. Callers that only used it to feed
   `useCategories(GLOBAL_GROUP_ID)` or to pass `groupId` into the
   check-in payload simply drop the argument.
5. `useCheckinForm.ts` schema drops both `categoryId` and the
   `CHECKIN_MESSAGES.categoryRequired` entry.
6. Groups-related mobile surface is empty today (the `/groups`
   page was already removed when Phase 15 landed), so there is
   no UI to delete.

### 4.3. Specs & docs

- Write ADR
  `docs/adr/0030-drop-groups-and-categories.md` covering the
  decision, the alternatives considered and the Phase 19
  reinterpretation.
- Patch
  `specs/004-categories-and-xp-domain/spec.md`,
  `specs/005-checkin-no-media/spec.md`,
  `specs/007-streak-and-multipliers/spec.md`,
  `specs/019-group-bonuses-and-streak/spec.md` and
  `specs/roadmap.md` with historical notes pointing at ADR 0030.

## 5. Open questions

- **Q1 (settled):** Where does the XP amount live? Hardcoded
  constant in `XpCalculator`. Phase 9 picks the real home
  (`app_settings`, user override, etc.) when it ships.
- **Q2 (settled):** Does streak keep daily/weekly modes? No —
  weekly was schema-ready but never shipped. Daily becomes the
  only mode and the mode selector disappears from the service.
- **Q3 (settled):** What happens to Phase 19's "group bonuses /
  group streak"? Phase 19 is re-scoped to challenges when
  picked up. The current spec gets a note; no code written
  against it here.
- **Q4 (settled):** Backwards compatibility for mobile clients?
  None — the mobile build is unreleased, the cutover is single-
  shot.

## 6. Rollout

Single PR. All layers go together. Testcontainers runs the full
migration chain so the drop is proven clean before the PR merges.
No feature flag.
