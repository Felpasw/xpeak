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
- Users context with `username`, `email`, password hashing.
- Endpoints for register, login, logout, current user.
- Google OAuth flow (server-side via Ueberauth + Google strategy).
- JWT access token issued by the Phoenix API (via Guardian).
- Mobile screens: register, login (email + Google), profile.
- Profile shows zeroed stats: `level=0`, `xp=0`, `streak_days=0`.
- Client-side token storage (Capacitor Preferences on device, in-memory
  fallback on web).
- Auth guard: any authenticated screen redirects to login if no token.
- Controller tests covering happy path + main failures.
- Vitest specs for the mobile auth flow (mocking the API).

**Out of scope (later phases)**
- Password reset / forgot password flow — later (or never for MVP).
- Email verification — later.
- 2FA / MFA — deferred.
- Apple Sign-in — deferred (mandatory only when iOS ships to App
  Store; noted in `README.md` §10 as open question).
- Magic links — deferred.
- Refresh tokens — deferred (see §7 risk trade-off).
- Any progression logic (XP, level up, streak) — Phase 4+.
- Profile customization (avatar upload, banner, frame) — Phase 10.

## 3. Rationale

Auth is the smallest possible gate that unblocks every subsequent
phase: XP belongs to a user, check-ins belong to a user, challenges
have members. Without a user identity, every later phase has to
either fake it (throwaway seeds) or wait — better to just get it out
of the way with the minimum viable surface.

"Simple" as requested: two auth methods, JWT, no refresh, no email
verification for MVP. Every deferred item is documented in §2 so we
know the debt we're taking.

## 4. Approach

### 4.1. Backend

- **Users context** (`Xpeak.Accounts`) with a `User` schema
  (Ecto), holding `username`, `email`, `hashed_password`,
  `google_uid`, `avatar_url`, and progression fields.
- **`bcrypt_elixir`** for password hashing (fast enough, matches
  `phx.gen.auth` defaults).
- **Guardian** for JWT issuance and verification.
- **`guardian_db`** for the denylist (revoked tokens persisted in
  Postgres — no in-memory state, works across nodes).
- **Ueberauth** + **`ueberauth_google`** for Google OAuth. The
  Google callback controller finds or creates the user and issues a
  Guardian JWT.
- **Hammer** (`hammer_backend_ets` in dev/test, upgrade to
  `hammer_backend_mnesia` or Redis later) for rate limiting on
  `/auth/register` and `/auth/login`.
- Token TTL: 30 days (long-lived, no refresh). Trade-off noted in §7.

### 4.2. Frontend

- Auth state managed by TanStack Query (`useMe` returns current user
  or `null`).
- Token stored via `@capacitor/preferences` on device, `localStorage`
  as web fallback (same key: `xpeak.auth.token`).
- Axios/fetch client injects `Authorization: Bearer <token>` on
  every request; on 401, clears the token and redirects to `/login`.
- Google Sign-in on mobile goes through Capacitor's browser plugin
  (`@capacitor/browser`) hitting our Phoenix OAuth start URL — no
  native Google SDK in this phase.

## 5. High-level artifact list

Files that will land in this phase (implementation-time, not now):

**API side (`apps/api/`)**
- `mix.exs` additions: `bcrypt_elixir`, `guardian`, `guardian_db`,
  `ueberauth`, `ueberauth_google`, `hammer`, `hammer_backend_ets`.
- `priv/repo/migrations/*_create_users.exs`,
  `*_add_progression_fields_to_users.exs`,
  `*_create_guardian_db_tokens.exs`.
- `lib/xpeak/accounts.ex` — Users context (public API).
- `lib/xpeak/accounts/user.ex` — Ecto schema + changesets.
- `lib/xpeak/accounts/username_generator.ex` — deterministic
  username generator for Google-created accounts.
- `lib/xpeak/accounts/reserved_usernames.ex` — block list module.
- `lib/xpeak/auth/guardian.ex` — Guardian implementation
  (`subject_for_token`, `resource_from_claims`).
- `lib/xpeak_web/plugs/authenticate.ex` — plug that requires a
  valid JWT.
- `lib/xpeak_web/plugs/rate_limit.ex` — plug wrapping Hammer for
  the auth endpoints.
- `lib/xpeak_web/controllers/auth/registration_controller.ex`.
- `lib/xpeak_web/controllers/auth/session_controller.ex`.
- `lib/xpeak_web/controllers/auth/google_controller.ex`.
- `lib/xpeak_web/controllers/me_controller.ex`.
- `lib/xpeak_web/controllers/user_json.ex` — response shaping.
- `config/config.exs` — Guardian, Ueberauth, Hammer config.
- `config/runtime.exs` — reads `GOOGLE_CLIENT_ID`,
  `GOOGLE_CLIENT_SECRET`, `GUARDIAN_SECRET_KEY`.
- Tests: `test/xpeak/accounts_test.exs`,
  `test/xpeak_web/controllers/auth/*_test.exs`,
  `test/xpeak_web/controllers/me_controller_test.exs`.

**Mobile side (`apps/mobile/`)**
- `app/(auth)/register/page.tsx`, `login/page.tsx`.
- `app/(app)/profile/page.tsx`.
- `app/auth/callback/page.tsx`.
- `lib/auth/client.ts` (token storage + axios instance).
- `lib/auth/queries.ts` (`useMe`, `useLogin`, `useRegister`,
  `useLogout`).
- Middleware or client-side guard for authenticated routes.
- Specs: `app/**/*.test.tsx` for the auth screens using MSW to mock
  the API.

**Shared (`packages/shared/`)**
- `src/auth/types.ts` — `User`, `AuthResponse` types shared between
  API contract and mobile consumer (single source of truth for the
  DTO shape).

## 6. Success criteria

- A user can register with `username + email + password` and lands
  on the profile screen.
- A user can log in with `identifier` (email or username) +
  `password` and lands on the profile screen.
- A user can log in with Google (OAuth round-trip through the API)
  and lands on the profile screen.
- The profile screen shows `@username`, `level=0`, `xp=0`,
  `streak=0`, and email.
- Logging out revokes the token via GuardianDb and forces a
  redirect to `/login`.
- Any authenticated route hit without a valid token redirects to
  `/login`.
- All happy-path and main-failure tests pass on the API.
- All mobile auth screen specs pass under Vitest.
- CI green on `api-test`, `mobile-test`, `lint`, `build`,
  `commitlint`.

## 7. Risks and trade-offs

| Risk / trade-off                                          | Position                                                                 |
|-----------------------------------------------------------|--------------------------------------------------------------------------|
| Long-lived JWT (30 days) without refresh                  | Accepted for MVP. GuardianDb mitigates leaked tokens via revocation.     |
| No email verification means anyone can register           | Accepted for MVP. Hammer rate-limits signup. Add verification later.     |
| Google OAuth callback needs a valid HTTPS redirect URI    | Dev uses `http://localhost:4000/auth/google/callback`; Google Cloud project must whitelist it. Documented in `spec.md` §5. |
| Storing token in `localStorage` on web is XSS-vulnerable  | Accepted for MVP; web is a fallback anyway. Capacitor Preferences on the actual app. Sanitize all rendered strings. |
| `guardian_db` table grows unbounded                       | Cleanup job (later phase — or a small `Oban` cron in this phase) prunes expired rows. |
| Google account with same email as existing password user  | Auto-link on match: if `email` already exists, attach `google_uid` to that user and issue JWT. Documented in `spec.md` §4.5. |
| Hammer ETS backend loses counters on node restart         | Acceptable for MVP single-node; migrate to a shared backend when we scale horizontally. |

## 8. Decisions (locked) and remaining open questions

**Locked:**
- ✅ Register payload: **username + email + password** only. No
  `name` field.
- ✅ Password minimum length: **8 characters**.
- ✅ Rate limiting: **yes**, via Hammer on `/auth/register` and
  `/auth/login`.
- ✅ Google Cloud project: personal account (owned by @Felpasw).

**Still open:**
1. **Mobile OAuth callback strategy** — how the Phoenix callback
   returns control to the Capacitor app:
   - **Option A — Deep link** (recommended). Phoenix redirects to
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
     first Google callback for a new user (extra hop, more control).
3. **Structured logging** — introduce `logger_json` (or `ex_json_logger`)
   now, or defer to a later observability phase?

## 9. Dependencies

- Requires Phase 1 (release workflow) and Phase 2 (Phoenix + Next
  skeleton, `packages/shared`) — both already mapped.
- Blocks Phase 4 onwards (no user, no XP owner).

## 10. Next step

Answer the questions in §8, then finalize `tasks.md` and open the
first working branch for Section A of that file.
