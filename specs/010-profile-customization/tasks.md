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

- [x] **T-010-01 `[T][S]`** — `Backgrounds` enum + `background` on `users` ✅ commit `b892a56`
  - Native Postgres `ENUM` type `backgrounds` with members
    `stars`, `lightning`, `etched_accretion`; CLR enum
    `Backgrounds` mirrored to it via `MapEnum` on both the data
    source and the EF options plus `HasPostgresEnum` on the model.
  - `AppUser.Background` is `Backgrounds?` (null = render the
    mobile default); column `background` nullable.
  - Tests: defaults-to-null + Theory round-trip covering the three
    members, verifying the on-disk wire form via raw SQL.

- [x] **T-010-02 `[T][S]`** — `/me` response grows `background` ✅ commit `86fe1c3`
  - `UserResponse` and `AppUserExtensions.ToResponse` carry the
    enum through as `Backgrounds?`; the global
    `JsonStringEnumConverter(SnakeCaseLower)` serialises it to the
    wire form (`"stars"`, `"etched_accretion"`, or `null`).
  - All three callers (`/me`, `/auth/register`, `/auth/login`)
    already go through `ToResponse`, so no endpoint changes.
  - Tests: `/auth/register` happy-path asserts `Background` null
    on a fresh account; `/me` round-trip asserts it echoes
    `"etched_accretion"` after seeding the field directly through
    `UserManager`.

- [x] **T-010-03 `[T][S]`** — `PATCH /me/background` endpoint ✅ commit `5bb2795`
  - DTO takes `string? Background` (not the enum type) so bind-time
    failures stay off the hot path; the FluentValidation rule parses
    through `BackgroundsExtensions.TryParseWire` (a dict derived
    from the enum via `JsonNamingPolicy.SnakeCaseLower`), so the
    rejection surfaces as 422 with the standard `errors[]` shape.
  - Endpoint reuses `AppUserExtensions.ToResponse` so the 200 body
    is the same `/me` payload the mobile already consumes.
  - **The enum is the single source of truth on both sides.**
    Adding a new background requires:
    1. Add the member to `Backgrounds` in C# (the wire map rebuilds
       itself, the Postgres enum needs an `ALTER TYPE … ADD VALUE`
       migration).
    2. Add the matching key to the TS `Backgrounds` enum in
       `@xpeak/shared`.
    3. Register the component in `backgrounds/registry.ts`.
    All three or nothing — the compiler will yell if the mobile
    picks a slug the backend doesn't accept.
  - Tests: happy path (slug persists + echoes in `/me`), 422 for
    unknown slug, 422 for blank/missing slug, 401 without auth.

## Section B — Shared types + mobile registry

- [x] **T-010-04 `[S]`** — Shared types + mobile service ✅ commit `aa2e61c`
  - `enum Backgrounds { Stars = 'stars', Lightning =
    'lightning', EtchedAccretion = 'etched_accretion' }` in
    `@xpeak/shared`; snake_case values byte-match the C# wire
    form (`JsonNamingPolicy.SnakeCaseLower`).
  - `User.background: Backgrounds | null`;
    `UpdateBackgroundRequest { background: Backgrounds }`.
  - Mobile: `authService.patchBackground(bg)` + matching
    mutation on `useAuth` (TanStack Query) following the
    class+interface+singleton pattern. `onSuccess` writes the
    fresh user into the `me` query cache so every screen
    bound to the auth hook reflects the pick without a refetch.

- [x] **T-010-05 `[S]`** — Register `stars` and `etched_accretion` ✅ commit `29fee07`
  - `starsManifest` wraps `atoms/StarsBackground`;
    `etchedAccretionManifest` wraps `etched-accretion` with the
    `crimson` preset and `interactive={false}` so it stays a pure
    render layer behind the UI.
  - `BACKGROUNDS` is now `Record<Backgrounds, BackgroundManifest>`
    keyed by the shared enum, `DEFAULT_BACKGROUND` flipped to
    `Backgrounds.Stars`, `getBackground` returns the default for
    null/undefined inputs.
  - `UserBackground` client component resolves `me.data?.background`
    through the registry and renders the manifest Component as an
    `absolute inset-0 -z-10` layer; `layout.tsx` dropped the
    hard-coded `<StarsBackground>` wrapper in favour of it.

## Section C — Mobile UX

- [x] **T-010-06 `[T][S]`** — `/profile/background` picker page ✅ commit `a1adb15`
  - `apps/mobile/src/app/(app)/profile/background/page.tsx`: ARIA
    radiogroup with a live mini-instance of each manifest Component,
    `aria-checked` driven by the pending `override` (user tap) or
    the persisted `current` when nothing is touched yet.
  - Save fires `authHooks.patchBackground.mutate(selection)` and
    navigates to `/profile` on success; Cancel just navigates back.
    Save stays disabled until the override diverges from the
    persisted pick (no no-op PATCH).
  - Spec stubs `window.matchMedia` once in `vitest.setup.ts` so the
    Etched Accretion component (and any future reduced-motion
    consumer) can mount under jsdom.
  - Tests: current slug is marked + a new pick lands a PATCH with
    the expected body and routes to `/profile`; Save is disabled
    when override matches current; Cancel never hits the API.

- [x] **T-010-07 `[S]`** — "Trocar background" affordance on profile ✅ commit `672dc13`
  - `next/link` pill between the username header and the stats
    tabs on `/profile`, lucide Palette icon + "Trocar background"
    label, routes to `/profile/background`.

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

---

## Section F — Scope pivot (2026-11-04): background is a public showcase

Original cut treated `background` as self-decoration (the viewer's
pick painted every screen of the app). Intent was actually a
**public profile showcase**: when another user navigates to your
profile they should see the background **you** picked, not the one
they picked for themselves. The MVP work from Sections A–E stays
usable (column, enum, PATCH endpoint, picker, registry) but the
rendering path has to flip: the bg is a decoration of the user
being **viewed**, not the user viewing.

Open questions resolved:
- `/profile` stays as the owner's management view (header, stats,
  picker affordance, future logout). It keeps rendering the owner's
  own background as a preview of what visitors see.
- `/u/<username>` is the public showcase route — any authenticated
  user can land there and see the owner's background + public card.
- First entry point is the **leaderboard** row (cheapest wiring).
  Challenge membership lists and check-in feed avatars link later.

- [x] **T-010-14 `[S]`** — Un-wire the global background layer ✅ commit `88d1257`
  - `app/layout.tsx` drops the `<UserBackground>` wrapper and goes
    back to a plain `<div className="flex flex-1 flex-col">` so the
    app chrome is viewer-agnostic again.
  - Component renamed `UserBackground` → `ProfileBackground` and
    now takes `background: Backgrounds | null | undefined` as a
    prop instead of reading from the `me` query; falls back to the
    registry default when null. Meant to be dropped inside a
    `relative` parent that owns its height.

- [x] **T-010-15 `[T][S]`** — Public user endpoint ✅ commit `218e21a`
  - `GET /users/{username}` returns `PublicUserResponse
    { id, username, avatarUrl, level, xp, background, createdAt }`
    — email and streak counters stay on `/me`.
  - Case-insensitive lookup against `NormalizedUserName`; 404 when
    the row does not exist.
  - Auth is required (`RequireAuthorization`) so the endpoint
    lives inside the signed-in surface; opening it to anonymous
    traffic later is a one-line change if we ever want shareable
    public links.
  - Tests: 401 without a token, happy path echoing the background
    wire form, case-insensitive lookup, 404 for unknown.

- [x] **T-010-16 `[S]`** — Shared types + mobile service ✅ commit `5e24796`
  - `PublicUser` interface in `@xpeak/shared/users/types` mirrors
    the backend `PublicUserResponse` (no email, no streaks).
  - `usersService.get(username)` wraps `GET /users/{username}`,
    returns `null` on 404 so callers render an empty state instead
    of unwinding into an error boundary.
  - `usersHooks.useOne(username)` is a TanStack `useQuery` keyed by
    `['users', 'one', username]`, gated by `enabled: Boolean(username)`
    so pages that pass `undefined` while a param loads do not fire
    a stray `/users/undefined` request. Follows the
    class+interface+singleton pattern.

- [x] **T-010-17 `[T][S]`** — `/u?username=<name>` showcase page ✅ commit `d76d389`
  - Route is `/u?username=<name>` (search param) instead of
    `/u/<name>`. Next.js 16 static export rejects dynamic segments
    without `generateStaticParams`, and enumerating every user at
    build time is a non-starter — the search-param variant builds
    a single static page that resolves the owner at runtime.
  - `page.tsx` wraps `useSearchParams()` in a `<Suspense>`
    boundary (required by Next 16's CSR bailout rules) and reads
    the username from there.
  - Renders `<ProfileBackground background={user.background} />`
    (owner's pick, not the viewer's) plus a public card with
    avatar/initials, `@username`, level and XP.
  - 404 state when `usersService.get` returns `null`.
  - Spec: renders the owner's background slug + 404 flow under a
    mocked `useSearchParams` returning `?username=felipe`.

- [ ] **T-010-18 `[S][DEFERRED]`** — Leaderboard entry point
  - Blocked on the real leaderboard page landing — today it is a
    `<ComingSoon>` placeholder, no rows to link. When the ranked
    list ships, wrap each row in
    `<Link href={\`/u?username=${username}\`}>` to jump into the
    showcase. In the meantime T-010-19 adds the owner-side entry
    point ("Ver como visitante") so the loop is at least
    verifiable end-to-end.

- [x] **T-010-19 `[S]`** — Own-profile preview parity ✅ commit `cbf101c`
  - `/profile` wraps its content in `relative overflow-hidden` and
    drops `<ProfileBackground background={user.background} />` as
    the back layer so the owner sees the same chrome visitors see
    at `/u?username=<me>`.
  - Adds a "Ver como visitante" link next to "Trocar background"
    that routes to `/u?username=<me>` (with `encodeURIComponent`
    on the username). Lets the owner verify the public card in one
    tap and covers the leaderboard entry point that T-010-18
    cannot reach while the leaderboard is still a placeholder.

---

## Section G — Challenge member ranking (another entry point)

Challenges already model `ChallengeMembership` + `MembershipProgress`
on the backend, but today only the caller's own progress is
reachable (`GET /challenges/{id}/progress`). Nowhere in the app
can a user see the other people in a group or how they rank. That
makes the showcase feature almost invisible — the only way to land
on `/u?username=X` is to paste the URL by hand, since the
leaderboard (T-010-18) is still a placeholder.

Shipping the member list inside the challenge detail closes the
loop: every challenge becomes a mini-leaderboard whose rows link
into `/u?...`, which is where backgrounds pay off. Likely a
`XPK-35/felpa-challenge-members` branch so XPK-34 can go to review
without pulling in challenge-UI changes.

- [ ] **T-010-20 `[T][S]`** — Member ranking endpoint
  - `GET /challenges/{id}/members` returns a paged list of
    `{ user: PublicUser, progress: MembershipProgress, tier?: … }`
    ordered by progress (descending). Only callable by a current
    member of the challenge (404 for non-members, same no-leak
    rule `/challenges/{id}/progress` already follows).
  - Query joins `ChallengeMemberships` to `AppUser` and reuses the
    existing `IProgressCalculator` per row — batch the
    calculations so the whole list is one round trip.
  - Tests: non-member 404, member sees full ranking ordered by
    progress, pagination cursor stable across identical-progress
    ties (tiebreak by `UserId`).

- [ ] **T-010-21 `[S]`** — Shared types + mobile ranking row
  - `ChallengeMemberRanking` interface in `@xpeak/shared` wrapping
    `PublicUser` + progress + optional tier.
  - `challengesService.listMembers(id, cursor?)` + a hook on the
    existing `challengesHooks` singleton (`useMembers(id)`).
  - Renders inside the challenge view page as a vertical list; each
    row is a `<Link href="/u?username=${member.user.username}">`
    with avatar/initials, display name, a compact progress bar and
    the tier pill if present. Highlights the current user's row so
    they can find themselves fast.
  - Closes T-010-18's intent without waiting on the global
    leaderboard — challenge view becomes the natural way to hop
    into other people's showcases.

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
