# Phase 31 — Challenge Tiers (TASKS)

> Stack: ASP.NET Core 9 + EF Core 9 + Postgres 16 + xUnit (api);
> Next.js 16 + Capacitor + TanStack Query + Vitest (mobile).

## Legend

- `[T]` TDD (failing test first, show red, then implement).
- `[S]` sequential (must run in listed order).
- `[P]` parallel (safe to run in any order or in parallel with
  peers).
- `[DEFERRED]` out of scope for this phase.
- `[ ]` not started, `[~]` in progress,
  `[x] ✅ commit <hash>` done.

## Dependency note

Blocked by **XPK-32 (share-link MVP) merging to main first**. This
phase extends `ChallengeService.CreateAsync`,
`CreateChallengeInput` / `CreateChallengeResponse` /
`GetChallengeResponse`, and the mobile `CreateChallengeForm` — all
touched by XPK-32. Starting work before merge means an immediate
rebase conflict.

---

## Section A — Schema

- [ ] **T-031-01 `[T][S]`** — `challenge_tiers` table
  - Failing test (`apps/api.Tests/Challenges/ChallengeTierPersistenceTests.cs`):
    round-trip insert reloads title, min_level, position; rejects
    `min_level <= 0`; rejects blank/61+ title; rejects duplicate
    `(challenge_id, min_level)`; cascades on challenge delete.
  - New entity `ChallengeTier` in
    `apps/api/src/Challenges/Entities/ChallengeTier.cs`.
  - `AppDbContext.ChallengeTiers` DbSet + model config:
    - 2 check constraints: `min_level > 0`, title shape.
    - 2 indexes: unique `(challenge_id, min_level)`, non-unique
      `(challenge_id, position)`.
    - FK to `challenges` with cascade delete.
  - Migration `AddChallengeTiers` generated via `dotnet ef
    migrations add`.
  - Green.

## Section B — Domain

- [ ] **T-031-02 `[T][P]`** — tier resolver (pure function)
  - Depends on T-031-01 for the type only.
  - Failing test (`apps/api.Tests/Challenges/ChallengeTierResolverTests.cs`):
    `ResolveCurrent([], 42) == null`; `ResolveCurrent([{10}, {30}, {60}], 5) == null`;
    `ResolveCurrent([{10}, {30}, {60}], 10) == {10}`;
    `ResolveCurrent([{10}, {30}, {60}], 29) == {10}`;
    `ResolveCurrent([{10}, {30}, {60}], 30) == {30}`;
    `ResolveCurrent([{10}, {30}, {60}], 999) == {60}`.
  - Pure static function (no DI, no DbContext). Lives on the
    entity or in a small `ChallengeTierResolver` static class
    inside `apps/api/src/Challenges/Services/`.
  - Green.

- [ ] **T-031-03 `[T][S]`** — tier exceptions
  - Depends on T-031-01.
  - Failing tests (per exception: constructor sets properties,
    message includes the id).
  - Add `TooManyTiersException(Guid challengeId, int count, int max)`
    and `DuplicateTierException(Guid challengeId, int minLevel)` to
    `apps/api/src/Challenges/Services/ChallengeInvitationExceptions.cs`
    (rename the file to `ChallengeExceptions.cs` at this point —
    the file already holds non-invitation exceptions).
  - Green.

## Section C — Service

- [ ] **T-031-04 `[T][S]`** — `ChallengeService.CreateAsync` persists tiers
  - Depends on T-031-01, T-031-03.
  - Failing tests (`apps/api.Tests/Challenges/ChallengeServiceTests.cs`):
    - Create with 3 tiers persists them in position order.
    - Create with empty list persists no rows and returns empty
      `Tiers` on the result.
    - Create with 11 tiers throws `TooManyTiersException`.
    - Create with duplicate `MinLevel` throws
      `DuplicateTierException`.
    - Create normalises titles with `Trim()`.
  - Extend `CreateChallengeInput` with
    `IReadOnlyList<CreateChallengeTierInput>? Tiers = null`.
  - Extend `CreateChallengeResult` with
    `IReadOnlyList<ChallengeTier> Tiers`.
  - Add insert of tier rows to the same `SaveChangesAsync` call.
  - Green.

- [ ] **T-031-05 `[T][S]`** — `ChallengeService.GetForUserAsync` returns tiers + currentTier
  - Depends on T-031-02, T-031-04.
  - Failing tests:
    - GET for a member with `progress.value = 30` and tiers at
      10/30/60 returns `CurrentTier.MinLevel == 30`.
    - GET for a member with `progress.value = 5` returns
      `CurrentTier == null` and `Tiers.Count == 3`.
    - GET for a non-member on a public challenge returns
      `Tiers` populated and `CurrentTier == null`.
    - GET for a challenge with no tiers returns
      `Tiers.Count == 0` and `CurrentTier == null`.
  - Extend `ChallengeDetail` with
    `IReadOnlyList<ChallengeTier>? Tiers = null,
     ChallengeTier? CurrentTier = null`.
  - Load tiers ordered by `MinLevel`.
  - Resolve `CurrentTier` via the pure function from T-031-02.
  - Green.

## Section D — HTTP

- [ ] **T-031-06 `[T][S]`** — request DTO + validator
  - Depends on T-031-04.
  - Failing tests
    (`apps/api.Tests/Challenges/CreateChallengeRequestValidatorTests.cs`
    — create if absent):
    - 11-tier list is invalid with "max 10 tiers".
    - Blank title inside any tier is invalid.
    - Duplicate `minLevel` across tiers is invalid.
    - `minLevel` ≤ 0 or `> 999` is invalid.
    - Empty list and null list are both valid.
  - Add `CreateChallengeTierRequest(int MinLevel, string Title)`
    to `apps/api/src/Challenges/Dto/CreateChallengeRequest.cs`
    (or a sibling file).
  - Grow `CreateChallengeRequest` with
    `IReadOnlyList<CreateChallengeTierRequest>? Tiers`.
  - Add the four rules to
    `CreateChallengeRequestValidator`.
  - Green.

- [ ] **T-031-07 `[T][S]`** — response DTOs
  - Depends on T-031-04, T-031-05.
  - Failing tests (`apps/api.Tests/Challenges/ChallengesEndpointTests.cs`
    new cases):
    - 201 with 3 tiers echoes them in the response body under
      `shareLink`.
    - 422 on 11 tiers / duplicate / blank title (match validator
      output).
    - GET detail returns `tiers[]` (ordered) and `currentTier`.
  - Add `ChallengeTierResponse(id, minLevel, title, position)`
    with a `From(ChallengeTier)` factory to
    `apps/api/src/Challenges/Dto/ChallengeResponse.cs`.
  - Extend `CreateChallengeResponse` with
    `IReadOnlyList<ChallengeTierResponse> Tiers`.
  - Extend `GetChallengeResponse` with
    `IReadOnlyList<ChallengeTierResponse> Tiers` and
    `ChallengeTierResponse? CurrentTier`.
  - Pass the mapped lists through
    `CreateChallengeEndpoint` and `GetChallengeEndpoint`.
  - Green.

## Section E — Shared types

- [ ] **T-031-08 `[S]`** — shared types + mobile service
  - Depends on T-031-07.
  - Add `ChallengeTier`, `CreateChallengeTierInput` to
    `packages/shared/src/challenges/types.ts`.
  - Extend `CreateChallengeRequest`, `CreateChallengeResponse`
    and `GetChallengeResponse` with the new fields.
  - Nothing to add in `challenges.service.ts` — the extended
    response types flow through automatically.
  - Green (TypeScript compiles cleanly across api / mobile /
    shared).

## Section F — Mobile

- [ ] **T-031-09 `[T][S]`** — `<CreateChallengeForm />` grows the tiers
  section
  - Depends on T-031-08.
  - Failing spec
    (`apps/mobile/test/components/organisms/CreateChallengeForm.spec.tsx`
    new cases): adds a tier row, submits payload with
    `tiers: [{ minLevel: 10, title: 'Mago' }]`; cap at 10 rows
    disables the add button; blank title in any row blocks
    submit with the right error.
  - Add a `useFieldArray`-driven "Fases (opcional)" section with
    repeatable `[Nivel]` + `[Título]` + `[remover]` rows and an
    "Adicionar fase" affordance.
  - Extend the zod schema with a `tiers` field capped at 10
    entries.
  - Submit payload omits `tiers` entirely when zero rows.
  - Green.

- [ ] **T-031-10 `[T][S]`** — `<ChallengeDetailTiers />` organism
  - Depends on T-031-08.
  - Failing spec
    (`apps/mobile/test/components/organisms/ChallengeDetailTiers.spec.tsx`
    — create):
    - Renders a 3-step ladder with the middle tier marked
      "Atual" and the next tier marked "Próximo".
    - Renders nothing when `tiers.length === 0`.
    - Below-first-tier state: no "Atual" marker, first tier is
      "Próximo".
  - New organism at
    `apps/mobile/src/components/organisms/ChallengeDetailTiers.tsx`.
  - Props: `{ tiers: ChallengeTier[], currentTier: ChallengeTier | null }`.
  - Green.

- [ ] **T-031-11 `[T][S]`** — wire the ladder into the detail page
  - Depends on T-031-10.
  - Failing spec
    (`apps/mobile/test/app/(app)/challenges/view/page.spec.tsx`
    new cases): owner with tiers sees the ladder; member below
    first tier sees it with "Próximo" on tier 0.
  - Render `<ChallengeDetailTiers tiers={detail.tiers}
    currentTier={detail.currentTier} />` on
    `apps/mobile/src/app/(app)/challenges/view/page.tsx` between
    the progress and the share button.
  - Green.

## Section G — Wrap-up

- [ ] **T-031-12 `[P]`** — ADR
  `docs/adr/0031-challenge-tiers.md`
  - Captures the decision to make tiers owner-defined per
    challenge (not reusing Phase 8), the 10-row hard cap, the
    "no edit after creation" scope cut, and the resolver pattern
    (pure function on an ordered list).

- [ ] **T-031-13 `[P]`** — DOCS update
  - If `DOCS/back-end/Challenges/` exists at implementation time,
    add a "Challenge Tiers" subflow doc; otherwise skip.

- [ ] **T-031-14 `[S]`** — Phase close
  - `dotnet test apps/api.Tests` green.
  - `pnpm --filter @xpeak/mobile test` green.
  - `dotnet format --verify-no-changes` clean on `apps/api` +
    `apps/api.Tests`.
  - `pnpm --filter @xpeak/mobile lint` clean.
  - `pnpm --filter @xpeak/mobile build` succeeds.
  - CI green on the PR.

---

## Dependencies

```
A1 ──▶ B1 (resolver)
   └──▶ B2 (exceptions) ──▶ C1 (service.create) ──▶ C2 (service.get)
                                                       │
                                     C1 ──▶ D1 (request DTO + validator)
                                     C1,C2 ──▶ D2 (response DTOs + endpoints)
                                                       │
                                                       ▼
                                                   E1 (shared types)
                                                       │
                                                       ▼
                                            F1 (form) + F2 (ladder) ──▶ F3 (wire)
                                                       │
                                                       ▼
                                                G1 (ADR), G2 (docs), G3 (close)
```

## Bundling strategy for PRs

Single PR under branch `XPK-33/felpa-challenge-tiers`. Suggested
commit sequence:

1. `feat(challenges): add challenge_tiers table` — A1
2. `feat(challenges): tier resolver + exceptions` — B1 + B2
3. `feat(challenges): persist tiers on create and return them on get` — C1 + C2
4. `feat(challenges): tier request validation and response DTOs` — D1 + D2
5. `feat(shared): challenge tier types` — E1
6. `feat(mobile): create form grows a tiers section` — F1
7. `feat(mobile): challenge detail renders the tier ladder` — F2 + F3
8. `docs(adr): challenge tiers` — G1
9. Phase close — G2 + G3

The order deliberately keeps schema + domain + service in the
first three commits so a reviewer can approve the data plane
before the HTTP + UI diff lands.
