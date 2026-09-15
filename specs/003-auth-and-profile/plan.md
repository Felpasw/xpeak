# Phase 3 — Auth & Basic Profile

> Status: **planning only**. Companion to `spec.md` (details) and
> `tasks.md` (execution breakdown).

## 1. Goal

Enable a user to create an account, log in, and see their profile.
Keep the scope tight: two auth methods (email+password and Google
OAuth), one profile screen showing zeroed-out progression stats. No
XP flow, no check-ins, no challenges — just identity and the seat
where progression will live from Phase 4 onwards.

## 2. Scope

**In scope**
- `AppUser` entity extending `IdentityUser<Guid>` with `Username`,
  progression fields (`Level`, `Xp`, `CurrentStreakDays`,
  `LongestStreakDays`), and `GoogleUid`.
- Endpoints for register, login, logout, current user.
- Google OAuth flow (server-side via
  `Microsoft.AspNetCore.Authentication.Google`) with JWT issuance
  on callback.
- JWT access token issued by our API via
  `Microsoft.AspNetCore.Authentication.JwtBearer` +
  `Microsoft.IdentityModel.Tokens`.
- Token revocation via a `RevokedToken` table checked in a custom
  `IJwtValidator`.
- Rate limiting on `/auth/register` and `/auth/login` via the
  built-in `Microsoft.AspNetCore.RateLimiting`.
- Mobile screens: register, login (email + Google), profile.
- Profile shows zeroed stats: `Level=0`, `Xp=0`, `Streak=0`.
- Client-side token storage (Capacitor Preferences on device,
  in-memory fallback on web).
- Auth guard: any authenticated screen redirects to login if no
  token.
- Integration tests covering happy path + main failures.
- Vitest specs for the mobile auth flow (mocking the API).

**Out of scope (later phases)**
- Password reset / forgot password flow.
- Email verification.
- 2FA / MFA.
- Apple Sign-in (deferred; mandatory only when iOS ships to App
  Store).
- Magic links.
- Refresh tokens (deferred; see §7 risk trade-off).
- Any progression logic (XP, level up, streak) — Phase 4+.
- Profile customization (avatar upload, banner, frame) — Phase 10.

## 3. Rationale

Auth is the smallest possible gate that unblocks every subsequent
phase: XP belongs to a user, check-ins belong to a user, challenges
have members. Without a user identity, every later phase has to
either fake it (throwaway seeds) or wait — better to just get it
out of the way with the minimum viable surface.

"Simple" as requested: two auth methods, JWT, no refresh, no email
verification for MVP. Every deferred item is documented in §2 so
we know the debt we're taking.

## 4. Approach

### 4.1. Backend

- **ASP.NET Core Identity** with a custom `AppUser : IdentityUser<Guid>`
  and EF Core stores (`Microsoft.AspNetCore.Identity.EntityFrameworkCore`).
  We keep Identity's password hashing (`PasswordHasher<AppUser>`) and
  validation stack but strip the cookie/UI parts (`AddIdentityCore`,
  not `AddDefaultIdentity`).
- **JWT bearer** for API sessions
  (`Microsoft.AspNetCore.Authentication.JwtBearer` +
  `System.IdentityModel.Tokens.Jwt`).
- **Google OAuth** via `Microsoft.AspNetCore.Authentication.Google`.
  The callback handler uses a **temporary cookie** to bridge the
  redirect, then exchanges the Google identity for our JWT and
  clears the cookie.
- **Revocation:** custom `RevokedToken` table with `Jti` + `ExpiresAt`
  columns; a JWT validation event checks the table and rejects
  revoked tokens. A `BackgroundService` sweeps expired rows daily.
- **Rate limiting:** `builder.Services.AddRateLimiter(...)` with
  named policies applied to `/auth/register` and `/auth/login` via
  `RequireRateLimiting("auth-register")` /
  `RequireRateLimiting("auth-login")`.
- Token TTL: **30 days** (long-lived, no refresh). Trade-off noted
  in §7.

### 4.2. Frontend

- Auth state managed by TanStack Query (`useMe` returns current
  user or `null`).
- Token stored via `@capacitor/preferences` on device,
  `localStorage` as web fallback (same key: `xpeak.auth.token`).
- Axios/fetch client injects `Authorization: Bearer <token>` on
  every request; on 401, clears the token and redirects to
  `/login`.
- Google Sign-in on mobile goes through Capacitor's browser plugin
  (`@capacitor/browser`) hitting our API OAuth start URL — no
  native Google SDK in this phase.

## 5. High-level artifact list

Files that will land in this phase (implementation-time, not now):

**API side (`apps/api/`)**
- NuGet additions: `Microsoft.AspNetCore.Identity.EntityFrameworkCore`,
  `Microsoft.AspNetCore.Authentication.JwtBearer`,
  `Microsoft.AspNetCore.Authentication.Google`,
  `Microsoft.AspNetCore.Authentication.Cookies` (temporary
  bridge), `Microsoft.IdentityModel.Tokens`,
  `System.IdentityModel.Tokens.Jwt`.
- Migrations: `AddIdentitySchema`, `AddProgressionAndUsernameToUsers`,
  `AddRevokedTokens`.
- `Domain/User/AppUser.cs`, `Domain/User/UsernameGenerator.cs`,
  `Domain/User/ReservedUsernames.cs`.
- `Infrastructure/AppDbContext.cs` extended with
  `DbSet<AppUser>` + `DbSet<RevokedToken>`.
- `Infrastructure/Auth/JwtTokenIssuer.cs`,
  `Infrastructure/Auth/RevokedTokenStore.cs`,
  `Infrastructure/Auth/RevokedTokenSweeper.cs` (`BackgroundService`).
- `Endpoints/AuthEndpoints.cs` with `MapAuthEndpoints` extension
  (register, login, logout, google, google/callback).
- `Endpoints/MeEndpoints.cs` with `MapMeEndpoints` extension.
- `Endpoints/Contracts/*.cs` — request/response `record`s.
- Tests: `AuthEndpointTests.cs`, `MeEndpointTests.cs`,
  `GoogleOAuthEndpointTests.cs`, `RateLimitTests.cs`,
  `UsernameGeneratorTests.cs`.

**Mobile side (`apps/mobile/`)**
- `app/(auth)/register/page.tsx`, `login/page.tsx`.
- `app/(app)/profile/page.tsx`.
- `app/auth/callback/page.tsx`.
- `lib/auth/client.ts` (token storage + axios instance).
- `lib/auth/queries.ts` (`useMe`, `useLogin`, `useRegister`,
  `useLogout`).
- Middleware or client-side guard for authenticated routes.
- Specs: `app/**/*.test.tsx` for the auth screens using MSW to
  mock the API.

**Shared (`packages/shared/`)**
- `src/auth/types.ts` — `User`, `AuthResponse` types shared
  between API contract and mobile consumer (single source of truth
  for the DTO shape).

## 6. Success criteria

- A user can register with `Username + Email + Password` and lands
  on the profile screen.
- A user can log in with `identifier` (email or username) +
  `password` and lands on the profile screen.
- A user can log in with Google (OAuth round-trip through the API)
  and lands on the profile screen.
- The profile screen shows `@username`, `level=0`, `xp=0`,
  `streak=0`, and email.
- Logging out revokes the token in `RevokedToken` and forces a
  redirect to `/login`.
- Any authenticated route hit without a valid token redirects to
  `/login`.
- All happy-path and main-failure integration tests pass on the
  API.
- All mobile auth screen specs pass under Vitest.
- CI green on `api-test`, `mobile-test`, `commitlint`.

## 7. Risks and trade-offs

| Risk / trade-off                                          | Position                                                                 |
|-----------------------------------------------------------|--------------------------------------------------------------------------|
| Long-lived JWT (30 days) without refresh                  | Accepted for MVP. `RevokedToken` mitigates leaked tokens via revocation. |
| No email verification means anyone can register           | Accepted for MVP. Rate limiter throttles signup. Add verification later. |
| Google OAuth callback needs a valid HTTPS redirect URI    | Dev uses `http://localhost:5000/auth/google/callback`; Google Cloud project must whitelist it. Documented in `spec.md` §5. |
| Storing token in `localStorage` on web is XSS-vulnerable  | Accepted for MVP; web is a fallback anyway. Capacitor Preferences on the actual app. Sanitize all rendered strings. |
| `RevokedToken` table grows unbounded                      | `RevokedTokenSweeper` `BackgroundService` prunes expired rows daily.     |
| Google account with same email as existing password user  | Auto-link on match: if `Email` already exists, attach `GoogleUid` to that user and issue JWT. Documented in `spec.md` §4.5. |
| Rate limiter is in-process (per-node counters)            | Acceptable for MVP single-node. Migrate to a shared backend (Redis) when we scale horizontally. |
| Bridging Google OAuth → JWT needs a temporary cookie      | Standard pattern in ASP.NET Core. Cookie is signed-out immediately after JWT issuance. Covered by an integration test. |

## 8. Decisions (locked) and remaining open questions

**Locked:**
- ✅ Register payload: **Username + Email + Password** only. No
  `Name` field.
- ✅ Password minimum length: **8 characters**.
- ✅ Rate limiting: **yes**, via `Microsoft.AspNetCore.RateLimiting`
  on `/auth/register` and `/auth/login`.
- ✅ Google Cloud project: personal account (owned by @Felpasw).

**Still open:**
1. **Mobile OAuth callback strategy** — how the API callback
   returns control to the Capacitor app:
   - **Option A — Deep link** (recommended). API redirects to
     `xpeak://auth/callback?token=<jwt>`. The OS intercepts the
     custom scheme and reopens the app on the callback page.
     Requires registering the URL scheme in
     `capacitor.config.ts`, `AndroidManifest.xml` and iOS
     `Info.plist`.
   - **Option B — In-app browser polling.** Open the OAuth flow
     inside `@capacitor/browser`, watch the URL for a token query
     param, close the browser when it appears. No custom scheme
     needed, but flakier UX.
2. **Google-created accounts have no username.** Google returns
   name/email, not a handle. Two options:
   - **Option A — Auto-generate** from the email local-part (e.g.,
     `felipe@x.com` → `felipe`, `felipe_2` on collision). Ships as
     "simple".
   - **Option B — Force a "choose username" screen** after the
     first Google callback for a new user (extra hop, more
     control).
3. **Structured logging** — introduce Serilog now (with JSON sink)
   or defer to a later observability phase?

## 9. Dependencies

- Requires Phase 1 (release workflow) and Phase 2 (ASP.NET Core +
  Next skeleton, `packages/shared`) — Phase 1 done, Phase 2 in
  progress.
- Blocks Phase 4 onwards (no user, no XP owner).

## 10. Next step

Answer the questions in §8, then finalize `tasks.md` and open the
first working branch for Section A of that file.
