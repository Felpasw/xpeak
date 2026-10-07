# Phase 29 — Groups Listing (SPEC)

> Stack: Next.js 16 + Capacitor + TanStack Query + Vitest (mobile);
> ASP.NET Core 9 + xUnit (api).

## 1. Summary

Split the current `/groups` surface into a two-step flow:

1. **`/groups`** — list of groups the user is a member of (picker).
2. **`/groups/[id]`** — the per-group check-in history (what
   `/groups` renders today, hard-coded to `GLOBAL_GROUP_ID`).

Backend grows a `GET /groups` endpoint returning the caller's
memberships (no admin listing, no public discovery — those come
with phase 15+ challenges). Mobile adds a `GroupCard` atom + a
`GroupList` organism and introduces the dynamic route.

Today only the `Global` group exists in production, so the picker
will show a single card. That's intentional: ship the plumbing
now so phase 15+ (challenges) can drop multi-group content in
without re-wiring the navigation.

## 2. Backend contract

### 2.1. New endpoint

```
GET /groups
→ 200 {
    groups: GroupListItem[]
  }
```

- Auth required. Returns groups where the caller has a row in
  `group_memberships`.
- No pagination (realistic cap: a user won't belong to hundreds
  of groups; if that ever changes, add cursor pagination then).
- Ordered by `joined_at ASC` so the Global group (seeded on
  signup) stays on top and newer groups land below.

### 2.2. `GroupListItem` shape

```csharp
public sealed record GroupListItem(
    Guid Id,
    string Name,
    string? IconPublicId,
    int MemberCount,
    DateTimeOffset? LastCheckInAt);

public sealed record ListGroupsResponse(IReadOnlyList<GroupListItem> Groups);
```

- `IconPublicId`: nullable, same convention as `Category`.
  Reserved for later — groups don't have icons yet; the field
  ships `null` for now so the DTO is stable when they do.
- `MemberCount`: `COUNT(*)` on `group_memberships` per group.
  Cheap today (index on `group_id` exists); if it becomes a hot
  path later, materialize as a projection.
- `LastCheckInAt`: `MAX(performed_at)` of the caller's own
  check-ins in that group. Enables sorting / "stale group" hints
  in the UI. `null` when the caller has no check-ins there.

### 2.3. Shared types (mirror in `packages/shared/src/group/types.ts`)

```ts
export interface GroupListItem {
  id: string;
  name: string;
  iconPublicId: string | null;
  memberCount: number;
  lastCheckInAt: string | null;
}

export interface ListGroupsResponse {
  groups: GroupListItem[];
}
```

## 3. Mobile surfaces

### 3.1. `/groups` — picker

Replaces today's hard-coded `CheckinList` call. Renders:

- `PageHeader title="Grupos" description="Escolha um grupo pra ver o histórico"`.
- `GroupList` organism consuming `useGroups()`.
- Each `GroupCard` is a tap target → `router.push(\`/groups/\${group.id}\`)`.

### 3.2. `/groups/[id]` — per-group history

New dynamic route. Takes `id` from the URL and passes it to
`CheckinList` (already group-scoped via its `groupId` prop).

- `PageHeader` shows the group's name (fetched via `useGroups()`
  cache — reuse the picker's data, no second fetch). Fallback to
  "Grupo" while loading.
- Back-navigation leads to `/groups`.
- If `id` is not in the user's groups cache, show the empty
  "grupo não encontrado" state with a link back to `/groups`
  (defense against stale URLs, no secret-group leaks).

## 4. Component shape

### 4.1. `GroupCard` (atom)

```ts
interface GroupCardProps {
  group: GroupListItem;
  onPress: () => void;
}
```

- Left: avatar circle (initial letter of `name` until `iconPublicId`
  is populated).
- Center: name (semibold) + `${memberCount} membros` subdued.
- Right: relative `lastCheckInAt` (`há 2 dias`) or `—` when null.
- Entire card is a button (role=button, keyboard-focusable).

### 4.2. `GroupList` (organism)

```ts
interface GroupListProps {}
```

- Calls `useGroups()`.
- Renders `GroupListSkeleton` on first load.
- Renders `GroupCard`s on success.
- Empty state (defensive — shouldn't happen after signup since
  Global is auto-added, but belt + suspenders).
- Error state with inline retry (same pattern as `CheckinList`).

## 5. Data layer

### 5.1. `useGroups` hook

```ts
export function useGroups(): UseQueryResult<GroupListItem[]>;
```

- `queryKey`: `['groups', 'list']`.
- `queryFn: groupsService.list()`.
- Infinite stale time is OK — memberships change rarely; the
  `/groups` navigation triggers a refetch on focus which is
  enough.

### 5.2. Cache invalidation

Only two writes affect this cache today:

- **Join a new group** (phase 15+ feature, not here) — the mutation
  owner will invalidate `['groups', 'list']`.
- **Leave a group** (same).

Phase 29 doesn't ship any mutation.

## 6. States (per surface)

| Surface            | Loading                 | Empty                                    | Error            |
|--------------------|-------------------------|------------------------------------------|------------------|
| `/groups`          | `GroupListSkeleton`     | friendly copy + link to home (shouldn't happen) | inline retry |
| `/groups/[id]`     | spinner on PageHeader name; `CheckinList` has its own skeleton | reuses `CheckinList` empty state | reuses `CheckinList` error state |

## 7. Deferred / out of scope

- Group creation / editing / deletion → phase 15+ (challenges)
  owns multi-group lifecycle; Phase 29 only *reads* memberships.
- Join by invite / link / code → phase 15+.
- Group avatars (`iconPublicId` upload) → whenever UI design calls
  for it; DTO is forward-compatible.
- "People in the group" view (who's a member) → defer; Phase 29
  only exposes a count.
- Public group directory / discovery → not in roadmap today.
- Auto-navigate when the user has only 1 group → **no**. Picker
  shows even with 1 item for navigational consistency.

## 8. Acceptance

- `GET /groups` returns the caller's memberships ordered by
  `joined_at ASC`, with name / member count / last check-in at.
- `/groups` renders the picker (currently 1 card: Global).
- Tapping Global navigates to `/groups/{globalId}` and shows the
  check-in history that `/groups` previously served.
- Zero regression on the Phase 28 `CheckinList` behavior.
- Lint, format, unit and integration tests all green across
  `apps/api`, `apps/api.Tests`, `apps/mobile`, `packages/shared`.
