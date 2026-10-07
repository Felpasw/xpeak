# Phase 28 — Check-in History (SPEC)

> Stack: Next.js 16 (App Router) + Capacitor + TanStack Query + Vitest.
> Backend: ASP.NET Core 9 (minor DTO enrichment on `GET /check_ins`).

## 1. Summary

Ship the first user-facing listing of check-ins. The paginated
`GET /check_ins` endpoint already exists (shipped in Phase 5,
`apps/api/src/CheckIns/Endpoints/ListCheckInsEndpoint.cs`), scoped
to the caller and optionally filtered by `group_id`. This phase
plugs that endpoint into the mobile UI in two surfaces:

- **Groups page** (`/groups`) — rendered as the Global group's
  history by default; the primary surface for testing end-to-end.
- **Home page** (`/home`) — a compact dashboard CTA; no listing,
  but it stops being a `ComingSoon` placeholder.

Scope stays caller-only (my check-ins). A cross-user group feed
belongs to Phase 13 (`activity-timeline`) and is explicitly out.

## 2. Backend contract

### 2.1. Existing endpoint

```
GET /check_ins?limit=&cursor=&group_id=
→ 200 { checkIns: CheckInResponse[], nextCursor: string | null }
```

- `limit` defaults to 20, capped at 100.
- `cursor` is opaque, round-trips.
- `group_id` is optional; when omitted the response includes rows
  from every group the caller is a member of.
- Caller-scoped — the endpoint never returns another user's rows.

### 2.2. DTO enrichment (new in this phase)

`CheckInResponse` grows two fields so the mobile card renders
property-direct, no client-side joins:

```csharp
public sealed record CheckInResponse(
    Guid Id,
    Guid CategoryId,
    Guid GroupId,
    string Title,
    int XpEarned,
    ScoringSnapshot ScoringSnapshot,
    DateTimeOffset PerformedAt,
    int? DurationMinutes,
    string? Notes,
    bool HasMedia,
    // ↓ new
    CategorySnapshot Category,
    MediaPreview? MediaPreview);

public sealed record CategorySnapshot(string Slug, string Name, string? IconPublicId);
public sealed record MediaPreview(string Kind, string ThumbUrl);
```

- `Category` is a denormalized snapshot (not a FK fetch) — the
  category rows are tiny and already live in-process; joining in
  the repository keeps the shape stable even if a category is
  renamed later. Rationale is the shape rule in the global
  CLAUDE.md: "backend define, frontend consome property-direct".
- `MediaPreview` is `null` when `HasMedia == false`. When present,
  it carries the first attachment's `kind` (`photo`/`video`) and a
  Cloudinary-transformed thumbnail URL (`c_fill,w_240,h_240,q_auto`
  for photos; `so_auto` poster frame for videos).

The `CheckIn` TypeScript interface in
`packages/shared/src/checkin/types.ts` grows the same two fields.

### 2.3. What is NOT changing

- Query shape, auth, cursor format, limits.
- Any Phase 5 behaviour — this is purely additive on the response.

## 3. Mobile surfaces

### 3.1. Groups page — `/groups` (priority surface)

The page stops being a `ComingSoon` placeholder. For Phase 28 it
renders the Global group's history directly (there's only one
group today). When Phase 15+ adds real multi-group routing, this
screen gets a group picker on top and the detail screen moves to
`/groups/[id]`.

**Layout, top → bottom:**

- `PageHeader` with `title="Grupos"` and
  `description="Histórico de check-ins do grupo"`.
- Group identity strip: group name, member count (if cheap to
  fetch — may be deferred), optional group avatar.
- `CheckinList` (see §4) consuming
  `useCheckInsList({ groupId: GLOBAL_GROUP_ID, limit: 20 })`.

**Interaction:**

- Infinite scroll via `useInfiniteQuery`; `nextCursor` drives
  `getNextPageParam`.
- Pull-to-refresh invalidates the first page.
- Tapping a card is a no-op in Phase 28 (detail view is Phase 26
  reactions/comments territory).

### 3.2. Home page — `/home`

The home replaces the current "coming soon" slab with a dashboard
CTA. No list.

**Layout:**

- Existing header (wordmark + profile shortcut).
- Welcome block (`@username`, `Level X · YY XP · ZZd streak`) —
  keep.
- **New** primary CTA: `Check-in agora` → navigates to `/checkin`.
  Full-width, animated border (reuse `AnimatedBorderButton`).
- **New** streak hint: if `me.currentStreakDays > 0` and the user
  has NOT checked in today (derive from
  `me.lastCheckInAt`/local), show `Teu streak de Nd tá em jogo
  hoje` in a subdued tone. If `me.currentStreakDays === 0`, show
  `Começa um streak hoje`. If already checked in today, hide the
  line entirely.
- Secondary link: `Ver histórico →` → navigates to `/groups` (the
  listing surface). Deliberately low-key so users understand the
  listing lives there.

**No listing, no cards.** Resisting the urge to put a mini-feed
here because:

- Phase 13 (activity-timeline) is the real feed; a bespoke home
  feed now gets thrown away.
- Home should push action, not browse history.

## 4. Component shape

### 4.1. `CheckinCard` (atom)

Pure, no data fetching. Props mirror the enriched `CheckIn` DTO
one-to-one:

```ts
interface CheckinCardProps {
  checkIn: CheckIn; // the enriched shared type
}
```

**Visual anatomy (top-left to bottom-right):**

- Left: `MediaThumb` (reuses the existing atom) when
  `mediaPreview` is set; falls back to a category-icon placeholder.
- Center column:
  - Line 1: `title` (semibold, truncates at 1 line).
  - Line 2: `category.name` · `formatPerformedAt(performedAt)`.
  - Line 3 (optional): `notes` truncated to 1 line, muted.
- Right: XP pill (`+${xpEarned} XP`, sky accent, mono font).

Styling follows the existing `zinc-900/60` surface + `zinc-800`
border pattern used in `/home`.

### 4.2. `CheckinList` (organism)

```ts
interface CheckinListProps {
  groupId?: string;        // undefined = cross-group
  initialLimit?: number;   // default 20
}
```

Responsibilities:

- Calls `useCheckInsList({ groupId, limit })`.
- Renders list of `CheckinCard`.
- Shows `CheckinListSkeleton` (3 ghost cards) on first load.
- Empty state: friendly copy + CTA to `/checkin` (reuse icon from
  `lucide-react`).
- Error state: inline retry button, no toast.
- Infinite scroll sentinel at the bottom.

## 5. Data layer

### 5.1. `useCheckInsList` hook (new)

```ts
export function useCheckInsList(
  params: { groupId?: string; limit?: number } = {},
): UseInfiniteQueryResult<ListCheckInsResponse>;
```

- `queryKey`: `['checkins', 'list', params.groupId ?? 'all']` — the
  key already exists in `CHECK_IN_QUERY_KEYS.list(...)`.
- `queryFn({ pageParam }): checkInService.list({ ...params, cursor:
  pageParam })`.
- `getNextPageParam: last => last.nextCursor ?? undefined`.

### 5.2. Cache invalidation on write

Already wired in `useCreateCheckIn.onSuccess`, which currently
only invalidates `useMe`. Extend it to also invalidate
`['checkins', 'list']` (all variants). Confirm case not already
hitting a stale paginated cache.

## 6. States (per surface)

| Surface   | Loading           | Empty                              | Error             |
|-----------|-------------------|------------------------------------|-------------------|
| `/groups` | `CheckinListSkeleton` | `Nenhum check-in ainda · Fazer o primeiro` | retry inline |
| `/home`   | header + stats only (no skeleton for the list, since there's no list) | n/a | n/a — stats come from `useMe` which already handles its own error |

## 7. Deferred / out of scope

- Cross-user group feed → Phase 13 (`activity-timeline`).
- Date-range / category filters, search box → later polish.
- Multi-group selector + `/groups/[id]` dynamic route → arrives
  with Phase 15+ (challenges) when non-Global groups start
  existing in practice.
- Check-in detail view (expand card, see all media, read full
  notes) → Phase 26 (reactions/comments) owns that surface.
- Pending-media state in the list (orphan check-ins like the one
  in dev DB today) → fixed at the source in Phase 6 (T-006-18):
  rollback the pending row via `DELETE /check_ins/{id}` when the
  mobile upload fails. No filter on the listing — if a row makes
  it to `check_ins`, it is considered published and shown as-is.
- Streak heatmap / calendar visualization → Phase 21 (metrics).
- Pull-to-refresh gesture inside Capacitor native container →
  reuse Phase 25 (mobile polish) scope; Phase 28 ships only the
  web-friendly invalidate-on-focus fallback.

## 8. Acceptance

- `GET /check_ins` response carries `category` + `mediaPreview`
  without regressing the Phase 5 integration tests.
- `/groups` renders the Global group's check-ins, newest first,
  infinite-scrollable, in ≤ 3s on 4G (same NFR as the feed in
  spec §5 of the root `README.md`).
- `/home` opens with the welcome block + primary CTA; no
  `ComingSoon`.
- Creating a check-in refreshes the listing without a manual
  reload.
- Lint, format, unit tests, integration tests all green across
  `apps/api`, `apps/api.Tests`, `apps/mobile`, `packages/shared`.
