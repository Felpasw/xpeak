# ADR 0005 — Check-in and streak (schema, group scoping, derived streak, tz-aware boundaries)

- **Status:** Accepted
- **Date:** 2026-09-26
- **Context:** Phase 5 (`specs/005-checkin-no-media/`), shipped on
  branch `XPK-16/felpa-checkin`.

## Context

Phase 5 is the first phase that turns the XP engine into a real
loop: a user opens the app, taps a category, submits, and their
`xp` / `level` / streak move. Every phase beyond this consumes the
same `check_ins` row and the same streak surface — Phase 6 attaches
media, Phase 7 stacks streak / group / challenge multipliers on the
same scoring snapshot, Phase 11 hangs medal thresholds off the
streak service, Phase 13 emits feed events from the check-in
transaction, Phase 14 reads the group-scoped ranking indexes.

Locking in the wrong shape here compounds badly, so we made ten
decisions up-front instead of shipping the smallest thing possible
and migrating later.

## Decisions

### 1. `check_ins.group_id` is denormalized from the category

Every check-in row carries `group_id` in addition to `category_id`,
populated server-side from the loaded category. The client cannot
pass it in; the request DTO doesn't have the field.

**Why:**
- The hottest downstream query is "give me every check-in in group
  X for the last N days" (rankings in Phase 14, feed in Phase 13,
  group streak in Phase 19). Without denormalization, every one of
  those queries needs a `JOIN categories ON ... WHERE group_id = ?`,
  which the planner can't collapse as cheaply as a single-table
  `WHERE group_id = ?` on the `(group_id, performed_at DESC)`
  composite index we shipped in T-005-01.
- Historical integrity: if an admin ever moves a category between
  groups (Phase 9 admin surface), the past check-ins **must not
  migrate retroactively**. Denormalizing at insert time locks the
  scope of that check-in forever.

**Trade-off:** the invariant `check_ins.group_id == category.group_id`
lives in the service layer instead of the DB. Locked with an
explicit test (`CheckInServiceTests.Populates_group_id_from_the_category_regardless_of_context`)
and by never accepting `group_id` on the wire.

### 2. Membership is enforced in the service, not the endpoint

`CheckInService.CreateAsync` calls
`IGroupService.IsMemberAsync(userId, category.GroupId)` before
anything else. If the caller isn't a member, it throws
`NotAGroupMemberException`, which the endpoint layer maps to HTTP
403.

**Why:**
- The check is a business rule (only members can write into a
  group's history), not a routing concern. Middleware / route
  filters can't see which group is being written to — that's
  derived from the loaded category, mid-request.
- Every future write path (Phase 15+ challenge submissions,
  Phase 19 group bonuses) will need the same check. Keeping it in
  a service the endpoints call keeps the enforcement DRY and
  testable at the service level.

### 3. Payload uses `category_id`, not `category_slug`

The `POST /check_ins` body accepts a `categoryId` UUID. Slugs are
never on the write path.

**Why:**
- Since Phase 4 categories are group-scoped, slug is `(group_id,
  slug)` unique — not globally unique. A payload with just
  `category_slug` would be ambiguous.
- The mobile client lists categories via `GET /groups/{groupId}/categories`
  before the picker renders, so it already has the ids. Passing
  them directly avoids a server-side slug→id lookup and any risk
  of the client resolving the wrong scope.

### 4. `scoring_snapshot` is a structured JSONB from day one

Every `check_in` row carries a `scoring_snapshot` JSONB column with
the shape
`{ baseXp, multipliers: [{source, value}], total }`.
Phase 5 writes exactly one multiplier (`category_weight`); Phase 7
will append `streak`, `group_bonus`, `challenge` entries into the
same array without a schema change.

**Why:**
- Audit: even after admins edit an XP rule in Phase 9, historical
  check-ins still show the numbers that were in effect at the time
  they were logged. No retroactive rewrite of past XP.
- UI breakdown: mobile can render the "Base 15 × 1.4 weight = 21
  XP" breakdown the user asked for in review, straight from the
  snapshot — no client-side reconstruction of what the server
  computed.
- Migration avoidance: Phase 7 doesn't need an
  `ALTER TABLE check_ins ADD COLUMN multiplier_snapshot` or a
  backfill; it appends to a JSONB array already there.

**Trade-off:** ~50–100 bytes of JSONB per row. Negligible at any
realistic scale.

### 5. Streak is per-group AND derived on demand

There is no `group_streaks` table. `IStreakService.ComputeAsync(
userId, groupId, asOf)` queries `check_ins` for the distinct dates
the user has checked into that group and hands them to a pure
`StreakCalculator.Compute` static.

**Why (per-group, not global):**
- Phase 7's streak multiplier gives bonus XP proportional to
  current streak. If streak were global, a user could farm a
  30-day streak by daily check-ins in the easy "Global" group and
  then dump one check-in in a harder sub-group to trigger the
  bonus there. Per-group streaks close that loop — you earn the
  multiplier only in the context you built it in.

**Why (derived, not cached):**
- No cache invalidation to get wrong. If the row is derived from
  `check_ins`, it can never disagree with the source of truth.
- If a group is ever deleted, no orphaned rows in a side table —
  the `check_ins` CASCADE takes care of everything.
- The query is bounded by the `(user_id, group_id, performed_at
  DESC)` composite index; even at years of daily check-ins the
  set is under 2000 dates. `ComputeCurrent` breaks at the first
  gap; `ComputeLongest` is one O(n) pass.
- Original `AppUser` docstring (shipped in Phase 3) already said
  "streaks are NOT cached; they are derived on demand from
  check_ins." Following the intent instead of fighting it.

**Trade-off:** one extra query per profile load / per check-in
response. Sub-ms with the index.

### 6. Streak config lives in `group_configs`, not inline on `Group`

New `group_configs` table (1:1 with `groups`, cascade-delete) holds
a `streak_config` JSONB. Global group is seeded with
`{"mode":"daily"}` in the same migration that creates the table.

**Why not inline on `Group`?**
- Phase 7 will add group-level bonus multipliers, Phase 11 will
  add streak medal thresholds, Phase 9 will add editable-XP knobs.
  If they all pile onto `groups`, the table becomes a bag of
  unrelated fields. A separate 1:1 config table gives them a
  natural home and keeps `groups` about identity.

**Why not a generic `xp_config` (scope + key + value) table?**
- Overkill for Phase 5. Migrating `group_configs` into a broader
  config table when Phase 9 wires the admin surface is trivial;
  building the abstraction now costs more than deferring.

### 7. Weekly streak mode is schema-ready but not implemented

The `streak_config` JSONB shape accepts
`{"mode":"weekly","required_days_per_week":3,"week_start":"monday"}`,
mapped to a strongly-typed C# record
(`StreakConfig(Mode, RequiredDaysPerWeek, WeekStart)` with
`StreakMode` enum + `DayOfWeek`). `StreakCalculator` throws
`NotSupportedException` on anything other than `StreakMode.Daily`.

**Why:**
- Locking the shape now avoids a JSONB schema migration when
  Phase 9 / Phase 11 turn on weekly. Existing rows deserialise with
  default `RequiredDaysPerWeek = null`, `WeekStart = null` — no
  backfill required.
- Failing loud on unimplemented modes prevents silent bugs. A
  group whose config accidentally lands with `"weekly"` gets a
  500 on read, not a wrong streak.

### 8. Streak boundaries live in the user's timezone

`AppUser.TimeZone` defaults to `"America/Sao_Paulo"` (Brazilian
audience, override in profile later). `StreakService` loads that
TZ and converts every `performed_at` timestamp with
`TimeZoneInfo.ConvertTime` before extracting a `DateOnly`.
`CheckInService` computes the `asOf` cursor the same way, so both
sides bucket dates against the same frame of reference.

**Why:**
- A user checking in at 22:00 SP on Monday and 08:00 SP on Tuesday
  is on two distinct days. Using UTC would collapse the Monday
  check-in into Tuesday (01:00 UTC) and either double-count Tuesday
  or break the streak silently.
- Storing timestamps in UTC (`DateTimeOffset`) stays the best
  practice; only the derivation of "which day" flips into the
  user's TZ.

**Trade-off:** every streak query loads the user row for the TZ.
Sub-ms; not worth caching.

### 9. FluentValidation as the endpoint-level validation contract

`POST /check_ins` uses `CreateCheckInRequestValidator :
AbstractValidator<CreateCheckInRequest>`, run by a small in-house
`ValidationEndpointFilter<T>` that catches failures and returns
HTTP 422 with a structured
`{ errors: [{field, message}] }` body. Endpoint handlers stop
carrying `if (payload.Notes.Length > 280)` scaffolding.

**Why:**
- The C# equivalent of Zod in expressiveness. Cross-property rules
  (e.g. `RuleFor(x => x.DurationMinutes).GreaterThan(0).When(x =>
  x.DurationMinutes.HasValue)`) that are painful with Data
  Annotations become one line.
- Validators are their own classes → unit-testable without booting
  the whole HTTP pipeline.
- Adding a new endpoint from Phase 6 onwards is
  `.ValidateBody<TDto>()` plus a validator file — one line at the
  route, zero if-branches in the handler.

**Trade-off:** one new NuGet dep
(`FluentValidation.DependencyInjectionExtensions`). Small package,
stable, well-maintained.

### 10. Mobile shape — interfaces separated, form composed of Controlled\* atoms

- Every service has `services/interfaces/<name>.interface.ts` +
  `services/<name>.service.ts`.
- Every non-trivial hook has `hooks/interfaces/<name>.interface.ts`
  for its result types.
- Every form input in the check-in flow is a `Controlled<X>` atom
  wrapping the visual `<X>` atom with react-hook-form `Controller`
  — `ControlledInput`, `ControlledTextarea`, `ControlledCategoryPicker`.
- `<CheckinForm />` composes four Controlled atoms plus the overlay
  — 55 lines, zero raw form JSX, zero ternaries in the render.

**Why:**
- Interfaces separated mirrors the backend module pattern
  (`IGroupService`, `IProgressionService`, etc.). Consumers depend
  on the contract, not the impl.
- Every future form (Phase 6 media picker, Phase 12 friend search,
  Phase 15 challenge builder) drops into the same
  `Controlled<X>` shape. New devs learn the pattern once.
- Early-return branches instead of nested ternaries in the render
  keep the JSX flat (`CheckinForm` and `CategoryPicker` both).

## Consequences

**Now:**

- 2 new tables (`check_ins`, `group_configs`) + `users.time_zone`.
- 2 new mobile atoms (`Textarea`, `CategoryPicker`) plus 2
  `Controlled*` wrappers plus `LevelUpOverlay`.
- 1 new organism (`CheckinForm`), 1 new hook pair
  (`useCheckIn` + `useCheckinForm`).
- 3 new endpoints (`POST /check_ins`, `GET /check_ins`,
  `GET /groups/{groupId}/categories`).
- 214 tests total across the api and mobile suites
  (`174` api + `37` mobile + `3` new categories endpoint).

**Downstream unlocks:**

- **Phase 6 (media)** — attaches uploaded assets to `check_ins`;
  the `scoring_snapshot` shape doesn't care.
- **Phase 7 (streak / group / challenge multipliers)** — appends
  entries to `scoring_snapshot.multipliers`; reads streak via
  `IStreakService`; adds a `bonus_rule_id` (nullable FK) to
  `group_configs` for group-level bonuses.
- **Phase 8 (themed titles)** — reads `AppUser.Level`, maps to
  titles via a new `level_title_tiers` table.
- **Phase 9 (editable XP config)** — implements the weekly-mode
  branch of `StreakCalculator`, exposes `xp_rules` +
  `group_configs.streak_config` behind the admin surface.
- **Phase 11 (streak medals)** — hangs threshold rules off
  `IStreakService.ComputeAsync` results.
- **Phase 12+ (sub-groups)** — group CRUD, membership management,
  and the switch away from the hardcoded `GLOBAL_GROUP_ID` on the
  mobile client.
- **Phase 13 (feed / notifications)** — the same check-in
  transaction becomes the emitter of `check_in.created` and
  `check_in.leveled_up` events.
- **Phase 14 (rankings)** — leans on the
  `(group_id, performed_at DESC)` index shipped in T-005-01 for
  the leaderboard queries.

**Trade-offs accepted:**

- Denormalized `group_id` on `check_ins` — application-enforced
  invariant, not DB-enforced. Test locks it.
- Extra query per profile load for TZ + derived streak — sub-ms;
  not worth caching until it isn't.
- Structured JSONB scoring snapshot ships with a single-multiplier
  entry — dead weight until Phase 7 fills the array.
- FluentValidation adds a NuGet dep purely for two validation
  rules today — pays off with every new endpoint from Phase 6 on.
- pt-BR strings hardcoded in the check-in flow while the auth
  screens stay in EN — inconsistent until the auth migration
  ships in a follow-up bundle.

## Related

- `specs/005-checkin-no-media/plan.md`
- `specs/005-checkin-no-media/spec.md`
- `specs/005-checkin-no-media/tasks.md`
- `docs/adr/0004-xp-engine.md` (curve, rounding, rule extraction,
  group scoping, `Xp` namespace split — all consumed here)
- Roadmap Phase 6, Phase 7, Phase 8, Phase 9, Phase 11, Phase 12,
  Phase 13, Phase 14, Phase 19.
