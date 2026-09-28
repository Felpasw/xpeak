# Phase 5 — Check-in (No Media) (SPEC)

> Stack: C# 12 + ASP.NET Core 9 + EF Core 9 + Postgres 16 + xUnit + FsCheck.

## 1. Summary

Two tables (`check_ins`, `group_configs`) plus one column
(`users.time_zone`). One end-to-end write flow (`POST /check_ins`)
that inserts a check-in, updates the user's global XP/level, and
returns the derived per-group streak. **Zero media, zero compound
multipliers.**

## 2. Data model

### 2.1. `check_ins`

| Column             | Type          | Constraints                                          |
|--------------------|---------------|------------------------------------------------------|
| `id`               | `uuid`        | PK                                                   |
| `user_id`          | `uuid`        | not null, FK → `users.id` cascade                    |
| `category_id`      | `uuid`        | not null, FK → `categories.id` restrict              |
| `group_id`         | `uuid`        | not null, FK → `groups.id` restrict — denormalized   |
| `xp_earned`        | `int`         | not null, `CHECK (xp_earned > 0)`                    |
| `scoring_snapshot` | `jsonb`       | not null — structured breakdown of the XP calc       |
| `performed_at`     | `timestamptz` | not null                                             |
| `duration_minutes` | `int`         | nullable, `CHECK (duration_minutes > 0)`             |
| `notes`            | `text`        | nullable, `CHECK (length(notes) <= 280)`             |
| `created_at`       | `timestamptz` | not null                                             |
| `updated_at`       | `timestamptz` | not null                                             |

**Indexes:**

- `(user_id, performed_at DESC)` — personal history / list endpoint.
- `(user_id, group_id, performed_at DESC)` — streak derivation
  and per-group-per-user reads.
- `(group_id, performed_at DESC)` — group-level feed / ranking
  queries (Phase 13, 14 will lean on this).

**FK behaviour:**

- `user_id` cascades on user delete (align with existing
  `group_memberships`).
- `category_id` and `group_id` are `restrict` — deleting a
  category or a group with historical check-ins requires explicit
  cleanup, no silent history loss.

**`group_id` invariant:** must equal `category.group_id` at insert
time. Enforced by `CheckInService.CreateAsync` (populates
`group_id` server-side from the loaded category, never accepts it
from the client). Locked by a test.

### 2.2. `scoring_snapshot` JSONB shape

Structured for extensibility. Phase 5 emits:

```json
{
  "base_xp": 15,
  "multipliers": [
    { "source": "category_weight", "value": 1.4 }
  ],
  "total": 21
}
```

Phase 7 appends new multiplier entries without a schema change:

```json
{
  "base_xp": 15,
  "multipliers": [
    { "source": "category_weight", "value": 1.4 },
    { "source": "streak", "value": 1.2 },
    { "source": "group_bonus", "value": 1.1 }
  ],
  "total": 28
}
```

**C# shape:**

```csharp
public sealed record ScoringSnapshot(
    int BaseXp,
    IReadOnlyList<ScoringMultiplier> Multipliers,
    int Total);

public sealed record ScoringMultiplier(string Source, decimal Value);
```

Persisted via EF Core JSON column mapping
(`HasColumnType("jsonb")` + `HasConversion` to/from JSON string).

### 2.3. `group_configs`

1:1 with `groups`. Holds per-group toggles that affect the check-in
/ XP flow.

| Column          | Type          | Constraints                                                          |
|-----------------|---------------|----------------------------------------------------------------------|
| `group_id`      | `uuid`        | PK, FK → `groups.id` cascade                                         |
| `streak_config` | `jsonb`       | not null, default `'{"mode":"daily"}'`                               |
| `created_at`    | `timestamptz` | not null                                                             |
| `updated_at`    | `timestamptz` | not null                                                             |

**Seed:** Global group gets a row via `HasData` in the Phase 5
migration with `{"mode":"daily"}`.

**`streak_config` shape (JSONB on the wire):**

```json
// Phase 5 default
{ "mode": "daily" }

// Phase 9+ (schema-ready, not implemented in Phase 5)
{ "mode": "weekly", "required_days_per_week": 3 }
{ "mode": "weekly", "required_days_per_week": 5, "week_start": "monday" }
```

**C# shape (typed both sides via EF Core value converter):**

```csharp
public enum StreakMode { Daily, Weekly }

public sealed record StreakConfig(
    StreakMode Mode,
    int? RequiredDaysPerWeek = null,   // only meaningful when Mode == Weekly, 1..7
    DayOfWeek? WeekStart = null);      // only when Weekly, default Monday
```

`StreakMode` and `DayOfWeek` serialize as lowercase strings via
`JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower)`. JSONB
column type keeps the shape flexible; EF Core `HasConversion` on
`GroupConfig.StreakConfig` runs `JsonSerializer.Serialize` /
`Deserialize` between the record and the JSONB text.

### 2.4. `users` addition

- Add `time_zone` (`text`, not null, default `"UTC"`, max 64 chars).

No cached streak fields. Streak is derived from `check_ins` on
demand — see §4.2.

## 3. Endpoints

### 3.1. `POST /check_ins`

**Auth:** JWT required.

**Request:**
```json
{
  "category_id": "11111111-1111-1111-1111-111111111111",
  "duration_minutes": 45,
  "notes": "leg day"
}
```

`category_id` is required. `duration_minutes` and `notes` are
optional.

**Response `201`:**
```json
{
  "check_in": {
    "id": "uuid",
    "category_id": "uuid",
    "group_id": "uuid",
    "xp_earned": 21,
    "scoring_snapshot": {
      "base_xp": 15,
      "multipliers": [{ "source": "category_weight", "value": 1.4 }],
      "total": 21
    },
    "performed_at": "2026-09-24T12:34:56.000000Z",
    "duration_minutes": 45,
    "notes": "leg day"
  },
  "user": {
    "id": "uuid",
    "xp": 21,
    "level": 1,
    "leveled_up": true,
    "levels_gained": 1
  },
  "streak": {
    "current": 1,
    "longest": 1,
    "unit": "day"
  }
}
```

Top-level `streak` is scoped to the **group of this check-in**
(`check_in.group_id`), computed after commit. `unit` reflects the
group's `streak_config.mode` — `"day"` for daily mode, `"week"`
for weekly (weekly mode not yet implemented; Phase 5 always
returns `"day"`).

**Failure responses:**

- `401` — missing / invalid / revoked token.
- `403` — user is not a member of `category.group_id`.
- `404` — `category_id` does not exist.
- `422` — validation error:
  - `duration_minutes <= 0`.
  - `notes.Length > 280`.
  - Group has non-daily `streak_config` and Phase 5 impl throws.
    (Won't happen with the seeded Global default.)

### 3.2. `GET /check_ins?limit=&cursor=&group_id=`

**Auth:** JWT required.

**Query params:**

- `limit` — 1..100, default 20.
- `cursor` — opaque base64 string returned by the previous page.
- `group_id` — optional filter; if omitted, all groups the caller
  is a member of.

**Response `200`:**
```json
{
  "check_ins": [ /* same shape as `check_in` in POST response */ ],
  "next_cursor": "opaque-string-or-null"
}
```

Rows always belong to the caller (`user_id = current user`). No
cross-user reads.

**Cursor encoding:** base64 of `"{performed_at:o}|{id}"`. Server
decodes, applies `WHERE (performed_at, id) < (cursor_ts, cursor_id)`
with the primary index.

## 4. Domain logic

### 4.1. `CheckInService.CreateAsync`

```csharp
public sealed class CheckInService(
    AppDbContext db,
    IProgressionService progression,
    IGroupService groups,
    IStreakService streak,
    TimeProvider time) : ICheckInService
{
    public async Task<CreateCheckInResult> CreateAsync(
        Guid userId,
        CreateCheckInInput input,
        CancellationToken ct = default)
    {
        var category = await progression.GetCategoryByIdAsync(input.CategoryId, ct)
            ?? throw new CategoryNotFoundException(input.CategoryId);

        var isMember = await groups.IsMemberAsync(userId, category.GroupId, ct);
        if (!isMember)
        {
            throw new NotAGroupMemberException(userId, category.GroupId);
        }

        var rule = await progression.GetRuleAsync(category.XpRuleId, ct)
            ?? throw new XpRuleNotFoundException(category.XpRuleId);

        var xp = progression.ComputeXp(rule);
        var snapshot = new ScoringSnapshot(
            rule.BaseXp,
            [new ScoringMultiplier("category_weight", rule.WeightMultiplier)],
            xp);

        var user = await db.Users.SingleAsync(u => u.Id == userId, ct);
        var prevXp = user.Xp;
        var newXp = prevXp + xp;
        var levelUp = progression.EvaluateLevelUp(prevXp, newXp);

        var checkIn = new CheckIn
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CategoryId = category.Id,
            GroupId = category.GroupId,   // server-populated, never from client
            XpEarned = xp,
            ScoringSnapshot = snapshot,
            PerformedAt = time.GetUtcNow(),
            DurationMinutes = input.DurationMinutes,
            Notes = input.Notes,
        };
        db.CheckIns.Add(checkIn);

        user.Xp = newXp;
        user.Level = levelUp.NewLevel;

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        // Post-commit: derive streak for the response.
        var streakInfo = await streak.ComputeAsync(
            userId,
            category.GroupId,
            DateOnly.FromDateTime(checkIn.PerformedAt.UtcDateTime),
            ct);

        return new CreateCheckInResult(checkIn, user, levelUp, streakInfo);
    }
}
```

Notes on the transaction:
- `SaveChangesAsync` runs the insert + the user update as a single
  batch under one Postgres transaction.
- Streak is queried **after** commit — it reads the just-inserted
  row and never participates in the write set. Safe even if the
  streak query fails (returns 500, but the check-in is already
  persisted; retry on the client picks up correctly).

### 4.2. `StreakService.ComputeAsync` (derived)

```csharp
public sealed class StreakService(
    AppDbContext db,
    IGroupConfigService configs) : IStreakService
{
    public async Task<StreakInfo> ComputeAsync(
        Guid userId,
        Guid groupId,
        DateOnly asOf,
        CancellationToken ct = default)
    {
        var streakConfig = await configs.GetStreakConfigAsync(groupId, ct);

        var days = await db.CheckIns
            .AsNoTracking()
            .Where(c => c.UserId == userId && c.GroupId == groupId)
            .Select(c => c.PerformedAt)
            .ToListAsync(ct);

        var distinctDates = days
            .Select(d => DateOnly.FromDateTime(d.UtcDateTime))
            .Distinct()
            .OrderByDescending(d => d)
            .ToList();

        return StreakCalculator.Compute(distinctDates, asOf, streakConfig);
    }
}
```

Bounded read: even 5 years of daily check-ins is ~1800 dates.
Query uses `(user_id, group_id, performed_at DESC)` index.

### 4.3. `StreakCalculator` (pure static)

```csharp
public enum StreakUnit { Day, Week }

public sealed record StreakInfo(int Current, int Longest, StreakUnit Unit);

public static class StreakCalculator
{
    public static StreakInfo Compute(
        IReadOnlyList<DateOnly> distinctDatesDescending,
        DateOnly asOf,
        StreakConfig config)
    {
        if (config.Mode != StreakMode.Daily)
        {
            throw new NotSupportedException(
                $"Streak mode '{config.Mode}' is not implemented in Phase 5.");
        }

        // current: walk backwards from asOf, counting consecutive days.
        var current = 0;
        var expected = asOf;
        foreach (var date in distinctDatesDescending)
        {
            if (date == expected)
            {
                current++;
                expected = expected.AddDays(-1);
            }
            else if (date < expected)
            {
                break;   // gap
            }
            // date > expected: check-in in the future — ignore.
        }

        // longest: single pass over the same list, tracking run length.
        var longest = ComputeLongest(distinctDatesDescending);

        return new StreakInfo(current, longest, StreakUnit.Day);
    }

    private static int ComputeLongest(IReadOnlyList<DateOnly> descending)
    {
        if (descending.Count == 0)
        {
            return 0;
        }

        var longest = 1;
        var run = 1;
        for (var i = 1; i < descending.Count; i++)
        {
            if (descending[i] == descending[i - 1].AddDays(-1))
            {
                run++;
                longest = Math.Max(longest, run);
            }
            else
            {
                run = 1;
            }
        }
        return longest;
    }
}
```

**FsCheck-friendly invariants** (locked in tests):
- `Compute(empty, today, daily) == StreakInfo(0, 0, Day)`.
- `Compute([today], today, daily) == StreakInfo(1, 1, Day)`.
- Same-day duplicate has no effect (distinct filter upstream).
- `current <= longest` always.
- Adding a gap-day date resets `current` to 1 on the next same-day.
- Consecutive `n` days back from `asOf` yields `current == n`.

## 5. Module layout

```
apps/api/src/CheckIns/
  Entities/
    CheckIn.cs
  Dto/
    CreateCheckInRequest.cs
    CheckInResponse.cs
    CheckInPageResponse.cs
    ScoringSnapshot.cs
    ScoringMultiplier.cs
    StreakInfo.cs
  Repositories/
    ICheckInRepository.cs
    CheckInRepository.cs
  Services/
    ICheckInService.cs
    CheckInService.cs
    IStreakService.cs
    StreakService.cs
    StreakCalculator.cs
  Endpoints/
    CreateCheckInEndpoint.cs
    ListCheckInsEndpoint.cs
  CheckInsModule.cs

apps/api/src/Groups/
  Entities/
    Group.cs
    GroupMembership.cs
    GroupConfig.cs               ← NEW
  Services/
    IGroupService.cs             ← +IsMemberAsync
    GroupService.cs              ← impl
    IGroupConfigService.cs       ← NEW
    GroupConfigService.cs        ← NEW
  GroupIds.cs
  GroupsModule.cs                ← register the new service

apps/api/src/Progression/
  Services/
    IProgressionService.cs       ← +GetCategoryByIdAsync, +GetRuleAsync
    ProgressionService.cs        ← impl
```

**DI:**

- `CheckInsModule.AddCheckIns()` registers repo + service + streak
  service + calculator (static, no registration).
- `GroupsModule.AddGroups()` gains
  `AddScoped<IGroupConfigService, GroupConfigService>()`.
- `Program.cs` chain:
  `.AddUsers().AddGroups().AddProgression().AddCheckIns().AddAuthCore(...).AddPasswordAuth().AddGoogleAuth(...)`.
- `app.UseCheckIns()` mounts the two endpoints under `/check_ins`.

## 6. Mobile

### 6.1. `/checkin` screen (replaces the current `ComingSoon`)

- Header: "New check-in".
- Group indicator (Phase 5 shows "Global" — future phases let the
  user switch).
- Category grid — cards with icon (Cloudinary URL built from
  `category.icon_public_id` + transformations) and name. Tap to
  select.
- Optional duration slider (0–180 min).
- Optional notes input (280 char cap, character counter).
- "Log check-in" button — disabled until a category is picked.
- On submit → `POST /check_ins` → success toast + level-up overlay
  (if `leveled_up`) → redirect to `/profile`.

### 6.2. Level-up overlay

Renders when `response.user.leveled_up === true`. Shows:
`"Level up! You're now level ${response.user.level}."` plus a small
celebration animation (existing `motion/react` primitives).
Dismisses on tap.

### 6.3. Profile refresh

- `useMe` cache is invalidated on successful check-in mutation via
  the standard TanStack Query `onSuccess` hook, so `/profile`
  shows fresh `xp` / `level` on next render.
- Streak on profile stays on Global's streak for MVP; the
  check-in response includes streak for the acting group but the
  profile view doesn't display it (Phase 5 profile scope).

## 7. Test plan

### 7.1. API

```
apps/api.Tests/CheckIns/
  CheckInPersistenceTests.cs           # Testcontainers — schema constraints
  StreakCalculatorTests.cs             # xUnit + FsCheck properties
  StreakServiceTests.cs                # Testcontainers — end-to-end
  CheckInServiceTests.cs               # Testcontainers — transaction, membership, invariants
  CreateCheckInEndpointTests.cs        # Testcontainers — HTTP layer
  ListCheckInsEndpointTests.cs         # Testcontainers — cursor pagination

apps/api.Tests/Groups/
  GroupConfigTests.cs                  # NEW — seed of Global, JSONB round-trip, IsMemberAsync
```

### 7.2. Coverage targets

- **CheckInPersistenceTests**:
  - `xp_earned <= 0` rejected by DB CHECK.
  - `duration_minutes = 0` rejected.
  - `notes` > 280 chars rejected.
  - Missing FK (unknown user / category / group) rejected.
- **StreakCalculatorTests**:
  - Empty list → `(0, 0)`.
  - Single date == asOf → `(1, 1)`.
  - N consecutive days back from asOf → `(N, N)`.
  - Gap resets current, longest preserved.
  - Property: `current <= longest`.
  - Property: same-day duplicate irrelevant (input is distinct).
  - Non-daily config throws `NotSupportedException`.
- **StreakServiceTests**:
  - Fresh user + no check-ins → `(0, 0)`.
  - After first check-in → `(1, 1)`.
  - Two consecutive days → `(2, 2)`.
  - Gap day → next check-in `(1, 2)`.
  - Two groups isolated: check-ins in group A don't affect streak
    in group B.
- **CheckInServiceTests**:
  - Happy path bumps user `xp` and `level` in one transaction.
  - `group_id` on the inserted row equals `category.group_id`
    even if the client sends garbage (invariant test).
  - Non-member of the category's group → `NotAGroupMemberException`
    (no row inserted).
  - Unknown category → `CategoryNotFoundException` (no row).
  - Scoring snapshot round-trip: JSON persisted matches
    `{base_xp, multipliers:[{source:"category_weight",value:...}], total}`.
- **CreateCheckInEndpointTests**:
  - 201 happy path + payload shape.
  - 401 without token.
  - 403 for non-member.
  - 404 for unknown category.
  - 422 for `notes` > 280 / `duration <= 0`.
- **ListCheckInsEndpointTests**:
  - Returns only the caller's rows (never leaks another user's).
  - Ordered newest first.
  - `next_cursor` round-trips: fetching page N+1 with the returned
    cursor excludes items from page N.
  - `group_id` filter returns only that group's rows.
- **GroupConfigTests**:
  - Global group has `group_configs` row after migration.
  - `StreakConfig` JSONB round-trips through EF conversion.
  - `IsMemberAsync` returns true for the Global membership added
    at register.

### 7.3. Mobile

```
apps/mobile/test/services/checkin.service.spec.ts
apps/mobile/test/hooks/useCheckIn.spec.tsx
apps/mobile/test/app/(app)/checkin/page.spec.tsx
apps/mobile/test/components/organisms/CheckinForm.spec.tsx
apps/mobile/test/components/atoms/LevelUpOverlay.spec.tsx
```

Coverage:
- Screen renders category grid from mocked `/categories` endpoint.
- Submit disabled until a category is picked.
- On 201, `/profile` navigated to; level-up overlay renders when
  `leveled_up === true`.
- `useMe` cache invalidation happens on success.

### 7.4. Fixtures

- Reuse `XpeakWebApplicationFactory` (Testcontainers per test class).
- `StreakCalculatorTests` is fixture-less (pure math).

## 8. Non-goals

- Media attachments (Phase 6).
- Compound multipliers beyond category weight (Phase 7).
- Weekly streak calc implementation (schema ready, calc throws).
- Editable admin surface for `xp_rules` / `group_configs`
  (Phase 9).
- Group CRUD, invitations, sub-group creation (Phase 12+).
- Feed / timeline emission on check-in (Phase 13).
- Group-scoped rankings endpoint (Phase 14).
- Undo / edit / delete a check-in.
- TZ-aware streak math beyond UTC.
- HTTP endpoint listing categories — arrives on demand when the
  mobile check-in screen consumes it (added under `Progression`
  as a small `GET /groups/{id}/categories` endpoint if the mobile
  work needs it; otherwise the check-in flow reads categories via
  the existing `IProgressionService.ListActiveCategoriesAsync`
  behind a Phase-5-only endpoint).
