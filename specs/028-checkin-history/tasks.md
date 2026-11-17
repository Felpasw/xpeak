# Phase 28 — Check-in History (TASKS)

> Stack: Next.js 16 + Capacitor + TanStack Query + Vitest (mobile);
> ASP.NET Core 9 + xUnit (api).

## Legend

- `[T]` TDD (failing test first, show red, then implement).
- `[S]` sequential (must run in listed order).
- `[P]` parallel (safe to run in any order or in parallel with peers).
- `[HUMAN]` human-only step.
- `[ ]` not started, `[~]` in progress, `[x] ✅ commit <hash>` done.

---

## Section A — Groups surface (priority: testable end-to-end first)

- [~] **T-028-01 `[CANCELLED]`** — Backend: filter pending rows from the listing
  - Original plan: add `.Where(c => c.XpEarned > 0)` to
    `CheckInRepository.ListAsync` to hide pending check-ins from
    the history.
  - **Cancelled** after review — the root cause is a missing
    rollback when the mobile upload fails (the row is persisted
    by `POST /check_ins { withMedia: true }` and never cleaned up
    on the `catch` branch of `useCheckinForm`, see
    `apps/mobile/src/hooks/useCheckinForm.ts:95-116`). Fixing the
    symptom at the listing hides a real inconsistency instead of
    repairing it.
  - Moved to **T-006-18** (checkin-media phase): rollback the
    pending check-in on upload failure. T-028-02 onwards no
    longer depend on this.

- [x] **T-028-02 `[T][S]`** ✅ commit `d899c06` — Backend: enrich `CheckInResponse` with `category`
  - Depends on nothing (previous dependency on T-028-01 removed).
  - Failing test
    (`apps/api.Tests/CheckIns/ListCheckInsEndpointTests.cs`): the
    response for a check-in with a known category exposes
    `category.slug`, `category.name`, `category.iconPublicId`;
    serialization uses camelCase.
  - Add `CategorySnapshot` record in
    `apps/api/src/CheckIns/Dto/CheckInResponse.cs`; extend
    `CheckInResponse` with a non-nullable `Category` property.
  - Update `CheckInRepository.ListAsync` to project the category
    into the row (JOIN, no N+1).
  - Update `CheckInResponse.From(...)` and all call sites (create
    endpoint too — same DTO).
  - Mirror the shape in
    `packages/shared/src/checkin/types.ts` (`CategorySnapshot` +
    `CheckIn.category`).
  - Green.

- [x] **T-028-03 `[T][S]`** ✅ commit `d899c06` — Backend: enrich `CheckInResponse` with `mediaPreview`
  - Depends on T-028-02.
  - Failing test
    (`apps/api.Tests/CheckIns/ListCheckInsEndpointTests.cs`): a
    check-in with media returns a `mediaPreview` carrying
    `kind` and `thumbUrl`; without media the field is `null`.
  - Add `MediaPreview` record + `IMediaStorage.BuildThumbUrl(
    storageKey, kind)` method (default `c_fill,w_240,h_240,
    q_auto` for photos, `so_auto` poster for videos). Implement
    for both `CloudinaryMediaStorage` and `FakeMediaStorage`
    (fake returns the storage key suffixed with `?thumb=1`).
  - Repository already loads media (Phase 6) — reuse the first
    attachment ordered by `position`.
  - Mirror in `packages/shared/src/checkin/types.ts`.
  - Green.

- [x] **T-028-04 `[T][S]`** ✅ commit `72e6c40` — Mobile: `useCheckInsList` infinite-query hook
  - Depends on T-028-03.
  - Failing spec
    (`apps/mobile/test/hooks/useCheckInsList.spec.ts` with MSW):
    first page loads, `fetchNextPage` requests with `cursor`,
    stops when `nextCursor === null`.
  - Add `useCheckInsList` in
    `apps/mobile/src/hooks/useCheckIn.ts` using
    `useInfiniteQuery`; reuse `CHECK_IN_QUERY_KEYS.list(groupId)`
    which already exists.
  - Extend `useCreateCheckIn.onSuccess` to also invalidate
    `['checkins', 'list']` (all variants).
  - Green.

- [x] **T-028-05 `[T][S]`** ✅ commit `72e6c40` — Mobile: `CheckinCard` atom
  - Depends on T-028-04.
  - Failing spec
    (`apps/mobile/test/components/atoms/CheckinCard.spec.tsx`):
    renders title, category name, performedAt (formatted), XP
    pill; shows `MediaThumb` when `mediaPreview` is set; falls
    back to category-icon placeholder when `null`.
  - Add `apps/mobile/src/components/atoms/CheckinCard.tsx`;
    style mirrors the existing `rounded-2xl border border-zinc-800
    bg-zinc-900/40` surface used in `/home`.
  - Green.

- [x] **T-028-06 `[T][S]`** ✅ commit `72e6c40` — Mobile: `CheckinList` organism
  - Depends on T-028-05.
  - Failing spec
    (`apps/mobile/test/components/organisms/CheckinList.spec.tsx`):
    renders skeleton on first load; renders list of
    `CheckinCard` on success; empty state when `checkIns.length
    === 0`; error state renders inline retry that calls
    `refetch`; sentinel triggers `fetchNextPage` when visible.
  - Add
    `apps/mobile/src/components/organisms/CheckinList.tsx` +
    `apps/mobile/src/components/atoms/CheckinListSkeleton.tsx`.
  - Use `IntersectionObserver` for the sentinel; guard
    `!isFetchingNextPage && hasNextPage` before firing.
  - Green.

- [x] **T-028-07 `[T][S]`** ✅ commit `968e655` — Mobile: `/groups` page shows Global group's history
  - Depends on T-028-06.
  - Failing spec
    (`apps/mobile/test/app/(app)/groups/page.spec.tsx`): renders
    `PageHeader`, renders `CheckinList` with
    `groupId={GLOBAL_GROUP_ID}`; does NOT render `ComingSoon`.
  - Rewrite `apps/mobile/src/app/(app)/groups/page.tsx`.
  - Import `GLOBAL_GROUP_ID` from `@xpeak/shared/checkin/types`.
  - Green.

## Section B — Home surface (do after Section A ships and testing confirms the list)

- [ ] **T-028-08 `[HUMAN]`** — Confirm open question in `plan.md` §4
  - Decide between (a) extend `UserResponse` with
    `lastCheckInAt`, (b) query first check-in on the home,
    (c) defer the streak hint. Default: (a).
  - Also confirm date formatting locale (default
    `date-fns/formatDistanceToNow` with `enUS`).

- [ ] **T-028-09 `[T][S]`** — Backend: `UserResponse` carries `lastCheckInAt`
  - Depends on T-028-08 choosing (a).
  - Failing test
    (`apps/api.Tests/Users/MeEndpointTests.cs` or equivalent):
    `GET /me` returns `lastCheckInAt` as the max of the user's
    `check_ins.performed_at`, `null` if none.
  - Extend `UserResponse` + the projection in the users service.
  - Mirror in `packages/shared/src/user/types.ts`.
  - Green.

- [ ] **T-028-10 `[T][S]`** — Mobile: Home CTA + streak hint
  - Depends on T-028-09.
  - Failing spec
    (`apps/mobile/test/app/(app)/home/page.spec.tsx`): renders
    welcome block (existing), renders `Check-in agora` CTA that
    navigates to `/checkin`, shows streak hint when streak > 0
    and no check-in today, shows "comece um streak" when streak
    === 0, hides the hint when checked in today; renders
    `Ver histórico →` link to `/groups`.
  - Rewrite `apps/mobile/src/app/(app)/home/page.tsx` — remove
    the `ComingSoon`-style placeholder.
  - Reuse `AnimatedBorderButton` for the primary CTA.
  - Green.

- [ ] **T-028-15 `[T][S]`** — Backend + mobile: home surfaces active challenges with progress
  - Backend: new endpoint `GET /challenges/summary` returning the
    caller's active memberships with per-challenge progress inline,
    isolated from the existing `GET /challenges` listing so
    unrelated consumers do not have to adapt.
  - New DTO `ChallengeSummaryResponse(Id, Title, State, Progress)`
    where `Progress` is `MembershipProgressResponse`.
  - Failing test
    (`apps/api.Tests/Challenges/MyChallengesSummaryEndpointTests.cs`):
    401 without a token; 200 returning only the caller's active
    memberships; each row exposes `progress.value`, `progress.target`.
  - Mirror in `packages/shared/src/challenges/types.ts`
    (`ChallengeSummary` + `ListChallengesSummaryResponse`).
  - Mobile: `ChallengesService.summary()` + `useMyChallengesSummary`
    hook following the singleton/class pattern. New
    `MyChallengesSummary` organism rendering each row with title,
    state badge, progress bar (reuses `lib/progress`) and a link to
    the challenge detail. Empty, error and skeleton states.
  - Plug between the CTA and `CheckinList` on the home. Hide the
    section entirely when the user has zero active challenges to
    avoid shouting at a new user.
  - Green.

- [ ] **T-028-14 `[T][S]`** — Backend + mobile: home shows global level progress hero
  - Backend: extend `UserResponse` (and `AppUser.ToResponse`) with a
    `LevelProgress(XpIntoLevel, XpToNextLevel, Percent)` block
    computed from `LevelCurve.XpForLevel(level)` and
    `LevelCurve.XpForLevel(level + 1)`. At max level the block stays
    valid with `XpToNextLevel = 0` and `Percent = 1`.
  - Failing test
    (`apps/api.Tests/Auth/Core/MeEndpointTests.cs` or equivalent):
    `GET /me` with a user at `level=3, xp=1250` returns
    `levelProgress.xpIntoLevel`, `xpToNextLevel` and `percent`
    matching the curve.
  - Mirror in `packages/shared/src/user/types.ts` (`LevelProgress`
    interface + `User.levelProgress` field).
  - Mobile: new atom `LevelProgressHero` rendering level, username,
    animated progress bar and a `🔥 Nd` streak chip. Spec covers
    percent-width, "faltam X XP" copy and chip visibility.
  - Rewrite `apps/mobile/src/app/(app)/home/page.tsx` welcome block
    to use the hero; keep the header + CTA + list intact.
  - Green.

- [ ] **T-028-13 `[T][S]`** — Mobile: home renders the check-in list inline
  - Product direction override of T-028-10's "Ver histórico →" link:
    the home itself is the primary surface for the check-in feed.
    Groups route no longer exists (dropped in Phase 30), so there
    is no secondary surface to link to.
  - Failing spec
    (`apps/mobile/test/app/(app)/home/page.spec.tsx`): renders the
    existing welcome block, renders a `Check-in agora` CTA linking
    to `/checkin`, and renders `<CheckinList />` below the CTA.
  - Rewrite `apps/mobile/src/app/(app)/home/page.tsx` — drop the
    "Your workout log lives here soon" placeholder, keep the
    header/profile pill and the welcome section.
  - Streak hint and `lastCheckInAt` (T-028-08/09) stay deferred —
    scope is the list + CTA only.
  - Green.

## Section C — Wrap-up

- [ ] **T-028-11 `[P]`** — ADR `docs/adr/0028-check-in-history.md`
  - Only if any decision in `plan.md` §3 is contested in review.
    Covers: backend enriches the list payload (shape rule),
    no multi-group route yet, home has no list.

- [ ] **T-028-12 `[S]`** — Phase close
  - `dotnet test apps/api.Tests` green.
  - `pnpm --filter @xpeak/mobile test` green.
  - `pnpm --filter @xpeak/shared build` green.
  - `dotnet format --verify-no-changes` clean.
  - `pnpm --filter @xpeak/mobile lint` clean.
  - CI green on the PR.

---

## Dependencies

```
A2 ──▶ A3 ──▶ A4 ──▶ A5 ──▶ A6 ──▶ A7
                                    │
                                    ▼  (ship + test)
                                   B1 ──▶ B2 ──▶ B3
                                                 │
                                                 ▼
                                                C1 (opt), C2

(A1 cancelled — moved to T-006-18 in checkin-media phase)
```

## Bundling strategy for PRs

1. `feat(check-ins): enrich list response with category and media preview` — A2 + A3
2. `feat(mobile): check-in list hook + card + organism` — A4 + A5 + A6
3. `feat(mobile): groups page shows check-in history` — A7
4. `feat(users): expose last check-in timestamp on /me` — B2 (after B1 human sign-off)
5. `feat(mobile): home dashboard with check-in CTA` — B3
6. `docs(adr): check-in history surfaces` — C1 (optional)
