# Phase 31 — Challenge Tiers (SPEC)

> Builds on top of XPK-32 (share-link MVP). Assumes `challenges`,
> `challenge_memberships` and `challenge_invitations` exist as of
> that merge. The membership's `progress.value` is the ladder's
> input: tiers are not computed from the user's global XP, only
> from the member's challenge-scoped progress.

## 1. Data model

### 1.1. `challenge_tiers`

| Column         | Type              | Constraints                                     |
|----------------|-------------------|-------------------------------------------------|
| `id`           | uuid              | PK                                              |
| `challenge_id` | uuid              | FK → `challenges`, cascade on delete            |
| `min_level`    | integer           | not null, `> 0`                                 |
| `title`        | varchar(60)       | not null, `length(trim(title)) BETWEEN 1 AND 60`|
| `created_at`   | timestamptz       | not null                                        |
| `updated_at`   | timestamptz       | not null                                        |

Constraints:
- Check `ck_challenge_tiers_min_level_positive`:
  `min_level > 0`.
- Check `ck_challenge_tiers_title_shape`:
  `length(trim(title)) BETWEEN 1 AND 60`.
- Unique index `ux_challenge_tiers_challenge_min_level`:
  `(challenge_id, min_level)` — no two tiers in the same
  challenge share a floor, and serves ordered reads cheaply.

There is no separate `position` column: tier order is always
derived from `min_level ASC`. The service and HTTP response
return the list already sorted; the client renders by array
index.

Hard cap enforced at the service layer (not the DB):
**max 10 tiers per challenge**. Rejects extra rows with a
`TooManyTiersException` → HTTP 422.

### 1.2. No changes to `challenges` or `challenge_memberships`

Tiers hang off `challenges` by FK; nothing on the parent row
changes. Membership keeps the same `progress` JSONB; the current
tier is derived at read time, not cached.

## 2. Domain

### 2.1. Entity

```csharp
public sealed class ChallengeTier
{
    public Guid Id { get; set; }
    public Guid ChallengeId { get; set; }
    public int MinLevel { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
```

No enum. No value object. No position column. The row is the
whole story.

### 2.2. Resolver

Static pure function on the entity (or a tiny service, decide
during implementation):

```csharp
public static ChallengeTier? ResolveCurrent(
    IReadOnlyList<ChallengeTier> ordered, int progressValue)
{
    // ordered must be sorted by MinLevel ascending.
    ChallengeTier? current = null;
    foreach (var tier in ordered)
    {
        if (tier.MinLevel > progressValue) break;
        current = tier;
    }
    return current;
}
```

Returns `null` when progress is below the first tier. The
resolver never looks at the next tier — the mobile derives
"next" from the ordered list + current index.

### 2.3. Service wiring

`ChallengeService.CreateAsync` grows a tiers argument on
`CreateChallengeInput`:

```csharp
public sealed record CreateChallengeInput(
    string Title,
    string? Description,
    string? IconPublicId,
    ChallengeVisibility Visibility,
    int TargetLevel,
    decimal XpMultiplier,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    int? MemberCap,
    IReadOnlyList<CreateChallengeTierInput>? Tiers = null);

public sealed record CreateChallengeTierInput(
    int MinLevel,
    string Title);
```

Behaviour:
- `null` or empty list ⇒ no tier rows persisted. `GetChallengeResponse.Tiers`
  will be an empty list.
- Non-empty list ⇒ validate count ≤ 10, each `min_level > 0`,
  each title non-blank 1..60 chars, no duplicate `min_level`
  within the input. Normalise titles with `Trim()`.
- All inserts land in the same `SaveChangesAsync` as the
  challenge, owner membership and share-link invitation. Order
  is implicit via `min_level`.

`ChallengeService.GetForUserAsync` grows two outputs:
- `IReadOnlyList<ChallengeTier> Tiers` — always populated
  (empty when none).
- `ChallengeTier? CurrentTier` — resolved via the pure function
  when the caller has a membership with
  `progress.value > 0`; otherwise `null`.

Both land on `ChallengeDetail`:

```csharp
public sealed record ChallengeDetail(
    Challenge Challenge,
    ChallengeMembership? Membership,
    ChallengeInvitation? ShareLink = null,
    IReadOnlyList<ChallengeTier>? Tiers = null,
    ChallengeTier? CurrentTier = null);
```

Null `Tiers` is the "unset" marker used by `ListForUserAsync`,
which does not load tiers for the list surface. `GetForUserAsync`
always materialises the list (possibly empty).

## 3. Endpoints

### 3.1. `POST /challenges` request body

```json
{
  "title": "100 dias de treino",
  "description": null,
  "iconPublicId": null,
  "visibility": "private",
  "targetLevel": 60,
  "xpMultiplier": 1.0,
  "startsAt": "2026-10-20T00:00:00Z",
  "endsAt": "2027-01-28T00:00:00Z",
  "memberCap": null,
  "tiers": [
    { "minLevel": 10, "title": "Aprendiz" },
    { "minLevel": 30, "title": "Mago" },
    { "minLevel": 60, "title": "ArchMago" }
  ]
}
```

`tiers` is optional. Omitting it (or `null` or `[]`) persists no
tier rows.

Validator rules on `tiers`:

- Count `0..10`.
- Each row:
  - `minLevel` integer, `>= 1`, `<= 999` (same ceiling as
    `targetLevel`).
  - `title` string, trimmed length `1..60`.
- Unique `minLevel` within the list.

Failure modes:

- Count `> 10` ⇒ 422 with validation error on `tiers`.
- Any individual row invalid ⇒ 422 with per-index error.
- Duplicate `minLevel` ⇒ 422 with "minLevel must be unique".

### 3.2. `POST /challenges` response body

Grows `tiers`:

```json
{
  "challenge": { ... },
  "ownerMembership": { ... },
  "shareLink": { ... },
  "tiers": [
    { "id": "uuid", "minLevel": 10, "title": "Aprendiz" },
    { "id": "uuid", "minLevel": 30, "title": "Mago" },
    { "id": "uuid", "minLevel": 60, "title": "ArchMago" }
  ]
}
```

Empty array when no tiers were posted.

### 3.3. `GET /challenges/{id}` response body

Grows `tiers` and `currentTier`:

```json
{
  "challenge": { ... },
  "membership": { ... },
  "shareLink": { ... } | null,
  "tiers": [
    { "id": "uuid", "minLevel": 10, "title": "Aprendiz" },
    { "id": "uuid", "minLevel": 30, "title": "Mago" },
    { "id": "uuid", "minLevel": 60, "title": "ArchMago" }
  ],
  "currentTier": { "id": "uuid", "minLevel": 30, "title": "Mago" }
}
```

- `currentTier` is `null` when:
  - the caller has no membership on this challenge; or
  - the membership exists but `progress.value < tiers[0].minLevel`;
  - the challenge has no tiers.

### 3.4. No new endpoints

- No `POST /challenges/{id}/tiers`.
- No `PATCH /challenges/{id}/tiers`.
- No `DELETE /challenges/{id}/tiers/{tierId}`.

Tiers are frozen at challenge creation. The owner must delete
the challenge to replace them.

## 4. Shared types

```ts
export interface ChallengeTier {
    id: string;
    minLevel: number;
    title: string;
}

export interface CreateChallengeTierInput {
    minLevel: number;
    title: string;
}

export interface CreateChallengeRequest {
    // ...existing fields...
    tiers?: CreateChallengeTierInput[];
}

export interface CreateChallengeResponse {
    // ...existing fields...
    tiers: ChallengeTier[];
}

export interface GetChallengeResponse {
    // ...existing fields...
    tiers: ChallengeTier[];
    currentTier: ChallengeTier | null;
}
```

## 5. Mobile

### 5.1. Create form

- New collapsible section "Fases (opcional)".
- Repeatable row: `[Lv min]` + `[Título]` + `[remover]`.
- "Adicionar fase" button; disabled when 10 rows are present.
- Submit payload omits `tiers` entirely when zero rows; sends
  the array otherwise.
- Zod schema in `useCreateChallengeForm` (or inline) validates
  per field + enforces the 10-row cap.

### 5.2. Detail organism `<ChallengeDetailTiers>`

- Rendered only when `tiers.length > 0`.
- The index into `tiers` matches `currentTier` by id to pick the
  "current" row — there is no numeric position field.
- Ladder layout:
  - Tiers before the current one in `tiers` ⇒ muted, labeled
    "Alcançado".
  - `currentTier` ⇒ highlighted ring + "Atual" badge.
  - Tiers after the current one ⇒ default; the immediate next
    one is labeled "Próximo", the rest "Em breve".
- When `currentTier == null`:
  - If `tiers.length > 0` ⇒ first tier shown as "Próximo", the
    rest as "Em breve".
  - If `tiers.length == 0` ⇒ organism not rendered.

### 5.3. Shared hooks

The challenges hooks module (`challengesHooks`) does not grow
new methods — `useDetail` and `useCreate` already return the
expanded types.

## 6. Test plan

### 6.1. Backend

- `ChallengeTierPersistenceTests`: round-trip, rejects
  `min_level <= 0`, rejects blank title, rejects duplicate
  `min_level` on the same challenge, cascades on challenge
  delete.
- `ChallengeTierResolverTests` (unit, no DB): resolves correctly
  for a progress between two tiers, returns null below the
  first, returns the last when progress exceeds every tier.
- `ChallengeServiceTests` (new cases):
  - CreateAsync persists tiers and GetForUserAsync returns them
    sorted by min_level ascending.
  - CreateAsync with 11 tiers throws `TooManyTiersException`.
  - CreateAsync with duplicate `min_level` throws
    `DuplicateTierException`.
  - GetForUserAsync returns tiers + computed currentTier.
  - GetForUserAsync for a non-member returns tiers but
    `currentTier == null`.
- `ChallengesEndpointTests` (new cases):
  - 201 happy path with 3 tiers echoes them in the response.
  - 422 on 11 tiers.
  - 422 on duplicate `minLevel` in request body.
  - 422 on blank or 61-char title.
  - GET detail returns `tiers[]` and `currentTier`.

### 6.2. Mobile

- `CreateChallengeForm.spec.tsx` new cases:
  - Adds and removes tier rows.
  - Rejects submission with blank tier title.
  - Submits payload with `tiers: [...]`.
  - Submits without `tiers` when zero rows (field omitted or
    empty array — pick one and assert it).
- `ChallengeDetailTiers.spec.tsx` (new):
  - Renders a 3-step ladder with the middle tier highlighted
    given a mocked `currentTier`.
  - Renders nothing when `tiers` is empty.
- `view/page.spec.tsx` extended: owner with tiers sees the
  ladder; non-member on public with tiers also sees the ladder
  but no "Atual" badge.

## 7. Non-goals

- Editing tiers after challenge creation.
- Per-tier artwork or avatar swap.
- Automatic insignia issuance on tier reach.
- Server-emitted "tier reached" event / notification.
- Reward hooks (XP bonus, unlocks).
- Visual badge / shareable card for reaching a tier.
- Localization of tier titles.

## 8. Open follow-ups

- **Phase 18 extension** — when insignias ship, consider wiring
  "insignia on tier reached" with a new row in `user_insignias`
  keyed by `(user_id, challenge_id, tier_id)`.
- **Phase 22 extension** — when evolution forms ship, allow a
  tier to reference a `theme_form_id` for the member's avatar
  swap within the challenge.
- **Phase 24 extension** — when push lands, "You just reached
  Mago in <challenge>" becomes a push category.
- **Tier edit endpoint** — if beta feedback asks for it, add a
  dedicated `PUT /challenges/{id}/tiers` that replaces the full
  list. Would need to decide whether existing memberships keep
  the pre-edit `currentTier` snapshot or jump to the new
  resolution. Deferred until there is signal.
