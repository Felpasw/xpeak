# Phase 3 — Auth & Basic Profile (TASKS)

> Companion to `spec.md` (what) and `plan.md` (why). Atomic execution
> steps. Nothing implemented yet.

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
    `http://localhost:4000/auth/google/callback`.
  - Paste `GOOGLE_CLIENT_ID` and `GOOGLE_CLIENT_SECRET` into local
    `.env` (not committed).
  - Deliverable: credentials in place locally + noted in
    `.env.example` as blank placeholders.

- [ ] **T-003-02 `[S]`** — Answer remaining open questions
  - Decisions already locked (see `plan.md` §8):
    - Register payload: `username + email + password` (no `name`).
    - Password length: 8+.
    - Rate limiting: yes.
    - Google Cloud: personal account.
  - Still open (`plan.md` §8): Q1 (mobile OAuth callback strategy —
    deep link vs. in-app browser), Q2 (Google username strategy —
    auto-generate vs. choose-username screen), Q3 (structured
    logging).
  - Deliverable: decisions pasted into this task before starting
    Section B.

---

## Section B — Accounts context & User schema

- [ ] **T-003-03 `[S]`** — Add dependencies
  - `bcrypt_elixir`, `guardian`, `guardian_db`, `ueberauth`,
    `ueberauth_google`, `hammer`, `hammer_backend_ets`.
  - `mix deps.get` clean.
  - Deliverable: updated `mix.exs`, `mix.lock`.

- [ ] **T-003-04 `[T][S]`** — `users` migration + `User` schema
  - **Write** `test/xpeak/accounts_test.exs` covering:
    - `username` presence + format + uniqueness (case-insensitive)
      + reserved-word rejection.
    - `email` presence + format + uniqueness (case-insensitive).
    - `password` length ≥ 8; `hashed_password` populated after
      changeset apply.
    - Progression fields defaulting to `0`.
    - Expect red.
  - Migration: enable `pgcrypto` + `citext`; create `users` table
    per `spec.md` §2.1 with `binary_id` PK.
  - `lib/xpeak/accounts.ex` — public API (`get_user/1`,
    `register_user/1`, `authenticate_by_identifier/2`,
    `find_or_create_from_google/1`).
  - `lib/xpeak/accounts/user.ex` — Ecto schema + `registration_changeset/2`,
    `oauth_changeset/2`, `password_changeset/2`.
  - `lib/xpeak/accounts/reserved_usernames.ex` — list module with
    `reserved?/1`.
  - `mix ecto.migrate` + `mix test` → green.
  - Deliverable: migration + accounts module + schema + concern +
    green tests.

- [ ] **T-003-05 `[T][S]`** — `UsernameGenerator`
  - **Write** `test/xpeak/accounts/username_generator_test.exs`
    covering sanitization, collision suffix, reserved-word skip,
    padding for short local-parts. Expect red.
  - Implement `lib/xpeak/accounts/username_generator.ex`.
  - `mix test` → green.
  - Deliverable: module + green tests.

- [ ] **T-003-06 `[S]`** — Guardian + GuardianDb setup
  - `mix guardian.gen.secret` — paste into local `.env` as
    `GUARDIAN_SECRET_KEY`; add placeholder to `.env.example`.
  - `mix guardian_db.gen.migration` — generates migration for
    `guardian_db_tokens` table.
  - `mix ecto.migrate`.
  - `lib/xpeak/auth/guardian.ex` implementing `subject_for_token`
    and `resource_from_claims` per `spec.md` §3.1.
  - `config/config.exs`: Guardian + GuardianDb config per §3.1.
  - Add `Guardian.DB.Token.SweeperServer` to the supervision tree
    (sweeps expired tokens hourly).
  - Deliverable: Guardian module + configs + migration + supervisor
    child.

---

## Section C — Endpoints (email/password)

- [ ] **T-003-07 `[T][S]`** — Register endpoint
  - **Write**
    `test/xpeak_web/controllers/auth/registration_controller_test.exs`
    covering happy (`username + email + password`) + sad paths
    (missing/invalid username, reserved username, taken
    username/email, weak password) per `spec.md` §8.1 — expect red.
  - `lib/xpeak_web/controllers/auth/registration_controller.ex`:
    calls `Accounts.register_user/1`, issues Guardian token via
    `Guardian.encode_and_sign/1` on success, puts token in
    `authorization` header.
  - `lib/xpeak_web/controllers/user_json.ex`: shapes user response
    per `spec.md` §4.1.
  - Route inside `scope "/auth", XpeakWeb.Auth`.
  - `mix test` → green.
  - Deliverable: controller + JSON view + routes + green tests.

- [ ] **T-003-08 `[T][S]`** — Login endpoint (identifier)
  - **Write** `test/xpeak_web/controllers/auth/session_controller_test.exs`
    covering login via email AND login via username, plus wrong
    password / unknown identifier → 401. Expect red.
  - `lib/xpeak_web/controllers/auth/session_controller.ex`:
    - Resolves `identifier` (email if contains `@`, else username).
    - Calls `Accounts.authenticate_by_identifier/2`.
    - Issues Guardian token on success.
  - `mix test` → green.
  - Deliverable: controller + routes + green tests.

- [ ] **T-003-09 `[T][S]`** — Logout endpoint (GuardianDb revocation)
  - Extend session controller test: logout revokes token in
    GuardianDb; second `GET /me` with same token → 401. Expect red.
  - Add `destroy/2` action calling
    `Xpeak.Auth.Guardian.revoke/1` on the current token.
  - `mix test` → green.
  - Deliverable: destroy action + extended tests.

- [ ] **T-003-10 `[T][S]`** — `GET /me` endpoint
  - **Write** `test/xpeak_web/controllers/me_controller_test.exs`
    per `spec.md` §8.1 — expect red.
  - `lib/xpeak_web/plugs/authenticate.ex`: plug using
    `Guardian.Plug.Pipeline` (VerifyHeader + LoadResource +
    EnsureAuthenticated).
  - `lib/xpeak_web/controllers/me_controller.ex` with the plug in
    the pipeline.
  - `mix test` → green.
  - Deliverable: plug + controller + route + green tests.

---

## Section D — Google OAuth

- [ ] **T-003-11 `[T][S]`** — OAuth callback (new user)
  - Configure `Ueberauth.Strategy.Test` for `:test` env in
    `config/test.exs` so we can stub `conn.assigns.ueberauth_auth`.
  - **Write** `test/xpeak_web/controllers/auth/google_controller_test.exs`
    covering the "new user" path — expect red.
  - `lib/xpeak_web/controllers/auth/google_controller.ex`:
    - `request/2` — Ueberauth handles the redirect to Google.
    - `callback/2` — reads `conn.assigns.ueberauth_auth`, calls
      `Accounts.find_or_create_from_google/1`, issues Guardian
      token, redirects to configured callback URL with
      `?token=<jwt>`.
  - `Accounts.find_or_create_from_google/1` uses
    `UsernameGenerator.generate/2` for new users.
  - Route: Ueberauth pipeline in router for `/auth/google` and
    `/auth/google/callback`.
  - `mix test` → green.
  - Deliverable: controller + accounts function + routes + green
    tests.

- [ ] **T-003-12 `[T][S]`** — OAuth callback (existing user by
      google_uid)
  - Extend test: pre-create user with `google_uid` matching stubbed
    UID; callback logs in without creating a new user.
  - Ensure `find_or_create_from_google/1` finds by `google_uid`
    first.
  - `mix test` → green.
  - Deliverable: test + accounts adjustment.

- [ ] **T-003-13 `[T][S]`** — OAuth callback (link by email)
  - Extend test: pre-create user with matching email but no
    `google_uid`; callback attaches `google_uid` and logs in.
  - Ensure `find_or_create_from_google/1` handles the linking
    branch.
  - `mix test` → green.
  - Deliverable: test + accounts adjustment.

- [ ] **T-003-14 `[T][S]`** — OAuth failure path
  - Extend test: stub `conn.assigns.ueberauth_failure`; callback
    redirects with `?error=...`.
  - Implement failure branch in `callback/2`.
  - `mix test` → green.
  - Deliverable: test + branch.

---

## Section E — Rate limiting

- [ ] **T-003-15 `[T][S]`** — Hammer rate-limit plug
  - Add Hammer config per `spec.md` §3.3.
  - **Write** tests asserting the throttles trigger and reset:
    - 4th register attempt within an hour from the same IP → 429.
    - 6th failed login for the same identifier within 15 minutes → 429.
    - Successful login doesn't count toward the failure counter.
  - Expect red.
  - Implement `lib/xpeak_web/plugs/rate_limit.ex` wrapping Hammer;
    mount on the register and login routes with per-route options.
  - Return `429` with `retry-after` header.
  - `mix test` → green.
  - Deliverable: plug + tests + router changes.

---

## Section F — Env vars & repo hygiene

- [ ] **T-003-16 `[P]`** — `.env.example` updates
  - Add `GOOGLE_CLIENT_ID`, `GOOGLE_CLIENT_SECRET`,
    `GOOGLE_REDIRECT_URI`, `GUARDIAN_SECRET_KEY` (blank) to
    `apps/api/.env.example`.
  - Add `NEXT_PUBLIC_AUTH_DEEP_LINK_SCHEME` to
    `apps/mobile/.env.example`.
  - Deliverable: updated `.env.example` files.

- [ ] **T-003-17 `[P]`** — README updates
  - `apps/api/README.md`: Google OAuth setup section (link to Google
    Cloud console, redirect URI, env vars, `mix guardian.gen.secret`).
  - `apps/mobile/README.md`: auth flow overview and callback URL.
  - Deliverable: updated READMEs.

---

## Section G — Shared types

- [ ] **T-003-18 `[S]`** — Shared `User` / `AuthResponse` types
  - Create `packages/shared/src/auth/types.ts` per `spec.md` §7.
  - Re-export from `packages/shared/src/index.ts`.
  - Deliverable: two files.

---

## Section H — Mobile auth client

- [ ] **T-003-19 `[T][S]`** — Token storage abstraction
  - `lib/auth/storage.ts` with `getToken()`, `setToken(t)`,
    `clearToken()`, backed by `@capacitor/preferences` on device and
    `localStorage` on web.
  - Vitest spec: mocks storage backends and asserts round-trip.
  - Deliverable: module + spec.

- [ ] **T-003-20 `[T][S]`** — API client (axios instance)
  - `lib/auth/client.ts` exports an axios instance with
    `Authorization` interceptor + 401 handler that clears token and
    fires a callback.
  - Vitest spec asserts header injection and 401 behavior.
  - Deliverable: module + spec.

- [ ] **T-003-21 `[T][S]`** — Auth queries
  - `lib/auth/queries.ts` exports `useMe`, `useLogin`, `useRegister`,
    `useLogout` (TanStack Query hooks).
  - Vitest specs using MSW: assert each hook posts the right
    payload, stores token on success, invalidates the `useMe` cache
    on login/register/logout.
  - Deliverable: module + specs.

---

## Section I — Mobile screens

- [ ] **T-003-22 `[T][S]`** — Register screen
  - **Write** `app/(auth)/register/register.test.tsx` per `spec.md`
    §8.2: 3 fields (username, email, password), live-lowercase on
    username, per-field 422 errors — expect red.
  - Build the page: 3 fields, client-side validation, submit →
    `useRegister`.
  - Run spec → green.
  - Deliverable: page + spec.

- [ ] **T-003-23 `[T][S]`** — Login screen
  - **Write** login test: `identifier` field accepts email OR
    username, 401 error surface — expect red.
  - Build the page with `useLogin` posting `{ identifier, password }`.
  - Run spec → green.
  - Deliverable: page + spec.

- [ ] **T-003-24 `[T][S]`** — Google button (both screens)
  - Extend both specs: clicking "Log in with Google" opens
    `NEXT_PUBLIC_API_URL/auth/google` via `@capacitor/browser`
    (mocked in the test).
  - Add the button + handler.
  - Run specs → green.
  - Deliverable: shared `GoogleSignInButton` component + specs.

- [ ] **T-003-25 `[T][S]`** — Auth callback page
  - **Write** `app/auth/callback/callback.test.tsx` — expect red.
  - Build the page: reads `token`/`error`, stores or shows error.
  - Run spec → green.
  - Deliverable: page + spec.

- [ ] **T-003-26 `[T][S]`** — Profile screen
  - **Write** `app/(app)/profile/profile.test.tsx` — expect red.
  - Build the profile page: reads `useMe`, renders `@username`,
    email, avatar, zeroed stats, logout button.
  - Run spec → green.
  - Deliverable: page + spec.

- [ ] **T-003-27 `[T][S]`** — Route guard
  - Add a client-side guard (either a shared layout component under
    `app/(app)/layout.tsx` or a hook) that redirects to `/login`
    when `useMe` resolves to `null`.
  - Vitest spec covers the redirect path.
  - Deliverable: guard + spec.

---

## Section J — E2E validation

- [ ] **T-003-28 `[T][S]`** — Local end-to-end walkthrough
  - Run the full stack, follow `spec.md` §9 walkthrough.
  - Register via UI, hit `/me` via curl with captured token, log out,
    confirm 401.
  - Google login walkthrough on browser.
  - Deliverable: walkthrough notes + screenshots pasted into
    `docs/adr/0003-auth-and-profile.md`.

- [ ] **T-003-29 `[P]`** — ADR
  - `docs/adr/0003-auth-and-profile.md` documenting: Guardian + JWT
    with GuardianDb revocation, Ueberauth for Google, no refresh
    token, long-lived token trade-off, deferred items (email
    verification, password reset, Apple sign-in).
  - Deliverable: ADR file.

- [ ] **T-003-30 `[S]`** — Phase close
  - Mark all tasks with `[x]` and commit hashes.
  - Confirm success criteria (`spec.md` §9) fully met.
  - Verify release-please Release PR bumps root to `0.2.0` (and
    fans out to `apps/api/VERSION`, `apps/mobile/package.json`).
  - Deliverable: clean `main`, updated `roadmap.md` (flip Phase 3
    from `📋` to `🛠️`), ready for Phase 4.

---

## Dependencies

```
A (prereqs) ──▶ B (accounts + guardian) ──▶ C (email/password endpoints)
                                        └─▶ D (google oauth)
                                        └─▶ E (rate limit)
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

1. `chore(api): add guardian, ueberauth, hammer deps` — B (T-003-03)
2. `feat(auth): accounts context with user schema and username generator` — B (T-003-04, T-003-05)
3. `feat(auth): guardian setup with guardian_db revocation` — B (T-003-06)
4. `feat(auth): email+password endpoints (register/login/logout/me)` — C (T-003-07 → T-003-10)
5. `feat(auth): google oauth callback with link-by-email` — D (T-003-11 → T-003-14)
6. `feat(auth): rate limit register and login` — E (T-003-15)
7. `chore(auth): env and readme updates` — F (T-003-16, T-003-17)
8. `feat(shared): user and auth response types` — G (T-003-18)
9. `feat(mobile): auth client and queries` — H (T-003-19 → T-003-21)
10. `feat(mobile): register, login, callback, profile screens` — I (T-003-22 → T-003-27)
11. `docs(adr): auth and basic profile` — J (T-003-29)

Each bundle stops at `git add` and waits for explicit approval before
committing.
