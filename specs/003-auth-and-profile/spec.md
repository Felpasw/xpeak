# Phase 3 — Auth & Basic Profile (SPEC)

> Companion to `plan.md` (why) and `tasks.md` (atomic steps).

## 1. Summary

Deliver the minimum identity layer: email+password and Google OAuth
auth, JWT-based session (Guardian), one profile screen. No
progression logic.

## 2. Data model

### 2.1. `users` table

| Column                  | Type      | Constraints                          |
|-------------------------|-----------|--------------------------------------|
| `id`                    | uuid      | PK, default `gen_random_uuid()`      |
| `username`              | citext    | not null, unique (case-insensitive)  |
| `email`                 | citext    | not null, unique (case-insensitive)  |
| `hashed_password`       | string    | nullable (Google-only users may have no local password until they set one) |
| `google_uid`            | string    | nullable, unique when present        |
| `avatar_url`            | string    | nullable                             |
| `level`                 | integer   | not null, default `0`                |
| `xp`                    | integer   | not null, default `0`                |
| `current_streak_days`   | integer   | not null, default `0`                |
| `longest_streak_days`   | integer   | not null, default `0`                |
| `inserted_at`           | utc_datetime_usec | not null                     |
| `updated_at`            | utc_datetime_usec | not null                     |

Indexes: `username` (unique), `email` (unique), `google_uid` (unique
where not null).

Enable `pgcrypto` and `citext` extensions in a preceding migration
(case-insensitive uniqueness handled by the column type instead of
app-level lowercasing).

**Username rules:**
- Length: 3–20 characters.
- Allowed chars: `[a-z0-9_]` (lowercase enforced; ASCII only for MVP).
- Regex: `~r/^[a-z0-9_]{3,20}$/`.
- Reserved words blocked (e.g., `admin`, `me`, `xpeak`, `auth`,
  `api`) via `Xpeak.Accounts.ReservedUsernames.reserved?/1` used in
  the changeset validation. Full list lives in that module.

**No `name` field** — the display identity is the `username`.
Additional profile fields (display name, bio, etc.) can be added
later without changing the auth contract.

### 2.2. `guardian_db_tokens` table

Managed by the `guardian_db` migration generator:

| Column      | Type              | Notes                        |
|-------------|-------------------|------------------------------|
| `jti`       | string, unique    | JWT ID                       |
| `aud`       | string            | audience                     |
| `typ`       | string            | token type (`access`, etc.)  |
| `iss`       | string            | issuer                       |
| `sub`       | string            | subject (user id)            |
| `exp`       | bigint            | expiration epoch             |
| `jwt`       | text              | full token (searchable)      |
| `claims`    | map (jsonb)       | full claims                  |
| `inserted_at` / `updated_at` | utc_datetime_usec |             |

A token being **present** in this table means it's active. Logout
deletes the row (revocation).

## 3. Guardian & Ueberauth configuration

### 3.1. Guardian

`config/config.exs`:

```elixir
config :xpeak, Xpeak.Auth.Guardian,
  issuer: "xpeak",
  ttl: {30, :days},
  verify_module: Guardian.JWT,
  secret_key: {:system, "GUARDIAN_SECRET_KEY"}

config :guardian, Guardian.DB,
  repo: Xpeak.Repo,
  schema_name: "guardian_db_tokens",
  sweep_interval: 60
```

`lib/xpeak/auth/guardian.ex`:

```elixir
defmodule Xpeak.Auth.Guardian do
  use Guardian, otp_app: :xpeak

  alias Xpeak.Accounts

  def subject_for_token(%{id: id}, _claims), do: {:ok, to_string(id)}
  def subject_for_token(_, _), do: {:error, :invalid_resource}

  def resource_from_claims(%{"sub" => id}) do
    case Accounts.get_user(id) do
      nil -> {:error, :not_found}
      user -> {:ok, user}
    end
  end
end
```

Password length is enforced at the changeset level: 8..128.

### 3.2. Ueberauth (Google)

`config/config.exs`:

```elixir
config :ueberauth, Ueberauth,
  providers: [
    google: {Ueberauth.Strategy.Google, [default_scope: "email profile"]}
  ]

config :ueberauth, Ueberauth.Strategy.Google.OAuth,
  client_id: {:system, "GOOGLE_CLIENT_ID"},
  client_secret: {:system, "GOOGLE_CLIENT_SECRET"},
  redirect_uri: {:system, "GOOGLE_REDIRECT_URI"}
```

### 3.3. Hammer (rate limit)

`config/config.exs`:

```elixir
config :hammer,
  backend: {Hammer.Backend.ETS,
    [expiry_ms: 60_000 * 60 * 4, cleanup_interval_ms: 60_000 * 10]}
```

## 4. Endpoints

All under `/auth/*` unless stated. JSON in / JSON out.

### 4.1. `POST /auth/register`

Request:
```json
{ "username": "felpa", "email": "u@x.com", "password": "secret12" }
```

Response `201`:
```json
{
  "user": {
    "id": "uuid",
    "username": "felpa",
    "email": "u@x.com",
    "avatar_url": null,
    "level": 0, "xp": 0,
    "current_streak_days": 0, "longest_streak_days": 0,
    "inserted_at": "2026-01-01T00:00:00.000000Z"
  }
}
```
Header: `authorization: Bearer <jwt>`.

Failures:
- `422` — validation error. Possible causes:
  - Missing/invalid email; email already taken.
  - Missing/short (< 8) password.
  - Missing username; username shorter than 3 or longer than 20;
    invalid characters; reserved word; already taken.
- `429` — rate limit exceeded (see §4.6).

### 4.2. `POST /auth/login`

Request (either email or username as the identifier):
```json
{ "identifier": "u@x.com", "password": "secret12" }
```
or
```json
{ "identifier": "felpa", "password": "secret12" }
```

The controller resolves `identifier` as email if it contains `@`,
otherwise as username, then delegates to
`Xpeak.Accounts.authenticate_by_identifier/2`.

Response `200`: same body shape as `4.1`, JWT in `authorization`
header.

Failures:
- `401` — invalid credentials.
- `422` — malformed request body.
- `429` — rate limit exceeded (see §4.6).

### 4.3. `DELETE /auth/logout`

Header: `authorization: Bearer <jwt>` (required).

Response `204`. The JWT's DB row is deleted (via
`Guardian.revoke/1`); subsequent requests with the same token
return `401`.

### 4.4. `GET /me`

Header: `authorization: Bearer <jwt>` (required).

Response `200`: `{ "user": { ...same shape... } }`.

Failure: `401` if missing/invalid/revoked token.

### 4.5. Google OAuth

- `GET /auth/google` — Ueberauth pipeline redirects to Google.
- `GET /auth/google/callback` — Google returns; controller receives
  `%Ueberauth.Auth{}` in `conn.assigns.ueberauth_auth` and:
  - If `google_uid` matches an existing user → log in.
  - Else if `email` matches an existing user → link
    (`user.google_uid = auth.uid`) and log in.
  - Else create new user:
    - `email` from Google.
    - `avatar_url` from Google profile picture.
    - `hashed_password` left `nil` (Google is the auth).
    - `username`: auto-generated from the email local-part, sanitized
      to `[a-z0-9_]{3,20}` and collision-suffixed (see username
      strategy below). Decision pending in `plan.md` §8 Q2 — the
      alternative is a "choose username" screen after callback.
- Response strategy for mobile:
  - Success: redirect to `xpeak://auth/callback?token=<jwt>` (deep
    link — see `plan.md` §8 Q1).
  - Failure: redirect to `xpeak://auth/callback?error=<code>`.
- Response strategy for web (Next.js dev):
  - Success: redirect to
    `http://localhost:3001/auth/callback?token=<jwt>`.

**Auto-generated username strategy (if Option A in `plan.md` §8 Q2):**
- Start from the email local-part.
- Lowercase, strip anything not in `[a-z0-9_]`, truncate to 20 chars.
- If shorter than 3 chars after cleanup, pad with `_user`.
- If reserved or already taken, append `_2`, `_3`, ... until free.
- Example: `Felipe.Souza@x.com` → `felipesouza`; second collision →
  `felipesouza_2`.

Implemented as `Xpeak.Accounts.UsernameGenerator.generate/2`.

### 4.6. Rate limiting

Hammer throttles on:
- `POST /auth/register`: **3 requests per IP per hour**.
- `POST /auth/login`: **10 requests per IP per 5 minutes**;
  additionally **5 failed logins per identifier per 15 minutes**
  (identifier hashed for logging).

Response on throttle: `429 Too Many Requests`, header
`retry-after: <seconds>`.

Implemented as `XpeakWeb.Plugs.RateLimit` mounted per-route in the
router.

## 5. Environment variables

Additions to `apps/api/.env.example`:

```
GOOGLE_CLIENT_ID=
GOOGLE_CLIENT_SECRET=
GOOGLE_REDIRECT_URI=http://localhost:4000/auth/google/callback
GUARDIAN_SECRET_KEY=
```

Notes for local dev:
- Generate `GUARDIAN_SECRET_KEY` with `mix guardian.gen.secret` and
  paste into `.env` (never commit).
- The Google Cloud project must whitelist
  `http://localhost:4000/auth/google/callback` as an authorized
  redirect URI.

Additions to `apps/mobile/.env.example`:

```
NEXT_PUBLIC_API_URL=http://localhost:4000
NEXT_PUBLIC_AUTH_DEEP_LINK_SCHEME=xpeak
```

## 6. Mobile screens

### 6.1. `/register`
- Fields: **username**, **email**, **password** (3 fields, no name).
- Client-side validation:
  - Username: matches `/^[a-z0-9_]{3,20}$/`; live-normalize to
    lowercase as the user types.
  - Email: standard email regex.
  - Password: at least 8 chars.
- Submit → `POST /auth/register` → store token → redirect to
  `/profile`.
- Handles server-side `422` per field (username taken/reserved,
  email taken).
- "Log in with Google" button opens
  `NEXT_PUBLIC_API_URL/auth/google` via `@capacitor/browser` (or
  `window.location` on web).

### 6.2. `/login`
- Fields: **identifier** (email or username), **password**.
- Placeholder text: "email or username".
- Submit → `POST /auth/login` → store token → redirect to
  `/profile`.
- "Log in with Google" button same as above.
- Link "Don't have an account? Register".

### 6.3. `/auth/callback`
- Reads `token` (or `error`) from URL query.
- If `token`: persists it via `authClient.setToken(token)`, then
  redirects to `/profile`.
- If `error`: shows error and links back to `/login`.

### 6.4. `/profile`
- Requires auth (redirects to `/login` if no token).
- Shows: `@username`, email, avatar (default placeholder or Google
  picture), level, xp, current streak, longest streak.
- Button: **Logout** → `DELETE /auth/logout` → clears token →
  redirects to `/login`.

### 6.5. Guard behavior

- `useMe()` from `lib/auth/queries.ts` returns
  `{ user, isLoading, error }`.
- Any page under `app/(app)/*` runs a client-side effect: if
  `!isLoading && !user` → `router.replace('/login')`.
- On any request receiving `401`: axios interceptor calls
  `authClient.clearToken()` and triggers the same redirect.

## 7. Shared types (`packages/shared/`)

`packages/shared/src/auth/types.ts`:

```ts
export interface User {
  id: string;
  username: string;
  email: string;
  avatar_url: string | null;
  level: number;
  xp: number;
  current_streak_days: number;
  longest_streak_days: number;
  inserted_at: string;
}

export interface AuthResponse {
  user: User;
}
```

Re-exported from `packages/shared/src/index.ts`.

Phoenix contract is authoritative; the type mirrors the JSON shape
exactly. When they drift, controller tests on the api side catch it.

## 8. Test plan

### 8.1. API (ExUnit)

`test/xpeak/accounts_test.exs`:
- Validations:
  - `username` presence + format `~r/^[a-z0-9_]{3,20}$/` +
    uniqueness (case-insensitive) + reserved-word rejection.
  - `email` presence + format + uniqueness (case-insensitive).
  - `password` length ≥ 8; `hashed_password` populated after
    changeset apply.
- Defaults: `level`, `xp`, `current_streak_days`,
  `longest_streak_days` all default to `0` on insert.
- `authenticate_by_identifier/2` resolves email vs. username and
  returns `{:ok, user}` on match, `{:error, :invalid_credentials}`
  otherwise.

`test/xpeak/accounts/username_generator_test.exs`:
- Sanitization: strip diacritics/uppercase/special chars.
- Collision suffix: appends `_2`, `_3`, ... until unique.
- Reserved-word skip: `admin@x.com` → `admin_2` (skips `admin`).
- Padding: too-short local-part gets `_user` appended.

`test/xpeak_web/controllers/auth/registration_controller_test.exs`:
- Happy: valid payload (`username + email + password`) → 201 +
  `authorization` header + correct body.
- Sad:
  - missing/invalid username → 422.
  - reserved username (`admin`, `me`, etc.) → 422.
  - taken username → 422.
  - taken email → 422.
  - weak password (< 8) → 422.
- Rate limit: 4th register attempt within an hour from the same IP
  → 429 with `retry-after` header.

`test/xpeak_web/controllers/auth/session_controller_test.exs`:
- Happy — identifier as email → 200 + JWT header.
- Happy — identifier as username → 200 + JWT header.
- Sad: wrong password → 401; unknown identifier → 401.
- Rate limit: 6th failed login for the same identifier within 15
  minutes → 429.
- Logout: token becomes invalid after `DELETE /auth/logout`
  (subsequent `GET /me` → 401).

`test/xpeak_web/controllers/me_controller_test.exs`:
- With valid token → 200 + correct body.
- Without token → 401.
- With revoked token → 401.

`test/xpeak_web/controllers/auth/google_controller_test.exs`:
- Uses `Ueberauth.Strategy.Test` (or a stubbed
  `conn.assigns.ueberauth_auth`).
- New user path: creates user with auto-generated username, issues
  JWT, redirects with token.
- Existing user by google_uid: no new user created.
- Existing user by email (no google_uid yet): links google_uid,
  issues JWT.
- Failure path (`conn.assigns.ueberauth_failure`) → redirect with
  `error`.

### 8.2. Mobile (Vitest + Testing Library)

`app/(auth)/register/register.test.tsx`:
- Renders 3 fields (username, email, password); disables submit while
  invalid; submit calls API mock; on success stores token and
  navigates to `/profile`.
- Live-normalizes the username input to lowercase.
- Shows per-field error on API 422 (`username taken`, `email taken`,
  etc.).

`app/(auth)/login/login.test.tsx`:
- Renders `identifier` + `password`; accepts either email or username
  input; covers 401 error surface.

`app/auth/callback/callback.test.tsx`:
- With `?token=...` → stores token, navigates to `/profile`.
- With `?error=...` → shows error message.

`app/(app)/profile/profile.test.tsx`:
- With `useMe` returning a user → renders
  `@username`/email/level/xp/streak.
- With `useMe` returning null → triggers `router.replace('/login')`.
- Logout button clears token and navigates.

MSW (`msw` handlers under `lib/mocks/`) mocks the API for these
specs.

## 9. Success criteria (measurable)

- All tests above green.
- `curl` walkthrough:
  1. `POST /auth/register` → 201, capture JWT from `authorization`
     header.
  2. `GET /me` with JWT → 200 with zeroed stats.
  3. `DELETE /auth/logout` → 204.
  4. `GET /me` with same JWT → 401.
- Manual browser walkthrough of Google login on `localhost:3001`
  ends on `/profile` with the auto-generated `@username` and the
  Google account's email/avatar.
- CI green on the phase PR.
- release-please PR bumps to `0.2.0` (first `feat:` after `0.1.0`
  from Phase 2).

## 10. Explicit non-goals

- Password reset flow.
- Email confirmation flow.
- Any UI polish beyond a functional form.
- Avatar upload (Phase 10).
- Any XP/level/streak mutation (Phase 4+).
