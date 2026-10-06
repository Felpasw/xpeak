# Phase 28 — Check-in History

> Status: **planning only**. Companion to `spec.md` and `tasks.md`.
> Stack: Next.js 16 + Capacitor + TanStack Query (mobile);
> ASP.NET Core 9 (minor backend enrichment).

## 1. Goal

Give the user the first real way to see their own check-ins
inside the app. The paginated `GET /check_ins` endpoint has been
shipped since Phase 5 but nothing on the client consumes it as a
list — this phase closes that gap and promotes the groups page
from `ComingSoon` to the primary history surface.

## 2. Scope

### 2.1. In scope

- `GET /check_ins` DTO enrichment: `category` snapshot +
  `mediaPreview` on each item, so the mobile renders
  property-direct without client-side joins.
- `useCheckInsList` infinite-query hook on the mobile.
- `CheckinCard` atom + `CheckinList` organism (with skeleton,
  empty, error states).
- `/groups` page rewrite: Global group's history, infinite scroll.
- `/home` page rewrite: welcome block + `Check-in agora` CTA +
  streak hint + `Ver histórico →` link. No list.
- Cache invalidation on `POST /check_ins` success extended to the
  new list query key.

### 2.2. Out of scope

- Cross-user group feed — belongs to Phase 13
  (`activity-timeline`).
- Multi-group selector / `/groups/[id]` dynamic route — belongs
  to Phase 15+ (challenges) when non-Global groups start existing
  in practice.
- Check-in detail view (expand, see all media, read notes) —
  Phase 26 (reactions/comments) owns that surface.
- Filters, search, date pickers over the list — later polish.
- Streak heatmap / metrics dashboard — Phase 21.
- Native pull-to-refresh gesture — Phase 25 (mobile polish).
- Backfill of the orphan check-in in dev DB (that `Acabei` row
  that stayed `xp_earned = 0`) — Phase 6 owns the pending→
  published lifecycle; Phase 28 just filters pending ones from
  the list response.

## 3. Decisions

### 3.1. Backend enriches the list payload

**Decided: yes.** Reasons:

- The project CLAUDE.md shape rule is explicit: "backend é dono do
  shape e da agregação. Frontend renderiza, não transforma."
- Alternative (joining `useCheckInsList` with `useCategories` on
  the client) requires two round-trips to be ready before the
  card can render — ergonomics regression and a `.map` reshape
  the rule forbids.
- Cost is low: the category table is tiny, denormalizing it into
  the response is a single JOIN in the repository.

### 3.2. Groups page = Global group listing (no dynamic route yet)

**Decided: yes.** Reasons:

- Only one group (`Global`) exists in production today. A
  dynamic route adds navigation plumbing we have no second
  destination for.
- When Phase 15 (solo challenges) or 17 (group challenges) land
  real additional groups, we introduce the picker + `[id]` route
  as part of that phase, not retroactively here.

### 3.3. Home has no list

**Decided: yes.** See spec §3.2 — pushing a mini-feed on home
duplicates work that Phase 13 will properly deliver as a
poliforme event feed. Home stays action-oriented.

### 3.4. Pending check-ins are filtered server-side

**Decided: yes.** The current query returns everything, including
rows left pending by Phase 6's two-step media flow. These have
`xp_earned = 0` and look broken in the list. The listing filter
is `xp_earned > 0`, applied in `CheckInRepository.ListAsync`.

- This is additive to Phase 6 — the repository filter does not
  change the pending row's lifecycle, only hides it from the
  "finished history" surface.
- A dedicated "pending uploads" view, if ever needed, is a
  Phase 6 polish task.

### 3.5. Media preview URL comes from Cloudinary transformation

**Decided: yes.** The thumbnail URL is derived from the stored
`storage_key` using Cloudinary's URL-based transformations
(`c_fill,w_240,h_240,q_auto`). No new storage, no new round-trip.
The derivation lives in the existing `IMediaStorage` abstraction
so the fake provider can stub it in tests.

## 4. Open questions

- **Date formatting locale.** Project default is `en-US` with
  `pt-BR` on the roadmap. Spec says `formatPerformedAt` but
  doesn't fix the locale. **Working assumption:** `date-fns`'s
  `formatDistanceToNow` with locale picked from the user's
  `me.timeZone` (or fallback to `enUS`). Confirm before
  implementing the card.
- **"Checked in today" derivation on the home.** The spec needs
  `me.lastCheckInAt` to decide whether to show the streak hint.
  `useMe` currently returns `currentStreakDays` but not
  `lastCheckInAt`. Options:
  - (a) extend `UserResponse` to carry `lastCheckInAt` (small
    backend touch, cheap);
  - (b) look at the first item of `useCheckInsList({ groupId:
    undefined })` on the home (zero backend touch but adds a
    second query to the home);
  - (c) skip the hint for Phase 28 and bring it back with Phase
    24 push-notifications.
  **Recommendation:** (a). Clean, cheap, keeps home with one
  query. Flag at kickoff.
- **Member count on the groups page strip.** No endpoint today
  returns it cheaply. **Recommendation:** omit for Phase 28,
  revisit in Phase 15.

## 5. UX notes

- The card is dense but calm — the only "loud" element is the XP
  pill on the right, which mirrors the gamified language used on
  the profile.
- Skeleton cards use the same `rounded-2xl border border-zinc-800
  bg-zinc-900/40` shell as the real card so the layout doesn't
  jump when data arrives.
- Infinite scroll sentinel is a `div` with `IntersectionObserver`;
  no library beyond what's already in the project.
- Empty state copy: `Ainda sem check-ins neste grupo. Bora
  treinar?` with a button linking to `/checkin`.
- Error state copy: `Falhou carregar o histórico. Tenta de novo.`
  + an inline button that calls `refetch()`.

## 6. Risks

- **DTO enrichment breaks Phase 5 contract tests.** Mitigation:
  the new fields are additive; existing assertions keep passing.
  Spot-check `apps/api.Tests/CheckIns/ListCheckInsEndpointTests.cs`
  before touching the DTO.
- **Infinite scroll loop misfires on fast scrolls.** Mitigation:
  guard `fetchNextPage` with `!isFetchingNextPage && hasNextPage`.
- **Media preview URL leaks beyond the owner.** Cloudinary URLs
  are signed per `apps/api/src/Media/Providers/Cloudinary/
  CloudinaryMediaStorage.cs`; reuse the same signing path for
  thumbnails. Do NOT roll a new URL builder.

## 7. Delivery shape

Suggested bundling (match `tasks.md` sections):

1. `feat(check-ins): enrich list response with category + media preview` (A1–A3)
2. `feat(mobile): check-in list hook + card + organism` (A4–A6)
3. `feat(mobile): groups page shows check-in history` (A7)
4. `feat(mobile): home dashboard with check-in CTA` (B1–B3)
5. `docs(adr): check-in history surfaces` (C1) — only if a
   decision in §3 is contested in review.
