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

- [ ] **T-028-01 `[T][S]`** — Backend: `CheckInRepository` filters out pending rows
  - Depends on nothing.
  - Failing test
    (`apps/api.Tests/CheckIns/ListCheckInsEndpointTests.cs`):
    a row with `xp_earned = 0` is excluded from the listing; a
    row with `xp_earned > 0` is included; cursor pagination
    stays stable.
  - Extend `CheckInRepository.ListAsync` with
    `.Where(c => c.XpEarned > 0)`.
  - Green.

- [ ] **T-028-02 `[T][S]`** — Backend: enrich `CheckInResponse` with `category`
  - Depends on T-028-01.
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

- [ ] **T-028-03 `[T][S]`** — Backend: enrich `CheckInResponse` with `mediaPreview`
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

- [ ] **T-028-04 `[T][S]`** — Mobile: `useCheckInsList` infinite-query hook
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

- [ ] **T-028-05 `[T][S]`** — Mobile: `CheckinCard` atom
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

- [ ] **T-028-06 `[T][S]`** — Mobile: `CheckinList` organism
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

- [ ] **T-028-07 `[T][S]`** — Mobile: `/groups` page shows Global group's history
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

## Section C — Wrap-up

- [ ] **T-028-11 `[P]`** — ADR `docs/adr/0028-check-in-history.md`
  - Only if any decision in `plan.md` §3 is contested in review.
    Covers: backend enriches the list payload (shape rule),
    pending rows filtered server-side, no multi-group route yet,
    home has no list.

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
A1 ──▶ A2 ──▶ A3 ──▶ A4 ──▶ A5 ──▶ A6 ──▶ A7
                                           │
                                           ▼  (ship + test)
                                          B1 ──▶ B2 ──▶ B3
                                                        │
                                                        ▼
                                                       C1 (opt), C2
```

## Bundling strategy for PRs

1. `feat(check-ins): filter pending rows + enrich list response` — A1 + A2 + A3
2. `feat(mobile): check-in list hook + card + organism` — A4 + A5 + A6
3. `feat(mobile): groups page shows check-in history` — A7
4. `feat(users): expose last check-in timestamp on /me` — B2 (after B1 human sign-off)
5. `feat(mobile): home dashboard with check-in CTA` — B3
6. `docs(adr): check-in history surfaces` — C1 (optional)
