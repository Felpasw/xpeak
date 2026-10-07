# Phase 29 — Groups Listing (TASKS)

> Stack: Next.js 16 + Capacitor + TanStack Query + Vitest (mobile);
> ASP.NET Core 9 + xUnit (api).

## Legend

- `[T]` TDD (failing test first, show red, then implement).
- `[S]` sequential (must run in listed order).
- `[P]` parallel (safe to run in any order or in parallel with peers).
- `[HUMAN]` human-only step.
- `[ ]` not started, `[~]` in progress, `[x] ✅ commit <hash>` done.

---

## Section A — Backend

- [ ] **T-029-01 `[T][S]`** — `GET /groups` endpoint returns caller's memberships
  - Depends on nothing.
  - Failing test
    (`apps/api.Tests/Groups/ListGroupsEndpointTests.cs`):
    401 without token; 200 with token returns only the caller's
    groups ordered by `joined_at ASC`; another user's groups do
    not leak into the response.
  - Add `apps/api/src/Groups/Dto/GroupListItem.cs` +
    `ListGroupsResponse` (fields per spec §2.2).
  - Add `apps/api/src/Groups/Endpoints/ListGroupsEndpoint.cs`.
  - Repository / service method on `IGroupService`:
    `ListForUserAsync(userId, ct)` — single grouped SQL projecting
    the group + COUNT(group_memberships) + MAX(check_ins.performed_at)
    scoped to the caller.
  - Green.

- [ ] **T-029-02 `[T][S]`** — Shared types mirror
  - Depends on T-029-01.
  - Add `packages/shared/src/group/types.ts` with `GroupListItem`
    and `ListGroupsResponse`.
  - Re-export from `packages/shared/src/index.ts`.
  - Update `apps/mobile/test` fixtures that reference the group
    shape, if any.

## Section B — Mobile data layer

- [ ] **T-029-03 `[T][S]`** — `groupsService` + `useGroups` hook
  - Depends on T-029-02.
  - Failing spec
    (`apps/mobile/test/hooks/useGroups.spec.tsx`): fetches on
    mount, exposes `data` with the expected shape, surfaces
    `isError` on 500.
  - Add `apps/mobile/src/services/groups.service.ts` +
    `apps/mobile/src/services/interfaces/groups.interface.ts`.
  - Add `apps/mobile/src/hooks/useGroups.ts` with
    `GROUPS_QUERY_KEYS = { list: ['groups', 'list'] as const }`.
  - Green.

## Section C — Mobile presentation

- [ ] **T-029-04 `[T][S]`** — `GroupCard` atom
  - Depends on T-029-03.
  - Failing spec
    (`apps/mobile/test/components/atoms/GroupCard.spec.tsx`):
    renders name, `${memberCount} membros`, relative
    `lastCheckInAt` (or `—` when null); calls `onPress` when
    clicked.
  - Add `apps/mobile/src/components/atoms/GroupCard.tsx`.
  - Same visual language as `CheckinCard` (rounded-2xl border +
    zinc-900/40 background).
  - Green.

- [ ] **T-029-05 `[T][S]`** — `GroupListSkeleton` atom
  - Depends on T-029-04.
  - 2 ghost cards following `CheckinListSkeleton` pattern.
  - Add `apps/mobile/src/components/atoms/GroupListSkeleton.tsx`.
  - Covered by `GroupList` spec; no dedicated test file.

- [ ] **T-029-06 `[T][S]`** — `GroupList` organism
  - Depends on T-029-05.
  - Failing spec
    (`apps/mobile/test/components/organisms/GroupList.spec.tsx`):
    skeleton on first load; `GroupCard`s on success; empty state;
    inline retry on error; tapping a card calls
    `router.push('/groups/{id}')`.
  - Add `apps/mobile/src/components/organisms/GroupList.tsx`.
  - Mock `useRouter` from `next/navigation` in tests.
  - Green.

## Section D — Mobile routes

- [ ] **T-029-07 `[T][S]`** — `/groups` becomes the picker
  - Depends on T-029-06.
  - Failing spec
    (`apps/mobile/test/app/(app)/groups/page.spec.tsx`): renders
    `GroupList`; does NOT render the `CheckinList` directly;
    header reads `Grupos`.
  - Rewrite `apps/mobile/src/app/(app)/groups/page.tsx` to render
    `GroupList`.
  - Update the existing Phase 28 spec in
    `apps/mobile/test/app/(app)/groups/page.spec.tsx` — the
    assertion about `CheckinList` on `/groups` moves to the
    detail page spec in T-029-08.
  - Green.

- [ ] **T-029-08 `[T][S]`** — `/groups/[id]` detail route
  - Depends on T-029-07.
  - Failing spec
    (`apps/mobile/test/app/(app)/groups/[id]/page.spec.tsx`):
    reads `id` from params, renders `PageHeader` with the group
    name (looked up in the `useGroups` cache; `"Grupo"` while the
    cache is empty), renders `CheckinList` scoped to that `id`;
    shows the "grupo não encontrado" fallback when `id` is not in
    the cached list.
  - Add `apps/mobile/src/app/(app)/groups/[id]/page.tsx`.
  - Green.

## Section E — Wrap-up

- [ ] **T-029-09 `[P]`** — Update `specs/028-checkin-history`
  - Add a short note in `spec.md` §7 and `plan.md` §3.2 pointing
    to Phase 29 as the owner of the picker + `[id]` route (the
    original decision to defer is now reversed).
  - Doc-only; no code change.

- [ ] **T-029-10 `[P]`** — ADR `docs/adr/0029-groups-listing.md`
  - Only if a decision in `plan.md` §3 is contested in review.
    Covers: picker always renders (no auto-nav), `memberCount` +
    `lastCheckInAt` on the response, no pagination, `/groups/[id]`
    reads name from the picker cache.

- [ ] **T-029-11 `[S]`** — Phase close
  - `dotnet test apps/api.Tests` green.
  - `pnpm --filter @xpeak/mobile test` green.
  - `pnpm --filter @xpeak/shared build` green (if a build script
    exists by then; today the package is source-only).
  - `dotnet format --verify-no-changes` clean on both projects.
  - `pnpm --filter @xpeak/mobile lint` clean.
  - CI green on the PR.

---

## Dependencies

```
A1 ──▶ A2 ──▶ B1 ──▶ C1 ──▶ C2 ──▶ C3 ──▶ D1 ──▶ D2
                                                  │
                                                  ▼
                                                 E1 (doc patch), E2 (opt), E3
```

## Bundling strategy for PRs

1. `feat(groups): GET /groups returns the caller's memberships` — A1 + A2
2. `feat(mobile): groups service + useGroups hook` — B1
3. `feat(mobile): group card + list organism` — C1 + C2 + C3
4. `feat(mobile): split /groups into picker + /groups/[id] detail` — D1 + D2
5. `docs(specs): link phase 28 picker-deferred decision to phase 29` — E1
6. `docs(adr): groups listing picker` — E2 (optional)
