# Phase 3 — Auth & Basic Profile (SPEC)

> Companion to `plan.md` (why) and `tasks.md` (atomic steps).

## 1. Summary

Deliver the minimum identity layer: email+password and Google OAuth
auth, JWT-based session (with server-side revocation), one profile
screen. No progression logic.

## 2. Data model

### 2.1. `Users` table (via ASP.NET Identity)

ASP.NET Identity generates the `AspNetUsers` schema (renamed to
`users` via `ToTable("users")` in the `AppDbContext`
configuration). We extend it with:

| Column                  | Type      | Constraints                          |
|-------------------------|-----------|--------------------------------------|
| `id`                    | uuid      | PK (Identity default with `Guid`)    |
| `user_name`             | citext    | Identity: normalized to uppercase in `normalized_user_name`; we lowercase for display. **Unique** via `normalized_user_name`. |
| `email`                 | citext    | Unique via `normalized_email`.       |
| `password_hash`         | text      | Nullable (Google-only users may have no local password until they set one). |
| `google_uid`            | text      | Nullable, unique when present.       |
| `avatar_url`            | text      | Nullable.                            |
| `level`                 | integer   | Not null, default `0`.               |
| `xp`                    | integer   | Not null, default `0`.               |
| `current_streak_days`   | integer   | Not null, default `0`.               |
| `longest_streak_days`   | integer   | Not null, default `0`.               |
| `security_stamp`        | text      | Identity default.                    |
| `concurrency_stamp`     | text      | Identity default.                    |

Indexes: Identity-provided uniqueness on `normalized_user_name`
and `normalized_email`; add unique filtered index on `google_uid
where google_uid is not null`.

Enable Postgres `citext` extension for case-insensitive text; use
it for `user_name` and `email` for parity with Identity's
normalization.

**Username rules:**
- Length: 3–20 characters.
- Allowed chars: `[a-z0-9_]` (lowercase enforced; ASCII only for
  MVP).
- Regex: `^[a-z0-9_]{3,20}$` (validated in
  `AppUserValidator : IUserValidator<AppUser>`).
- Reserved words blocked (e.g., `admin`, `me`, `xpeak`, `auth`,
  `api`) via `ReservedUsernames.IsReserved(string)` used in the
  validator.

**No `Name` field** — the display identity is the `Username`.
Additional profile fields (display name, bio, etc.) can be added
later without changing the auth contract.

### 2.2. `RevokedTokens` table

| Column        | Type              | Constraints             |
|---------------|-------------------|-------------------------|
| `jti`         | text              | PK                      |
| `user_id`     | uuid              | FK → users.id           |
| `expires_at`  | timestamptz       | Not null                |
| `revoked_at`  | timestamptz       | Not null, default now() |

Index: `expires_at` for the sweeper.

A JWT is considered active if its `jti` is **not** in this table.
Logout inserts a row; `RevokedTokenSweeper` (`BackgroundService`)
deletes rows past `expires_at` daily.

## 3. Configuration

### 3.1. Identity setup (`Program.cs`)

```csharp
builder.Services
    .AddIdentityCore<AppUser>(options =>
    {
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 8;
        options.User.RequireUniqueEmail = true;
        options.User.AllowedUserNameCharacters =
            "abcdefghijklmnopqrstuvwxyz0123456789_";
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddUserValidator<AppUserValidator>()
    .AddSignInManager()
    .AddDefaultTokenProviders();
```

### 3.2. JWT bearer

```csharp
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "xpeak",
            ValidAudience = "xpeak",
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async ctx =>
            {
                var jti = ctx.Principal!.FindFirstValue(JwtRegisteredClaimNames.Jti);
                var store = ctx.HttpContext.RequestServices
                    .GetRequiredService<RevokedTokenStore>();
                if (jti is not null && await store.IsRevokedAsync(jti))
                    ctx.Fail("Token revoked.");
            }
        };
    });
```

### 3.3. Google OAuth (+ cookie bridge)

```csharp
builder.Services
    .AddAuthentication()  // add extra schemes to the existing setup
    .AddCookie("GoogleTempCookie", options =>
    {
        options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
        options.Cookie.Name = ".Xpeak.GoogleTemp";
        options.Cookie.SameSite = SameSiteMode.Lax;
    })
    .AddGoogle("Google", options =>
    {
        options.ClientId = builder.Configuration["Google:ClientId"]!;
        options.ClientSecret = builder.Configuration["Google:ClientSecret"]!;
        options.CallbackPath = "/signin-google";
        options.SignInScheme = "GoogleTempCookie";
        options.SaveTokens = true;
    });
```

### 3.4. Rate limiting

```csharp
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (ctx, ct) =>
    {
        ctx.HttpContext.Response.Headers.RetryAfter = "60";
        await ctx.HttpContext.Response.WriteAsync("rate_limited", ct);
    };

    options.AddPolicy("auth-register", ctx =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 3,
                Window = TimeSpan.FromHours(1)
            }));

    options.AddPolicy("auth-login-ip", ctx =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(5)
            }));
});
```

A second dimension (5 failed logins per identifier per 15 min) is
implemented as a custom `FailedLoginCounter` service backed by
`MemoryCache` (per-node MVP; Redis when scaling).

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
    "avatarUrl": null,
    "level": 0, "xp": 0,
    "currentStreakDays": 0, "longestStreakDays": 0,
    "createdAt": "2026-01-01T00:00:00Z"
  }
}
```
Header: `Authorization: Bearer <jwt>`.

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

The endpoint resolves `identifier` as email if it contains `@`,
otherwise as username, then calls
`SignInManager.CheckPasswordSignInAsync`. On success, issues a JWT
via `JwtTokenIssuer`.

Response `200`: same body shape as `4.1`, JWT in `Authorization`
header.

Failures:
- `401` — invalid credentials.
- `422` — malformed request body.
- `429` — rate limit exceeded (see §4.6).

### 4.3. `POST /auth/logout`

Header: `Authorization: Bearer <jwt>` (required).

Response `204`. The JWT's `jti` claim is inserted into
`RevokedTokens`; subsequent requests with the same token return
`401`.

### 4.4. `GET /me`

Header: `Authorization: Bearer <jwt>` (required, enforced by
`.RequireAuthorization()`).

Response `200`: `{ "user": { ...same shape... } }`.

Failure: `401` if missing/invalid/revoked token.

### 4.5. Google OAuth

- `GET /auth/google` — kicks off Google flow:
  ```csharp
  app.MapGet("/auth/google", (HttpContext ctx) =>
      Results.Challenge(
          new AuthenticationProperties { RedirectUri = "/auth/google/callback" },
          new[] { "Google" }));
  ```
- `GET /auth/google/callback` — receives the Google identity via
  the temp cookie, finds or creates the user, issues our JWT,
  redirects.
  - If `GoogleUid` matches an existing user → log in.
  - Else if `Email` matches an existing user → link
    (`user.GoogleUid = googleUid`) and log in.
  - Else create new user:
    - `Email` from Google.
    - `AvatarUrl` from Google profile picture.
    - `PasswordHash` left `null` (Google is the auth).
    - `UserName`: auto-generated from the email local-part,
      sanitized to `[a-z0-9_]{3,20}` and collision-suffixed (see
      below). Decision pending in `plan.md` §8 Q2 — the
      alternative is a "choose username" screen after callback.
  - Signs out of `GoogleTempCookie`.
  - Redirects to `xpeak://auth/callback?token=<jwt>` (mobile) or
    `http://localhost:3001/auth/callback?token=<jwt>` (web).

**Auto-generated username strategy (if Option A in `plan.md` §8 Q2):**
- Start from the email local-part.
- Lowercase, strip anything not in `[a-z0-9_]`, truncate to 20 chars.
- If shorter than 3 chars after cleanup, pad with `_user`.
- If reserved or already taken, append `_2`, `_3`, ... until free.
- Example: `Felipe.Souza@x.com` → `felipesouza`; second collision →
  `felipesouza_2`.

Implemented as `Xpeak.Api.Domain.User.UsernameGenerator`.

### 4.6. Rate limiting

Applied per-endpoint:

```csharp
app.MapPost("/auth/register", ...).RequireRateLimiting("auth-register");
app.MapPost("/auth/login",    ...).RequireRateLimiting("auth-login-ip");
```

Plus the in-memory `FailedLoginCounter` for the identifier-based
15-min limit.

Response on throttle: `429 Too Many Requests`, header
`Retry-After: <seconds>`.

## 5. Environment variables

Additions to `apps/api/.env.example`:

```
Google__ClientId=
Google__ClientSecret=
Jwt__Key=
```

Notes for local dev:
- Generate `Jwt__Key` with `openssl rand -base64 48` and paste
  into `.env` (never commit).
- The Google Cloud project must whitelist
  `http://localhost:5000/signin-google` and
  `http://localhost:5000/auth/google/callback` as authorized
  redirect URIs.

Additions to `apps/mobile/.env.example`:

```
NEXT_PUBLIC_API_URL=http://localhost:5000
NEXT_PUBLIC_AUTH_DEEP_LINK_SCHEME=xpeak
```

## 6. Mobile screens

### 6.1. `/register`
- Fields: **username**, **email**, **password** (3 fields, no
  name).
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
- Shows: `@username`, email, avatar (default placeholder or
  Google picture), level, xp, current streak, longest streak.
- Button: **Logout** → `POST /auth/logout` → clears token →
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
  avatarUrl: string | null;
  level: number;
  xp: number;
  currentStreakDays: number;
  longestStreakDays: number;
  createdAt: string;
}

export interface AuthResponse {
  user: User;
}
```

Re-exported from `packages/shared/src/index.ts`.

ASP.NET Core defaults to `camelCase` JSON via
`JsonSerializerDefaults.Web` (System.Text.Json). The shared TS
types mirror the JSON exactly.

## 8. Test plan

### 8.1. API (xUnit + `WebApplicationFactory<Program>` + Testcontainers Postgres)

`AppUserValidatorTests`:
- Presence + format `^[a-z0-9_]{3,20}$` + reserved-word rejection.
- Uniqueness (case-insensitive) via full pipeline with real DB.

`UsernameGeneratorTests`:
- Sanitization (strip diacritics/uppercase/special chars).
- Collision suffix (`_2`, `_3`, ...).
- Reserved-word skip (`admin@x.com` → `admin_2`).
- Padding for short local-parts.

`AuthEndpointTests`:
- Register: happy (`username + email + password`) → 201 +
  `Authorization` header + correct body.
- Register sad: missing/invalid username, reserved username,
  taken username/email, weak password.
- Register rate limit: 4th attempt within an hour from the same
  IP → 429 with `Retry-After`.
- Login happy — identifier as email → 200 + JWT header.
- Login happy — identifier as username → 200 + JWT header.
- Login sad: wrong password → 401; unknown identifier → 401.
- Login rate limit: 6th failed login for the same identifier
  within 15 minutes → 429.
- Logout: token becomes invalid after `POST /auth/logout`
  (subsequent `GET /me` → 401).

`MeEndpointTests`:
- With valid token → 200 + correct body.
- Without token → 401.
- With revoked token → 401.

`GoogleOAuthEndpointTests`:
- Uses `TestAuthHandler` to fake Google auth results in
  integration tests.
- New user path: creates user with auto-generated username,
  issues JWT, redirects with token.
- Existing user by google_uid: no new user created.
- Existing user by email (no google_uid yet): links google_uid,
  issues JWT.
- Failure path (Google callback with error) → redirect with
  `?error=...`.

### 8.2. Mobile (Vitest + Testing Library)

`app/(auth)/register/register.test.tsx`:
- Renders 3 fields (username, email, password); disables submit
  while invalid; submit calls API mock; on success stores token
  and navigates to `/profile`.
- Live-normalizes the username input to lowercase.
- Shows per-field error on API 422 (`username taken`, `email
  taken`, etc.).

`app/(auth)/login/login.test.tsx`:
- Renders `identifier` + `password`; accepts either email or
  username input; covers 401 error surface.

`app/auth/callback/callback.test.tsx`:
- With `?token=...` → stores token, navigates to `/profile`.
- With `?error=...` → shows error message.

`app/(app)/profile/profile.test.tsx`:
- With `useMe` returning a user → renders
  `@username`/email/level/xp/streak.
- With `useMe` returning null → triggers
  `router.replace('/login')`.
- Logout button clears token and navigates.

MSW (`msw` handlers under `lib/mocks/`) mocks the API for these
specs.

## 9. Success criteria (measurable)

- All tests above green.
- `curl` walkthrough:
  1. `POST /auth/register` → 201, capture JWT from `Authorization`
     header.
  2. `GET /me` with JWT → 200 with zeroed stats.
  3. `POST /auth/logout` → 204.
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
