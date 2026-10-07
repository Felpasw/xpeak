# Phase 29 — Groups Listing

> Status: **planning only**. Companion to `spec.md` and `tasks.md`.
> Stack: Next.js 16 + Capacitor + TanStack Query (mobile);
> ASP.NET Core 9 (minor backend addition).

## 1. Goal

Reverse the "no picker" shortcut taken in Phase 28 (plan §3.2):
split `/groups` into a picker + a per-group detail route, so the
navigation stops hard-coding `GLOBAL_GROUP_ID` and the UI is
ready to carry additional groups the second Phase 15+ ships them.

## 2. Scope

### 2.1. In scope

- `GET /groups` endpoint returning the caller's memberships.
- `GroupListItem` DTO (id, name, iconPublicId, memberCount,
  lastCheckInAt).
- Shared types mirror in `packages/shared/src/group/types.ts`.
- `useGroups` hook + `groupsService.list()`.
- `GroupCard` atom + `GroupList` organism + `GroupListSkeleton`.
- `/groups` page becomes the picker.
- New `/groups/[id]` dynamic route hosts the per-group
  `CheckinList`.
- `PageHeader` on `/groups/[id]` reads the group name from the
  `useGroups` cache (no second network call).

### 2.2. Out of scope

- Group creation / editing / deletion → phase 15+.
- Invite flow (code / link / QR) → phase 15+.
- Member listing ("who's in") → defer; only count exposed here.
- Group avatar upload → forward-compat only (DTO field exists,
  no UI).
- Public group directory → not on the roadmap.

## 3. Decisions

### 3.1. Picker always renders, even with a single group

**Decided: yes.** Reasons:

- Consistency wins. Auto-navigating "when there's one group"
  creates a UX cliff the day a user joins a second one — the
  previous behavior ("tap-free") goes away unannounced.
- The picker is also the natural home for "join a group" and
  group settings when those arrive.

### 3.2. Response carries `memberCount` and `lastCheckInAt`

**Decided: yes.** Cheap today (single COUNT and MAX per group
via grouped SQL), and both unlock immediate UX wins (count badge
on the card, "stale" sorting potential). If either becomes a hot
path, materialize as a projection then — not now.

### 3.3. No pagination on `GET /groups`

**Decided: yes.** Realistic cap for v1 is single-digit groups per
user. Add cursor pagination the moment that assumption breaks;
not before.

### 3.4. `/groups/[id]` reads the group name from the picker cache

**Decided: yes.** The picker already lists every group the user
belongs to, so looking up `name` from the cached `useGroups`
array avoids a dedicated `GET /groups/{id}` fetch for a single
string. If the user lands directly on `/groups/[id]` via a push
notification or deep link (no cache yet), the hook falls back to
fetching the full list — same cost, keeps the surface simple.

### 3.5. Backward-compat redirect at `/groups` → NO

**Decided: no automatic redirect.** The previous `/groups`
behavior (direct history of Global) is replaced by the picker.
Users with that URL in muscle memory land on the picker and tap
once — acceptable one-time cost; no silent redirect that would
make the new picker invisible to them.

## 4. Open questions

- **Sort order on the picker.** Spec says `joined_at ASC` so
  Global stays on top. Alternative: `lastCheckInAt DESC NULLS
  LAST` so the "most active" surfaces first. **Working
  assumption:** `joined_at ASC` for Phase 29; revisit when
  Phase 15+ introduces real multi-group content and the user has
  a reason to prefer "most recent".
- **Avatar fallback.** Currently plan is "initial letter" when
  `iconPublicId` is null. Confirm design intent before shipping —
  if the team wants a lucide icon instead (e.g. `Users`), swap
  easily.

## 5. UX notes

- The picker card is wider / taller than a check-in card — groups
  are navigational, not content; they deserve to feel tappable.
- `GroupListSkeleton` ghosts 2 cards (one is the expected
  realistic count today).
- Empty state should never fire for a real user (Global is seeded
  on signup); if it does, the copy points to the home screen and
  logs a telemetry warning so we know something is off.
- Error state uses the same inline retry pattern as `CheckinList`
  for visual coherence.

## 6. Risks

- **Dynamic route under Next.js App Router + Capacitor.** Dynamic
  segments work the same under static export / Capacitor as under
  server rendering as long as we pre-render the shell and lazy-
  load the page body. Verify `next build` with `output: 'export'`
  (or whatever the project uses) doesn't choke on `[id]`.
- **Membership source stale.** If a user is removed from a group
  server-side (future admin flow), the picker shows stale data
  until the next focus-triggered refetch. Acceptable for Phase 29;
  revisit when admin lifecycle lands.
- **Phase 28 decision reversal in docs.** Spec 028 §7 Deferred
  explicitly ruled out the picker; Phase 29 reverses that. Keep
  the inconsistency honest by linking back from 028 to 029 (small
  note update, not a rewrite).

## 7. Delivery shape

Suggested bundling (match `tasks.md` sections):

1. `feat(groups): GET /groups returns the caller's memberships` — A1 + A2
2. `feat(mobile): add groups service and useGroups hook` — B1 + B2
3. `feat(mobile): add GroupCard atom and GroupList organism` — C1 + C2 + C3
4. `feat(mobile): split /groups into picker + /groups/[id] detail` — D1 + D2
5. `docs(adr): groups listing picker` — E1 (optional, only if a
   decision in §3 is contested in review).
