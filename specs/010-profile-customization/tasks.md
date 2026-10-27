# Phase 10 — Profile Customization (TASKS)

> **Scope cut (2026-11-10) — "Background picker MVP"**
>
> Phase 10 is being partially shipped under branch
> `XPK-34/felpa-profile-background` so the beta ships with
> something customisable without blocking on avatar upload UX
> (which already works via the Phase 3 default) or on a frames
> catalog table. The mobile `backgrounds/registry.ts` already has
> the shape (`BackgroundManifest { slug, name, Component,
> preview? }`) and a one-entry registry (`lightning`); this cut
> stores the user's choice server-side and wires the layout to
> use it. Everything else — avatar upload, banner, frames catalog
> — stays marked `[DEFERRED]` and ships in a follow-up phase.

## Legend
`[T]` TDD, `[S]` sequential, `[P]` parallel, `[DEFERRED]` out of
scope for this cut.

---

## Section A — Backend (background slug only)

- [ ] **T-010-01 `[T][S]`** — `BackgroundSlug` enum + `active_background_slug` on `users`
  - Failing test: round-trip the field via `BackgroundSlug.Stars`,
    nulls permitted, reload survives.
  - Add `enum BackgroundSlug { Stars, Lightning, EtchedAccretion }`
    in `apps/api/src/Users/Entities/` mirroring the mobile
    registry; map to lowercase + kebab-case on the wire via
    `JsonStringEnumConverter` (matches the global
    `JsonNamingPolicy.SnakeCaseLower` convention, with an explicit
    `[JsonPropertyName("etched-accretion")]` or `HasConversion`
    override for the kebab case).
  - Add `ActiveBackgroundSlug` property (`BackgroundSlug?`) to
    `AppUser`, map to `active_background_slug varchar(32)`
    nullable in `AppDbContext` (same enum→string conversion
    pattern as `ChallengeState`), generate migration
    `AddActiveBackgroundSlug`.
  - Green.

- [ ] **T-010-02 `[T][S]`** — `/me` response grows `activeBackgroundSlug`
  - Failing test: `UserResponse.From(user)` populates the field
    as the kebab/lowercase string; `/me` endpoint echoes it;
    register/login responses too.
  - Extend `UserResponse` (or `AppUserExtensions`) + the three
    endpoints that already map it (`/me`, `/auth/register`,
    `/auth/login`).
  - Green.

- [ ] **T-010-03 `[T][S]`** — `PATCH /me/background` endpoint
  - Failing tests: happy path updates the field and 200s with the
    new `/me` payload; **422 for an unknown slug** (any value
    outside the three enum members); 422 for missing/blank slug.
  - Request DTO `UpdateBackgroundRequest { Slug: BackgroundSlug }`
    — the global `JsonStringEnumConverter` rejects unknown values
    at bind time, which surfaces as 400 by default; make the
    endpoint explicitly return 422 (via `ValidateBody` or a
    try/catch on `JsonException`) so the mobile sees the same
    code it already handles for other validators.
  - **The enum is the single source of truth on both sides.**
    Adding a new background requires:
    1. Add the member to `BackgroundSlug` in C#.
    2. Add the matching key to the TS `BackgroundSlug` enum in
       `@xpeak/shared`.
    3. Register the component in `backgrounds/registry.ts`.
    All three or nothing — the compiler will yell if the mobile
    picks a slug the backend doesn't accept.
  - Green.

## Section B — Shared types + mobile registry

- [ ] **T-010-04 `[S]`** — Shared types + mobile service
  - Add `enum BackgroundSlug { Stars = 'stars', Lightning =
    'lightning', EtchedAccretion = 'etched-accretion' }` in
    `@xpeak/shared` — the TS enum values must byte-match the C#
    serialised form.
  - `User.activeBackgroundSlug: BackgroundSlug | null`.
  - `UpdateBackgroundRequest { slug: BackgroundSlug }`.
  - Mobile: `authService.patchBackground(slug)` method and the
    matching hook on the `useAuth` singleton (follow the
    class+interface pattern memorialised in
    `feedback_mobile_hooks_pattern.md`).

- [ ] **T-010-05 `[S]`** — Register `stars` and `etched-accretion`
  - Add `starsManifest` wrapping the existing
    `components/atoms/StarsBackground.tsx` and
    `etchedAccretionManifest` wrapping `etched-accretion.tsx`.
  - Register all three (`Stars`, `Lightning`, `EtchedAccretion`)
    in `BACKGROUNDS`, keyed by the shared `BackgroundSlug` enum,
    and flip `DEFAULT_BACKGROUND_SLUG` to `BackgroundSlug.Stars` —
    preserves the current cold-open look for existing users.
  - Rewire `app/layout.tsx` to render
    `getBackground(me.data?.activeBackgroundSlug).Component`
    instead of the hard-coded `<StarsBackground>` wrapper.

## Section C — Mobile UX

- [ ] **T-010-06 `[T][S]`** — `/profile/background` picker page
  - Failing spec: grid of registered backgrounds with the
    current one marked; tapping another + Save calls
    `PATCH /me/background` and lands on `/profile` with the new
    render active.
  - New page `apps/mobile/src/app/(app)/profile/background/page.tsx`.
  - Reuse the existing `useAuth` singleton for both read + the
    new mutation.

- [ ] **T-010-07 `[S]`** — "Trocar background" affordance on profile
  - Add a link/button on `/profile` that routes to
    `/profile/background`.

## Section D — Wrap-up

- [ ] **T-010-08 `[P]`** — ADR
  `docs/adr/0010-profile-background.md`
  - Captures the MVP cut (slug-only, no catalog table, no
    uploads), why registry stays on the mobile (components not
    assets), why we don't validate against an enum on the
    backend (single source of truth = mobile registry), and the
    deferred bits (avatar upload, frames, banner-mode uploaded
    images).

- [ ] **T-010-09 `[S]`** — Phase close
  - Backend `dotnet test` + `dotnet format` green.
  - Mobile `pnpm lint` + `pnpm test` + `pnpm build` green.
  - CI green on the PR.

## Section E — DEFERRED (future Phase 10 continuation)

- [ ] **T-010-10 `[T][S][DEFERRED]`** — Avatar upload via Cloudinary
  - Reuse the tier-image presign surface from Phase 31. The
    `AvatarUrl` field already exists on `AppUser`; this task
    adds the upload UX + swaps the free-form URL for a storage
    key.

- [ ] **T-010-11 `[T][S][DEFERRED]`** — Banner / cosmetic uploads
  - Only lands if we decide uploaded images are a distinct slot
    from `active_background_slug`. The roadmap already pivoted
    "banner" → "background" (a swappable **component**, not an
    uploaded asset), so this is likely re-scoped away.

- [ ] **T-010-12 `[T][S][DEFERRED]`** — Frames catalog
  - `frames` table + 10 seeded frames + overlay rendering on
    the avatar. Ships alongside Phase 18 (insignias) if we want
    frame unlocks tied to achievements.

<!-- T-010-13 was "backend enum validation deferred"; that is now
     part of T-010-01/03 since this cut rejects anything outside
     the shared BackgroundSlug enum on both sides. -->


---

## Bundling strategy for PRs

Background-only MVP cut (`XPK-34/felpa-profile-background`):

1. `docs(specs): carve background-only MVP out of phase 10` — this
   patch
2. `feat(profile): add active_background_slug column and endpoint` — A
3. `feat(mobile): register stars + accretion and wire layout` — B
4. `feat(mobile): background picker screen` — C
5. `docs(adr): profile background MVP` — D1
6. Phase close — D2

Deferred sections (E) ship under a follow-up branch after beta.
