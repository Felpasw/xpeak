# XPeak — Consolidated Roadmap

> Single source of truth for phase ordering. Consolidates the initial
> phase list from `README.md` §9 with the additional ideas from
> `ideas.md`. Reflects the current understanding of dependencies and
> priority. Each phase gets its own folder under `specs/NNN-slug/`
> when it's picked up.

## Stack migration note (2026-01-15)

Phases 1–3 were originally mapped against **Elixir + Phoenix**. Mid
Phase 2 the backend stack switched to **C# + ASP.NET Core 9** (Minimal
APIs + EF Core 9 + Hangfire + ASP.NET Identity + JWT bearer). The
switch was purely aesthetic/ergonomic — product goals didn't change.

Phase 1 (versioning) and Phase 2 (app skeleton) plan/spec/tasks are
already updated for the new stack. **Phase 3 (auth) is updated too.**
**Phase 4 (categories & pure XP domain) has been rewritten for the C#
stack under `specs/004-categories-and-xp-domain/`.**
Phases 5–25 still reference the old stack in their plan/spec/tasks:

| Old (Elixir/Phoenix)                       | New (C# / ASP.NET Core)                                     |
|--------------------------------------------|-------------------------------------------------------------|
| `Ecto` schemas, migrations, `Repo`         | EF Core `DbContext` + `dotnet ef migrations`                |
| `Oban` (background jobs)                   | `Hangfire` (Postgres storage, dashboard included)           |
| `Guardian` (JWT)                           | `Microsoft.AspNetCore.Authentication.JwtBearer` + custom issuer |
| `GuardianDb` (revocation)                  | `RevokedTokens` table + `IJwtValidator` event               |
| `Ueberauth` + `ueberauth_google`           | `Microsoft.AspNetCore.Authentication.Google` + cookie bridge |
| `Corsica`                                  | `Microsoft.AspNetCore.Cors` (built-in)                      |
| `Hammer`                                   | `Microsoft.AspNetCore.RateLimiting` (built-in .NET 7+)      |
| `Cachex`/`ETS`                             | `IMemoryCache` (built-in) or `StackExchange.Redis`          |
| `ExUnit` + `ExMachina` + `Mox`             | `xUnit` + `AutoFixture`/`Bogus` + `Moq`/`NSubstitute`       |
| `mix.exs` deps                             | `.csproj` + `packages.lock.json`                            |
| `IEx`                                      | `dotnet run` / integration REPL (`csharprepl`)              |
| `Xpeak.Module.function/1` naming           | `Xpeak.Api.Module.MethodName` (PascalCase)                  |
| `defmodule`/`def`/`|>` syntax              | `public class`/`public method`/method chains                |
| `Ecto.Multi`                               | `DbContext.SaveChangesAsync` in a transaction (or MediatR handler) |

**Concepts, data shapes, endpoint contracts, and phase ordering all
remain valid.** When a phase is picked up, we rewrite its
`plan.md`/`spec.md`/`tasks.md` against the C# stack (using this
table + the templates from Phase 2 and Phase 3) as part of the phase's
first PR.

## Legend

- ✅ = phase mapped (plan/spec/tasks written under `specs/NNN-*/`)
- 🛠️ = phase implemented (all tasks marked done)
- 📋 = phase not started yet (this file is the only reference)

## Blocks and phases

### Block A — Foundation

- 🛠️ **Phase 1 — Code versioning & release automation**
  `specs/001-versioning/` — shipped via PR #1 (commit `f999153`).
  - release-please, Conventional Commits, CI (lint/test/build),
    branch protection, PR template, ADR.

- ✅ **Phase 2 — Application skeleton**
  `specs/002-app-bootstrap/` — implementation done in XPK-3
  (`431b2bb`, `42e1a25`, `47543b3`, `8801969`); PR pending merge.
  Flip to 🛠️ once merged.
  - ASP.NET Core 9 Minimal API + Postgres (EF Core), Next.js 15
    + Capacitor, docker-compose, health endpoint, first tests on
    both sides.

### Block B — User core and progression engine

- 📋 **Phase 3 — Auth & basic profile** (next)
  - Email + password, Google OAuth (`omniauth-google-oauth2`), JWT.
  - Basic profile: username, email, avatar (default), level=0, xp=0,
    streak=0.
  - Mobile screens: register, login (email + Google), profile.

- 📋 **Phase 4 — Categories & pure XP domain**
  - Category CRUD (system + custom later).
  - Pure domain services: `XpCalculator`, `LevelUpService` — no UI.
  - 100% test coverage on the domain layer.

- 📋 **Phase 5 — Check-in (no media)**
  - `POST /check_ins` + minimal mobile screen.
  - Wires the XP engine end-to-end for the first time.

- 📋 **Phase 6 — Check-in media**
  - Multiple photo/video uploads per check-in.
  - **Cloudinary** as the storage/CDN backend (`CloudinaryDotNet`
    SDK server-side; direct client upload via signed preset).
    Transformations (thumbnails, format conversion, quality
    optimization) happen on the URL, not on save.
  - Background upload on the mobile side (offline queue arrives in
    Phase 21).

- 📋 **Phase 7 — Streak & category multipliers**
  - Streak tracking (current, longest).
  - Composed multiplier: streak × category × challenge (challenge
    factor is 1.0 until Phase 12).
  - Multiplier snapshot persisted on each check-in for auditability.

### Block C — Themed progression & configuration surface

- 📋 **Phase 8 — Themed titles & themes**
  - `LevelTitleTier` and `Theme` models.
  - Ships with a default theme (Medieval) + one alternative
    (Sci-Fi) as a data seed.
  - User can pick their active theme.

- 📋 **Phase 9 — Editable XP & level configuration** (from
  `ideas.md` §1 and §10)
  - All XP values, level curve, streak-tier multipliers move from
    code to persisted configuration.
  - Minimal admin surface: custom Razor Pages or Blazor admin
    served by the API (built in-house) + audit log. No off-the-shelf
    admin CRUD in this stack — matches the "own interface" decision.
  - Per-scope caps enforced at the domain layer to guard against
    exploit.
  - Includes personalizable XP per user/challenge (bounded).

### Block D — Cosmetics catalog & first achievements

- 📋 **Phase 10 — Profile customization: avatar, banner, frame**
  (from `ideas.md` §5)
  - Storage for cosmetic assets (static + animated per §8 policy).
  - Profile page shows avatar with frame + banner behind.
  - Unlock system stub (everything unlocked for MVP; real unlock
    rules land in later achievements phases).

- 📋 **Phase 11 — Medals for streaks** (from `ideas.md` §4)
  - Threshold-driven: 7-day, 30-day, 100-day, 365-day.
  - Displayed on profile.
  - Default artwork ships with the app; customization comes later.

### Block E — Social graph, timeline & discovery

- 📋 **Phase 12 — Friends & social graph** (from `ideas.md` §2.5)
  - Friendship model (mutual friends by default; profile privacy
    setting decides if requests are required — decision pending in
    `ideas.md` §2.5.1).
  - Add-by-`@username`, accept/reject requests, remove friend,
    block user.
  - Read-only view of a friend's profile (avatar, stats, trophy
    case).
  - Privacy settings: who can request, who can see check-ins,
    who can see trophies/medals.

- 📋 **Phase 13 — Activity timeline / feed** (from `ideas.md`
  §2.5.3)
  - `activity_events` table + emitters wired into check-in, level-up,
    streak-milestone, insignia, trophy services.
  - Personal feed (own events) + friends' feed (fan-out on read to
    start; upgrade path to fan-out on write when volume warrants).
  - Feed item rendering per event kind on mobile.
  - Notification records for social events (push delivery lives in
    Phase 23).

- 📋 **Phase 14 — Global rankings & discovery** (from `ideas.md`
  §2.6)
  - Multiple leaderboards: all-time XP, weekly, monthly, longest
    current/ever streak, per-category.
  - Default opt-in on every new user; privacy toggle to hide.
  - Ranking rows link into the friend/public profile view from
    Phase 12; "Add friend" inline action when applicable.
  - Storage strategy: snapshot job into `rankings_snapshots` +
    `IMemoryCache` (or Redis when we add it) cache for top-N.

### Block F — Challenges and their achievements

- 📋 **Phase 15 — Solo challenges**
  - `Challenge`, `ChallengeMembership` (single member).
  - Personal leaderboard (history of solo runs).
  - Challenge XP bonus multiplier feeds into the engine from
    Phase 7.

- 📋 **Phase 16 — Friend challenges** (from `ideas.md` §2)
  - Lightweight 1v1 or small-group challenges — faster to create
    than a full public/private challenge.
  - Uses the friendship graph from Phase 12 as the invitation source.

- 📋 **Phase 17 — Group challenges (full)** (from `ideas.md`
  §2.7 for visibility rules)
  - **Public** (browseable via search in the challenges tab) vs.
    **private** (share-link only, unguessable signed code).
  - Invitation code/link mechanics, member cap, group feed,
    collective leaderboard.
  - Discovery endpoint `GET /challenges?...` with filters.

- 📋 **Phase 18 — Insignias for challenge completion** (from
  `ideas.md` §4)
  - Awarded at challenge end based on rank/participation.
  - Displayable on profile; emits an event on the timeline.

- 📋 **Phase 19 — Group bonuses & group streak** (from `ideas.md`
  §2)
  - Detect co-located check-ins within a time window → group
    multiplier.
  - Collective streak state per group.

- 📋 **Phase 20 — Trophies (customizable artwork)** (from
  `ideas.md` §4 and §6)
  - Distinct from insignias: reserved for #1 or hardcore
    completion.
  - Challenge owner can upload custom trophy asset (image/GIF).
  - Curated catalog available as middle ground.

### Block G — Personal metrics & retrospectives

- 📋 **Phase 21 — Metrics dashboard & yearly recap** (from
  `ideas.md` §2.8)
  - Always-on dashboard: totals, category breakdown, streak
    calendar (heatmap), multiplier history, progression to next
    level.
  - "Year in XPeak" story-style recap generated by a Hangfire job
    after year rollover (or on anniversary — decision pending in
    `ideas.md` §2.8.3).
  - Recap payload cached in `recaps` table; endpoint returns
    payload or 202 while generating.
  - Shareable cards (per-stat + final summary image).

### Block H — Visual depth & exploration

- 📋 **Phase 22 — Level "evolution forms"** (from `ideas.md` §7)
  - Illustrated form per title tier, themed asset packs.
  - Animated forms unlock at higher tiers.
  - Optional user-uploaded form pack (moderation required — feature
    flag if we ship it).

- 📋 **Phase 23 — Exploration / locations** (from `ideas.md` §3)
  - Gym database source (question in `ideas.md` §3).
  - Location-tied check-ins and challenges.
  - Regional leaderboards feed back into Phase 14 rankings
    (regional scope).

### Block I — Communication & polish

- 📋 **Phase 24 — Push notifications**
  - Streak at risk, group activity, new title unlocked, challenge
    ending soon, friend requests / feed reactions, recap ready.
  - FCM + APNs via Capacitor (or OneSignal — decision in
    `README.md` §10).

- 📋 **Phase 25 — Mobile polish & PWA fallback**
  - Offline check-in queue.
  - Media optimization (client-side compression, resumable upload).
  - PWA manifest, service worker.
  - Performance pass (feed < 1s on 4G).

## Where each `ideas.md` bullet lands

| `ideas.md` section                          | Landing phase(s)          |
|---------------------------------------------|---------------------------|
| §1 XP/progression customization             | 4 (base), 7 (multipliers), 9 (editable config) |
| §2 Group workout bonus                      | 19                        |
| §2 Friend challenges                        | 16                        |
| §2 Group streak                             | 19                        |
| §2.5 Social graph (friends)                 | 12                        |
| §2.5.3 Activity timeline / feed             | 13                        |
| §2.5.4 Privacy & blocking                   | 12                        |
| §2.6 Global rankings & discovery            | 14                        |
| §2.7 Challenge visibility (public/private)  | 15 (solo), 17 (group)     |
| §3 Exploration / locations                  | 22                        |
| §4 Medals (streaks)                         | 11                        |
| §4 Insignias (challenge completion)         | 18                        |
| §4 Trophies (customizable)                  | 20                        |
| §5 Profile customization                    | 10                        |
| §6 Challenge customization                  | 15 (base), 17 (group), 20 (trophy asset) |
| §7 Level "evolution forms"                  | 22                        |
| §8 Media format policy (cross-cutting)      | 6 (check-in), 10, 20, 22  |
| §9 Workout logs media recap                 | 5, 6                      |
| §10 "Everything is data"                    | 9 (surface); enforced from 4 onwards |
| §2.8 Metrics dashboard & yearly recap       | 21                        |

## Cross-cutting concerns (touched in multiple phases)

- **Media handling** — introduced in Phase 6, extended in every
  cosmetics phase (10, 17, 18).
- **Testing (TDD)** — mandatory across all phases per global rule.
- **i18n** — architecture ready from the start, translations added
  as UI grows (not a dedicated phase).
- **Accessibility** — checklist item on every UI phase, not a
  phase itself.
- **Analytics & telemetry** — not scheduled yet; add as its own
  phase once we have real users to measure.

## Release milestones (proposed)

Rough cuts of what a "user-visible" release looks like along the way:

- **v0.1** — Fundação (Block A). Nothing user-visible; internal
  release only.
- **v0.2 — MVP loop** — Blocks A + B. First "checkin → xp → level"
  loop end-to-end.
- **v0.3 — Themed & customizable** — Blocks A + B + C.
- **v0.4 — Cosmetics & first achievements** — + Block D.
- **v0.5 — Social layer & discovery** — + Block E (friends + feed +
  global rankings). This is when XPeak stops being a personal tracker.
- **v0.6 — Challenges** — + Block F, Phases 15–17.
- **v0.7 — Group depth + trophies** — + rest of Block F.
- **v0.8 — Metrics & retrospectives** — + Block G. First
  end-of-year recap ready.
- **v0.9 — Visual depth & exploration** — + Block H.
- **v1.0 — Polish** — + Block I.

## Editing this file

- When a phase is picked up: create `specs/NNN-slug/` with `plan.md`,
  `spec.md`, `tasks.md`, then flip `📋` to `✅` here.
- When a phase is fully shipped: flip `✅` to `🛠️` and link the
  release tag.
- If priorities shift: reorder freely, update the dependency notes
  when the order affects them.
