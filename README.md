# XPeak

A gym app with an RPG layer. Log a workout, earn XP, level up, unlock themed titles (e.g., level 50 = Mage, level 100 = War God) and take on challenges alone or with a group. Every check-in is backed by media proof (photo or video), so there's no "trust me, I trained".

This document is the **initial product brief**. It exists to guide the split of the work into phases (`specs/NNN-slug/tasks.md`) and to align decisions before we start coding.

---

## 1. Product vision

Turn the habit of training into an addictive progression loop: consistency rewards you, absence doesn't punish in a frustrating way, and the social layer (group/challenge) pulls people back when motivation drops. The product should be fun to open even on a rest day — group progress feed, XP accrued, next title, challenge leaderboard.

**Not** a general-purpose social network. **It is** a gamified tracker where workout proof is mandatory and progression is the core loop.

---

## 2. Target audience

- Intermediate/advanced practitioners who already train and want to keep frequency
- Groups of friends that push each other (CrossFit class, running duo, neighborhood crew)
- Personal trainers who want to follow up on students without spreadsheets
- Mobile-first users: someone who opens the app in the locker room, checks in in 15 seconds, and closes it

---

## 3. Gameplay pillars

### 3.1. Check-in as proof
- Every XP unit is born from a check-in.
- A check-in **requires media** (photo or video, multiple attachments allowed).
- A check-in is always tied to a **workout category** (chest, legs, running, mobility, etc.).
- Metadata: date/time, optional location (GPS or free-text place), duration, notes.

### 3.2. XP and levels
- Each check-in has a base XP defined by its category.
- XP is adjusted by multipliers (see 3.3).
- Level curve: logarithmic progression (level 1→2 is cheap, 99→100 is expensive). The formula lives as a domain constant so it's tweakable.
- Level persists, XP never decreases (breaking a streak doesn't punish the past — the only punishment is losing the future multiplier).

### 3.3. XP multipliers
- **Frequency (streak):** consecutive days with at least one check-in raise the multiplier (e.g., 3 days = 1.1x, 7 days = 1.25x, 14 days = 1.5x, configurable cap).
- **Category:** each category has its own weight (e.g., legs is worth more than abs). Configurable by app admin and potentially by challenge owner.
- **Challenge bonus:** while participating in an active challenge, valid check-ins can carry an extra multiplier defined by the challenge.
- **Composite rules:** multipliers stack in a controlled way (e.g., multiplicative up to a cap so it doesn't become an exploit).

### 3.4. Themed titles per level range
- Configurable tiers: `{ minLevel, maxLevel, title, theme }`.
- Example: 1–9 "Apprentice", 10–24 "Warrior", 25–49 "Knight", 50–99 "Mage", 100+ "War God".
- **Customizable themes:** the user picks a theme (medieval, sci-fi, Greek mythology, anime) and titles change accordingly while the tiers stay put.
- The title shows up on the profile, feed, and leaderboard.

### 3.5. Workout categories
- Category record with: name, icon, base XP, weight (default multiplier), specific rules.
- Core system categories + custom categories per challenge owner (optional, see 3.6).

### 3.6. Challenges
- A challenge is a goal with a time scope (e.g., "30 days of legs", "500 km in 3 months").
- **Solo:** the user runs the challenge against themselves. Progress and personal historical ranking.
- **Group:** multiple users together. Internal leaderboard, shared XP (or individual, depending on challenge config), group chat/feed.
- Challenge config: title, description, period, valid categories, extra multiplier, win rule (total XP, consecutive days, numeric category target), participant cap, public/private.
- Invitation via link or code.

---

## 4. Main functional requirements

- Sign up / login (auth provider TBD — email+password, OAuth, magic link)
- Profile with current title, level, XP, streak, check-in history
- Personal feed and group feed
- Check-in flow in ≤ 3 taps + media capture
- Multiple media upload per check-in (photo + video)
- Challenge creation and management (solo/group)
- Group invitation system (link, code, QR)
- Leaderboard inside the challenge
- Push notifications (streak at risk, someone from the group checked in, new title unlocked)
- Per-user title theme configuration

---

## 5. Non-functional requirements

- **Mobile-first, always.** Designed for a small screen, one hand, offline-tolerant (check-in queues if there's no internet).
- **TDD is mandatory across ALL phases.** No production code without a failing test first. Backend with xUnit + FluentAssertions, frontend with Vitest/Testing Library, E2E with Playwright or Detox (TBD).
- **Performance:** feed opens in < 1s on 4G. Media upload does not block the UI (background upload).
- **Privacy:** check-in media is private by default (visible only to the owner and groups where the check-in was shared). No public indexing.
- **Accessibility:** AA contrast, labels on icons, alt text on every image, keyboard navigation on the web fallback.
- **i18n:** en-US as default going forward, architecture ready for pt-BR (and others).

---

## 6. Tech stack

### Frontend
- **Next.js** (App Router) wrapped with **Capacitor** for Android/iOS.
- Web works as a fallback (PWA), but the product is primarily a native app.
- Strict TypeScript.
- Remote state: TanStack Query.
- UI: TBD (Tailwind + shadcn/ui is a strong candidate).
- Tests: Vitest + Testing Library + Playwright (web E2E) / Detox (native E2E).

### Backend
- **C# + ASP.NET Core 9** (Minimal APIs, `dotnet new webapi --use-controllers false --auth None`).
- Postgres as the primary database, accessed via **Entity Framework Core 9** (`Npgsql.EntityFrameworkCore.PostgreSQL`).
- **Hangfire** for background jobs (Postgres-backed storage, dashboard UI for admin).
- Media storage: TBD provider (S3/R2/GCS) via `AWSSDK.S3` or equivalent — the wrapper picked when Phase 6 lands.
- Auth: `Microsoft.AspNetCore.Identity` + JWT bearer + `Microsoft.AspNetCore.Authentication.Google` for OAuth.
- Rate limiting: `Microsoft.AspNetCore.RateLimiting` (built-in from .NET 7+).
- Tests: **xUnit** + **FluentAssertions** + `Microsoft.AspNetCore.Mvc.Testing` (`WebApplicationFactory<T>`) + `Testcontainers.PostgreSql` for real DB in integration tests.
- Style: `dotnet format` (roslynator rules optional).
- Structured logging: **Serilog** with JSON sink (added when observability phase lands).

### Infra
- Monorepo with **pnpm workspaces** (JS) and a separate directory for the .NET app.
- CI: GitHub Actions with parallel jobs (test, build, commitlint) covering both sides.
- Deploy: TBD (Fly.io, Azure App Service, self-hosted Kamal — decided when we reach that phase).

---

## 7. Proposed repository layout

```
xpeak/
  apps/
    mobile/          # Next.js + Capacitor (frontend + native wrapper)
    api/             # ASP.NET Core 9 minimal API (backend)
  packages/
    shared/          # types, DTOs, shared constants
  specs/
    NNN-slug/
      plan.md        # high-level plan
      spec.md        # detailed specification
      tasks.md       # trackable T-NNN-XX tasks
    roadmap.md       # phase ordering across the project
    ideas.md         # unprioritized product backlog
  docs/
    adr/             # Architecture Decision Records
```

---

## 8. Data model (initial sketch)

Not the final schema — just enough to align vocabulary:

- **User** — id, email, name, avatar, theme_preference, current_level, current_xp, current_streak_days, longest_streak_days
- **Category** — id, name, icon, base_xp, weight_multiplier, is_system
- **CheckIn** — id, user_id, category_id, performed_at, duration_minutes, notes, location, xp_earned (calculated), multiplier_snapshot (JSON)
- **CheckInMedia** — id, check_in_id, kind (photo|video), storage_key, order
- **LevelTitleTier** — id, theme, min_level, max_level, title
- **Theme** — id, name, description, tiers (via LevelTitleTier)
- **Challenge** — id, owner_id, title, description, starts_at, ends_at, mode (solo|group), visibility (public|private), rules (JSON), xp_multiplier
- **ChallengeCategory** — challenge_id, category_id (valid categories)
- **ChallengeMembership** — id, challenge_id, user_id, joined_at, role (owner|member)
- **ChallengeInvite** — id, challenge_id, code, expires_at, max_uses, used_count
- **Notification** — id, user_id, kind, payload, read_at

---

## 9. Roadmap

Full phase-by-phase plan lives in `specs/roadmap.md` (single source of truth for ordering). Short version, grouped in blocks:

- **Block A — Foundation:** versioning + release automation, application skeleton (Phoenix API + Next.js/Capacitor).
- **Block B — User core & progression engine:** auth + profile, categories + XP domain, check-in (no media then with media), streak & category multipliers.
- **Block C — Themed progression & configuration surface:** themed titles, editable XP/level configuration.
- **Block D — Cosmetics catalog & first achievements:** profile customization (avatar, banner, frame), streak medals.
- **Block E — Social graph, timeline & discovery:** friends (add by @username, profile view, privacy), activity feed of friends' events (check-ins, medals, trophies, level ups), global rankings (all-time / weekly / monthly / streak / per-category).
- **Block F — Challenges:** solo, friend, group (public browseable vs. private share-link-only), insignias for completion, group bonuses & group streak, trophies (customizable artwork).
- **Block G — Personal metrics & retrospectives:** always-on metrics dashboard (totals, category breakdown, streak heatmap, multiplier history), "Year in XPeak" story-style recap.
- **Block H — Visual depth & exploration:** level "evolution forms", gyms/locations.
- **Block I — Communication & polish:** push notifications, mobile polish + PWA fallback.

Each phase starts with tests before code (global TDD rule). Product ideas not yet scheduled live in `specs/ideas.md`.

---

## 10. Open questions (decide before slicing)

- **Auth provider:** own email+password + JWT (ASP.NET Identity), OAuth (Google/Apple), magic link?
- **Media storage:** S3, R2, GCS, self-hosted MinIO?
- **Push notifications:** Firebase Cloud Messaging + APNs via Capacitor, or OneSignal?
- **Design system:** Tailwind + shadcn/ui, or something more native like Ionic Components?
- **API contract:** OpenAPI generated by ASP.NET (built-in from .NET 9) with codegen to the front, or GraphQL (HotChocolate), or manually typed REST?
- **Media moderation:** are we going to have it? (third-party photo/video, accidental nudity, spam)
- **Monetization:** free / freemium / paid? This changes infra decisions early.
- **Multi-tenant (gym as a client):** in the MVP or V2?
- **Group XP rule:** does each participant earn their normal XP and the group sums it? Or is there a separate "group" XP?
- **Streak break:** does it only lose the multiplier, or is there a "life" (streak freeze)?

---

## 11. Local setup (draft, will be updated per phase)

```sh
# Postgres (host or container)
docker compose up -d postgres

# JS side
pnpm install
pnpm dev                             # runs mobile on :3001

# API side (Docker-first — no local .NET SDK needed)
docker compose up -d api             # http://localhost:5000

# Or if you want the .NET SDK on host (optional)
cd apps/api
dotnet restore
dotnet run                           # http://localhost:5000
```

---

## 12. Project conventions

- Conventional Commits, English-only.
- Branch naming: `TAG-NNN/felpa-<name>`.
- Commit tag at end of subject: `[XPK-N]`.
- `main` is protected: no direct pushes; every change lands via PR.
- TDD across the whole codebase — no production code without a failing test first.
- ADRs for cross-cutting decisions in `docs/adr/NNNN-title.md`.
- `DOCS/` for living module documentation (created starting at phase 3).

---

## 13. Status

Initial brief. Nothing implemented yet beyond the monorepo skeleton. Next step: align the open questions in §10, create `specs/001-xpeak-mvp/tasks.md` with the phases in §9 broken into `T-001-XX`, and open the first working branch.
