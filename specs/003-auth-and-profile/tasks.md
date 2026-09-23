# Phase 3 — Auth & Basic Profile (TASKS)

> Companion to `spec.md` (what) and `plan.md` (why). Atomic
> execution steps. Nothing implemented yet.

## Legend

- `[T]` — TDD required: failing test first.
- `[S]` — sequential: order matters.
- `[P]` — parallelizable with sibling `[P]` tasks.
- `[HUMAN]` — human-only step (no automation possible).

Status glyphs:
- `[ ]` not started
- `[~]` in progress
- `[x] ✅ commit <hash>` done

---

## Section A — Prerequisites

- [ ] **T-003-01 `[HUMAN][S]`** — Google Cloud project
  - Create OAuth 2.0 Client ID (Web application type).
  - Authorized redirect URI:
    `http://localhost:5000/signin-google` and
    `http://localhost:5000/auth/google/callback`.
  - Paste `Google__ClientId` and `Google__ClientSecret` into
    local `.env` (not committed).
  - Deliverable: credentials in place locally + noted in
    `.env.example` as blank placeholders.

- [ ] **T-003-02 `[S]`** — Answer remaining open questions
  - Decisions already locked (see `plan.md` §8):
    - Register payload: `username + email + password` (no
      `name`).
    - Password length: 8+.
    - Rate limiting: yes.
    - Google Cloud: personal account.
  - Still open (`plan.md` §8): Q1 (mobile OAuth callback strategy
    — deep link vs. in-app browser), Q2 (Google username strategy
    — auto-generate vs. choose-username screen), Q3 (structured
    logging).
  - Deliverable: decisions pasted into this task before starting
    Section B.

## Section B — Identity, User, and JWT plumbing

- [x] ✅ commit `bd8a6f0` **T-003-03 `[S]`** — Add NuGet packages
  - `Microsoft.AspNetCore.Identity.EntityFrameworkCore`
  - `Microsoft.AspNetCore.Authentication.JwtBearer`
  - `Microsoft.AspNetCore.Authentication.Google`
  - `Microsoft.AspNetCore.Authentication.Cookies`
  - `Microsoft.IdentityModel.Tokens`
  - `System.IdentityModel.Tokens.Jwt`
  - `dotnet restore --locked-mode` clean.
  - Deliverable: updated `api.csproj`, `packages.lock.json`.

- [x] ✅ commit `bd8a6f0` (impl) + `886375d` (tests) **T-003-04 `[T][S]`** — `AppUser` entity + Identity schema
      migration
  - **Write** `AppUserValidatorTests`:
    - `username` presence + format `^[a-z0-9_]{3,20}$` +
      uniqueness (case-insensitive) + reserved-word rejection.
    - `email` presence + format + uniqueness (case-insensitive).
    - Progression fields defaulting to `0`.
    - Expect red.
  - Create `Domain/User/AppUser.cs` extending
    `IdentityUser<Guid>` with `AvatarUrl`, `GoogleUid`, `Level`,
    `Xp`, `CurrentStreakDays`, `LongestStreakDays`.
  - Create `Domain/User/ReservedUsernames.cs` with the block
    list.
  - Create `Domain/User/AppUserValidator.cs`
    (`IUserValidator<AppUser>`) enforcing the rules.
  - Extend `AppDbContext` to inherit from
    `IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>` and
    override `OnModelCreating` to enable `citext` and rename
    tables to lowercase snake_case.
  - Add migration `AddIdentitySchema` via `dotnet ef migrations
    add`.
  - `dotnet ef database update` → tables exist.
  - `dotnet test` → green.
  - Deliverable: migration + entity + validator + green tests.

- [x] ✅ commit `bd8a6f0` (impl) + `886375d` (tests) **T-003-05 `[T][S]`** — `UsernameGenerator`
  - **Write** `UsernameGeneratorTests` per `spec.md` §8.1 —
    expect red.
  - Implement `Domain/User/UsernameGenerator.cs` (pure static
    class, no DI needed).
  - `dotnet test` → green.
  - Deliverable: class + green tests.

- [x] ✅ commit `bd8a6f0` (impl) + `886375d` (tests) **T-003-06 `[T][S]`** — `RevokedTokens` table +
      `RevokedTokenStore`
  - **Write** `RevokedTokenStoreTests`: round-trip revoke →
    `IsRevokedAsync` returns true.
  - Migration `AddRevokedTokens`.
  - Implement `Infrastructure/Auth/RevokedTokenStore.cs` (async
    read + write against `DbContext`).
  - `dotnet test` → green.
  - Deliverable: migration + class + green tests.

- [x] ✅ commit `bd8a6f0` (impl) + `d5cdc0c` (defer config binding) **T-003-07 `[S]`** — Wire Identity + JWT in `Program.cs`
  - `AddIdentityCore<AppUser>()` per `spec.md` §3.1.
  - `AddAuthentication().AddJwtBearer(...)` with
    `OnTokenValidated` checking `RevokedTokenStore`.
  - Deliverable: `Program.cs` updated.

- [x] ✅ commit `bd8a6f0` **T-003-08 `[S]`** — `JwtTokenIssuer` service
  - `Infrastructure/Auth/JwtTokenIssuer.cs` with
    `IssueToken(AppUser)` → `(string jwt, string jti,
    DateTimeOffset expiresAt)`.
  - Registered as scoped in DI.
  - Deliverable: class + registration.

## Section C — Endpoints (email/password)

- [x] ✅ commit `bd8a6f0` (impl) + `886375d` (tests) **T-003-09 `[T][S]`** — Register endpoint
  - **Write** `AuthEndpointTests.Register_*` covering happy
    (`username + email + password`) + sad paths (missing/invalid
    username, reserved username, taken username/email, weak
    password) per `spec.md` §8.1 — expect red.
  - Create `Endpoints/AuthEndpoints.cs` with a
    `MapAuthEndpoints` extension method.
  - `POST /auth/register` uses `UserManager.CreateAsync`, issues
    JWT via `JwtTokenIssuer`, returns 201 with body + header.
  - Response contracts: `Endpoints/Contracts/RegisterRequest.cs`,
    `AuthResponse.cs`, `UserResponse.cs`.
  - `dotnet test` → green.
  - Deliverable: endpoints + contracts + green tests.

- [x] ✅ commit `bd8a6f0` (impl) + `886375d` (tests) **T-003-10 `[T][S]`** — Login endpoint (identifier)
  - **Write** `AuthEndpointTests.Login_*` covering login via
    email AND login via username, plus wrong password / unknown
    identifier → 401. Expect red.
  - `POST /auth/login` resolves `identifier` (email if contains
    `@`, else username), calls
    `SignInManager.CheckPasswordSignInAsync`, issues JWT on
    success.
  - `dotnet test` → green.
  - Deliverable: endpoint + green tests.

- [x] ✅ commit `bd8a6f0` (impl) + `886375d` (tests) **T-003-11 `[T][S]`** — Logout endpoint (revocation)
  - Extend `AuthEndpointTests`: logout inserts JTI into
    `RevokedTokens`; second `GET /me` with same token → 401.
    Expect red.
  - `POST /auth/logout` reads current `jti` from the principal
    and calls `RevokedTokenStore.RevokeAsync`.
  - `dotnet test` → green.
  - Deliverable: endpoint + extended tests.

- [x] ✅ commit `bd8a6f0` (impl) + `886375d` (tests) **T-003-12 `[T][S]`** — `GET /me` endpoint
  - **Write** `MeEndpointTests` per `spec.md` §8.1 — expect red.
  - Create `Endpoints/MeEndpoints.cs` with `MapMeEndpoints`.
  - `GET /me` is guarded by `.RequireAuthorization()`; loads user
    via `UserManager.FindByIdAsync(principal.FindFirstValue(ClaimTypes.NameIdentifier))`.
  - `dotnet test` → green.
  - Deliverable: endpoint + green tests.

## Section D — Google OAuth

- [x] ✅ commit `bd8a6f0` (impl) + `886375d` (tests) **T-003-13 `[T][S]`** — OAuth callback (new user)
  - Configure `TestAuthHandler` for the `Google` scheme in
    integration tests (stubs the Google identity).
  - **Write** `GoogleOAuthEndpointTests` covering the "new user"
    path — expect red.
  - `GET /auth/google` uses `Results.Challenge` to kick off flow.
  - `GET /auth/google/callback` reads temp cookie, calls
    `AccountLinker.FindOrCreateFromGoogle(...)`, issues JWT,
    signs out of temp cookie, redirects to configured URL.
  - `Infrastructure/Auth/AccountLinker.cs` handles find-or-create
    and uses `UsernameGenerator` for new users.
  - `dotnet test` → green.
  - Deliverable: endpoints + linker + green tests.

- [x] ✅ commit `bd8a6f0` (impl) + `886375d` (tests) **T-003-14 `[T][S]`** — OAuth callback (existing user by
      google_uid)
  - Extend test: pre-create user with `GoogleUid` matching
    stubbed UID; callback logs in without creating a new user.
  - Ensure `AccountLinker` finds by `GoogleUid` first.
  - `dotnet test` → green.
  - Deliverable: test + linker adjustment.

- [x] ✅ commit `bd8a6f0` (impl) + `886375d` (tests) **T-003-15 `[T][S]`** — OAuth callback (link by email)
  - Extend test: pre-create user with matching email but no
    `GoogleUid`; callback attaches `GoogleUid` and logs in.
  - Ensure `AccountLinker` handles the linking branch.
  - `dotnet test` → green.
  - Deliverable: test + linker adjustment.

- [x] ✅ commit `bd8a6f0` (impl) + `886375d` (tests) **T-003-16 `[T][S]`** — OAuth failure path
  - Extend test: stub the Google scheme to fail authentication;
    callback redirects with `?error=...`.
  - Implement failure branch in `google/callback` handler.
  - `dotnet test` → green.
  - Deliverable: test + handler branch.

## Section E — Rate limiting

- [x] ✅ commit `bd8a6f0` (impl) + `886375d` (tests) **T-003-17 `[T][S]`** — Rate limiter + `FailedLoginCounter`
  - Add rate limiter config per `spec.md` §3.4.
  - Apply `.RequireRateLimiting("auth-register")` and
    `.RequireRateLimiting("auth-login-ip")` on the endpoints.
  - Implement `FailedLoginCounter` (in-memory MVP) for the
    identifier-based 15-min cap.
  - **Write** tests asserting the throttles trigger and reset:
    - 4th register attempt within an hour from the same IP → 429.
    - 6th failed login for the same identifier within 15 minutes
      → 429.
    - Successful login doesn't count toward the failure counter.
  - `dotnet test` → green.
  - Deliverable: config + counter + tests.

- [x] ✅ commit `bd8a6f0` (impl) + `886375d` (tests) **T-003-18 `[T][S]`** — `RevokedTokenSweeper`
      (`BackgroundService`)
  - Failing test: seed old + fresh rows, run sweeper, assert
    only old ones deleted.
  - Implement in `Infrastructure/Auth/RevokedTokenSweeper.cs`;
    register in `Program.cs` via `AddHostedService`.
  - `dotnet test` → green.
  - Deliverable: sweeper + registration + tests.

## Section F — Env vars & repo hygiene

- [x] ✅ commit `bd8a6f0` **T-003-19 `[P]`** — `.env.example` updates
  - Add `Google__ClientId`, `Google__ClientSecret`,
    `Google__RedirectUri`, `Jwt__Key` (blank) to
    `apps/api/.env.example`.
  - Add `NEXT_PUBLIC_AUTH_DEEP_LINK_SCHEME` to
    `apps/mobile/.env.example`.
  - Deliverable: updated `.env.example` files.

- [ ] **T-003-20 `[P]`** — README updates
  - `apps/api/README.md`: Google OAuth setup section (link to
    Google Cloud console, redirect URI, env vars, `openssl rand
    -base64 48` for JWT key).
  - `apps/mobile/README.md`: auth flow overview and callback URL.
  - Deliverable: updated READMEs.

## Section G — Shared types

- [ ] **T-003-21 `[S]`** — Shared `User` / `AuthResponse` types
  - Create `packages/shared/src/auth/types.ts` per `spec.md` §7.
  - Re-export from `packages/shared/src/index.ts`.
  - Deliverable: two files.

## Section H — Mobile auth client

- [ ] **T-003-22 `[T][S]`** — Token storage abstraction
  - `lib/auth/storage.ts` with `getToken()`, `setToken(t)`,
    `clearToken()`, backed by `@capacitor/preferences` on device
    and `localStorage` on web.
  - Vitest spec: mocks storage backends and asserts round-trip.
  - Deliverable: module + spec.

- [ ] **T-003-23 `[T][S]`** — API client (axios instance)
  - `lib/auth/client.ts` exports an axios instance with
    `Authorization` interceptor + 401 handler that clears token
    and fires a callback.
  - Vitest spec asserts header injection and 401 behavior.
  - Deliverable: module + spec.

- [ ] **T-003-24 `[T][S]`** — Auth queries
  - `lib/auth/queries.ts` exports `useMe`, `useLogin`,
    `useRegister`, `useLogout` (TanStack Query hooks).
  - Vitest specs using MSW: assert each hook posts the right
    payload, stores token on success, invalidates the `useMe`
    cache on login/register/logout.
  - Deliverable: module + specs.

## Section I — Mobile screens

- [ ] **T-003-25 `[T][S]`** — Register screen
  - **Write** `app/(auth)/register/register.test.tsx` per
    `spec.md` §8.2: 3 fields (username, email, password),
    live-lowercase on username, per-field 422 errors — expect
    red.
  - Build the page: 3 fields, client-side validation, submit →
    `useRegister`.
  - Run spec → green.
  - Deliverable: page + spec.

- [ ] **T-003-26 `[T][S]`** — Login screen
  - **Write** login test: `identifier` field accepts email OR
    username, 401 error surface — expect red.
  - Build the page with `useLogin` posting
    `{ identifier, password }`.
  - Run spec → green.
  - Deliverable: page + spec.

- [ ] **T-003-27 `[T][S]`** — Google button (both screens)
  - Extend both specs: clicking "Log in with Google" opens
    `NEXT_PUBLIC_API_URL/auth/google` via `@capacitor/browser`
    (mocked in the test).
  - Add the button + handler.
  - Run specs → green.
  - Deliverable: shared `GoogleSignInButton` component + specs.

- [ ] **T-003-28 `[T][S]`** — Auth callback page
  - **Write** `app/auth/callback/callback.test.tsx` — expect red.
  - Build the page: reads `token`/`error`, stores or shows error.
  - Run spec → green.
  - Deliverable: page + spec.

- [ ] **T-003-29 `[T][S]`** — Profile screen
  - **Write** `app/(app)/profile/profile.test.tsx` — expect red.
  - Build the profile page: reads `useMe`, renders `@username`,
    email, avatar, zeroed stats, logout button.
  - Run spec → green.
  - Deliverable: page + spec.

- [ ] **T-003-30 `[T][S]`** — Route guard
  - Add a client-side guard (either a shared layout component
    under `app/(app)/layout.tsx` or a hook) that redirects to
    `/login` when `useMe` resolves to `null`.
  - Vitest spec covers the redirect path.
  - Deliverable: guard + spec.

## Section J — E2E validation

- [ ] **T-003-31 `[T][S]`** — Local end-to-end walkthrough
  - Run the full stack, follow `spec.md` §9 walkthrough.
  - Register via UI, hit `/me` via curl with captured token, log
    out, confirm 401.
  - Google login walkthrough on browser.
  - Deliverable: walkthrough notes + screenshots pasted into
    `docs/adr/0003-auth-and-profile.md`.

- [ ] **T-003-32 `[P]`** — ADR
  - `docs/adr/0003-auth-and-profile.md` documenting: ASP.NET
    Identity + JWT with `RevokedToken` server-side revocation,
    Google OAuth via cookie bridge, no refresh token, long-lived
    token trade-off, deferred items (email verification,
    password reset, Apple sign-in).
  - Deliverable: ADR file.

- [ ] **T-003-33 `[S]`** — Phase close
  - Mark all tasks with `[x]` and commit hashes.
  - Confirm success criteria (`spec.md` §9) fully met.
  - Verify release-please Release PR bumps root to `0.2.0` (and
    fans out to `apps/api/VERSION`, `apps/mobile/package.json`).
  - Deliverable: clean `main`, updated `roadmap.md` (flip Phase
    3 from `📋` to `🛠️`), ready for Phase 4.

---

## Dependencies

```
A (prereqs) ──▶ B (identity + user + jwt) ──▶ C (email/password endpoints)
                                          └─▶ D (google oauth)
                                          └─▶ E (rate limit + sweeper)
                                          └─▶ F (env + docs)
                                          └─▶ G (shared types)
                                               ↓
                                       H (mobile auth client)
                                               ↓
                                       I (mobile screens)
                                               ↓
                                       J (E2E validation + ADR)
```

## Bundling strategy for commits

1. `chore(api): add identity, jwt, google, cookies packages` — B
   (T-003-03)
2. `feat(auth): app user entity with identity + username validator` — B
   (T-003-04, T-003-05)
3. `feat(auth): revoked token store + jwt bearer with revocation check` — B
   (T-003-06, T-003-07, T-003-08)
4. `feat(auth): email+password endpoints (register/login/logout/me)` — C
   (T-003-09 → T-003-12)
5. `feat(auth): google oauth callback with link-by-email` — D
   (T-003-13 → T-003-16)
6. `feat(auth): rate limit register and login + revoked token sweeper` — E
   (T-003-17, T-003-18)
7. `chore(auth): env and readme updates` — F (T-003-19, T-003-20)
8. `feat(shared): user and auth response types` — G (T-003-21)
9. `feat(mobile): auth client and queries` — H (T-003-22 → T-003-24)
10. `feat(mobile): register, login, callback, profile screens` — I
    (T-003-25 → T-003-30)
11. `docs(adr): auth and basic profile` — J (T-003-32)

Each bundle stops at `git add` and waits for explicit approval
before committing.
